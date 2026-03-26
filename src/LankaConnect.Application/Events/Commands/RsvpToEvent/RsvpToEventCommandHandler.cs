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
// Phase 6A.137D: Added add-on bundling during registration checkout
public class RsvpToEventCommandHandler : ICommandHandler<RsvpToEventCommand, string?>
{
    private readonly IEventRepository _eventRepository;
    private readonly IDonationRepository _donationRepository;
    private readonly IAddOnDefinitionRepository _addOnDefinitionRepository;
    private readonly IAddOnPurchaseRepository _addOnPurchaseRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStripePaymentService _stripePaymentService;
    private readonly IRevenueCalculatorService _revenueCalculatorService;
    private readonly ILogger<RsvpToEventCommandHandler> _logger;

    public RsvpToEventCommandHandler(
        IEventRepository eventRepository,
        IDonationRepository donationRepository,
        IAddOnDefinitionRepository addOnDefinitionRepository,
        IAddOnPurchaseRepository addOnPurchaseRepository,
        IUnitOfWork unitOfWork,
        IStripePaymentService stripePaymentService,
        IRevenueCalculatorService revenueCalculatorService,
        ILogger<RsvpToEventCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _donationRepository = donationRepository;
        _addOnDefinitionRepository = addOnDefinitionRepository;
        _addOnPurchaseRepository = addOnPurchaseRepository;
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

        // Phase 6A.137D: Handle optional add-ons bundled with registration
        // C2 Guard: Add-on failures are isolated — registration succeeds even if add-on creation fails
        var bundledAddOns = new List<(AddOnPurchase Purchase, string DefinitionName)>();
        if (request.AddOnSelections != null && request.AddOnSelections.Any())
        {
            try
            {
                bundledAddOns = await HandleAddOnsDuringRegistration(
                    @event, registration, request.UserId, request.Email!,
                    request.Attendees?.FirstOrDefault()?.Name ?? "Attendee",
                    request.AddOnSelections,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "HandleMultiAttendeeRsvp: Add-on processing failed - EventId={EventId}, RegistrationId={RegistrationId}. Registration will continue without add-ons.",
                    @event.Id, registration.Id);
                bundledAddOns.Clear();
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

            // Build multi-line-item checkout when donation or add-ons are bundled
            var hasLineItems = bundledDonation != null || bundledAddOns.Count > 0;
            if (hasLineItems)
            {
                var currency = registration.TotalPrice.Currency.ToString();
                var lineItems = new List<CheckoutLineItem>
                {
                    new CheckoutLineItem
                    {
                        Name = $"Event Registration: {@event.Title.Value}",
                        Description = $"Registration for {registration.Attendees?.Count ?? 1} attendee(s)",
                        Amount = registration.TotalPrice.Amount,
                        Currency = currency
                    }
                };

                var totalAmount = registration.TotalPrice.Amount;

                // Add donation line item
                if (bundledDonation != null)
                {
                    lineItems.Add(new CheckoutLineItem
                    {
                        Name = $"Donation: {@event.Title.Value}",
                        Description = "Voluntary donation",
                        Amount = bundledDonation.Amount.Amount,
                        Currency = currency
                    });
                    totalAmount += bundledDonation.Amount.Amount;
                    checkoutRequest.Metadata["donation_id"] = bundledDonation.Id.ToString();
                }

                // Phase 6A.137D: Add add-on line items
                foreach (var (addOn, definitionName) in bundledAddOns)
                {
                    lineItems.Add(new CheckoutLineItem
                    {
                        Name = $"Add-on: {definitionName}",
                        Description = $"Qty: {addOn.Quantity} × {addOn.UnitPrice.Amount:F2} {currency}",
                        Amount = addOn.TotalAmount.Amount,
                        Currency = currency
                    });
                    totalAmount += addOn.TotalAmount.Amount;
                }

                // Store add-on purchase IDs in metadata for webhook processing
                if (bundledAddOns.Count > 0)
                {
                    var addOnIds = string.Join(",", bundledAddOns.Select(a => a.Purchase.Id));
                    checkoutRequest.Metadata["addon_purchase_ids"] = addOnIds;
                }

                checkoutRequest.LineItems = lineItems;
                checkoutRequest.Amount = totalAmount;
            }

            var checkoutResult = await _stripePaymentService.CreateEventCheckoutSessionAsync(checkoutRequest, cancellationToken);
            if (checkoutResult.IsFailure)
                return Result<string?>.Failure($"Failed to create payment session: {checkoutResult.Error}");

            // Phase 6A.136D: Store session ID (not URL) in StripeCheckoutSessionId
            // Phase 6A.136F: Pass Stripe's actual expiry to prevent timestamp drift
            var setSessionResult = registration.SetStripeCheckoutSession(checkoutResult.Value.SessionId, checkoutResult.Value.ExpiresAt);
            if (setSessionResult.IsFailure)
                return Result<string?>.Failure(setSessionResult.Error);

            // Set same checkout session on bundled donation (shared Stripe session)
            if (bundledDonation != null)
            {
                var donationSessionResult = bundledDonation.SetStripeCheckoutSession(
                    checkoutResult.Value.SessionId,
                    DateTime.UtcNow.AddHours(24));

                if (donationSessionResult.IsFailure)
                {
                    _logger.LogWarning(
                        "HandleMultiAttendeeRsvp: Failed to set checkout session on bundled donation - DonationId={DonationId}, Error={Error}",
                        bundledDonation.Id, donationSessionResult.Error);
                }

                await _donationRepository.AddAsync(bundledDonation, cancellationToken);
            }

            // Phase 6A.137D: Set checkout session on bundled add-on purchases and persist
            var addOnExpiresAt = checkoutResult.Value.ExpiresAt ?? DateTime.UtcNow.AddHours(24);
            foreach (var (addOn, _) in bundledAddOns)
            {
                var addOnSessionResult = addOn.SetStripeCheckoutSession(
                    checkoutResult.Value.SessionId,
                    addOnExpiresAt);

                if (addOnSessionResult.IsFailure)
                {
                    _logger.LogWarning(
                        "HandleMultiAttendeeRsvp: Failed to set checkout session on bundled add-on - PurchaseId={PurchaseId}, Error={Error}",
                        addOn.Id, addOnSessionResult.Error);
                }

                await _addOnPurchaseRepository.AddAsync(addOn, cancellationToken);
            }

            // Save changes with checkout session ID
            await _unitOfWork.CommitAsync(cancellationToken);

            // Return checkout URL for frontend to redirect
            return Result<string?>.Success(checkoutResult.Value.CheckoutUrl);
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
    /// Phase 6A.137D: Handles creating bundled add-on purchases during registration.
    /// C2 Guard: Called inside isolated try-catch so registration succeeds even if this fails.
    /// Returns empty list if any add-on creation fails (caller should continue without add-ons).
    /// Returns tuples of (Purchase, DefinitionName) for Stripe line item display.
    /// </summary>
    private async Task<List<(AddOnPurchase Purchase, string DefinitionName)>> HandleAddOnsDuringRegistration(
        Event @event,
        Registration registration,
        Guid userId,
        string buyerEmail,
        string buyerName,
        List<AddOnSelectionDto> selections,
        CancellationToken cancellationToken)
    {
        var purchases = new List<(AddOnPurchase Purchase, string DefinitionName)>();

        // Load all active add-on definitions for this event
        var definitions = await _addOnDefinitionRepository.GetActiveByEventIdAsync(@event.Id, cancellationToken);
        var definitionMap = definitions.ToDictionary(d => d.Id);

        foreach (var selection in selections)
        {
            if (selection.Quantity <= 0)
            {
                _logger.LogWarning(
                    "Skipping add-on selection with non-positive quantity - DefinitionId={DefinitionId}, Quantity={Quantity}",
                    selection.DefinitionId, selection.Quantity);
                continue;
            }

            // Validate definition exists and is active
            if (!definitionMap.TryGetValue(selection.DefinitionId, out var definition))
            {
                _logger.LogWarning(
                    "Add-on definition not found or inactive during registration - DefinitionId={DefinitionId}, EventId={EventId}",
                    selection.DefinitionId, @event.Id);
                continue;
            }

            // Check stock availability
            if (!definition.HasAvailableStock(selection.Quantity))
            {
                _logger.LogWarning(
                    "Insufficient stock for add-on during registration - DefinitionId={DefinitionId}, Requested={Requested}, Remaining={Remaining}",
                    selection.DefinitionId, selection.Quantity, definition.RemainingStock);
                continue;
            }

            // Reserve stock atomically
            var stockReserved = await _addOnDefinitionRepository.TryReserveStockAsync(
                selection.DefinitionId, selection.Quantity, cancellationToken);

            if (!stockReserved)
            {
                _logger.LogWarning(
                    "Failed to reserve stock for add-on during registration - DefinitionId={DefinitionId}, Quantity={Quantity}",
                    selection.DefinitionId, selection.Quantity);
                continue;
            }

            // Create bundled purchase entity with price snapshot
            var purchaseResult = AddOnPurchase.CreateBundledWithRegistration(
                eventId: @event.Id,
                addOnDefinitionId: definition.Id,
                registrationId: registration.Id,
                buyerUserId: userId,
                buyerName: buyerName,
                buyerEmail: buyerEmail,
                buyerPhone: null,
                quantity: selection.Quantity,
                unitPrice: definition.Price);

            if (purchaseResult.IsFailure)
            {
                _logger.LogWarning(
                    "Failed to create bundled add-on purchase - DefinitionId={DefinitionId}, Error={Error}",
                    selection.DefinitionId, purchaseResult.Error);

                // Restore stock since purchase creation failed
                await _addOnDefinitionRepository.TryRestoreStockAsync(
                    selection.DefinitionId, selection.Quantity, cancellationToken);
                continue;
            }

            // Calculate revenue breakdown for add-on
            try
            {
                var breakdownResult = await _revenueCalculatorService.CalculateBreakdownAsync(
                    purchaseResult.Value.TotalAmount,
                    @event.Location,
                    cancellationToken);

                if (breakdownResult.IsSuccess)
                {
                    purchaseResult.Value.SetRevenueBreakdown(
                        breakdownResult.Value.StripeFeeAmount,
                        breakdownResult.Value.PlatformCommission,
                        breakdownResult.Value.OrganizerPayout);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Exception calculating add-on revenue breakdown - PurchaseId={PurchaseId}",
                    purchaseResult.Value.Id);
            }

            purchases.Add((purchaseResult.Value, definition.Name));

            _logger.LogInformation(
                "Bundled add-on purchase created - PurchaseId={PurchaseId}, DefinitionId={DefinitionId}, Name={Name}, Quantity={Quantity}, Total={Total}",
                purchaseResult.Value.Id, definition.Id, definition.Name, selection.Quantity, purchaseResult.Value.TotalAmount.Amount);
        }

        return purchases;
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
