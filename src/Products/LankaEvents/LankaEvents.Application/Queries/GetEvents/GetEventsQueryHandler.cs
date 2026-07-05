using LankaConnect.Modules.Identity.Contracts; // W4.6.a: ICurrentUserService moved here
using System.Diagnostics;
using AutoMapper;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Modules.Identity.Domain.Entities;
using LankaConnect.Modules.Identity.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Queries.GetEvents;

/// <summary>
/// Handler for GetEventsQuery - Events listing page
/// Supports location-based sorting and traditional filtering
/// Returns ALL matching events (not limited to 4 like featured events)
/// </summary>
public class GetEventsQueryHandler : IQueryHandler<GetEventsQuery, IReadOnlyList<EventDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IIdentityQueries _identityQueries;
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService; // Phase 6A.133: Multi-organizer
    private readonly IMapper _mapper;
    private readonly ILogger<GetEventsQueryHandler> _logger;

    public GetEventsQueryHandler(
        IEventRepository eventRepository,
        IIdentityQueries identityQueries,
        IRegistrationRepository registrationRepository,
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService, // Phase 6A.133: Multi-organizer
        IMapper mapper,
        ILogger<GetEventsQueryHandler> logger)
    {
        _eventRepository = eventRepository;
        _identityQueries = identityQueries;
        _registrationRepository = registrationRepository;
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<EventDto>>> Handle(GetEventsQuery request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetEvents"))
        using (LogContext.PushProperty("EntityType", "Event"))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetEvents START: Category={Category}, City={City}, State={State}, Status={Status}, StatusFilter={StatusFilter}, SearchTerm={SearchTerm}, IsFreeOnly={IsFreeOnly}, UserId={UserId}, HasLocation={HasLocation}",
                request.Category, request.City, request.State, request.Status, request.StatusFilter, request.SearchTerm,
                request.IsFreeOnly, request.UserId, request.Latitude.HasValue && request.Longitude.HasValue);

            try
            {
                var now = DateTime.UtcNow;

                // Phase 6A.47: If SearchTerm provided, use full-text search first
                IReadOnlyList<Event> events;
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    _logger.LogInformation(
                        "GetEvents: Using full-text search - SearchTerm={SearchTerm}",
                        request.SearchTerm);

                    // Step 1a: Apply full-text search with filters
                    // Phase 6A.X Issue #36: Do not exclude cancelled events in GetEvents (events list page)
                    // Issue #33 FIX: Pass IncludeAllStatuses to SearchAsync for Dashboard Event Management
                    (events, _) = await _eventRepository.SearchAsync(
                        request.SearchTerm,
                        limit: 1000, // Large limit for search
                        offset: 0,
                        request.Category,
                        request.IsFreeOnly,
                        request.StartDateFrom,
                        excludeCancelled: false, // GetEvents shows all events including cancelled
                        includeAllStatuses: request.IncludeAllStatuses, // Issue #33: Pass flag for Dashboard search
                        cancellationToken);

                    _logger.LogInformation(
                        "GetEvents: Full-text search completed - SearchTerm={SearchTerm}, ResultCount={ResultCount}",
                        request.SearchTerm, events.Count);
                }
                else
                {
                    // Step 1b: Get base event list with traditional filtering
                    events = await GetFilteredEventsAsync(request, cancellationToken);

                    _logger.LogInformation(
                        "GetEvents: Traditional filtering completed - ResultCount={ResultCount}",
                        events.Count);
                }

                // Step 2: Apply location-based sorting if location parameters provided
                if (ShouldApplyLocationSorting(request))
                {
                    _logger.LogInformation(
                        "GetEvents: Applying location-based sorting - UserId={UserId}, HasCoordinates={HasCoordinates}, MetroAreaCount={MetroAreaCount}",
                        request.UserId, request.Latitude.HasValue && request.Longitude.HasValue,
                        request.MetroAreaIds?.Count ?? 0);

                    events = await ApplyLocationBasedSortingAsync(events, request, now, cancellationToken);

                    _logger.LogInformation(
                        "GetEvents: Location-based sorting completed - ResultCount={ResultCount}",
                        events.Count);
                }

                // Step 3: Apply additional in-memory filters
                var filteredEvents = ApplyInMemoryFilters(events, request);
                var filteredList = filteredEvents.ToList();

                _logger.LogInformation(
                    "GetEvents: In-memory filters applied - BeforeFilter={BeforeFilter}, AfterFilter={AfterFilter}",
                    events.Count, filteredList.Count);

                // Step 4: Sort and map to DTOs
                // Phase 6A.133: Compute IsCurrentUserOrganizer for each event
                // Phase 8YA.4 (Q1=A): TBD events appear in the public listing but
                // sort to the bottom — dated events are still scannable in date
                // order at the top. The tiebreaker `e.StartDate.HasValue ? 0 : 1`
                // groups dated events first (rank 0), TBD events second (rank 1);
                // ThenBy(e.StartDate) preserves chronological order within each
                // group (null compares equal so TBD events end up in input order
                // among themselves, which is fine — operators typically have one
                // or two TBD events at a time).
                var currentUserId = _currentUserService.IsAuthenticated ? _currentUserService.UserId : Guid.Empty;
                var result = filteredList
                    .OrderBy(e => e.StartDate.HasValue ? 0 : 1)
                    .ThenBy(e => e.StartDate)
                    .Select(e =>
                    {
                        var dto = _mapper.Map<EventDto>(e);
                        return dto with
                        {
                            IsCurrentUserOrganizer = currentUserId != Guid.Empty
                                ? e.IsOrganizer(currentUserId)
                                : null
                        };
                    })
                    .ToList();

                // Step 5: Populate UserRegistrationStatus when userId provided (Phase 6A.X - Fix registration badge)
                if (request.UserId.HasValue && request.UserId.Value != Guid.Empty)
                {
                    _logger.LogInformation(
                        "GetEvents: Populating UserRegistrationStatus for UserId={UserId}, EventCount={EventCount}",
                        request.UserId.Value, result.Count);

                    try
                    {
                        // Fetch user's registrations for these events
                        var userRegistrations = await _registrationRepository.GetByUserAsync(
                            request.UserId.Value,
                            cancellationToken);

                        // Phase 6A.137: Fix duplicate key crash — GroupBy picks highest-priority status per event
                        // Phase 6A.137F-Fix5: Filter out transient/terminal states from event listing badges.
                        // Only show badges for active registrations (Confirmed, CheckedIn, Attended, Waitlisted).
                        // Filtered states:
                        // - Preliminary = checkout in progress or expired
                        // - Abandoned = expired checkout formally marked
                        // - RefundRequested = refund in progress; webhook may never complete (metadata bug)
                        // - Cancelled, Refunded = terminal states, user no longer registered
                        var registrationStatusMap = userRegistrations
                            .Where(r => r.Status == RegistrationStatus.Confirmed
                                     || r.Status == RegistrationStatus.CheckedIn
                                     || r.Status == RegistrationStatus.Attended
                                     || r.Status == RegistrationStatus.Waitlisted)
                            .GroupBy(r => r.EventId)
                            .ToDictionary(
                                g => g.Key,
                                g => g.OrderBy(r => GetStatusPriority(r.Status)).First().Status);

                        _logger.LogInformation(
                            "GetEvents: User has {RegistrationCount} registrations",
                            userRegistrations.Count);

                        // Populate UserRegistrationStatus for each event.
                        // IMPORTANT: Cannot use GetValueOrDefault() because RegistrationStatus is a non-nullable
                        // enum where default(RegistrationStatus) = Preliminary = 0. GetValueOrDefault would
                        // return Preliminary for events with no registration, causing false "Payment Processing..." badges.
                        result = result
                            .Select(e => e with
                            {
                                UserRegistrationStatus = registrationStatusMap.TryGetValue(e.Id, out var status) ? status : null
                            })
                            .ToList();

                        _logger.LogInformation(
                            "GetEvents: UserRegistrationStatus populated for {EventsWithStatus} events",
                            result.Count(e => e.UserRegistrationStatus.HasValue));
                    }
                    catch (Exception ex)
                    {
                        // Log error but don't fail the request - registration status is supplementary data
                        _logger.LogWarning(ex,
                            "GetEvents: Failed to populate UserRegistrationStatus for UserId={UserId}. Continuing without status. Error={ErrorMessage}",
                            request.UserId.Value, ex.Message);
                    }
                }

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetEvents COMPLETE: TotalResults={TotalResults}, Duration={ElapsedMs}ms",
                    result.Count, stopwatch.ElapsedMilliseconds);

                return Result<IReadOnlyList<EventDto>>.Success(result);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetEvents FAILED: Exception occurred - Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }

    /// <summary>
    /// Issue #36: Resolves EventStatusFilter to an array of EventStatus values.
    /// Maps user-friendly filter groups to actual backend statuses.
    ///
    /// Phase 6A.152: <see cref="EventStatusFilter.Active"/> and
    /// <see cref="EventStatusFilter.Inactive"/> no longer resolve to a status
    /// array — they bucket by <c>StartDate</c> relative to <c>now</c> instead
    /// (handled by <see cref="ApplyDateBasedBucketFilter"/>). Returning
    /// <c>null</c> for those two values means "no status-array filter at this
    /// stage — date-based bucket logic will run in
    /// <see cref="ApplyInMemoryFilters"/> (and a pre-filter in
    /// <see cref="GetFilteredEventsAsync"/>) ".
    /// </summary>
    private static EventStatus[]? ResolveStatusFilter(EventStatusFilter? statusFilter, bool includeAllStatuses)
    {
        if (!statusFilter.HasValue)
            return null;

        return statusFilter.Value switch
        {
            // Phase 6A.152: Active + Inactive are now date-based — see ApplyDateBasedBucketFilter.
            EventStatusFilter.Active => null,
            EventStatusFilter.Inactive => null,
            EventStatusFilter.Cancelled => new[]
            {
                EventStatus.Cancelled
            },
            EventStatusFilter.Unpublished => includeAllStatuses
                ? new[] { EventStatus.Draft, EventStatus.UnderReview }
                : Array.Empty<EventStatus>(), // Security: Unpublished requires IncludeAllStatuses
            EventStatusFilter.All => null, // null means no status filter (handled separately)
            _ => null
        };
    }

    /// <summary>
    /// Phase 6A.152: Decides whether <paramref name="statusFilter"/> is a
    /// date-based bucket (Active/Inactive) that requires
    /// <see cref="ApplyDateBasedBucketFilter"/> rather than the legacy
    /// status-array filter.
    /// </summary>
    private static bool IsDateBasedBucketFilter(EventStatusFilter? statusFilter) =>
        statusFilter == EventStatusFilter.Active || statusFilter == EventStatusFilter.Inactive;

    /// <summary>
    /// Phase 6A.152: Splits events into the Upcoming (Active) and Completed
    /// (Inactive) buckets by <c>StartDate</c> relative to <c>now</c>, not by
    /// <see cref="EventStatus"/>.
    ///
    /// <para>Rules (locked-in with product owner 2026-05-24):</para>
    /// <list type="bullet">
    ///   <item><description>Always exclude <see cref="EventStatus.Cancelled"/>,
    ///         <see cref="EventStatus.Draft"/>, <see cref="EventStatus.UnderReview"/>
    ///         from both public buckets.</description></item>
    ///   <item><description><see cref="EventStatusFilter.Active"/> (Upcoming):
    ///         <c>StartDate IS NULL</c> (TBD) <c>OR StartDate &gt;= now</c>.</description></item>
    ///   <item><description><see cref="EventStatusFilter.Inactive"/> (Completed):
    ///         <c>StartDate.HasValue AND StartDate &lt; now</c>.</description></item>
    /// </list>
    ///
    /// <para>Replaces the status-array semantics from 6A.149. Motivation:
    /// events do not reliably auto-transition <c>Published → Completed</c>
    /// (Hangfire's <c>EventStatusUpdateJob</c> only handles the two-hop
    /// <c>Published → Active → Completed</c> path; events that miss the
    /// <c>Active</c> hop strand at <c>Published</c> forever). The previous
    /// status-based split therefore hid past events from the public listing
    /// entirely. Bucketing by date is correct by construction.</para>
    /// </summary>
    private static IEnumerable<Event> ApplyDateBasedBucketFilter(
        IEnumerable<Event> events,
        EventStatusFilter bucket,
        DateTime now)
    {
        // Both buckets share the public-visibility exclusions.
        var publiclyVisible = events.Where(e =>
            e.Status != EventStatus.Cancelled
            && e.Status != EventStatus.Draft
            && e.Status != EventStatus.UnderReview);

        return bucket switch
        {
            EventStatusFilter.Active => publiclyVisible.Where(e =>
                !e.StartDate.HasValue || e.StartDate.Value >= now),
            EventStatusFilter.Inactive => publiclyVisible.Where(e =>
                e.StartDate.HasValue && e.StartDate.Value < now),
            _ => events
        };
    }

    /// <summary>
    /// Gets filtered events based on status, city, or defaults to all visible events
    /// Phase 6A.59: Changed default from Published-only to all except Draft/UnderReview
    /// Phase 6A.88: Added IncludeAllStatuses flag to control Draft/UnderReview visibility
    /// Issue #36: Added StatusFilter support for user-friendly status group filtering
    /// This allows cancelled events to be visible to users
    /// </summary>
    private async Task<IReadOnlyList<Event>> GetFilteredEventsAsync(
        GetEventsQuery request,
        CancellationToken cancellationToken)
    {
        // Issue #36: StatusFilter takes precedence over Status when provided
        if (request.StatusFilter.HasValue)
        {
            // Phase 6A.152: Active/Inactive buckets are date-based, not status-based.
            // Delegate to ApplyDateBasedBucketFilter; the legacy status-array path
            // below only handles Cancelled / Unpublished / All.
            if (IsDateBasedBucketFilter(request.StatusFilter))
            {
                var allEventsForBucket = await _eventRepository.GetAllAsync(cancellationToken);
                var bucketed = ApplyDateBasedBucketFilter(
                    allEventsForBucket,
                    request.StatusFilter!.Value,
                    DateTime.UtcNow).ToList();

                _logger.LogDebug(
                    "GetFilteredEventsAsync: Date-based bucket filter applied - Bucket={Bucket}, BeforeFilter={BeforeFilter}, AfterFilter={AfterFilter}",
                    request.StatusFilter.Value, allEventsForBucket.Count, bucketed.Count);

                return bucketed;
            }

            var resolvedStatuses = ResolveStatusFilter(request.StatusFilter, request.IncludeAllStatuses);

            _logger.LogDebug(
                "GetFilteredEventsAsync: Using StatusFilter={StatusFilter}, ResolvedStatuses={ResolvedStatuses}, IncludeAllStatuses={IncludeAllStatuses}",
                request.StatusFilter.Value,
                resolvedStatuses != null ? string.Join(",", resolvedStatuses) : "null (All)",
                request.IncludeAllStatuses);

            // Get all events first
            var allEvents = await _eventRepository.GetAllAsync(cancellationToken);

            // Handle "All" filter
            if (resolvedStatuses == null)
            {
                // For "All" with IncludeAllStatuses=true: return everything
                if (request.IncludeAllStatuses)
                {
                    return allEvents.ToList();
                }

                // For "All" without IncludeAllStatuses: exclude Draft/UnderReview (public view)
                return allEvents
                    .Where(e => e.Status != EventStatus.Draft && e.Status != EventStatus.UnderReview)
                    .ToList();
            }

            // Handle Unpublished filter without authorization (returns empty)
            if (resolvedStatuses.Length == 0)
            {
                _logger.LogWarning(
                    "GetFilteredEventsAsync: Unpublished filter requested without IncludeAllStatuses=true, returning empty list");
                return Array.Empty<Event>();
            }

            // Filter by resolved statuses
            return allEvents
                .Where(e => resolvedStatuses.Contains(e.Status))
                .ToList();
        }

        // If status filter is provided, use repository method
        if (request.Status.HasValue)
        {
            _logger.LogDebug(
                "GetFilteredEventsAsync: Using status filter - Status={Status}",
                request.Status.Value);
            return await _eventRepository.GetEventsByStatusAsync(request.Status.Value, cancellationToken);
        }

        // If city filter is provided, use repository method
        if (!string.IsNullOrWhiteSpace(request.City))
        {
            _logger.LogDebug(
                "GetFilteredEventsAsync: Using city filter - City={City}",
                request.City);
            return await _eventRepository.GetEventsByCityAsync(request.City, cancellationToken: cancellationToken);
        }

        // Get all events from repository
        var allEventsDefault = await _eventRepository.GetAllAsync(cancellationToken);

        // Phase 6A.88: If IncludeAllStatuses is true, return ALL events (for organizer's Event Management)
        if (request.IncludeAllStatuses)
        {
            _logger.LogDebug(
                "GetFilteredEventsAsync: IncludeAllStatuses=true, returning all {EventCount} events including Draft/UnderReview",
                allEventsDefault.Count);
            return allEventsDefault.ToList();
        }

        // Phase 6A.59: Default behavior - exclude Draft and UnderReview for public listings
        // This includes Published, Active, Cancelled, Completed, Archived, Postponed
        // Cancelled events will show with CANCELLED badge in UI
        var filteredEvents = allEventsDefault
            .Where(e => e.Status != EventStatus.Draft && e.Status != EventStatus.UnderReview)
            .ToList();

        _logger.LogDebug(
            "GetFilteredEventsAsync: IncludeAllStatuses=false (default), filtered from {TotalCount} to {FilteredCount} events (excluded Draft/UnderReview)",
            allEventsDefault.Count, filteredEvents.Count);

        return filteredEvents;
    }

    /// <summary>
    /// Determines if location-based sorting should be applied
    /// </summary>
    private static bool ShouldApplyLocationSorting(GetEventsQuery request)
    {
        return request.UserId.HasValue
               || (request.Latitude.HasValue && request.Longitude.HasValue)
               || (request.MetroAreaIds != null && request.MetroAreaIds.Any());
    }

    /// <summary>
    /// Applies location-based sorting using the same logic as featured events
    /// Priority 1: Preferred metro areas
    /// Priority 2: User's home location
    /// Priority 3: Provided coordinates
    /// Priority 4: Date-sorted fallback
    /// </summary>
    private async Task<IReadOnlyList<Event>> ApplyLocationBasedSortingAsync(
        IReadOnlyList<Event> events,
        GetEventsQuery request,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var sortedEvents = new List<Event>();
        var remainingEvents = events.ToList();

        // PRIORITY 1: Explicit metro area filter (applies to all users)
        if (request.MetroAreaIds != null && request.MetroAreaIds.Any())
        {
            // Filter by specific metro areas - ONLY return events within metro area radius
            var metroFilteredEvents = await FilterEventsByMetroAreasAsync(
                remainingEvents,
                request.MetroAreaIds,
                now,
                cancellationToken);

            sortedEvents.AddRange(metroFilteredEvents);
            remainingEvents.Clear(); // Clear remaining since we're filtering, not just sorting
        }
        // PRIORITY 2: Get events from preferred metro areas (for authenticated users without explicit filter)
        else if (request.UserId.HasValue)
        {
            var user = await _identityQueries.GetPreferencesAsync(request.UserId.Value, cancellationToken);
            if (user != null && user.PreferredMetroAreaIds.Any())
            {
                var metroSortedEvents = await SortEventsByMetroAreasAsync(
                    remainingEvents,
                    user.PreferredMetroAreaIds.ToList(),
                    now,
                    cancellationToken);

                sortedEvents.AddRange(metroSortedEvents);
                remainingEvents = remainingEvents.Except(metroSortedEvents).ToList();
            }

            // PRIORITY 3: If user exists, sort remaining by user's home location
            if (user != null && user.LocationCity != null && user.LocationState != null && remainingEvents.Any())
            {
                var homeCoordinates = await GetMetroAreaCoordinatesByCityStateAsync(
                    user.LocationCity,
                    user.LocationState,
                    cancellationToken);

                if (homeCoordinates.HasValue)
                {
                    var homeSortedEvents = SortEventsByDistance(
                        remainingEvents,
                        homeCoordinates.Value.Latitude,
                        homeCoordinates.Value.Longitude);

                    sortedEvents.AddRange(homeSortedEvents);
                    remainingEvents.Clear();
                }
            }
        }
        // PRIORITY 4: Anonymous user with provided coordinates
        else if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            var locationSortedEvents = SortEventsByDistance(
                remainingEvents,
                request.Latitude.Value,
                request.Longitude.Value);

            sortedEvents.AddRange(locationSortedEvents);
            remainingEvents.Clear();
        }

        // PRIORITY 5: Add remaining events (will be sorted by date later)
        sortedEvents.AddRange(remainingEvents);

        return sortedEvents;
    }

    /// <summary>
    /// Sorts events by their distance to specific metro areas (for preferred metros - doesn't filter)
    /// </summary>
    private async Task<List<Event>> SortEventsByMetroAreasAsync(
        List<Event> events,
        List<Guid> metroAreaIds,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var sortedEvents = new List<Event>();

        foreach (var metroId in metroAreaIds)
        {
            var coordinates = await GetMetroAreaCoordinatesAsync(metroId, cancellationToken);
            if (coordinates.HasValue)
            {
                var nearbyEvents = SortEventsByDistance(
                    events.Except(sortedEvents).ToList(),
                    coordinates.Value.Latitude,
                    coordinates.Value.Longitude);

                sortedEvents.AddRange(nearbyEvents);
            }
        }

        return sortedEvents;
    }

    /// <summary>
    /// Filters events to only those within the radius of specified metro areas
    /// </summary>
    private async Task<List<Event>> FilterEventsByMetroAreasAsync(
        List<Event> events,
        List<Guid> metroAreaIds,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var matchingEvents = new List<Event>();

        foreach (var metroId in metroAreaIds)
        {
            var metroData = await GetMetroAreaDataAsync(metroId, cancellationToken);
            if (metroData.HasValue)
            {
                // Filter events within this metro area's radius
                var eventsInMetro = events
                    .Where(e => e.Location?.Coordinates != null)
                    .Where(e => {
                        var distance = CalculateDistance(
                            metroData.Value.Latitude,
                            metroData.Value.Longitude,
                            e.Location!.Coordinates!.Latitude,
                            e.Location.Coordinates.Longitude);
                        // Convert radius from miles to kilometers (1 mile = 1.60934 km)
                        var radiusKm = metroData.Value.RadiusMiles * 1.60934;
                        return distance <= radiusKm;
                    })
                    .ToList();

                // Add events not already in the result
                foreach (var evt in eventsInMetro)
                {
                    if (!matchingEvents.Contains(evt))
                    {
                        matchingEvents.Add(evt);
                    }
                }
            }
        }

        // Sort by distance to first selected metro area
        if (metroAreaIds.Any() && matchingEvents.Any())
        {
            var firstMetroData = await GetMetroAreaDataAsync(metroAreaIds[0], cancellationToken);
            if (firstMetroData.HasValue)
            {
                matchingEvents = SortEventsByDistance(
                    matchingEvents,
                    firstMetroData.Value.Latitude,
                    firstMetroData.Value.Longitude);
            }
        }

        return matchingEvents;
    }

    /// <summary>
    /// Sorts events by distance using Haversine formula.
    /// Issue #23 Fix: Events WITH coordinates are sorted by distance (nearest first).
    /// Events WITHOUT coordinates are appended at the end, sorted by StartDate.
    /// This ensures no events are lost during location-based sorting.
    /// </summary>
    private List<Event> SortEventsByDistance(
        List<Event> events,
        decimal latitude,
        decimal longitude)
    {
        // Issue #23 Fix: Partition events into those with and without coordinates
        var eventsWithCoords = new List<(Event Event, double Distance)>();
        var eventsWithoutCoords = new List<Event>();

        foreach (var e in events)
        {
            if (e.Location?.Coordinates != null)
            {
                var distance = CalculateDistance(
                    latitude,
                    longitude,
                    e.Location.Coordinates.Latitude,
                    e.Location.Coordinates.Longitude);
                eventsWithCoords.Add((e, distance));
            }
            else
            {
                eventsWithoutCoords.Add(e);
            }
        }

        // Sort events with coordinates by distance (ascending - nearest first)
        var result = eventsWithCoords
            .OrderBy(x => x.Distance)
            .Select(x => x.Event)
            .ToList();

        // Issue #23 Fix: Append events without coordinates, sorted by StartDate.
        // Phase 8YA.4: tiebreaker so TBD events fall to the very end of the
        // no-coords tail rather than mixing with dated no-coords events.
        result.AddRange(eventsWithoutCoords
            .OrderBy(e => e.StartDate.HasValue ? 0 : 1)
            .ThenBy(e => e.StartDate));

        _logger.LogDebug(
            "SortEventsByDistance: Sorted {WithCoords} events by distance, appended {WithoutCoords} events without coordinates",
            eventsWithCoords.Count, eventsWithoutCoords.Count);

        return result;
    }

    /// <summary>
    /// Calculates distance between two coordinates using Haversine formula
    /// </summary>
    private static double CalculateDistance(
        decimal lat1, decimal lon1, decimal lat2, decimal lon2)
    {
        const double R = 6371; // Earth's radius in kilometers
        var dLat = ToRadians((double)(lat2 - lat1));
        var dLon = ToRadians((double)(lon2 - lon1));

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians((double)lat1)) * Math.Cos(ToRadians((double)lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;

    /// <summary>
    /// Gets coordinates for a specific metro area by ID
    /// </summary>
    private async Task<(decimal Latitude, decimal Longitude)?> GetMetroAreaCoordinatesAsync(
        Guid metroAreaId,
        CancellationToken cancellationToken)
    {
        var dbContext = _dbContext as Microsoft.EntityFrameworkCore.DbContext
            ?? throw new InvalidOperationException("DbContext must be EF Core DbContext");

        var metroArea = await dbContext.Set<LankaConnect.Products.LankaEvents.Domain.MetroArea>()
            .FindAsync(new object[] { metroAreaId }, cancellationToken);

        if (metroArea != null)
        {
            return ((decimal)metroArea.CenterLatitude, (decimal)metroArea.CenterLongitude);
        }

        return null;
    }

    /// <summary>
    /// Gets full metro area data including radius for filtering
    /// </summary>
    private async Task<(decimal Latitude, decimal Longitude, int RadiusMiles)?> GetMetroAreaDataAsync(
        Guid metroAreaId,
        CancellationToken cancellationToken)
    {
        var dbContext = _dbContext as Microsoft.EntityFrameworkCore.DbContext
            ?? throw new InvalidOperationException("DbContext must be EF Core DbContext");

        var metroArea = await dbContext.Set<LankaConnect.Products.LankaEvents.Domain.MetroArea>()
            .FindAsync(new object[] { metroAreaId }, cancellationToken);

        if (metroArea != null)
        {
            return ((decimal)metroArea.CenterLatitude, (decimal)metroArea.CenterLongitude, metroArea.RadiusMiles);
        }

        return null;
    }

    /// <summary>
    /// Looks up metro area coordinates by city and state
    /// </summary>
    private async Task<(decimal Latitude, decimal Longitude)?> GetMetroAreaCoordinatesByCityStateAsync(
        string city,
        string state,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(city) || string.IsNullOrWhiteSpace(state))
            return null;

        var dbContext = _dbContext as Microsoft.EntityFrameworkCore.DbContext
            ?? throw new InvalidOperationException("DbContext must be EF Core DbContext");

        var metroArea = await dbContext.Set<LankaConnect.Products.LankaEvents.Domain.MetroArea>()
            .FirstOrDefaultAsync(m => m.Name.ToLower() == city.ToLower()
                                   && m.State.ToLower() == state.ToLower(),
                                   cancellationToken);

        if (metroArea != null)
        {
            return ((decimal)metroArea.CenterLatitude, (decimal)metroArea.CenterLongitude);
        }

        return null;
    }

    /// <summary>
    /// Applies additional in-memory filters
    /// </summary>
    private IEnumerable<Event> ApplyInMemoryFilters(IReadOnlyList<Event> events, GetEventsQuery request)
    {
        var filteredEvents = events.AsEnumerable();

        // Phase 6A.152: Active/Inactive buckets are date-based. Same rule used by
        // GetFilteredEventsAsync — re-applied here as defense-in-depth on the
        // SearchAsync path (which returns its own pre-filtered list independent
        // of GetFilteredEventsAsync).
        if (IsDateBasedBucketFilter(request.StatusFilter))
        {
            var beforeBucket = filteredEvents.Count();
            filteredEvents = ApplyDateBasedBucketFilter(
                filteredEvents,
                request.StatusFilter!.Value,
                DateTime.UtcNow);

            _logger.LogDebug(
                "ApplyInMemoryFilters: Date-based bucket filter applied - Bucket={Bucket}, BeforeBucketFilter={BeforeBucketFilter}",
                request.StatusFilter.Value, beforeBucket);
        }
        else
        {
            // Issue #66: Apply status filter for ALL code paths (search + traditional).
            // When SearchAsync is used, the status filter was previously skipped entirely.
            // null = no filter (EventStatusFilter.All), empty array = match nothing (unauthorized Unpublished).
            var resolvedStatuses = ResolveStatusFilter(request.StatusFilter, request.IncludeAllStatuses);
            if (resolvedStatuses != null)
            {
                filteredEvents = filteredEvents.Where(e => resolvedStatuses.Contains(e.Status));
            }
        }

        if (request.Category.HasValue)
        {
            filteredEvents = filteredEvents.Where(e => e.Category == request.Category.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.State))
        {
            filteredEvents = filteredEvents.Where(e =>
                e.Location != null &&
                e.Location.Address.State.Equals(request.State, StringComparison.OrdinalIgnoreCase));
        }

        // Phase 8YB.5 (D5b=A): TBD-Publish filter behaviour.
        //  - When BOTH StartDateFrom AND StartDateTo are set (specific window like
        //    "This Week" / "Next Month"), exclude TBD events. The user explicitly
        //    asked for a dated window — TBD events have no start date to match.
        //  - When only StartDateFrom is set (open-ended "Upcoming" bucket),
        //    INCLUDE TBD events. They surface alongside dated future events;
        //    Phase 8YA.4 sort tiebreaker pushes TBD to the bottom of the list.
        //  - When only StartDateTo is set (rare past-bound query), exclude TBD
        //    events — there's no anchor to compare against the upper bound.
        // Pre-fix the simple inequality `e.StartDate >= from` silently dropped
        // null-StartDate rows even on the open-ended Upcoming path; that's the
        // bug Phase 8YB.5 closes.
        if (request.StartDateFrom.HasValue && request.StartDateTo.HasValue)
        {
            var from = request.StartDateFrom.Value;
            var to = request.StartDateTo.Value;
            filteredEvents = filteredEvents.Where(e =>
                e.StartDate.HasValue &&
                e.StartDate.Value >= from &&
                e.StartDate.Value <= to);
        }
        else if (request.StartDateFrom.HasValue)
        {
            var from = request.StartDateFrom.Value;
            filteredEvents = filteredEvents.Where(e =>
                !e.StartDate.HasValue || e.StartDate.Value >= from);
        }
        else if (request.StartDateTo.HasValue)
        {
            var to = request.StartDateTo.Value;
            filteredEvents = filteredEvents.Where(e =>
                e.StartDate.HasValue && e.StartDate.Value <= to);
        }

        if (request.IsFreeOnly == true)
        {
            filteredEvents = filteredEvents.Where(e => e.IsFree());
        }

        return filteredEvents;
    }

    /// <summary>
    /// Phase 6A.137: Status priority for deduplication (lower = higher priority).
    /// Confirmed is highest priority since it represents a completed registration.
    /// </summary>
    private static int GetStatusPriority(RegistrationStatus status) => status switch
    {
        RegistrationStatus.Confirmed => 0,
        RegistrationStatus.CheckedIn => 1,
        RegistrationStatus.Attended => 2,
        RegistrationStatus.Waitlisted => 3,
        RegistrationStatus.RefundRequested => 4,
        RegistrationStatus.Preliminary => 5,
        _ => 6
    };
}
