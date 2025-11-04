using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using MediatR;

namespace LankaConnect.Application.Events.Commands.DeleteEventVideo;

/// <summary>
/// Command to delete a video from an event's gallery
/// Removes video from Event aggregate and deletes video + thumbnail from Azure Blob Storage via domain event handler
/// </summary>
public record DeleteEventVideoCommand : IRequest<Result>
{
    public Guid EventId { get; init; }
    public Guid VideoId { get; init; }
}

public class DeleteEventVideoCommandHandler : IRequestHandler<DeleteEventVideoCommand, Result>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteEventVideoCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteEventVideoCommand request, CancellationToken cancellationToken)
    {
        // 1. Get event
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result.Failure($"Event with ID {request.EventId} not found");

        // 2. Remove video from Event aggregate (raises VideoRemovedFromEventDomainEvent with blob names)
        var removeResult = @event.RemoveVideo(request.VideoId);
        if (!removeResult.IsSuccess)
            return removeResult;

        // 3. Save changes (domain event handler will delete both video and thumbnail blobs)
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
