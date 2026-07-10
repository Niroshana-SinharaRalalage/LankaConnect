using Microsoft.EntityFrameworkCore;
using LankaConnect.Modules.Media.Domain;
using LankaConnect.Modules.Media.Domain.Entities;
using LankaConnect.Modules.Media.Domain.Enums;
using LankaConnect.Modules.Media.Infrastructure.Data;
using Serilog;
using Serilog.Context;
namespace LankaConnect.Modules.Media.Infrastructure.Repositories;

/// <summary>
/// Hand-rolled <see cref="IPhotoAlbumRepository"/> implementation per ADR-010.
/// W4.2 capability extraction — injects <see cref="MediaDbContext"/> directly;
/// the legacy <c>LankaConnect.Infrastructure.Repository&lt;T&gt;</c> base is
/// not used (would create a transitional edge to the monolith assembly).
/// </summary>
/// <remarks>
/// Wave 6.5.b retired the self-saving pattern that used to live here (the class
/// remark documenting it was deleted along with the SaveChangesAsync calls
/// inside AddAsync/UpdateAsync/DeleteAsync). Callers now use
/// <see cref="LankaConnect.BuildingBlocks.Application.Common.Interfaces.IMultiContextUnitOfWork.CommitAsync(DbContext[], CancellationToken)"/>
/// so AppDbContext + MediaDbContext commit atomically inside a single Postgres
/// transaction — the F30a class of production data-loss cannot recur through
/// this repository.
/// </remarks>
public class PhotoAlbumRepository : IPhotoAlbumRepository
{
    private readonly MediaDbContext _context;
    private readonly DbSet<PhotoAlbum> _dbSet;
    private readonly ILogger _logger;

    public PhotoAlbumRepository(MediaDbContext context)
    {
        _context = context;
        _dbSet = context.Set<PhotoAlbum>();
        _logger = Log.ForContext<PhotoAlbumRepository>();
    }

    public async Task<PhotoAlbum?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await GetByIdAsync(id, trackChanges: true, cancellationToken);
    }

    public async Task<PhotoAlbum?> GetByIdAsync(Guid albumId, bool trackChanges, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetById"))
        using (LogContext.PushProperty("AlbumId", albumId))
        {
            _logger.Debug("Getting PhotoAlbum by ID {AlbumId}, trackChanges: {TrackChanges}", albumId, trackChanges);

            var query = trackChanges
                ? _dbSet.Include(a => a.Photos)
                : _dbSet.AsNoTracking().Include(a => a.Photos);

            return await query.FirstOrDefaultAsync(a => a.Id == albumId, cancellationToken);
        }
    }

    public async Task<List<PhotoAlbum>> GetAllByEventIdAsync(Guid eventId, bool trackChanges = false, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetAllByEventId"))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var query = trackChanges
                ? _dbSet.Include(a => a.Photos)
                : _dbSet.AsNoTracking().Include(a => a.Photos);

            return await query
                .Where(a => a.EventId == eventId)
                .OrderBy(a => a.CreatedAt)
                .ToListAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsByEventIdAndNameAsync(Guid eventId, string name, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(a => a.EventId == eventId && a.Name == name, cancellationToken);
    }

    public async Task<(IReadOnlyList<AlbumPhoto> Photos, bool HasMore)> GetApprovedPhotosAsync(
        Guid albumId,
        int pageSize,
        DateTime? cursor = null,
        CancellationToken cancellationToken = default)
    {
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

        return (photos, hasMore);
    }

    public async Task<IReadOnlyList<AlbumPhoto>> GetExpiredPhotosAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        return await _context.Set<AlbumPhoto>()
            .Where(p => p.ExpiresAt <= DateTime.UtcNow)
            .OrderBy(p => p.ExpiresAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(PhotoAlbum entity, CancellationToken cancellationToken = default)
    {
        // Wave 6.5.b: SaveChangesAsync deleted here (was line 117 pre-fix). The
        // caller MUST invoke IMultiContextUnitOfWork.CommitAsync(new DbContext[] { _mediaContext }, ct)
        // to persist. The change tracker holds the row until commit.
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    public Task UpdateAsync(PhotoAlbum entity, CancellationToken cancellationToken = default)
    {
        // Wave 6.5.b: SaveChangesAsync deleted. Same caller contract as AddAsync.
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(PhotoAlbum entity, CancellationToken cancellationToken = default)
    {
        // Wave 6.5.b: SaveChangesAsync deleted. Same caller contract as AddAsync.
        _dbSet.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<PhotoAlbum>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking().ToListAsync(cancellationToken);
    }
}
