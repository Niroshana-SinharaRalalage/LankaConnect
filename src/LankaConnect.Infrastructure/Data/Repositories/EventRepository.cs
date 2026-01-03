using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Services;

namespace LankaConnect.Infrastructure.Data.Repositories;

public class EventRepository : Repository<Event>, IEventRepository
{
    private readonly IGeoLocationService _geoLocationService;
    private readonly ILogger<EventRepository> _repoLogger;

    public EventRepository(
        AppDbContext context,
        IGeoLocationService geoLocationService,
        ILogger<EventRepository> logger) : base(context)
    {
        _geoLocationService = geoLocationService;
        _repoLogger = logger;
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

    // GetByIdAsync with eager loading for SignUpLists, Images, Videos, Registrations, and EmailGroups
    // This is required for GetEventSignUpLists query, media gallery display, correct DisplayOrder calculation,
    // registration management (cancel/update operations), and email group integration
    // Phase 6A.28: Removed duplicate .Include(SignUpLists).ThenInclude(Commitments) to fix EF Core change tracking bug
    // Phase 6A.33 FIX: After loading, sync shadow navigation entities to domain's email group ID list
    // Phase 6A.53 FIX: Add trackChanges parameter to support both command and query handlers
    public async Task<Event?> GetByIdAsync(Guid id, bool trackChanges, CancellationToken cancellationToken = default)
    {
        _repoLogger.LogInformation("[DIAG-R1] EventRepository.GetByIdAsync START - EventId: {EventId}, TrackChanges: {TrackChanges}", id, trackChanges);

        // Build query with eager loading
        IQueryable<Event> query = _dbSet
            .Include(e => e.Images)
            .Include(e => e.Videos)  // Phase 6A.12: Include videos for event media gallery
            .Include(e => e.Registrations)  // Session 21: Include registrations for cancel/update operations
            .Include("_emailGroupEntities")  // Phase 6A.33: Include email groups shadow navigation from junction table
            .Include(e => e.SignUpLists)
                .ThenInclude(s => s.Items)
                    .ThenInclude(i => i.Commitments);

        // Phase 6A.53 FIX: Apply tracking behavior based on parameter
        // Command handlers need tracked entities (trackChanges: true) for EF Core change detection
        // Query handlers need untracked entities (trackChanges: false) for better performance
        if (!trackChanges)
        {
            query = query.AsNoTracking();
            _repoLogger.LogInformation("[DIAG-R2] Loading entity WITHOUT change tracking (read-only)");
        }
        else
        {
            _repoLogger.LogInformation("[DIAG-R2] Loading entity WITH change tracking (for modifications)");
        }

        var eventEntity = await query.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (eventEntity == null)
        {
            _repoLogger.LogWarning("[DIAG-R3] Event not found: {EventId}", id);
            return null;
        }

        _repoLogger.LogInformation(
            "[DIAG-R4] Event loaded - Id: {EventId}, Status: {Status}, Tracked: {Tracked}",
            eventEntity.Id,
            eventEntity.Status,
            trackChanges);

        // Phase 6A.33 FIX: Sync email group IDs from shadow navigation to domain
        var emailGroupsCollection = _context.Entry(eventEntity).Collection("_emailGroupEntities");
        var emailGroupEntities = emailGroupsCollection.CurrentValue as IEnumerable<Domain.Communications.Entities.EmailGroup>;

        if (emailGroupEntities != null)
        {
            var emailGroupIds = emailGroupEntities.Select(eg => eg.Id).ToList();
            eventEntity.SyncEmailGroupIdsFromEntities(emailGroupIds);

            _repoLogger.LogInformation(
                "[DIAG-R5] Synced {EmailGroupCount} email group IDs to domain entity",
                emailGroupIds.Count);
        }

        _repoLogger.LogInformation(
            "[DIAG-R6] EventRepository.GetByIdAsync COMPLETE - EventId: {EventId}, TrackChanges: {TrackChanges}",
            eventEntity.Id,
            trackChanges);

        return eventEntity;
    }

    /// <summary>
    /// Phase 6A.53 FIX: Override base GetByIdAsync to forward to trackChanges version
    /// This ensures ALL calls use the 3-parameter overload with explicit change tracking control.
    /// Without this override, C# method resolution may call the base Repository.GetByIdAsync
    /// which uses FindAsync() that tracks entities by default, bypassing our trackChanges logic.
    /// </summary>
    public override async Task<Event?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Forward to the 3-parameter version with tracking ENABLED by default
        // This makes tracked entities the default behavior for command handlers
        return await GetByIdAsync(id, trackChanges: true, cancellationToken);
    }

