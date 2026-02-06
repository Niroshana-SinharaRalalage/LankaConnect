using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Infrastructure.Services;

/// <summary>
/// Phase 6A.74 Part 13: Service to determine which metro areas contain a given event based on geographic proximity
/// Reuses geo-spatial bucketing logic from GetEventsQueryHandler (Phase 6B) and EventNotificationRecipientService (Phase 6A.70)
///
/// Example: Event in Aurora, OH (41.3175°, -81.3473°) → Returns Cleveland metro ID
/// Cleveland metro center: (41.4993°, -81.6944°), radius: 50 miles → Aurora is ~20 miles away
/// </summary>
public class EventMetroAreaMatcher
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IGeoLocationService _geoLocationService;
    private readonly ILogger<EventMetroAreaMatcher> _logger;

    public EventMetroAreaMatcher(
        IApplicationDbContext dbContext,
        IGeoLocationService geoLocationService,
        ILogger<EventMetroAreaMatcher> logger)
    {
        _dbContext = dbContext;
        _geoLocationService = geoLocationService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all metro areas that contain the given event based on geographic distance
    /// Uses Haversine formula to calculate distance from event to metro center
    /// Returns metro area if event is within the metro's radius
    /// </summary>
    /// <param name="event">Event with location and coordinates</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of metro area IDs that contain this event</returns>
    public async Task<List<Guid>> GetMetroAreasForEventAsync(
        Event @event,
        CancellationToken cancellationToken = default)
    {
        if (@event?.Location?.Coordinates == null)
        {
            _logger.LogWarning(
                "[EventMetroAreaMatcher] Event {@EventId} has no location or coordinates",
                @event?.Id);
            return new List<Guid>();
        }

        var eventLat = (decimal)@event.Location.Coordinates.Latitude;
        var eventLng = (decimal)@event.Location.Coordinates.Longitude;

        _logger.LogInformation(
            "[EventMetroAreaMatcher] Matching event {EventId} at ({Lat}, {Lng}) to metro areas",
            @event.Id,
            eventLat,
            eventLng);

        try
        {
            // Get all active metro areas from database
            var dbContext = _dbContext as Microsoft.EntityFrameworkCore.DbContext
                ?? throw new InvalidOperationException("DbContext must be EF Core DbContext");

            var allMetros = await dbContext.Set<MetroArea>()
                .Where(m => m.IsActive)
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "[EventMetroAreaMatcher] Retrieved {Count} active metro areas for matching",
                allMetros.Count);

            var matchingMetroIds = new List<Guid>();

            // Check each metro area to see if event falls within its radius
            foreach (var metro in allMetros)
            {
                var isWithinRadius = _geoLocationService.IsWithinMetroRadius(
                    eventLat,
                    eventLng,
                    (decimal)metro.CenterLatitude,
                    (decimal)metro.CenterLongitude,
                    metro.RadiusMiles);

                if (isWithinRadius)
                {
                    var distanceKm = _geoLocationService.CalculateDistanceKm(
                        (decimal)metro.CenterLatitude,
                        (decimal)metro.CenterLongitude,
                        eventLat,
                        eventLng);

                    _logger.LogInformation(
                        "[EventMetroAreaMatcher] Event {EventId} within {MetroName} metro area: Distance={DistanceKm:F2}km, Radius={RadiusMiles}mi ({RadiusKm:F2}km)",
                        @event.Id,
                        metro.Name,
                        distanceKm,
                        metro.RadiusMiles,
                        metro.RadiusMiles * 1.60934);

                    matchingMetroIds.Add(metro.Id);
                }
            }

            _logger.LogInformation(
                "[EventMetroAreaMatcher] Event {EventId} matches {Count} metro area(s): [{MetroIds}]",
                @event.Id,
                matchingMetroIds.Count,
                string.Join(", ", matchingMetroIds));

            return matchingMetroIds;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[EventMetroAreaMatcher] Failed to match event {EventId} to metro areas",
                @event.Id);
            throw;
        }
    }

    /// <summary>
    /// Checks if an event location matches any of the specified metro areas
    /// Used for newsletter page filtering: Does Aurora event match Cleveland metro filter?
    /// </summary>
    /// <param name="event">Event with location</param>
    /// <param name="metroAreaIds">Metro area IDs to check against</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if event matches any of the specified metro areas</returns>
    public async Task<bool> MatchesAnyMetroAreaAsync(
        Event @event,
        IReadOnlyList<Guid> metroAreaIds,
        CancellationToken cancellationToken = default)
    {
        if (metroAreaIds == null || !metroAreaIds.Any())
        {
            return false;
        }

        var matchingMetros = await GetMetroAreasForEventAsync(@event, cancellationToken);
        var matches = matchingMetros.Any(id => metroAreaIds.Contains(id));

        _logger.LogInformation(
            "[EventMetroAreaMatcher] Event {EventId} matches filter: {Matches} (matched {MatchCount} of {FilterCount} metros)",
            @event.Id,
            matches,
            matchingMetros.Count(id => metroAreaIds.Contains(id)),
            metroAreaIds.Count);

        return matches;
    }
}
