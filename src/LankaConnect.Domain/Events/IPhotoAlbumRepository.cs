using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Entities;

namespace LankaConnect.Domain.Events;

/// <summary>
/// Repository interface for PhotoAlbum aggregate.
/// Extends IRepository for standard CRUD + adds album-specific queries.
/// Supports multiple albums per event.
/// </summary>
public interface IPhotoAlbumRepository : IRepository<PhotoAlbum>
{
    /// <summary>
    /// Get all albums for an event.
    /// Returns list ordered by CreatedAt ASC.
    /// </summary>
    Task<List<PhotoAlbum>> GetAllByEventIdAsync(Guid eventId, bool trackChanges = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get album by ID with change tracking control.
    /// Includes photos collection for aggregate operations.
    /// </summary>
    Task<PhotoAlbum?> GetByIdAsync(Guid albumId, bool trackChanges, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if an album with the given name already exists for this event.
    /// Used for duplicate name validation before create/update.
    /// </summary>
    Task<bool> ExistsByEventIdAndNameAsync(Guid eventId, string name, CancellationToken cancellationToken = default);

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
    /// Get expired photos for cleanup service.
    /// Returns photos where ExpiresAt <= now, limited to batchSize for batch processing.
    /// </summary>
    Task<IReadOnlyList<AlbumPhoto>> GetExpiredPhotosAsync(
        int batchSize,
        CancellationToken cancellationToken = default);
}
