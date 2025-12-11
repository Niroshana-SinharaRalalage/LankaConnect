using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;

namespace LankaConnect.Application.Events.Queries.GetWaitingList;

/// <summary>
/// Handler for GetWaitingListQuery
/// Returns list of users on waiting list with their positions
/// </summary>
public class GetWaitingListQueryHandler : IQueryHandler<GetWaitingListQuery, IReadOnlyList<WaitingListEntryDto>>
{
    private readonly IEventRepository _eventRepository;

    public GetWaitingListQueryHandler(IEventRepository eventRepository)
    {
        _eventRepository = eventRepository;
    }

    public async Task<Result<IReadOnlyList<WaitingListEntryDto>>> Handle(GetWaitingListQuery request, CancellationToken cancellationToken)
    {
        // Retrieve event
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result<IReadOnlyList<WaitingListEntryDto>>.Failure("Event not found");

        // Map waiting list entries to DTOs
        var waitingListDtos = @event.WaitingList
            .OrderBy(w => w.Position)
            .Select(w => new WaitingListEntryDto
            {
                UserId = w.UserId,
                Position = w.Position,
                JoinedAt = w.JoinedAt
            })
            .ToList()
            .AsReadOnly();

        return Result<IReadOnlyList<WaitingListEntryDto>>.Success(waitingListDtos);
    }
}
