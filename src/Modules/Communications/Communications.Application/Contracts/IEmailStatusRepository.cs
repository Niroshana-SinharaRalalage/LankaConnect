using LankaConnect.Modules.Communications.Domain.Enums;
using LankaConnect.Modules.Communications.Domain.ValueObjects;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.BuildingBlocks.Domain.Enums;
namespace LankaConnect.BuildingBlocks.Application.Common.Interfaces;

/// <summary>
/// Repository interface for email status queries
/// Delegates to EmailMessageRepository for status-related operations
/// </summary>
public interface IEmailStatusRepository
{
    /// <summary>
    /// Gets email status counts for monitoring and dashboard
    /// </summary>
    Task<Dictionary<EmailStatus, int>> GetStatusCountsAsync(DateTime? fromDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets email queue statistics
    /// </summary>
    Task<EmailQueueStats> GetQueueStatsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets status trend data for reporting
    /// </summary>
    Task<Dictionary<DateTime, Dictionary<EmailStatus, int>>> GetStatusTrendsAsync(
        DateTime fromDate, 
        DateTime toDate, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets email statuses with filtering and pagination
    /// </summary>
    Task<List<Domain.Communications.Entities.EmailMessage>> GetEmailStatusAsync(
        Guid? userId,
        string? emailAddress,
        Domain.Communications.Enums.EmailType? emailType,
        Domain.Communications.Enums.EmailStatus? status,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets count of email statuses with filtering
    /// </summary>
    Task<int> GetEmailStatusCountAsync(
        Guid? userId,
        string? emailAddress,
        Domain.Communications.Enums.EmailType? emailType,
        Domain.Communications.Enums.EmailStatus? status,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default);
}
