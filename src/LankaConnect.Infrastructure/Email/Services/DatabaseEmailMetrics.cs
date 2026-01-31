using System.Collections.Concurrent;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Shared.Email.Observability;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Infrastructure.Email.Services;

/// <summary>
/// Phase 6A.89: Database-backed implementation of IEmailMetrics with hybrid persistence.
///
/// Problem Solved:
/// - DefaultEmailMetrics stored all metrics in memory
/// - Azure Container Apps restarts caused complete data loss
/// - Dashboard showed 0 for all metrics after container restart
///
/// Solution:
/// - Hybrid approach: In-memory cache for real-time performance + database for durability
/// - Periodic background flush to database (every 30 seconds)
/// - Loads historical data from database on startup
/// - Metrics survive container restarts
///
/// Architecture:
/// - Real-time updates go to in-memory dictionaries (fast)
/// - Background task periodically syncs to database (durable)
/// - On startup, loads existing metrics from database (recovery)
/// </summary>
public class DatabaseEmailMetrics : IEmailMetrics, IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DatabaseEmailMetrics> _logger;

    // In-memory cache for real-time performance
    private readonly ConcurrentDictionary<string, TemplateMetrics> _templateMetrics = new();
    private readonly ConcurrentDictionary<string, HandlerMetrics> _handlerMetrics = new();
    private readonly ConcurrentBag<EmailFailureRecord> _failedEmails = new();
    private readonly ConcurrentBag<ValidationFailureRecord> _validationFailures = new();

    // Pending changes to flush to database
    private readonly ConcurrentDictionary<string, PendingMetricUpdate> _pendingUpdates = new();

    // Background flush timer
    private Timer? _flushTimer;
    private readonly TimeSpan _flushInterval = TimeSpan.FromSeconds(30);
    private bool _disposed;

    // Maximum records to keep in memory
    private const int MaxFailureRecords = 100;

    public DatabaseEmailMetrics(
        IServiceScopeFactory scopeFactory,
        ILogger<DatabaseEmailMetrics> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region IHostedService Implementation

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[Phase 6A.89] DatabaseEmailMetrics starting - loading historical data from database");

        try
        {
            await LoadMetricsFromDatabaseAsync(cancellationToken);
            _logger.LogInformation("[Phase 6A.89] Historical metrics loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Phase 6A.89] Failed to load historical metrics from database - starting with empty metrics");
        }

        // Start periodic flush timer
        _flushTimer = new Timer(
            async _ => await FlushToDatabaseAsync(),
            null,
            _flushInterval,
            _flushInterval);

        _logger.LogInformation("[Phase 6A.89] DatabaseEmailMetrics started - flush interval: {Interval}s", _flushInterval.TotalSeconds);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[Phase 6A.89] DatabaseEmailMetrics stopping - flushing pending metrics to database");

        // Stop the timer
        if (_flushTimer != null)
        {
            await _flushTimer.DisposeAsync();
            _flushTimer = null;
        }

        // Final flush
        try
        {
            await FlushToDatabaseAsync();
            _logger.LogInformation("[Phase 6A.89] Final metrics flush completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Phase 6A.89] Failed to flush metrics during shutdown");
        }
    }

    #endregion

    #region IEmailMetrics Implementation

    public void RecordEmailSent(string templateName, int durationMs, bool success)
    {
        try
        {
            // Update in-memory metrics
            var metrics = _templateMetrics.GetOrAdd(templateName, _ => new TemplateMetrics());
            lock (metrics)
            {
                metrics.TotalSent++;
                metrics.TotalDurationMs += durationMs;
                metrics.AverageDurationMs = metrics.TotalSent > 0 ? metrics.TotalDurationMs / metrics.TotalSent : 0;

                if (success)
                    metrics.SuccessCount++;
                else
                    metrics.FailureCount++;
            }

            // Queue pending update for database
            var pendingUpdate = _pendingUpdates.GetOrAdd(templateName, _ => new PendingMetricUpdate());
            lock (pendingUpdate)
            {
                pendingUpdate.TotalSent++;
                pendingUpdate.TotalDurationMs += durationMs;
                if (success)
                    pendingUpdate.Successful++;
                else
                    pendingUpdate.Failed++;
            }

            _logger.LogDebug("[Metrics] RecordEmailSent: Template={Template}, Duration={Duration}ms, Success={Success}",
                templateName, durationMs, success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Metrics] Error recording email sent for template {Template}", templateName);
        }
    }

    public void RecordParameterValidationFailure(string templateName)
    {
        try
        {
            var metrics = _templateMetrics.GetOrAdd(templateName, _ => new TemplateMetrics());
            lock (metrics)
            {
                metrics.ValidationFailures++;
            }

            var pendingUpdate = _pendingUpdates.GetOrAdd(templateName, _ => new PendingMetricUpdate());
            lock (pendingUpdate)
            {
                pendingUpdate.ValidationFailures++;
            }

            _logger.LogWarning("[Metrics] Validation failure recorded for template {Template}", templateName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Metrics] Error recording validation failure for template {Template}", templateName);
        }
    }

    public void RecordTemplateNotFound(string templateName)
    {
        try
        {
            var metrics = _templateMetrics.GetOrAdd(templateName, _ => new TemplateMetrics());
            lock (metrics)
            {
                metrics.TemplateNotFoundCount++;
            }

            var pendingUpdate = _pendingUpdates.GetOrAdd(templateName, _ => new PendingMetricUpdate());
            lock (pendingUpdate)
            {
                pendingUpdate.TemplateNotFoundCount++;
            }

            _logger.LogError("[Metrics] Template not found: {Template}", templateName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Metrics] Error recording template not found for {Template}", templateName);
        }
    }

    public void RecordHandlerUsage(string handlerName, bool usedTypedParameters)
    {
        try
        {
            var metrics = _handlerMetrics.GetOrAdd(handlerName, _ => new HandlerMetrics());
            lock (metrics)
            {
                metrics.TotalEmailsSent++;
                if (usedTypedParameters)
                    metrics.TypedParameterUsageCount++;
                else
                    metrics.DictionaryParameterUsageCount++;
            }

            _logger.LogDebug("[Metrics] Handler usage recorded: Handler={Handler}, TypedParams={UsedTyped}",
                handlerName, usedTypedParameters);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Metrics] Error recording handler usage for {Handler}", handlerName);
        }
    }

    public TemplateMetrics GetStatsByTemplate(string templateName)
    {
        return _templateMetrics.GetValueOrDefault(templateName) ?? new TemplateMetrics();
    }

    public HandlerMetrics GetStatsByHandler(string handlerName)
    {
        return _handlerMetrics.GetValueOrDefault(handlerName) ?? new HandlerMetrics();
    }

    public GlobalMetrics GetGlobalStats()
    {
        var globalMetrics = new GlobalMetrics();
        foreach (var metrics in _templateMetrics.Values)
        {
            globalMetrics.TotalEmailsSent += metrics.TotalSent;
            globalMetrics.TotalSuccesses += metrics.SuccessCount;
            globalMetrics.TotalFailures += metrics.FailureCount;
        }
        return globalMetrics;
    }

    public void ResetMetrics()
    {
        _templateMetrics.Clear();
        _handlerMetrics.Clear();
        _pendingUpdates.Clear();
        _failedEmails.Clear();
        _validationFailures.Clear();
        _logger.LogInformation("[Metrics] All metrics reset");
    }

    public IReadOnlyDictionary<string, TemplateMetrics> GetAllTemplateStats()
    {
        return new Dictionary<string, TemplateMetrics>(_templateMetrics);
    }

    public IReadOnlyDictionary<string, HandlerMetrics> GetAllHandlerStats()
    {
        return new Dictionary<string, HandlerMetrics>(_handlerMetrics);
    }

    public void RecordFailedEmail(string correlationId, string templateName, string recipientEmail, string errorMessage, string handlerName)
    {
        try
        {
            // Trim old records if limit exceeded
            while (_failedEmails.Count >= MaxFailureRecords)
            {
                _failedEmails.TryTake(out _);
            }

            _failedEmails.Add(new EmailFailureRecord
            {
                CorrelationId = correlationId,
                TemplateName = templateName,
                RecipientEmail = recipientEmail,
                ErrorMessage = errorMessage,
                HandlerName = handlerName,
                Timestamp = DateTime.UtcNow
            });

            _logger.LogWarning("[Metrics] Failed email recorded: CorrelationId={CorrelationId}, Template={Template}, Error={Error}",
                correlationId, templateName, errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Metrics] Error recording failed email");
        }
    }

    public IReadOnlyList<EmailFailureRecord> GetFailedEmails()
    {
        return _failedEmails.ToList();
    }

    public void RecordValidationFailureDetails(string correlationId, string templateName, List<string> missingParameters, string handlerName)
    {
        try
        {
            // Trim old records if limit exceeded
            while (_validationFailures.Count >= MaxFailureRecords)
            {
                _validationFailures.TryTake(out _);
            }

            _validationFailures.Add(new ValidationFailureRecord
            {
                CorrelationId = correlationId,
                TemplateName = templateName,
                MissingParameters = new List<string>(missingParameters),
                HandlerName = handlerName,
                Timestamp = DateTime.UtcNow
            });

            _logger.LogWarning("[Metrics] Validation failure details recorded: CorrelationId={CorrelationId}, Template={Template}, MissingParams={Params}",
                correlationId, templateName, string.Join(", ", missingParameters));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Metrics] Error recording validation failure details");
        }
    }

    public IReadOnlyList<ValidationFailureRecord> GetValidationFailures()
    {
        return _validationFailures.ToList();
    }

    #endregion

    #region Database Operations

    private async Task LoadMetricsFromDatabaseAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        // Load metrics for today (most relevant for dashboard)
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var startDate = today.AddDays(-7); // Load last 7 days for historical context

        var records = await ((DbContext)dbContext).Set<EmailMetricRecord>()
            .Where(m => m.MetricDate >= startDate)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("[Phase 6A.89] Loaded {Count} metric records from database (last 7 days)", records.Count);

        // Aggregate into template metrics
        foreach (var record in records)
        {
            var templateName = record.TemplateName ?? "global";
            var metrics = _templateMetrics.GetOrAdd(templateName, _ => new TemplateMetrics());
            lock (metrics)
            {
                metrics.TotalSent += record.TotalSent;
                metrics.SuccessCount += record.Successful;
                metrics.FailureCount += record.Failed;
                metrics.TotalDurationMs += (int)record.TotalDurationMs;
                metrics.ValidationFailures += record.ValidationFailures;
                metrics.TemplateNotFoundCount += record.TemplateNotFoundCount;

                // Recalculate average
                metrics.AverageDurationMs = metrics.TotalSent > 0 ? metrics.TotalDurationMs / metrics.TotalSent : 0;
            }
        }
    }

    private async Task FlushToDatabaseAsync()
    {
        if (_pendingUpdates.IsEmpty)
        {
            _logger.LogDebug("[Phase 6A.89] No pending metrics to flush");
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var pendingCopy = _pendingUpdates.ToArray();

            foreach (var (templateName, pending) in pendingCopy)
            {
                if (pending.IsEmpty)
                    continue;

                try
                {
                    // Get or create record for today's date and this template
                    var existingRecord = await ((DbContext)dbContext).Set<EmailMetricRecord>()
                        .FirstOrDefaultAsync(m => m.MetricDate == today && m.TemplateName == templateName);

                    if (existingRecord == null)
                    {
                        existingRecord = EmailMetricRecord.Create(today, templateName);
                        ((DbContext)dbContext).Set<EmailMetricRecord>().Add(existingRecord);
                    }

                    // Merge pending updates
                    lock (pending)
                    {
                        existingRecord.MergeFrom(
                            pending.TotalSent,
                            pending.Successful,
                            pending.Failed,
                            pending.TotalDurationMs,
                            pending.ValidationFailures,
                            pending.TemplateNotFoundCount);

                        // Clear pending after merge
                        pending.Reset();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Phase 6A.89] Failed to flush metrics for template {Template}", templateName);
                }
            }

            await dbContext.CommitAsync();
            _logger.LogDebug("[Phase 6A.89] Metrics flushed to database successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Phase 6A.89] Failed to flush metrics to database");
        }
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _flushTimer?.Dispose();
        }

        _disposed = true;
    }

    #endregion

    /// <summary>
    /// Tracks pending metric updates to be flushed to database
    /// </summary>
    private class PendingMetricUpdate
    {
        public int TotalSent { get; set; }
        public int Successful { get; set; }
        public int Failed { get; set; }
        public long TotalDurationMs { get; set; }
        public int ValidationFailures { get; set; }
        public int TemplateNotFoundCount { get; set; }

        public bool IsEmpty =>
            TotalSent == 0 && Successful == 0 && Failed == 0 &&
            TotalDurationMs == 0 && ValidationFailures == 0 && TemplateNotFoundCount == 0;

        public void Reset()
        {
            TotalSent = 0;
            Successful = 0;
            Failed = 0;
            TotalDurationMs = 0;
            ValidationFailures = 0;
            TemplateNotFoundCount = 0;
        }
    }
}
