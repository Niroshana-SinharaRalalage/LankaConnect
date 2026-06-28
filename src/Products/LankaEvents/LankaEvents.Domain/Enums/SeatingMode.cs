namespace LankaConnect.Domain.Events.Enums;

/// <summary>
/// Represents the seating mode for an event.
/// GeneralAdmission: Default — attendees get tier assignment but no specific seat.
/// AssignedSeating: Each attendee must select a specific seat during registration.
/// </summary>
public enum SeatingMode
{
    /// <summary>
    /// Default. No seat assignment required.
    /// Attendees register with tier selection only (if tiered).
    /// </summary>
    GeneralAdmission = 0,

    /// <summary>
    /// Seats must be selected during registration.
    /// Requires tiered ticketing mode with a venue layout assigned.
    /// Zones map to tiers for pricing.
    /// </summary>
    AssignedSeating = 1
}
