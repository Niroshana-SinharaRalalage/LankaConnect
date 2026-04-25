using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Domain.Events.Entities;

/// <summary>
/// Entity representing a ticket tier for a multi-tier event (e.g., VIP, Plus, Basic).
/// Each tier has its own adult/child pricing, capacity, and reservation tracking.
/// Follows the EventPass Reserve/Release pattern for capacity management.
/// </summary>
public class TicketTier : BaseEntity
{
    /// <summary>
    /// The event this tier belongs to
    /// </summary>
    public Guid EventId { get; private set; }

    /// <summary>
    /// Tier name (e.g., "VIP", "Plus", "Basic", or custom)
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Optional description of the tier
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Price for adult attendees (required for all tiers, can be zero for free tiers)
    /// </summary>
    public Money AdultPrice { get; private set; } = null!;

    /// <summary>
    /// Price for child attendees (null if this tier doesn't offer child pricing)
    /// </summary>
    public Money? ChildPrice { get; private set; }

    /// <summary>
    /// Age limit for child pricing (inclusive, 1-18). Null if no child pricing.
    /// </summary>
    public int? ChildAgeLimit { get; private set; }

    /// <summary>
    /// Whether this tier has separate child pricing enabled
    /// </summary>
    public bool HasChildPricing => ChildPrice != null && ChildAgeLimit != null;

    /// <summary>
    /// Maximum tickets available for this tier
    /// </summary>
    public int Capacity { get; private set; }

    /// <summary>
    /// Number of tickets currently reserved (sold/held)
    /// </summary>
    public int ReservedCount { get; private set; }

    /// <summary>
    /// Remaining tickets available
    /// </summary>
    public int AvailableQuantity => Capacity - ReservedCount;

    /// <summary>
    /// Maximum tickets a single user can purchase for this tier (default 10)
    /// </summary>
    public int MaxPerUser { get; private set; }

    /// <summary>
    /// Display order for the tier (lower = displayed first)
    /// </summary>
    public int SortOrder { get; private set; }

    /// <summary>
    /// Whether this tier is currently available for purchase
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Whether this is a free tier (adult price is zero)
    /// </summary>
    public bool IsFree => AdultPrice.IsZero;

    /// <summary>
    /// Polymorphic tier→zone/table assignments replacing the legacy <c>VenueZone.TicketTierId</c> FK.
    /// Populated via <see cref="AssignToZone"/> / <see cref="AssignToTable"/>; removed via <see cref="RemoveAssignment"/>.
    /// </summary>
    private readonly List<TierAssignment> _assignments = new();
    public IReadOnlyList<TierAssignment> Assignments => _assignments.AsReadOnly();

    // EF Core constructor
    private TicketTier()
    {
    }

    private TicketTier(
        Guid eventId,
        string name,
        string? description,
        Money adultPrice,
        Money? childPrice,
        int? childAgeLimit,
        int capacity,
        int maxPerUser,
        int sortOrder)
    {
        EventId = eventId;
        Name = name;
        Description = description;
        AdultPrice = adultPrice;
        ChildPrice = childPrice;
        ChildAgeLimit = childAgeLimit;
        Capacity = capacity;
        ReservedCount = 0;
        MaxPerUser = maxPerUser;
        SortOrder = sortOrder;
        IsActive = true;
    }

    /// <summary>
    /// Creates a new ticket tier for an event
    /// </summary>
    public static Result<TicketTier> Create(
        Guid eventId,
        string name,
        string? description,
        Money adultPrice,
        Money? childPrice,
        int? childAgeLimit,
        int capacity,
        int maxPerUser,
        int sortOrder)
    {
        if (eventId == Guid.Empty)
            return Result<TicketTier>.Failure("Event ID is required");

        if (string.IsNullOrWhiteSpace(name))
            return Result<TicketTier>.Failure("Tier name is required");

        if (adultPrice == null)
            return Result<TicketTier>.Failure("Adult price is required");

        if (capacity <= 0)
            return Result<TicketTier>.Failure("Capacity must be greater than 0");

        if (maxPerUser <= 0)
            return Result<TicketTier>.Failure("Max per user must be greater than 0");

        // Child pricing validation: both or neither
        if ((childPrice != null) != (childAgeLimit != null))
            return Result<TicketTier>.Failure("Both child price and age limit are required for child pricing");

        // Child-specific validations when child pricing is enabled
        if (childPrice != null && childAgeLimit != null)
        {
            if (childAgeLimit.Value < 1 || childAgeLimit.Value > 18)
                return Result<TicketTier>.Failure("Child age limit must be between 1 and 18 years");

            if (adultPrice.Currency != childPrice.Currency)
                return Result<TicketTier>.Failure("Adult and child prices must use the same currency");

            if (childPrice.IsGreaterThan(adultPrice))
                return Result<TicketTier>.Failure("Child price cannot be greater than adult price");
        }

        return Result<TicketTier>.Success(new TicketTier(
            eventId,
            name.Trim(),
            description?.Trim(),
            adultPrice,
            childPrice,
            childAgeLimit,
            capacity,
            maxPerUser,
            sortOrder));
    }

