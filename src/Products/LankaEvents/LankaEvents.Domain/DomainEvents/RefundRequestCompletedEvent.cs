using LankaConnect.BuildingBlocks.Domain.Contracts;
using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.Products.LankaEvents.Domain.DomainEvents;

/// <summary>
/// Phase 6A.148.W5.6.B — raised from <see cref="LankaConnect.Products.LankaEvents.Domain.Entities.RefundRequest.MarkCompletedIfAllSettled"/>
/// at the exact moment the request's <see cref="LankaConnect.Products.LankaEvents.Domain.Enums.RefundRequestStatus"/>
/// flips to <see cref="LankaConnect.Products.LankaEvents.Domain.Enums.RefundRequestStatus.Completed"/>.
///
/// Replaces the workflow-path use of <see cref="RefundCompletedEvent"/> (which was emitted
/// from the entity-level <c>Registration.CompleteRefundFromCancelled</c> when a single
/// charge.refunded webhook arrived — race-prone because sibling lines could still be
/// in flight inside the per-line dispatcher). The 4th-report regression
/// (operator UAT 2026-05-23, RR 86d0a7dc displayed $94 instead of $204) was caused by
/// that race: the ticket webhook fired the email-raising event at 21:04:26.540 while
/// the Sponsor line was still being processed by the dispatcher (its commit landed
/// 831ms later at 21:04:27.371).
///
/// This event is raised from inside the aggregate's <c>MarkCompletedIfAllSettled</c>
/// state-machine guard <c>_lineItems.All(...terminal)</c> — so by construction every
/// line item is in a terminal state (<see cref="LankaConnect.Products.LankaEvents.Domain.Enums.RefundLineItemStatus.Refunded"/>,
/// <see cref="LankaConnect.Products.LankaEvents.Domain.Enums.RefundLineItemStatus.Failed"/>, or
/// <see cref="LankaConnect.Products.LankaEvents.Domain.Enums.RefundLineItemStatus.Rejected"/>) at the
/// moment the event fires. The total carried below is therefore the final settled
/// amount; no race window can produce an undercount.
///
/// The event carries the precomputed <see cref="TotalRefundedAmount"/> so the email +
/// WhatsApp handlers don't need to re-query the DB — they just render. The
/// <see cref="PrimaryStripeRefundId"/> is the ticket line's Stripe refund id when present
/// (operator-recognisable as "the registration refund"), else the first Refunded line's
/// id, else null when all lines were Rejected (no money moved).
///
/// Legacy direct-Stripe path (pre-6A.148 CancelRsvp) keeps using
/// <see cref="RefundCompletedEvent"/> via <c>Registration.CompleteRefund</c> — no RR
/// exists for that path so this event cannot fire there. The two events are mutually
/// exclusive by construction.
/// </summary>
public record RefundRequestCompletedEvent(
    Guid RefundRequestId,
    Guid RegistrationId,
    string? PrimaryStripeRefundId,
    decimal TotalRefundedAmount,
    string Currency,
    DateTime CompletedAt
) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
