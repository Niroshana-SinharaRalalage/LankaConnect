using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Repositories;
using System.Diagnostics;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Phase 2B: Repository implementation for VenueLayout aggregate operations.
/// Loads full aggregate (layout → zones → seats) for domain operations.
/// </summary>
public class VenueLayoutRepository : Repository<VenueLayout>, IVenueLayoutRepository
{
    private readonly ILogger<VenueLayoutRepository> _repoLogger;

    public VenueLayoutRepository(
        AppDbContext context,
        ILogger<VenueLayoutRepository> logger) : base(context)
    {
        _repoLogger = logger;
    }

    /// <inheritdoc />
    public async Task<VenueLayout?> GetWithZonesAndSeatsAsync(Guid layoutId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetWithZonesAndSeats"))
        using (LogContext.PushProperty("EntityType", "VenueLayout"))
        using (LogContext.PushProperty("LayoutId", layoutId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetWithZonesAndSeatsAsync START: LayoutId={LayoutId}", layoutId);

            try
            {
                // Slice 5 Chunk 6: eager-load the full aggregate (zones + their seats,
                // tables + their seats, decorations) so write handlers can call
                // GetTable / GetDecoration on the in-memory aggregate. The method name
                // is retained for caller back-compat — effectively "full aggregate by id".
                var layout = await _dbSet
                    .Include(v => v.Zones)
                        .ThenInclude(z => z.Seats)
                    .Include(v => v.Tables)
                        .ThenInclude(t => t.Seats)
                    .Include(v => v.Decorations)
                    .FirstOrDefaultAsync(v => v.Id == layoutId, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetWithZonesAndSeatsAsync COMPLETE: LayoutId={LayoutId}, Found={Found}, Duration={ElapsedMs}ms",
                    layoutId,
                    layout != null,
                    stopwatch.ElapsedMilliseconds);

                return layout;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetWithZonesAndSeatsAsync FAILED: LayoutId={LayoutId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    layoutId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<VenueLayout?> GetByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByEventId"))
        using (LogContext.PushProperty("EntityType", "VenueLayout"))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByEventIdAsync START: EventId={EventId}", eventId);

            try
            {
                // Slice 5 Chunk 6: match GetWithZonesAndSeatsAsync — return the full
                // aggregate so the event's layout-scoped write surface sees tables too.
                var layout = await _dbSet
                    .Include(v => v.Zones)
                        .ThenInclude(z => z.Seats)
                    .Include(v => v.Tables)
                        .ThenInclude(t => t.Seats)
                    .Include(v => v.Decorations)
                    .FirstOrDefaultAsync(v => v.EventId == eventId, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByEventIdAsync COMPLETE: EventId={EventId}, Found={Found}, Duration={ElapsedMs}ms",
                    eventId,
                    layout != null,
                    stopwatch.ElapsedMilliseconds);

                return layout;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByEventIdAsync FAILED: EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    eventId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<VenueLayout>> GetTemplatesByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetTemplatesByUser"))
        using (LogContext.PushProperty("EntityType", "VenueLayout"))
        using (LogContext.PushProperty("UserId", userId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetTemplatesByUserAsync START: UserId={UserId}", userId);

            try
            {
                var templates = await _dbSet
                    .AsNoTracking()
                    .Include(v => v.Zones)
                    .Where(v => v.CreatedByUserId == userId && v.IsTemplate)
                    .OrderByDescending(v => v.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetTemplatesByUserAsync COMPLETE: UserId={UserId}, Count={Count}, Duration={ElapsedMs}ms",
                    userId,
                    templates.Count,
                    stopwatch.ElapsedMilliseconds);

                return templates;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetTemplatesByUserAsync FAILED: UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    userId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    /// <inheritdoc />
    public void SetOriginalRowVersion(VenueLayout layout, uint expectedRowVersion)
    {
        // EF Core compares OriginalValue against the DB xmin in the UPDATE WHERE clause.
        // If the caller-supplied expected RowVersion differs from the row's actual xmin,
        // SaveChangesAsync throws DbUpdateConcurrencyException — mapped to 409 by the handler.
        _context.Entry(layout).Property(v => v.RowVersion).OriginalValue = expectedRowVersion;
    }

    /// <inheritdoc />
    public async Task<bool> NameExistsForEventAsync(string name, Guid eventId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "NameExistsForEvent"))
        using (LogContext.PushProperty("EntityType", "VenueLayout"))
        {
            try
            {
                return await _dbSet.AnyAsync(
                    v => v.EventId == eventId && v.Name == name,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _repoLogger.LogError(ex,
                    "NameExistsForEventAsync FAILED: Name={Name}, EventId={EventId}, Error={ErrorMessage}",
                    name, eventId, ex.Message);
                throw;
            }
        }
    }
}
