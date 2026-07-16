using System.Diagnostics;
using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.SharedKernel.Money;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using LankaConnect.Products.LankaEvents.Domain.Services;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using LankaConnect.Products.LankaEvents.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Commands.RsvpToEvent;

// Session 23: Updated to support Stripe payment integration for paid events
// Phase 6A.X: Added revenue breakdown calculation for paid registrations
// Phase 6A.137D: Added add-on bundling during registration checkout
// Phase 6A.137E: Added collection and sponsorship bundling during registration checkout
public class RsvpToEventCommandHandler : ICommandHandler<RsvpToEventCommand, string?>
{
    private readonly IEventRepository _eventRepository;
    private readonly IDonationRepository _donationRepository;
    private readonly IAddOnDefinitionRepository _addOnDefinitionRepository;
    private readonly IAddOnPurchaseRepository _addOnPurchaseRepository;
    private readonly ICollectionRepository _collectionRepository;
    private readonly ISponsorRepository _sponsorRepository;
    private readonly IUnitOfWork _unitOfWork;
    // Wave 8.5.g (2026-07-15): direct-SaveChanges pattern per Consult #25 Q6 blanket.
    // _unitOfWork.CommitAsync fires on AppDbContext = 0 changes (Registration + Event live on
    // LankaEventsDbContext post-Consult #20 sweep). Same split-brain fixed for CreateEvent
    // (17th deploy) and PublishEvent (27th deploy). Wave 8.5.f interceptor dispatches events.
    private readonly LankaEventsDbContext _dbContext;
    private readonly IStripePaymentService _stripePaymentService;
    private readonly IRevenueCalculatorService _revenueCalculatorService;
    // Phase 7E.3b (architect edit #2): Mode-B paid checkout shared between auth + anon handlers.
    private readonly LankaConnect.Products.LankaEvents.Application.Services.IRegistrationCheckoutService _checkoutService;
    // Phase 8 S8.2.B: Validates assigned-seating seat selections + builds the
    // pending-seat-assignment list that gets stashed on the registration.
    private readonly LankaConnect.Products.LankaEvents.Application.Services.ISeatAssignmentValidator _seatAssignmentValidator;
    private readonly ILogger<RsvpToEventCommandHandler> _logger;

    public RsvpToEventCommandHandler(
        IEventRepository eventRepository,
        IDonationRepository donationRepository,
        IAddOnDefinitionRepository addOnDefinitionRepository,
        IAddOnPurchaseRepository addOnPurchaseRepository,
        ICollectionRepository collectionRepository,
        ISponsorRepository sponsorRepository,
        IUnitOfWork unitOfWork,
        LankaEventsDbContext dbContext,
        IStripePaymentService stripePaymentService,
        IRevenueCalculatorService revenueCalculatorService,
        LankaConnect.Products.LankaEvents.Application.Services.IRegistrationCheckoutService checkoutService,
        LankaConnect.Products.LankaEvents.Application.Services.ISeatAssignmentValidator seatAssignmentValidator,
        ILogger<RsvpToEventCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _donationRepository = donationRepository;
        _addOnDefinitionRepository = addOnDefinitionRepository;
        _addOnPurchaseRepository = addOnPurchaseRepository;
        _collectionRepository = collectionRepository;
        _sponsorRepository = sponsorRepository;
        _unitOfWork = unitOfWork;
        _dbContext = dbContext;
        _stripePaymentService = stripePaymentService;
        _revenueCalculatorService = revenueCalculatorService;
        _checkoutService = checkoutService;
        _seatAssignmentValidator = seatAssignmentValidator;
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

                // Phase 8X.4b — ExternalPaid events have no internal registration path. The
                // architect-locked guard message points the buyer at the organiser-supplied URL
                // (matches LankaConnect.Products.LankaEvents.Domain.Event.ExternalRegistrationGuardMessage).
                // This check fires BEFORE the NoRegistration mode check below so the more-specific
                // ExternalPaid message wins (ExternalPaid events are forced to NoRegistration mode
                // by SetExternalPayment, which would otherwise return the generic NoRegistration
                // message).
                if (@event.PaymentMode == LankaConnect.Products.LankaEvents.Domain.Enums.EventPaymentMode.ExternalPaid)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "RsvpToEvent REJECTED: ExternalPaid event - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);
                    return Result<string?>.Failure(
                        "This event uses external registration. Users must register via the link provided by the organiser.");
                }

