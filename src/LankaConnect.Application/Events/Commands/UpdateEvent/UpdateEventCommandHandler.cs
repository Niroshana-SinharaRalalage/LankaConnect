using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Commands.CreateEvent;
using LankaConnect.Application.Events.Commands.UpdateEventOrganizerContact;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here
using LankaConnect.Domain.Events.Enums; // Phase 8X: EventPaymentMode
using LankaConnect.Domain.Events.Services; // Phase 6A.X: Revenue breakdown
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Modules.Communications.Contracts; // Wave 5.4.d.1: IEmailGroupQueries swap
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.UpdateEvent;

public class UpdateEventCommandHandler : ICommandHandler<UpdateEventCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailGroupQueries _emailGroupQueries; // Wave 5.4.d.1
    private readonly IApplicationDbContext _dbContext; // legacy injection (unused after W5.4.d.1 but kept for DI compatibility until W5.4.d.3 cleanup)
    private readonly IRevenueCalculatorService _revenueCalculatorService; // Phase 6A.X: Revenue breakdown
    private readonly ITimeZoneLookupService _timeZoneLookupService; // Issue #55: Timezone lookup
    private readonly ILogger<UpdateEventCommandHandler> _logger;

    public UpdateEventCommandHandler(
        IEventRepository eventRepository,
        IUnitOfWork unitOfWork,
        IEmailGroupQueries emailGroupQueries, // Wave 5.4.d.1
        IApplicationDbContext dbContext, // Phase 6A.32: ChangeTracker API
        IRevenueCalculatorService revenueCalculatorService, // Phase 6A.X: Revenue breakdown
        ITimeZoneLookupService timeZoneLookupService, // Issue #55: Timezone lookup
        ILogger<UpdateEventCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
        _emailGroupQueries = emailGroupQueries;
        _dbContext = dbContext; // Phase 6A.32: ChangeTracker API
        _revenueCalculatorService = revenueCalculatorService; // Phase 6A.X: Revenue breakdown
        _timeZoneLookupService = timeZoneLookupService; // Issue #55: Timezone lookup
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateEventCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UpdateEvent"))
        using (LogContext.PushProperty("EntityType", "Event"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("EventTitle", request.Title))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UpdateEvent START: EventId={EventId}, Title={Title}",
                request.EventId, request.Title);

            try
            {
                // Check for cancellation at the start
                cancellationToken.ThrowIfCancellationRequested();

                // Phase 6A.53 FIX: Retrieve event WITH CHANGE TRACKING (trackChanges: true)
                // This is required for EF Core to detect changes when we modify the entity
                var @event = await _eventRepository.GetByIdAsync(request.EventId, trackChanges: true, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "UpdateEvent FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result.Failure("Event not found");
                }

        // NOTE: Allowing updates for all event statuses
        // Future enhancement: Implement status-based field restrictions (see ADR-011)
        // For now, organizers can update events regardless of status

        // Create updated value objects
        var titleResult = EventTitle.Create(request.Title);
        if (titleResult.IsFailure)
            return Result.Failure(titleResult.Error);

        var descriptionResult = EventDescription.Create(request.Description);
        if (descriptionResult.IsFailure)
            return Result.Failure(descriptionResult.Error);

        // Phase 8YA.2: TBD-dates handling. Validator already rejected the mixed case;
        // here we branch on the both-null vs both-set paths.
        // - Both null: leave existing dates unchanged (organiser is updating other fields).
        // - Both set: validate now-vs-start + end-vs-start, then SetDates() below.
        var bothDatesProvided = request.StartDate.HasValue && request.EndDate.HasValue;
        if (bothDatesProvided)
        {
            if (request.StartDate!.Value <= DateTime.UtcNow)
                return Result.Failure("Start date cannot be in the past");

            if (request.EndDate!.Value <= request.StartDate.Value)
                return Result.Failure("End date must be after start date");
        }

        if (request.Capacity <= 0)
            return Result.Failure("Capacity must be greater than 0");

        // Check capacity against current registrations
        if (request.Capacity < @event.CurrentRegistrations)
            return Result.Failure("Cannot reduce capacity below current registrations");

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
                return Result.Failure(addressResult.Error);

            GeoCoordinate? coordinates = null;
            if (request.LocationLatitude.HasValue && request.LocationLongitude.HasValue)
            {
                var coordinatesResult = GeoCoordinate.Create(
                    request.LocationLatitude.Value,
                    request.LocationLongitude.Value
                );

                if (coordinatesResult.IsFailure)
                    return Result.Failure(coordinatesResult.Error);

                coordinates = coordinatesResult.Value;
            }

            var locationResult = EventLocation.Create(addressResult.Value, coordinates, request.LocationName);
            if (locationResult.IsFailure)
                return Result.Failure(locationResult.Error);

            location = locationResult.Value;
        }

        // Phase 7C.1: Build optional secondary location (null Type means "clear")
        EventSecondaryLocation? secondaryLocation = null;
        if (request.SecondaryLocationType.HasValue)
        {
            if (string.IsNullOrWhiteSpace(request.SecondaryLocationAddress) ||
                string.IsNullOrWhiteSpace(request.SecondaryLocationCity))
            {
                return Result.Failure("Secondary location address and city are required when a secondary location type is selected");
            }

            var secAddressResult = Address.Create(
                request.SecondaryLocationAddress,
                request.SecondaryLocationCity,
                request.SecondaryLocationState ?? string.Empty,
                request.SecondaryLocationZipCode ?? string.Empty,
                request.SecondaryLocationCountry ?? "Sri Lanka"
            );
            if (secAddressResult.IsFailure)
                return Result.Failure(secAddressResult.Error);

            GeoCoordinate? secCoordinates = null;
            if (request.SecondaryLocationLatitude.HasValue && request.SecondaryLocationLongitude.HasValue)
            {
                var secCoordsResult = GeoCoordinate.Create(
                    request.SecondaryLocationLatitude.Value,
                    request.SecondaryLocationLongitude.Value
                );
                if (secCoordsResult.IsFailure)
                    return Result.Failure(secCoordsResult.Error);
                secCoordinates = secCoordsResult.Value;
            }

            var secInnerLocationResult = EventLocation.Create(secAddressResult.Value, secCoordinates, request.SecondaryLocationName);
            if (secInnerLocationResult.IsFailure)
                return Result.Failure(secInnerLocationResult.Error);

            var secondaryResult = EventSecondaryLocation.Create(request.SecondaryLocationType.Value, secInnerLocationResult.Value);
            if (secondaryResult.IsFailure)
                return Result.Failure(secondaryResult.Error);

            secondaryLocation = secondaryResult.Value;
        }

        // Session 33: Handle pricing - group pricing takes precedence over dual and legacy single pricing
        Money? ticketPrice = null;
        TicketPricing? pricing = null;
        bool isGroupPricing = false;

        // Session 33: Check if group pricing tiers are provided (highest priority)
        if (request.GroupPricingTiers != null && request.GroupPricingTiers.Count > 0)
        {
            // Phase 6A.X Issue #22: Validate tier max attendees against event capacity
            foreach (var tierRequest in request.GroupPricingTiers)
            {
                if (tierRequest.MaxAttendees.HasValue && tierRequest.MaxAttendees.Value > request.Capacity)
                {
                    _logger.LogWarning(
                        "UpdateEvent VALIDATION FAILED: Tier maxAttendees ({TierMax}) exceeds event capacity ({Capacity}) for EventId={EventId}",
                        tierRequest.MaxAttendees.Value, request.Capacity, request.EventId);
                    return Result.Failure($"Pricing tier maximum ({tierRequest.MaxAttendees.Value}) cannot exceed event capacity ({request.Capacity})");
                }
                if (tierRequest.MinAttendees > request.Capacity)
                {
                    _logger.LogWarning(
                        "UpdateEvent VALIDATION FAILED: Tier minAttendees ({TierMin}) exceeds event capacity ({Capacity}) for EventId={EventId}",
                        tierRequest.MinAttendees, request.Capacity, request.EventId);
                    return Result.Failure($"Pricing tier minimum ({tierRequest.MinAttendees}) cannot exceed event capacity ({request.Capacity})");
                }
            }

            // Build GroupPricingTier objects from request
            var tiers = new List<GroupPricingTier>();
            var currency = request.GroupPricingTiers[0].Currency; // Use currency from first tier

            foreach (var tierRequest in request.GroupPricingTiers)
            {
                var priceResult = Money.Create(tierRequest.PricePerPerson, tierRequest.Currency);
                if (priceResult.IsFailure)
                    return Result.Failure(priceResult.Error);

                var tierResult = GroupPricingTier.Create(
                    tierRequest.MinAttendees,
                    tierRequest.MaxAttendees,
                    priceResult.Value
                );

                if (tierResult.IsFailure)
                    return Result.Failure(tierResult.Error);

                tiers.Add(tierResult.Value);
            }

            // Phase 6A.X Issue #34: Use overloaded method with maxAttendeesPerRegistration to validate tier coverage
            // Tiers should cover the max attendees per registration, not total event capacity
            var maxAttendeesPerReg = request.MaxAttendeesPerRegistration ?? @event.MaxAttendeesPerRegistration;
            var groupPricingResult = TicketPricing.CreateGroupTiered(tiers, currency, maxAttendeesPerReg);
            if (groupPricingResult.IsFailure)
            {
                _logger.LogWarning(
                    "UpdateEvent VALIDATION FAILED: Group pricing tiers do not cover max attendees per registration ({MaxAttendeesPerReg}) for EventId={EventId} - Error={Error}",
                    maxAttendeesPerReg, request.EventId, groupPricingResult.Error);
                return Result.Failure(groupPricingResult.Error);
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
                return Result.Failure(adultPriceResult.Error);

            Money? childPrice = null;
            if (request.ChildPriceAmount.HasValue && request.ChildPriceCurrency.HasValue)
            {
                var childPriceResult = Money.Create(request.ChildPriceAmount.Value, request.ChildPriceCurrency.Value);
                if (childPriceResult.IsFailure)
                    return Result.Failure(childPriceResult.Error);

                childPrice = childPriceResult.Value;
            }

            var pricingResult = TicketPricing.Create(adultPriceResult.Value, childPrice, request.ChildAgeLimit);
            if (pricingResult.IsFailure)
                return Result.Failure(pricingResult.Error);

            pricing = pricingResult.Value;
        }
        // Fallback to legacy single pricing format if dual pricing not provided
        else if (request.TicketPriceAmount.HasValue && request.TicketPriceCurrency.HasValue)
        {
            var moneyResult = Money.Create(request.TicketPriceAmount.Value, request.TicketPriceCurrency.Value);
            if (moneyResult.IsFailure)
                return Result.Failure(moneyResult.Error);

            // Convert legacy format to new TicketPricing format (single pricing = childPrice null)
            var pricingResult = TicketPricing.Create(moneyResult.Value, null, null);
            if (pricingResult.IsFailure)
                return Result.Failure(pricingResult.Error);

            pricing = pricingResult.Value;
            ticketPrice = moneyResult.Value; // Keep for backward compatibility
        }

        // Update event (using reflection to set private setters - not ideal but works for now)
        // TODO: Add proper domain methods to Event entity for updates
        var titleProperty = typeof(Event).GetProperty(nameof(Event.Title));
        titleProperty?.SetValue(@event, titleResult.Value);

        var descriptionProperty = typeof(Event).GetProperty(nameof(Event.Description));
        descriptionProperty?.SetValue(@event, descriptionResult.Value);

        // Phase 8YA.2: route through Event.SetDates when both dates supplied so the
        // domain enforces UTC coercion + Planning → Draft transition. When dates are
        // omitted (both null), leave the event's existing dates unchanged — the
        // organiser is updating other fields and we never silently clear dates.
        if (bothDatesProvided)
        {
            var setDatesResult = @event.SetDates(request.StartDate!.Value, request.EndDate!.Value);
            if (setDatesResult.IsFailure)
                return Result.Failure(setDatesResult.Error);
        }

        var capacityResult = @event.UpdateCapacity(request.Capacity);
        if (capacityResult.IsFailure)
            return capacityResult;

        // Issue #51: Update max attendees per registration if provided
        if (request.MaxAttendeesPerRegistration.HasValue)
        {
            var maxAttendeesResult = @event.UpdateMaxAttendeesPerRegistration(request.MaxAttendeesPerRegistration.Value);
            if (maxAttendeesResult.IsFailure)
                return maxAttendeesResult;
        }

        // Phase 6A.154: Apply vanity slug only when organizer toggled the
        // update flag. Tri-state design mirrors UpdateRegistrationWindow
        // from 6A.153 — unrelated edits don't accidentally clear the slug.
        if (request.UpdateVanitySlug)
        {
            LankaConnect.Domain.Events.ValueObjects.EventVanitySlug? slug = null;
            if (!string.IsNullOrWhiteSpace(request.VanitySlug))
            {
                var slugResult = LankaConnect.Domain.Events.ValueObjects.EventVanitySlug.Create(request.VanitySlug);
                if (slugResult.IsFailure)
                {
                    _logger.LogWarning(
                        "UpdateEvent: VanitySlug rejected - EventId={EventId}, Slug={Slug}, Error={Error}",
                        @event.Id, request.VanitySlug, slugResult.Error);
                    return Result.Failure(slugResult.Error);
                }
                slug = slugResult.Value;
            }
            var setResult = @event.SetVanitySlug(slug);
            if (setResult.IsFailure)
            {
                _logger.LogWarning(
                    "UpdateEvent: SetVanitySlug rejected - EventId={EventId}, Slug={Slug}, Error={Error}",
                    @event.Id, request.VanitySlug, setResult.Error);
                return setResult;
            }
            _logger.LogInformation(
                "UpdateEvent: VanitySlug updated - EventId={EventId}, NewSlug={NewSlug}",
                @event.Id, request.VanitySlug ?? "(cleared)");
        }

        if (request.Category.HasValue)
        {
            var categoryProperty = typeof(Event).GetProperty(nameof(Event.Category));
            categoryProperty?.SetValue(@event, request.Category.Value);
        }

        // Update location
        if (location != null)
        {
            var setLocationResult = @event.SetLocation(location);
            if (setLocationResult.IsFailure)
                return setLocationResult;
        }
        else if (@event.HasLocation())
        {
            var removeLocationResult = @event.RemoveLocation();
            if (removeLocationResult.IsFailure)
                return removeLocationResult;
        }

        // Phase 7C.1: Apply secondary location (set-or-clear based on request.SecondaryLocationType)
        if (secondaryLocation != null)
        {
            var setSecondaryResult = @event.SetSecondaryLocation(secondaryLocation);
            if (setSecondaryResult.IsFailure)
                return setSecondaryResult;

            _logger.LogDebug(
                "UpdateEvent: Secondary location set - EventId={EventId}, Type={Type}",
                request.EventId, secondaryLocation.Type);
        }
        else if (@event.HasSecondaryLocation())
        {
            var clearSecondaryResult = @event.ClearSecondaryLocation();
            if (clearSecondaryResult.IsFailure)
                return clearSecondaryResult;

            _logger.LogDebug(
                "UpdateEvent: Secondary location cleared - EventId={EventId}",
                request.EventId);
        }

        // Issue #55: Update TimeZoneId when location changes
        // This ensures emails and frontend display times in the correct timezone
        try
        {
            if (location?.Address?.State != null && !string.IsNullOrWhiteSpace(location.Address.State))
            {
                // Location was set or updated - derive timezone from state
                var timeZoneId = _timeZoneLookupService.GetTimeZoneFromState(location.Address.State);
                var setTzResult = @event.SetTimeZone(timeZoneId);

                if (setTzResult.IsFailure)
                {
                    _logger.LogWarning(
                        "UpdateEvent: Failed to set TimeZoneId - EventId={EventId}, State={State}, TimeZoneId={TimeZoneId}, Error={Error}",
                        request.EventId, location.Address.State, timeZoneId, setTzResult.Error);
                }
                else
                {
                    _logger.LogDebug(
                        "UpdateEvent: Updated TimeZoneId based on state - EventId={EventId}, State={State}, TimeZoneId={TimeZoneId}",
                        request.EventId, location.Address.State, timeZoneId);
                }
            }
            // If location removed (converted to virtual), keep existing timezone
            // This maintains the original timezone for events that become virtual
        }
        catch (Exception tzEx)
        {
            // Log warning but don't fail event update - timezone is informational
            _logger.LogWarning(tzEx,
                "UpdateEvent: Exception setting TimeZoneId - EventId={EventId}, State={State}",
                request.EventId, location?.Address?.State);
        }

        // Phase 8X.4b — Resolve effective payment mode using the same inference helper as Create.
        var effectivePaymentMode = LankaConnect.Application.Events.Commands.CreateEvent.CreateEventCommandValidator
            .InferPaymentMode(request.IsFree, request.PaymentMode).Mode;

        // Phase 8X.4b — ExternalPaid branch: bundles pricing + VO + RegistrationMode coercion.
        // When transitioning INTO ExternalPaid, SetExternalPayment internally calls
        // ApplyPricingForExternalPayment which dispatches to SetDualPricing / SetGroupPricing.
        // For an existing ExternalPaid event whose URL/instructions are being updated, we still
        // call SetExternalPayment — the active-regs guard is naturally satisfied (ExternalPaid
        // events have no registrations by definition).
        if (request.PaymentMode == EventPaymentMode.ExternalPaid)
        {
            // Phase 8X.12 — pricing is optional. Null pricing + null existing pricing is now
            // a valid state (organiser publishes without on-platform price). When caller
            // passes null pricing on update, SetExternalPayment clears any stale legacy pricing.
            // Phase 8X.11 — externalReg may be null when organiser cleared all 3 fields.
            ExternalRegistration? externalReg = null;
            var allEmpty = string.IsNullOrWhiteSpace(request.ExternalRegistrationUrl)
                && string.IsNullOrWhiteSpace(request.ExternalRegistrationInstructions)
                && string.IsNullOrWhiteSpace(request.ExternalRegistrationVendorName);

            if (!allEmpty)
            {
                try
                {
                    var voResult = ExternalRegistration.Create(
                        request.ExternalRegistrationUrl,
                        request.ExternalRegistrationInstructions,
                        request.ExternalRegistrationVendorName);
                    if (voResult.IsFailure)
                    {
                        _logger.LogWarning(
                            "UpdateEvent: ExternalRegistration VO rejected - EventId={EventId}, Reason={Reason}",
                            request.EventId, voResult.Error);
                        return Result.Failure(voResult.Error);
                    }
                    externalReg = voResult.Value;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "UpdateEvent: ExternalRegistration VO creation faulted - EventId={EventId}",
                        request.EventId);
                    return Result.Failure("Failed to validate external registration details");
                }
            }

            // Phase 8X.12 — caller-as-source-of-truth: pricing reflects form intent.
            // - Form submits existing pricing values when user didn't touch the field
            //   (the FE pre-populates from the loaded event).
            // - Form submits null when user explicitly cleared the field.
            // SetExternalPayment treats null as "clear stale pricing" (organiser
            // moves pricing off-platform); the public detail page renders the
            // "See external site or reach out organizer for pricing" copy.
            var setExternalResult = @event.SetExternalPayment(externalReg, pricing);
            if (setExternalResult.IsFailure)
            {
                _logger.LogWarning(
                    "UpdateEvent: SetExternalPayment domain rejection - EventId={EventId}, Error={Error}",
                    request.EventId, setExternalResult.Error);
                return setExternalResult;
            }

            _logger.LogInformation(
                "UpdateEvent: ExternalPaid configured - EventId={EventId}, Url={Url}, AllEmpty={AllEmpty}",
                request.EventId, externalReg?.Url ?? "(none)", allEmpty);
        }
        // Session 33 + Session 21: Update pricing if provided
        else if (pricing != null)
        {
            Result setPricingResult;
            if (isGroupPricing)
            {
                // Session 33: Use SetGroupPricing for group tiered pricing
                setPricingResult = @event.SetGroupPricing(pricing);
            }
            else
            {
                // Session 21: Use SetDualPricing for dual or single pricing
                setPricingResult = @event.SetDualPricing(pricing);
            }

            if (setPricingResult.IsFailure)
                return setPricingResult;

            // Session 33 CORRECTED: EF Core automatically detects changes when Pricing object reference changes
            // The domain methods (SetGroupPricing/SetDualPricing) assign "Pricing = pricing" which triggers automatic tracking
            // No explicit change marking needed for JSONB columns - object replacement is sufficient

            // Phase 6A.X: Recalculate revenue breakdown when pricing changes
            // Use updated location if provided, otherwise use existing location
            var locationForBreakdown = location ?? @event.Location;

            Money? priceForBreakdown = null;
            if (isGroupPricing && pricing.GroupTiers != null && pricing.GroupTiers.Count > 0)
            {
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
                    locationForBreakdown,
                    cancellationToken);

                if (breakdownResult.IsSuccess)
                {
                    @event.SetRevenueBreakdown(breakdownResult.Value);
                }
                // Tax lookup failures don't block event update - breakdown is informational
            }
        }
        else if (location != null && @event.Pricing != null)
        {
            // Phase 6A.X: Location changed but pricing didn't - recalculate breakdown with existing pricing
            Money? priceForBreakdown = null;
            var existingPricing = @event.Pricing;

            if (existingPricing.HasGroupTiers &&
                existingPricing.GroupTiers != null && existingPricing.GroupTiers.Count > 0)
            {
                priceForBreakdown = existingPricing.GroupTiers[0].PricePerPerson;
            }
            else if (existingPricing.AdultPrice != null)
            {
                priceForBreakdown = existingPricing.AdultPrice;
            }

            if (priceForBreakdown != null && priceForBreakdown.Amount > 0)
            {
                var breakdownResult = await _revenueCalculatorService.CalculateBreakdownAsync(
                    priceForBreakdown,
                    location,
                    cancellationToken);

                if (breakdownResult.IsSuccess)
                {
                    @event.SetRevenueBreakdown(breakdownResult.Value);
                }
            }
        }

        // Legacy: Update ticket price for backward compatibility
        var ticketPriceProperty = typeof(Event).GetProperty(nameof(Event.TicketPrice));
        ticketPriceProperty?.SetValue(@event, ticketPrice);

        // IsFreeEvent fix: Explicitly mark as free event when frontend sends IsFree=true
        if (request.PaymentMode != EventPaymentMode.ExternalPaid
            && request.IsFree == true && pricing == null)
        {
            var setFreeResult = @event.SetAsFreeEvent();
            if (setFreeResult.IsFailure)
                return setFreeResult;

            _logger.LogInformation(
                "UpdateEvent: Event marked as free - EventId={EventId}",
                request.EventId);
        }
        else if (request.IsFree == false && pricing != null)
        {
            // Paid event with pricing - IsFreeEvent is already set by SetDualPricing/SetGroupPricing
            // via the domain methods that manage the flag
        }

        // Phase 8X.4b — Sync PaymentMode for explicit transitions (Free / OnPlatformPaid).
        // ExternalPaid handled in the dedicated branch above. SetPaymentMode is idempotent
        // on same-mode set, and surfaces the active-registration guard from the domain
        // when a paid → free / paid → paid transition would orphan internal regs.
        if (request.PaymentMode.HasValue && request.PaymentMode.Value != EventPaymentMode.ExternalPaid)
        {
            var setPaymentModeResult = @event.SetPaymentMode(request.PaymentMode.Value);
            if (setPaymentModeResult.IsFailure)
            {
                _logger.LogWarning(
                    "UpdateEvent: SetPaymentMode rejected - EventId={EventId}, RequestedMode={Mode}, Reason={Reason}",
                    request.EventId, request.PaymentMode.Value, setPaymentModeResult.Error);
                return setPaymentModeResult;
            }
        }

        // Phase 7E.2: Handle optional registration mode change. Null = caller didn't request a
        // change → leave the existing mode untouched.
        if (request.RegistrationMode.HasValue && request.RegistrationMode.Value != @event.RegistrationMode)
        {
            // Validate the requested mode against the post-update event shape (using the just-applied
            // pricing/free state). Same compatibility helper that CreateEvent uses — single source of truth.
            var modeContext = new LankaConnect.Domain.Events.Services.RegistrationModeContext
            {
                IsFreeAttendance = @event.IsFreeEvent,
                // Phase 8X.11 — pass payment-mode axis so the compatibility helper rejects
                // External for non-ExternalPaid events and the other modes for ExternalPaid.
                PaymentMode = @event.PaymentMode,
                HasDualPricing = @event.Pricing != null && @event.Pricing.HasChildPricing,
                HasGroupTiers = @event.Pricing != null && @event.Pricing.HasGroupTiers,
                HasTicketTiers = @event.HasTicketTiers,
                HasSeating = @event.HasAssignedSeating,
                // Other axes (named seating, identity-bound add-on, per-ticket name, matrix pricing)
                // are not yet modelled on Event — they default to false. Phase 7F revisits.
            };
            var modeCompatibility = LankaConnect.Domain.Events.Services
                .RegistrationModeCompatibility.Check(request.RegistrationMode.Value, modeContext);
            if (modeCompatibility.IsFailure)
            {
                _logger.LogWarning(
                    "UpdateEvent VALIDATION FAILED: incompatible registration mode - EventId={EventId}, " +
                    "RequestedMode={Mode}, Reason={Reason}",
                    request.EventId, request.RegistrationMode.Value, modeCompatibility.Error);
                return Result.Failure(modeCompatibility.Error);
            }

            // Domain rule: SetRegistrationMode throws if Registrations.Any().
            // Surface that as a clear 400 with the registration count in the message.
            var setModeResult = @event.SetRegistrationMode(request.RegistrationMode.Value);
            if (setModeResult.IsFailure)
            {
                _logger.LogWarning(
                    "UpdateEvent: SetRegistrationMode rejected - EventId={EventId}, RequestedMode={Mode}, Reason={Reason}",
                    request.EventId, request.RegistrationMode.Value, setModeResult.Error);
                return setModeResult;
            }

            _logger.LogInformation(
                "UpdateEvent: RegistrationMode changed - EventId={EventId}, NewMode={Mode}",
                request.EventId, request.RegistrationMode.Value);
        }

        // Phase 6A.32/33: Validate and update email groups (Fix #3: Batch query to prevent N+1)
        if (request.EmailGroupIds != null && request.EmailGroupIds.Any())
        {
            var distinctGroupIds = request.EmailGroupIds.Distinct().ToList();

            // Wave 5.4.d.1 (2026-06-22): validate email groups via IEmailGroupQueries.
            // The previous dbContext.Set<EmailGroup>() WITH-TRACKING pull is gone —
            // Wave 5.4.c.0 (Event.cs typed-nav surgery) made the tracking concern moot
            // because Event.SetEmailGroups now manipulates _emailGroupLinks directly
            // without needing an EmailGroup CLR reference at all.
            var emailGroups = await _emailGroupQueries.GetByIdsAsync(distinctGroupIds, cancellationToken);

            foreach (var groupId in distinctGroupIds)
            {
                var emailGroup = emailGroups.FirstOrDefault(g => g.Id == groupId);

                if (emailGroup == null)
                    return Result.Failure($"Email group with ID {groupId} not found");

                if (emailGroup.OwnerId != @event.OrganizerId)
                    return Result.Failure($"You do not have permission to use email group '{emailGroup.Name}'");

                if (!emailGroup.IsActive)
                    return Result.Failure($"Email group '{emailGroup.Name}' is inactive and cannot be used");
            }

            // Wave 5.4.c.0 (2026-06-13). Single SetEmailGroups call suffices —
            // it updates _emailGroupLinks directly (the EF-tracked junction
            // collection). The Phase 6A.32 manual ChangeTracker shadow-nav
            // manipulation that used to follow is gone with the typed nav.
            var updateResult = @event.SetEmailGroups(distinctGroupIds);
            if (updateResult.IsFailure)
                return updateResult;
        }
        else if (request.EmailGroupIds != null && !request.EmailGroupIds.Any())
        {
            // Wave 5.4.c.0 (2026-06-13). ClearEmailGroups clears _emailGroupLinks
            // directly — no separate shadow-nav clear needed anymore.
            @event.ClearEmailGroups();
        }
        // If null, don't modify existing email groups

        // Update organizer contacts if provided
        if (request.PublishOrganizerContact.HasValue)
        {
            var contacts = (request.OrganizerContacts ?? new List<UpdateEventOrganizerContact.OrganizerContactRequest>())
                .Select(c => (c.ContactName, c.ContactEmail, c.ContactPhone, c.LinkedUserId, c.IsPrimary))
                .ToList();

            var contactResult = @event.SetOrganizerContacts(
                request.PublishOrganizerContact.Value,
                contacts);

            if (contactResult.IsFailure)
                return contactResult;
        }

        // Donation Feature: Update donation configuration if provided
        if (request.DonationsEnabled.HasValue)
        {
            if (request.DonationsEnabled.Value)
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
                    return Result.Failure(donationConfigResult.Error);

                var setDonationResult = @event.SetDonationConfiguration(donationConfigResult.Value);
                if (setDonationResult.IsFailure)
                    return setDonationResult;
            }
            else
            {
                var disableResult = @event.DisableDonations();
                if (disableResult.IsFailure)
                    return disableResult;
            }
        }

                // Save changes (EF Core now detects changes via ChangeTracker)
                _eventRepository.Update(@event);
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "UpdateEvent COMPLETE: EventId={EventId}, Title={Title}, Status={Status}, Duration={ElapsedMs}ms",
                    request.EventId, request.Title, @event.Status, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "UpdateEvent FAILED: Exception occurred - EventId={EventId}, Title={Title}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.Title, stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }
}
