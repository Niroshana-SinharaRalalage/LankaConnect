using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Domain.Events.ValueObjects;

/// <summary>
/// Value object representing ticket pricing configuration for an event
/// Supports both single pricing (adult only) and dual pricing (adult + child)
/// </summary>
public class TicketPricing : ValueObject
{
    public Money AdultPrice { get; }
    public Money? ChildPrice { get; }
    public int? ChildAgeLimit { get; }

    /// <summary>
    /// Indicates whether this pricing configuration includes separate child pricing
    /// </summary>
    public bool HasChildPricing => ChildPrice != null && ChildAgeLimit != null;

    // EF Core constructor
    private TicketPricing()
    {
        // Required for EF Core
        AdultPrice = null!;
    }

    private TicketPricing(Money adultPrice, Money? childPrice, int? childAgeLimit)
    {
        AdultPrice = adultPrice;
        ChildPrice = childPrice;
        ChildAgeLimit = childAgeLimit;
    }

    /// <summary>
    /// Creates a new TicketPricing configuration
    /// </summary>
    /// <param name="adultPrice">Required adult ticket price</param>
    /// <param name="childPrice">Optional child ticket price (null for single pricing)</param>
    /// <param name="childAgeLimit">Optional age limit for child pricing (inclusive, 1-18)</param>
    public static Result<TicketPricing> Create(Money? adultPrice, Money? childPrice, int? childAgeLimit)
    {
        // Validation: Adult price is required
        if (adultPrice == null)
            return Result<TicketPricing>.Failure("Adult price is required");

        // Validation: Child price and age limit must both be provided or both be null
        if (childPrice != null && childAgeLimit == null)
            return Result<TicketPricing>.Failure("Child age limit is required when child price is specified");

        if (childPrice == null && childAgeLimit != null)
            return Result<TicketPricing>.Failure("Child price is required when age limit is specified");

        // Validation: Child age limit must be between 1 and 18
        if (childAgeLimit.HasValue && (childAgeLimit.Value < 1 || childAgeLimit.Value > 18))
            return Result<TicketPricing>.Failure("Child age limit must be between 1 and 18 years");

        // Validation: Prices must use the same currency (check before price comparison)
        if (childPrice != null && adultPrice.Currency != childPrice.Currency)
            return Result<TicketPricing>.Failure("Adult and child prices must use the same currency");

        // Validation: Child price cannot be greater than adult price
        if (childPrice != null && childPrice.IsGreaterThan(adultPrice))
            return Result<TicketPricing>.Failure("Child price cannot be greater than adult price");

        return Result<TicketPricing>.Success(new TicketPricing(adultPrice, childPrice, childAgeLimit));
    }

    /// <summary>
    /// Determines if the given age qualifies for child pricing
    /// </summary>
    /// <param name="age">Age to check</param>
    /// <returns>True if age qualifies for child pricing, false otherwise</returns>
    public bool IsChildAge(int age)
    {
        if (!HasChildPricing)
            return false;

        if (age <= 0)
            return false;

        return age <= ChildAgeLimit!.Value;
    }

    /// <summary>
    /// Calculates the ticket price for an attendee based on their age
    /// </summary>
    /// <param name="age">Attendee age</param>
    /// <returns>Appropriate ticket price (child or adult)</returns>
    public Money CalculateForAttendee(int age)
    {
        if (HasChildPricing && IsChildAge(age))
            return ChildPrice!;

        return AdultPrice;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return AdultPrice;
        if (ChildPrice != null)
            yield return ChildPrice;
        if (ChildAgeLimit != null)
            yield return ChildAgeLimit;
    }

    public override string ToString()
    {
        if (HasChildPricing)
            return $"Adult: {AdultPrice}, Child (≤{ChildAgeLimit}): {ChildPrice}";

        return $"Single Price: {AdultPrice}";
    }
}