    /// <summary>
    /// Reserves tickets in this tier (called during registration)
    /// </summary>
    public Result Reserve(int quantity)
    {
        if (quantity <= 0)
            return Result.Failure("Quantity must be greater than 0");

        if (AvailableQuantity < quantity)
            return Result.Failure("Insufficient capacity in this tier");

        ReservedCount += quantity;
        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Releases reserved tickets (called on cancellation/refund)
    /// </summary>
    public Result Release(int quantity)
    {
        if (quantity <= 0)
            return Result.Failure("Quantity must be greater than 0");

        if (ReservedCount < quantity)
            return Result.Failure("Cannot release more than reserved");

        ReservedCount -= quantity;
        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Whether this tier has enough capacity for the requested quantity
    /// </summary>
    public bool HasCapacityFor(int requestedQuantity)
    {
        return AvailableQuantity >= requestedQuantity;
    }

    /// <summary>
    /// Whether the user can purchase the requested quantity given their existing purchases
    /// </summary>
    public bool CanUserPurchase(int requestedQuantity, int userCurrentQuantity)
    {
        return userCurrentQuantity + requestedQuantity <= MaxPerUser;
    }

    /// <summary>
    /// Calculates the price for an attendee based on their age category.
    /// Returns child price if child pricing is enabled and attendee is a child, otherwise adult price.
    /// </summary>
    public Money CalculatePriceForAttendee(AgeCategory ageCategory)
    {
        if (HasChildPricing && ageCategory == AgeCategory.Child)
            return ChildPrice!;

        return AdultPrice;
    }

    /// <summary>
    /// Updates the tier's configurable properties
    /// </summary>
    public Result Update(
        string name,
        string? description,
        Money adultPrice,
        Money? childPrice,
        int? childAgeLimit,
        int capacity,
        int maxPerUser,
        int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Result.Failure("Tier name is required");

        if (adultPrice == null)
            return Result.Failure("Adult price is required");

        if (capacity <= 0)
            return Result.Failure("Capacity must be greater than 0");

        if (capacity < ReservedCount)
            return Result.Failure("Cannot reduce capacity below reserved count");

        if (maxPerUser <= 0)
            return Result.Failure("Max per user must be greater than 0");

        if ((childPrice != null) != (childAgeLimit != null))
            return Result.Failure("Both child price and age limit are required for child pricing");

        if (childPrice != null && childAgeLimit != null)
        {
            if (childAgeLimit.Value < 1 || childAgeLimit.Value > 18)
                return Result.Failure("Child age limit must be between 1 and 18 years");

            if (adultPrice.Currency != childPrice.Currency)
                return Result.Failure("Adult and child prices must use the same currency");

            if (childPrice.IsGreaterThan(adultPrice))
                return Result.Failure("Child price cannot be greater than adult price");
        }

        Name = name.Trim();
        Description = description?.Trim();
        AdultPrice = adultPrice;
        ChildPrice = childPrice;
        ChildAgeLimit = childAgeLimit;
        Capacity = capacity;
        MaxPerUser = maxPerUser;
        SortOrder = sortOrder;
        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Deactivates the tier (hides from purchase but preserves data)
    /// </summary>
    public Result Deactivate()
    {
        if (ReservedCount > 0)
            return Result.Failure("Cannot deactivate tier with existing reservations");

        IsActive = false;
        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Reactivates a deactivated tier
    /// </summary>
    public Result Activate()
    {
        IsActive = true;
        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Assigns this tier to a <see cref="VenueZone"/>. Idempotent — a second call with the
    /// same zone ID is a no-op that still returns success (matches the architect's contract that
    /// a zone can only be mapped to a given tier once).
    /// </summary>
    public Result AssignToZone(Guid zoneId)
    {
        return AddAssignment(AssignableKind.Zone, zoneId);
    }

    /// <summary>
    /// Assigns this tier to a <see cref="VenueTable"/>. Idempotent (see <see cref="AssignToZone"/>).
    /// </summary>
    public Result AssignToTable(Guid tableId)
    {
        return AddAssignment(AssignableKind.Table, tableId);
    }

    /// <summary>
    /// Removes an assignment. Returns failure if no matching assignment exists so callers can
    /// detect stale UI state.
    /// </summary>
    public Result RemoveAssignment(AssignableKind kind, Guid assignableId)
    {
        if (assignableId == Guid.Empty)
            return Result.Failure("Assignable ID is required");

        var existing = _assignments.FirstOrDefault(a => a.AssignableKind == kind && a.AssignableId == assignableId);
        if (existing == null)
            return Result.Failure("Assignment not found");

        _assignments.Remove(existing);
        MarkAsUpdated();
        return Result.Success();
    }

    private Result AddAssignment(AssignableKind kind, Guid assignableId)
    {
        if (assignableId == Guid.Empty)
            return Result.Failure("Assignable ID is required");

        var alreadyAssigned = _assignments.Any(a => a.AssignableKind == kind && a.AssignableId == assignableId);
        if (alreadyAssigned)
            return Result.Success(); // idempotent

        var assignmentResult = TierAssignment.Create(Id, kind, assignableId);
        if (!assignmentResult.IsSuccess)
            return Result.Failure(assignmentResult.Error);

        _assignments.Add(assignmentResult.Value);
        MarkAsUpdated();
        return Result.Success();
    }
}
