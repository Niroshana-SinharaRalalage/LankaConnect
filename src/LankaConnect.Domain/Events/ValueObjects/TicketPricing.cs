using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Domain.Events.ValueObjects;

/// <summary>
/// Phase 6D: Value object representing ticket pricing configuration for an event
/// Supports three pricing models: Single, AgeDual (Adult/Child), and GroupTiered (quantity-based)
/// </summary>
public class TicketPricing : ValueObject
{
    /// <summary>
    /// Type of pricing model
    /// </summary>
    public PricingType Type { get; private set; }

    /// <summary>
    /// Adult price (used for Single and AgeDual pricing types)
    /// </summary>
    public Money AdultPrice { get; private set; }

    /// <summary>
    /// Child price (only for AgeDual pricing type)
    /// </summary>
    public Money? ChildPrice { get; private set; }

    /// <summary>
    /// Child age limit (only for AgeDual pricing type, inclusive 1-18)
    /// </summary>
    public int? ChildAgeLimit { get; private set; }

    /// <summary>
    /// Phase 6D: Group pricing tiers (only for GroupTiered pricing type)
    /// </summary>
    private readonly List<GroupPricingTier> _groupTiers = new();
    public IReadOnlyList<GroupPricingTier> GroupTiers => _groupTiers.AsReadOnly();

    /// <summary>
    /// Currency used for all prices in this configuration
    /// </summary>
    public Currency Currency { get; private set; }

    /// <summary>
    /// Indicates whether this pricing configuration includes separate child pricing
    /// </summary>
    public bool HasChildPricing => Type == PricingType.AgeDual && ChildPrice != null && ChildAgeLimit != null;

    /// <summary>
    /// Phase 6D: Indicates whether this pricing uses group tiers
    /// </summary>
    public bool HasGroupTiers => Type == PricingType.GroupTiered && _groupTiers.Count > 0;

    // EF Core constructor
    private TicketPricing()
    {
        // Required for EF Core
        AdultPrice = null!;
    }

    private TicketPricing(
        PricingType type,
        Money adultPrice,
        Money? childPrice,
        int? childAgeLimit,
        List<GroupPricingTier>? groupTiers)
    {
        Type = type;
        AdultPrice = adultPrice;
        ChildPrice = childPrice;
        ChildAgeLimit = childAgeLimit;
        Currency = adultPrice?.Currency ?? groupTiers?.FirstOrDefault()?.PricePerPerson.Currency ?? Currency.USD;

        if (groupTiers != null && groupTiers.Any())
        {
            _groupTiers = groupTiers;
        }
    }

    #region Factory Methods

    /// <summary>
    /// Phase 6D: Creates single-price configuration (flat rate for all attendees)
    /// </summary>
    public static Result<TicketPricing> CreateSinglePrice(Money? price)
    {
        if (price == null)
            return Result<TicketPricing>.Failure("Single price is required");

        return Result<TicketPricing>.Success(new TicketPricing(
            PricingType.Single,
            price,
            null,
            null,
            null));
    }

    /// <summary>
    /// Phase 6D: Creates age-based dual pricing (Adult/Child)
    /// </summary>
    public static Result<TicketPricing> CreateDualPrice(Money? adultPrice, Money? childPrice, int? childAgeLimit)
    {
        // Validation: Adult price is required
        if (adultPrice == null)
            return Result<TicketPricing>.Failure("Adult price is required");

        // Validation: Child price and age limit must both be provided
        if (childPrice == null || childAgeLimit == null)
            return Result<TicketPricing>.Failure("Both child price and age limit are required for dual pricing");

        // Validation: Child age limit must be between 1 and 18
        if (childAgeLimit.Value < 1 || childAgeLimit.Value > 18)
            return Result<TicketPricing>.Failure("Child age limit must be between 1 and 18 years");

        // Validation: Prices must use the same currency
        if (adultPrice.Currency != childPrice.Currency)
            return Result<TicketPricing>.Failure("Adult and child prices must use the same currency");

        // Validation: Child price cannot be greater than adult price
        if (childPrice.IsGreaterThan(adultPrice))
            return Result<TicketPricing>.Failure("Child price cannot be greater than adult price");

        return Result<TicketPricing>.Success(new TicketPricing(
            PricingType.AgeDual,
            adultPrice,
            childPrice,
            childAgeLimit,
            null));
    }

    /// <summary>
    /// Phase 6D: Creates group-based tiered pricing with quantity discounts
    /// </summary>
    public static Result<TicketPricing> CreateGroupTiered(List<GroupPricingTier>? tiers, Currency currency)
    {
        // Validation: At least one tier is required
        if (tiers == null || !tiers.Any())
            return Result<TicketPricing>.Failure("At least one tier is required for group pricing");

        // Validation: All tiers must use the same currency
        if (tiers.Any(t => t.PricePerPerson.Currency != currency))
            return Result<TicketPricing>.Failure("All tiers must use the same currency");

        // Sort tiers by MinAttendees for validation
        var sortedTiers = tiers.OrderBy(t => t.MinAttendees).ToList();

        // Validation: First tier must start at 1
        if (sortedTiers.First().MinAttendees != 1)
            return Result<TicketPricing>.Failure("Group pricing tiers must start at 1 attendee");

        // Validation: Check for overlaps
        for (int i = 0; i < sortedTiers.Count - 1; i++)
        {
            if (sortedTiers[i].OverlapsWith(sortedTiers[i + 1]))
                return Result<TicketPricing>.Failure($"Tiers cannot overlap: Tier starting at {sortedTiers[i].MinAttendees} overlaps with tier starting at {sortedTiers[i + 1].MinAttendees}");
        }

        // Validation: Check for gaps (unless last tier is unlimited)
        for (int i = 0; i < sortedTiers.Count - 1; i++)
        {
            var currentTier = sortedTiers[i];
            var nextTier = sortedTiers[i + 1];

            // Current tier must have MaxAttendees if it's not the last tier
            if (!currentTier.MaxAttendees.HasValue)
                return Result<TicketPricing>.Failure($"Only the last tier can be unlimited. Tier starting at {currentTier.MinAttendees} must have a maximum.");

            // Next tier must start immediately after current tier ends
            if (nextTier.MinAttendees != currentTier.MaxAttendees.Value + 1)
                return Result<TicketPricing>.Failure($"gap detected between tiers: Tier ending at {currentTier.MaxAttendees} and tier starting at {nextTier.MinAttendees}");
        }

        // Create a dummy Money object for the AdultPrice (required by constructor, but not used for GroupTiered)
        var dummyPrice = Money.Create(0, currency).Value;

        return Result<TicketPricing>.Success(new TicketPricing(
            PricingType.GroupTiered,
            dummyPrice,
            null,
            null,
            sortedTiers));
    }

