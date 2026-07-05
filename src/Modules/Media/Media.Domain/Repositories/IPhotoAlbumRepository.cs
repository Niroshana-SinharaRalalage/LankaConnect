using LankaConnect.Modules.Media.Domain.Entities;

namespace LankaConnect.Modules.Media.Domain;

/// <summary>
/// Repository interface for the PhotoAlbum aggregate.
/// Hand-rolled per ADR-010 (Repository-per-Aggregate) — no <c>IRepository&lt;T&gt;</c>
/// base extension. Each query has explicit named intent.
/// </summary>
/// <remarks>
/// W4.2 (2026-06-06): de-coupled from legacy <c>LankaConnect.BuildingBlocks.Domain.IRepository&lt;T&gt;</c>
/// per architect rule "module domain depends on BB only" — the generic CRUD members
/// (AddAsync, UpdateAsync, DeleteAsync, GetByIdAsync) are inlined here.
/// </remarks>
public interface IPhotoAlbumRepository
{
    /// <summary>Get a PhotoAlbum by ID with photos eagerly loaded; tracking enabled by default.</summary>
    Task<PhotoAlbum?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Get a PhotoAlbum by ID with explicit tracking control.</summary>
    Task<PhotoAlbum?> GetByIdAsync(Guid albumId, bool trackChanges, CancellationToken cancellationToken = default);

    /// <summary>Get all albums for an event, ordered by CreatedAt ASC.</summary>
    Task<List<PhotoAlbum>> GetAllByEventIdAsync(Guid eventId, bool trackChanges = false, CancellationToken cancellationToken = default);

    /// <summary>Check whether an album with the given name already exists for the event.</summary>
    Task<bool> ExistsByEventIdAndNameAsync(Guid eventId, string name, CancellationToken cancellationToken = default);

    /// <summary>Cursor-paginated approved photos for gallery display.</summary>
    Task<(IReadOnlyList<AlbumPhoto> Photos, bool HasMore)> GetApprovedPhotosAsync(
        Guid albumId,
        int pageSize,
        DateTime? cursor = null,
        CancellationToken cancellationToken = default);

    /// <summary>Batch of expired photos for the cleanup background service.</summary>
    Task<IReadOnlyList<AlbumPhoto>> GetExpiredPhotosAsync(int batchSize, CancellationToken cancellationToken = default);

    /// <summary>Add a new PhotoAlbum; saves immediately (self-saving repository, see PhotoAlbumRepository remarks).</summary>
    Task AddAsync(PhotoAlbum entity, CancellationToken cancellationToken = default);

    /// <summary>Update a tracked PhotoAlbum; saves immediately.</summary>
    Task UpdateAsync(PhotoAlbum entity, CancellationToken cancellationToken = default);

    /// <summary>Delete a PhotoAlbum; saves immediately.</summary>
    Task DeleteAsync(PhotoAlbum entity, CancellationToken cancellationToken = default);
}
