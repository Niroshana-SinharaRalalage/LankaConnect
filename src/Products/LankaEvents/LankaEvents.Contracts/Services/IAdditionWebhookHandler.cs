namespace LankaConnect.Products.LankaEvents.Contracts.Services; // Wave 8.5.d (2026-07-18): split from LegacyPromotions/ per Consult #17 Q2 Day 10 debt.

/// <summary>
/// Handles Stripe webhook events for add-only attendee (addition) payments.
/// Phase 0: Extracted from PaymentsController to support multiple payment types.
/// </summary>
public interface IAdditionWebhookHandler
{
    /// <summary>
    /// Handles checkout.session.completed for addition payments.
    /// Completes the RegistrationAddition, merges attendees into registration,
    /// creates a RegistrationPayment record, and recalculates revenue breakdown.
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
    /// Phase 6A.136 Issue #2: Handles checkout.session.expired for addition payments.
    /// Marks the RegistrationAddition as Abandoned to prevent leaking Pending entities.
    /// </summary>
    Task HandleCheckoutExpiredAsync(
        string sessionId,
        Dictionary<string, string> metadata,
        Guid correlationId,
        CancellationToken ct = default);
}
