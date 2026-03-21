namespace LankaConnect.Application.Events.Services;

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
}
