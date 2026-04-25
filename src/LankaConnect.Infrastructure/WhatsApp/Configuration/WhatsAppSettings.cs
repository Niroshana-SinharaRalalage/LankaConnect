using LankaConnect.Domain.Communications.Enums;

namespace LankaConnect.Infrastructure.WhatsApp.Configuration;

/// <summary>
/// Phase 7A/7B: Configuration settings for WhatsApp integration.
/// Supports both Azure Communication Services (ACS) and Twilio as BSP.
/// Bound from appsettings.json "WhatsAppSettings" section.
/// </summary>
public class WhatsAppSettings
{
    public const string SectionName = "WhatsAppSettings";

    // --- Provider selection ---

    /// <summary>
    /// Which BSP to use: Acs or Twilio.
    /// Change this to switch providers. Default: Acs (backward compatible).
    /// </summary>
    public WhatsAppProvider Provider { get; set; } = WhatsAppProvider.Acs;

    // --- Common settings (used by all providers) ---

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
    /// Rate limit: maximum messages per second to the provider.
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

    // --- ACS-specific settings ---

    /// <summary>
    /// Azure Communication Services connection string.
    /// Shared with EmailSettings.AzureConnectionString — same ACS resource.
    /// If empty, falls back to EmailSettings:AzureConnectionString via DI.
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// ACS WhatsApp channel registration ID from Azure portal.
    /// </summary>
    public string ChannelRegistrationId { get; set; } = string.Empty;

    /// <summary>
    /// Secret for verifying ACS webhook callback authenticity.
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;

    // --- Twilio-specific settings (Phase 7B) ---

    /// <summary>
    /// Twilio Account SID (starts with "AC").
    /// </summary>
    public string TwilioAccountSid { get; set; } = string.Empty;

    /// <summary>
    /// Twilio Auth Token for API authentication and webhook signature validation.
    /// </summary>
    public string TwilioAuthToken { get; set; } = string.Empty;

    /// <summary>
    /// Twilio WhatsApp sender number in E.164 format (e.g., "+14155551234").
    /// The strategy prepends "whatsapp:" internally.
    /// </summary>
    public string TwilioWhatsAppNumber { get; set; } = string.Empty;

    /// <summary>
    /// Twilio Verify Service SID for phone verification (e.g., "VAxxxxxxxxx").
    /// </summary>
    public string TwilioVerifyServiceSid { get; set; } = string.Empty;

    /// <summary>
    /// Phase 7B.4: Dev-only flag. When true, the Twilio strategy will fall back to a
    /// plain-text debug body for template sends that have no <c>TwilioContentSid</c>.
    /// When false (default, and required in production), template sends without a
    /// ContentSid are rejected before the Twilio SDK is invoked.
    /// MUST remain false in staging/production.
    /// </summary>
    public bool SandboxMode { get; set; } = false;

    /// <summary>
    /// Phase 7B.4: Per-environment mapping of WhatsApp template name → Twilio Content API SID.
    /// Read by <c>TwilioTemplateSeeder</c> at startup to upsert/approve each template row in
    /// <c>communications.whatsapp_templates</c>. Keys must match <see cref="WhatsAppTemplateContract.TemplateNames"/>.
    /// Values are Twilio Content SIDs starting with "HX" followed by 32 hex chars.
    /// Staging and production use different Twilio accounts and therefore different SIDs —
    /// values come from Azure App Settings env vars (e.g. <c>WhatsAppSettings__TwilioContentSids__event_registration_confirmed</c>).
    /// An empty dictionary means the seeder is a no-op.
    /// </summary>
    public Dictionary<string, string> TwilioContentSids { get; set; } = new();
}
