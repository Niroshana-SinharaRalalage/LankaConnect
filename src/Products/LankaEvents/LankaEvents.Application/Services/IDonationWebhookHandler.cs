namespace LankaConnect.Products.LankaEvents.Application.Services;

/// <summary>
/// Handles Stripe webhook events for standalone donation payments.
/// Phase 0: Extracted from PaymentsController to support multiple payment types.
/// </summary>
public interface IDonationWebhookHandler
{
    /// <summary>
    /// Handles checkout.session.completed for standalone donation payments.
    /// Loads the donation, verifies pending status, completes payment, and saves.
    /// Errors are swallowed to prevent HTTP 500 to Stripe.
    /// </summary>
    /// <param name="sessionId">Stripe checkout session ID</param>
    /// <param name="paymentIntentId">Stripe PaymentIntent ID (or session ID as fallback)</param>
    /// <param name="metadata">Session metadata dictionary</param>
    /// <param name="correlationId">Correlation ID for end-to-end tracing</param>
    /// <param name="ct">Cancellation token</param>
    Task HandleCheckoutCompletedAsync(
        string sessionId,
        string paymentIntentId,
        Dictionary<string, string> metadata,
        Guid correlationId,
        CancellationToken ct = default);

    /// <summary>
    /// Handles checkout.session.expired for standalone donation payments.
    /// Marks the donation as Abandoned.
    /// Errors are swallowed to prevent HTTP 500 to Stripe.
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
    /// Phase 6A.136E: Handles charge.refunded for donation payments.
    /// Marks the donation as Refunded when Stripe processes a refund.
    /// </summary>
    Task HandleChargeRefundedAsync(
        string paymentIntentId,
        string refundId,
        Guid correlationId,
        CancellationToken ct = default);
}
