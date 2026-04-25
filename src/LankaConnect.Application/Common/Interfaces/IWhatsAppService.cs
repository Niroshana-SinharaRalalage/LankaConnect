using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.Enums;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Phase 7A: Application-level WhatsApp messaging service.
/// Handles feature flag checks, user preference validation, deduplication, and persistence.
/// </summary>
public interface IWhatsAppService
{
    /// <summary>Send a template message to a registered user (checks preferences).</summary>
    Task<Result<WhatsAppSendResult>> SendTemplateMessageAsync(
        Guid userId,
        string templateName,
        Dictionary<string, string> parameters,
        WhatsAppNotificationType notificationType,
        Guid? eventId = null,
        Guid? registrationId = null,
        CancellationToken ct = default);

    /// <summary>Send a template message directly to a phone number (anonymous/unregistered).</summary>
    Task<Result<WhatsAppSendResult>> SendTemplateMessageToPhoneAsync(
        string phoneNumber,
        string templateName,
        Dictionary<string, string> parameters,
        Guid? eventId = null,
        CancellationToken ct = default);

    /// <summary>Broadcast a template message to all opted-in attendees of an event.</summary>
    Task<Result<int>> BroadcastToEventAttendeesAsync(
        Guid eventId,
        string templateName,
        Dictionary<string, string> parameters,
        WhatsAppNotificationType notificationType,
        CancellationToken ct = default);
}

/// <summary>Result of a WhatsApp send operation.</summary>
public class WhatsAppSendResult
{
    public Guid MessageRecordId { get; init; }

    /// <summary>Provider-assigned message ID (ACS MessageId or Twilio MessageSid).</summary>
    public string? ProviderMessageId { get; init; }

    public bool WasSkipped { get; init; }
    public string? SkipReason { get; init; }

    /// <summary>
    /// Structured skip discriminator for observability / tests.
    /// <c>null</c> when the message was sent or the skip predates enum support.
    /// </summary>
    public WhatsAppSkipReason? SkipReasonCode { get; init; }

    /// <summary>Backward-compatible alias for ProviderMessageId.</summary>
    [Obsolete("Use ProviderMessageId instead. Will be removed in a future version.")]
    public string? AcsMessageId => ProviderMessageId;

    public static WhatsAppSendResult Sent(Guid recordId, string providerMessageId) =>
        new() { MessageRecordId = recordId, ProviderMessageId = providerMessageId };

    public static WhatsAppSendResult Skipped(string reason) =>
        new() { WasSkipped = true, SkipReason = reason };

    public static WhatsAppSendResult Skipped(WhatsAppSkipReason code, string reason) =>
        new() { WasSkipped = true, SkipReason = reason, SkipReasonCode = code };
}
