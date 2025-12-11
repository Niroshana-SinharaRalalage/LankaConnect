using AutoMapper;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace LankaConnect.Application.Events.Queries.GetFeaturedEvents;

/// <summary>
/// Handler for GetFeaturedEventsQuery - Landing page featured events
/// Implements location-based sorting with metro area preferences support
/// Returns up to 4 published upcoming events sorted by relevance
/// </summary>
public class GetFeaturedEventsQueryHandler : IQueryHandler<GetFeaturedEventsQuery, IReadOnlyList<EventDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUserRepository _userRepository;
    private readonly IApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    // Default location (Los Angeles, CA) used when no location data available
    private const decimal DEFAULT_LATITUDE = 34.0522m;
    private const decimal DEFAULT_LONGITUDE = -118.2437m;
    private const int MAX_RESULTS = 4;

    public GetFeaturedEventsQueryHandler(
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

    public async Task<Result<IReadOnlyList<EventDto>>> Handle(
        GetFeaturedEventsQuery request,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var featuredEvents = new List<Event>();

        // PRIORITY 1: Get events from preferred metro areas (for authenticated users)
        if (request.UserId.HasValue)
        {
            var user = await _userRepository.GetByIdAsync(request.UserId.Value, cancellationToken);
            if (user != null && user.PreferredMetroAreaIds.Any())
            {
                // Try to get events from ALL preferred metros, not just the first
                foreach (var metroId in user.PreferredMetroAreaIds)
                {
                    if (featuredEvents.Count >= MAX_RESULTS)
                        break;

                    var coordinates = await GetMetroAreaCoordinatesAsync(metroId, cancellationToken);
                    if (coordinates.HasValue)
                    {
                        var metroEvents = await GetEventsNearLocationAsync(
                            coordinates.Value.Latitude,
                            coordinates.Value.Longitude,
                            MAX_RESULTS - featuredEvents.Count,
                            now,
                            cancellationToken);

                        // Add events that aren't already in the list
                        foreach (var evt in metroEvents)
                        {
                            if (!featuredEvents.Any(fe => fe.Id == evt.Id))
                            {
                                featuredEvents.Add(evt);
                                if (featuredEvents.Count >= MAX_RESULTS)
                                    break;
                            }
                        }
                    }
                }
            }

            // PRIORITY 2: If not enough events from preferred metros, try user's home location
            if (featuredEvents.Count < MAX_RESULTS && user != null && user.Location != null)
            {
                var homeCoordinates = await GetMetroAreaCoordinatesByCityStateAsync(
                    user.Location.City,
                    user.Location.State,
                    cancellationToken);

                if (homeCoordinates.HasValue)
                {
                    var homeEvents = await GetEventsNearLocationAsync(
                        homeCoordinates.Value.Latitude,
                        homeCoordinates.Value.Longitude,
                        MAX_RESULTS - featuredEvents.Count,
                        now,
                        cancellationToken);

                    foreach (var evt in homeEvents)
                    {
                        if (!featuredEvents.Any(fe => fe.Id == evt.Id))
                        {
                            featuredEvents.Add(evt);
                            if (featuredEvents.Count >= MAX_RESULTS)
                                break;
                        }
                    }
                }
            }
        }
        else if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            // Anonymous user with provided coordinates
            featuredEvents = await GetEventsNearLocationAsync(
                request.Latitude.Value,
                request.Longitude.Value,
                MAX_RESULTS,
                now,
                cancellationToken);
        }

        // PRIORITY 3: Final fallback - get any published upcoming events
        if (featuredEvents.Count < MAX_RESULTS)
        {
            var publishedEvents = await _eventRepository.GetPublishedEventsAsync(cancellationToken);
            var additionalEvents = publishedEvents
                .Where(e => e.StartDate > now
                         && !featuredEvents.Any(fe => fe.Id == e.Id))
                .OrderBy(e => e.StartDate)
                .Take(MAX_RESULTS - featuredEvents.Count)
                .ToList();

            featuredEvents.AddRange(additionalEvents);
        }

        // Map to DTOs
        var result = featuredEvents
            .Select(e => _mapper.Map<EventDto>(e))
            .ToList();

        return Result<IReadOnlyList<EventDto>>.Success(result);
    }

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
    /// Looks up metro area coordinates by city and state
    /// Used for fallback to user's home location when UserLocation doesn't have coordinates
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

        // Find metro area matching the city name and state
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
    /// Gets events near a specific location using Haversine distance calculation
    /// Returns published upcoming events sorted by distance
    /// </summary>
    private async Task<List<Event>> GetEventsNearLocationAsync(
        decimal latitude,
        decimal longitude,
        int maxResults,
        DateTime now,
        CancellationToken cancellationToken)
    {
        // Get more results than needed to ensure we have enough after filtering
        var nearestEvents = await _eventRepository.GetNearestEventsAsync(
            latitude,
            longitude,
            maxResults: maxResults * 3,
            cancellationToken);

        // Filter for published and upcoming events only
        return nearestEvents
            .Where(e => e.Status == Domain.Events.Enums.EventStatus.Published
                     && e.StartDate > now)
            .Take(maxResults)
            .ToList();
    }
}
