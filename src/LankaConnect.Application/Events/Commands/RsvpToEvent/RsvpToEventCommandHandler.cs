using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Services;
using LankaConnect.Domain.Events.ValueObjects;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.RsvpToEvent;

// Session 23: Updated to support Stripe payment integration for paid events
// Phase 6A.X: Added revenue breakdown calculation for paid registrations
public class RsvpToEventCommandHandler : ICommandHandler<RsvpToEventCommand, string?>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStripePaymentService _stripePaymentService;
    private readonly IRevenueCalculatorService _revenueCalculatorService;
    private readonly ILogger<RsvpToEventCommandHandler> _logger;

    public RsvpToEventCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        IStripePaymentService stripePaymentService,
        IRevenueCalculatorService revenueCalculatorService,
        ILogger<RsvpToEventCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _stripePaymentService = stripePaymentService;
        _revenueCalculatorService = revenueCalculatorService;
        _logger = logger;
    }

    public async Task<Result<string?>> Handle(RsvpToEventCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "RsvpToEvent"))
        using (LogContext.PushProperty("EntityType", "Event"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            var isMultiAttendee = request.Attendees != null && request.Attendees.Any();
            _logger.LogInformation(
                "RsvpToEvent START: EventId={EventId}, UserId={UserId}, IsMultiAttendee={IsMultiAttendee}, AttendeesCount={AttendeesCount}",
                request.EventId, request.UserId, isMultiAttendee, request.Attendees?.Count ?? 0);

            try
            {
                // Retrieve event
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "RsvpToEvent FAILED: Event not found - EventId={EventId}, UserId={UserId}, Duration={ElapsedMs}ms",
                        request.EventId, request.UserId, stopwatch.ElapsedMilliseconds);

                    return Result<string?>.Failure("Event not found");
                }

                _logger.LogInformation(
                    "RsvpToEvent: Event loaded - EventId={EventId}, Title={Title}, HasPricing={HasPricing}, CurrentRegistrations={CurrentRegistrations}",
                    @event.Id, @event.Title.Value, @event.Pricing != null, @event.CurrentRegistrations);

                Result<string?> result;

                // Session 21: Determine if using new multi-attendee format or legacy format
                if (isMultiAttendee)
                {
                    _logger.LogInformation(
                        "RsvpToEvent: Using multi-attendee format - EventId={EventId}, AttendeesCount={Count}",
                        request.EventId, request.Attendees!.Count);

                    // NEW FORMAT: Multiple attendees with names and ages
                    result = await HandleMultiAttendeeRsvp(@event, request, cancellationToken);
                }
                else
                {
                    _logger.LogInformation(
                        "RsvpToEvent: Using legacy format - EventId={EventId}",
                        request.EventId);

                    // LEGACY FORMAT: Simple quantity-based RSVP
                    result = await HandleLegacyRsvp(@event, request, cancellationToken);
                }

                stopwatch.Stop();

                if (result.IsSuccess)
                {
                    _logger.LogInformation(
                        "RsvpToEvent COMPLETE: EventId={EventId}, UserId={UserId}, SessionUrl={HasSessionUrl}, Duration={ElapsedMs}ms",
                        request.EventId, request.UserId, result.Value != null, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogWarning(
                        "RsvpToEvent FAILED: EventId={EventId}, UserId={UserId}, Errors={Errors}, Duration={ElapsedMs}ms",
                        request.EventId, request.UserId, string.Join(", ", result.Errors), stopwatch.ElapsedMilliseconds);
                }

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                // Phase 6A.10: Catch unhandled exceptions and return proper error response
                // This prevents empty HTTP 500 responses and provides meaningful error details
                var errorMessage = $"Registration failed: {ex.GetType().Name}: {ex.Message}";

                _logger.LogError(ex,
                    "RsvpToEvent FAILED: Exception occurred - EventId={EventId}, UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.UserId, stopwatch.ElapsedMilliseconds, ex.Message);

                return Result<string?>.Failure(errorMessage);
            }
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
        // Phase 6A.43: Updated to use AgeCategory instead of Age
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

        // Use new domain method to register multiple attendees for authenticated user
        var registerResult = @event.RegisterWithAttendees(
            userId: request.UserId,
            attendeeDetailsList,
            contactResult.Value
        );

        if (registerResult.IsFailure)
            return Result<string?>.Failure(registerResult.Error);

        // DEFENSIVE FIX Phase 6A.24: Explicitly mark event as modified for change tracking
        _eventRepository.Update(@event);

        // Session 23: Handle payment for paid events
        var registration = @event.Registrations.Last();  // Get the just-created registration

        // Phase 6A.X: Calculate and store revenue breakdown for paid events
        if (!@event.IsFree() && registration.TotalPrice != null && registration.TotalPrice.Amount > 0)
        {
            try
            {
                _logger.LogInformation(
                    "Calculating revenue breakdown for registration {RegistrationId} (RSVP): Price={Price}, Event={EventId}, Location={Location}",
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

        // DEFENSIVE FIX Phase 6A.24: Explicitly mark event as modified for change tracking
        _eventRepository.Update(@event);

        // Save changes (EF Core tracks changes automatically)
        await _unitOfWork.CommitAsync(cancellationToken);

        // Legacy format always returns null (no payment support)
        return Result<string?>.Success(null);
    }
}
