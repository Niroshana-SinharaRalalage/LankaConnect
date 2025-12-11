using LankaConnect.Domain.Common;

namespace LankaConnect.Infrastructure.Payments.Entities;

/// <summary>
/// Tracks processed Stripe webhook events for idempotency
/// </summary>
public class StripeWebhookEvent : BaseEntity
{
    /// <summary>
    /// Stripe event ID (evt_xxx)
    /// </summary>
    public string EventId { get; private set; } = string.Empty;

    /// <summary>
    /// Event type (e.g., customer.subscription.created)
    /// </summary>
    public string EventType { get; private set; } = string.Empty;

    /// <summary>
    /// Whether the event has been processed
    /// </summary>
    public bool Processed { get; private set; }

    /// <summary>
    /// When the event was processed
    /// </summary>
    public DateTime? ProcessedAt { get; private set; }

    /// <summary>
    /// Error message if processing failed
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Number of processing attempts
    /// </summary>
    public int AttemptCount { get; private set; }

    // EF Core constructor
    private StripeWebhookEvent()
    {
    }

    private StripeWebhookEvent(string eventId, string eventType)
    {
        EventId = eventId;
        EventType = eventType;
        Processed = false;
        AttemptCount = 0;
    }

    public static StripeWebhookEvent Create(string eventId, string eventType)
    {
        return new StripeWebhookEvent(eventId, eventType);
    }

    public void MarkAsProcessed()
    {
        Processed = true;
        ProcessedAt = DateTime.UtcNow;
    }

    public void RecordAttempt(string? errorMessage = null)
    {
        AttemptCount++;
        ErrorMessage = errorMessage;
        MarkAsUpdated();
    }
}
