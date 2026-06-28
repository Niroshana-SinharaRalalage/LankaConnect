using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.DomainEvents;

/// <summary>
/// Phase 8 S8.3 — domain event raised when a registration's seat reservations
/// must be released back to availability because the registration left the
/// "owns the seats" lifecycle states (Confirmed/PaymentCompleted).
///
/// Trigger paths (per ADR-011):
///   - <c>Registration.CompleteRefund</c> — buyer's refund settled by Stripe
///   - <c>Registration.MarkAbandoned</c> — Stripe checkout expired
///   - <c>Registration.FailPayment</c> — Stripe reported payment failure
///   - <c>Registration.Cancel</c> — organiser-side cancellation
///   - <c>Registration.ForceCancelStuckRefund</c> — operator force-cancel of
///     a row stuck in RefundRequested
///
/// The matching <c>SeatReservationsReleasedEventHandler</c> in the Application
/// layer hard-deletes the registration's <c>seat_reservations</c> rows (V1
/// architect-approved cancellation policy: hard-delete, not tombstone).
/// Idempotent — calling DeleteByRegistrationIdAsync on a registration that
/// has no reservations is a no-op.
/// </summary>
public record SeatReservationsReleasedEvent(
    Guid EventId,
    Guid RegistrationId,
    string Reason
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
