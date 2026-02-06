using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Application.Common.Interfaces;
using Serilog;
using Serilog.Context;
using System.Diagnostics;
using DomainEmailMessage = LankaConnect.Domain.Communications.Entities.EmailMessage;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for EmailMessage entities with specialized email queue operations
/// Follows TDD principles and integrates Result pattern for error handling
/// Phase 6A.X: Enhanced with comprehensive observability logging
/// </summary>
public class EmailMessageRepository : Repository<DomainEmailMessage>, IEmailMessageRepository
{
    private readonly ILogger<EmailMessageRepository> _repoLogger;

    public EmailMessageRepository(
        AppDbContext context,
        ILogger<EmailMessageRepository> logger) : base(context)
    {
        _repoLogger = logger;
    }

    public async Task<IReadOnlyList<DomainEmailMessage>> GetQueuedEmailsAsync(int batchSize = 50, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetQueuedEmails"))
        using (LogContext.PushProperty("EntityType", "EmailMessage"))
        using (LogContext.PushProperty("BatchSize", batchSize))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetQueuedEmailsAsync START: BatchSize={BatchSize}", batchSize);

            try
            {
                var result = await _dbSet
                    .Where(e => e.Status == EmailStatus.Queued)
                    .OrderBy(e => e.Priority)
                    .ThenBy(e => e.CreatedAt) // FIFO within priority
                    .Take(batchSize)
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetQueuedEmailsAsync COMPLETE: BatchSize={BatchSize}, Count={Count}, Duration={ElapsedMs}ms",
                    batchSize,
                    result.Count,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetQueuedEmailsAsync FAILED: BatchSize={BatchSize}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    batchSize,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<DomainEmailMessage>> GetFailedEmailsForRetryAsync(CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetFailedEmailsForRetry"))
        using (LogContext.PushProperty("EntityType", "EmailMessage"))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetFailedEmailsForRetryAsync START");

            try
            {
                var now = DateTime.UtcNow;
                var result = await _context.Set<DomainEmailMessage>()
                    .Where(e => e.Status == EmailStatus.Failed)
                    .Where(e => e.NextRetryAt.HasValue && e.NextRetryAt <= now)
                    .Where(e => e.CanRetry())
                    .OrderBy(e => e.NextRetryAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetFailedEmailsForRetryAsync COMPLETE: Count={Count}, Duration={ElapsedMs}ms",
                    result.Count,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetFailedEmailsForRetryAsync FAILED: Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<DomainEmailMessage>> GetEmailsByStatusAsync(EmailStatus status, int limit = 100, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetEmailsByStatus"))
        using (LogContext.PushProperty("EntityType", "EmailMessage"))
        using (LogContext.PushProperty("Status", status))
        using (LogContext.PushProperty("Limit", limit))
        {
            var stopwatch = Stopwatch.StartNew();
            _repoLogger.LogDebug("GetEmailsByStatusAsync START: Status={Status}, Limit={Limit}", status, limit);

            try
            {
                var result = await _context.Set<DomainEmailMessage>()
                    .Where(e => e.Status == status)
                    .OrderByDescending(e => e.CreatedAt)
                    .Take(limit)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();
                _repoLogger.LogInformation("GetEmailsByStatusAsync COMPLETE: Status={Status}, Limit={Limit}, Count={Count}, Duration={ElapsedMs}ms",
                    status, limit, result.Count, stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _repoLogger.LogError(ex, "GetEmailsByStatusAsync FAILED: Status={Status}, Limit={Limit}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    status, limit, stopwatch.ElapsedMilliseconds, ex.Message, (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");
                throw;
            }
        }
    }

    public async Task<IReadOnlyList<DomainEmailMessage>> GetEmailsByTypeAsync(EmailType type, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetEmailsByType"))
        using (LogContext.PushProperty("EntityType", "EmailMessage"))
        using (LogContext.PushProperty("EmailType", type))
        using (LogContext.PushProperty("FromDate", fromDate))
        using (LogContext.PushProperty("ToDate", toDate))
        {
            var stopwatch = Stopwatch.StartNew();
            _repoLogger.LogDebug("GetEmailsByTypeAsync START: Type={EmailType}, FromDate={FromDate}, ToDate={ToDate}",
                type, fromDate, toDate);

            try
            {
                var query = _context.Set<DomainEmailMessage>().Where(e => e.Type == type);

                if (fromDate.HasValue)
                    query = query.Where(e => e.CreatedAt >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(e => e.CreatedAt <= toDate.Value);

                var result = await query
                    .OrderByDescending(e => e.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();
                _repoLogger.LogInformation("GetEmailsByTypeAsync COMPLETE: Type={EmailType}, Count={Count}, Duration={ElapsedMs}ms",
                    type, result.Count, stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _repoLogger.LogError(ex, "GetEmailsByTypeAsync FAILED: Type={EmailType}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    type, stopwatch.ElapsedMilliseconds, ex.Message, (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");
                throw;
            }
        }
    }

    public async Task<EmailQueueStats> GetEmailQueueStatsAsync(CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetEmailQueueStats"))
        using (LogContext.PushProperty("EntityType", "EmailMessage"))
        {
            var stopwatch = Stopwatch.StartNew();
            _repoLogger.LogDebug("GetEmailQueueStatsAsync START");

            try
            {
                var stats = await _context.Set<DomainEmailMessage>()
                    .GroupBy(e => e.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync(cancellationToken);

                var statsDict = stats.ToDictionary(s => s.Status, s => s.Count);

                var now = DateTime.UtcNow;
                var retryableFailedCount = await _context.Set<DomainEmailMessage>()
                    .Where(e => e.Status == EmailStatus.Failed)
                    .Where(e => e.NextRetryAt.HasValue && e.NextRetryAt <= now)
                    .Where(e => e.RetryCount < e.MaxRetries)
                    .CountAsync(cancellationToken);

                var result = new EmailQueueStats(
                    statsDict.GetValueOrDefault(EmailStatus.Pending, 0),
                    statsDict.GetValueOrDefault(EmailStatus.Queued, 0),
                    statsDict.GetValueOrDefault(EmailStatus.Sending, 0),
                    statsDict.GetValueOrDefault(EmailStatus.Sent, 0),
                    statsDict.GetValueOrDefault(EmailStatus.Delivered, 0),
                    statsDict.GetValueOrDefault(EmailStatus.Failed, 0),
                    retryableFailedCount
                );

                stopwatch.Stop();
                _repoLogger.LogInformation("GetEmailQueueStatsAsync COMPLETE: TotalEmails={TotalEmails}, Duration={ElapsedMs}ms",
                    statsDict.Values.Sum(), stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _repoLogger.LogError(ex, "GetEmailQueueStatsAsync FAILED: Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    stopwatch.ElapsedMilliseconds, ex.Message, (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");
                throw;
            }
        }
    }

    /// <summary>
    /// Bulk operation to mark multiple emails as processing with Result pattern
    /// </summary>
    public async Task<Result<int>> MarkAsProcessingAsync(
        IEnumerable<Guid> emailIds,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "MarkAsProcessing"))
        using (LogContext.PushProperty("EntityType", "EmailMessage"))
        {
            var stopwatch = Stopwatch.StartNew();
            var ids = emailIds.ToList();

            _repoLogger.LogDebug("MarkAsProcessingAsync START: Count={Count}", ids.Count);

            try
            {
                if (!ids.Any())
                {
                    stopwatch.Stop();
                    _repoLogger.LogInformation("MarkAsProcessingAsync COMPLETE: Empty input, SuccessCount=0, Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                    return Result<int>.Success(0);
                }

                var emails = await _dbSet
                    .Where(e => ids.Contains(e.Id) && e.Status == EmailStatus.Queued)
                    .ToListAsync(cancellationToken);

                int successCount = 0;
                foreach (var email in emails)
                {
                    var result = email.MarkAsSending();
                    if (result.IsSuccess)
                    {
                        successCount++;
                    }
                    else
                    {
                        _repoLogger.LogWarning("Failed to mark email {EmailId} as sending: {Error}",
                            email.Id, result.Error);
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);

                stopwatch.Stop();
                _repoLogger.LogInformation("MarkAsProcessingAsync COMPLETE: Count={Count}, SuccessCount={SuccessCount}, Duration={ElapsedMs}ms",
                    ids.Count, successCount, stopwatch.ElapsedMilliseconds);
                return Result<int>.Success(successCount);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                stopwatch.Stop();
                _repoLogger.LogWarning(ex, "MarkAsProcessingAsync FAILED: Concurrency conflict, Duration={ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds);
                return Result<int>.Failure("Concurrency conflict: emails may have been processed by another instance");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _repoLogger.LogError(ex, "MarkAsProcessingAsync FAILED: Count={Count}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    ids.Count, stopwatch.ElapsedMilliseconds, ex.Message, (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");
                return Result<int>.Failure($"Failed to mark emails as processing: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Creates email message with comprehensive error handling and Result pattern
    /// </summary>
    public async Task<Result<DomainEmailMessage>> CreateEmailMessageAsync(
        DomainEmailMessage emailMessage,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "CreateEmailMessage"))
        using (LogContext.PushProperty("EntityType", "EmailMessage"))
        using (LogContext.PushProperty("EmailId", emailMessage.Id))
        using (LogContext.PushProperty("EmailType", emailMessage.Type))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("CreateEmailMessageAsync START: EmailId={EmailId}, EmailType={EmailType}",
                emailMessage.Id, emailMessage.Type);

            try
            {
                await AddAsync(emailMessage, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);

                stopwatch.Stop();
                _repoLogger.LogInformation("CreateEmailMessageAsync COMPLETE: EmailId={EmailId}, EmailType={EmailType}, Duration={ElapsedMs}ms",
                    emailMessage.Id, emailMessage.Type, stopwatch.ElapsedMilliseconds);
                return Result<DomainEmailMessage>.Success(emailMessage);
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                stopwatch.Stop();
                _repoLogger.LogWarning("CreateEmailMessageAsync FAILED: Duplicate EmailId={EmailId}, Duration={ElapsedMs}ms",
                    emailMessage.Id, stopwatch.ElapsedMilliseconds);
                return Result<DomainEmailMessage>.Failure("Email message with the same identifier already exists");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _repoLogger.LogError(ex, "CreateEmailMessageAsync FAILED: EmailId={EmailId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    emailMessage.Id, stopwatch.ElapsedMilliseconds, ex.Message, (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");
                return Result<DomainEmailMessage>.Failure($"Failed to create email message: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Gets status counts for monitoring and reporting
    /// </summary>
    public async Task<Dictionary<EmailStatus, int>> GetStatusCountsAsync(
        DateTime? fromDate = null,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetStatusCounts"))
        using (LogContext.PushProperty("EntityType", "EmailMessage"))
        using (LogContext.PushProperty("FromDate", fromDate))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetStatusCountsAsync START: FromDate={FromDate}", fromDate);

            try
            {
                var query = _dbSet.AsNoTracking();

                if (fromDate.HasValue)
                    query = query.Where(e => e.CreatedAt >= fromDate.Value);

                var result = await query
                    .GroupBy(e => e.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation("GetStatusCountsAsync COMPLETE: FromDate={FromDate}, TotalStatuses={TotalStatuses}, Duration={ElapsedMs}ms",
                    fromDate, result.Count, stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex, "GetStatusCountsAsync FAILED: FromDate={FromDate}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    fromDate, stopwatch.ElapsedMilliseconds, ex.Message, (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");
                throw;
            }
        }
    }

    /// <summary>
    /// Gets filtered email messages with pagination
    /// </summary>
    public async Task<List<DomainEmailMessage>> GetFilteredAsync(
        Guid? userId,
        string? emailAddress,
        EmailType? emailType,
        EmailStatus? status,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetFilteredEmails"))
        using (LogContext.PushProperty("EntityType", "EmailMessage"))
        using (LogContext.PushProperty("PageNumber", pageNumber))
        using (LogContext.PushProperty("PageSize", pageSize))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetFilteredAsync START: PageNumber={PageNumber}, PageSize={PageSize}, UserId={UserId}, EmailType={EmailType}, Status={Status}",
                pageNumber, pageSize, userId, emailType, status);

            try
            {
                var query = _dbSet.AsNoTracking();

                // Apply filters
                if (userId.HasValue)
                    query = query.Where(e => e.ToEmails.Any(email => email.Contains(userId.ToString()!)));

                if (!string.IsNullOrWhiteSpace(emailAddress))
                    query = query.Where(e => e.ToEmails.Any(email => email.Contains(emailAddress)));

                if (emailType.HasValue)
                    query = query.Where(e => e.Type == emailType.Value);

                if (status.HasValue)
                    query = query.Where(e => e.Status == status.Value);

                if (fromDate.HasValue)
                    query = query.Where(e => e.CreatedAt >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(e => e.CreatedAt <= toDate.Value);

                // Apply pagination
                var result = await query
                    .OrderByDescending(e => e.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation("GetFilteredAsync COMPLETE: PageNumber={PageNumber}, PageSize={PageSize}, Count={Count}, Duration={ElapsedMs}ms",
                    pageNumber, pageSize, result.Count, stopwatch.ElapsedMilliseconds);
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex, "GetFilteredAsync FAILED: PageNumber={PageNumber}, PageSize={PageSize}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    pageNumber, pageSize, stopwatch.ElapsedMilliseconds, ex.Message, (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");
                throw;
            }
        }
    }

    /// <summary>
    /// Gets count of filtered email messages
    /// </summary>
    public async Task<int> GetFilteredCountAsync(
        Guid? userId,
        string? emailAddress,
        EmailType? emailType,
        EmailStatus? status,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetFilteredEmailsCount"))
        using (LogContext.PushProperty("EntityType", "EmailMessage"))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetFilteredCountAsync START: UserId={UserId}, EmailType={EmailType}, Status={Status}",
                userId, emailType, status);

            try
            {
                var query = _dbSet.AsNoTracking();

                // Apply same filters as GetFilteredAsync
                if (userId.HasValue)
                    query = query.Where(e => e.ToEmails.Any(email => email.Contains(userId.ToString()!)));

                if (!string.IsNullOrWhiteSpace(emailAddress))
                    query = query.Where(e => e.ToEmails.Any(email => email.Contains(emailAddress)));

                if (emailType.HasValue)
                    query = query.Where(e => e.Type == emailType.Value);

                if (status.HasValue)
                    query = query.Where(e => e.Status == status.Value);

                if (fromDate.HasValue)
                    query = query.Where(e => e.CreatedAt >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(e => e.CreatedAt <= toDate.Value);

                var count = await query.CountAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation("GetFilteredCountAsync COMPLETE: Count={Count}, Duration={ElapsedMs}ms",
                    count, stopwatch.ElapsedMilliseconds);
                return count;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex, "GetFilteredCountAsync FAILED: Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    stopwatch.ElapsedMilliseconds, ex.Message, (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");
                throw;
            }
        }
    }

    /// <summary>
    /// Helper method to detect unique constraint violations
    /// </summary>
    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        return ex.InnerException?.Message?.Contains("duplicate key") == true ||
               ex.InnerException?.Message?.Contains("UNIQUE constraint") == true;
    }
}