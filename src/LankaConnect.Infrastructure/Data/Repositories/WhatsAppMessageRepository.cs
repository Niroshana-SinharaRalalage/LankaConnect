using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.Enums;
using Serilog.Context;
using System.Diagnostics;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Phase 7A: Repository implementation for WhatsAppMessageRecord entities.
/// Follows existing codebase patterns with structured logging and observability.
/// </summary>
public class WhatsAppMessageRepository : Repository<WhatsAppMessageRecord>, IWhatsAppMessageRepository
{
    private readonly ILogger<WhatsAppMessageRepository> _repoLogger;

    public WhatsAppMessageRepository(
        AppDbContext context,
        ILogger<WhatsAppMessageRepository> logger) : base(context)
    {
        _repoLogger = logger;
    }

    public async Task<WhatsAppMessageRecord?> GetByAcsMessageIdAsync(string acsMessageId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(acsMessageId))
        {
            _repoLogger.LogWarning("GetByAcsMessageIdAsync called with null/empty acsMessageId");
            return null;
        }

        using (LogContext.PushProperty("Operation", "GetByAcsMessageId"))
        using (LogContext.PushProperty("EntityType", "WhatsAppMessageRecord"))
        using (LogContext.PushProperty("AcsMessageId", acsMessageId))
        {
            var stopwatch = Stopwatch.StartNew();
            _repoLogger.LogDebug("GetByAcsMessageIdAsync START: AcsMessageId={AcsMessageId}", acsMessageId);

            try
            {
                var result = await _dbSet
                    .FirstOrDefaultAsync(m => m.AcsMessageId == acsMessageId, ct);

                stopwatch.Stop();
                _repoLogger.LogInformation(
                    "GetByAcsMessageIdAsync COMPLETE: AcsMessageId={AcsMessageId}, Found={Found}, Duration={ElapsedMs}ms",
                    acsMessageId, result != null, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _repoLogger.LogError(ex,
                    "GetByAcsMessageIdAsync FAILED: AcsMessageId={AcsMessageId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    acsMessageId, stopwatch.ElapsedMilliseconds, ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");
                throw;
            }
        }
    }

    public async Task<IReadOnlyList<WhatsAppMessageRecord>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        using (LogContext.PushProperty("Operation", "GetByUserId"))
        using (LogContext.PushProperty("EntityType", "WhatsAppMessageRecord"))
        using (LogContext.PushProperty("UserId", userId))
        {
            var stopwatch = Stopwatch.StartNew();
            _repoLogger.LogDebug("GetByUserIdAsync START: UserId={UserId}", userId);

            try
            {
                var result = await _dbSet
                    .Where(m => m.UserId == userId)
                    .OrderByDescending(m => m.CreatedAt)
                    .AsNoTracking()
                    .ToListAsync(ct);

                stopwatch.Stop();
                _repoLogger.LogInformation(
                    "GetByUserIdAsync COMPLETE: UserId={UserId}, Count={Count}, Duration={ElapsedMs}ms",
                    userId, result.Count, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _repoLogger.LogError(ex,
                    "GetByUserIdAsync FAILED: UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    userId, stopwatch.ElapsedMilliseconds, ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");
                throw;
            }
        }
    }

    public async Task<IReadOnlyList<WhatsAppMessageRecord>> GetByEventIdAsync(Guid eventId, CancellationToken ct = default)
    {
        using (LogContext.PushProperty("Operation", "GetByEventId"))
        using (LogContext.PushProperty("EntityType", "WhatsAppMessageRecord"))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var stopwatch = Stopwatch.StartNew();
            _repoLogger.LogDebug("GetByEventIdAsync START: EventId={EventId}", eventId);

            try
            {
                var result = await _dbSet
                    .Where(m => m.EventId == eventId)
                    .OrderByDescending(m => m.CreatedAt)
                    .AsNoTracking()
                    .ToListAsync(ct);

                stopwatch.Stop();
                _repoLogger.LogInformation(
                    "GetByEventIdAsync COMPLETE: EventId={EventId}, Count={Count}, Duration={ElapsedMs}ms",
                    eventId, result.Count, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _repoLogger.LogError(ex,
                    "GetByEventIdAsync FAILED: EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    eventId, stopwatch.ElapsedMilliseconds, ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");
                throw;
            }
        }
    }

