using AutoMapper;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using Microsoft.EntityFrameworkCore;

namespace LankaConnect.Application.Events.Queries.GetEvents;

public class GetEventsQueryHandler : IQueryHandler<GetEventsQuery, IReadOnlyList<EventDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;

    public GetEventsQueryHandler(IEventRepository eventRepository, IMapper mapper)
    {
        _eventRepository = eventRepository;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<EventDto>>> Handle(GetEventsQuery request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Event> events;

        // If status filter is provided, use repository method
        if (request.Status.HasValue)
        {
            events = await _eventRepository.GetEventsByStatusAsync(request.Status.Value, cancellationToken);
        }
        // If city filter is provided, use repository method
        else if (!string.IsNullOrWhiteSpace(request.City))
        {
            events = await _eventRepository.GetEventsByCityAsync(request.City, cancellationToken: cancellationToken);
        }
        // Otherwise get published events by default
        else
        {
            events = await _eventRepository.GetPublishedEventsAsync(cancellationToken);
        }

        // Apply additional in-memory filters
        var filteredEvents = events.AsEnumerable();

        if (request.Category.HasValue)
        {
            filteredEvents = filteredEvents.Where(e => e.Category == request.Category.Value);
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

        var result = filteredEvents
            .OrderBy(e => e.StartDate)
            .Select(e => _mapper.Map<EventDto>(e))
            .ToList();

        return Result<IReadOnlyList<EventDto>>.Success(result);
    }
}
