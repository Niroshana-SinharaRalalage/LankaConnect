using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;

namespace LankaConnect.Application.Events.Commands.PublishEvent;

public class PublishEventCommandHandler : ICommandHandler<PublishEventCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public PublishEventCommandHandler(IEventRepository eventRepository, IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(PublishEventCommand request, CancellationToken cancellationToken)
    {
        // Retrieve event
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result.Failure("Event not found");

        // Use domain method to publish
        var publishResult = @event.Publish();
        if (publishResult.IsFailure)
            return publishResult;

        // Save changes (EF Core tracks changes automatically)
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
