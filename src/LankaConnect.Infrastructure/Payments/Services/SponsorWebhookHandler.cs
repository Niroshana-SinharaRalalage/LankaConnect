using LankaConnect.Application.Events.Services;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Infrastructure.Payments.Services;

/// <summary>
/// Phase 0: Stub implementation for sponsor webhook handling.
/// Will be replaced with real implementation in Phase 3D.
/// </summary>
public class SponsorWebhookHandler : ISponsorWebhookHandler
{
    private readonly ILogger<SponsorWebhookHandler> _logger;

    public SponsorWebhookHandler(ILogger<SponsorWebhookHandler> logger)
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
            "[Sponsor] [Webhook-Stub] checkout.session.completed received but handler not yet implemented (Phase 3D) - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
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
            "[Sponsor] [Webhook-Stub] checkout.session.expired received but handler not yet implemented (Phase 3D) - CorrelationId: {CorrelationId}, SessionId: {SessionId}",
            correlationId, sessionId);
        return Task.CompletedTask;
    }
}