                // Phase 7E.3a: Dispatch by event.RegistrationMode BEFORE the legacy / multi-attendee
                // detection. NoRegistration → 400 (no Registration row to anchor RSVP against).
                // Head-count modes (B1-B4) → dedicated head-count flow. DetailedAttendees → existing
                // per-attendee flow below (no behaviour change for legacy events).
                if (@event.RegistrationMode == LankaConnect.Products.LankaEvents.Domain.Enums.RegistrationMode.NoRegistration)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "RsvpToEvent REJECTED: event uses NoRegistration mode - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);
                    return Result<string?>.Failure(
                        "Registration is not required for this event. Standalone donations / sponsors / " +
                        "add-on purchases / collections are still accepted via their own endpoints.");
                }

                if (@event.RegistrationMode != LankaConnect.Products.LankaEvents.Domain.Enums.RegistrationMode.DetailedAttendees)
                {
                    _logger.LogInformation(
                        "RsvpToEvent: Using head-count format - EventId={EventId}, Mode={Mode}",
                        request.EventId, @event.RegistrationMode);

                    result = await HandleHeadCountRsvp(@event, request, cancellationToken);
                }
                // Session 21: Determine if using new multi-attendee format or legacy format
                else if (isMultiAttendee)
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
        // Phase 8: Pass TicketTierId for tiered events; resolve tier name for denormalization
        var attendeeDetailsList = new List<AttendeeDetails>();
        foreach (var attendeeDto in request.Attendees!)
        {
            // Phase 8: Resolve tier name if TicketTierId is provided
            string? tierName = null;
            if (attendeeDto.TicketTierId.HasValue && @event.TicketingMode == LankaConnect.Products.LankaEvents.Domain.Enums.TicketingMode.Tiered)
            {
                var tier = @event.GetTicketTier(attendeeDto.TicketTierId.Value);
                if (tier == null)
                {
                    _logger.LogWarning(
                        "RsvpToEvent: Ticket tier not found - EventId={EventId}, TierId={TierId}, Attendee={AttendeeName}",
                        request.EventId, attendeeDto.TicketTierId.Value, attendeeDto.Name);
                    return Result<string?>.Failure($"Ticket tier not found for attendee '{attendeeDto.Name}'");
                }
                if (!tier.IsActive)
                {
                    return Result<string?>.Failure($"Ticket tier '{tier.Name}' is not active");
                }
                tierName = tier.Name;
            }
            else if (@event.TicketingMode == LankaConnect.Products.LankaEvents.Domain.Enums.TicketingMode.Tiered && !attendeeDto.TicketTierId.HasValue)
            {
                return Result<string?>.Failure($"Ticket tier is required for attendee '{attendeeDto.Name}' (event uses tiered ticketing)");
            }

            var attendeeResult = AttendeeDetails.Create(
                attendeeDto.Name, attendeeDto.AgeCategory, attendeeDto.Gender,
                attendeeDto.TicketTierId, tierName);
            if (attendeeResult.IsFailure)
                return Result<string?>.Failure(attendeeResult.Error);

            attendeeDetailsList.Add(attendeeResult.Value);
        }

        _logger.LogInformation(
            "RsvpToEvent: Attendees created - EventId={EventId}, TicketingMode={Mode}, Count={Count}",
            request.EventId, @event.TicketingMode, attendeeDetailsList.Count);

        // Phase 8 S8.2.B: Assigned-seating validation. Performs the seat-state
        // checks BEFORE we create the registration so a failure cleanly returns
        // a 400 to the buyer without leaving an orphan Preliminary row behind.
        // The pendingSeatAssignments list is null when the event uses general
        // admission; the SetPendingSeatAssignments call later is conditioned
        // on this being non-null.
        IReadOnlyList<PendingSeatAssignment>? pendingSeatAssignments = null;
        if (@event.SeatingMode == SeatingMode.AssignedSeating)
        {
            // Mode-A only — Mode-B (head-count) doesn't bind individual seats.
            // The architect plan defers add-attendees-with-seats to S9.
            if (request.SeatIds is null || request.SeatSessionId is null)
            {
                return Result<string?>.Failure(
                    "This event uses assigned seating — please select your seats before registering. " +
                    "(seatIds and seatSessionId are required.)");
            }

            var seatValidation = await _seatAssignmentValidator.ValidateAndBuildAssignmentsAsync(
                request.EventId,
                request.SeatSessionId,
                request.SeatIds,
                attendeeDetailsList.Count,
                cancellationToken);
            if (seatValidation.IsFailure)
            {
                _logger.LogWarning(
                    "RsvpToEvent: assigned-seating validation failed — EventId={EventId}, Error={Error}",
                    request.EventId, seatValidation.Error);
                return Result<string?>.Failure(seatValidation.Error);
            }
            pendingSeatAssignments = seatValidation.Value;
        }
        else
        {
            // GeneralAdmission: reject stale assigned-seating fields to avoid
            // silent client-state drift. If a frontend cache sends seat IDs for
            // an event whose SeatingMode just flipped to GA, we want a clear 400
            // rather than a confused-state success.
            if (request.SeatIds is { Count: > 0 } || !string.IsNullOrWhiteSpace(request.SeatSessionId))
            {
                return Result<string?>.Failure(
                    "This event uses general admission — seat selection is not supported. " +
                    "Refresh the page and try again.");
            }
        }

        // Create RegistrationContact value object
        // Phase 7A.6D: Pass WhatsApp phone + opt-in flag
        var contactResult = RegistrationContact.Create(
            request.Email,
            request.PhoneNumber,
            request.Address,
            request.WhatsAppPhoneNumber,
            !string.IsNullOrWhiteSpace(request.WhatsAppPhoneNumber)
        );

        if (contactResult.IsFailure)
            return Result<string?>.Failure(contactResult.Error);

        // Phase 6A.81 Part 4 FIX: Check for existing Preliminary registration to prevent duplicates
        // If user already has a Preliminary registration (payment not completed), reuse it
        var existingPreliminary = @event.Registrations.FirstOrDefault(r =>
            r.UserId == request.UserId &&
            r.Status == LankaConnect.Products.LankaEvents.Domain.Enums.RegistrationStatus.Preliminary);

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

        // Phase 8 S8.2.B: Stash the validated pending seat assignments on the
        // freshly-created registration. Webhook-side ConfirmSeatAssignments
        // (S8.2.C) reads this list to write SeatReservation rows and bind the
        // SeatId/SeatLabel onto each attendee after payment completes. For
        // free events the registration is already Confirmed at this point, so
        // SetPendingSeatAssignments would refuse — S8.2.C handles the free
        // event sync conversion path. We only stash for Preliminary (paid) here.
        if (pendingSeatAssignments is not null
            && registration.Status == LankaConnect.Products.LankaEvents.Domain.Enums.RegistrationStatus.Preliminary)
        {
            var stashResult = registration.SetPendingSeatAssignments(
                request.SeatSessionId!,
                pendingSeatAssignments);
            if (stashResult.IsFailure)
            {
                // Should be unreachable — the validator already confirmed
                // count match + invariants. Log loudly if reached.
                _logger.LogError(
                    "RsvpToEvent: SetPendingSeatAssignments failed unexpectedly after validation passed — " +
                    "RegistrationId={RegistrationId}, Error={Error}",
                    registration.Id, stashResult.Error);
                return Result<string?>.Failure(stashResult.Error);
            }
            _logger.LogInformation(
                "[Phase 8 S8.2.B] Pending seat assignments stashed — RegistrationId={RegistrationId}, SeatCount={Count}, SessionId={SessionId}",
                registration.Id, pendingSeatAssignments.Count, request.SeatSessionId);
        }

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

        // Phase 6A.137E: Handle optional collection (event fund) contribution during registration
        // C2 Guard: Collection failures are isolated — registration succeeds even if collection fails
        Collection? bundledCollection = null;
        if (request.CollectionAmount.HasValue && request.CollectionAmount.Value > 0 && @event.AreCollectionsEnabled())
        {
            try
            {
                var validateResult = @event.ValidateCollectionAmount(request.CollectionAmount.Value);
                if (validateResult.IsSuccess)
                {
                    var currency = registration.TotalPrice?.Currency
                        ?? @event.Pricing?.Currency
                        ?? LankaConnect.SharedKernel.Money.Currency.USD;

                    var amountResult = MoneyBuilder.Create(request.CollectionAmount.Value, currency);
                    if (amountResult.IsSuccess)
                    {
                        var collectionResult = Collection.Create(
                            @event.Id,
                            request.UserId,
                            request.Attendees?.FirstOrDefault()?.Name ?? "Contributor",
                            request.Email!,
                            request.PhoneNumber,
                            request.CollectionNotes,
                            amountResult.Value);

                        if (collectionResult.IsSuccess)
                        {
                            bundledCollection = collectionResult.Value;

                            // Calculate revenue breakdown
                            try
                            {
                                var breakdown = await _revenueCalculatorService.CalculateBreakdownAsync(
                                    amountResult.Value, @event.Location, cancellationToken);
                                if (breakdown.IsSuccess)
                                {
                                    bundledCollection.SetRevenueBreakdown(
                                        breakdown.Value.StripeFeeAmount,
                                        breakdown.Value.PlatformCommission,
                                        breakdown.Value.OrganizerPayout);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Exception calculating collection revenue breakdown");
                            }

                            _logger.LogInformation(
                                "Bundled collection created - CollectionId={CollectionId}, Amount={Amount}",
                                bundledCollection.Id, request.CollectionAmount.Value);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("Collection validation failed during registration - Error={Error}", validateResult.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "HandleMultiAttendeeRsvp: Collection processing failed - EventId={EventId}. Registration will continue without collection.",
                    @event.Id);
                bundledCollection = null;
            }
        }

        // Phase 6A.137E: Handle optional money sponsorship during registration
        // C2 Guard: Sponsor failures are isolated — registration succeeds even if sponsor fails
        Sponsor? bundledSponsor = null;
        if (request.SponsorAmount.HasValue && request.SponsorAmount.Value > 0 && @event.AreSponsorsEnabled())
        {
            try
            {
                var currency = registration.TotalPrice?.Currency
                    ?? @event.Pricing?.Currency
                    ?? LankaConnect.SharedKernel.Money.Currency.USD;

                var amountResult = MoneyBuilder.Create(request.SponsorAmount.Value, currency);
                if (amountResult.IsSuccess)
                {
                    // W5.D10.c — prefer caller-supplied sponsor contact fields when provided
                    // (parity with the standalone /sponsors flow); fall back to the
                    // registering user's identity when blank. Trim + null-coalesce so a
                    // whitespace-only override doesn't accidentally null a real value.
                    var sponsorName = !string.IsNullOrWhiteSpace(request.SponsorName)
                        ? request.SponsorName.Trim()
                        : (request.Attendees?.FirstOrDefault()?.Name ?? "Sponsor");
                    var sponsorEmail = !string.IsNullOrWhiteSpace(request.SponsorEmail)
                        ? request.SponsorEmail.Trim()
                        : request.Email!;
                    var sponsorPhone = !string.IsNullOrWhiteSpace(request.SponsorPhone)
                        ? request.SponsorPhone.Trim()
                        : request.PhoneNumber;

                    var sponsorResult = Sponsor.CreateMoneySponsor(
                        @event.Id,
                        request.UserId,
                        sponsorName,
                        sponsorEmail,
                        sponsorPhone,
                        request.SponsorOrganization,
                        request.SponsorNotes,
                        amountResult.Value);

                    if (sponsorResult.IsSuccess)
                    {
                        bundledSponsor = sponsorResult.Value;

                        // Phase 6A.151 C5 — attach a pre-staged sponsor image
                        // when the FE supplied {SponsorStagingBlobName, SponsorStagingBlobUrl}.
                        // Best-effort: if SetImage rejects (defensive), the sponsor
                        // row is still created without an image so the registration
                        // doesn't fail because of a bad logo upload.
                        if (!string.IsNullOrWhiteSpace(request.SponsorStagingBlobUrl)
                            && !string.IsNullOrWhiteSpace(request.SponsorStagingBlobName))
                        {
                            try
                            {
                                var imgResult = bundledSponsor.SetImage(
                                    request.SponsorStagingBlobUrl,
                                    request.SponsorStagingBlobName);
                                if (imgResult.IsFailure)
                                {
                                    _logger.LogWarning(
                                        "Bundled sponsor SetImage rejected — SponsorId={SponsorId}, Error={Error}",
                                        bundledSponsor.Id, imgResult.Error);
                                }
                                else
                                {
                                    _logger.LogInformation(
                                        "Bundled sponsor image attached — SponsorId={SponsorId}, BlobName={BlobName}",
                                        bundledSponsor.Id, request.SponsorStagingBlobName);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex,
                                    "Bundled sponsor SetImage threw — SponsorId={SponsorId}. Sponsor row will be created without image.",
                                    bundledSponsor.Id);
                            }
                        }

                        // Calculate revenue breakdown
                        try
                        {
                            var breakdown = await _revenueCalculatorService.CalculateBreakdownAsync(
                                amountResult.Value, @event.Location, cancellationToken);
                            if (breakdown.IsSuccess)
                            {
                                bundledSponsor.SetRevenueBreakdown(
                                    breakdown.Value.StripeFeeAmount,
                                    breakdown.Value.PlatformCommission,
                                    breakdown.Value.OrganizerPayout);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Exception calculating sponsor revenue breakdown");
                        }

                        _logger.LogInformation(
                            "Bundled money sponsor created - SponsorId={SponsorId}, Amount={Amount}",
                            bundledSponsor.Id, request.SponsorAmount.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "HandleMultiAttendeeRsvp: Sponsor processing failed - EventId={EventId}. Registration will continue without sponsor.",
                    @event.Id);
                bundledSponsor = null;
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

            // Phase 8: Always use line items for tiered events (per-tier breakdown)
            // Also use line items when donation, add-ons, collection, or sponsor are bundled
            var isTiered = @event.TicketingMode == LankaConnect.Products.LankaEvents.Domain.Enums.TicketingMode.Tiered;
            var hasLineItems = isTiered || bundledDonation != null || bundledAddOns.Count > 0
                || bundledCollection != null || bundledSponsor != null;
            if (hasLineItems)
            {
                var currency = registration.TotalPrice.Currency.ToString();
                var lineItems = new List<CheckoutLineItem>();

                if (isTiered && registration.Attendees != null)
                {
                    // Phase 8: Per-tier line items for tiered events
                    var tierGroups = registration.Attendees
                        .Where(a => a.TicketTierId != null)
                        .GroupBy(a => new { a.TicketTierId, a.TicketTierName });

                    foreach (var group in tierGroups)
                    {
                        var tier = @event.GetTicketTier(group.Key.TicketTierId!.Value);
                        if (tier == null) continue;

                        var adultCount = group.Count(a => a.AgeCategory == LankaConnect.Products.LankaEvents.Domain.Enums.AgeCategory.Adult);
                        var childCount = group.Count(a => a.AgeCategory == LankaConnect.Products.LankaEvents.Domain.Enums.AgeCategory.Child);
                        var tierTotal = group.Sum(a => tier.CalculatePriceForAttendee(a.AgeCategory).Amount);

                        var descParts = new List<string>();
                        if (adultCount > 0) descParts.Add($"{adultCount} Adult @ {tier.AdultPrice.Amount:F2}");
                        if (childCount > 0 && tier.ChildPrice != null) descParts.Add($"{childCount} Child @ {tier.ChildPrice.Amount:F2}");

                        lineItems.Add(new CheckoutLineItem
                        {
                            Name = $"{group.Key.TicketTierName ?? "Tier"}: {@event.Title.Value}",
                            Description = string.Join(", ", descParts),
                            Amount = tierTotal,
                            Currency = currency
                        });

                        _logger.LogInformation(
                            "RsvpToEvent: Tiered line item - Tier={TierName}, Adults={Adults}, Children={Children}, Total={Total}",
                            group.Key.TicketTierName, adultCount, childCount, tierTotal);
                    }
                }
                else
                {
                    // SingleTier: single registration line item
                    lineItems.Add(new CheckoutLineItem
                    {
                        Name = $"Event Registration: {@event.Title.Value}",
                        Description = $"Registration for {registration.Attendees?.Count ?? 1} attendee(s)",
                        Amount = registration.TotalPrice.Amount,
                        Currency = currency
                    });
                }

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

                // Phase 6A.137E: Add collection line item
                if (bundledCollection != null)
                {
                    lineItems.Add(new CheckoutLineItem
                    {
                        Name = $"Event Fund Contribution: {@event.Title.Value}",
                        Description = "Collection contribution",
                        Amount = bundledCollection.Amount.Amount,
                        Currency = currency
                    });
                    totalAmount += bundledCollection.Amount.Amount;
                    checkoutRequest.Metadata["collection_id"] = bundledCollection.Id.ToString();
                }

                // Phase 6A.137E: Add sponsor line item
                if (bundledSponsor != null)
                {
                    lineItems.Add(new CheckoutLineItem
                    {
                        Name = $"Sponsorship: {@event.Title.Value}",
                        Description = request.SponsorOrganization != null
                            ? $"Sponsored by {request.SponsorOrganization}"
                            : "Money sponsorship",
                        Amount = bundledSponsor.Amount!.Amount,
                        Currency = currency
                    });
                    totalAmount += bundledSponsor.Amount!.Amount;
                    checkoutRequest.Metadata["sponsor_id"] = bundledSponsor.Id.ToString();
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

            // Phase 6A.137E: Set checkout session on bundled collection and persist
            if (bundledCollection != null)
            {
                var collectionSessionResult = bundledCollection.SetStripeCheckoutSession(
                    checkoutResult.Value.SessionId,
                    addOnExpiresAt);

                if (collectionSessionResult.IsFailure)
                {
                    _logger.LogWarning(
                        "HandleMultiAttendeeRsvp: Failed to set checkout session on bundled collection - CollectionId={CollectionId}, Error={Error}",
                        bundledCollection.Id, collectionSessionResult.Error);
                }

                await _collectionRepository.AddAsync(bundledCollection, cancellationToken);
            }

            // Phase 6A.137E: Set checkout session on bundled sponsor and persist
            if (bundledSponsor != null)
            {
                var sponsorSessionResult = bundledSponsor.SetStripeCheckoutSession(
                    checkoutResult.Value.SessionId,
                    addOnExpiresAt);

                if (sponsorSessionResult.IsFailure)
                {
                    _logger.LogWarning(
                        "HandleMultiAttendeeRsvp: Failed to set checkout session on bundled sponsor - SponsorId={SponsorId}, Error={Error}",
                        bundledSponsor.Id, sponsorSessionResult.Error);
                }

                await _sponsorRepository.AddAsync(bundledSponsor, cancellationToken);
            }

            // Save changes with checkout session ID
            await _dbContext.SaveChangesAsync(cancellationToken); // Wave 8.5.g: direct-SaveChanges on LankaEventsDbContext (was IUnitOfWork = 0 changes on AppDbContext)

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
                    await _dbContext.SaveChangesAsync(cancellationToken); // Wave 8.5.g: direct-SaveChanges on LankaEventsDbContext (was IUnitOfWork = 0 changes on AppDbContext)

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
        await _dbContext.SaveChangesAsync(cancellationToken); // Wave 8.5.g: direct-SaveChanges on LankaEventsDbContext (was IUnitOfWork = 0 changes on AppDbContext)
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
            ?? LankaConnect.SharedKernel.Money.Currency.USD;

        var amountResult = MoneyBuilder.Create(donationAmount, currency);
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
        await _dbContext.SaveChangesAsync(cancellationToken); // Wave 8.5.g: direct-SaveChanges on LankaEventsDbContext (was IUnitOfWork = 0 changes on AppDbContext)

        // Legacy format always returns null (no payment support)
        return Result<string?>.Success(null);
    }

    /// <summary>
    /// Phase 7E.3a: Handles RSVP for head-count modes (B1-B4). Free events only — paid path
    /// (with Stripe checkout) lands in 7E.3b alongside explicit amount-calc tests.
    ///
    /// Builds a <see cref="HeadCountBreakdown"/> from the request DTO using the mode-specific
    /// factory, then delegates to <see cref="Event.RegisterWithHeadCount"/> which performs the
    /// shared status / capacity / duplicate / pricing guards and creates the registration row.
    /// </summary>
    private async Task<Result<string?>> HandleHeadCountRsvp(
        Event @event,
        RsvpToEventCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Lead name + HeadCount payload required for B-mode events.
        if (string.IsNullOrWhiteSpace(request.LeadAttendeeName))
            return Result<string?>.Failure(
                $"Lead attendee name is required for {@event.RegistrationMode} events.");

        if (request.HeadCount == null)
            return Result<string?>.Failure(
                $"Head-count breakdown is required for {@event.RegistrationMode} events.");

        // 2. Contact info — same shape as multi-attendee path.
        if (string.IsNullOrWhiteSpace(request.Email))
            return Result<string?>.Failure("Email is required");

        var contactResult = LankaConnect.Products.LankaEvents.Domain.ValueObjects.RegistrationContact.Create(
            request.Email!,
            request.PhoneNumber ?? string.Empty,
            request.Address);
        if (contactResult.IsFailure)
            return Result<string?>.Failure(contactResult.Errors);

        // 3. Build HeadCountBreakdown using the mode-specific factory.
        // For 7E.3c the optional TierCounts list flows through; the factory validates the sum
        // invariant against Total. For 7E.3a (no tiers in scope), TierCounts will be null.
        var hc = request.HeadCount;
        IReadOnlyList<LankaConnect.Products.LankaEvents.Domain.ValueObjects.TierCount>? tierCounts = null;
        if (hc.TierCounts != null && hc.TierCounts.Count > 0)
        {
            // Resolve tier names from event tiers (snapshotted onto each TierCount VO).
            var resolvedTiers = new List<LankaConnect.Products.LankaEvents.Domain.ValueObjects.TierCount>();
            foreach (var tcDto in hc.TierCounts)
            {
                var tier = @event.TicketTiers.FirstOrDefault(t => t.Id == tcDto.TierId);
                if (tier == null)
                    return Result<string?>.Failure($"Ticket tier {tcDto.TierId} not found on this event.");
                // Phase 7F-C: forward optional per-tier-by-age axis. Phase 7F-E.7: forward
                // optional per-tier 4-leaf demographic split. Domain factory enforces
                // all-or-nothing + sum-match invariants on both axes; cross-axis agreement
                // (age split derived from 4-leaf) auto-fills for back-compat.
                var tcResult = LankaConnect.Products.LankaEvents.Domain.ValueObjects.TierCount.Create(
                    tier.Id, tier.Name, tcDto.Count,
                    tcDto.AdultCount, tcDto.ChildCount,
                    tcDto.AdultMaleCount, tcDto.AdultFemaleCount,
                    tcDto.ChildMaleCount, tcDto.ChildFemaleCount);
                if (tcResult.IsFailure)
                    return Result<string?>.Failure(tcResult.Errors);
                resolvedTiers.Add(tcResult.Value);
            }
            tierCounts = resolvedTiers;
        }

        Result<LankaConnect.Products.LankaEvents.Domain.ValueObjects.HeadCountBreakdown> hcResult;
        switch (@event.RegistrationMode)
        {
            case LankaConnect.Products.LankaEvents.Domain.Enums.RegistrationMode.HeadCountOnly:
                if (!hc.Total.HasValue)
                    return Result<string?>.Failure("Total head-count is required for HeadCountOnly mode.");
                hcResult = LankaConnect.Products.LankaEvents.Domain.ValueObjects.HeadCountBreakdown.ForTotalOnly(hc.Total.Value, tierCounts);
                break;
            case LankaConnect.Products.LankaEvents.Domain.Enums.RegistrationMode.HeadCountByAge:
                if (!hc.Adults.HasValue || !hc.Children.HasValue)
                    return Result<string?>.Failure("Adults and Children counts are required for HeadCountByAge mode.");
                hcResult = LankaConnect.Products.LankaEvents.Domain.ValueObjects.HeadCountBreakdown.ForByAge(
                    hc.Adults.Value, hc.Children.Value, tierCounts);
                break;
            case LankaConnect.Products.LankaEvents.Domain.Enums.RegistrationMode.HeadCountByGender:
                if (!hc.Males.HasValue || !hc.Females.HasValue)
                    return Result<string?>.Failure("Males and Females counts are required for HeadCountByGender mode.");
                hcResult = LankaConnect.Products.LankaEvents.Domain.ValueObjects.HeadCountBreakdown.ForByGender(
                    hc.Males.Value, hc.Females.Value, tierCounts);
                break;
            case LankaConnect.Products.LankaEvents.Domain.Enums.RegistrationMode.HeadCountByAgeAndGender:
                if (!hc.AdultMales.HasValue || !hc.AdultFemales.HasValue ||
                    !hc.ChildMales.HasValue || !hc.ChildFemales.HasValue)
                    return Result<string?>.Failure(
                        "AdultMales, AdultFemales, ChildMales, and ChildFemales counts are required for HeadCountByAgeAndGender mode.");
                hcResult = LankaConnect.Products.LankaEvents.Domain.ValueObjects.HeadCountBreakdown.ForByAgeAndGender(
                    hc.AdultMales.Value, hc.AdultFemales.Value, hc.ChildMales.Value, hc.ChildFemales.Value,
                    tierCounts);
                break;
            default:
                // Should not happen — handler dispatches DetailedAttendees / NoRegistration elsewhere.
                return Result<string?>.Failure(
                    $"Unexpected registration mode in head-count handler: {@event.RegistrationMode}");
        }

        if (hcResult.IsFailure)
            return Result<string?>.Failure(hcResult.Errors);

        // 4. Delegate to the domain method — performs status / date / mode / duplicate / capacity /
        //    paid-event guards. For 7E.3a, paid events return a clear "deferred to 7E.3b" message.
        var registerResult = @event.RegisterWithHeadCount(
            request.UserId == Guid.Empty ? null : request.UserId,
            request.LeadAttendeeName!,
            hcResult.Value,
            contactResult.Value);

        if (registerResult.IsFailure)
            return Result<string?>.Failure(registerResult.Errors);

        _eventRepository.Update(@event);

        // Phase 7E.3b: paid B-mode → create Stripe Checkout session via the shared service.
        // For free events the registration is already Confirmed; just commit and return null.
        var registration = @event.Registrations.Last();
        if (!@event.IsFree() && registration.TotalPrice != null && registration.TotalPrice.Amount > 0)
        {
            if (string.IsNullOrWhiteSpace(request.SuccessUrl) || string.IsNullOrWhiteSpace(request.CancelUrl))
                return Result<string?>.Failure("Success and Cancel URLs are required for paid events");

            var checkoutResult = await _checkoutService.CreateSessionForRegistrationAsync(
                @event, registration,
                request.SuccessUrl!, request.CancelUrl!,
                cancellationToken);
            if (checkoutResult.IsFailure)
                return Result<string?>.Failure(checkoutResult.Error);

            await _dbContext.SaveChangesAsync(cancellationToken); // Wave 8.5.g: direct-SaveChanges on LankaEventsDbContext (was IUnitOfWork = 0 changes on AppDbContext)
            return Result<string?>.Success(checkoutResult.Value);
        }

        await _dbContext.SaveChangesAsync(cancellationToken); // Wave 8.5.g: direct-SaveChanges on LankaEventsDbContext (was IUnitOfWork = 0 changes on AppDbContext)
        return Result<string?>.Success(null);
    }
}
