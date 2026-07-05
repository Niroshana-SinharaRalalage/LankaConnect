using LankaConnect.Domain.Common;
namespace LankaConnect.BuildingBlocks.Application.Common.Interfaces;

/// <summary>
/// Phase 7A: Strategy interface for sending WhatsApp messages via a provider (ACS).
/// Infrastructure layer implements this; application layer consumes it.
/// </summary>
public interface IWhatsAppSendStrategy
{
    /// <summary>
    /// Send a WhatsApp template message via the provider.
    /// </summary>
    /// <param name="providerTemplateId">
    /// Optional provider-specific template identifier. For Twilio this is the Content API
    /// <c>ContentSid</c> (HX-prefixed). ACS does not require it and ignores the value.
    /// Phase 7B.4: added to wire Twilio Content API without mutating MetaTemplateId semantics.
    /// </param>
    Task<Result<string>> SendTemplateMessageAsync(
        string toPhoneNumber,
        string templateName,
        IReadOnlyList<string> parameterValues,
        string language = "en",
        string? providerTemplateId = null,
        CancellationToken ct = default);

    /// <summary>Send a plain text WhatsApp message (for 24-hour window replies).</summary>
    Task<Result<string>> SendTextMessageAsync(
        string toPhoneNumber,
        string text,
        CancellationToken ct = default);
}
