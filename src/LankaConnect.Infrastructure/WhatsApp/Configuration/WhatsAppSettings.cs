namespace LankaConnect.Infrastructure.WhatsApp.Configuration;

/// <summary>
/// Phase 7A: Configuration settings for WhatsApp integration via Azure Communication Services.
/// Bound from appsettings.json "WhatsAppSettings" section.
/// </summary>
public class WhatsAppSettings
{
    public const string SectionName = "WhatsAppSettings";

    /// <summary>
    /// ACS WhatsApp channel registration ID from Azure portal.
    /// </summary>
    public string ChannelRegistrationId { get; set; } = string.Empty;

    /// <summary>
    /// Sender phone number registered with Meta Business Manager.
    /// </summary>
    public string SenderPhoneNumber { get; set; } = string.Empty;

    /// <summary>
    /// Meta Business Account ID for template management.
    /// </summary>
    public string BusinessAccountId { get; set; } = string.Empty;

    /// <summary>
    /// Feature flag to enable/disable WhatsApp messaging globally.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Rate limit: maximum messages per second to ACS.
    /// </summary>
    public int MaxMessagesPerSecond { get; set; } = 10;

    /// <summary>
    /// Maximum retry attempts for failed messages.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Base delay in seconds for exponential backoff retry (2s, 4s, 8s).
    /// </summary>
    public int RetryDelayBaseSeconds { get; set; } = 2;

    /// <summary>
    /// Secret for verifying ACS webhook callback authenticity.
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;
}
