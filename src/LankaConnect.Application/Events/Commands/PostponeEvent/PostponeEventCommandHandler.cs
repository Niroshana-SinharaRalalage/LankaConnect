using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;

namespace LankaConnect.Application.Events.Commands.PostponeEvent;

public class PostponeEventCommandHandler : ICommandHandler<PostponeEventCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PostponeEventCommandHandler(IEventRepository eventRepository, IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(PostponeEventCommand request, CancellationToken cancellationToken)
    {
        // Phase 6A.53 FIX: Retrieve event WITH CHANGE TRACKING (trackChanges: true)
        // This is required for EF Core to detect changes when we modify the entity
        var @event = await _eventRepository.GetByIdAsync(request.EventId, trackChanges: true, cancellationToken);
        if (@event == null)
            return Result.Failure("Event not found");

        // Use domain method to postpone
        var postponeResult = @event.Postpone(request.PostponementReason);
        if (postponeResult.IsFailure)
            return postponeResult;

        // Save changes (EF Core tracks changes automatically)
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
