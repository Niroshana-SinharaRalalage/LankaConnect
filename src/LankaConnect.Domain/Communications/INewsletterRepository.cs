using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.Enums;

namespace LankaConnect.Domain.Communications;

/// <summary>
/// Repository interface for Newsletter aggregate
/// Phase 6A.74: Newsletter management
/// </summary>
public interface INewsletterRepository : IRepository<Newsletter>
{
    /// <summary>
    /// Gets newsletters created by a specific user
    /// </summary>
    Task<IReadOnlyList<Newsletter>> GetByCreatorAsync(Guid createdByUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets newsletters by status
    /// </summary>
    Task<IReadOnlyList<Newsletter>> GetByStatusAsync(NewsletterStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets newsletters linked to a specific event
    /// </summary>
    Task<IReadOnlyList<Newsletter>> GetByEventAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets Active newsletters that have expired (ExpiresAt <= NOW())
    /// </summary>
    Task<IReadOnlyList<Newsletter>> GetExpiredNewslettersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets published (Active) newsletters for public display
    /// </summary>
    Task<IReadOnlyList<Newsletter>> GetPublishedNewslettersAsync(int limit = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets published (Active) newsletters with filtering and location-based sorting
    /// Phase 6A.74 Parts 10 & 11: Public newsletter list page
    /// </summary>
    Task<IReadOnlyList<Newsletter>> GetPublishedWithFiltersAsync(
        DateTime? publishedFrom = null,
        DateTime? publishedTo = null,
        string? state = null,
        List<Guid>? metroAreaIds = null,
        string? searchTerm = null,
        Guid? userId = null,
        decimal? latitude = null,
        decimal? longitude = null,
        CancellationToken cancellationToken = default);
}
