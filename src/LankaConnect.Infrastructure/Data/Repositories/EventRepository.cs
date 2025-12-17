using Microsoft.EntityFrameworkCore;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Services;

namespace LankaConnect.Infrastructure.Data.Repositories;

public class EventRepository : Repository<Event>, IEventRepository
{
    private readonly IGeoLocationService _geoLocationService;

    public EventRepository(AppDbContext context, IGeoLocationService geoLocationService) : base(context)
    {
        _geoLocationService = geoLocationService;
    }

    /// <summary>
    /// Phase 6A.33 FIX: Override AddAsync to sync shadow navigation for email groups when adding new event
    /// When creating a new event with email groups, the domain's _emailGroupIds list contains the email group GUIDs,
    /// but the shadow navigation _emailGroupEntities needs to be populated with actual EmailGroup entities
    /// for EF Core to create the many-to-many junction table rows.
    /// Pattern mirrors UserRepository.AddAsync for metro areas - no entity state changes, just set CurrentValue.
    /// </summary>
    public override async Task AddAsync(Event entity, CancellationToken cancellationToken = default)
    {
        // Call base implementation to add entity to DbSet (state = Added)
        await base.AddAsync(entity, cancellationToken);

        // Sync email groups from domain list to shadow navigation for persistence
        // This bridges the gap between domain's List<Guid> and EF Core's ICollection<EmailGroup>
        if (entity.EmailGroupIds.Any())
        {
            // Load the EmailGroup entities from the database based on the domain's ID list
            var emailGroupEntities = await _context.Set<Domain.Communications.Entities.EmailGroup>()
                .Where(eg => entity.EmailGroupIds.Contains(eg.Id))
                .ToListAsync(cancellationToken);

            // Access shadow navigation using EF Core's Entry API
            var emailGroupsCollection = _context.Entry(entity).Collection("_emailGroupEntities");

            // Set the loaded entities into the shadow navigation
            // EF Core will detect this and create rows in event_email_groups junction table
            // Entity remains in Added state - NO state changes needed
            emailGroupsCollection.CurrentValue = emailGroupEntities;
        }
    }

    // Override GetByIdAsync to eagerly load SignUpLists, Images, Videos, Registrations, and EmailGroups with all related data
    // This is required for GetEventSignUpLists query, media gallery display, correct DisplayOrder calculation,
    // registration management (cancel/update operations), and email group integration
    // Phase 6A.28: Removed duplicate .Include(SignUpLists).ThenInclude(Commitments) to fix EF Core change tracking bug
    // Phase 6A.33 FIX: After loading, sync shadow navigation entities to domain's email group ID list
    public override async Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var eventEntity = await _dbSet
            .Include(e => e.Images)
            .Include(e => e.Videos)  // Phase 6A.12: Include videos for event media gallery
            .Include(e => e.Registrations)  // Session 21: Include registrations for cancel/update operations
            .Include("_emailGroupEntities")  // Phase 6A.33: Include email groups shadow navigation from junction table
            .Include(e => e.SignUpLists)
                .ThenInclude(s => s.Items)
                    .ThenInclude(i => i.Commitments)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        // Phase 6A.33 FIX: Sync loaded shadow navigation entities to domain's email group ID list
        // This bridges the gap between EF Core's entity references and domain's business logic IDs
        // Pattern mirrors UserRepository.GetByIdAsync per ADR-009
        if (eventEntity != null)
        {
            // Access the shadow navigation using EF Core's Entry API
            var emailGroupsCollection = _context.Entry(eventEntity).Collection("_emailGroupEntities");
            var emailGroupEntities = emailGroupsCollection.CurrentValue as IEnumerable<Domain.Communications.Entities.EmailGroup>;

            if (emailGroupEntities != null)
            {
                // Extract IDs and sync to domain's _emailGroupIds list
                var emailGroupIds = emailGroupEntities.Select(eg => eg.Id).ToList();

                // CRITICAL FIX Phase 6A.33: Store original state before sync
                var originalState = _context.Entry(eventEntity).State;

                // Sync the email group IDs from shadow navigation to domain list
                eventEntity.SyncEmailGroupIdsFromEntities(emailGroupIds);

                // CRITICAL FIX Phase 6A.33: Reset entity state to prevent UPDATE conflicts
                // The sync modifies _emailGroupIds which EF Core detects as a change
                // But this is infrastructure hydration, not a business modification
                // Reset state to what it was before sync (Unchanged for tracked entities)
                _context.Entry(eventEntity).State = originalState;
            }
        }

