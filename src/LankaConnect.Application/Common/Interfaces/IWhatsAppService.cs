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
    public string? AcsMessageId { get; init; }
    public bool WasSkipped { get; init; }
    public string? SkipReason { get; init; }

    public static WhatsAppSendResult Sent(Guid recordId, string acsMessageId) =>
        new() { MessageRecordId = recordId, AcsMessageId = acsMessageId };

    public static WhatsAppSendResult Skipped(string reason) =>
        new() { WasSkipped = true, SkipReason = reason };
}
