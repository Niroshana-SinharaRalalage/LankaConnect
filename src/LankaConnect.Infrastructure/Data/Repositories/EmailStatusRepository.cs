using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Communications.ValueObjects;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Email status repository that delegates to EmailMessageRepository
/// Provides status-specific queries and analytics
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
        _logger.LogDebug("Getting email status counts from date: {FromDate}", fromDate);
        
        return await _emailMessageRepository.GetStatusCountsAsync(fromDate, cancellationToken);
    }

    public async Task<EmailQueueStats> GetQueueStatsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting email queue statistics");
        
        return await _emailMessageRepository.GetEmailQueueStatsAsync(cancellationToken);
    }

    public async Task<Dictionary<DateTime, Dictionary<EmailStatus, int>>> GetStatusTrendsAsync(
        DateTime fromDate, 
        DateTime toDate, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting email status trends from {FromDate} to {ToDate}", fromDate, toDate);
        
        // For now, return a simple implementation
        // In a real scenario, this would query by date ranges and group by day
        var trends = new Dictionary<DateTime, Dictionary<EmailStatus, int>>();
        var currentCounts = await GetStatusCountsAsync(fromDate, cancellationToken);
        
        trends[DateTime.UtcNow.Date] = currentCounts;
        
        return trends;
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
        _logger.LogDebug("Getting email status with filters: UserId={UserId}, Type={EmailType}, Status={Status}", 
            userId, emailType, status);
        
        return await _emailMessageRepository.GetFilteredAsync(
            userId, emailAddress, emailType, status, fromDate, toDate, 
            pageNumber, pageSize, cancellationToken);
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
        _logger.LogDebug("Getting email status count with filters: UserId={UserId}, Type={EmailType}, Status={Status}", 
            userId, emailType, status);
        
        return await _emailMessageRepository.GetFilteredCountAsync(
            userId, emailAddress, emailType, status, fromDate, toDate, cancellationToken);
    }
}