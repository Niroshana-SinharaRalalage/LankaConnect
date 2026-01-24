using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Services;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Users;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.RegisterAnonymousAttendee;

/// <summary>
/// Phase 6A.44: Updated to support:
/// 1. Email validation - check if email belongs to existing member
/// 2. FREE events - complete registration immediately with confirmation email
/// 3. PAID events - create Stripe checkout session and return URL
/// Phase 6A.X: Added revenue breakdown calculation for paid registrations
/// </summary>
public class RegisterAnonymousAttendeeCommandHandler : ICommandHandler<RegisterAnonymousAttendeeCommand, string?>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;
    private readonly IStripePaymentService _stripePaymentService;
    private readonly IRevenueCalculatorService _revenueCalculatorService;
    private readonly ILogger<RegisterAnonymousAttendeeCommandHandler> _logger;

    public RegisterAnonymousAttendeeCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        IUserRepository userRepository,
        IStripePaymentService stripePaymentService,
        IRevenueCalculatorService revenueCalculatorService,
        ILogger<RegisterAnonymousAttendeeCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
        _stripePaymentService = stripePaymentService;
        _revenueCalculatorService = revenueCalculatorService;
        _logger = logger;
    }

    public async Task<Result<string?>> Handle(RegisterAnonymousAttendeeCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "RegisterAnonymousAttendee"))
        using (LogContext.PushProperty("EntityType", "Registration"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("Email", request.Email))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "RegisterAnonymousAttendee START: EventId={EventId}, Email={Email}, AttendeeCount={AttendeeCount}",
                request.EventId, request.Email, request.Attendees?.Count ?? request.Quantity);

            try
            {
                // Check for cancellation at the start
                cancellationToken.ThrowIfCancellationRequested();

                // Phase 6A.44: Validate email and check if it belongs to existing member
                var emailResult = Email.Create(request.Email);
                if (emailResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "RegisterAnonymousAttendee FAILED: Invalid email - EventId={EventId}, Email={Email}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, request.Email, emailResult.Error, stopwatch.ElapsedMilliseconds);

                    return Result<string?>.Failure(emailResult.Error);
                }

                _logger.LogInformation(
                    "RegisterAnonymousAttendee: Email validated - Email={Email}",
                    request.Email);

                var emailExists = await _userRepository.ExistsWithEmailAsync(emailResult.Value, cancellationToken);
                if (emailExists)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "RegisterAnonymousAttendee FAILED: Email belongs to existing member - EventId={EventId}, Email={Email}, Duration={ElapsedMs}ms",
                        request.EventId, request.Email, stopwatch.ElapsedMilliseconds);

                    return Result<string?>.Failure(
                        "This email is already registered as a member. Please log in to register for events.");
                }

                _logger.LogInformation(
                    "RegisterAnonymousAttendee: Email not found in user database - proceeding with anonymous registration - Email={Email}",
                    request.Email);

                // Retrieve event with registrations
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "RegisterAnonymousAttendee FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result<string?>.Failure("Event not found");
                }

                _logger.LogInformation(
                    "RegisterAnonymousAttendee: Event loaded - EventId={EventId}, Title={Title}, IsFree={IsFree}, Status={Status}",
                    @event.Id, @event.Title.Value, @event.IsFree(), @event.Status);

                // Phase 6A.44: Check if this email is already registered for the event (anonymous registration)
                var existingAnonymousRegistration = @event.Registrations
                    .Where(r => r.UserId == null) // Only anonymous registrations
                    .Where(r => r.Status != RegistrationStatus.Cancelled && r.Status != RegistrationStatus.Refunded)
                    .FirstOrDefault(r =>
                        (r.Contact != null && r.Contact.Email == request.Email) ||
                        (r.AttendeeInfo != null && r.AttendeeInfo.Email.Value == request.Email));

                if (existingAnonymousRegistration != null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "RegisterAnonymousAttendee FAILED: Email already registered - EventId={EventId}, Email={Email}, ExistingRegistrationId={RegistrationId}, Duration={ElapsedMs}ms",
                        request.EventId, request.Email, existingAnonymousRegistration.Id, stopwatch.ElapsedMilliseconds);

                    return Result<string?>.Failure(
                        "This email is already registered for this event. Each email can only register once.");
                }

                _logger.LogInformation(
                    "RegisterAnonymousAttendee: Email not found in event registrations - proceeding - EventId={EventId}, Email={Email}",
                    request.EventId, request.Email);

                // Session 21: Determine if using new multi-attendee format or legacy format
                if (request.Attendees != null && request.Attendees.Any())
                {
                    _logger.LogInformation(
                        "RegisterAnonymousAttendee: Using multi-attendee format - EventId={EventId}, AttendeeCount={AttendeeCount}",
                        request.EventId, request.Attendees.Count);

                    // NEW FORMAT: Multiple attendees with names and ages
                    return await HandleMultiAttendeeRegistration(@event, request, cancellationToken);
                }
                else
                {
                    _logger.LogInformation(
                        "RegisterAnonymousAttendee: Using legacy format - EventId={EventId}, Quantity={Quantity}",
                        request.EventId, request.Quantity);

                    // LEGACY FORMAT: Single attendee with Name/Age/Address
                    return await HandleLegacyRegistration(@event, request, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();

                _logger.LogWarning(
                    "RegisterAnonymousAttendee CANCELLED: Operation was cancelled - EventId={EventId}, Email={Email}, Duration={ElapsedMs}ms",
                    request.EventId, request.Email, stopwatch.ElapsedMilliseconds);

                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "RegisterAnonymousAttendee FAILED: Exception occurred - EventId={EventId}, Email={Email}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.Email, stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
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
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "HandleMultiAttendeeRegistration: Creating attendee value objects - EventId={EventId}, AttendeeCount={AttendeeCount}",
            @event.Id, request.Attendees!.Count);

        // Create AttendeeDetails value objects from DTOs
        var attendeeDetailsList = new List<AttendeeDetails>();
        foreach (var attendeeDto in request.Attendees!)
        {
            var attendeeResult = AttendeeDetails.Create(attendeeDto.Name, attendeeDto.AgeCategory, attendeeDto.Gender);
            if (attendeeResult.IsFailure)
            {
                stopwatch.Stop();

                _logger.LogWarning(
                    "HandleMultiAttendeeRegistration FAILED: Invalid attendee details - EventId={EventId}, AttendeeName={Name}, Error={Error}, Duration={ElapsedMs}ms",
                    @event.Id, attendeeDto.Name, attendeeResult.Error, stopwatch.ElapsedMilliseconds);

                return Result<string?>.Failure(attendeeResult.Error);
            }

            attendeeDetailsList.Add(attendeeResult.Value);
        }

        _logger.LogInformation(
            "HandleMultiAttendeeRegistration: Attendee value objects created - EventId={EventId}, Count={Count}",
            @event.Id, attendeeDetailsList.Count);

        // Create RegistrationContact value object
        var contactResult = RegistrationContact.Create(
            request.Email,
            request.PhoneNumber,
            request.Address
        );

        if (contactResult.IsFailure)
        {
            stopwatch.Stop();

            _logger.LogWarning(
                "HandleMultiAttendeeRegistration FAILED: Invalid contact details - EventId={EventId}, Email={Email}, Error={Error}, Duration={ElapsedMs}ms",
                @event.Id, request.Email, contactResult.Error, stopwatch.ElapsedMilliseconds);

            return Result<string?>.Failure(contactResult.Error);
        }

        _logger.LogInformation(
            "HandleMultiAttendeeRegistration: Contact value object created - EventId={EventId}, Email={Email}",
            @event.Id, request.Email);

        // Use new domain method to register multiple attendees
        var registerResult = @event.RegisterWithAttendees(
            userId: null, // Anonymous registration
            attendeeDetailsList,
            contactResult.Value
        );

        if (registerResult.IsFailure)
        {
            stopwatch.Stop();

            _logger.LogWarning(
                "HandleMultiAttendeeRegistration FAILED: Domain validation failed - EventId={EventId}, Error={Error}, Duration={ElapsedMs}ms",
                @event.Id, registerResult.Error, stopwatch.ElapsedMilliseconds);

            return Result<string?>.Failure(registerResult.Error);
        }

        _logger.LogInformation(
            "HandleMultiAttendeeRegistration: Domain method succeeded - EventId={EventId}, AttendeeCount={AttendeeCount}",
            @event.Id, attendeeDetailsList.Count);

        // Explicitly mark event as modified for change tracking
        _eventRepository.Update(@event);

        // Get the just-created registration
        var registration = @event.Registrations.Last();

        // Phase 6A.81: Log registration state for observability
        _logger.LogInformation(
            "HandleMultiAttendeeRegistration: Registration created - RegistrationId={RegistrationId}, Status={Status}, PaymentStatus={PaymentStatus}, IsPaidEvent={IsPaidEvent}, ExpiresAt={ExpiresAt}",
            registration.Id,
            registration.Status,
            registration.PaymentStatus,
            !@event.IsFree(),
            registration.CheckoutSessionExpiresAt?.ToString("o") ?? "null");

        // Phase 6A.X: Calculate and store revenue breakdown for paid events
        if (!@event.IsFree() && registration.TotalPrice != null && registration.TotalPrice.Amount > 0)
        {
            try
            {
                _logger.LogInformation(
                    "Calculating revenue breakdown for registration {RegistrationId} (Anonymous): Price={Price}, Event={EventId}, Location={Location}",
                    registration.Id,
                    registration.TotalPrice.Amount,
                    @event.Id,
                    @event.Location?.ToString() ?? "null");

                var breakdownResult = await _revenueCalculatorService.CalculateBreakdownAsync(
                    registration.TotalPrice,
                    @event.Location,
                    cancellationToken);

                if (breakdownResult.IsSuccess)
                {
                    registration.SetRevenueBreakdown(breakdownResult.Value);
                    _logger.LogInformation(
                        "Revenue breakdown calculated successfully for registration {RegistrationId}: Tax={Tax}, StripeFee={StripeFee}, Commission={Commission}, Payout={Payout}",
                        registration.Id,
                        breakdownResult.Value.SalesTaxAmount.Amount,
                        breakdownResult.Value.StripeFeeAmount.Amount,
                        breakdownResult.Value.PlatformCommission.Amount,
                        breakdownResult.Value.OrganizerPayout.Amount);
                }
                else
                {
                    _logger.LogWarning(
                        "Revenue breakdown calculation failed for registration {RegistrationId}: {Error}",
                        registration.Id,
                        breakdownResult.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Exception while calculating revenue breakdown for registration {RegistrationId}. Registration will continue without breakdown.",
                    registration.Id);
            }
        }

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

            stopwatch.Stop();

            _logger.LogInformation(
                "HandleMultiAttendeeRegistration COMPLETE (PAID): EventId={EventId}, RegistrationId={RegistrationId}, CheckoutSessionId={SessionId}, Amount={Amount}, Duration={ElapsedMs}ms",
                @event.Id, registration.Id, checkoutResult.Value, registration.TotalPrice!.Amount, stopwatch.ElapsedMilliseconds);

            // Return checkout session URL for frontend to redirect
            return Result<string?>.Success(checkoutResult.Value);
        }

        // FREE event - save and return null (no payment needed)
        // Domain event will trigger confirmation email
        await _unitOfWork.CommitAsync(cancellationToken);

        stopwatch.Stop();

        _logger.LogInformation(
            "HandleMultiAttendeeRegistration COMPLETE (FREE): EventId={EventId}, RegistrationId={RegistrationId}, AttendeeCount={AttendeeCount}, Duration={ElapsedMs}ms",
            @event.Id, registration.Id, attendeeDetailsList.Count, stopwatch.ElapsedMilliseconds);

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
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation(
            "HandleLegacyRegistration: Validating legacy format - EventId={EventId}, Name={Name}, Age={Age}, Quantity={Quantity}",
            @event.Id, request.Name ?? "null", request.Age ?? 0, request.Quantity);

        // Validate legacy format fields
        if (string.IsNullOrWhiteSpace(request.Name) || !request.Age.HasValue)
        {
            stopwatch.Stop();

            _logger.LogWarning(
                "HandleLegacyRegistration FAILED: Missing required fields - EventId={EventId}, Name={Name}, Age={Age}, Duration={ElapsedMs}ms",
                @event.Id, request.Name ?? "null", request.Age ?? 0, stopwatch.ElapsedMilliseconds);

            return Result<string?>.Failure("Name and Age are required for registration");
        }

        // Create AttendeeInfo value object (legacy)
        var attendeeInfoResult = AttendeeInfo.Create(
            request.Name,
            request.Age.Value,
            request.Address ?? string.Empty,
            request.Email,
            request.PhoneNumber
        );

        if (attendeeInfoResult.IsFailure)
        {
            stopwatch.Stop();

            _logger.LogWarning(
                "HandleLegacyRegistration FAILED: Invalid attendee info - EventId={EventId}, Name={Name}, Error={Error}, Duration={ElapsedMs}ms",
                @event.Id, request.Name, attendeeInfoResult.Error, stopwatch.ElapsedMilliseconds);

            return Result<string?>.Failure(attendeeInfoResult.Error);
        }

        _logger.LogInformation(
            "HandleLegacyRegistration: AttendeeInfo value object created - EventId={EventId}, Name={Name}",
            @event.Id, request.Name);

        // Use legacy domain method
        var registerResult = @event.RegisterAnonymous(attendeeInfoResult.Value, request.Quantity);
        if (registerResult.IsFailure)
        {
            stopwatch.Stop();

            _logger.LogWarning(
                "HandleLegacyRegistration FAILED: Domain validation failed - EventId={EventId}, Error={Error}, Duration={ElapsedMs}ms",
                @event.Id, registerResult.Error, stopwatch.ElapsedMilliseconds);

            return Result<string?>.Failure(registerResult.Error);
        }

        _logger.LogInformation(
            "HandleLegacyRegistration: Domain method succeeded - EventId={EventId}, Quantity={Quantity}",
            @event.Id, request.Quantity);

        // Explicitly mark event as modified for change tracking
        _eventRepository.Update(@event);

        // Get the just-created registration
        var registration = @event.Registrations.Last();

        // Phase 6A.X: Calculate and store revenue breakdown for paid events
        if (!@event.IsFree() && registration.TotalPrice != null && registration.TotalPrice.Amount > 0)
        {
            try
            {
                _logger.LogInformation(
                    "Calculating revenue breakdown for registration {RegistrationId} (Anonymous): Price={Price}, Event={EventId}, Location={Location}",
                    registration.Id,
                    registration.TotalPrice.Amount,
                    @event.Id,
                    @event.Location?.ToString() ?? "null");

                var breakdownResult = await _revenueCalculatorService.CalculateBreakdownAsync(
                    registration.TotalPrice,
                    @event.Location,
                    cancellationToken);

                if (breakdownResult.IsSuccess)
                {
                    registration.SetRevenueBreakdown(breakdownResult.Value);
                    _logger.LogInformation(
                        "Revenue breakdown calculated successfully for registration {RegistrationId}: Tax={Tax}, StripeFee={StripeFee}, Commission={Commission}, Payout={Payout}",
                        registration.Id,
                        breakdownResult.Value.SalesTaxAmount.Amount,
                        breakdownResult.Value.StripeFeeAmount.Amount,
                        breakdownResult.Value.PlatformCommission.Amount,
                        breakdownResult.Value.OrganizerPayout.Amount);
                }
                else
                {
                    _logger.LogWarning(
                        "Revenue breakdown calculation failed for registration {RegistrationId}: {Error}",
                        registration.Id,
                        breakdownResult.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Exception while calculating revenue breakdown for registration {RegistrationId}. Registration will continue without breakdown.",
                    registration.Id);
            }
        }

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

            stopwatch.Stop();

            _logger.LogInformation(
                "HandleLegacyRegistration COMPLETE (PAID): EventId={EventId}, RegistrationId={RegistrationId}, CheckoutSessionId={SessionId}, Amount={Amount}, Quantity={Quantity}, Duration={ElapsedMs}ms",
                @event.Id, registration.Id, checkoutResult.Value, registration.TotalPrice!.Amount, request.Quantity, stopwatch.ElapsedMilliseconds);

            // Return checkout session URL for frontend to redirect
            return Result<string?>.Success(checkoutResult.Value);
        }

        // FREE event - save and return null (no payment needed)
        await _unitOfWork.CommitAsync(cancellationToken);

        stopwatch.Stop();

        _logger.LogInformation(
            "HandleLegacyRegistration COMPLETE (FREE): EventId={EventId}, RegistrationId={RegistrationId}, Quantity={Quantity}, Duration={ElapsedMs}ms",
            @event.Id, registration.Id, request.Quantity, stopwatch.ElapsedMilliseconds);

        return Result<string?>.Success(null);
    }
}
