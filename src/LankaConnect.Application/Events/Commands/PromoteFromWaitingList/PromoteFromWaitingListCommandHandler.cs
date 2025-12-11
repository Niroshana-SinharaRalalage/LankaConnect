using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;

namespace LankaConnect.Application.Events.Commands.PromoteFromWaitingList;

/// <summary>
/// Handler for PromoteFromWaitingListCommand
/// Promotes user from waiting list to confirmed registration when capacity is available
/// </summary>
public class PromoteFromWaitingListCommandHandler : ICommandHandler<PromoteFromWaitingListCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PromoteFromWaitingListCommandHandler(IEventRepository eventRepository, IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(PromoteFromWaitingListCommand request, CancellationToken cancellationToken)
    {
        // Retrieve event
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result.Failure("Event not found");

        // Use domain method to promote user from waiting list
        var promoteResult = @event.PromoteFromWaitingList(request.UserId);
        if (promoteResult.IsFailure)
            return promoteResult;

        // Save changes (EF Core tracks changes automatically)
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
