namespace LankaConnect.Application.Common.Options;

/// <summary>
/// Phase 7A.2: Application-layer configuration for WhatsApp feature flags.
/// Mirrors WhatsAppSettings from Infrastructure but only exposes what the Application layer needs.
/// Bound from appsettings.json "WhatsAppSettings" section.
/// </summary>
public class WhatsAppOptions
{
    public const string SectionName = "WhatsAppSettings";

    /// <summary>
    /// Feature flag to enable/disable WhatsApp messaging globally.
    /// </summary>
    public bool Enabled { get; set; }
}
