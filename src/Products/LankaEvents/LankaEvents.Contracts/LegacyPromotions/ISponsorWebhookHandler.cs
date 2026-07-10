namespace LankaConnect.Products.LankaEvents.Contracts.LegacyPromotions; // Wave 6.5.g Day 5 (2026-07-10): promoted per Consult #15 PASS C — webhook handler interfaces belong in Contracts (final home Payments.Contracts in Phase B).

/// <summary>
/// Handles Stripe webhook events for sponsor payments.
/// Phase 0: Placeholder interface — implementation will be added in Phase 3D.
/// </summary>
public interface ISponsorWebhookHandler
{
    /// <summary>
    /// Handles checkout.session.completed for sponsor payments.
    /// </summary>
    Task HandleCheckoutCompletedAsync(
        string sessionId,
        string paymentIntentId,
        Dictionary<string, string> metadata,
        Guid correlationId,
        CancellationToken ct = default);

    /// <summary>
    /// Handles checkout.session.expired for sponsor payments.
    /// </summary>
    Task HandleCheckoutExpiredAsync(
        string sessionId,
        Dictionary<string, string> metadata,
        Guid correlationId,
        CancellationToken ct = default);

    /// <summary>
    /// Phase 6A.136E: Handles charge.refunded for sponsor payments.
    /// Marks the sponsor as Refunded when Stripe processes a refund.
    /// </summary>
    Task HandleChargeRefundedAsync(
        string paymentIntentId,
        string refundId,
        Guid correlationId,
        CancellationToken ct = default);
}
