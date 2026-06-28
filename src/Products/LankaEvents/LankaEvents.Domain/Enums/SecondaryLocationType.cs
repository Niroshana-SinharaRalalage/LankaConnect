namespace LankaConnect.Domain.Events.Enums;

/// <summary>
/// Classifies the purpose of an event's optional secondary location.
/// Stored as a string in the database via HasConversion&lt;string&gt;() to survive enum reordering.
/// Serialized as string via the global JsonStringEnumConverter.
/// Phase 7C.1: Location Name + Secondary Location feature.
/// </summary>
public enum SecondaryLocationType
{
    /// <summary>
    /// The secondary location is a parking lot for event attendees.
    /// Displayed on the event details page as "Parking Lot Address:".
    /// </summary>
    ParkingLot = 0,

    /// <summary>
    /// The secondary location is an additional/secondary venue for the event
    /// (e.g., afterparty hall, overflow room, second-day venue).
    /// Displayed on the event details page as "Secondary Venue:".
    /// </summary>
    SecondaryVenue = 1
}
