using AutoMapper;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;

namespace LankaConnect.Application.Events.Queries.GetEventsByOrganizer;

public class GetEventsByOrganizerQueryHandler : IQueryHandler<GetEventsByOrganizerQuery, IReadOnlyList<EventDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;

    public GetEventsByOrganizerQueryHandler(IEventRepository eventRepository, IMapper mapper)
    {
        _eventRepository = eventRepository;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<EventDto>>> Handle(GetEventsByOrganizerQuery request, CancellationToken cancellationToken)
    {
        var events = await _eventRepository.GetByOrganizerAsync(request.OrganizerId, cancellationToken);

        var eventDtos = events
            .Select(e => _mapper.Map<EventDto>(e))
            .ToList();

        return Result<IReadOnlyList<EventDto>>.Success(eventDtos);
    }
}
