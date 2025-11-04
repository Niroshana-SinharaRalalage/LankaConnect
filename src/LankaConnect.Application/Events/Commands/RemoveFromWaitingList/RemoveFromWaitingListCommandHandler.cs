using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;

namespace LankaConnect.Application.Events.Commands.RemoveFromWaitingList;

/// <summary>
/// Handler for RemoveFromWaitingListCommand
/// Removes user from event waiting list and resequences positions
/// </summary>
public class RemoveFromWaitingListCommandHandler : ICommandHandler<RemoveFromWaitingListCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveFromWaitingListCommandHandler(IEventRepository eventRepository, IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RemoveFromWaitingListCommand request, CancellationToken cancellationToken)
    {
        // Retrieve event
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result.Failure("Event not found");

        // Use domain method to remove user from waiting list
        var removeResult = @event.RemoveFromWaitingList(request.UserId);
        if (removeResult.IsFailure)
            return removeResult;

        // Save changes (EF Core tracks changes automatically)
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
