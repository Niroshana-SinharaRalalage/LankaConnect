using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Domain.Events.Services;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.RsvpToEvent;

// Session 23: Updated to support Stripe payment integration for paid events
// Phase 6A.X: Added revenue breakdown calculation for paid registrations
public class RsvpToEventCommandHandler : ICommandHandler<RsvpToEventCommand, string?>
{
    private readonly IEventRepository _eventRepository;
    private readonly IDonationRepository _donationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStripePaymentService _stripePaymentService;
    private readonly IRevenueCalculatorService _revenueCalculatorService;
    private readonly ILogger<RsvpToEventCommandHandler> _logger;

    public RsvpToEventCommandHandler(
        IEventRepository eventRepository,
        IDonationRepository donationRepository,
        IUnitOfWork unitOfWork,
        IStripePaymentService stripePaymentService,
        IRevenueCalculatorService revenueCalculatorService,
        ILogger<RsvpToEventCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _donationRepository = donationRepository;
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

        // Phase 6A.81 Part 4 FIX: Check for existing Preliminary registration to prevent duplicates
        // If user already has a Preliminary registration (payment not completed), reuse it
        var existingPreliminary = @event.Registrations.FirstOrDefault(r =>
            r.UserId == request.UserId &&
            r.Status == Domain.Events.Enums.RegistrationStatus.Preliminary);

        if (existingPreliminary != null && !@event.IsFree())
        {
            _logger.LogInformation(
                "Found existing Preliminary registration - RegistrationId={RegistrationId}, UserId={UserId}, EventId={EventId}. Retrieving existing checkout URL.",
                existingPreliminary.Id, request.UserId, @event.Id);

            // Retrieve existing Stripe checkout URL (Phase 6A.81 Part 2)
            if (string.IsNullOrEmpty(existingPreliminary.StripeCheckoutSessionId))
            {
                _logger.LogError(
                    "Existing Preliminary registration missing checkout session ID - RegistrationId={RegistrationId}",
                    existingPreliminary.Id);
                return Result<string?>.Failure("Existing registration is in invalid state. Please contact support.");
            }

            var checkoutUrlResult = await _stripePaymentService.GetCheckoutSessionUrlAsync(
                existingPreliminary.StripeCheckoutSessionId,
                cancellationToken);

            if (checkoutUrlResult.IsFailure)
            {
                _logger.LogError(
                    "Failed to retrieve checkout URL for existing Preliminary registration - RegistrationId={RegistrationId}, Error={Error}",
                    existingPreliminary.Id, checkoutUrlResult.Error);
                return Result<string?>.Failure($"Failed to retrieve payment link: {checkoutUrlResult.Error}");
            }

            _logger.LogInformation(
                "Successfully retrieved existing checkout URL - RegistrationId={RegistrationId}, URL exists={HasUrl}",
                existingPreliminary.Id, !string.IsNullOrEmpty(checkoutUrlResult.Value));

            return Result<string?>.Success(checkoutUrlResult.Value);
        }

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

        // Phase 6A.81: Log registration state for observability
        _logger.LogInformation(
            "HandleMultiAttendeeRsvp: Registration created - RegistrationId={RegistrationId}, Status={Status}, PaymentStatus={PaymentStatus}, IsPaidEvent={IsPaidEvent}, ExpiresAt={ExpiresAt}",
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

        // Donation Feature: Handle optional donation during registration
        // C2 Guard: Donation in isolated try-catch, registration succeeds even if donation fails
        // C3 Guard: Check > 0, not just HasValue. Treat 0 same as null.
        Donation? bundledDonation = null;
        if (request.DonationAmount.HasValue && request.DonationAmount.Value > 0 && @event.AreDonationsEnabled())
        {
            try
            {
                bundledDonation = await HandleDonationDuringRegistration(
                    @event, registration, request.UserId, request.Email!,
                    request.DonationAmount.Value, request.DonorName,
                    request.DonorPhone, request.DonorNotes,
                    request.Attendees?.FirstOrDefault()?.Name,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "HandleMultiAttendeeRsvp: Donation processing failed - EventId={EventId}, RegistrationId={RegistrationId}. Registration will continue without donation.",
                    @event.Id, registration.Id);
                bundledDonation = null;
            }
        }

        // Check if event requires payment
        if (!@event.IsFree())
        {
            // Validate URLs are provided for paid events
            if (string.IsNullOrWhiteSpace(request.SuccessUrl) || string.IsNullOrWhiteSpace(request.CancelUrl))
                return Result<string?>.Failure("Success and Cancel URLs are required for paid events");

            // Build checkout request
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

            // Combined checkout: Add donation as second line item
            // C1 Guard: When LineItems is null, existing single-item behavior preserved
            if (bundledDonation != null)
            {
                var currency = registration.TotalPrice.Currency.ToString();
                checkoutRequest.LineItems = new List<CheckoutLineItem>
                {
                    new CheckoutLineItem
                    {
                        Name = $"Event Registration: {@event.Title.Value}",
                        Description = $"Registration for {registration.Attendees?.Count ?? 1} attendee(s)",
                        Amount = registration.TotalPrice.Amount,
                        Currency = currency
                    },
                    new CheckoutLineItem
                    {
                        Name = $"Donation: {@event.Title.Value}",
                        Description = "Voluntary donation",
                        Amount = bundledDonation.Amount.Amount,
                        Currency = currency
                    }
                };
                checkoutRequest.Amount = registration.TotalPrice.Amount + bundledDonation.Amount.Amount;
                checkoutRequest.Metadata["donation_id"] = bundledDonation.Id.ToString();
            }

            var checkoutResult = await _stripePaymentService.CreateEventCheckoutSessionAsync(checkoutRequest, cancellationToken);
            if (checkoutResult.IsFailure)
                return Result<string?>.Failure($"Failed to create payment session: {checkoutResult.Error}");

            // Set checkout session ID on registration
            var setSessionResult = registration.SetStripeCheckoutSession(checkoutResult.Value);
            if (setSessionResult.IsFailure)
                return Result<string?>.Failure(setSessionResult.Error);

            // Set same checkout session on bundled donation (shared Stripe session)
            if (bundledDonation != null)
            {
                var donationSessionResult = bundledDonation.SetStripeCheckoutSession(
                    checkoutResult.Value,
                    DateTime.UtcNow.AddHours(24));

                if (donationSessionResult.IsFailure)
                {
                    _logger.LogWarning(
                        "HandleMultiAttendeeRsvp: Failed to set checkout session on bundled donation - DonationId={DonationId}, Error={Error}",
                        bundledDonation.Id, donationSessionResult.Error);
                }

                await _donationRepository.AddAsync(bundledDonation, cancellationToken);
            }

            // Save changes with checkout session ID
            await _unitOfWork.CommitAsync(cancellationToken);

            // Return checkout session URL for frontend to redirect
            return Result<string?>.Success(checkoutResult.Value);
        }

        // Free event handling
        if (bundledDonation != null)
        {
            // Free event + donation: Create standalone donation checkout
            if (!string.IsNullOrWhiteSpace(request.SuccessUrl) && !string.IsNullOrWhiteSpace(request.CancelUrl))
            {
                var donationCheckoutRequest = new CreateDonationCheckoutSessionRequest
                {
                    EventId = @event.Id,
                    DonationId = bundledDonation.Id,
                    EventTitle = @event.Title.Value,
                    Amount = bundledDonation.Amount.Amount,
                    Currency = bundledDonation.Amount.Currency.ToString(),
                    SuccessUrl = request.SuccessUrl,
                    CancelUrl = request.CancelUrl,
                    Metadata = new Dictionary<string, string>
                    {
                        { "payment_type", "donation" },
                        { "event_id", @event.Id.ToString() },
                        { "donation_id", bundledDonation.Id.ToString() },
                        { "registration_id", registration.Id.ToString() },
                        { "donor_user_id", request.UserId.ToString() }
                    }
                };

                var donationCheckoutResult = await _stripePaymentService.CreateDonationCheckoutSessionAsync(
                    donationCheckoutRequest, cancellationToken);

                if (donationCheckoutResult.IsSuccess)
                {
                    bundledDonation.SetStripeCheckoutSession(
                        donationCheckoutResult.Value.SessionId,
                        donationCheckoutResult.Value.ExpiresAt);

                    await _donationRepository.AddAsync(bundledDonation, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    _logger.LogInformation(
                        "HandleMultiAttendeeRsvp: Free event + donation checkout created - DonationId={DonationId}",
                        bundledDonation.Id);

                    return Result<string?>.Success(donationCheckoutResult.Value.CheckoutUrl);
                }
                else
                {
                    _logger.LogWarning(
                        "HandleMultiAttendeeRsvp: Failed to create donation checkout - DonationId={DonationId}, Error={Error}. Registration will complete without donation.",
                        bundledDonation.Id, donationCheckoutResult.Error);
                }
            }
            else
            {
                _logger.LogWarning(
                    "HandleMultiAttendeeRsvp: Success/Cancel URLs missing for free event donation - saving registration without donation checkout");
            }
        }

        // Free event - save and return null (no payment needed)
        await _unitOfWork.CommitAsync(cancellationToken);
        return Result<string?>.Success(null);
    }

    /// <summary>
    /// Handles creating a bundled donation during registration.
    /// C2 Guard: Called inside isolated try-catch so registration succeeds even if this fails.
    /// Returns null if donation creation fails (caller should continue without donation).
    /// </summary>
    private async Task<Donation?> HandleDonationDuringRegistration(
        Event @event,
        Registration registration,
        Guid userId,
        string donorEmail,
        decimal donationAmount,
        string? donorName,
        string? donorPhone,
        string? donorNotes,
        string? fallbackName,
        CancellationToken cancellationToken)
    {
        // Validate donation amount against event config
        var validateResult = @event.ValidateDonationAmount(donationAmount);
        if (validateResult.IsFailure)
        {
            _logger.LogWarning(
                "Donation validation failed during registration - EventId={EventId}, Amount={Amount}, Error={Error}",
                @event.Id, donationAmount, validateResult.Error);
            return null;
        }

        // Determine currency from registration's pricing or event
        var currency = registration.TotalPrice?.Currency
            ?? @event.Pricing?.Currency
            ?? Domain.Shared.Enums.Currency.USD;

        var amountResult = Money.Create(donationAmount, currency);
        if (amountResult.IsFailure)
        {
            _logger.LogWarning("Failed to create donation Money - Error={Error}", amountResult.Error);
            return null;
        }

        // Use DonorName from request, fallback to first attendee name
        var resolvedDonorName = !string.IsNullOrWhiteSpace(donorName)
            ? donorName
            : fallbackName ?? "Anonymous";

        var donationResult = Donation.CreateBundledWithRegistration(
            @event.Id,
            registration.Id,
            userId,
            resolvedDonorName,
            donorEmail,
            donorPhone,
            donorNotes,
            amountResult.Value);

        if (donationResult.IsFailure)
        {
            _logger.LogWarning("Failed to create bundled donation - Error={Error}", donationResult.Error);
            return null;
        }

        // Calculate revenue breakdown for donation
        try
        {
            var breakdownResult = await _revenueCalculatorService.CalculateBreakdownAsync(
                amountResult.Value,
                @event.Location,
                cancellationToken);

            if (breakdownResult.IsSuccess)
            {
                donationResult.Value.SetRevenueBreakdown(
                    breakdownResult.Value.StripeFeeAmount,
                    breakdownResult.Value.PlatformCommission,
                    breakdownResult.Value.OrganizerPayout);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Exception calculating donation revenue breakdown - DonationId={DonationId}",
                donationResult.Value.Id);
        }

        _logger.LogInformation(
            "Bundled donation created - DonationId={DonationId}, RegistrationId={RegistrationId}, Amount={Amount}",
            donationResult.Value.Id, registration.Id, donationAmount);

        return donationResult.Value;
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
