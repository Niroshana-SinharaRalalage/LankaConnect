using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;

namespace LankaConnect.Application.Events.Commands.AddToWaitingList;

/// <summary>
/// Handler for AddToWaitingListCommand
/// Adds user to event waiting list when event is at capacity
/// </summary>
public class AddToWaitingListCommandHandler : ICommandHandler<AddToWaitingListCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddToWaitingListCommandHandler(IEventRepository eventRepository, IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(AddToWaitingListCommand request, CancellationToken cancellationToken)
    {
        // Retrieve event
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result.Failure("Event not found");

        // Use domain method to add user to waiting list
        var addResult = @event.AddToWaitingList(request.UserId);
        if (addResult.IsFailure)
            return addResult;

        // Save changes (EF Core tracks changes automatically)
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
