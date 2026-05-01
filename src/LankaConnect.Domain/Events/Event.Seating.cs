using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Entities;
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

    #region Publish Readiness (Slice 9.1)

    /// <summary>
    /// Slice 9.1: returns success when the supplied <paramref name="layout"/> is ready to
    /// be published with this event — every zone with seats must be mapped to an active
    /// ticket tier, capacity invariants hold, and the layout's id matches this event's
    /// <see cref="VenueLayoutId"/>.
    ///
    /// <para>
    /// This is the strict counterpart to the permissive validation in
    /// <see cref="VenueLayout.ValidateForEvent"/> with <c>requireTierMapping=false</c>
    /// used at apply-preset / apply-template time. The split lets organisers attach a
    /// preset and customise it incrementally before committing to publish — but blocks
    /// publication until the layout is fully wired up.
    /// </para>
    ///
    /// <para>
    /// Returns <see cref="Result.Success"/> when layout is null + this event has
    /// no <see cref="VenueLayoutId"/> assigned (general-admission events are publish-
    /// ready without a layout). Returns failure if the caller hands a layout whose id
    /// does not match this event's assignment (defence in depth — protects against a
    /// mis-wired application layer fetching the wrong aggregate).
    /// </para>
    /// </summary>
    public Result CheckLayoutPublishReadiness(VenueLayout? layout)
    {
        // GA event with no layout — nothing to validate.
        if (!VenueLayoutId.HasValue)
            return layout is null
                ? Result.Success()
                : Result.Failure("Event has no venue layout assigned but a layout was supplied");

        // Seated event MUST have a layout supplied.
        if (layout is null)
            return Result.Failure("Event references a venue layout but none was supplied for the readiness check");

        // Cross-aggregate id sanity — the caller (handler) is responsible for fetching
        // the correct layout. If they hand the wrong one, fail fast and loud.
        if (layout.Id != VenueLayoutId.Value)
            return Result.Failure(
                $"Layout id mismatch: event references {VenueLayoutId.Value} but supplied layout is {layout.Id}");

        // Strict tier-mapping + capacity invariants.
        return layout.ValidateForEvent(_ticketTiers, requireTierMapping: true);
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

        // Slice S1.5 invariant: AssignedSeating requires DetailedAttendees registration.
        // Mode B (head-count*) tracks aggregated counts only — there is no individual
        // attendee to bind a seat to. The buyer flow's HeadCountRsvpForm has no seat
        // picker. Allowing the combination here would let the organiser publish an event
        // with AssignedSeating that buyers literally cannot register for.
        // Mode C (NoRegistration) has no buyer flow at all.
        if (RegistrationMode != RegistrationMode.DetailedAttendees)
            return Result.Failure(
                $"Assigned seating requires individual-attendee registration (DetailedAttendees mode). " +
                $"This event uses {RegistrationMode} which tracks counts, not individuals — " +
                $"the buyer flow cannot map seats to attendees in that mode. " +
                $"Switch the registration mode to DetailedAttendees first, or keep general-admission seating.");

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
