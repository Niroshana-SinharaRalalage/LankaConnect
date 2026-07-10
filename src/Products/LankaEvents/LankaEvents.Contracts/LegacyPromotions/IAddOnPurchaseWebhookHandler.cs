namespace LankaConnect.Products.LankaEvents.Contracts.LegacyPromotions; // Wave 6.5.g Day 5 (2026-07-10): promoted per Consult #15 PASS C — webhook handler interfaces belong in Contracts (final home Payments.Contracts in Phase B).

/// <summary>
/// Handles Stripe webhook events for add-on purchase payments.
/// Phase 0: Placeholder interface — implementation will be added in Phase 3D.
/// </summary>
public interface IAddOnPurchaseWebhookHandler
{
    /// <summary>
    /// Handles checkout.session.completed for add-on purchase payments.
    /// </summary>
    Task HandleCheckoutCompletedAsync(
        string sessionId,
        string paymentIntentId,
        Dictionary<string, string> metadata,
        Guid correlationId,
        CancellationToken ct = default);

    /// <summary>
    /// Handles checkout.session.expired for add-on purchase payments.
    /// </summary>
    Task HandleCheckoutExpiredAsync(
        string sessionId,
        Dictionary<string, string> metadata,
        Guid correlationId,
        CancellationToken ct = default);

    /// <summary>
    /// Phase 6A.148.W4.D11 (G1 fix): handles Stripe's <c>charge.refunded</c> webhook
    /// for add-on purchases. Before this method existed, the PaymentsController
    /// switch-case for <c>add_on_purchase</c> / <c>add_on_cancellation</c> was a NO-OP
    /// (returned without touching the entity), so AddOnPurchase rows stayed
    /// <c>Status=Completed</c> even after Stripe successfully refunded the money —
    /// operator UAT defect F2.
    ///
    /// Cart-aware: when N purchases share a single PaymentIntent (cart checkout),
    /// the workflow refund issues one Stripe refund call per line item, so multiple
    /// <c>charge.refunded</c> webhooks land with the same PaymentIntent. The implementation
    /// resolves the specific purchase via the workflow line-item's <c>ReferenceId</c>
    /// when present (workflow path); falls back to refunding ALL purchases sharing
    /// the PI when no line item is found (legacy cart-refund semantics).
    ///
    /// Idempotent: already-Refunded purchases are skipped with a WARN log.
    /// Fail-silent: errors are caught + logged; no exception propagates to Stripe.
    /// </summary>
    Task HandleChargeRefundedAsync(
        string paymentIntentId,
        string refundId,
        Guid correlationId,
        CancellationToken ct = default);
}
