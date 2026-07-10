namespace LankaConnect.Products.LankaEvents.Contracts.LegacyPromotions; // Wave 6.5.g Day 5 (2026-07-10): promoted per Consult #15 PASS C — interfaces belong in Contracts. Interfaces are Payments-domain concepts (final home is Payments.Contracts in Phase B).

/// <summary>
/// Phase 6A.148.W5.6.A — single source of truth for the attendee-facing GRAND TOTAL
/// shown in the post-refund completion email + WhatsApp message.
///
/// Why this exists: <see cref="LankaConnect.Products.LankaEvents.Domain.DomainEvents.RefundCompletedEvent"/>
/// carries <c>RefundAmount</c> (Registration.TotalPrice = ticket portion) +
/// <c>AddOnRefundAmount</c> (legacy Registration column that the 6A.148 workflow path
/// never populates). For workflow refunds this undercounts by the Sponsor + Collection
/// amounts — operator UAT proof: refund request 329f2505 approved $262 across 4 lines
/// but the completion email displayed only $80 (the ticket).
///
/// Architecture (W5.6.A — handler-side aggregation per architect verdict):
/// <list type="number">
///   <item>Look up the workflow line by Stripe refund id (cheap indexed read).</item>
///   <item>If a workflow line exists, load the parent <c>RefundRequest</c> and SUM all
///     <see cref="LankaConnect.Products.LankaEvents.Domain.Enums.RefundLineItemStatus.Refunded"/>
///     line items' <c>ApprovedAmount</c> — the authoritative truth for the consolidated
///     email (matches what the Decision email already shows via the per-item table).</item>
///   <item>If no workflow line exists, the refund originated from the legacy
///     direct-Stripe <c>CancelRsvp</c> path — return the caller's
///     <paramref name="legacyFallbackTotal"/> verbatim (preserves byte-identical
///     email behaviour for pre-6A.148 flows).</item>
///   <item>If the lookup throws, log + fall through to legacy total — fail-OPEN so a
///     transient DB blip never silences the completion email.</item>
/// </list>
///
/// Timing guarantee (W5.5.D5 dispatcher contract): per-line dispatcher executes
/// serially, ticket line LAST. By the time <see cref="LankaConnect.Products.LankaEvents.Domain.Registration.CompleteRefundFromCancelled"/>
/// raises <c>RefundCompletedEvent</c> on the ticket webhook, every sibling line item
/// is already terminal in the DB. The SUM here is therefore the true settled total,
/// not a running partial. (If a sibling fails the Stripe call, its line stays in
/// <c>Failed</c> and is correctly EXCLUDED from the SUM.)
/// </summary>
public interface IRefundTotalCalculator
{
    /// <summary>
    /// Computes the dollar amount the attendee will see in the completion email.
    /// </summary>
    /// <param name="stripeRefundId">The Stripe refund id from <c>RefundCompletedEvent.StripeRefundId</c>.</param>
    /// <param name="legacyFallbackTotal">
    /// The pre-6A.148 formula's result (<c>RefundAmount + AddOnRefundAmount</c> from the event).
    /// Returned verbatim when no workflow line owns the refund (= legacy direct-Stripe refund path).
    /// </param>
    /// <param name="cancellationToken">Cooperative cancellation token.</param>
    Task<decimal> ComputeAttendeeFacingTotalAsync(
        string stripeRefundId,
        decimal legacyFallbackTotal,
        CancellationToken cancellationToken = default);
}
