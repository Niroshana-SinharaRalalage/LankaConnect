using LankaConnect.SharedKernel.Money;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
namespace LankaConnect.Products.LankaEvents.Domain;

/// <summary>
/// Partial class extending Event with multi-tier ticketing capabilities.
/// Manages TicketTier collection, ticketing mode, and tier-aware pricing/capacity.
/// </summary>
public partial class Event
{
    private readonly List<TicketTier> _ticketTiers = new();

    /// <summary>
    /// The ticketing mode for this event (SingleTier = legacy, Tiered = multi-tier)
    /// </summary>
    public TicketingMode TicketingMode { get; private set; } = TicketingMode.SingleTier;

    /// <summary>
    /// Read-only collection of ticket tiers (only populated in Tiered mode)
    /// </summary>
    public IReadOnlyList<TicketTier> TicketTiers => _ticketTiers.AsReadOnly();

    /// <summary>
    /// Whether this event uses multi-tier ticketing with at least one active tier
    /// </summary>
    public bool HasTicketTiers => TicketingMode == TicketingMode.Tiered && _ticketTiers.Any(t => t.IsActive);

    #region Ticketing Mode Management

    /// <summary>
    /// Sets the event to Tiered ticketing mode.
    /// Requires at least one tier to be added after calling this.
    /// Cannot switch if the event already has registrations.
    /// </summary>
    public Result SetTicketingMode(TicketingMode mode)
    {
        if (mode == TicketingMode)
            return Result.Success();

        // Cannot switch modes if there are active registrations
        if (_registrations.Any(r =>
            r.Status == RegistrationStatus.Confirmed ||
            r.Status == RegistrationStatus.Preliminary))
        {
            return Result.Failure("Cannot change ticketing mode with existing registrations");
        }

        if (mode == TicketingMode.SingleTier && _ticketTiers.Any(t => t.ReservedCount > 0))
        {
            return Result.Failure("Cannot switch to SingleTier mode while tiers have reservations");
        }

        TicketingMode = mode;
        return Result.Success();
    }

    #endregion

    #region Tier CRUD

    /// <summary>
    /// Adds a new ticket tier to the event
    /// </summary>
    public Result<TicketTier> AddTicketTier(
        string name,
        string? description,
        Money adultPrice,
        Money? childPrice,
        int? childAgeLimit,
        int capacity,
        int maxPerUser,
        int sortOrder)
    {
        if (TicketingMode != TicketingMode.Tiered)
            return Result<TicketTier>.Failure("Event must be in Tiered mode to add ticket tiers");

        // Check for duplicate tier name
        if (_ticketTiers.Any(t => t.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase) && t.IsActive))
            return Result<TicketTier>.Failure($"A tier with name '{name.Trim()}' already exists");

        // Validate total tier capacity does not exceed event capacity
        var currentTierCapacity = _ticketTiers.Where(t => t.IsActive).Sum(t => t.Capacity);
        if (currentTierCapacity + capacity > Capacity)
            return Result<TicketTier>.Failure(
                $"Total tier capacity ({currentTierCapacity + capacity}) cannot exceed event capacity ({Capacity})");

        var tierResult = TicketTier.Create(
            Id, name, description, adultPrice, childPrice, childAgeLimit,
            capacity, maxPerUser, sortOrder);

        if (tierResult.IsFailure)
            return tierResult;

