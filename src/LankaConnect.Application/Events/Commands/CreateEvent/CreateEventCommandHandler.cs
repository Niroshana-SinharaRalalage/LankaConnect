using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Users.Enums;

namespace LankaConnect.Application.Events.Commands.CreateEvent;

public class CreateEventCommandHandler : ICommandHandler<CreateEventCommand, Guid>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateEventCommandHandler(
        IEventRepository eventRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateEventCommand request, CancellationToken cancellationToken)
    {
        // Validate user can create events based on role
        var user = await _userRepository.GetByIdAsync(request.OrganizerId, cancellationToken);
        if (user == null)
            return Result<Guid>.Failure("User not found");

        // Check if user has permission to create events (EventOrganizer or Admin roles)
        if (!user.Role.CanCreateEvents())
        {
            return Result<Guid>.Failure("You do not have permission to create events. Only Event Organizers and Administrators can create events.");
        }

        // Create EventTitle value object
        var titleResult = EventTitle.Create(request.Title);
        if (titleResult.IsFailure)
            return Result<Guid>.Failure(titleResult.Error);

        // Create EventDescription value object
        var descriptionResult = EventDescription.Create(request.Description);
        if (descriptionResult.IsFailure)
            return Result<Guid>.Failure(descriptionResult.Error);

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

            var locationResult = EventLocation.Create(addressResult.Value, coordinates);
            if (locationResult.IsFailure)
                return Result<Guid>.Failure(locationResult.Error);

            location = locationResult.Value;
        }

        // Session 21: Handle pricing - dual pricing takes precedence over legacy single pricing
        Money? ticketPrice = null;
        TicketPricing? pricing = null;

        // Check if dual pricing fields are provided
        if (request.AdultPriceAmount.HasValue && request.AdultPriceCurrency.HasValue)
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

        // Session 21: Set dual pricing if provided
        if (pricing != null)
        {
            var setPricingResult = eventResult.Value.SetDualPricing(pricing);
            if (setPricingResult.IsFailure)
                return Result<Guid>.Failure(setPricingResult.Error);
        }

        // Add to repository and commit
        await _eventRepository.AddAsync(eventResult.Value, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result<Guid>.Success(eventResult.Value.Id);
    }
}
