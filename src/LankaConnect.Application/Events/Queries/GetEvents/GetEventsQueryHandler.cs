using AutoMapper;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace LankaConnect.Application.Events.Queries.GetEvents;

/// <summary>
/// Handler for GetEventsQuery - Events listing page
/// Supports location-based sorting and traditional filtering
/// Returns ALL matching events (not limited to 4 like featured events)
/// </summary>
public class GetEventsQueryHandler : IQueryHandler<GetEventsQuery, IReadOnlyList<EventDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUserRepository _userRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public GetEventsQueryHandler(
        IEventRepository eventRepository,
        IUserRepository userRepository,
        IApplicationDbContext dbContext,
        IMapper mapper)
    {
        _eventRepository = eventRepository;
        _userRepository = userRepository;
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<EventDto>>> Handle(GetEventsQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        // Phase 6A.47: If SearchTerm provided, use full-text search first
        IReadOnlyList<Event> events;
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            // Step 1a: Apply full-text search with filters
            (events, _) = await _eventRepository.SearchAsync(
                request.SearchTerm,
                limit: 1000, // Large limit for search
                offset: 0,
                request.Category,
                request.IsFreeOnly,
                request.StartDateFrom,
                cancellationToken);
        }
        else
        {
            // Step 1b: Get base event list with traditional filtering
            events = await GetFilteredEventsAsync(request, cancellationToken);
        }

        // Step 2: Apply location-based sorting if location parameters provided
        if (ShouldApplyLocationSorting(request))
        {
            events = await ApplyLocationBasedSortingAsync(events, request, now, cancellationToken);
        }

        // Step 3: Apply additional in-memory filters
        var filteredEvents = ApplyInMemoryFilters(events, request);

        // Step 4: Sort and map to DTOs
        var result = filteredEvents
            .OrderBy(e => e.StartDate)
            .Select(e => _mapper.Map<EventDto>(e))
            .ToList();

        return Result<IReadOnlyList<EventDto>>.Success(result);
    }

    /// <summary>
    /// Gets filtered events based on status, city, or defaults to all visible events
    /// Phase 6A.59: Changed default from Published-only to all except Draft/UnderReview
    /// This allows cancelled events to be visible to users
    /// </summary>
    private async Task<IReadOnlyList<Event>> GetFilteredEventsAsync(
        GetEventsQuery request,
        CancellationToken cancellationToken)
    {
        // If status filter is provided, use repository method
        if (request.Status.HasValue)
        {
            return await _eventRepository.GetEventsByStatusAsync(request.Status.Value, cancellationToken);
        }

        // If city filter is provided, use repository method
        if (!string.IsNullOrWhiteSpace(request.City))
        {
            return await _eventRepository.GetEventsByCityAsync(request.City, cancellationToken: cancellationToken);
        }

        // Phase 6A.59: Get ALL events except Draft and UnderReview
        // This includes Published, Active, Cancelled, Completed, Archived, Postponed
        // Cancelled events will show with CANCELLED badge in UI
        var allEvents = await _eventRepository.GetAllAsync(cancellationToken);
        return allEvents
            .Where(e => e.Status != EventStatus.Draft && e.Status != EventStatus.UnderReview)
            .ToList();
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
            var user = await _userRepository.GetByIdAsync(request.UserId.Value, cancellationToken);
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
            if (user != null && user.Location != null && remainingEvents.Any())
            {
                var homeCoordinates = await GetMetroAreaCoordinatesByCityStateAsync(
                    user.Location.City,
                    user.Location.State,
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
    /// Sorts events by distance using Haversine formula
    /// </summary>
    private List<Event> SortEventsByDistance(
        List<Event> events,
        decimal latitude,
        decimal longitude)
    {
        return events
            .Where(e => e.Location?.Coordinates != null)
            .Select(e => new
            {
                Event = e,
                Distance = CalculateDistance(
                    latitude,
                    longitude,
                    e.Location!.Coordinates!.Latitude,
                    e.Location.Coordinates.Longitude)
            })
            .OrderBy(x => x.Distance)
            .Select(x => x.Event)
            .ToList();
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

        var metroArea = await dbContext.Set<Domain.Events.MetroArea>()
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

        var metroArea = await dbContext.Set<Domain.Events.MetroArea>()
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

        var metroArea = await dbContext.Set<Domain.Events.MetroArea>()
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

        if (request.StartDateFrom.HasValue)
        {
            filteredEvents = filteredEvents.Where(e => e.StartDate >= request.StartDateFrom.Value);
        }

        if (request.StartDateTo.HasValue)
        {
            filteredEvents = filteredEvents.Where(e => e.StartDate <= request.StartDateTo.Value);
        }

        if (request.IsFreeOnly == true)
        {
            filteredEvents = filteredEvents.Where(e => e.IsFree());
        }

        return filteredEvents;
    }
}
