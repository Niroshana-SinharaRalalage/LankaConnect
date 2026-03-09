using Microsoft.EntityFrameworkCore;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using Serilog;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data.Repositories;

public class PhotoAlbumRepository : Repository<PhotoAlbum>, IPhotoAlbumRepository
{
    public PhotoAlbumRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<List<PhotoAlbum>> GetAllByEventIdAsync(Guid eventId, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetAllByEventId"))
        using (LogContext.PushProperty("EventId", eventId))
        {
            _logger.Debug("Getting all PhotoAlbums for EventId {EventId}, trackChanges: {TrackChanges}", eventId, trackChanges);

            var query = trackChanges
                ? _dbSet.Include(a => a.Photos)
                : _dbSet.AsNoTracking().Include(a => a.Photos);

            var result = await query
                .Where(a => a.EventId == eventId)
                .OrderBy(a => a.CreatedAt)
                .ToListAsync(cancellationToken);

            _logger.Debug("Found {Count} albums for EventId {EventId}", result.Count, eventId);
            return result;
        }
    }

    public async Task<PhotoAlbum?> GetByIdAsync(Guid albumId, bool trackChanges, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByIdTracked"))
        using (LogContext.PushProperty("AlbumId", albumId))
        {
            _logger.Debug("Getting PhotoAlbum by ID {AlbumId}, trackChanges: {TrackChanges}", albumId, trackChanges);

            var query = trackChanges
                ? _dbSet.Include(a => a.Photos)
                : _dbSet.AsNoTracking().Include(a => a.Photos);

            var result = await query.FirstOrDefaultAsync(a => a.Id == albumId, cancellationToken);

            _logger.Debug("PhotoAlbum with ID {AlbumId} {Result}", albumId, result != null ? "found" : "not found");
            return result;
        }
    }

    public override async Task<PhotoAlbum?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await GetByIdAsync(id, trackChanges: true, cancellationToken);
    }

    public async Task<bool> ExistsByEventIdAndNameAsync(Guid eventId, string name, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "ExistsByEventIdAndName"))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var exists = await _dbSet.AnyAsync(
                a => a.EventId == eventId && a.Name == name,
                cancellationToken);

            _logger.Debug("Album with name '{Name}' exists for EventId {EventId}: {Exists}", name, eventId, exists);
            return exists;
        }
    }

    public async Task<(IReadOnlyList<AlbumPhoto> Photos, bool HasMore)> GetApprovedPhotosAsync(
        Guid albumId,
        int pageSize,
        DateTime? cursor = null,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetApprovedPhotos"))
        using (LogContext.PushProperty("AlbumId", albumId))
        {
            _logger.Debug("Getting approved photos for AlbumId {AlbumId}, pageSize: {PageSize}, cursor: {Cursor}",
                albumId, pageSize, cursor);

            var query = _context.Set<AlbumPhoto>()
                .AsNoTracking()
                .Where(p => p.AlbumId == albumId && p.Status == AlbumPhotoStatus.Approved);

            if (cursor.HasValue)
                query = query.Where(p => p.UploadedAt < cursor.Value);

            var photos = await query
                .OrderByDescending(p => p.UploadedAt)
                .Take(pageSize + 1)
                .ToListAsync(cancellationToken);

            var hasMore = photos.Count > pageSize;
            if (hasMore)
                photos = photos.Take(pageSize).ToList();

            _logger.Debug("Retrieved {Count} approved photos for AlbumId {AlbumId}, hasMore: {HasMore}",
                photos.Count, albumId, hasMore);

            return (photos, hasMore);
        }
    }

    public async Task<IReadOnlyList<AlbumPhoto>> GetExpiredPhotosAsync(
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetExpiredPhotos"))
        {
            _logger.Debug("Getting expired photos, batchSize: {BatchSize}", batchSize);

            var photos = await _context.Set<AlbumPhoto>()
                .Where(p => p.ExpiresAt <= DateTime.UtcNow)
                .OrderBy(p => p.ExpiresAt)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            _logger.Debug("Retrieved {Count} expired photos", photos.Count);
            return photos;
        }
    }
}
