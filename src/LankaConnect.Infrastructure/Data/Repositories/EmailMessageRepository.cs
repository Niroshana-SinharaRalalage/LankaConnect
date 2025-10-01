using Microsoft.EntityFrameworkCore;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Application.Common.Interfaces;
using Serilog;
using Serilog.Context;
using DomainEmailMessage = LankaConnect.Domain.Communications.Entities.EmailMessage;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for EmailMessage entities with specialized email queue operations
/// Follows TDD principles and integrates Result pattern for error handling
/// </summary>
public class EmailMessageRepository : Repository<DomainEmailMessage>, IEmailMessageRepository
{
    public EmailMessageRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<DomainEmailMessage>> GetQueuedEmailsAsync(int batchSize = 50, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetQueuedEmails"))
        using (LogContext.PushProperty("BatchSize", batchSize))
        {
            _logger.Debug("Getting queued emails with batch size {BatchSize}", batchSize);
            
            var result = await _dbSet
                .Where(e => e.Status == EmailStatus.Queued)
                .OrderBy(e => e.Priority)
                .ThenBy(e => e.CreatedAt) // FIFO within priority
                .Take(batchSize)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
                
            _logger.Debug("Retrieved {Count} queued emails", result.Count);
            return result;
        }
    }

    public async Task<IReadOnlyList<DomainEmailMessage>> GetFailedEmailsForRetryAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.Set<DomainEmailMessage>()
            .Where(e => e.Status == EmailStatus.Failed)
            .Where(e => e.NextRetryAt.HasValue && e.NextRetryAt <= now)
            .Where(e => e.CanRetry())
            .OrderBy(e => e.NextRetryAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DomainEmailMessage>> GetEmailsByStatusAsync(EmailStatus status, int limit = 100, CancellationToken cancellationToken = default)
    {
        return await _context.Set<DomainEmailMessage>()
            .Where(e => e.Status == status)
            .OrderByDescending(e => e.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DomainEmailMessage>> GetEmailsByTypeAsync(EmailType type, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<DomainEmailMessage>().Where(e => e.Type == type);

        if (fromDate.HasValue)
            query = query.Where(e => e.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(e => e.CreatedAt <= toDate.Value);

        return await query
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<EmailQueueStats> GetEmailQueueStatsAsync(CancellationToken cancellationToken = default)
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

        return new EmailQueueStats(
            statsDict.GetValueOrDefault(EmailStatus.Pending, 0),
            statsDict.GetValueOrDefault(EmailStatus.Queued, 0),
            statsDict.GetValueOrDefault(EmailStatus.Sending, 0),
            statsDict.GetValueOrDefault(EmailStatus.Sent, 0),
            statsDict.GetValueOrDefault(EmailStatus.Delivered, 0),
            statsDict.GetValueOrDefault(EmailStatus.Failed, 0),
            retryableFailedCount
        );
    }

    /// <summary>
    /// Bulk operation to mark multiple emails as processing with Result pattern
    /// </summary>
    public async Task<Result<int>> MarkAsProcessingAsync(
        IEnumerable<Guid> emailIds, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var ids = emailIds.ToList();
            if (!ids.Any())
                return Result<int>.Success(0);

            using (LogContext.PushProperty("Operation", "MarkAsProcessing"))
            using (LogContext.PushProperty("Count", ids.Count))
            {
                _logger.Information("Marking {Count} emails as processing", ids.Count);
                
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
                        _logger.Warning("Failed to mark email {EmailId} as sending: {Error}", 
                            email.Id, result.Error);
                    }
                }

                await _context.SaveChangesAsync(cancellationToken);
                
                _logger.Information("Marked {SuccessCount} emails as sending", successCount);
                return Result<int>.Success(successCount);
            }
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.Warning(ex, "Concurrency conflict while marking emails as processing");
            return Result<int>.Failure("Concurrency conflict: emails may have been processed by another instance");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to mark emails as processing");
            return Result<int>.Failure($"Failed to mark emails as processing: {ex.Message}");
        }
    }

    /// <summary>
    /// Creates email message with comprehensive error handling and Result pattern
    /// </summary>
    public async Task<Result<DomainEmailMessage>> CreateEmailMessageAsync(
        DomainEmailMessage emailMessage, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            using (LogContext.PushProperty("Operation", "CreateEmailMessage"))
            using (LogContext.PushProperty("EmailId", emailMessage.Id))
            using (LogContext.PushProperty("EmailType", emailMessage.Type))
            {
                await AddAsync(emailMessage, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                
                _logger.Information("Email message created successfully {EmailId} of type {EmailType}", 
                    emailMessage.Id, emailMessage.Type);
                return Result<DomainEmailMessage>.Success(emailMessage);
            }
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            _logger.Warning("Email message creation failed due to duplicate {EmailId}", emailMessage.Id);
            return Result<DomainEmailMessage>.Failure("Email message with the same identifier already exists");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to create email message {EmailId}", emailMessage.Id);
            return Result<DomainEmailMessage>.Failure($"Failed to create email message: {ex.Message}");
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
        using (LogContext.PushProperty("FromDate", fromDate))
        {
            _logger.Debug("Getting status counts from {FromDate}", fromDate);
            
            var query = _dbSet.AsNoTracking();
            
            if (fromDate.HasValue)
                query = query.Where(e => e.CreatedAt >= fromDate.Value);

            var result = await query
                .GroupBy(e => e.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);
            
            _logger.Debug("Retrieved status counts: {@StatusCounts}", result);
            return result;
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
        {
            _logger.Debug("Getting filtered emails with pagination: Page {PageNumber}, Size {PageSize}", pageNumber, pageSize);
            
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
                
            _logger.Debug("Retrieved {Count} filtered emails", result.Count);
            return result;
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
        {
            _logger.Debug("Getting filtered emails count");
            
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
            _logger.Debug("Filtered emails count: {Count}", count);
            return count;
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