        _ticketTiers.Add(tierResult.Value);
        return tierResult;
    }

    /// <summary>
    /// Updates an existing ticket tier
    /// </summary>
    public Result UpdateTicketTier(
        Guid tierId,
        string name,
        string? description,
        Money adultPrice,
        Money? childPrice,
        int? childAgeLimit,
        int capacity,
        int maxPerUser,
        int sortOrder)
    {
        if (TicketingMode != TicketingMode.Tiered)
            return Result.Failure("Event must be in Tiered mode to update ticket tiers");

        var tier = _ticketTiers.FirstOrDefault(t => t.Id == tierId);
        if (tier == null)
            return Result.Failure("Ticket tier not found");

        // Check for duplicate tier name (exclude self)
        if (_ticketTiers.Any(t => t.Id != tierId &&
            t.Name.Equals(name.Trim(), StringComparison.OrdinalIgnoreCase) && t.IsActive))
            return Result.Failure($"A tier with name '{name.Trim()}' already exists");

        // Validate total tier capacity does not exceed event capacity
        var otherTierCapacity = _ticketTiers.Where(t => t.IsActive && t.Id != tierId).Sum(t => t.Capacity);
        if (otherTierCapacity + capacity > Capacity)
            return Result.Failure(
                $"Total tier capacity ({otherTierCapacity + capacity}) cannot exceed event capacity ({Capacity})");

        return tier.Update(name, description, adultPrice, childPrice, childAgeLimit, capacity, maxPerUser, sortOrder);
    }

    /// <summary>
    /// Removes (deactivates) a ticket tier
    /// </summary>
    public Result RemoveTicketTier(Guid tierId)
    {
        var tier = _ticketTiers.FirstOrDefault(t => t.Id == tierId);
        if (tier == null)
            return Result.Failure("Ticket tier not found");

        return tier.Deactivate();
    }

    /// <summary>
    /// Gets a specific ticket tier by ID
    /// </summary>
    public TicketTier? GetTicketTier(Guid tierId)
    {
        return _ticketTiers.FirstOrDefault(t => t.Id == tierId);
    }

    /// <summary>
    /// Gets all active ticket tiers sorted by SortOrder
    /// </summary>
    public IReadOnlyList<TicketTier> GetActiveTiers()
    {
        return _ticketTiers
            .Where(t => t.IsActive)
            .OrderBy(t => t.SortOrder)
            .ToList()
            .AsReadOnly();
    }

    #endregion

    #region Tier-Aware Pricing

    /// <summary>
    /// Calculates total price for attendees in Tiered mode.
    /// Each attendee has a TicketTierId; their price comes from the tier's adult/child pricing.
    /// </summary>
    public Result<Money> CalculateTieredPriceForAttendees(IEnumerable<AttendeeDetails> attendees)
    {
        if (TicketingMode != TicketingMode.Tiered)
            return Result<Money>.Failure("Event is not in Tiered mode");

        if (attendees == null || !attendees.Any())
            return Result<Money>.Failure("At least one attendee is required");

        Money? totalPrice = null;

        foreach (var attendee in attendees)
        {
            if (attendee.TicketTierId == null)
                return Result<Money>.Failure($"Attendee '{attendee.Name}' does not have a ticket tier assigned");

            var tier = _ticketTiers.FirstOrDefault(t => t.Id == attendee.TicketTierId.Value && t.IsActive);
            if (tier == null)
                return Result<Money>.Failure($"Ticket tier not found or inactive for attendee '{attendee.Name}'");

            var attendeePrice = tier.CalculatePriceForAttendee(attendee.AgeCategory);

            if (totalPrice == null)
            {
                totalPrice = attendeePrice;
            }
            else
            {
                totalPrice = totalPrice + attendeePrice;
            }
        }

        return Result<Money>.Success(totalPrice!);
    }

    #endregion

    #region Tier-Aware Capacity

    /// <summary>
    /// Checks if the event has capacity for the requested attendees, per-tier.
    /// Groups attendees by tier and validates each tier independently.
    /// Also checks overall event capacity.
    /// </summary>
    public Result HasTieredCapacityFor(IEnumerable<AttendeeDetails> attendees)
    {
        if (TicketingMode != TicketingMode.Tiered)
            return Result.Failure("Event is not in Tiered mode");

        var attendeeList = attendees.ToList();

        // Overall event capacity check
        if (!HasCapacityFor(attendeeList.Count))
            return Result.Failure("Event does not have enough overall capacity");

        // Group by tier and check each tier's capacity
        var tierGroups = attendeeList
            .Where(a => a.TicketTierId != null)
            .GroupBy(a => a.TicketTierId!.Value);

        foreach (var group in tierGroups)
        {
            var tier = _ticketTiers.FirstOrDefault(t => t.Id == group.Key && t.IsActive);
            if (tier == null)
                return Result.Failure($"Ticket tier {group.Key} not found or inactive");

            if (!tier.HasCapacityFor(group.Count()))
                return Result.Failure($"Tier '{tier.Name}' does not have enough capacity (requested: {group.Count()}, available: {tier.AvailableQuantity})");
        }

        // Check for attendees without tier assignment
        var unassigned = attendeeList.Where(a => a.TicketTierId == null).ToList();
        if (unassigned.Any())
            return Result.Failure($"{unassigned.Count} attendee(s) do not have a ticket tier assigned");

        return Result.Success();
    }

    #endregion
}
