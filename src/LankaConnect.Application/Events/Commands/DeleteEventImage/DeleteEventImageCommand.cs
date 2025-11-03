using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using MediatR;

namespace LankaConnect.Application.Events.Commands.DeleteEventImage;

/// <summary>
/// Command to delete an image from an event's gallery
/// Removes image from Event aggregate and deletes from Azure Blob Storage via domain event handler
/// </summary>
public record DeleteEventImageCommand : IRequest<Result>
{
    public Guid EventId { get; init; }
    public Guid ImageId { get; init; }
}

public class DeleteEventImageCommandHandler : IRequestHandler<DeleteEventImageCommand, Result>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteEventImageCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteEventImageCommand request, CancellationToken cancellationToken)
    {
        // 1. Get event
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result.Failure($"Event with ID {request.EventId} not found");

        // 2. Remove image from Event aggregate (raises ImageRemovedFromEventDomainEvent)
        var removeResult = @event.RemoveImage(request.ImageId);
        if (!removeResult.IsSuccess)
            return removeResult;

        // 3. Save changes (event handler will delete blob)
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
