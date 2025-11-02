using Microsoft.EntityFrameworkCore;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Infrastructure.Data.Repositories;

public class EventRepository : Repository<Event>, IEventRepository
{
    public EventRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Event>> GetByOrganizerAsync(Guid organizerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(e => e.OrganizerId == organizerId)
            .OrderByDescending(e => e.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Event>> GetUpcomingEventsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(e => e.Status == EventStatus.Published && 
                       e.StartDate > DateTime.UtcNow)
            .OrderBy(e => e.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Event>> GetEventsByStatusAsync(EventStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(e => e.Status == status)
            .OrderByDescending(e => e.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Event>> GetEventsWithAvailableCapacityAsync(CancellationToken cancellationToken = default)
    {
        var events = await _dbSet
            .AsNoTracking()
            .Include(e => e.Registrations)
            .Where(e => e.Status == EventStatus.Published && 
                       e.StartDate > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        // Apply business logic filtering on client-side
        return events
            .Where(e => e.HasCapacityFor(1))
            .OrderBy(e => e.StartDate)
            .ToList();
    }

    public async Task<Event?> GetWithRegistrationsAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(e => e.Registrations)
            .FirstOrDefaultAsync(e => e.Id == eventId, cancellationToken);
    }

    public async Task<IReadOnlyList<Event>> GetPublishedEventsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(e => e.Status == EventStatus.Published)
            .OrderByDescending(e => e.StartDate)
            .ToListAsync(cancellationToken);
    }

    // Location-based queries (Epic 2 Phase 1 - PostGIS spatial queries)
    public async Task<IReadOnlyList<Event>> GetEventsByRadiusAsync(decimal latitude, decimal longitude, double radiusMiles, CancellationToken cancellationToken = default)
    {
        // Convert miles to meters for PostGIS (1 mile = 1609.344 meters)
        var radiusMeters = radiusMiles * 1609.344;

        // Create search point using NetTopologySuite
        var geometryFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        var searchPoint = geometryFactory.CreatePoint(new NetTopologySuite.Geometries.Coordinate((double)longitude, (double)latitude));

        return await _dbSet
            .AsNoTracking()
            .Where(e => e.Location != null && e.Location.Coordinates != null)
            .Where(e => e.Status == EventStatus.Published && e.StartDate > DateTime.UtcNow)
            .Where(e => searchPoint.IsWithinDistance(
                geometryFactory.CreatePoint(new NetTopologySuite.Geometries.Coordinate(
                    (double)e.Location!.Coordinates!.Longitude,
                    (double)e.Location.Coordinates.Latitude
                )),
                radiusMeters
            ))
            .OrderBy(e => e.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Event>> GetEventsByCityAsync(string city, string? state = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(city))
            return Array.Empty<Event>();

        var query = _dbSet
            .AsNoTracking()
            .Where(e => e.Location != null)
            .Where(e => EF.Functions.Like(e.Location!.Address.City.ToLower(), $"%{city.Trim().ToLower()}%"))
            .Where(e => e.Status == EventStatus.Published && e.StartDate > DateTime.UtcNow);

        if (!string.IsNullOrWhiteSpace(state))
        {
            query = query.Where(e => EF.Functions.Like(e.Location!.Address.State.ToLower(), $"%{state.Trim().ToLower()}%"));
        }

        return await query
            .OrderBy(e => e.StartDate)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Event>> GetNearestEventsAsync(decimal latitude, decimal longitude, int maxResults = 10, CancellationToken cancellationToken = default)
    {
        // Create search point using NetTopologySuite
        var geometryFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        var searchPoint = geometryFactory.CreatePoint(new NetTopologySuite.Geometries.Coordinate((double)longitude, (double)latitude));

        return await _dbSet
            .AsNoTracking()
            .Where(e => e.Location != null && e.Location.Coordinates != null)
            .Where(e => e.Status == EventStatus.Published && e.StartDate > DateTime.UtcNow)
            .OrderBy(e => searchPoint.Distance(
                geometryFactory.CreatePoint(new NetTopologySuite.Geometries.Coordinate(
                    (double)e.Location!.Coordinates!.Longitude,
                    (double)e.Location.Coordinates.Latitude
                ))
            ))
            .Take(maxResults)
            .ToListAsync(cancellationToken);
    }
}