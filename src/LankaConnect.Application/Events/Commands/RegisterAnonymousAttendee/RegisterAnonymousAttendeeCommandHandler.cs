using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Domain.Events.Services;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Application.Events.Commands.RsvpToEvent;
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
    private readonly IDonationRepository _donationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;
    private readonly IStripePaymentService _stripePaymentService;
    private readonly IRevenueCalculatorService _revenueCalculatorService;
    // Phase 6A.137F: Add-on, collection, and sponsor repositories for bundled checkout
    private readonly IAddOnDefinitionRepository _addOnDefinitionRepository;
    private readonly IAddOnPurchaseRepository _addOnPurchaseRepository;
    private readonly ICollectionRepository _collectionRepository;
    private readonly ISponsorRepository _sponsorRepository;
    private readonly ILogger<RegisterAnonymousAttendeeCommandHandler> _logger;

    public RegisterAnonymousAttendeeCommandHandler(
        IEventRepository eventRepository,
        IDonationRepository donationRepository,
        IUnitOfWork unitOfWork,
        IUserRepository userRepository,
        IStripePaymentService stripePaymentService,
        IRevenueCalculatorService revenueCalculatorService,
        // Phase 6A.137F: Inject bundling dependencies
        IAddOnDefinitionRepository addOnDefinitionRepository,
        IAddOnPurchaseRepository addOnPurchaseRepository,
        ICollectionRepository collectionRepository,
        ISponsorRepository sponsorRepository,
        ILogger<RegisterAnonymousAttendeeCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _donationRepository = donationRepository;
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
        _stripePaymentService = stripePaymentService;
        _revenueCalculatorService = revenueCalculatorService;
        _addOnDefinitionRepository = addOnDefinitionRepository;
        _addOnPurchaseRepository = addOnPurchaseRepository;
        _collectionRepository = collectionRepository;
        _sponsorRepository = sponsorRepository;
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

                // Phase 6A.XXX FIX: Check ALL registrations by email (both anonymous AND authenticated)
                // Previously filtered on r.UserId == null which missed authenticated registrations,
                // allowing the same email to have duplicate registrations via different paths
                var existingRegistrationByEmail = @event.Registrations
                    .Where(r => r.Status != RegistrationStatus.Cancelled
                             && r.Status != RegistrationStatus.Refunded
                             && r.Status != RegistrationStatus.RefundRequested
                             && r.Status != RegistrationStatus.Preliminary
                             && r.Status != RegistrationStatus.Abandoned)
                    .FirstOrDefault(r =>
                        (r.Contact != null && r.Contact.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase)) ||
                        (r.AttendeeInfo != null && r.AttendeeInfo.Email != null &&
                         r.AttendeeInfo.Email.Value.Equals(request.Email, StringComparison.OrdinalIgnoreCase)));

                if (existingRegistrationByEmail != null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "RegisterAnonymousAttendee FAILED: Email already registered (cross-path check) - EventId={EventId}, Email={Email}, ExistingRegistrationId={RegistrationId}, ExistingUserId={ExistingUserId}, Duration={ElapsedMs}ms",
                        request.EventId, request.Email, existingRegistrationByEmail.Id, existingRegistrationByEmail.UserId, stopwatch.ElapsedMilliseconds);

                    return Result<string?>.Failure(
                        "This email is already registered for this event. Each email can only register once.");
                }

                _logger.LogInformation(
                    "RegisterAnonymousAttendee: Email not found in event registrations - proceeding - EventId={EventId}, Email={Email}",
                    request.EventId, request.Email);

                // Phase 7E.3a: Dispatch by event.RegistrationMode BEFORE format detection.
                // Mode C → 400; B-mode → head-count flow; DetailedAttendees → existing logic.
                if (@event.RegistrationMode == LankaConnect.Domain.Events.Enums.RegistrationMode.NoRegistration)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "RegisterAnonymousAttendee REJECTED: event uses NoRegistration mode - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);
                    return Result<string?>.Failure(
                        "Registration is not required for this event. Standalone donations / sponsors / " +
                        "add-on purchases / collections are still accepted via their own endpoints.");
                }

                if (@event.RegistrationMode != LankaConnect.Domain.Events.Enums.RegistrationMode.DetailedAttendees)
                {
                    _logger.LogInformation(
                        "RegisterAnonymousAttendee: Using head-count format - EventId={EventId}, Mode={Mode}",
                        request.EventId, @event.RegistrationMode);
                    return await HandleHeadCountAnonymousRegistration(@event, request, cancellationToken);
                }

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
        // Phase 7A.6D: Pass WhatsApp phone + opt-in flag
        var contactResult = RegistrationContact.Create(
            request.Email,
            request.PhoneNumber,
            request.Address,
            request.WhatsAppPhoneNumber,
            !string.IsNullOrWhiteSpace(request.WhatsAppPhoneNumber)
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

        // Donation Feature: Handle optional donation during anonymous registration
        // C2 Guard: Donation in isolated try-catch, registration succeeds even if donation fails
        // C3 Guard: Check > 0, not just HasValue. Treat 0 same as null.
        Donation? bundledDonation = null;
        if (request.DonationAmount.HasValue && request.DonationAmount.Value > 0 && @event.AreDonationsEnabled())
        {
            try
            {
                bundledDonation = await HandleDonationDuringAnonymousRegistration(
                    @event, registration, request, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "HandleMultiAttendeeRegistration: Donation processing failed - EventId={EventId}, RegistrationId={RegistrationId}. Registration will continue without donation.",
                    @event.Id, registration.Id);
                bundledDonation = null;
            }
        }

        // Phase 6A.137F: Handle optional add-ons bundled with anonymous registration
        // C2 Guard: Add-on failures are isolated — registration succeeds even if add-on creation fails
        var bundledAddOns = new List<(AddOnPurchase Purchase, string DefinitionName)>();
        if (request.AddOnSelections != null && request.AddOnSelections.Any())
        {
            try
            {
                bundledAddOns = await HandleAddOnsDuringAnonymousRegistration(
                    @event, registration, request.Email,
                    request.Attendees?.FirstOrDefault()?.Name ?? request.Name ?? "Attendee",
                    request.AddOnSelections,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Anonymous registration: Add-on processing failed - EventId={EventId}, RegistrationId={RegistrationId}. Registration will continue without add-ons.",
                    @event.Id, registration.Id);
                bundledAddOns.Clear();
            }
        }

        // Phase 6A.137F: Handle optional collection contribution during anonymous registration
        // C2 Guard: Collection failures are isolated
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
                        ?? Domain.Shared.Enums.Currency.USD;

                    var amountResult = Money.Create(request.CollectionAmount.Value, currency);
                    if (amountResult.IsSuccess)
                    {
                        // Anonymous users don't have a UserId — use Guid.Empty
                        var collectionResult = Collection.Create(
                            @event.Id,
                            Guid.Empty,
                            request.Attendees?.FirstOrDefault()?.Name ?? request.Name ?? "Contributor",
                            request.Email,
                            request.PhoneNumber,
                            request.CollectionNotes,
                            amountResult.Value);

                        if (collectionResult.IsSuccess)
                        {
                            bundledCollection = collectionResult.Value;

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
                                _logger.LogError(ex, "Exception calculating collection revenue breakdown for anonymous registration");
                            }

                            _logger.LogInformation(
                                "Anonymous registration: Bundled collection created - CollectionId={CollectionId}, Amount={Amount}",
                                bundledCollection.Id, request.CollectionAmount.Value);
                        }
                    }
                }
                else
                {
                    _logger.LogWarning("Collection validation failed during anonymous registration - Error={Error}", validateResult.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Anonymous registration: Collection processing failed - EventId={EventId}. Registration will continue without collection.",
                    @event.Id);
                bundledCollection = null;
            }
        }

        // Phase 6A.137F: Handle optional money sponsorship during anonymous registration
        // C2 Guard: Sponsor failures are isolated
        Sponsor? bundledSponsor = null;
        if (request.SponsorAmount.HasValue && request.SponsorAmount.Value > 0 && @event.AreSponsorsEnabled())
        {
            try
            {
                var currency = registration.TotalPrice?.Currency
                    ?? @event.Pricing?.Currency
                    ?? Domain.Shared.Enums.Currency.USD;

                var amountResult = Money.Create(request.SponsorAmount.Value, currency);
                if (amountResult.IsSuccess)
                {
                    // Anonymous users don't have a UserId — use Guid.Empty
                    var sponsorResult = Sponsor.CreateMoneySponsor(
                        @event.Id,
                        Guid.Empty,
                        request.Attendees?.FirstOrDefault()?.Name ?? request.Name ?? "Sponsor",
                        request.Email,
                        request.PhoneNumber,
                        request.SponsorOrganization,
                        request.SponsorNotes,
                        amountResult.Value);

                    if (sponsorResult.IsSuccess)
                    {
                        bundledSponsor = sponsorResult.Value;

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
                            _logger.LogError(ex, "Exception calculating sponsor revenue breakdown for anonymous registration");
                        }

                        _logger.LogInformation(
                            "Anonymous registration: Bundled money sponsor created - SponsorId={SponsorId}, Amount={Amount}",
                            bundledSponsor.Id, request.SponsorAmount.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Anonymous registration: Sponsor processing failed - EventId={EventId}. Registration will continue without sponsor.",
                    @event.Id);
                bundledSponsor = null;
            }
        }

        // Phase 6A.44: Handle payment for paid events
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
                    { "anonymous", "true" },
                    { "email", request.Email }
                }
            };

            // Phase 6A.137F: Build multi-line-item checkout when any bundled items exist
            var hasLineItems = bundledDonation != null || bundledAddOns.Count > 0
                || bundledCollection != null || bundledSponsor != null;
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

                // Add add-on line items
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

                if (bundledAddOns.Count > 0)
                {
                    var addOnIds = string.Join(",", bundledAddOns.Select(a => a.Purchase.Id));
                    checkoutRequest.Metadata["addon_purchase_ids"] = addOnIds;
                }

                // Add collection line item
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

                // Add sponsor line item
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
            // Phase 6A.136F: Pass Stripe expiry to align with actual session lifetime
            var setSessionResult = registration.SetStripeCheckoutSession(checkoutResult.Value.SessionId, checkoutResult.Value.ExpiresAt);
            if (setSessionResult.IsFailure)
                return Result<string?>.Failure(setSessionResult.Error);

            // Set same checkout session on bundled donation
            if (bundledDonation != null)
            {
                var donationSessionResult = bundledDonation.SetStripeCheckoutSession(
                    checkoutResult.Value.SessionId,
                    DateTime.UtcNow.AddHours(24));

                if (donationSessionResult.IsFailure)
                {
                    _logger.LogWarning(
                        "Anonymous registration: Failed to set checkout session on bundled donation - DonationId={DonationId}, Error={Error}",
                        bundledDonation.Id, donationSessionResult.Error);
                }

                await _donationRepository.AddAsync(bundledDonation, cancellationToken);
            }

            // Phase 6A.137F: Set checkout session on bundled add-on purchases and persist
            var addOnExpiresAt = checkoutResult.Value.ExpiresAt ?? DateTime.UtcNow.AddHours(24);
            foreach (var (addOn, _) in bundledAddOns)
            {
                var addOnSessionResult = addOn.SetStripeCheckoutSession(
                    checkoutResult.Value.SessionId,
                    addOnExpiresAt);

                if (addOnSessionResult.IsFailure)
                {
                    _logger.LogWarning(
                        "Anonymous registration: Failed to set checkout session on bundled add-on - PurchaseId={PurchaseId}, Error={Error}",
                        addOn.Id, addOnSessionResult.Error);
                }

                await _addOnPurchaseRepository.AddAsync(addOn, cancellationToken);
            }

            // Phase 6A.137F: Set checkout session on bundled collection and persist
            if (bundledCollection != null)
            {
                var collectionSessionResult = bundledCollection.SetStripeCheckoutSession(
                    checkoutResult.Value.SessionId,
                    addOnExpiresAt);

                if (collectionSessionResult.IsFailure)
                {
                    _logger.LogWarning(
                        "Anonymous registration: Failed to set checkout session on bundled collection - CollectionId={CollectionId}, Error={Error}",
                        bundledCollection.Id, collectionSessionResult.Error);
                }

                await _collectionRepository.AddAsync(bundledCollection, cancellationToken);
            }

            // Phase 6A.137F: Set checkout session on bundled sponsor and persist
            if (bundledSponsor != null)
            {
                var sponsorSessionResult = bundledSponsor.SetStripeCheckoutSession(
                    checkoutResult.Value.SessionId,
                    addOnExpiresAt);

                if (sponsorSessionResult.IsFailure)
                {
                    _logger.LogWarning(
                        "Anonymous registration: Failed to set checkout session on bundled sponsor - SponsorId={SponsorId}, Error={Error}",
                        bundledSponsor.Id, sponsorSessionResult.Error);
                }

                await _sponsorRepository.AddAsync(bundledSponsor, cancellationToken);
            }

            // Save changes with checkout session ID
            await _unitOfWork.CommitAsync(cancellationToken);

            stopwatch.Stop();

            _logger.LogInformation(
                "Anonymous registration COMPLETE (PAID): EventId={EventId}, RegistrationId={RegistrationId}, Amount={Amount}, HasDonation={HasDonation}, HasAddOns={HasAddOns}, HasCollection={HasCollection}, HasSponsor={HasSponsor}, Duration={ElapsedMs}ms",
                @event.Id, registration.Id, registration.TotalPrice!.Amount,
                bundledDonation != null, bundledAddOns.Count > 0, bundledCollection != null, bundledSponsor != null,
                stopwatch.ElapsedMilliseconds);

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
                        { "anonymous", "true" },
                        { "email", request.Email }
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

                    stopwatch.Stop();

                    _logger.LogInformation(
                        "HandleMultiAttendeeRegistration COMPLETE (FREE+DONATION): EventId={EventId}, RegistrationId={RegistrationId}, DonationId={DonationId}, Duration={ElapsedMs}ms",
                        @event.Id, registration.Id, bundledDonation.Id, stopwatch.ElapsedMilliseconds);

                    return Result<string?>.Success(donationCheckoutResult.Value.CheckoutUrl);
                }
                else
                {
                    _logger.LogWarning(
                        "HandleMultiAttendeeRegistration: Failed to create donation checkout - DonationId={DonationId}, Error={Error}. Registration will complete without donation.",
                        bundledDonation.Id, donationCheckoutResult.Error);
                }
            }
        }

        // FREE event - save and return null (no payment needed)
        await _unitOfWork.CommitAsync(cancellationToken);

        stopwatch.Stop();

        _logger.LogInformation(
            "HandleMultiAttendeeRegistration COMPLETE (FREE): EventId={EventId}, RegistrationId={RegistrationId}, AttendeeCount={AttendeeCount}, Duration={ElapsedMs}ms",
            @event.Id, registration.Id, attendeeDetailsList.Count, stopwatch.ElapsedMilliseconds);

        return Result<string?>.Success(null);
    }

    /// <summary>
    /// Handles creating a bundled donation during anonymous registration.
    /// C2 Guard: Called inside isolated try-catch so registration succeeds even if this fails.
    /// Returns null if donation creation fails.
    /// </summary>
    private async Task<Donation?> HandleDonationDuringAnonymousRegistration(
        Event @event,
        Registration registration,
        RegisterAnonymousAttendeeCommand request,
        CancellationToken cancellationToken)
    {
        var validateResult = @event.ValidateDonationAmount(request.DonationAmount!.Value);
        if (validateResult.IsFailure)
        {
            _logger.LogWarning(
                "Donation validation failed during anonymous registration - EventId={EventId}, Amount={Amount}, Error={Error}",
                @event.Id, request.DonationAmount.Value, validateResult.Error);
            return null;
        }

        var currency = registration.TotalPrice?.Currency
            ?? @event.Pricing?.Currency
            ?? Domain.Shared.Enums.Currency.USD;

        var amountResult = Money.Create(request.DonationAmount!.Value, currency);
        if (amountResult.IsFailure)
        {
            _logger.LogWarning("Failed to create donation Money - Error={Error}", amountResult.Error);
            return null;
        }

        var donorName = !string.IsNullOrWhiteSpace(request.DonorName)
            ? request.DonorName
            : request.Attendees?.FirstOrDefault()?.Name ?? request.Name ?? "Anonymous";

        var donationResult = Donation.CreateBundledWithRegistration(
            @event.Id,
            registration.Id,
            donorUserId: null, // Anonymous
            donorName,
            request.Email,
            request.DonorPhone,
            request.DonorNotes,
            amountResult.Value);

        if (donationResult.IsFailure)
        {
            _logger.LogWarning("Failed to create bundled donation - Error={Error}", donationResult.Error);
            return null;
        }

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
            "Bundled donation created (anonymous) - DonationId={DonationId}, RegistrationId={RegistrationId}, Amount={Amount}",
            donationResult.Value.Id, registration.Id, request.DonationAmount.Value);

        return donationResult.Value;
    }

    // Phase 6A.137F: Handle add-ons during anonymous registration (mirrors RsvpToEventCommandHandler.HandleAddOnsDuringRegistration)
    // TODO: Extract shared BundledCheckoutService to eliminate duplication with RsvpToEventCommandHandler
    private async Task<List<(AddOnPurchase Purchase, string DefinitionName)>> HandleAddOnsDuringAnonymousRegistration(
        Event @event,
        Registration registration,
        string buyerEmail,
        string buyerName,
        List<AddOnSelectionDto> selections,
        CancellationToken cancellationToken)
    {
        var purchases = new List<(AddOnPurchase Purchase, string DefinitionName)>();

        var definitions = await _addOnDefinitionRepository.GetActiveByEventIdAsync(@event.Id, cancellationToken);
        var definitionMap = definitions.ToDictionary(d => d.Id);

        foreach (var selection in selections)
        {
            if (selection.Quantity <= 0)
            {
                _logger.LogWarning(
                    "Anonymous registration: Skipping add-on with non-positive quantity - DefinitionId={DefinitionId}, Quantity={Quantity}",
                    selection.DefinitionId, selection.Quantity);
                continue;
            }

            if (!definitionMap.TryGetValue(selection.DefinitionId, out var definition))
            {
                _logger.LogWarning(
                    "Anonymous registration: Add-on definition not found or inactive - DefinitionId={DefinitionId}, EventId={EventId}",
                    selection.DefinitionId, @event.Id);
                continue;
            }

            if (!definition.HasAvailableStock(selection.Quantity))
            {
                _logger.LogWarning(
                    "Anonymous registration: Insufficient add-on stock - DefinitionId={DefinitionId}, Requested={Requested}, Remaining={Remaining}",
                    selection.DefinitionId, selection.Quantity, definition.RemainingStock);
                continue;
            }

            var stockReserved = await _addOnDefinitionRepository.TryReserveStockAsync(
                selection.DefinitionId, selection.Quantity, cancellationToken);

            if (!stockReserved)
            {
                _logger.LogWarning(
                    "Anonymous registration: Failed to reserve add-on stock - DefinitionId={DefinitionId}, Quantity={Quantity}",
                    selection.DefinitionId, selection.Quantity);
                continue;
            }

            // Anonymous users don't have a UserId — use Guid.Empty
            var purchaseResult = AddOnPurchase.CreateBundledWithRegistration(
                eventId: @event.Id,
                addOnDefinitionId: definition.Id,
                registrationId: registration.Id,
                buyerUserId: Guid.Empty,
                buyerName: buyerName,
                buyerEmail: buyerEmail,
                buyerPhone: null,
                quantity: selection.Quantity,
                unitPrice: definition.Price);

            if (purchaseResult.IsFailure)
            {
                _logger.LogWarning(
                    "Anonymous registration: Failed to create bundled add-on purchase - DefinitionId={DefinitionId}, Error={Error}",
                    selection.DefinitionId, purchaseResult.Error);

                await _addOnDefinitionRepository.TryRestoreStockAsync(
                    selection.DefinitionId, selection.Quantity, cancellationToken);
                continue;
            }

            // Calculate revenue breakdown for add-on
            try
            {
                var breakdown = await _revenueCalculatorService.CalculateBreakdownAsync(
                    purchaseResult.Value.TotalAmount, @event.Location, cancellationToken);
                if (breakdown.IsSuccess)
                {
                    purchaseResult.Value.SetRevenueBreakdown(
                        breakdown.Value.StripeFeeAmount,
                        breakdown.Value.PlatformCommission,
                        breakdown.Value.OrganizerPayout);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception calculating add-on revenue breakdown for anonymous registration");
            }

            purchases.Add((purchaseResult.Value, definition.Name));
            _logger.LogInformation(
                "Anonymous registration: Bundled add-on created - PurchaseId={PurchaseId}, DefinitionId={DefinitionId}, Qty={Quantity}, Amount={Amount}",
                purchaseResult.Value.Id, definition.Id, selection.Quantity, purchaseResult.Value.TotalAmount.Amount);
        }

        return purchases;
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

            // Phase 6A.136D: Store session ID (not URL) in StripeCheckoutSessionId
            // Phase 6A.136F: Pass Stripe expiry to align with actual session lifetime
            var setSessionResult = registration.SetStripeCheckoutSession(checkoutResult.Value.SessionId, checkoutResult.Value.ExpiresAt);
            if (setSessionResult.IsFailure)
                return Result<string?>.Failure(setSessionResult.Error);

            // Save changes with checkout session ID
            await _unitOfWork.CommitAsync(cancellationToken);

            stopwatch.Stop();

            _logger.LogInformation(
                "HandleLegacyRegistration COMPLETE (PAID): EventId={EventId}, RegistrationId={RegistrationId}, CheckoutSessionId={SessionId}, Amount={Amount}, Quantity={Quantity}, Duration={ElapsedMs}ms",
                @event.Id, registration.Id, checkoutResult.Value.SessionId, registration.TotalPrice!.Amount, request.Quantity, stopwatch.ElapsedMilliseconds);

            // Return checkout URL for frontend to redirect
            return Result<string?>.Success(checkoutResult.Value.CheckoutUrl);
        }

        // FREE event - save and return null (no payment needed)
        await _unitOfWork.CommitAsync(cancellationToken);

        stopwatch.Stop();

        _logger.LogInformation(
            "HandleLegacyRegistration COMPLETE (FREE): EventId={EventId}, RegistrationId={RegistrationId}, Quantity={Quantity}, Duration={ElapsedMs}ms",
            @event.Id, registration.Id, request.Quantity, stopwatch.ElapsedMilliseconds);

        return Result<string?>.Success(null);
    }

    /// <summary>
    /// Phase 7E.3a: Handles anonymous RSVP for head-count modes (B1-B4). Free events only —
    /// paid path lands in 7E.3b alongside Stripe amount-calc tests.
    ///
    /// Mirrors <see cref="RsvpToEvent.RsvpToEventCommandHandler"/>'s head-count branch but
    /// passes <c>userId: null</c> to <see cref="Event.RegisterWithHeadCount"/>. The domain
    /// method's email-duplicate check covers both anonymous and authenticated registrations
    /// (cross-path detection per Phase 6A.XXX).
    /// </summary>
    private async Task<Result<string?>> HandleHeadCountAnonymousRegistration(
        Event @event,
        RegisterAnonymousAttendeeCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Lead name + HeadCount payload required.
        if (string.IsNullOrWhiteSpace(request.LeadAttendeeName))
            return Result<string?>.Failure(
                $"Lead attendee name is required for {@event.RegistrationMode} events.");

        if (request.HeadCount == null)
            return Result<string?>.Failure(
                $"Head-count breakdown is required for {@event.RegistrationMode} events.");

        // 2. Build the contact value object (same shape as multi-attendee anonymous).
        var contactResult = RegistrationContact.Create(
            request.Email,
            request.PhoneNumber,
            request.Address);
        if (contactResult.IsFailure)
            return Result<string?>.Failure(contactResult.Errors);

        // 3. Resolve tier counts (snapshot tier names from event.TicketTiers).
        var hc = request.HeadCount;
        IReadOnlyList<TierCount>? tierCounts = null;
        if (hc.TierCounts != null && hc.TierCounts.Count > 0)
        {
            var resolvedTiers = new List<TierCount>();
            foreach (var tcDto in hc.TierCounts)
            {
                var tier = @event.TicketTiers.FirstOrDefault(t => t.Id == tcDto.TierId);
                if (tier == null)
                    return Result<string?>.Failure($"Ticket tier {tcDto.TierId} not found on this event.");
                var tcResult = TierCount.Create(tier.Id, tier.Name, tcDto.Count);
                if (tcResult.IsFailure)
                    return Result<string?>.Failure(tcResult.Errors);
                resolvedTiers.Add(tcResult.Value);
            }
            tierCounts = resolvedTiers;
        }

        // 4. Build HeadCountBreakdown via the mode-specific factory.
        Result<HeadCountBreakdown> hcResult;
        switch (@event.RegistrationMode)
        {
            case LankaConnect.Domain.Events.Enums.RegistrationMode.HeadCountOnly:
                if (!hc.Total.HasValue)
                    return Result<string?>.Failure("Total head-count is required for HeadCountOnly mode.");
                hcResult = HeadCountBreakdown.ForTotalOnly(hc.Total.Value, tierCounts);
                break;
            case LankaConnect.Domain.Events.Enums.RegistrationMode.HeadCountByAge:
                if (!hc.Adults.HasValue || !hc.Children.HasValue)
                    return Result<string?>.Failure("Adults and Children counts are required for HeadCountByAge mode.");
                hcResult = HeadCountBreakdown.ForByAge(hc.Adults.Value, hc.Children.Value, tierCounts);
                break;
            case LankaConnect.Domain.Events.Enums.RegistrationMode.HeadCountByGender:
                if (!hc.Males.HasValue || !hc.Females.HasValue)
                    return Result<string?>.Failure("Males and Females counts are required for HeadCountByGender mode.");
                hcResult = HeadCountBreakdown.ForByGender(hc.Males.Value, hc.Females.Value, tierCounts);
                break;
            case LankaConnect.Domain.Events.Enums.RegistrationMode.HeadCountByAgeAndGender:
                if (!hc.AdultMales.HasValue || !hc.AdultFemales.HasValue ||
                    !hc.ChildMales.HasValue || !hc.ChildFemales.HasValue)
                    return Result<string?>.Failure(
                        "AdultMales, AdultFemales, ChildMales, and ChildFemales counts are required for HeadCountByAgeAndGender mode.");
                hcResult = HeadCountBreakdown.ForByAgeAndGender(
                    hc.AdultMales.Value, hc.AdultFemales.Value, hc.ChildMales.Value, hc.ChildFemales.Value,
                    tierCounts);
                break;
            default:
                return Result<string?>.Failure(
                    $"Unexpected registration mode in head-count anonymous handler: {@event.RegistrationMode}");
        }

        if (hcResult.IsFailure)
            return Result<string?>.Failure(hcResult.Errors);

        // 5. Delegate to Event.RegisterWithHeadCount (anonymous: userId = null).
        // The domain method enforces status / date / mode / duplicate / capacity / paid-event guards.
        var registerResult = @event.RegisterWithHeadCount(
            userId: null,
            request.LeadAttendeeName!,
            hcResult.Value,
            contactResult.Value);

        if (registerResult.IsFailure)
            return Result<string?>.Failure(registerResult.Errors);

        _eventRepository.Update(@event);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result<string?>.Success(null); // Free path returns null URL.
    }
}
