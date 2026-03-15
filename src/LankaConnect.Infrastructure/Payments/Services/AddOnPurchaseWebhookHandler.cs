using LankaConnect.Application.Events.Services;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Infrastructure.Payments.Services;

/// <summary>
/// Phase 0: Stub implementation for add-on purchase webhook handling.
/// Will be replaced with real implementation in Phase 3D.
/// </summary>
public class AddOnPurchaseWebhookHandler : IAddOnPurchaseWebhookHandler
{
    private readonly ILogger<AddOnPurchaseWebhookHandler> _logger;

    public AddOnPurchaseWebhookHandler(ILogger<AddOnPurchaseWebhookHandler> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task HandleCheckoutCompletedAsync(
        string sessionId,
        string paymentIntentId,
        Dictionary<string, string> metadata,
        Guid correlationId,
        CancellationToken ct = default)
    {
        _logger.LogWarning(
            "[AddOnPurchase] [Webhook-Stub] checkout.session.completed received but handler not yet implemented (Phase 3D) - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
            correlationId, sessionId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task HandleCheckoutExpiredAsync(
        string sessionId,
        Dictionary<string, string> metadata,
        Guid correlationId,
        CancellationToken ct = default)
    {
        _logger.LogWarning(
            "[AddOnPurchase] [Webhook-Stub] checkout.session.expired received but handler not yet implemented (Phase 3D) - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
            correlationId, sessionId);
        return Task.CompletedTask;
    }
}
