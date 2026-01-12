using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;

namespace LankaConnect.Application.Events.Commands.UpdateEventOrganizerContact;

public class UpdateEventOrganizerContactCommandHandler : ICommandHandler<UpdateEventOrganizerContactCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateEventOrganizerContactCommandHandler(IEventRepository eventRepository, IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateEventOrganizerContactCommand request, CancellationToken cancellationToken)
    {
        // Retrieve event
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result.Failure("Event not found");

        // Call domain method to set organizer contact details
        var setContactResult = @event.SetOrganizerContactDetails(
            request.PublishOrganizerContact,
            request.OrganizerContactName,
            request.OrganizerContactPhone,
            request.OrganizerContactEmail
        );

        if (setContactResult.IsFailure)
            return setContactResult;

        // Save changes (EF Core tracks changes automatically)
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
