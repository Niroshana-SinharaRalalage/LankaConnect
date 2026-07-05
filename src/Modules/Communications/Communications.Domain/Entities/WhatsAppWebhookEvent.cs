using LankaConnect.Domain.Common;
namespace LankaConnect.Modules.Communications.Domain.Entities;

/// <summary>
/// Phase 7A: Raw webhook payload from ACS/Meta for debugging and idempotency.
/// Stores delivery status callbacks for WhatsApp messages.
/// </summary>
public class WhatsAppWebhookEvent : LegacyBaseEntity
{
    public string EventType { get; private set; } = null!;
    public string Payload { get; private set; } = null!;
    public string? AcsMessageId { get; private set; }
    public string? MetaMessageId { get; private set; }
    public bool Processed { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    // EF Core constructor
    private WhatsAppWebhookEvent() { }

    /// <summary>
    /// Factory method to create a new webhook event record.
    /// </summary>
    public static WhatsAppWebhookEvent Create(
        string eventType,
        string payloadJson,
        string? acsMessageId = null,
        string? metaMessageId = null)
    {
        return new WhatsAppWebhookEvent
        {
            EventType = eventType,
            Payload = payloadJson,
            AcsMessageId = acsMessageId,
            MetaMessageId = metaMessageId,
            Processed = false
        };
    }

    /// <summary>
    /// Mark webhook event as successfully processed.
    /// </summary>
    public void MarkProcessed()
    {
        Processed = true;
        ProcessedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Mark webhook event as failed to process.
    /// </summary>
    public void MarkFailed(string error)
    {
        Processed = false;
        ErrorMessage = error;
    }
}
