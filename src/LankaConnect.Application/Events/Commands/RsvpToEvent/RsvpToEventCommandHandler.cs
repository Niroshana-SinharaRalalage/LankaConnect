using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.ValueObjects;

namespace LankaConnect.Application.Events.Commands.RsvpToEvent;

public class RsvpToEventCommandHandler : ICommandHandler<RsvpToEventCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RsvpToEventCommandHandler(IEventRepository eventRepository, IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RsvpToEventCommand request, CancellationToken cancellationToken)
    {
        // Retrieve event
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result.Failure("Event not found");

        // Session 21: Determine if using new multi-attendee format or legacy format
        if (request.Attendees != null && request.Attendees.Any())
        {
            // NEW FORMAT: Multiple attendees with names and ages
            return await HandleMultiAttendeeRsvp(@event, request, cancellationToken);
        }
        else
        {
            // LEGACY FORMAT: Simple quantity-based RSVP
            return await HandleLegacyRsvp(@event, request, cancellationToken);
        }
    }

    /// <summary>
    /// Session 21: Handles new multi-attendee RSVP format for authenticated users
    /// </summary>
    private async Task<Result> HandleMultiAttendeeRsvp(
        Event @event,
        RsvpToEventCommand request,
        CancellationToken cancellationToken)
    {
        // Validate that contact info is provided for new format
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.PhoneNumber))
            return Result.Failure("Email and phone number are required for multi-attendee registration");

        // Create AttendeeDetails value objects from DTOs
        var attendeeDetailsList = new List<AttendeeDetails>();
        foreach (var attendeeDto in request.Attendees!)
        {
            var attendeeResult = AttendeeDetails.Create(attendeeDto.Name, attendeeDto.Age);
            if (attendeeResult.IsFailure)
                return Result.Failure(attendeeResult.Error);

            attendeeDetailsList.Add(attendeeResult.Value);
        }

        // Create RegistrationContact value object
        var contactResult = RegistrationContact.Create(
            request.Email,
            request.PhoneNumber,
            request.Address
        );

        if (contactResult.IsFailure)
            return Result.Failure(contactResult.Error);

        // Use new domain method to register multiple attendees for authenticated user
        var registerResult = @event.RegisterWithAttendees(
            userId: request.UserId,
            attendeeDetailsList,
            contactResult.Value
        );

        if (registerResult.IsFailure)
            return registerResult;

        // Save changes (EF Core tracks changes automatically)
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }

    /// <summary>
    /// Handles legacy quantity-based RSVP format (backward compatibility)
    /// </summary>
    private async Task<Result> HandleLegacyRsvp(
        Event @event,
        RsvpToEventCommand request,
        CancellationToken cancellationToken)
    {
        // Use legacy domain method
        var registerResult = @event.Register(request.UserId, request.Quantity);
        if (registerResult.IsFailure)
            return registerResult;

        // Save changes (EF Core tracks changes automatically)
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
