using System.Collections.Concurrent;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Shared.Email.Observability;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Phase 6A.99: Email failure detail persistence imports

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
    // Phase 6A.99: Changed from ConcurrentBag to ConcurrentQueue for FIFO ordering
    private readonly ConcurrentQueue<EmailFailureRecord> _failedEmails = new();
    private readonly ConcurrentQueue<ValidationFailureRecord> _validationFailures = new();

    // Pending changes to flush to database
    private readonly ConcurrentDictionary<string, PendingMetricUpdate> _pendingUpdates = new();
    // Phase 6A.99: Pending failure details to flush to database
    private readonly ConcurrentQueue<EmailFailureDetail> _pendingFailureDetails = new();

    // Background flush timer
    private Timer? _flushTimer;
    private readonly TimeSpan _flushInterval = TimeSpan.FromSeconds(30);
    private bool _disposed;

    // Phase 6A.99: Increased from 100 to 1000 for better debugging capability
    private const int MaxFailureRecords = 1000;

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
        // Phase 6A.99: Clear ConcurrentQueues properly
        while (_failedEmails.TryDequeue(out _)) { }
        while (_validationFailures.TryDequeue(out _)) { }
        while (_pendingFailureDetails.TryDequeue(out _)) { }
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
            // Phase 6A.99: Changed to ConcurrentQueue for FIFO ordering
            // Trim old records if limit exceeded (FIFO - remove oldest first)
            while (_failedEmails.Count >= MaxFailureRecords)
            {
                _failedEmails.TryDequeue(out _);
            }

            var record = new EmailFailureRecord
            {
                CorrelationId = correlationId,
                TemplateName = templateName,
                RecipientEmail = recipientEmail,
                ErrorMessage = errorMessage,
                HandlerName = handlerName,
                Timestamp = DateTime.UtcNow
            };
            _failedEmails.Enqueue(record);

            // Phase 6A.99: Queue for database persistence (email will be encrypted when flushing)
            var failureDetail = EmailFailureDetail.Create(
                correlationId ?? string.Empty,
                templateName ?? "unknown",
                recipientEmail ?? string.Empty, // Will be encrypted during flush
                errorMessage ?? "No error message",
                handlerName);
            _pendingFailureDetails.Enqueue(failureDetail);

            _logger.LogWarning("[Phase 6A.99] Failed email recorded and queued for persistence: CorrelationId={CorrelationId}, Template={Template}, Error={Error}",
                correlationId, templateName, errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Metrics] Error recording failed email");
        }
    }

    public IReadOnlyList<EmailFailureRecord> GetFailedEmails()
    {
        // Phase 6A.99: Return ordered by timestamp descending (most recent first)
        return _failedEmails.ToArray().OrderByDescending(f => f.Timestamp).ToList();
    }

    public void RecordValidationFailureDetails(string correlationId, string templateName, List<string> missingParameters, string handlerName)
    {
        try
        {
            // Phase 6A.99: Changed to ConcurrentQueue for FIFO ordering
            // Trim old records if limit exceeded (FIFO - remove oldest first)
            while (_validationFailures.Count >= MaxFailureRecords)
            {
                _validationFailures.TryDequeue(out _);
            }

            _validationFailures.Enqueue(new ValidationFailureRecord
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
        // Phase 6A.99: Return ordered by timestamp descending (most recent first)
        return _validationFailures.ToArray().OrderByDescending(f => f.Timestamp).ToList();
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

        // Phase 6A.99: Load failure details from database
        await LoadFailureDetailsFromDatabaseAsync(dbContext, cancellationToken);
    }

    /// <summary>
    /// Phase 6A.99: Load recent failure details from database on startup
    /// </summary>
    private async Task LoadFailureDetailsFromDatabaseAsync(IApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        try
        {
            var encryptionService = _scopeFactory.CreateScope().ServiceProvider.GetService<IEmailEncryptionService>();
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

            var recentFailures = await ((DbContext)dbContext).Set<EmailFailureDetail>()
                .Where(f => f.Timestamp >= thirtyDaysAgo && f.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(f => f.Timestamp)
                .Take(MaxFailureRecords)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("[Phase 6A.99] Loading {Count} failure details from database", recentFailures.Count);

            // Clear existing in-memory queue and repopulate
            while (_failedEmails.TryDequeue(out _)) { }

            // Add in chronological order so queue maintains proper FIFO order
            foreach (var failure in recentFailures.OrderBy(f => f.Timestamp))
            {
                // Decrypt email if encryption service available
                var email = failure.RecipientEmailEncrypted;
                if (encryptionService != null && !string.IsNullOrEmpty(email))
                {
                    try
                    {
                        email = encryptionService.Decrypt(email);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[Phase 6A.99] Could not decrypt email for failure {FailureId}", failure.Id);
                        email = "[encrypted]";
                    }
                }

                _failedEmails.Enqueue(new EmailFailureRecord
                {
                    CorrelationId = failure.CorrelationId ?? string.Empty,
                    TemplateName = failure.TemplateName,
                    RecipientEmail = email ?? string.Empty,
                    ErrorMessage = failure.ErrorMessage,
                    HandlerName = failure.HandlerName ?? string.Empty,
                    Timestamp = failure.Timestamp
                });
            }

            _logger.LogInformation("[Phase 6A.99] Loaded {Count} failure details from database - failures now survive restarts", _failedEmails.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Phase 6A.99] Failed to load failure details from database - starting with empty failures list");
        }
    }

    private async Task FlushToDatabaseAsync()
    {
        var hasMetrics = !_pendingUpdates.IsEmpty;
        var hasFailures = !_pendingFailureDetails.IsEmpty;

        if (!hasMetrics && !hasFailures)
        {
            _logger.LogDebug("[Phase 6A.89] No pending metrics or failures to flush");
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            // Flush metrics
            if (hasMetrics)
            {
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
            }

            // Phase 6A.99: Flush failure details
            if (hasFailures)
            {
                FlushFailureDetailsToDatabaseSync(scope, dbContext);
            }

            await dbContext.CommitAsync();
            _logger.LogDebug("[Phase 6A.89/6A.99] Metrics and failure details flushed to database successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Phase 6A.89] Failed to flush to database");
        }
    }

    /// <summary>
    /// Phase 6A.99: Flush pending failure details to database with email encryption
    /// </summary>
    private void FlushFailureDetailsToDatabaseSync(IServiceScope scope, IApplicationDbContext dbContext)
    {
        if (_pendingFailureDetails.IsEmpty)
            return;

        try
        {
            var encryptionService = scope.ServiceProvider.GetService<IEmailEncryptionService>();
            var detailsToSave = new List<EmailFailureDetail>();
            var count = 0;

            while (_pendingFailureDetails.TryDequeue(out var detail) && count < 100) // Batch max 100 at a time
            {
                count++;

                // Encrypt email before saving if encryption service is available
                if (encryptionService != null && !string.IsNullOrEmpty(detail.RecipientEmailEncrypted))
                {
                    // The RecipientEmailEncrypted field currently contains plain email
                    // We need to create a new entity with encrypted email
                    var encryptedEmail = encryptionService.Encrypt(detail.RecipientEmailEncrypted);
                    var encryptedDetail = EmailFailureDetail.Create(
                        detail.CorrelationId ?? string.Empty,
                        detail.TemplateName,
                        encryptedEmail,
                        detail.ErrorMessage,
                        detail.HandlerName);
                    detailsToSave.Add(encryptedDetail);
                }
                else
                {
                    // No encryption service - store as-is (not recommended for production)
                    detailsToSave.Add(detail);
                }
            }

            if (detailsToSave.Count > 0)
            {
                ((DbContext)dbContext).Set<EmailFailureDetail>().AddRange(detailsToSave);
                _logger.LogInformation("[Phase 6A.99] Queued {Count} failure details for database persistence", detailsToSave.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Phase 6A.99] Failed to flush failure details to database");
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
