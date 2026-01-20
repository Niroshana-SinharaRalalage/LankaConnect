using System.Diagnostics;
using AutoMapper;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Queries.GetNearbyEvents;

/// <summary>
/// Handler for GetNearbyEventsQuery - Location-based event discovery (Epic 2 Phase 3)
/// Uses PostGIS spatial queries for efficient distance-based searches
/// </summary>
public class GetNearbyEventsQueryHandler : IQueryHandler<GetNearbyEventsQuery, IReadOnlyList<EventDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetNearbyEventsQueryHandler> _logger;

    // Conversion factor: 1 kilometer = 0.621371 miles
    private const double KM_TO_MILES = 0.621371;

    public GetNearbyEventsQueryHandler(
        IEventRepository eventRepository,
        IMapper mapper,
        ILogger<GetNearbyEventsQueryHandler> logger)
    {
        _eventRepository = eventRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<EventDto>>> Handle(GetNearbyEventsQuery request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetNearbyEvents"))
        using (LogContext.PushProperty("EntityType", "Event"))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetNearbyEvents START: Lat={Latitude}, Lon={Longitude}, RadiusKm={RadiusKm}, Category={Category}, IsFreeOnly={IsFreeOnly}",
                request.Latitude, request.Longitude, request.RadiusKm, request.Category, request.IsFreeOnly);

            try
            {
                // Validate coordinates
                if (request.Latitude < -90 || request.Latitude > 90)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetNearbyEvents FAILED: Invalid latitude - Latitude={Latitude}, Duration={ElapsedMs}ms",
                        request.Latitude, stopwatch.ElapsedMilliseconds);

                    return Result<IReadOnlyList<EventDto>>.Failure("Invalid latitude. Latitude must be between -90 and 90 degrees.");
                }

                if (request.Longitude < -180 || request.Longitude > 180)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetNearbyEvents FAILED: Invalid longitude - Longitude={Longitude}, Duration={ElapsedMs}ms",
                        request.Longitude, stopwatch.ElapsedMilliseconds);

                    return Result<IReadOnlyList<EventDto>>.Failure("Invalid longitude. Longitude must be between -180 and 180 degrees.");
                }

                // Validate radius
                if (request.RadiusKm <= 0 || request.RadiusKm > 1000)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "GetNearbyEvents FAILED: Invalid radius - RadiusKm={RadiusKm}, Duration={ElapsedMs}ms",
                        request.RadiusKm, stopwatch.ElapsedMilliseconds);

                    return Result<IReadOnlyList<EventDto>>.Failure("Invalid radius. Radius must be between 0.1 and 1000 kilometers.");
                }

                // Convert kilometers to miles for repository call
                var radiusMiles = request.RadiusKm * KM_TO_MILES;

                _logger.LogInformation(
                    "GetNearbyEvents: Executing spatial query - RadiusKm={RadiusKm}, RadiusMiles={RadiusMiles}",
                    request.RadiusKm, radiusMiles);

                // Get nearby events using PostGIS spatial query
                var events = await _eventRepository.GetEventsByRadiusAsync(
                    request.Latitude,
                    request.Longitude,
                    radiusMiles,
                    cancellationToken
                );

                _logger.LogInformation(
                    "GetNearbyEvents: Spatial query completed - EventCount={EventCount}",
                    events.Count);

                // Apply optional in-memory filters
                var filteredEvents = events.AsEnumerable();
                var beforeFilterCount = events.Count;

                if (request.Category.HasValue)
                {
                    filteredEvents = filteredEvents.Where(e => e.Category == request.Category.Value);
                }

                if (request.IsFreeOnly == true)
                {
                    filteredEvents = filteredEvents.Where(e => e.IsFree());
                }

                if (request.StartDateFrom.HasValue)
                {
                    filteredEvents = filteredEvents.Where(e => e.StartDate >= request.StartDateFrom.Value);
                }

                // Map to DTOs
                var result = filteredEvents
                    .OrderBy(e => e.StartDate)
                    .Select(e => _mapper.Map<EventDto>(e))
                    .ToList();

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetNearbyEvents COMPLETE: Lat={Latitude}, Lon={Longitude}, TotalResults={TotalResults}, BeforeFilter={BeforeFilter}, Duration={ElapsedMs}ms",
                    request.Latitude, request.Longitude, result.Count, beforeFilterCount, stopwatch.ElapsedMilliseconds);

                return Result<IReadOnlyList<EventDto>>.Success(result);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "GetNearbyEvents FAILED: Exception occurred - Lat={Latitude}, Lon={Longitude}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.Latitude, request.Longitude, stopwatch.ElapsedMilliseconds, ex.Message);

                throw;
            }
        }
    }
}
