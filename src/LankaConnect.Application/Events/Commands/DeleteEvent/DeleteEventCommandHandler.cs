using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Application.Events.Commands.DeleteEvent;

public class DeleteEventCommandHandler : ICommandHandler<DeleteEventCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteEventCommandHandler(IEventRepository eventRepository, IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteEventCommand request, CancellationToken cancellationToken)
    {
        // Retrieve event
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result.Failure("Event not found");

        // Phase 6A.59: Security fix - verify event owner
        if (@event.OrganizerId != request.UserId)
            return Result.Failure("Only the event organizer can delete this event");

        // Business rule: Only draft or cancelled events can be deleted
        if (@event.Status != EventStatus.Draft && @event.Status != EventStatus.Cancelled)
            return Result.Failure("Only draft or cancelled events can be deleted");

        // Business rule: Cannot delete events with registrations
        if (@event.CurrentRegistrations > 0)
            return Result.Failure("Cannot delete events with existing registrations");

        // Delete the event using repository method
        await _eventRepository.DeleteAsync(request.EventId, cancellationToken);

        // Save changes
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
