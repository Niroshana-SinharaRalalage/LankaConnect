using LankaConnect.Infrastructure.Data;
using LankaConnect.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using System.Diagnostics;
using Serilog.Context;

using LankaConnect.BuildingBlocks.Abstractions;
namespace LankaConnect.Products.LankaEvents.Infrastructure.Repositories;

/// <summary>
/// Phase 2B: Repository implementation for VenueLayout aggregate operations.
/// Loads full aggregate (layout → zones → seats) for domain operations.
/// </summary>
[Wave6_5TransitionalException("Inherits Repository<T> + uses AppDbContext via LankaConnect.Infrastructure.Data; cleared in Wave 6.5 LankaEventsDbContext extraction")]
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
    public async Task<VenueLayout?> GetAssignedLayoutForEventAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetAssignedLayoutForEvent"))
        using (LogContext.PushProperty("EntityType", "VenueLayout"))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetAssignedLayoutForEventAsync START: EventId={EventId}", eventId);

            try
            {
                // Slice 9.3: read the canonical assignment via events.venue_layout_id, NOT
                // venue_layouts.event_id. Filtering by event_id returned orphan layouts
                // (created via from-preset but where the subsequent assign step failed —
                // see Slice 8 RC-2). The canonical assignment is the events table column.
                //
                // Step 1: resolve the event's assigned layoutId. Untracked because we don't
                // need to mutate the event here.
                var assignedLayoutId = await _context.Events
                    .AsNoTracking()
                    .Where(e => e.Id == eventId)
                    .Select(e => e.VenueLayoutId)
                    .SingleOrDefaultAsync(cancellationToken);

                if (assignedLayoutId is null)
                {
                    stopwatch.Stop();
                    _repoLogger.LogInformation(
                        "GetAssignedLayoutForEventAsync COMPLETE: EventId={EventId}, Found=False (no layout assigned), Duration={ElapsedMs}ms",
                        eventId,
                        stopwatch.ElapsedMilliseconds);
                    return null;
                }

                // Step 2: load the full aggregate by id (matches GetWithZonesAndSeatsAsync).
                var layout = await _dbSet
                    .Include(v => v.Zones)
                        .ThenInclude(z => z.Seats)
                    .Include(v => v.Tables)
                        .ThenInclude(t => t.Seats)
                    .Include(v => v.Decorations)
                    .FirstOrDefaultAsync(v => v.Id == assignedLayoutId.Value, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetAssignedLayoutForEventAsync COMPLETE: EventId={EventId}, AssignedLayoutId={LayoutId}, Found={Found}, Duration={ElapsedMs}ms",
                    eventId,
                    assignedLayoutId.Value,
                    layout != null,
                    stopwatch.ElapsedMilliseconds);

                return layout;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetAssignedLayoutForEventAsync FAILED: EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    eventId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);

                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<int> HardDeleteByEventIdAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "HardDeleteByEventId"))
        using (LogContext.PushProperty("EntityType", "VenueLayout"))
        using (LogContext.PushProperty("EventId", eventId))
        {
            var stopwatch = Stopwatch.StartNew();
            _repoLogger.LogDebug("HardDeleteByEventIdAsync START: EventId={EventId}", eventId);

            try
            {
                // Step 1: capture the zone + table ids of every layout for this event,
                // so we can delete their tier_assignments rows manually (polymorphic FK
                // has no DB cascade — same pattern as Slice 9.3's seat_holds cleanup).
                var doomedLayoutIds = await _dbSet
                    .Where(v => v.EventId == eventId)
                    .Select(v => v.Id)
                    .ToListAsync(cancellationToken);

                if (doomedLayoutIds.Count == 0)
                {
                    stopwatch.Stop();
                    _repoLogger.LogInformation(
                        "HardDeleteByEventIdAsync COMPLETE: EventId={EventId}, DeletedCount=0 (no rows), Duration={ElapsedMs}ms",
                        eventId, stopwatch.ElapsedMilliseconds);
                    return 0;
                }

                // Step 2: cascade-clean tier_assignments referencing zones/tables of the
                // doomed layouts. Use raw SQL because EF Core's polymorphic-junction model
                // doesn't allow a navigation-based delete here.
                var assignmentsCleaned = await _context.Database.ExecuteSqlInterpolatedAsync($@"
                    DELETE FROM events.tier_assignments ta
                    WHERE (ta.assignable_kind = 'Zone' AND ta.assignable_id IN (
                        SELECT z.""Id"" FROM events.venue_zones z
                        WHERE z.venue_layout_id = ANY({doomedLayoutIds})))
                       OR (ta.assignable_kind = 'Table' AND ta.assignable_id IN (
                        SELECT t.""Id"" FROM events.venue_tables t
                        WHERE t.venue_layout_id = ANY({doomedLayoutIds})))
                ", cancellationToken);

                // Step 3: delete the layouts. Cascade FKs handle zones / tables / seats /
                // decorations / canvas (verified S1.5 pre-flight — all OnDelete.Cascade).
                var layoutsDeleted = await _dbSet
                    .Where(v => doomedLayoutIds.Contains(v.Id))
                    .ExecuteDeleteAsync(cancellationToken);

                stopwatch.Stop();
                _repoLogger.LogInformation(
                    "HardDeleteByEventIdAsync COMPLETE: EventId={EventId}, LayoutsDeleted={LayoutsDeleted}, TierAssignmentsCleaned={AssignmentsCleaned}, Duration={ElapsedMs}ms",
                    eventId, layoutsDeleted, assignmentsCleaned, stopwatch.ElapsedMilliseconds);

                return layoutsDeleted;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _repoLogger.LogError(ex,
                    "HardDeleteByEventIdAsync FAILED: EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    eventId, stopwatch.ElapsedMilliseconds, ex.Message);
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
                // Slice 8 S8.10: include Seats + Tables + Decorations so the Mine
                // tab card shows an accurate `totalCapacity` (computed from the
                // EnabledSeatCount of every zone + table). Pre-S8.10 the include
                // graph stopped at Zones, so totalCapacity always rendered as 0
                // — cosmetic but misleading. AsSplitQuery prevents the cartesian
                // explosion EventRepository hit at high cardinality (the Phase
                // 6A perf RCA's pattern).
                var templates = await _dbSet
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Include(v => v.Zones)
                        .ThenInclude(z => z.Seats)
                    .Include(v => v.Tables)
                        .ThenInclude(t => t.Seats)
                    .Include(v => v.Decorations)
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
