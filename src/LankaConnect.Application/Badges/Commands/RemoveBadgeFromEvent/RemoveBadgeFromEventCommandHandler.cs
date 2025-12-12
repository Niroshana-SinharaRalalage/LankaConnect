using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using MediatR;

namespace LankaConnect.Application.Badges.Commands.RemoveBadgeFromEvent;

/// <summary>
/// Handler for RemoveBadgeFromEventCommand
/// Phase 6A.25: Removes a badge from an event
/// </summary>
public class RemoveBadgeFromEventCommandHandler : IRequestHandler<RemoveBadgeFromEventCommand, Result>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveBadgeFromEventCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RemoveBadgeFromEventCommand request, CancellationToken cancellationToken)
    {
        // 1. Get event
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result.Failure($"Event with ID {request.EventId} not found");

        // 2. Remove badge from event
        var removeResult = @event.RemoveBadge(request.BadgeId);
        if (!removeResult.IsSuccess)
            return removeResult;

        // 3. Save changes
        _eventRepository.Update(@event);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
