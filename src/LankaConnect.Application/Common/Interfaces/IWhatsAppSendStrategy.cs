using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Phase 7A: Strategy interface for sending WhatsApp messages via a provider (ACS).
/// Infrastructure layer implements this; application layer consumes it.
/// </summary>
public interface IWhatsAppSendStrategy
{
    /// <summary>Send a WhatsApp template message via the provider.</summary>
    Task<Result<string>> SendTemplateMessageAsync(
        string toPhoneNumber,
        string templateName,
        IReadOnlyList<string> parameterValues,
        string language = "en",
        CancellationToken ct = default);

    /// <summary>Send a plain text WhatsApp message (for 24-hour window replies).</summary>
    Task<Result<string>> SendTextMessageAsync(
        string toPhoneNumber,
        string text,
        CancellationToken ct = default);
}