        return eventEntity;
    }

    public async Task<IReadOnlyList<Event>> GetByOrganizerAsync(Guid organizerId, CancellationToken cancellationToken = default)
    {
        // Session 33: Include Registrations to populate CurrentRegistrations for dashboard
        return await _dbSet
            .AsNoTracking()
            .Include(e => e.Registrations)
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
            .Include(e => e.Images)
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
            .Include(e => e.Images)
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
            .Include(e => e.Images)
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
        // Fetch published future events with valid coordinates
        // Client-side distance calculation using Haversine formula
        var events = await _dbSet
            .Include(e => e.Images)
            .AsNoTracking()
            .Where(e => e.Location != null && e.Location.Coordinates != null)
            .Where(e => e.Status == EventStatus.Published && e.StartDate > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        // Calculate distances and sort client-side
        return events
            .Select(e => new
            {
                Event = e,
                Distance = _geoLocationService.CalculateDistanceKm(
                    latitude,
                    longitude,
                    e.Location!.Coordinates!.Latitude,
                    e.Location.Coordinates.Longitude
                )
            })
            .OrderBy(x => x.Distance)
            .Take(maxResults)
            .Select(x => x.Event)
            .ToList();
    }

    public async Task<IReadOnlyList<Event>> GetEventsStartingInTimeWindowAsync(
        DateTime startTime,
        DateTime endTime,
        EventStatus[] statuses,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(e => e.Registrations) // Include registrations for attendee notifications
            .Where(e => e.StartDate >= startTime && e.StartDate <= endTime)
            .Where(e => statuses.Contains(e.Status))
            .OrderBy(e => e.StartDate)
            .ToListAsync(cancellationToken);
    }

    // Full-text search implementation (Epic 2 Phase 3 - PostgreSQL FTS)
    public async Task<(IReadOnlyList<Event> Events, int TotalCount)> SearchAsync(
        string searchTerm,
        int limit,
        int offset,
        EventCategory? category = null,
        bool? isFreeOnly = null,
        DateTime? startDateFrom = null,
        CancellationToken cancellationToken = default)
    {
        // Build the WHERE clause dynamically based on filters
        var whereConditions = new List<string>
        {
            "e.search_vector @@ websearch_to_tsquery('english', {0})",
            "e.status = {1}" // Only search Published events
        };

        var parameters = new List<object>
        {
            searchTerm,
            (int)EventStatus.Published
        };

        // Add category filter if provided
        if (category.HasValue)
        {
            whereConditions.Add($"e.category = {{{parameters.Count}}}");
            parameters.Add((int)category.Value);
        }

        // Add free-only filter if provided
        if (isFreeOnly.HasValue && isFreeOnly.Value)
        {
            whereConditions.Add("e.ticket_price_amount = 0");
        }

        // Add start date filter if provided
        if (startDateFrom.HasValue)
        {
            whereConditions.Add($"e.start_date >= {{{parameters.Count}}}");
            parameters.Add(startDateFrom.Value);
        }

        var whereClause = string.Join(" AND ", whereConditions);

        // Query for events with ranking
        var eventsSql = $@"
            SELECT e.*
            FROM events e
            WHERE {whereClause}
            ORDER BY ts_rank(e.search_vector, websearch_to_tsquery('english', {{0}})) DESC, e.start_date ASC
            LIMIT {{{parameters.Count}}} OFFSET {{{parameters.Count + 1}}}";

        parameters.Add(limit);
        parameters.Add(offset);

        var events = await _dbSet
            .FromSqlRaw(eventsSql, parameters.ToArray())
            .AsNoTracking()
            .Include(e => e.Images)
            .Include(e => e.Videos)
            .ToListAsync(cancellationToken);

        // Count query (same filters, no ranking needed)
        var countSql = $@"
            SELECT COUNT(*)
            FROM events e
            WHERE {whereClause}";

        // Remove limit and offset parameters for count query
        var countParameters = parameters.Take(parameters.Count - 2).ToArray();

        var totalCount = await _context.Database
            .SqlQueryRaw<int>(countSql, countParameters)
            .FirstOrDefaultAsync(cancellationToken);

        return (events, totalCount);
    }

    /// <summary>
    /// Phase 6A.27: Gets all events that have a specific badge assigned
    /// Used by ExpiredBadgeCleanupJob to remove expired badges from events
    /// </summary>
    public async Task<IReadOnlyList<Event>> GetEventsWithBadgeAsync(Guid badgeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(e => e.Badges)
            .Where(e => e.Badges.Any(b => b.BadgeId == badgeId))
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Phase 6A.28: Gets all events that have at least one expired badge assignment
    /// Used by ExpiredBadgeCleanupJob to clean up expired EventBadge assignments
    /// </summary>
    public async Task<IReadOnlyList<Event>> GetEventsWithExpiredBadgesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .Include(e => e.Badges)
                .ThenInclude(eb => eb.Badge)
            .Where(e => e.Badges.Any(eb => eb.ExpiresAt.HasValue && eb.ExpiresAt < now))
            .ToListAsync(cancellationToken);
    }
}