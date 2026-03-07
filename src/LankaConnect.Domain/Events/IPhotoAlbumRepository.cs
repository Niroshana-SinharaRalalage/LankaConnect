using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Entities;

namespace LankaConnect.Domain.Events;

/// <summary>
/// Repository interface for PhotoAlbum aggregate.
/// Extends IRepository for standard CRUD + adds album-specific queries.
/// </summary>
public interface IPhotoAlbumRepository : IRepository<PhotoAlbum>
{
    /// <summary>
    /// Get album by event ID (one album per event).
    /// Includes photos collection for aggregate operations.
    /// </summary>
    Task<PhotoAlbum?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get album by event ID with change tracking control.
    /// Use trackChanges: false for read-only queries (better performance).
    /// </summary>
    Task<PhotoAlbum?> GetByEventIdAsync(Guid eventId, bool trackChanges, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get album by ID with change tracking control.
    /// </summary>
    Task<PhotoAlbum?> GetByIdAsync(Guid albumId, bool trackChanges, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get paginated approved photos for gallery display.
    /// Uses cursor-based pagination for infinite scroll.
    /// </summary>
    Task<(IReadOnlyList<AlbumPhoto> Photos, bool HasMore)> GetApprovedPhotosAsync(
        Guid albumId,
        int pageSize,
        DateTime? cursor = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get photos pending approval (organizer moderation queue).
    /// </summary>
    Task<IReadOnlyList<AlbumPhoto>> GetPendingPhotosAsync(
        Guid albumId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get expired photos for cleanup service.
    /// Returns photos where ExpiresAt <= now, limited to batchSize for batch processing.
    /// </summary>
    Task<IReadOnlyList<AlbumPhoto>> GetExpiredPhotosAsync(
        int batchSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if an album already exists for this event.
    /// </summary>
    Task<bool> AlbumExistsForEventAsync(Guid eventId, CancellationToken cancellationToken = default);
}
