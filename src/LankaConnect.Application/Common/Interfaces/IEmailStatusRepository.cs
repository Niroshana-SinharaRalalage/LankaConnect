using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Enterprise;
using LankaConnect.Domain.Common.Models;
using LankaConnect.Domain.Common.Monitoring;
using LankaConnect.Domain.Common.Security;
using LankaConnect.Domain.Common.Recovery;
using LankaConnect.Domain.Common.Database;
using LankaConnect.Domain.Common.Enums;
using MultiLanguageModels = LankaConnect.Domain.Common.Database.MultiLanguageRoutingModels;

namespace LankaConnect.Application.Common.Interfaces;

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