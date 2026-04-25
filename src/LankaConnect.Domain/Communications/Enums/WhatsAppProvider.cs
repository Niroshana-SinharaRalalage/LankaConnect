namespace LankaConnect.Domain.Communications.Enums;

/// <summary>
/// Phase 7B: WhatsApp Business Solution Provider (BSP) selection.
/// Determines which provider sends WhatsApp messages.
/// </summary>
public enum WhatsAppProvider
{
    /// <summary>Azure Communication Services WhatsApp channel.</summary>
    Acs = 1,

    /// <summary>Twilio WhatsApp Business API.</summary>
    Twilio = 2
}
