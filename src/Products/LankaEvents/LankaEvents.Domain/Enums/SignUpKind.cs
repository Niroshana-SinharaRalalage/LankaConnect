namespace LankaConnect.Products.LankaEvents.Domain.Enums;

/// <summary>
/// Discriminates a SignUpList as either an item-bringing signup or a volunteer recruitment list.
/// Phase 7D.1: Volunteer signups reuse the SignUpList aggregate via this kind flag rather than
/// a parallel aggregate. Volunteer lists are constrained to slot-based items only (1 slot = 1
/// volunteer) via <see cref="Entities.SignUpList.CreateVolunteerList"/>.
/// </summary>
public enum SignUpKind
{
    /// <summary>
    /// Classic signup list where users commit to bringing items or taking slots for an event
    /// (food, decorations, supplies, etc.). Supports both quantity-based and slot-based items.
    /// </summary>
    Items = 0,

    /// <summary>
    /// Volunteer recruitment list where users sign up as volunteers for a role with a capped
    /// number of positions. Constrained to slot-based items only — the Volunteer factory rejects
    /// quantity-based items and user-submitted open roles. Example: "Food Committee: 5 volunteers".
    /// </summary>
    Volunteers = 1
}
