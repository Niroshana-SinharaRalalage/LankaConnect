using AutoMapper;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users;

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
        decimal targetLatitude;
        decimal targetLongitude;

        // Determine target location for sorting
        if (request.UserId.HasValue)
        {
            // Authenticated user - check for preferred metro areas or user location
            var locationResult = await GetUserTargetLocationAsync(request.UserId.Value, cancellationToken);
            if (locationResult.IsSuccess && locationResult.Value != null)
            {
                (targetLatitude, targetLongitude) = locationResult.Value.Value;
            }
            else
            {
                // User has no location data - use default
                targetLatitude = DEFAULT_LATITUDE;
                targetLongitude = DEFAULT_LONGITUDE;
            }
        }
        else if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            // Anonymous user with provided coordinates
            targetLatitude = request.Latitude.Value;
            targetLongitude = request.Longitude.Value;
        }
        else
        {
            // Anonymous user without coordinates - use default location
            targetLatitude = DEFAULT_LATITUDE;
            targetLongitude = DEFAULT_LONGITUDE;
        }

        // Get nearest published events using spatial query
        var nearestEvents = await _eventRepository.GetNearestEventsAsync(
            targetLatitude,
            targetLongitude,
            maxResults: MAX_RESULTS * 3, // Get more to filter
            cancellationToken);

        // Filter for published and upcoming events only
        var now = DateTime.UtcNow;
        var featuredEvents = nearestEvents
            .Where(e => e.Status == Domain.Events.Enums.EventStatus.Published
                     && e.StartDate > now)
            .OrderBy(e => e.StartDate)
            .Take(MAX_RESULTS)
            .ToList();

        // If we don't have enough events from nearest query, get more published events
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
    /// Gets the target location for an authenticated user
    /// Uses first preferred metro area if available, otherwise returns null
    /// Note: UserLocation value object doesn't contain coordinates, so only metro areas are used
    /// </summary>
    private async Task<Result<(decimal Latitude, decimal Longitude)?>> GetUserTargetLocationAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            return Result<(decimal Latitude, decimal Longitude)?>.Success(null);
        }

        // Priority 1: Use first preferred metro area if available
        if (user.PreferredMetroAreaIds.Any())
        {
            var metroAreaId = user.PreferredMetroAreaIds.First();

            // Get metro area details from database
            var dbContext = _dbContext as Microsoft.EntityFrameworkCore.DbContext
                ?? throw new InvalidOperationException("DbContext must be EF Core DbContext");

            var metroArea = await dbContext.Set<Domain.Events.MetroArea>()
                .FindAsync(new object[] { metroAreaId }, cancellationToken);

            if (metroArea != null)
            {
                return Result<(decimal Latitude, decimal Longitude)?>.Success(
                    ((decimal)metroArea.CenterLatitude, (decimal)metroArea.CenterLongitude));
            }
        }

        // No location data available
        return Result<(decimal Latitude, decimal Longitude)?>.Success(null);
    }
}
