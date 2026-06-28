namespace LankaConnect.Domain.Events.Enums;

/// <summary>
/// Phase 6D: Represents the type of pricing model for an event
/// Single: Flat rate per attendee (legacy TicketPrice or single AdultPrice)
/// AgeDual: Age-based pricing with separate adult and child rates
/// GroupTiered: Quantity-based pricing tiers (e.g., 1-2 = $15/person, 3+ = $12/person)
/// </summary>
public enum PricingType
{
    /// <summary>
    /// Single flat price for all attendees (legacy model)
    /// </summary>
    Single = 0,

    /// <summary>
    /// Dual pricing based on age (Adult/Child) - Session 21
    /// </summary>
    AgeDual = 1,

    /// <summary>
    /// Group-based tiered pricing with quantity discounts - Phase 6D
    /// </summary>
    GroupTiered = 2
}
