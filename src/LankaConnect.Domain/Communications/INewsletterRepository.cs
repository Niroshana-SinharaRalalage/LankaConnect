using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.Enums;

namespace LankaConnect.Domain.Communications;

/// <summary>
/// Repository interface for Newsletter aggregate
/// Phase 6A.74: Newsletter/News Alert Feature
/// </summary>
public interface INewsletterRepository : IRepository<Newsletter>
{
    /// <summary>
    /// Gets all newsletters created by a specific user
    /// </summary>
    Task<IReadOnlyList<Newsletter>> GetByCreatorAsync(
        Guid createdByUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets newsletters filtered by status
    /// </summary>
    Task<IReadOnlyList<Newsletter>> GetByStatusAsync(
        NewsletterStatus status,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets newsletters linked to a specific event
    /// </summary>
    Task<IReadOnlyList<Newsletter>> GetByEventIdAsync(
        Guid eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active (published) newsletters for landing page display
    /// Only returns newsletters with Status = Active
    /// </summary>
    Task<IReadOnlyList<Newsletter>> GetPublishedNewslettersAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets expired newsletters (Active status with ExpiresAt <= now)
    /// Used by auto-deactivation background job
    /// </summary>
    Task<IReadOnlyList<Newsletter>> GetExpiredNewslettersAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets newsletters with pagination and filtering
    /// </summary>
    /// <param name="status">Optional status filter</param>
    /// <param name="createdByUserId">Optional creator filter</param>
    /// <param name="eventId">Optional event filter</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Items per page</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Paged list of newsletters with total count</returns>
    Task<(IReadOnlyList<Newsletter> Items, int TotalCount)> GetPagedAsync(
        NewsletterStatus? status = null,
        Guid? createdByUserId = null,
        Guid? eventId = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default);
}
