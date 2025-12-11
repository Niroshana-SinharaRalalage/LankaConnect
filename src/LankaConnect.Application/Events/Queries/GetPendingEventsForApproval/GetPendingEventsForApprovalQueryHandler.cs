using AutoMapper;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Application.Events.Queries.GetPendingEventsForApproval;

public class GetPendingEventsForApprovalQueryHandler : IQueryHandler<GetPendingEventsForApprovalQuery, IReadOnlyList<EventDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;

    public GetPendingEventsForApprovalQueryHandler(IEventRepository eventRepository, IMapper mapper)
    {
        _eventRepository = eventRepository;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<EventDto>>> Handle(GetPendingEventsForApprovalQuery request, CancellationToken cancellationToken)
    {
        // Get all events with UnderReview status
        var allEvents = await _eventRepository.GetAllAsync(cancellationToken);
        var pendingEvents = allEvents
            .Where(e => e.Status == EventStatus.UnderReview)
            .OrderBy(e => e.CreatedAt) // Oldest first for admin review
            .ToList();

        var eventDtos = _mapper.Map<IReadOnlyList<EventDto>>(pendingEvents);

        return Result<IReadOnlyList<EventDto>>.Success(eventDtos);
    }
}