    /// <summary>
    /// Phase 6A.67 FIX: Override GetAllAsync to include Images for dashboard event cards
    /// Base repository only loads the Event entity without related data
    /// Dashboard needs Images to display event thumbnails
    /// </summary>
    public override async Task<IReadOnlyList<Event>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(e => e.Images)
            .Include(e => e.Registrations)  // For CurrentRegistrations count
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Event>> GetByOrganizerAsync(Guid organizerId, CancellationToken cancellationToken = default)
    {
        // Session 33: Include Registrations to populate CurrentRegistrations for dashboard
        // Phase 6A.67 FIX: Include Images for dashboard event thumbnails
        return await _dbSet
            .AsNoTracking()
            .Include(e => e.Images)
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
        _repoLogger.LogInformation("[SEARCH-1] SearchAsync START - Term: {SearchTerm}, Limit: {Limit}, Offset: {Offset}, Category: {Category}, IsFreeOnly: {IsFreeOnly}, StartDateFrom: {StartDateFrom}",
            searchTerm, limit, offset, category, isFreeOnly, startDateFrom);

        // Build the WHERE clause dynamically based on filters
        // Phase 6A.58 FIX: Use QUOTED PascalCase for enum/date columns (confirmed from PostgreSQL hints)
        // search_vector is snake_case (has explicit HasColumnName), Status/Category/StartDate are PascalCase (EF defaults)
        // Phase 6A.59 FIX: Include Cancelled events in search results so users can see them
        // Phase 6A.59 FIX 7: Use string enum values (Status/Category stored as VARCHAR via HasConversion<string>())
        var whereConditions = new List<string>
        {
            "e.search_vector @@ websearch_to_tsquery('english', {0})",
            @"e.""Status"" IN ({1}, {2})" // Allow both Published and Cancelled
        };

        var parameters = new List<object>
        {
            searchTerm,
            EventStatus.Published.ToString(),  // "Published" - string enum value
            EventStatus.Cancelled.ToString()   // "Cancelled" - string enum value (user wants to see these)
        };

        _repoLogger.LogInformation("[SEARCH-2] Initial WHERE conditions: {Conditions}, Parameters: {Parameters}",
            string.Join(" AND ", whereConditions), string.Join(", ", parameters));

        // Add category filter if provided
        if (category.HasValue)
        {
            whereConditions.Add($@"e.""Category"" = {{{parameters.Count}}}");
            parameters.Add(category.Value.ToString()); // Use string enum value (stored as VARCHAR)
            _repoLogger.LogInformation("[SEARCH-3] Added category filter: {Category}", category.Value);
        }

        // Add free-only filter if provided
        if (isFreeOnly.HasValue && isFreeOnly.Value)
        {
            // ticket_price is JSONB column, access Amount property
            whereConditions.Add("(e.ticket_price->>'Amount')::numeric = 0");
            _repoLogger.LogInformation("[SEARCH-4] Added free-only filter");
        }

        // Add start date filter if provided
        if (startDateFrom.HasValue)
        {
            whereConditions.Add($@"e.""StartDate"" >= {{{parameters.Count}}}"); // PascalCase with quotes
            parameters.Add(startDateFrom.Value);
            _repoLogger.LogInformation("[SEARCH-5] Added start date filter: {StartDateFrom}", startDateFrom.Value);
        }

        var whereClause = string.Join(" AND ", whereConditions);

        // Phase 6A.59 FIX 3: Save count of WHERE clause parameters BEFORE adding duplicates
        // Count query needs all parameters used in WHERE clause
        var whereClauseParameterCount = parameters.Count;

        // Phase 6A.59 FIX 3: Duplicate searchTerm parameter for ORDER BY clause
        // EF Core FromSqlRaw doesn't support using same parameter index twice
        var searchTermIndexForOrderBy = parameters.Count;
        parameters.Add(searchTerm); // Duplicate searchTerm for ORDER BY

        // Query for events with ranking
        // Phase 6A.59 FIX 6: Build parameter placeholders for LIMIT and OFFSET
        // EF Core FromSqlRaw expects {0}, {1}, {2} format for parameters
        var limitIndex = parameters.Count;
        var offsetIndex = parameters.Count + 1;

        var eventsSql = $@"
            SELECT e.*
            FROM events.events e
            WHERE {whereClause}
            ORDER BY ts_rank(e.search_vector, websearch_to_tsquery('english', {{{searchTermIndexForOrderBy}}})) DESC, e.""StartDate"" ASC
            LIMIT {{{limitIndex}}} OFFSET {{{offsetIndex}}}";

        parameters.Add(limit);
        parameters.Add(offset);

        _repoLogger.LogInformation("[SEARCH-6] Events SQL Query:\n{EventsSql}\nParameters: {Parameters}",
            eventsSql, string.Join(", ", parameters));

        try
        {
            var events = await _dbSet
                .FromSqlRaw(eventsSql, parameters.ToArray())
                .AsNoTracking()
                .Include(e => e.Images)
                .Include(e => e.Videos)
                .ToListAsync(cancellationToken);

            _repoLogger.LogInformation("[SEARCH-7] Events query succeeded - Found {EventCount} events", events.Count);

            // Count query (same filters, no ranking needed)
            // Phase 6A.58 FIX: Use FormattableString for SqlQuery (EF Core 8+ requirement)
            var countSql = $@"
                SELECT COUNT(*)::int AS ""Value""
                FROM events.events e
                WHERE {whereClause}";

            // Phase 6A.59 FIX 4: Remove searchTerm duplicate, limit, and offset from count parameters
            // Count query needs ALL parameters used in WHERE clause (may include category, startDateFrom, etc.)
            // Exclude: duplicate searchTerm, limit, offset (last 3 parameters)
            var countParameters = parameters.Take(whereClauseParameterCount).ToArray();

            _repoLogger.LogInformation("[SEARCH-8] Count SQL Query:\n{CountSql}\nParameters: {Parameters}",
                countSql, string.Join(", ", countParameters));

            var totalCount = await _context.Database
                .SqlQueryRaw<int>(countSql, countParameters)
                .FirstOrDefaultAsync(cancellationToken);

            _repoLogger.LogInformation("[SEARCH-9] Count query succeeded - Total: {TotalCount}", totalCount);
            _repoLogger.LogInformation("[SEARCH-10] SearchAsync COMPLETE - Returning {EventCount} events, Total: {TotalCount}",
                events.Count, totalCount);

            return (events, totalCount);
        }
        catch (Exception ex)
        {
            _repoLogger.LogError(ex, "[SEARCH-ERROR] SearchAsync FAILED - Term: {SearchTerm}, Error: {ErrorMessage}, StackTrace: {StackTrace}",
                searchTerm, ex.Message, ex.StackTrace);
            throw;
        }
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