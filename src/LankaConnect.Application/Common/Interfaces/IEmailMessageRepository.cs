using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Communications.ValueObjects;
using DomainEmailMessage = LankaConnect.Domain.Communications.Entities.EmailMessage;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Enterprise;
using LankaConnect.Domain.Common.Models;
using LankaConnect.Domain.Common.Monitoring;
using LankaConnect.Domain.Common.Security;
using LankaConnect.Domain.Common.Recovery;
using LankaConnect.Domain.Common.Database;
using MultiLanguageModels = LankaConnect.Domain.Common.Database.MultiLanguageRoutingModels;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Repository interface for EmailMessage entities with specialized email queue operations
/// Follows Clean Architecture principles with Result pattern integration
/// </summary>
public interface IEmailMessageRepository : IRepository<DomainEmailMessage>
{
    /// <summary>
    /// Gets queued emails ready for processing with priority ordering
    /// </summary>
    Task<IReadOnlyList<DomainEmailMessage>> GetQueuedEmailsAsync(int batchSize = 50, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets failed emails that are eligible for retry based on retry logic
    /// </summary>
    Task<IReadOnlyList<DomainEmailMessage>> GetFailedEmailsForRetryAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets emails filtered by status with optional limit
    /// </summary>
    Task<IReadOnlyList<DomainEmailMessage>> GetEmailsByStatusAsync(EmailStatus status, int limit = 100, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets emails filtered by type with optional date range
    /// </summary>
    Task<IReadOnlyList<DomainEmailMessage>> GetEmailsByTypeAsync(EmailType type, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets comprehensive email queue statistics for monitoring
    /// </summary>
    Task<EmailQueueStats> GetEmailQueueStatsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Marks queued emails as processing (sending) with Result pattern for error handling
    /// Maps to domain's MarkAsSending() method following proper state transitions
    /// </summary>
    Task<Result<int>> MarkAsProcessingAsync(IEnumerable<Guid> emailIds, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates email message with comprehensive validation and Result pattern
    /// </summary>
    Task<Result<DomainEmailMessage>> CreateEmailMessageAsync(DomainEmailMessage emailMessage, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets email status counts for monitoring and reporting
    /// </summary>
    Task<Dictionary<EmailStatus, int>> GetStatusCountsAsync(DateTime? fromDate = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets filtered email messages with pagination
    /// </summary>
    Task<List<DomainEmailMessage>> GetFilteredAsync(
        Guid? userId,
        string? emailAddress,
        EmailType? emailType,
        EmailStatus? status,
        DateTime? fromDate,
        DateTime? toDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets count of filtered email messages
    /// </summary>
    Task<int> GetFilteredCountAsync(
        Guid? userId,
        string? emailAddress,
        EmailType? emailType,
        EmailStatus? status,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken = default);
}

