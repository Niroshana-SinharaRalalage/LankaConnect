using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;

namespace LankaConnect.Application.Events.Commands.RemovePassFromEvent;

public class RemovePassFromEventCommandHandler : ICommandHandler<RemovePassFromEventCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemovePassFromEventCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RemovePassFromEventCommand request, CancellationToken cancellationToken)
    {
        // Get the event
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result.Failure("Event not found");

        // Remove pass from event
        var removeResult = @event.RemovePass(request.PassId);
        if (removeResult.IsFailure)
            return Result.Failure(removeResult.Error);

        // Save changes
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
