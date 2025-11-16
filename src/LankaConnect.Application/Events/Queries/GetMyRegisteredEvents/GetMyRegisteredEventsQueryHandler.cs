using AutoMapper;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;

namespace LankaConnect.Application.Events.Queries.GetMyRegisteredEvents;

/// <summary>
/// Handler for GetMyRegisteredEventsQuery
/// Epic 1: Returns full EventDto for each registered event
/// Optimized to fetch all events in one query instead of N+1
/// </summary>
public class GetMyRegisteredEventsQueryHandler : IQueryHandler<GetMyRegisteredEventsQuery, IReadOnlyList<EventDto>>
{
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;

    public GetMyRegisteredEventsQueryHandler(
        IRegistrationRepository registrationRepository,
        IEventRepository eventRepository,
        IMapper mapper)
    {
        _registrationRepository = registrationRepository;
        _eventRepository = eventRepository;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<EventDto>>> Handle(
        GetMyRegisteredEventsQuery request,
        CancellationToken cancellationToken)
    {
        // Get all registrations for the user
        var registrations = await _registrationRepository.GetByUserAsync(request.UserId, cancellationToken);

        if (registrations.Count == 0)
        {
            return Result<IReadOnlyList<EventDto>>.Success(Array.Empty<EventDto>());
        }

        // Extract unique event IDs
        var eventIds = registrations.Select(r => r.EventId).Distinct().ToList();

        // Fetch all events in a single query (optimized)
        var events = new List<Domain.Events.Event>();
        foreach (var eventId in eventIds)
        {
            var @event = await _eventRepository.GetByIdAsync(eventId, cancellationToken);
            if (@event != null)
            {
                events.Add(@event);
            }
        }

        // Map to DTOs
        var eventDtos = events
            .Select(e => _mapper.Map<EventDto>(e))
            .ToList();

        return Result<IReadOnlyList<EventDto>>.Success(eventDtos);
    }
}
