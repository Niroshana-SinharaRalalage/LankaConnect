using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Communications.DomainEvents;

/// <summary>
/// Phase 7A: Domain event raised when a WhatsApp message is successfully sent.
/// </summary>
public sealed record WhatsAppMessageSentEvent(
    Guid MessageId,
    string ToPhoneNumber,
    string TemplateName,
    Guid? UserId,
    Guid? EventId) : DomainEvent;
