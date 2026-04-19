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

    #region Orchestrated Seating Helpers (Slice 2+3A)

    /// <summary>
    /// Atomically enables assigned seating and links this event to an already-persisted
    /// venue layout. This is the sanctioned entry point used by the 3-transaction
    /// orchestration (Slice 2+3B): Transaction 2 persists the layout; Transaction 3
    /// calls this method and flips <see cref="SeatingMode"/>. Throws
    /// <see cref="InvalidOperationException"/> when called with <see cref="Guid.Empty"/>
    /// so callers cannot bypass the orchestrator by linking an unpersisted layout.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="venueLayoutId"/> is empty — indicates the caller
    /// skipped the layout-persistence transaction.
    /// </exception>
    public Result EnableAssignedSeating(Guid venueLayoutId)
    {
        if (venueLayoutId == Guid.Empty)
            throw new InvalidOperationException(
                "EnableAssignedSeating requires a persisted VenueLayout ID. " +
                "Persist the layout in a prior transaction before calling this method.");

        if (TicketingMode != TicketingMode.Tiered)
            return Result.Failure(
                "Assigned seating requires tiered ticketing mode. " +
                "Enable tiered ticketing first, then enable assigned seating.");

        if (_registrations.Any(r =>
            r.Status == RegistrationStatus.Confirmed ||
            r.Status == RegistrationStatus.Preliminary))
        {
            return Result.Failure(
                "Assigned seating cannot be enabled after registrations exist.");
        }

        VenueLayoutId = venueLayoutId;
        SeatingMode = SeatingMode.AssignedSeating;
        MarkAsUpdated();
        return Result.Success();
    }

    /// <summary>
    /// Atomically disables assigned seating and detaches the venue layout. Mirror of
    /// <see cref="EnableAssignedSeating"/> for the toggle-off flow.
    /// </summary>
    public Result DisableAssignedSeating()
    {
        if (SeatingMode == SeatingMode.GeneralAdmission && !VenueLayoutId.HasValue)
            return Result.Success();

        if (_registrations.Any(r =>
            r.Status == RegistrationStatus.Confirmed ||
            r.Status == RegistrationStatus.Preliminary))
        {
            return Result.Failure(
                "Assigned seating cannot be disabled after registrations exist.");
        }

        SeatingMode = SeatingMode.GeneralAdmission;
        VenueLayoutId = null;
        MarkAsUpdated();
        return Result.Success();
    }

    #endregion
}
