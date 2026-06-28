namespace LankaConnect.Domain.Events.Enums;

/// <summary>
/// Represents the category of a generated ticket within a registration.
/// Standard: Legacy single ticket per registration (backward compatible)
/// Master: Group ticket for all attendees of one tier — used for group check-in
/// Individual: Per-attendee ticket — used for individual check-in
/// </summary>
public enum TicketCategory
{
    /// <summary>
    /// Legacy: one ticket per registration (SingleTier mode)
    /// </summary>
    Standard = 0,

    /// <summary>
    /// Master ticket grouping all attendees of one tier (e.g., "VIP Master — 2 attendees")
    /// </summary>
    Master = 1,

    /// <summary>
    /// Individual per-attendee ticket with own barcode
    /// </summary>
    Individual = 2
}
