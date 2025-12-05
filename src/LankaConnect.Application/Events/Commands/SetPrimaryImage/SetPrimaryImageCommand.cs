using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using MediatR;

namespace LankaConnect.Application.Events.Commands.SetPrimaryImage;

/// <summary>
/// Command to set an image as the primary/main thumbnail for an event
/// Phase 6A.13: Primary image selection feature
/// </summary>
public record SetPrimaryImageCommand : IRequest<Result>
{
    public Guid EventId { get; init; }
    public Guid ImageId { get; init; }
}

public class SetPrimaryImageCommandHandler : IRequestHandler<SetPrimaryImageCommand, Result>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetPrimaryImageCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SetPrimaryImageCommand request, CancellationToken cancellationToken)
    {
        // 1. Get event
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result.Failure($"Event with ID {request.EventId} not found");

        // 2. Set primary image (domain logic handles unmarking previous primary)
        var result = @event.SetPrimaryImage(request.ImageId);
        if (!result.IsSuccess)
            return result;

        // 3. Save changes
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
