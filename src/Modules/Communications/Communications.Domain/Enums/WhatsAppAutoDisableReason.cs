namespace LankaConnect.Modules.Communications.Domain.Enums;

/// <summary>
/// Phase 7D Fix 4: classifies why <see cref="Entities.UserWhatsAppPreferences.AutoDisableUnverified"/>
/// flipped WhatsApp off on a user's behalf. Today only the grace-period expiry path exists,
/// but the enum leaves room for future administrative / compliance-driven auto-disables
/// without forcing a schema change.
/// </summary>
public enum WhatsAppAutoDisableReason
{
    /// <summary>The 30-day verification grace period elapsed without the user verifying their phone.</summary>
    UnverifiedGraceExpired = 1
}
