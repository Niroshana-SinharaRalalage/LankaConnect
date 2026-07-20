namespace LankaConnect.Products.LankaEvents.Contracts.Services; // Wave 8.5.d (2026-07-18): split from LegacyPromotions/ per Consult #17 Q2 Day 10 debt.

/// <summary>
/// Handles Stripe webhook events for registration payments.
/// Phase 0: Extracted from PaymentsController to support multiple payment types.
/// </summary>
public interface IRegistrationWebhookHandler
{
    /// <summary>
    /// Handles checkout.session.completed for registration payments.
    /// Verifies metadata, loads registration, completes payment, and saves.
    /// Also handles bundled donation completion in an isolated try-catch.
    /// </summary>
    /// <param name="sessionId">Stripe checkout session ID</param>
    /// <param name="paymentStatus">Stripe payment status (e.g., "paid")</param>
    /// <param name="paymentIntentId">Stripe PaymentIntent ID (or session ID as fallback)</param>
    /// <param name="metadata">Session metadata dictionary</param>
    /// <param name="correlationId">Correlation ID for end-to-end tracing</param>
    /// <param name="ct">Cancellation token</param>
    Task HandleCheckoutCompletedAsync(
        string sessionId,
        string paymentStatus,
        string paymentIntentId,
        Dictionary<string, string> metadata,
        Guid correlationId,
        CancellationToken ct = default);

    /// <summary>
    /// Handles checkout.session.expired for registration payments.
    /// Marks the registration as Abandoned (Preliminary -> Abandoned).
    /// Also handles bundled donation abandonment in an isolated try-catch.
    /// </summary>
    /// <param name="sessionId">Stripe checkout session ID</param>
    /// <param name="metadata">Session metadata dictionary</param>
    /// <param name="correlationId">Correlation ID for end-to-end tracing</param>
    /// <param name="ct">Cancellation token</param>
    Task HandleCheckoutExpiredAsync(
        string sessionId,
        Dictionary<string, string> metadata,
        Guid correlationId,
        CancellationToken ct = default);

    /// <summary>
    /// Handles charge.refunded for registration payments.
    /// Finds the registration via metadata or PaymentIntentId, then completes the refund.
    /// </summary>
    /// <param name="chargeId">Stripe Charge ID</param>
    /// <param name="paymentIntentId">PaymentIntent ID from the charge (nullable)</param>
    /// <param name="refundId">Latest refund ID</param>
    /// <param name="amountRefunded">Amount refunded in cents</param>
    /// <param name="refundMetadata">Metadata from the refund object (nullable)</param>
    /// <param name="correlationId">Correlation ID for end-to-end tracing</param>
    /// <param name="ct">Cancellation token</param>
    Task HandleChargeRefundedAsync(
        string chargeId,
        string? paymentIntentId,
        string refundId,
        long amountRefunded,
        Dictionary<string, string>? refundMetadata,
        Guid correlationId,
        CancellationToken ct = default);
}
