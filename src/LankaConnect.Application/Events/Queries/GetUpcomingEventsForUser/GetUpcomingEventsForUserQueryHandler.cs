using AutoMapper;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Application.Events.Queries.GetUpcomingEventsForUser;

public class GetUpcomingEventsForUserQueryHandler : IQueryHandler<GetUpcomingEventsForUserQuery, IReadOnlyList<EventDto>>
{
    private readonly IRegistrationRepository _registrationRepository;
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;

    public GetUpcomingEventsForUserQueryHandler(
        IRegistrationRepository registrationRepository,
        IEventRepository eventRepository,
        IMapper mapper)
    {
        _registrationRepository = registrationRepository;
        _eventRepository = eventRepository;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<EventDto>>> Handle(GetUpcomingEventsForUserQuery request, CancellationToken cancellationToken)
    {
        // Get all confirmed registrations for the user
        var registrations = await _registrationRepository.GetByUserAsync(request.UserId, cancellationToken);
        var confirmedRegistrations = registrations.Where(r => r.Status == RegistrationStatus.Confirmed).ToList();

        // Get event IDs
        var eventIds = confirmedRegistrations.Select(r => r.EventId).ToList();

        // Fetch events
        var upcomingEvents = new List<EventDto>();

        foreach (var eventId in eventIds)
        {
            var @event = await _eventRepository.GetByIdAsync(eventId, cancellationToken);

            // Filter: upcoming events (start date in the future) and published status
            if (@event != null && @event.StartDate > DateTime.UtcNow && @event.Status == EventStatus.Published)
            {
                upcomingEvents.Add(_mapper.Map<EventDto>(@event));
            }
        }

        // Sort by start date
        var sortedEvents = upcomingEvents.OrderBy(e => e.StartDate).ToList();

        return Result<IReadOnlyList<EventDto>>.Success(sortedEvents);
    }
}
