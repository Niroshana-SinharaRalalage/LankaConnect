using AutoMapper;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;

namespace LankaConnect.Application.Events.Queries.GetNearbyEvents;

/// <summary>
/// Handler for GetNearbyEventsQuery - Location-based event discovery (Epic 2 Phase 3)
/// Uses PostGIS spatial queries for efficient distance-based searches
/// </summary>
public class GetNearbyEventsQueryHandler : IQueryHandler<GetNearbyEventsQuery, IReadOnlyList<EventDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;

    // Conversion factor: 1 kilometer = 0.621371 miles
    private const double KM_TO_MILES = 0.621371;

    public GetNearbyEventsQueryHandler(IEventRepository eventRepository, IMapper mapper)
    {
        _eventRepository = eventRepository;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<EventDto>>> Handle(GetNearbyEventsQuery request, CancellationToken cancellationToken)
    {
        // Validate coordinates
        if (request.Latitude < -90 || request.Latitude > 90)
        {
            return Result<IReadOnlyList<EventDto>>.Failure("Invalid latitude. Latitude must be between -90 and 90 degrees.");
        }

        if (request.Longitude < -180 || request.Longitude > 180)
        {
            return Result<IReadOnlyList<EventDto>>.Failure("Invalid longitude. Longitude must be between -180 and 180 degrees.");
        }

        // Validate radius
        if (request.RadiusKm <= 0 || request.RadiusKm > 1000)
        {
            return Result<IReadOnlyList<EventDto>>.Failure("Invalid radius. Radius must be between 0.1 and 1000 kilometers.");
        }

        // Convert kilometers to miles for repository call
        var radiusMiles = request.RadiusKm * KM_TO_MILES;

        // Get nearby events using PostGIS spatial query
        var events = await _eventRepository.GetEventsByRadiusAsync(
            request.Latitude,
            request.Longitude,
            radiusMiles,
            cancellationToken
        );

        // Apply optional in-memory filters
        var filteredEvents = events.AsEnumerable();

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

        return Result<IReadOnlyList<EventDto>>.Success(result);
    }
}
