namespace LankaConnect.Domain.Events.Enums;

/// <summary>
/// Represents the ticketing mode for an event.
/// SingleTier: Legacy mode — one price config applies to all attendees (Single/AgeDual/GroupTiered)
/// Tiered: Multi-tier mode — each tier (VIP/Plus/Basic/Custom) has its own pricing and capacity
/// </summary>
public enum TicketingMode
{
    /// <summary>
    /// Default. Event uses a single pricing configuration for all attendees.
    /// Compatible with existing PricingType (Single, AgeDual, GroupTiered).
    /// </summary>
    SingleTier = 0,

    /// <summary>
    /// Event has multiple ticket tiers (e.g., VIP, Plus, Basic).
    /// Each tier defines its own adult/child pricing and capacity.
    /// </summary>
    Tiered = 1
}
