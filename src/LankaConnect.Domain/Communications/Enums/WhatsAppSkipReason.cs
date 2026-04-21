namespace LankaConnect.Domain.Communications.Enums;

/// <summary>
/// Discriminated skip reasons for WhatsApp sends.
/// Replaces the single misleading "User opted out" log that previously covered
/// every negative outcome of <see cref="Entities.UserWhatsAppPreferences.ShouldNotify"/>.
/// </summary>
public enum WhatsAppSkipReason
{
    /// <summary>Global feature flag is off (WhatsAppSettings.Enabled = false).</summary>
    GloballyDisabled = 1,

    /// <summary>No <c>UserWhatsAppPreferences</c> row exists for the user.</summary>
    NoPreferences = 2,

    /// <summary>User has never turned WhatsApp on for their account.</summary>
    WhatsAppDisabled = 3,

    /// <summary>User turned WhatsApp on but has not completed phone verification.</summary>
    PhoneUnverified = 4,

    /// <summary>User disabled this specific notification type.</summary>
    TypeDisabled = 5,

    /// <summary>User has no phone number recorded (should not happen after EnableWhatsApp).</summary>
    MissingPhoneNumber = 6,

    /// <summary>An identical message was already sent within the dedup window.</summary>
    Deduplicated = 7
}
