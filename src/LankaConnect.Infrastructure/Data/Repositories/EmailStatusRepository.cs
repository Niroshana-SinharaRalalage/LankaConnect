using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Communications.ValueObjects;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System.Diagnostics;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Email status repository that delegates to EmailMessageRepository
/// Provides status-specific queries and analytics
/// Phase 6A.X: Enhanced with comprehensive observability logging
/// </summary>
public class EmailStatusRepository : IEmailStatusRepository
{
    private readonly IEmailMessageRepository _emailMessageRepository;
    private readonly ILogger<EmailStatusRepository> _logger;

    public EmailStatusRepository(
        IEmailMessageRepository emailMessageRepository,
        ILogger<EmailStatusRepository> logger)
    {
        _emailMessageRepository = emailMessageRepository;
        _logger = logger;
    }

    public async Task<Dictionary<EmailStatus, int>> GetStatusCountsAsync(DateTime? fromDate = null, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetStatusCounts"))
        using (LogContext.PushProperty("EntityType", "EmailStatus"))
        using (LogContext.PushProperty("FromDate", fromDate))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogDebug("GetStatusCountsAsync START: FromDate={FromDate}", fromDate);

            try
            {
                var result = await _emailMessageRepository.GetStatusCountsAsync(fromDate, cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetStatusCountsAsync COMPLETE: FromDate={FromDate}, StatusCount={StatusCount}, Duration={ElapsedMs}ms",
                    fromDate,
                    result.Count,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetStatusCountsAsync FAILED: FromDate={FromDate}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    fromDate,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    public async Task<EmailQueueStats> GetQueueStatsAsync(CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetQueueStats"))
        using (LogContext.PushProperty("EntityType", "EmailStatus"))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogDebug("GetQueueStatsAsync START");

            try
            {
                var result = await _emailMessageRepository.GetEmailQueueStatsAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetQueueStatsAsync COMPLETE: QueuedCount={QueuedCount}, SendingCount={SendingCount}, FailedCount={FailedCount}, TotalCount={TotalCount}, Duration={ElapsedMs}ms",
                    result.QueuedCount,
                    result.SendingCount,
                    result.FailedCount,
                    result.TotalCount,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetQueueStatsAsync FAILED: Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    public async Task<Dictionary<DateTime, Dictionary<EmailStatus, int>>> GetStatusTrendsAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetStatusTrends"))
        using (LogContext.PushProperty("EntityType", "EmailStatus"))
        using (LogContext.PushProperty("FromDate", fromDate))
        using (LogContext.PushProperty("ToDate", toDate))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogDebug("GetStatusTrendsAsync START: FromDate={FromDate}, ToDate={ToDate}", fromDate, toDate);

            try
            {
                // For now, return a simple implementation
                // In a real scenario, this would query by date ranges and group by day
                var trends = new Dictionary<DateTime, Dictionary<EmailStatus, int>>();
                var currentCounts = await GetStatusCountsAsync(fromDate, cancellationToken);

                trends[DateTime.UtcNow.Date] = currentCounts;

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetStatusTrendsAsync COMPLETE: FromDate={FromDate}, ToDate={ToDate}, DateCount={DateCount}, Duration={ElapsedMs}ms",
                    fromDate,
                    toDate,
                    trends.Count,
                    stopwatch.ElapsedMilliseconds);

                return trends;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetStatusTrendsAsync FAILED: FromDate={FromDate}, ToDate={ToDate}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    fromDate,
                    toDate,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    public async Task<List<Domain.Communications.Entities.EmailMessage>> GetEmailStatusAsync(
        Guid? userId,
        string? emailAddress,
        Domain.Communications.Enums.EmailType? emailType,
        Domain.Communications.Enums.EmailStatus? status,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetEmailStatus"))
        using (LogContext.PushProperty("EntityType", "EmailStatus"))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("EmailType", emailType))
        using (LogContext.PushProperty("Status", status))
        using (LogContext.PushProperty("PageNumber", pageNumber))
        using (LogContext.PushProperty("PageSize", pageSize))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogDebug(
                "GetEmailStatusAsync START: UserId={UserId}, Email={EmailAddress}, Type={EmailType}, Status={Status}, PageNumber={PageNumber}, PageSize={PageSize}",
                userId, emailAddress, emailType, status, pageNumber, pageSize);

            try
            {
                var result = await _emailMessageRepository.GetFilteredAsync(
                    userId, emailAddress, emailType, status, fromDate, toDate,
                    pageNumber, pageSize, cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetEmailStatusAsync COMPLETE: UserId={UserId}, Type={EmailType}, Status={Status}, PageNumber={PageNumber}, PageSize={PageSize}, Count={Count}, Duration={ElapsedMs}ms",
                    userId,
                    emailType,
                    status,
                    pageNumber,
                    pageSize,
                    result.Count,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetEmailStatusAsync FAILED: UserId={UserId}, Type={EmailType}, Status={Status}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    userId,
                    emailType,
                    status,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    public async Task<int> GetEmailStatusCountAsync(
        Guid? userId,
        string? emailAddress,
        Domain.Communications.Enums.EmailType? emailType,
        Domain.Communications.Enums.EmailStatus? status,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetEmailStatusCount"))
        using (LogContext.PushProperty("EntityType", "EmailStatus"))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("EmailType", emailType))
        using (LogContext.PushProperty("Status", status))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogDebug(
                "GetEmailStatusCountAsync START: UserId={UserId}, Email={EmailAddress}, Type={EmailType}, Status={Status}",
                userId, emailAddress, emailType, status);

            try
            {
                var count = await _emailMessageRepository.GetFilteredCountAsync(
                    userId, emailAddress, emailType, status, fromDate, toDate, cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetEmailStatusCountAsync COMPLETE: UserId={UserId}, Type={EmailType}, Status={Status}, Count={Count}, Duration={ElapsedMs}ms",
                    userId,
                    emailType,
                    status,
                    count,
                    stopwatch.ElapsedMilliseconds);

                return count;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetEmailStatusCountAsync FAILED: UserId={UserId}, Type={EmailType}, Status={Status}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    userId,
                    emailType,
                    status,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }
}
