namespace LankaConnect.Domain.Payments;

/// <summary>
/// Repository interface for Stripe webhook event tracking (idempotency)
/// Phase 6A.4: Stripe Payment Integration - Webhook Idempotency
/// </summary>
public interface IStripeWebhookEventRepository
{
    /// <summary>
    /// Check if event has already been processed (idempotency)
    /// </summary>
    Task<bool> IsEventProcessedAsync(string eventId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Record a new webhook event
    /// </summary>
    Task<Guid> RecordEventAsync(
        string eventId,
        string eventType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Mark event as successfully processed
    /// </summary>
    Task MarkEventAsProcessedAsync(
        string eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Record processing attempt with optional error
    /// </summary>
    Task RecordAttemptAsync(
        string eventId,
        string? errorMessage = null,
        CancellationToken cancellationToken = default);
}
