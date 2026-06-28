using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Domain.Events.ValueObjects;

/// <summary>
/// Phase 6D: Value object representing a single pricing tier in group-based pricing
/// Example: "1-2 attendees: $15/person" or "3+ attendees: $12/person"
/// </summary>
public class GroupPricingTier : ValueObject
{
    /// <summary>
    /// Minimum number of attendees for this tier (inclusive)
    /// </summary>
    public int MinAttendees { get; private set; }

    /// <summary>
    /// Maximum number of attendees for this tier (inclusive)
    /// Null represents unlimited (e.g., "3+" tier)
    /// </summary>
    public int? MaxAttendees { get; private set; }

    /// <summary>
    /// Price per person for this tier
    /// </summary>
    public Money PricePerPerson { get; private set; }

    /// <summary>
    /// Indicates whether this tier has no upper limit (3+, 5+, etc.)
    /// </summary>
    public bool IsUnlimitedTier => MaxAttendees == null;

    // EF Core constructor
    private GroupPricingTier()
    {
        // Required for EF Core
        PricePerPerson = null!;
    }

    private GroupPricingTier(int minAttendees, int? maxAttendees, Money pricePerPerson)
    {
        MinAttendees = minAttendees;
        MaxAttendees = maxAttendees;
        PricePerPerson = pricePerPerson;
    }

    /// <summary>
    /// Creates a new GroupPricingTier with validation
    /// </summary>
    /// <param name="minAttendees">Minimum attendees (must be >= 1)</param>
    /// <param name="maxAttendees">Maximum attendees (null for unlimited, must be >= minAttendees if specified)</param>
    /// <param name="pricePerPerson">Price per person for this tier</param>
    public static Result<GroupPricingTier> Create(int minAttendees, int? maxAttendees, Money? pricePerPerson)
    {
        // Validation: Minimum attendees must be at least 1
        if (minAttendees < 1)
            return Result<GroupPricingTier>.Failure("MinAttendees must be at least 1");

        // Validation: If MaxAttendees is specified, it must be >= MinAttendees
        if (maxAttendees.HasValue && maxAttendees.Value < minAttendees)
            return Result<GroupPricingTier>.Failure("MaxAttendees must be greater than or equal to MinAttendees");

        // Validation: PricePerPerson is required
        if (pricePerPerson == null)
            return Result<GroupPricingTier>.Failure("PricePerPerson is required");

        return Result<GroupPricingTier>.Success(new GroupPricingTier(minAttendees, maxAttendees, pricePerPerson));
    }

    /// <summary>
    /// Checks if this tier covers the given attendee count
    /// </summary>
    /// <param name="attendeeCount">Number of attendees to check</param>
    /// <returns>True if this tier covers the count, false otherwise</returns>
    public bool CoversAttendeeCount(int attendeeCount)
    {
        if (attendeeCount < 1)
            return false;

        // Check minimum bound
        if (attendeeCount < MinAttendees)
            return false;

        // Check maximum bound (if defined)
        if (MaxAttendees.HasValue && attendeeCount > MaxAttendees.Value)
            return false;

        return true;
    }

    /// <summary>
    /// Checks if this tier overlaps with another tier
    /// Used for validation to ensure no gaps or overlaps in tier coverage
    /// </summary>
    /// <param name="other">Another tier to check for overlap</param>
    /// <returns>True if tiers overlap, false otherwise</returns>
    public bool OverlapsWith(GroupPricingTier other)
    {
        if (other == null)
            return false;

        // Both tiers are unlimited - they always overlap
        // Example: 3+ and 5+ both cover 5, 6, 7, ... so they overlap
        if (IsUnlimitedTier && other.IsUnlimitedTier)
        {
            return true;
        }

        // This tier is unlimited
        if (IsUnlimitedTier)
        {
            // Overlaps if other's max is >= this tier's min
            return other.MaxAttendees!.Value >= MinAttendees;
        }

        // Other tier is unlimited
        if (other.IsUnlimitedTier)
        {
            // Overlaps if this tier's max is >= other's min
            return MaxAttendees!.Value >= other.MinAttendees;
        }

        // Both tiers have defined ranges
        // No overlap if one tier ends before the other starts
        if (MaxAttendees!.Value < other.MinAttendees)
            return false;

        if (other.MaxAttendees!.Value < MinAttendees)
            return false;

        return true;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return MinAttendees;
        if (MaxAttendees.HasValue)
            yield return MaxAttendees.Value;
        yield return PricePerPerson;
    }

    public override string ToString()
    {
        var range = IsUnlimitedTier
            ? $"{MinAttendees}+"
            : MinAttendees == MaxAttendees
                ? $"{MinAttendees}"
                : $"{MinAttendees}-{MaxAttendees}";

        return $"{range} attendees: {PricePerPerson}/person";
    }
}
