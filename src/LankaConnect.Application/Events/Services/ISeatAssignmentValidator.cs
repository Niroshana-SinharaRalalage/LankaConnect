using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;

namespace LankaConnect.Application.Events.Services;

/// <summary>
/// Phase 8 S8.2.B — validates that a buyer's selected seats are eligible for
/// stash-on-Registration before Stripe Checkout, and projects them into a
/// list of <see cref="PendingSeatAssignment"/> ready to feed into
/// <c>Registration.SetPendingSeatAssignments(...)</c>.
///
/// Reusable across <c>RsvpToEventCommandHandler</c> (auth path) and
/// <c>RegisterAnonymousAttendeeCommandHandler</c> (anonymous path) — both
/// need identical semantics and friendly error messages, so the validation
/// lives in one place.
///
/// Eligibility checks (all must pass; first failure short-circuits):
/// <list type="number">
///   <item>The event has a venue layout assigned (else "Layout not found").</item>
///   <item>Every <paramref name="seatIds"/> belongs to that layout (across
///   both zones and tables).</item>
///   <item>Every <paramref name="seatIds"/> is currently held in
///   <paramref name="seatSessionId"/> (rejects "borrowed" seat IDs from a
///   different buyer's session).</item>
///   <item>None of the <paramref name="seatIds"/> are already reserved
///   (defence in depth — the DB unique index will also catch this later).</item>
///   <item><paramref name="seatIds"/>.Count == <paramref name="attendeeCount"/>
///   (one seat per attendee).</item>
/// </list>
///
/// Returns a list of <see cref="PendingSeatAssignment"/> with attendee
/// indices in input order (i.e., <c>seatIds[0]</c> goes to attendee 0,
/// <c>seatIds[1]</c> to attendee 1, …). The seat label is denormalised from
/// the layout at validation time so the webhook can reach
/// <c>ConfirmSeatAssignments</c> without re-loading the layout.
/// </summary>
public interface ISeatAssignmentValidator
{
    Task<Result<IReadOnlyList<PendingSeatAssignment>>> ValidateAndBuildAssignmentsAsync(
        Guid eventId,
        string seatSessionId,
        IReadOnlyList<Guid> seatIds,
        int attendeeCount,
        CancellationToken cancellationToken = default);
}