    public async Task<IReadOnlyList<WhatsAppMessageRecord>> GetPendingRetryAsync(int maxCount, CancellationToken ct = default)
    {
        using (LogContext.PushProperty("Operation", "GetPendingRetry"))
        using (LogContext.PushProperty("EntityType", "WhatsAppMessageRecord"))
        using (LogContext.PushProperty("MaxCount", maxCount))
        {
            var stopwatch = Stopwatch.StartNew();
            _repoLogger.LogDebug("GetPendingRetryAsync START: MaxCount={MaxCount}", maxCount);

            try
            {
                var result = await _dbSet
                    .Where(m => m.Status == WhatsAppMessageStatus.Failed)
                    .Where(m => m.RetryCount < m.MaxRetries)
                    .OrderBy(m => m.FailedAt)
                    .Take(maxCount)
                    .ToListAsync(ct);

                stopwatch.Stop();
                _repoLogger.LogInformation(
                    "GetPendingRetryAsync COMPLETE: MaxCount={MaxCount}, Count={Count}, Duration={ElapsedMs}ms",
                    maxCount, result.Count, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _repoLogger.LogError(ex,
                    "GetPendingRetryAsync FAILED: MaxCount={MaxCount}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    maxCount, stopwatch.ElapsedMilliseconds, ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");
                throw;
            }
        }
    }

    public async Task<IReadOnlyList<WhatsAppMessageRecord>> GetScheduledMessagesAsync(DateTime before, CancellationToken ct = default)
    {
        using (LogContext.PushProperty("Operation", "GetScheduledMessages"))
        using (LogContext.PushProperty("EntityType", "WhatsAppMessageRecord"))
        using (LogContext.PushProperty("Before", before))
        {
            var stopwatch = Stopwatch.StartNew();
            _repoLogger.LogDebug("GetScheduledMessagesAsync START: Before={Before}", before);

            try
            {
                var result = await _dbSet
                    .Where(m => m.Status == WhatsAppMessageStatus.Scheduled)
                    .Where(m => m.ScheduledFor.HasValue && m.ScheduledFor <= before)
                    .OrderBy(m => m.ScheduledFor)
                    .ToListAsync(ct);

                stopwatch.Stop();
                _repoLogger.LogInformation(
                    "GetScheduledMessagesAsync COMPLETE: Before={Before}, Count={Count}, Duration={ElapsedMs}ms",
                    before, result.Count, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _repoLogger.LogError(ex,
                    "GetScheduledMessagesAsync FAILED: Before={Before}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    before, stopwatch.ElapsedMilliseconds, ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");
                throw;
            }
        }
    }

    public async Task<bool> HasRecentDuplicateAsync(Guid? userId, Guid? eventId, string templateName, int withinMinutes = 5, CancellationToken ct = default)
    {
        using (LogContext.PushProperty("Operation", "HasRecentDuplicate"))
        using (LogContext.PushProperty("EntityType", "WhatsAppMessageRecord"))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("EventId", eventId))
        using (LogContext.PushProperty("TemplateName", templateName))
        {
            var stopwatch = Stopwatch.StartNew();
            _repoLogger.LogDebug(
                "HasRecentDuplicateAsync START: UserId={UserId}, EventId={EventId}, TemplateName={TemplateName}, WithinMinutes={WithinMinutes}",
                userId, eventId, templateName, withinMinutes);

            try
            {
                var cutoff = DateTime.UtcNow.AddMinutes(-withinMinutes);

                var result = await _dbSet
                    .AsNoTracking()
                    .AnyAsync(m =>
                        m.UserId == userId &&
                        m.EventId == eventId &&
                        m.TemplateName == templateName &&
                        m.CreatedAt >= cutoff &&
                        m.Status != WhatsAppMessageStatus.Failed, ct);

                stopwatch.Stop();
                _repoLogger.LogInformation(
                    "HasRecentDuplicateAsync COMPLETE: UserId={UserId}, EventId={EventId}, TemplateName={TemplateName}, HasDuplicate={HasDuplicate}, Duration={ElapsedMs}ms",
                    userId, eventId, templateName, result, stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _repoLogger.LogError(ex,
                    "HasRecentDuplicateAsync FAILED: UserId={UserId}, EventId={EventId}, TemplateName={TemplateName}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    userId, eventId, templateName, stopwatch.ElapsedMilliseconds, ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");
                throw;
            }
        }
    }

    public async Task<WhatsAppMessageMetrics> GetMetricsAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        using (LogContext.PushProperty("Operation", "GetMetrics"))
        using (LogContext.PushProperty("EntityType", "WhatsAppMessageRecord"))
        using (LogContext.PushProperty("From", from))
        using (LogContext.PushProperty("To", to))
        {
            var stopwatch = Stopwatch.StartNew();
            _repoLogger.LogDebug("GetMetricsAsync START: From={From}, To={To}", from, to);

            try
            {
                var messages = await _dbSet
                    .AsNoTracking()
                    .Where(m => m.CreatedAt >= from && m.CreatedAt <= to)
                    .ToListAsync(ct);

                var metrics = new WhatsAppMessageMetrics
                {
                    TotalSent = messages.Count(m => m.SentAt.HasValue),
                    TotalDelivered = messages.Count(m => m.DeliveredAt.HasValue),
                    TotalRead = messages.Count(m => m.ReadAt.HasValue),
                    TotalFailed = messages.Count(m => m.FailedAt.HasValue),
                    ByTemplate = messages
                        .Where(m => !string.IsNullOrEmpty(m.TemplateName))
                        .GroupBy(m => m.TemplateName!)
                        .ToDictionary(g => g.Key, g => g.Count())
                };

                stopwatch.Stop();
                _repoLogger.LogInformation(
                    "GetMetricsAsync COMPLETE: From={From}, To={To}, TotalSent={TotalSent}, TotalDelivered={TotalDelivered}, TotalFailed={TotalFailed}, Duration={ElapsedMs}ms",
                    from, to, metrics.TotalSent, metrics.TotalDelivered, metrics.TotalFailed, stopwatch.ElapsedMilliseconds);

                return metrics;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _repoLogger.LogError(ex,
                    "GetMetricsAsync FAILED: From={From}, To={To}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    from, to, stopwatch.ElapsedMilliseconds, ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");
                throw;
            }
        }
    }
}
