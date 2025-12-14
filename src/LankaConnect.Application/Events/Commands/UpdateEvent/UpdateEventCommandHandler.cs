using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Commands.CreateEvent;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Application.Events.Commands.UpdateEvent;

public class UpdateEventCommandHandler : ICommandHandler<UpdateEventCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateEventCommandHandler(IEventRepository eventRepository, IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UpdateEventCommand request, CancellationToken cancellationToken)
    {
        // Retrieve existing event
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result.Failure("Event not found");

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

        // Validate dates
        if (request.StartDate <= DateTime.UtcNow)
            return Result.Failure("Start date cannot be in the past");

        if (request.EndDate <= request.StartDate)
            return Result.Failure("End date must be after start date");

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

            var locationResult = EventLocation.Create(addressResult.Value, coordinates);
            if (locationResult.IsFailure)
                return Result.Failure(locationResult.Error);

            location = locationResult.Value;
        }

        // Session 33: Handle pricing - group pricing takes precedence over dual and legacy single pricing
        Money? ticketPrice = null;
        TicketPricing? pricing = null;
        bool isGroupPricing = false;

        // Session 33: Check if group pricing tiers are provided (highest priority)
        if (request.GroupPricingTiers != null && request.GroupPricingTiers.Count > 0)
        {
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

            var groupPricingResult = TicketPricing.CreateGroupTiered(tiers, currency);
            if (groupPricingResult.IsFailure)
                return Result.Failure(groupPricingResult.Error);

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

        var startDateProperty = typeof(Event).GetProperty(nameof(Event.StartDate));
        startDateProperty?.SetValue(@event, request.StartDate);

        var endDateProperty = typeof(Event).GetProperty(nameof(Event.EndDate));
        endDateProperty?.SetValue(@event, request.EndDate);

        var capacityResult = @event.UpdateCapacity(request.Capacity);
        if (capacityResult.IsFailure)
            return capacityResult;

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

        // Session 33 + Session 21: Update pricing if provided
        if (pricing != null)
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
        }

        // Legacy: Update ticket price for backward compatibility
        var ticketPriceProperty = typeof(Event).GetProperty(nameof(Event.TicketPrice));
        ticketPriceProperty?.SetValue(@event, ticketPrice);

        // Save changes (EF Core tracks changes automatically)
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
