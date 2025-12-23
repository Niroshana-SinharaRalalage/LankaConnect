using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Application.Events.Commands.RegisterAnonymousAttendee;

/// <summary>
/// Phase 6A.44: Updated to support:
/// 1. Email validation - check if email belongs to existing member
/// 2. FREE events - complete registration immediately with confirmation email
/// 3. PAID events - create Stripe checkout session and return URL
/// </summary>
public class RegisterAnonymousAttendeeCommandHandler : ICommandHandler<RegisterAnonymousAttendeeCommand, string?>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;
    private readonly IStripePaymentService _stripePaymentService;

    public RegisterAnonymousAttendeeCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        IUserRepository userRepository,
        IStripePaymentService stripePaymentService)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
        _stripePaymentService = stripePaymentService;
    }

    public async Task<Result<string?>> Handle(RegisterAnonymousAttendeeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Phase 6A.44: Validate email and check if it belongs to existing member
            var emailResult = Email.Create(request.Email);
            if (emailResult.IsFailure)
                return Result<string?>.Failure(emailResult.Error);

            var emailExists = await _userRepository.ExistsWithEmailAsync(emailResult.Value, cancellationToken);
            if (emailExists)
            {
                return Result<string?>.Failure(
                    "This email is already registered as a member. Please log in to register for events.");
            }

            // Retrieve event
            var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
            if (@event == null)
                return Result<string?>.Failure("Event not found");

            // Session 21: Determine if using new multi-attendee format or legacy format
            if (request.Attendees != null && request.Attendees.Any())
            {
                // NEW FORMAT: Multiple attendees with names and ages
                return await HandleMultiAttendeeRegistration(@event, request, cancellationToken);
            }
            else
            {
                // LEGACY FORMAT: Single attendee with Name/Age/Address
                return await HandleLegacyRegistration(@event, request, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            // Phase 6A.44: Catch unhandled exceptions and return proper error response
            var errorMessage = $"Anonymous registration failed: {ex.GetType().Name}: {ex.Message}";
            Console.WriteLine($"🔴 [RegisterAnonymousAttendeeCommandHandler] EXCEPTION: {errorMessage}");
            return Result<string?>.Failure(errorMessage);
        }
    }

    /// <summary>
    /// Session 21: Handles new multi-attendee registration format
    /// Phase 6A.44: Integrated with Stripe payment for paid events
    /// </summary>
    private async Task<Result<string?>> HandleMultiAttendeeRegistration(
        Event @event,
        RegisterAnonymousAttendeeCommand request,
        CancellationToken cancellationToken)
    {
        // Create AttendeeDetails value objects from DTOs
        var attendeeDetailsList = new List<AttendeeDetails>();
        foreach (var attendeeDto in request.Attendees!)
        {
            var attendeeResult = AttendeeDetails.Create(attendeeDto.Name, attendeeDto.AgeCategory, attendeeDto.Gender);
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

        // Use new domain method to register multiple attendees
        var registerResult = @event.RegisterWithAttendees(
            userId: null, // Anonymous registration
            attendeeDetailsList,
            contactResult.Value
        );

        if (registerResult.IsFailure)
            return Result<string?>.Failure(registerResult.Error);

        // Explicitly mark event as modified for change tracking
        _eventRepository.Update(@event);

        // Get the just-created registration
        var registration = @event.Registrations.Last();

        // Phase 6A.44: Handle payment for paid events
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
                    { "anonymous", "true" },
                    { "email", request.Email }
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

            Console.WriteLine($"✅ [RegisterAnonymousAttendeeCommandHandler] Created Stripe checkout for paid event. RegistrationId: {registration.Id}");

            // Return checkout session URL for frontend to redirect
            return Result<string?>.Success(checkoutResult.Value);
        }

        // FREE event - save and return null (no payment needed)
        // Domain event will trigger confirmation email
        await _unitOfWork.CommitAsync(cancellationToken);

        Console.WriteLine($"✅ [RegisterAnonymousAttendeeCommandHandler] FREE event registration complete. RegistrationId: {registration.Id}");

        return Result<string?>.Success(null);
    }

    /// <summary>
    /// Session 20: Handles legacy single attendee registration format (backward compatibility)
    /// Phase 6A.44: Updated to support Stripe payment for paid events
    /// </summary>
    private async Task<Result<string?>> HandleLegacyRegistration(
        Event @event,
        RegisterAnonymousAttendeeCommand request,
        CancellationToken cancellationToken)
    {
        // Validate legacy format fields
        if (string.IsNullOrWhiteSpace(request.Name) || !request.Age.HasValue)
            return Result<string?>.Failure("Name and Age are required for registration");

        // Create AttendeeInfo value object (legacy)
        var attendeeInfoResult = AttendeeInfo.Create(
            request.Name,
            request.Age.Value,
            request.Address ?? string.Empty,
            request.Email,
            request.PhoneNumber
        );

        if (attendeeInfoResult.IsFailure)
            return Result<string?>.Failure(attendeeInfoResult.Error);

        // Use legacy domain method
        var registerResult = @event.RegisterAnonymous(attendeeInfoResult.Value, request.Quantity);
        if (registerResult.IsFailure)
            return Result<string?>.Failure(registerResult.Error);

        // Explicitly mark event as modified for change tracking
        _eventRepository.Update(@event);

        // Get the just-created registration
        var registration = @event.Registrations.Last();

        // Phase 6A.44: Handle payment for paid events
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
                    { "anonymous", "true" },
                    { "email", request.Email }
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

            Console.WriteLine($"✅ [RegisterAnonymousAttendeeCommandHandler] Created Stripe checkout for paid event (legacy). RegistrationId: {registration.Id}");

            // Return checkout session URL for frontend to redirect
            return Result<string?>.Success(checkoutResult.Value);
        }

        // FREE event - save and return null (no payment needed)
        await _unitOfWork.CommitAsync(cancellationToken);

        Console.WriteLine($"✅ [RegisterAnonymousAttendeeCommandHandler] FREE event registration complete (legacy). RegistrationId: {registration.Id}");

        return Result<string?>.Success(null);
    }
}
