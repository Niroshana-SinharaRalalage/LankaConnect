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
    private readonly ILogger<CreateEventCommandHandler> _logger;

    public CreateEventCommandHandler(
        IEventRepository eventRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IEmailGroupRepository emailGroupRepository, // Phase 6A.32: Email groups
        IApplicationDbContext dbContext, // Phase 6A.32: ChangeTracker API
        IRevenueCalculatorService revenueCalculatorService, // Phase 6A.X: Revenue breakdown
        ILogger<CreateEventCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _emailGroupRepository = emailGroupRepository; // Phase 6A.32: Email groups
        _dbContext = dbContext; // Phase 6A.32: ChangeTracker API
        _revenueCalculatorService = revenueCalculatorService; // Phase 6A.X: Revenue breakdown
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

            var locationResult = EventLocation.Create(addressResult.Value, coordinates);
            if (locationResult.IsFailure)
                return Result<Guid>.Failure(locationResult.Error);

            location = locationResult.Value;
        }

        // Phase 6D: Handle pricing - group pricing takes precedence over dual and legacy single pricing
        Money? ticketPrice = null;
        TicketPricing? pricing = null;
        bool isGroupPricing = false;

        // Phase 6D: Check if group pricing tiers are provided (highest priority)
        if (request.GroupPricingTiers != null && request.GroupPricingTiers.Count > 0)
        {
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

            var groupPricingResult = TicketPricing.CreateGroupTiered(tiers, currency);
            if (groupPricingResult.IsFailure)
                return Result<Guid>.Failure(groupPricingResult.Error);

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

        // Phase 6D + Session 21: Set pricing if provided
        if (pricing != null)
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

        // Phase 6A.X: Set organizer contact details if provided
        if (request.PublishOrganizerContact.GetValueOrDefault())
        {
            var contactResult = eventResult.Value.SetOrganizerContactDetails(
                publishContact: true, // Safe to use true since we're inside the GetValueOrDefault() check
                request.OrganizerContactName,
                request.OrganizerContactPhone,
                request.OrganizerContactEmail);

            if (contactResult.IsFailure)
                return Result<Guid>.Failure(contactResult.Error);
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
