using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Domain.Events;

/// <summary>
/// Phase 6A.153: Organizer-controlled registration window.
///
/// Lets organizers publish an event in advance with registration held closed
/// until a date they choose (e.g. open registration two weeks before a paid
/// event). Both <see cref="Event.RegistrationOpensAt"/> and
/// <see cref="Event.RegistrationClosesAt"/> are nullable; null = legacy
/// "always open" behaviour, backward-compatible with all pre-6A.153 events.
///
/// Window enforcement lives in the three <c>Register*</c> methods (defense in
/// depth, single source of truth). This file owns the mutator + invariants.
/// </summary>
public partial class Event
{
    /// <summary>
    /// Sets the registration window. Both arguments are nullable — pass
    /// <c>null</c> for either to clear that side of the window. Times are
    /// coerced to UTC so callers don't have to worry about Kind.
    /// </summary>
    /// <param name="opensAt">Earliest UTC moment registration is accepted. Null = open immediately.</param>
    /// <param name="closesAt">Latest UTC moment registration is accepted. Null = open until <see cref="StartDate"/>.</param>
    /// <returns>
    /// Success when the window is consistent with the event's current state.
    /// Failure when an invariant is violated (status-locked / opens-after-closes
    /// / closes-after-start / opens-after-start).
    /// </returns>
    public Result SetRegistrationWindow(DateTime? opensAt, DateTime? closesAt)
    {
        // D4: window is editable in Planning / Draft / Published only.
        // Locked once the event is in a terminal or in-flight state, because
        // changing the window after the event has been cancelled / completed
        // is at best a no-op and at worst misleading audit data.
        if (Status == EventStatus.Cancelled
            || Status == EventStatus.Completed
            || Status == EventStatus.Archived
            || Status == EventStatus.Active)
        {
            return Result.Failure(
                $"Registration window cannot be changed when event is {Status}.");
        }

        // Coerce to UTC before any comparison so the invariants are timezone-clean.
        var opensUtc = CoerceToUtc(opensAt);
        var closesUtc = CoerceToUtc(closesAt);

        // D3 invariant #1: when both set, opens must strictly precede closes.
        // Strict inequality (not <=) because an instantaneous open-and-close
        // window is degenerate — the FE state-machine treats it as
        // "closed-by-organizer" the moment closesAt is reached.
        if (opensUtc.HasValue && closesUtc.HasValue && opensUtc.Value >= closesUtc.Value)
        {
            return Result.Failure(
                "Registration opens-at must be before registration closes-at.");
        }

        // D3 invariant #2: closesAt cannot be after StartDate (when StartDate is set).
        // ClosesAt > StartDate would mean "accept registrations after the event
        // begins", which the existing StartDate guard already rejects. Catch it
        // here for the organizer-form UX rather than only at register-time.
        if (closesUtc.HasValue && StartDate.HasValue && closesUtc.Value > StartDate.Value)
        {
            return Result.Failure(
                "Registration closes-at cannot be after the event start date.");
        }

        // D3 invariant #3: opensAt cannot be at-or-after StartDate. "We open
        // the moment the event starts" is logically a no-op (the StartDate
        // guard rejects immediately afterwards). Block at configuration time.
        if (opensUtc.HasValue && StartDate.HasValue && opensUtc.Value >= StartDate.Value)
        {
            return Result.Failure(
                "Registration opens-at must be before the event start date.");
        }

        // D3 invariant #4 (implicit): TBD events (StartDate == null) accept
        // any opensAt / closesAt pair. The Save-the-date use case is exactly
        // "RSVPs open Jan 15 for an event whose date we'll lock in later."
        // The two cross-checks above short-circuit on `StartDate.HasValue` so
        // TBD events flow straight through.

        RegistrationOpensAt = opensUtc;
        RegistrationClosesAt = closesUtc;
        MarkAsUpdated();

        return Result.Success();
    }

    /// <summary>
    /// Convenience reader used by the three Register* methods. Returns the
    /// effective availability state without mutating anything. Centralises
    /// the boundary logic so all four call sites (the three Register methods
    /// + the mapper that computes <c>RegistrationAvailability</c>) stay in
    /// lockstep.
    /// </summary>
    /// <remarks>
    /// Boundary semantics (locked with architect, 2026-05-25):
    /// <list type="bullet">
    ///   <item><description>OpensAt: <c>OpensAt &gt; UtcNow</c> blocks. At the exact moment
    ///         it equals UtcNow, registration is open (<c>&lt;=</c>, not <c>&lt;</c>).</description></item>
    ///   <item><description>ClosesAt: <c>ClosesAt &lt;= UtcNow</c> blocks (strict
    ///         &lt;=). At the exact moment it equals UtcNow, registration is
    ///         closed.</description></item>
    /// </list>
    /// </remarks>
    internal RegistrationAvailabilityState GetRegistrationAvailability(DateTime utcNow)
    {
        // StartDate guard takes priority — matches the existing Register*
        // ordering (Status → StartDate → window → ...). An event that has
        // already started is "closed-event-started" regardless of window.
        if (StartDate.HasValue && StartDate.Value <= utcNow)
        {
            return RegistrationAvailabilityState.ClosedEventStarted;
        }

        if (RegistrationOpensAt.HasValue && RegistrationOpensAt.Value > utcNow)
        {
            return RegistrationAvailabilityState.NotYetOpen;
        }

        if (RegistrationClosesAt.HasValue && RegistrationClosesAt.Value <= utcNow)
        {
            return RegistrationAvailabilityState.ClosedByOrganizer;
        }

        return RegistrationAvailabilityState.Open;
    }

    private static DateTime? CoerceToUtc(DateTime? value)
    {
        if (!value.HasValue) return null;
        return value.Value.Kind == DateTimeKind.Utc
            ? value.Value
            : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
    }
}

/// <summary>
/// Phase 6A.153: Internal enum mirroring the four <c>RegistrationAvailability</c>
/// string-union values the FE consumes. Internal because it never leaves the
/// domain assembly; the mapper translates to the wire-format string.
/// </summary>
internal enum RegistrationAvailabilityState
{
    Open,
    NotYetOpen,
    ClosedByOrganizer,
    ClosedEventStarted
}
