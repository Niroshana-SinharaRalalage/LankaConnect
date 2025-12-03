using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.ValueObjects;

namespace LankaConnect.Application.Events.Commands.RsvpToEvent;

// Session 23: Updated to support Stripe payment integration for paid events
public class RsvpToEventCommandHandler : ICommandHandler<RsvpToEventCommand, string?>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStripePaymentService _stripePaymentService;

    public RsvpToEventCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        IStripePaymentService stripePaymentService)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _stripePaymentService = stripePaymentService;
    }

    public async Task<Result<string?>> Handle(RsvpToEventCommand request, CancellationToken cancellationToken)
    {
        // Retrieve event
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result<string?>.Failure("Event not found");

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
    /// Session 23: Integrated with Stripe payment for paid events
    /// </summary>
    private async Task<Result<string?>> HandleMultiAttendeeRsvp(
        Event @event,
        RsvpToEventCommand request,
        CancellationToken cancellationToken)
    {
        // Validate that contact info is provided for new format
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.PhoneNumber))
            return Result<string?>.Failure("Email and phone number are required for multi-attendee registration");

        // Create AttendeeDetails value objects from DTOs
        var attendeeDetailsList = new List<AttendeeDetails>();
        foreach (var attendeeDto in request.Attendees!)
        {
            var attendeeResult = AttendeeDetails.Create(attendeeDto.Name, attendeeDto.Age);
            if (attendeeResult.IsFailure)
                return Result<string?>.Failure(attendeeResult.Error);

            attendeeDetailsList.Add(attendeeResult.Value);
        }

        // Create RegistrationContact value object
        var contactResult = RegistrationContact.Create(
            request.Email,
            request.PhoneNumber,
            request.Address
        );

        if (contactResult.IsFailure)
            return Result<string?>.Failure(contactResult.Error);

        // Use new domain method to register multiple attendees for authenticated user
        var registerResult = @event.RegisterWithAttendees(
            userId: request.UserId,
            attendeeDetailsList,
            contactResult.Value
        );

        if (registerResult.IsFailure)
            return Result<string?>.Failure(registerResult.Error);

        // Session 23: Handle payment for paid events
        var registration = @event.Registrations.Last();  // Get the just-created registration

        // Check if event requires payment
        if (!@event.IsFree())
        {
            // Validate URLs are provided for paid events
            if (string.IsNullOrWhiteSpace(request.SuccessUrl) || string.IsNullOrWhiteSpace(request.CancelUrl))
                return Result<string?>.Failure("Success and Cancel URLs are required for paid events");

            // Create Stripe Checkout session
            var checkoutRequest = new CreateEventCheckoutSessionRequest
            {
                EventId = @event.Id,
                RegistrationId = registration.Id,
                EventTitle = @event.Title.Value,
                Amount = registration.TotalPrice!.Amount,
                Currency = registration.TotalPrice.Currency.ToString(),
                SuccessUrl = request.SuccessUrl,
                CancelUrl = request.CancelUrl,
                Metadata = new Dictionary<string, string>
                {
                    { "event_id", @event.Id.ToString() },
                    { "registration_id", registration.Id.ToString() },
                    { "user_id", request.UserId.ToString() }
                }
            };

            var checkoutResult = await _stripePaymentService.CreateEventCheckoutSessionAsync(checkoutRequest, cancellationToken);
            if (checkoutResult.IsFailure)
                return Result<string?>.Failure($"Failed to create payment session: {checkoutResult.Error}");

            // Set checkout session ID on registration
            var setSessionResult = registration.SetStripeCheckoutSession(checkoutResult.Value);
            if (setSessionResult.IsFailure)
                return Result<string?>.Failure(setSessionResult.Error);

            // Save changes with checkout session ID
            await _unitOfWork.CommitAsync(cancellationToken);

            // Return checkout session URL for frontend to redirect
            return Result<string?>.Success(checkoutResult.Value);
        }

        // Free event - save and return null (no payment needed)
        await _unitOfWork.CommitAsync(cancellationToken);
        return Result<string?>.Success(null);
    }

    /// <summary>
    /// Handles legacy quantity-based RSVP format (backward compatibility)
    /// Session 23: Legacy format always returns null (no payment support in legacy mode)
    /// </summary>
    private async Task<Result<string?>> HandleLegacyRsvp(
        Event @event,
        RsvpToEventCommand request,
        CancellationToken cancellationToken)
    {
        // Use legacy domain method
        var registerResult = @event.Register(request.UserId, request.Quantity);
        if (registerResult.IsFailure)
            return Result<string?>.Failure(registerResult.Error);

        // Save changes (EF Core tracks changes automatically)
        await _unitOfWork.CommitAsync(cancellationToken);

        // Legacy format always returns null (no payment support)
        return Result<string?>.Success(null);
    }
}