    /// <summary>
    /// Legacy Create method for backward compatibility (Single or AgeDual pricing)
    /// </summary>
    public static Result<TicketPricing> Create(Money? adultPrice, Money? childPrice, int? childAgeLimit)
    {
        // Validation: Adult price is required (backward compatibility)
        if (adultPrice == null)
            return Result<TicketPricing>.Failure("Adult price is required");

        // If child pricing is provided, create dual pricing
        if (childPrice != null || childAgeLimit != null)
        {
            if (childPrice == null || childAgeLimit == null)
            {
                return Result<TicketPricing>.Failure(
                    childPrice == null
                        ? "Child price is required when age limit is specified"
                        : "Child age limit is required when child price is specified");
            }

            return CreateDualPrice(adultPrice, childPrice, childAgeLimit);
        }

        // Otherwise, create single pricing
        return CreateSinglePrice(adultPrice);
    }

    #endregion

    #region Age-based Pricing Methods

    /// <summary>
    /// Calculates the ticket price for an attendee based on their age category
    /// Child category gets child price if available, otherwise adult price
    /// </summary>
    public Money CalculateForCategory(AgeCategory ageCategory)
    {
        if (HasChildPricing && ageCategory == AgeCategory.Child)
            return ChildPrice!;

        return AdultPrice;
    }

    /// <summary>
    /// Determines if the given age qualifies for child pricing (AgeDual only)
    /// Kept for backward compatibility during migration
    /// </summary>
    public bool IsChildAge(int age)
    {
        if (!HasChildPricing)
            return false;

        if (age <= 0)
            return false;

        return age <= ChildAgeLimit!.Value;
    }

    /// <summary>
    /// Calculates the ticket price for an attendee based on their age (AgeDual only)
    /// Kept for backward compatibility during migration - use CalculateForCategory instead
    /// </summary>
    [Obsolete("Use CalculateForCategory(AgeCategory) instead")]
    public Money CalculateForAttendee(int age)
    {
        if (HasChildPricing && IsChildAge(age))
            return ChildPrice!;

        return AdultPrice;
    }

    #endregion

    #region Group Tiered Pricing Methods

    /// <summary>
    /// Phase 6D: Finds the appropriate tier for the given attendee count
    /// </summary>
    public Result<GroupPricingTier> FindTierForAttendeeCount(int attendeeCount)
    {
        if (Type != PricingType.GroupTiered)
            return Result<GroupPricingTier>.Failure("Tier finding is only available for GroupTiered pricing");

        if (attendeeCount < 1)
            return Result<GroupPricingTier>.Failure("Attendee count must be at least 1");

        var matchingTier = _groupTiers.FirstOrDefault(t => t.CoversAttendeeCount(attendeeCount));

        if (matchingTier == null)
            return Result<GroupPricingTier>.Failure($"No tier found for {attendeeCount} attendees");

        return Result<GroupPricingTier>.Success(matchingTier);
    }

    /// <summary>
    /// Phase 6D: Calculates total price for a group based on attendee count
    /// </summary>
    public Result<Money> CalculateGroupPrice(int attendeeCount)
    {
        if (Type != PricingType.GroupTiered)
            return Result<Money>.Failure("Group price calculation is only available for GroupTiered pricing");

        var tierResult = FindTierForAttendeeCount(attendeeCount);
        if (tierResult.IsFailure)
            return Result<Money>.Failure(tierResult.Error);

        var tier = tierResult.Value;
        var totalPriceResult = tier.PricePerPerson.Multiply(attendeeCount);

        if (totalPriceResult.IsFailure)
            return Result<Money>.Failure(totalPriceResult.Error);

        return Result<Money>.Success(totalPriceResult.Value);
    }

    #endregion

    #region Value Object Overrides

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Type;

        // Include AdultPrice for Single and AgeDual
        if (Type == PricingType.Single || Type == PricingType.AgeDual)
        {
            yield return AdultPrice;

            if (ChildPrice != null)
                yield return ChildPrice;

            if (ChildAgeLimit != null)
                yield return ChildAgeLimit;
        }

        // Include GroupTiers for GroupTiered
        if (Type == PricingType.GroupTiered)
        {
            foreach (var tier in _groupTiers)
                yield return tier;
        }
    }

    public override string ToString()
    {
        return Type switch
        {
            PricingType.Single => $"Single Price: {AdultPrice}",
            PricingType.AgeDual => $"Adult: {AdultPrice}, Child (≤{ChildAgeLimit}): {ChildPrice}",
            PricingType.GroupTiered => $"Group Pricing: {string.Join(", ", _groupTiers.Select(t => t.ToString()))}",
            _ => "Unknown Pricing Type"
        };
    }

    #endregion
}
