using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Domain.Events;

/// <summary>
/// Phase 7E: Per-event registration capture mode (DetailedAttendees / HeadCount* / NoRegistration).
/// Default <see cref="RegistrationMode.DetailedAttendees"/> preserves all pre-7E event behaviour.
/// </summary>
public partial class Event
{
    /// <summary>
    /// Phase 7E: Organiser-selected registration capture shape.
    /// - <see cref="RegistrationMode.DetailedAttendees"/> = today's per-attendee registration (default).
    /// - <see cref="RegistrationMode.HeadCountOnly"/> through <c>HeadCountByAgeAndGender"/> = head-count modes.
    /// - <see cref="RegistrationMode.NoRegistration"/> = drop-in event; standalone donations/sponsors/add-ons/collections still work.
    ///
    /// Persisted with DB-level <c>DEFAULT 0</c> so legacy events materialise as <see cref="RegistrationMode.DetailedAttendees"/>
    /// automatically (Phase 6A.123 lesson).
    /// </summary>
    public RegistrationMode RegistrationMode { get; private set; } = RegistrationMode.DetailedAttendees;

    /// <summary>
    /// Phase 7E: Sets the registration mode for this event.
    ///
    /// Business rules:
    /// 1. Mode change is forbidden once <see cref="Registrations"/>.Any() — protects historical data.
    ///    Mode A↔B conversion with attendee backfill is deferred to Phase 7F.
    /// 2. Standalone contributions (donations / sponsors / add-on purchases / collections) are
    ///    intentionally NOT considered by this guard. They are mode-agnostic by design — their
    ///    aggregates live outside the <c>Event.Registrations</c> collection (verified in 7E.0
    ///    audit §6: Event has no <c>Donations</c>/<c>Sponsors</c>/<c>AddOnPurchases</c>/<c>Collections</c>
    ///    navigation collections; only nullable <c>*Configuration</c> value-objects).
    /// 3. Compatibility with pricing / seating / add-on shapes is enforced at the application layer
    ///    via <c>FluentValidation</c> in 7E.2 (the 14-row compatibility table). This domain method
    ///    only enforces the registration-locking rule.
    /// </summary>
    /// <param name="mode">The new registration mode.</param>
    /// <returns>Result indicating success or failure with a clear message.</returns>
    public Result SetRegistrationMode(RegistrationMode mode)
    {
        // Architect rule (Phase 7E plan §3.2): forbid mode change once registrations exist.
        if (_registrations.Any())
        {
            return Result.Failure(
                $"Cannot change registration mode while registrations exist. " +
                $"Existing registrations: {_registrations.Count}. " +
                $"Mode change with attendee backfill is deferred to Phase 7F. " +
                $"EventId={Id}, CurrentMode={RegistrationMode}, RequestedMode={mode}");
        }

        if (RegistrationMode == mode)
        {
            return Result.Success(); // Idempotent — no change to make.
        }

        RegistrationMode = mode;
        MarkAsUpdated();
        return Result.Success();
    }
}
