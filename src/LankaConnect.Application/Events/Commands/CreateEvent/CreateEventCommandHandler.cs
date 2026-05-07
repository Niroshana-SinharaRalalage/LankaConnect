using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Services; // Phase 6A.X: Revenue breakdown
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.Enums;
using LankaConnect.Domain.Communications; // Phase 6A.32: Email groups
using LankaConnect.Domain.Communications.Entities; // Phase 6A.32: EmailGroup entity
using Microsoft.EntityFrameworkCore; // Phase 6A.32: ChangeTracker API for shadow navigation
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.CreateEvent;

public class CreateEventCommandHandler : ICommandHandler<CreateEventCommand, Guid>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailGroupRepository _emailGroupRepository; // Phase 6A.32: Email groups
    private readonly IApplicationDbContext _dbContext; // Phase 6A.32: ChangeTracker API
    private readonly IRevenueCalculatorService _revenueCalculatorService; // Phase 6A.X: Revenue breakdown
    private readonly ITimeZoneLookupService _timeZoneLookupService; // Issue #55: Timezone lookup
    private readonly ILogger<CreateEventCommandHandler> _logger;

    public CreateEventCommandHandler(
        IEventRepository eventRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IEmailGroupRepository emailGroupRepository, // Phase 6A.32: Email groups
        IApplicationDbContext dbContext, // Phase 6A.32: ChangeTracker API
        IRevenueCalculatorService revenueCalculatorService, // Phase 6A.X: Revenue breakdown
        ITimeZoneLookupService timeZoneLookupService, // Issue #55: Timezone lookup
        ILogger<CreateEventCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _emailGroupRepository = emailGroupRepository; // Phase 6A.32: Email groups
        _dbContext = dbContext; // Phase 6A.32: ChangeTracker API
        _revenueCalculatorService = revenueCalculatorService; // Phase 6A.X: Revenue breakdown
        _timeZoneLookupService = timeZoneLookupService; // Issue #55: Timezone lookup
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "CreateEvent"))
        using (LogContext.PushProperty("EntityType", "Event"))
        using (LogContext.PushProperty("OrganizerId", request.OrganizerId))
        using (LogContext.PushProperty("EventTitle", request.Title))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "CreateEvent START: OrganizerId={OrganizerId}, Title={Title}, Category={Category}, StartDate={StartDate}",
                request.OrganizerId, request.Title, request.Category ?? EventCategory.Community, request.StartDate);

            try
            {
                // Check for cancellation at the start
                cancellationToken.ThrowIfCancellationRequested();

                // Validate user can create events based on role
                var user = await _userRepository.GetByIdAsync(request.OrganizerId, cancellationToken);
                if (user == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CreateEvent FAILED: User not found - OrganizerId={OrganizerId}, Duration={ElapsedMs}ms",
                        request.OrganizerId, stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Failure("User not found");
                }

                // Check if user has permission to create events (EventOrganizer or Admin roles)
                if (!user.Role.CanCreateEvents())
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CreateEvent FAILED: Insufficient permissions - OrganizerId={OrganizerId}, Role={Role}, Duration={ElapsedMs}ms",
                        request.OrganizerId, user.Role, stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Failure("You do not have permission to create events. Only Event Organizers and Administrators can create events.");
                }

                // Create EventTitle value object
                var titleResult = EventTitle.Create(request.Title);
                if (titleResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CreateEvent VALIDATION FAILED: Invalid title - OrganizerId={OrganizerId}, Title={Title}, Error={Error}, Duration={ElapsedMs}ms",
                        request.OrganizerId, request.Title, titleResult.Error, stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Failure(titleResult.Error);
                }

                // Create EventDescription value object
                var descriptionResult = EventDescription.Create(request.Description);
                if (descriptionResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CreateEvent VALIDATION FAILED: Invalid description - OrganizerId={OrganizerId}, Duration={ElapsedMs}ms",
                        request.OrganizerId, stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Failure(descriptionResult.Error);
                }

        // Create EventLocation if location data provided
        EventLocation? location = null;
        if (!string.IsNullOrWhiteSpace(request.LocationAddress) &&
            !string.IsNullOrWhiteSpace(request.LocationCity))
        {
            var addressResult = Address.Create(
                request.LocationAddress,
                request.LocationCity,
                request.LocationState ?? string.Empty,
                request.LocationZipCode ?? string.Empty,
                request.LocationCountry ?? "Sri Lanka"
            );

            if (addressResult.IsFailure)
                return Result<Guid>.Failure(addressResult.Error);

            // Create GeoCoordinate if lat/long provided
            GeoCoordinate? coordinates = null;
            if (request.LocationLatitude.HasValue && request.LocationLongitude.HasValue)
            {
                var coordinatesResult = GeoCoordinate.Create(
                    request.LocationLatitude.Value,
                    request.LocationLongitude.Value
                );

                if (coordinatesResult.IsFailure)
                    return Result<Guid>.Failure(coordinatesResult.Error);

                coordinates = coordinatesResult.Value;
            }

            var locationResult = EventLocation.Create(addressResult.Value, coordinates, request.LocationName);
            if (locationResult.IsFailure)
                return Result<Guid>.Failure(locationResult.Error);

            location = locationResult.Value;
        }

        // Phase 7C.1: Build optional secondary location (parking lot or secondary venue)
        EventSecondaryLocation? secondaryLocation = null;
        if (request.SecondaryLocationType.HasValue)
        {
            if (string.IsNullOrWhiteSpace(request.SecondaryLocationAddress) ||
                string.IsNullOrWhiteSpace(request.SecondaryLocationCity))
            {
                return Result<Guid>.Failure("Secondary location address and city are required when a secondary location type is selected");
            }

            var secAddressResult = Address.Create(
                request.SecondaryLocationAddress,
                request.SecondaryLocationCity,
                request.SecondaryLocationState ?? string.Empty,
                request.SecondaryLocationZipCode ?? string.Empty,
                request.SecondaryLocationCountry ?? "Sri Lanka"
            );
            if (secAddressResult.IsFailure)
                return Result<Guid>.Failure(secAddressResult.Error);

            GeoCoordinate? secCoordinates = null;
            if (request.SecondaryLocationLatitude.HasValue && request.SecondaryLocationLongitude.HasValue)
            {
                var secCoordsResult = GeoCoordinate.Create(
                    request.SecondaryLocationLatitude.Value,
                    request.SecondaryLocationLongitude.Value
                );
                if (secCoordsResult.IsFailure)
                    return Result<Guid>.Failure(secCoordsResult.Error);
                secCoordinates = secCoordsResult.Value;
            }

            var secInnerLocationResult = EventLocation.Create(secAddressResult.Value, secCoordinates, request.SecondaryLocationName);
            if (secInnerLocationResult.IsFailure)
                return Result<Guid>.Failure(secInnerLocationResult.Error);

            var secondaryResult = EventSecondaryLocation.Create(request.SecondaryLocationType.Value, secInnerLocationResult.Value);
            if (secondaryResult.IsFailure)
                return Result<Guid>.Failure(secondaryResult.Error);

            secondaryLocation = secondaryResult.Value;
        }

        // Phase 6D: Handle pricing - group pricing takes precedence over dual and legacy single pricing
        Money? ticketPrice = null;
        TicketPricing? pricing = null;
        bool isGroupPricing = false;

        // Phase 6D: Check if group pricing tiers are provided (highest priority)
        if (request.GroupPricingTiers != null && request.GroupPricingTiers.Count > 0)
        {
            // Phase 6A.X Issue #22: Validate tier max attendees against event capacity
            foreach (var tierRequest in request.GroupPricingTiers)
            {
                if (tierRequest.MaxAttendees.HasValue && tierRequest.MaxAttendees.Value > request.Capacity)
                {
                    _logger.LogWarning(
                        "CreateEvent VALIDATION FAILED: Tier maxAttendees ({TierMax}) exceeds event capacity ({Capacity})",
                        tierRequest.MaxAttendees.Value, request.Capacity);
                    return Result<Guid>.Failure($"Pricing tier maximum ({tierRequest.MaxAttendees.Value}) cannot exceed event capacity ({request.Capacity})");
                }
                if (tierRequest.MinAttendees > request.Capacity)
                {
                    _logger.LogWarning(
                        "CreateEvent VALIDATION FAILED: Tier minAttendees ({TierMin}) exceeds event capacity ({Capacity})",
                        tierRequest.MinAttendees, request.Capacity);
                    return Result<Guid>.Failure($"Pricing tier minimum ({tierRequest.MinAttendees}) cannot exceed event capacity ({request.Capacity})");
                }
            }

            // Build GroupPricingTier objects from request
            var tiers = new List<GroupPricingTier>();
            var currency = request.GroupPricingTiers[0].Currency; // Use currency from first tier

            foreach (var tierRequest in request.GroupPricingTiers)
            {
                var priceResult = Money.Create(tierRequest.PricePerPerson, tierRequest.Currency);
                if (priceResult.IsFailure)
                    return Result<Guid>.Failure(priceResult.Error);

                var tierResult = GroupPricingTier.Create(
                    tierRequest.MinAttendees,
                    tierRequest.MaxAttendees,
                    priceResult.Value
                );

                if (tierResult.IsFailure)
                    return Result<Guid>.Failure(tierResult.Error);

                tiers.Add(tierResult.Value);
            }

            // Phase 6A.X Issue #34: Use overloaded method with maxAttendeesPerRegistration to validate tier coverage
            // Tiers should cover the max attendees per registration, not total event capacity
            var maxAttendeesPerReg = request.MaxAttendeesPerRegistration ?? 10; // Default to 10 if not specified
            var groupPricingResult = TicketPricing.CreateGroupTiered(tiers, currency, maxAttendeesPerReg);
            if (groupPricingResult.IsFailure)
            {
                _logger.LogWarning(
                    "CreateEvent VALIDATION FAILED: Group pricing tiers do not cover max attendees per registration ({MaxAttendeesPerReg}) - Error={Error}",
                    maxAttendeesPerReg, groupPricingResult.Error);
                return Result<Guid>.Failure(groupPricingResult.Error);
            }

            pricing = groupPricingResult.Value;
            isGroupPricing = true;
        }
        // Session 21: Check if dual pricing fields are provided
        else if (request.AdultPriceAmount.HasValue && request.AdultPriceCurrency.HasValue)
        {
            // Build dual pricing using TicketPricing value object
            var adultPriceResult = Money.Create(request.AdultPriceAmount.Value, request.AdultPriceCurrency.Value);
            if (adultPriceResult.IsFailure)
                return Result<Guid>.Failure(adultPriceResult.Error);

            Money? childPrice = null;
            if (request.ChildPriceAmount.HasValue && request.ChildPriceCurrency.HasValue)
            {
                var childPriceResult = Money.Create(request.ChildPriceAmount.Value, request.ChildPriceCurrency.Value);
                if (childPriceResult.IsFailure)
                    return Result<Guid>.Failure(childPriceResult.Error);

                childPrice = childPriceResult.Value;
            }

            var pricingResult = TicketPricing.Create(adultPriceResult.Value, childPrice, request.ChildAgeLimit);
            if (pricingResult.IsFailure)
                return Result<Guid>.Failure(pricingResult.Error);

            pricing = pricingResult.Value;
        }
        // Fallback to legacy single pricing format if dual pricing not provided
        else if (request.TicketPriceAmount.HasValue && request.TicketPriceCurrency.HasValue)
        {
            var moneyResult = Money.Create(request.TicketPriceAmount.Value, request.TicketPriceCurrency.Value);
            if (moneyResult.IsFailure)
                return Result<Guid>.Failure(moneyResult.Error);

            // Convert legacy format to new TicketPricing format (single pricing = childPrice null)
            var pricingResult = TicketPricing.Create(moneyResult.Value, null, null);
            if (pricingResult.IsFailure)
                return Result<Guid>.Failure(pricingResult.Error);

            pricing = pricingResult.Value;
            ticketPrice = moneyResult.Value; // Keep for backward compatibility with Event.Create
        }

        // Determine category (use provided or default to Community)
        var category = request.Category ?? EventCategory.Community;

        // Phase 7E.2: Validate the requested registration mode against the event shape BEFORE
        // we create the aggregate — fail fast on incompatible combinations (the 14-row
        // compatibility table from the Phase 7E plan §2). The compatibility helper is the
        // single source of truth, also used by UpdateEventCommandHandler and
        // GetAllowedRegistrationModesQueryHandler.
        var requestedRegistrationMode = request.RegistrationMode ?? RegistrationMode.DetailedAttendees;
        var registrationModeContext = new LankaConnect.Domain.Events.Services.RegistrationModeContext
        {
            // Free attendance iff: caller said IsFree=true, OR no pricing/ticket price was provided.
            IsFreeAttendance = request.IsFree == true || (pricing == null && ticketPrice == null),
            HasDualPricing = pricing != null && pricing.HasChildPricing,
            HasGroupTiers = isGroupPricing,
            // The remaining axes (seating, named-seating, per-ticket name, identity-bound add-on,
            // ticket tiers, matrix pricing) aren't part of CreateEvent's request DTO today —
            // they're set via separate commands (SetSeatingLayout / CreateTicketTier / etc.).
            // Compatibility for those flows is enforced when those commands run; for the create
            // flow, we treat them as absent by default. Phase 7F will revisit if create-time
            // shape becomes richer.
        };
        var modeCompatibilityResult = LankaConnect.Domain.Events.Services
            .RegistrationModeCompatibility.Check(requestedRegistrationMode, registrationModeContext);
        if (modeCompatibilityResult.IsFailure)
        {
            stopwatch.Stop();
            _logger.LogWarning(
                "CreateEvent VALIDATION FAILED: incompatible registration mode - OrganizerId={OrganizerId}, " +
                "RequestedMode={Mode}, Reason={Reason}, Duration={ElapsedMs}ms",
                request.OrganizerId, requestedRegistrationMode, modeCompatibilityResult.Error,
                stopwatch.ElapsedMilliseconds);
            return Result<Guid>.Failure(modeCompatibilityResult.Error);
        }

        // Create Event aggregate
        var eventResult = Event.Create(
            titleResult.Value,
            descriptionResult.Value,
            request.StartDate,
            request.EndDate,
            request.OrganizerId,
            request.Capacity,
            location,
            category,
            ticketPrice // Pass legacy ticketPrice for backward compatibility
        );

        if (eventResult.IsFailure)
            return Result<Guid>.Failure(eventResult.Error);

        // Phase 7C.1: Persist secondary location if built
        if (secondaryLocation != null)
        {
            var setSecondaryResult = eventResult.Value.SetSecondaryLocation(secondaryLocation);
            if (setSecondaryResult.IsFailure)
                return Result<Guid>.Failure(setSecondaryResult.Error);

            _logger.LogInformation(
                "CreateEvent: Secondary location configured - EventId={EventId}, Type={Type}",
                eventResult.Value.Id, secondaryLocation.Type);
        }

        // Phase 8X.4b — Resolve effective payment mode (validator already enforced consistency).
        // Security default per Phase 6A.81: missing PaymentMode + non-true IsFree → OnPlatformPaid.
        var effectivePaymentMode = CreateEventCommandValidator
            .InferPaymentMode(request.IsFree, request.PaymentMode).Mode;

        // Phase 8X.4b — ExternalPaid branch: bundles pricing + VO + RegistrationMode coercion in one call.
        // SetExternalPayment internally dispatches to SetDualPricing / SetGroupPricing, so the
        // legacy pricing block below is skipped for this branch.
        if (effectivePaymentMode == EventPaymentMode.ExternalPaid)
        {
            if (pricing == null)
            {
                _logger.LogWarning(
                    "CreateEvent: ExternalPaid requested but no pricing supplied — OrganizerId={OrganizerId}",
                    request.OrganizerId);
                return Result<Guid>.Failure("Pricing is required for ExternalPaid events");
            }

            ExternalRegistration externalReg;
            try
            {
                var voResult = ExternalRegistration.Create(
                    request.ExternalRegistrationUrl,
                    request.ExternalRegistrationInstructions,
                    request.ExternalRegistrationVendorName);
                if (voResult.IsFailure)
                {
                    _logger.LogWarning(
                        "CreateEvent: ExternalRegistration VO rejected - OrganizerId={OrganizerId}, Reason={Reason}",
                        request.OrganizerId, voResult.Error);
                    return Result<Guid>.Failure(voResult.Error);
                }
                externalReg = voResult.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "CreateEvent: ExternalRegistration VO creation faulted - OrganizerId={OrganizerId}",
                    request.OrganizerId);
                return Result<Guid>.Failure("Failed to validate external registration details");
            }

            var setExternalResult = eventResult.Value.SetExternalPayment(externalReg, pricing);
            if (setExternalResult.IsFailure)
            {
                _logger.LogWarning(
                    "CreateEvent: SetExternalPayment domain rejection - OrganizerId={OrganizerId}, Error={Error}",
                    request.OrganizerId, setExternalResult.Error);
                return Result<Guid>.Failure(setExternalResult.Error);
            }

            _logger.LogInformation(
                "CreateEvent: ExternalPaid configured - EventId={EventId}, Url={Url}, Vendor={Vendor}",
                eventResult.Value.Id, externalReg.Url, externalReg.VendorName ?? "(none)");
        }
        // Phase 6D + Session 21: Set pricing if provided
        else if (pricing != null)
        {
            Result setPricingResult;
            if (isGroupPricing)
            {
                // Phase 6D: Use SetGroupPricing for group tiered pricing
                setPricingResult = eventResult.Value.SetGroupPricing(pricing);
            }
            else
            {
                // Session 21: Use SetDualPricing for dual or single pricing
                setPricingResult = eventResult.Value.SetDualPricing(pricing);
            }

            if (setPricingResult.IsFailure)
                return Result<Guid>.Failure(setPricingResult.Error);

            // Phase 6A.X: Calculate and store revenue breakdown for paid events
            // Use adult price for dual pricing, first tier for group pricing, or single price
            Money? priceForBreakdown = null;
            if (isGroupPricing && pricing.GroupTiers != null && pricing.GroupTiers.Count > 0)
            {
                // Use first tier price for group pricing breakdown preview
                priceForBreakdown = pricing.GroupTiers[0].PricePerPerson;
            }
            else if (pricing.AdultPrice != null)
            {
                priceForBreakdown = pricing.AdultPrice;
            }

            if (priceForBreakdown != null && priceForBreakdown.Amount > 0)
            {
                var breakdownResult = await _revenueCalculatorService.CalculateBreakdownAsync(
                    priceForBreakdown,
                    location,
                    cancellationToken);

                if (breakdownResult.IsSuccess)
                {
                    var setBreakdownResult = eventResult.Value.SetRevenueBreakdown(breakdownResult.Value);
                    if (setBreakdownResult.IsFailure)
                    {
                        // Log warning but don't fail event creation - breakdown is informational
                        // Revenue breakdown failure shouldn't block event creation
                    }
                }
                // Note: Tax lookup failures don't block event creation - breakdown is informational
            }
        }

        // IsFreeEvent fix: Explicitly mark as free event when frontend sends IsFree=true
        // This is needed because Event.Create() defaults IsFreeEvent=false when ticketPrice is null
        if (request.IsFree == true && pricing == null)
        {
            var setFreeResult = eventResult.Value.SetAsFreeEvent();
            if (setFreeResult.IsFailure)
                return Result<Guid>.Failure(setFreeResult.Error);

            _logger.LogInformation(
                "CreateEvent: Event marked as free - EventId={EventId}",
                eventResult.Value.Id);
        }

        // Phase 8X.4b — Sync PaymentMode to keep Option B (PaymentMode source-of-truth)
        // in lockstep with IsFreeEvent. ExternalPaid already handled in the dedicated
        // branch above; Free is the default; only OnPlatformPaid needs an explicit flip.
        // SetPaymentMode is idempotent on same-mode set, so calling it for Free is a no-op.
        if (effectivePaymentMode == EventPaymentMode.OnPlatformPaid)
        {
            var setPaymentModeResult = eventResult.Value.SetPaymentMode(EventPaymentMode.OnPlatformPaid);
            if (setPaymentModeResult.IsFailure)
            {
                _logger.LogError(
                    "CreateEvent: SetPaymentMode(OnPlatformPaid) failed - EventId={EventId}, Error={Error}",
                    eventResult.Value.Id, setPaymentModeResult.Error);
                return Result<Guid>.Failure(setPaymentModeResult.Error);
            }
        }

        // Phase 7E.2: Apply the validated registration mode (skip for DetailedAttendees — that's
        // the default and the property's no-op idempotent path; saves a domain-event roundtrip).
        if (requestedRegistrationMode != RegistrationMode.DetailedAttendees)
        {
            var setModeResult = eventResult.Value.SetRegistrationMode(requestedRegistrationMode);
            if (setModeResult.IsFailure)
            {
                // SetRegistrationMode only fails if registrations exist — impossible during Create.
                // Surfacing as 500-equivalent: indicates a domain invariant violation.
                _logger.LogError(
                    "CreateEvent: SetRegistrationMode unexpectedly failed during create - EventId={EventId}, Mode={Mode}, Error={Error}",
                    eventResult.Value.Id, requestedRegistrationMode, setModeResult.Error);
                return Result<Guid>.Failure(setModeResult.Error);
            }

            _logger.LogInformation(
                "CreateEvent: RegistrationMode set - EventId={EventId}, Mode={Mode}",
                eventResult.Value.Id, requestedRegistrationMode);
        }

        // Phase 6A.32/33: Validate and assign email groups
        if (request.EmailGroupIds != null && request.EmailGroupIds.Any())
        {
            var distinctGroupIds = request.EmailGroupIds.Distinct().ToList();

            // Load EmailGroup entities for validation
            var dbContext = _dbContext as Microsoft.EntityFrameworkCore.DbContext
                ?? throw new InvalidOperationException("DbContext must be EF Core DbContext");

            var emailGroups = await dbContext.Set<EmailGroup>()
                .Where(g => distinctGroupIds.Contains(g.Id))
                .ToListAsync(cancellationToken);

            // Validate all groups exist, belong to organizer, and are active
            foreach (var groupId in distinctGroupIds)
            {
                var emailGroup = emailGroups.FirstOrDefault(g => g.Id == groupId);

                if (emailGroup == null)
                    return Result<Guid>.Failure($"Email group with ID {groupId} not found");

                if (emailGroup.OwnerId != request.OrganizerId)
                    return Result<Guid>.Failure($"You do not have permission to use email group '{emailGroup.Name}'");

                if (!emailGroup.IsActive)
                    return Result<Guid>.Failure($"Email group '{emailGroup.Name}' is inactive and cannot be used");
            }

            // Assign email group IDs to domain model (for business logic)
            var assignResult = eventResult.Value.SetEmailGroups(distinctGroupIds);
            if (assignResult.IsFailure)
                return Result<Guid>.Failure(assignResult.Error);
        }

        // Set organizer contacts if provided
        if (request.PublishOrganizerContact.GetValueOrDefault() && request.OrganizerContacts?.Any() == true)
        {
            var contacts = request.OrganizerContacts
                .Select(c => (c.ContactName, c.ContactEmail, c.ContactPhone, c.LinkedUserId, c.IsPrimary))
                .ToList();

            var contactResult = eventResult.Value.SetOrganizerContacts(
                publishContact: true,
                contacts);

            if (contactResult.IsFailure)
                return Result<Guid>.Failure(contactResult.Error);
        }

        // Donation Feature: Set donation configuration if enabled
        if (request.DonationsEnabled == true)
        {
            var donationConfigResult = DonationConfiguration.Create(
                isEnabled: true,
                suggestedAmounts: request.DonationSuggestedAmounts,
                allowCustomAmount: request.DonationAllowCustomAmount ?? true,
                minAmount: request.DonationMinAmount,
                maxAmount: request.DonationMaxAmount,
                donationMessage: request.DonationMessage,
                showDonationSummary: request.ShowDonationSummary ?? false);

            if (donationConfigResult.IsFailure)
                return Result<Guid>.Failure(donationConfigResult.Error);

            var setDonationResult = eventResult.Value.SetDonationConfiguration(donationConfigResult.Value);
            if (setDonationResult.IsFailure)
                return Result<Guid>.Failure(setDonationResult.Error);

            _logger.LogInformation(
                "CreateEvent: Donation configuration set - EventId={EventId}, MinAmount={MinAmount}, MaxAmount={MaxAmount}",
                eventResult.Value.Id, request.DonationMinAmount, request.DonationMaxAmount);
        }

        // Issue #55: Set TimeZoneId based on event location state
        // This ensures emails and frontend display times in the correct timezone
        try
        {
            string timeZoneId;
            if (location?.Address?.State != null && !string.IsNullOrWhiteSpace(location.Address.State))
            {
                // Physical event: derive timezone from state
                timeZoneId = _timeZoneLookupService.GetTimeZoneFromState(location.Address.State);
                _logger.LogDebug(
                    "CreateEvent: Setting TimeZoneId based on state - State={State}, TimeZoneId={TimeZoneId}",
                    location.Address.State, timeZoneId);
            }
            else
            {
                // Virtual event: use default timezone (Eastern - most Sri Lankan communities in USA)
                timeZoneId = _timeZoneLookupService.DefaultTimeZoneId;
                _logger.LogDebug(
                    "CreateEvent: Setting default TimeZoneId for virtual event - TimeZoneId={TimeZoneId}",
                    timeZoneId);
            }

            var setTzResult = eventResult.Value.SetTimeZone(timeZoneId);
            if (setTzResult.IsFailure)
            {
                // Log warning but don't fail event creation - timezone is informational
                _logger.LogWarning(
                    "CreateEvent: Failed to set TimeZoneId - State={State}, TimeZoneId={TimeZoneId}, Error={Error}",
                    location?.Address?.State, timeZoneId, setTzResult.Error);
            }
        }
        catch (Exception tzEx)
        {
            // Log warning but don't fail event creation - timezone is informational
            _logger.LogWarning(tzEx,
                "CreateEvent: Exception setting TimeZoneId - State={State}",
                location?.Address?.State);
        }

                // Add EventId to LogContext now that we have it
                using (LogContext.PushProperty("EventId", eventResult.Value.Id))
                {
                    _logger.LogInformation(
                        "CreateEvent: Event aggregate created - EventId={EventId}, Title={Title}, Category={Category}",
                        eventResult.Value.Id, request.Title, eventResult.Value.Category);

                    // Phase 6A.33 FIX: Repository.AddAsync now handles email group shadow navigation sync
                    // No manual EF Core state manipulation needed - repository pattern handles it
                    await _eventRepository.AddAsync(eventResult.Value, cancellationToken);

                    // Commit changes (EF Core now detects changes via ChangeTracker)
                    await _unitOfWork.CommitAsync(cancellationToken);

                    stopwatch.Stop();

                    _logger.LogInformation(
                        "CreateEvent COMPLETE: EventId={EventId}, OrganizerId={OrganizerId}, Title={Title}, Category={Category}, " +
                        "StartDate={StartDate}, Capacity={Capacity}, HasPricing={HasPricing}, EmailGroupsCount={EmailGroupsCount}, Duration={ElapsedMs}ms",
                        eventResult.Value.Id, request.OrganizerId, request.Title, eventResult.Value.Category,
                        request.StartDate, request.Capacity, pricing != null, request.EmailGroupIds?.Count ?? 0, stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Success(eventResult.Value.Id);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "CreateEvent FAILED: Exception occurred - OrganizerId={OrganizerId}, Title={Title}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.OrganizerId, request.Title, stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }
}
