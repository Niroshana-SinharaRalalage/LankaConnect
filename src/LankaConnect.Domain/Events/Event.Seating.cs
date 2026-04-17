using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Domain.Events;

/// <summary>
/// Partial class extending Event with venue seating capabilities.
/// Manages seating mode and venue layout assignment.
/// </summary>
public partial class Event
{
    /// <summary>
    /// The seating mode for this event (GeneralAdmission = default, AssignedSeating = seat selection required).
    /// </summary>
    public SeatingMode SeatingMode { get; private set; } = SeatingMode.GeneralAdmission;

    /// <summary>
    /// The venue layout ID assigned to this event (null for GA events).
    /// Cross-aggregate reference — VenueLayout is a separate aggregate root.
    /// </summary>
    public Guid? VenueLayoutId { get; private set; }

    /// <summary>
    /// Whether this event uses assigned seating.
    /// </summary>
    public bool HasAssignedSeating => SeatingMode == SeatingMode.AssignedSeating;

    #region Seating Mode Management

    /// <summary>
    /// Sets the seating mode for this event.
    /// AssignedSeating requires Tiered ticketing mode.
    /// Seating mode is permanent after the first registration (Revision #7).
    /// </summary>
    public Result SetSeatingMode(SeatingMode mode)
    {
        if (SeatingMode == mode)
            return Result.Success();

        // Revision #7: Cannot change after any non-cancelled registration exists
        if (_registrations.Any(r =>
            r.Status == RegistrationStatus.Confirmed ||
            r.Status == RegistrationStatus.Preliminary))
        {
            return Result.Failure(
                "Seating mode cannot be changed after registrations exist. " +
                "Cancel all registrations first or create a new event.");
        }

        if (mode == SeatingMode.AssignedSeating)
        {
            // Assigned seating requires tiered ticketing mode (zones map to tiers)
            if (TicketingMode != TicketingMode.Tiered)
                return Result.Failure(
                    "Assigned seating requires tiered ticketing mode. " +
                    "Enable tiered ticketing first, then set seating mode.");
        }

        SeatingMode = mode;

        // If switching back to GA, clear the venue layout reference
        if (mode == SeatingMode.GeneralAdmission)
            VenueLayoutId = null;

        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Assigns a venue layout to this event.
    /// Layout assignment is only allowed when no registrations exist.
    /// </summary>
    public Result AssignVenueLayout(Guid venueLayoutId)
    {
        if (venueLayoutId == Guid.Empty)
            return Result.Failure("Venue layout ID is required");

        if (_registrations.Any(r =>
            r.Status == RegistrationStatus.Confirmed ||
            r.Status == RegistrationStatus.Preliminary))
        {
            return Result.Failure("Cannot change venue layout after registrations exist");
        }

        VenueLayoutId = venueLayoutId;
        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Removes the venue layout from this event.
    /// Only allowed when in GA mode or when no registrations exist.
    /// </summary>
    public Result RemoveVenueLayout()
    {
        if (!VenueLayoutId.HasValue)
            return Result.Failure("No venue layout is assigned to this event");

        if (SeatingMode == SeatingMode.AssignedSeating &&
            _registrations.Any(r =>
                r.Status == RegistrationStatus.Confirmed ||
                r.Status == RegistrationStatus.Preliminary))
        {
            return Result.Failure("Cannot remove venue layout while assigned seating is active with existing registrations");
        }

        VenueLayoutId = null;

        // Reset to GA if removing layout from an assigned seating event
        if (SeatingMode == SeatingMode.AssignedSeating)
            SeatingMode = SeatingMode.GeneralAdmission;

        MarkAsUpdated();
        return Result.Success();
    }

    #endregion
}
