using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.ValueObjects;

namespace LankaConnect.Application.Events.Commands.UpdateRegistrationDetails;

/// <summary>
/// Phase 6A.14: Handler for updating registration details
/// Orchestrates the update through the Event aggregate root
/// </summary>
public class UpdateRegistrationDetailsCommandHandler : ICommandHandler<UpdateRegistrationDetailsCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateRegistrationDetailsCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateRegistrationDetailsCommand request, CancellationToken cancellationToken)
    {
        // Get the event with registrations
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result.Failure($"Event with ID {request.EventId} not found");

        // Convert DTOs to domain value objects
        var attendeeResults = request.Attendees
            .Select(a => AttendeeDetails.Create(a.Name, a.Age))
            .ToList();

        // Check if any attendee creation failed
        var failedAttendee = attendeeResults.FirstOrDefault(r => r.IsFailure);
        if (failedAttendee != null)
            return Result.Failure(failedAttendee.Errors);

        var attendees = attendeeResults.Select(r => r.Value).ToList();

        // Create contact value object
        var contactResult = RegistrationContact.Create(
            request.Email,
            request.PhoneNumber,
            request.Address);

        if (contactResult.IsFailure)
            return Result.Failure(contactResult.Errors);

        // Update registration through the aggregate root
        var updateResult = @event.UpdateRegistrationDetails(
            request.UserId,
            attendees,
            contactResult.Value);

        if (updateResult.IsFailure)
            return updateResult;

        // Commit changes
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
