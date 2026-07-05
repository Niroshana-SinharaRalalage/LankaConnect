using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Modules.Communications.Domain.Enums;
namespace LankaConnect.Modules.Communications.Domain.Entities;

/// <summary>
/// Phase 7A: Persistence record for WhatsApp messages stored in the database.
/// Maps 1:1 with communications.whatsapp_messages table.
/// Uses private setters with domain methods per architect review C1.
/// </summary>
public class WhatsAppMessageRecord : LegacyBaseEntity
{
    public string FromPhoneNumber { get; private set; } = null!;
    public string ToPhoneNumber { get; private set; } = null!;
    public WhatsAppMessageType MessageType { get; private set; }
    public WhatsAppMessageStatus Status { get; private set; }
    public string? TemplateName { get; private set; }
    public string? TemplateParameters { get; private set; }
    public string MessageContent { get; private set; } = string.Empty;
    public string Language { get; private set; } = "en";

    // Cultural intelligence
    public string? CulturalContext { get; private set; }
    public double CulturalAppropriatenessScore { get; private set; }
    public bool RequiresCulturalValidation { get; private set; }

    // Geographic
    public string? DiasporaRegion { get; private set; }
    public string? TimeZone { get; private set; }

    // Delivery tracking
    public DateTime? SentAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public DateTime? FailedAt { get; private set; }
    public DateTime? ScheduledFor { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int RetryCount { get; private set; }
    public int MaxRetries { get; private set; } = 3;

    // Provider/Meta tracking
    public string? AcsMessageId { get; private set; }  // Stores provider message ID (ACS or Twilio SID). Column name kept for backward compat.
    public string? MetaMessageId { get; private set; }
    public WhatsAppProvider? Provider { get; private set; }

    // Foreign keys (nullable — audit-only references for registration_id/newsletter_id per C5)
    public Guid? UserId { get; private set; }
    public Guid? EventId { get; private set; }
    public Guid? RegistrationId { get; private set; }
    public Guid? NewsletterId { get; private set; }

    // Computed properties
    public bool IsRead => ReadAt.HasValue;
    public bool IsFailed => FailedAt.HasValue;
    public bool IsDelivered => DeliveredAt.HasValue;
    public bool CanRetry => Status == WhatsAppMessageStatus.Failed && RetryCount < MaxRetries;

    // EF Core constructor
    private WhatsAppMessageRecord() { }

    /// <summary>
    /// Factory method to create a new WhatsApp message record for persistence.
    /// </summary>
    public static WhatsAppMessageRecord Create(
        string fromPhoneNumber,
        string toPhoneNumber,
        WhatsAppMessageType messageType,
        string? templateName,
        Dictionary<string, string>? parameters,
        string language = "en",
        Guid? userId = null,
        Guid? eventId = null,
        Guid? registrationId = null,
        Guid? newsletterId = null)
    {
        var record = new WhatsAppMessageRecord
        {
            FromPhoneNumber = fromPhoneNumber,
            ToPhoneNumber = toPhoneNumber,
            MessageType = messageType,
            Status = WhatsAppMessageStatus.Draft,
            TemplateName = templateName,
            TemplateParameters = parameters != null
                ? System.Text.Json.JsonSerializer.Serialize(parameters)
                : null,
            Language = language,
            UserId = userId,
            EventId = eventId,
            RegistrationId = registrationId,
            NewsletterId = newsletterId
        };

        return record;
    }

    /// <summary>
    /// Mark message as successfully sent via provider (ACS or Twilio).
    /// </summary>
    public void MarkAsSent(string providerMessageId)
    {
        Status = WhatsAppMessageStatus.Sent;
        SentAt = DateTime.UtcNow;
        AcsMessageId = providerMessageId;  // DB column name kept for backward compat
    }

    /// <summary>
    /// Set which BSP provider sent this message.
    /// </summary>
    public void SetProvider(WhatsAppProvider provider)
    {
        Provider = provider;
    }

    /// <summary>
    /// Mark message as delivered to recipient's device.
    /// </summary>
    public void MarkAsDelivered()
    {
        Status = WhatsAppMessageStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Mark message as read by recipient.
    /// </summary>
    public void MarkAsRead()
    {
        Status = WhatsAppMessageStatus.Read;
        ReadAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Mark message as failed with error details.
    /// </summary>
    public void MarkAsFailed(string error)
    {
        Status = WhatsAppMessageStatus.Failed;
        FailedAt = DateTime.UtcNow;
        ErrorMessage = error;
    }

    /// <summary>
    /// Increment retry count for failed message retry.
    /// </summary>
    public void IncrementRetry()
    {
        RetryCount++;
        // Reset failure state for retry attempt
        Status = WhatsAppMessageStatus.Sending;
        FailedAt = null;
        ErrorMessage = null;
    }

    /// <summary>
    /// Set scheduling for cultural-optimal timing.
    /// </summary>
    public void ScheduleFor(DateTime scheduledTime)
    {
        Status = WhatsAppMessageStatus.Scheduled;
        ScheduledFor = scheduledTime;
    }

    /// <summary>
    /// Update Meta message ID from webhook callback.
    /// </summary>
    public void SetMetaMessageId(string metaMessageId)
    {
        MetaMessageId = metaMessageId;
    }

    /// <summary>
    /// Set message content for non-template messages.
    /// </summary>
    public void SetContent(string content)
    {
        MessageContent = content;
    }

    /// <summary>
    /// Set cultural context for cultural intelligence tracking.
    /// </summary>
    public void SetCulturalContext(string? culturalContextJson, double appropriatenessScore, bool requiresValidation)
    {
        CulturalContext = culturalContextJson;
        CulturalAppropriatenessScore = appropriatenessScore;
        RequiresCulturalValidation = requiresValidation;
    }

    /// <summary>
    /// Set geographic context for diaspora targeting.
    /// </summary>
    public void SetGeographicContext(string? diasporaRegion, string? timeZone)
    {
        DiasporaRegion = diasporaRegion;
        TimeZone = timeZone;
    }
}
