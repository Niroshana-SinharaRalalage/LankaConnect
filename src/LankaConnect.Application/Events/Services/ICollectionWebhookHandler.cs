namespace LankaConnect.Application.Events.Services;

/// <summary>
/// Handles Stripe webhook events for collection payments.
/// Phase 0: Placeholder interface — implementation will be added in Phase 3D.
/// </summary>
public interface ICollectionWebhookHandler
{
    /// <summary>
    /// Handles checkout.session.completed for collection payments.
    /// </summary>
    Task HandleCheckoutCompletedAsync(
        string sessionId,
        string paymentIntentId,
        Dictionary<string, string> metadata,
        Guid correlationId,
        CancellationToken ct = default);

    /// <summary>
    /// Handles checkout.session.expired for collection payments.
    /// </summary>
    Task HandleCheckoutExpiredAsync(
        string sessionId,
        Dictionary<string, string> metadata,
        Guid correlationId,
        CancellationToken ct = default);
}
