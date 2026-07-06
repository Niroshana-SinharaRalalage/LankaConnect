using System.Security.Cryptography;
using System.Text.RegularExpressions;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Modules.Communications.Domain.DomainEvents;
using LankaConnect.Modules.Communications.Domain.Enums;
using System.Diagnostics.CodeAnalysis;
namespace LankaConnect.Modules.Communications.Domain.Entities;

/// <summary>
/// Phase 7A: User opt-in preferences for WhatsApp notifications.
/// Supports per-notification-type toggles, phone verification, and cultural timing.
/// Uses WhatsAppNotificationType enum per architect review D2.
/// Uses cryptographic random for verification codes per architect review D4.
/// Includes lockout reset mechanism per architect review D5.
/// </summary>
public class UserWhatsAppPreferences : LegacyBaseEntity
{
    private static readonly Regex E164Regex = new(@"^\+[1-9]\d{1,14}$", RegexOptions.Compiled);

    public Guid UserId { get; private set; }
    public bool WhatsAppEnabled { get; private set; }
    public string? WhatsAppPhoneNumber { get; private set; }
    public bool PhoneVerified { get; private set; }
    public DateTime? PhoneVerifiedAt { get; private set; }
    public string? VerificationCode { get; private set; }
    public DateTime? VerificationCodeExpires { get; private set; }
    public int VerificationAttempts { get; private set; }
    public DateTime? VerificationLockedUntil { get; private set; }

    // Phase 7D Fix 4: grace-clock + audit columns for auto-disable-on-unverified.
    // WhatsAppEnabledAt is the *dedicated* enable timestamp — we cannot reuse UpdatedAt
    // because any preference edit (quiet hours, per-type toggles) touches that field and
    // would silently reset the grace period.
    public DateTime? WhatsAppEnabledAt { get; private set; }
    public DateTime? WhatsAppAutoDisabledAt { get; private set; }
    public WhatsAppAutoDisableReason? WhatsAppAutoDisableReason { get; private set; }

    // Per-notification preferences (all default to true when WhatsApp is enabled)
    public bool NotifyEventRegistration { get; private set; } = true;
    public bool NotifyEventReminder { get; private set; } = true;
    public bool NotifyEventCancellation { get; private set; } = true;
    public bool NotifyEventUpdate { get; private set; } = true;
    public bool NotifySignupCommitment { get; private set; } = true;
    public bool NotifyRefund { get; private set; } = true;
    public bool NotifyNewsletter { get; private set; } = true;
    public bool NotifyNewEvent { get; private set; } = true;
    public bool NotifyPayment { get; private set; } = true;

    // Cultural preferences
    public string PreferredLanguage { get; private set; } = "en";
    public TimeOnly? QuietHoursStart { get; private set; }
    public TimeOnly? QuietHoursEnd { get; private set; }
    public bool RespectCulturalTiming { get; private set; } = true;

    // Computed
    public bool IsFullyVerified => WhatsAppEnabled && PhoneVerified;
    public bool IsLocked => VerificationLockedUntil.HasValue && DateTime.UtcNow < VerificationLockedUntil;

    // EF Core constructor
    [SetsRequiredMembers]
    private UserWhatsAppPreferences() { }

    /// <summary>
    /// Factory method to create default WhatsApp preferences for a user.
    /// </summary>
    public static UserWhatsAppPreferences Create(Guid userId)
    {
        return new UserWhatsAppPreferences
        {
            UserId = userId,
            WhatsAppEnabled = false,
            PhoneVerified = false,
            VerificationAttempts = 0
        };
    }

    /// <summary>
    /// Enable WhatsApp notifications with a phone number.
    /// Phone number must be in E.164 format (e.g., +14155551234).
    /// </summary>
    public Result EnableWhatsApp(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return Result.Failure("WhatsApp phone number is required.");

        if (!E164Regex.IsMatch(phoneNumber))
            return Result.Failure("Phone number must be in E.164 format (e.g., +14155551234).");

        WhatsAppPhoneNumber = phoneNumber;
        WhatsAppEnabled = true;
        // Fix 4: restart the grace clock on every enable (new phone = new 30-day window).
        WhatsAppEnabledAt = DateTime.UtcNow;
        // Re-enabling clears any prior auto-disable audit so the row reads cleanly.
        WhatsAppAutoDisabledAt = null;
        WhatsAppAutoDisableReason = null;
        // Reset verification when phone number changes
        PhoneVerified = false;
        PhoneVerifiedAt = null;
        VerificationCode = null;
        VerificationCodeExpires = null;
        VerificationAttempts = 0;
        VerificationLockedUntil = null;

        return Result.Success();
    }

    /// <summary>
    /// Disable WhatsApp notifications entirely.
    /// </summary>
    public Result DisableWhatsApp()
    {
        WhatsAppEnabled = false;
        WhatsAppEnabledAt = null;
        return Result.Success();
    }

    /// <summary>
    /// Phase 7D Fix 4: background-job-driven auto-disable after the verification grace
    /// period elapses. Guards protect against:
    /// - disabling a user who has actually verified (invariant: never auto-disable a working channel)
    /// - disabling a row that is already disabled (idempotency: a second job run must not re-fire the email)
    /// Raises <see cref="WhatsAppAutoDisabledDomainEvent"/> so the application layer can send
    /// the "we turned WhatsApp off" notification email via the standard domain-event pipeline.
    /// </summary>
    public Result AutoDisableUnverified(WhatsAppAutoDisableReason reason)
    {
        if (PhoneVerified)
            return Result.Failure("Cannot auto-disable a verified WhatsApp preference.");

        if (!WhatsAppEnabled)
            return Result.Failure("WhatsApp is already disabled.");

        var phoneForEvent = WhatsAppPhoneNumber ?? string.Empty;

        WhatsAppEnabled = false;
        WhatsAppEnabledAt = null;
        WhatsAppAutoDisabledAt = DateTime.UtcNow;
        WhatsAppAutoDisableReason = reason;

        RaiseDomainEvent(new WhatsAppAutoDisabledDomainEvent(UserId, phoneForEvent, reason));

        return Result.Success();
    }

    /// <summary>
    /// Generate a new 6-digit verification code.
    /// Uses cryptographic random per architect review D4.
    /// Includes lockout mechanism per architect review D5.
    /// </summary>
    public Result GenerateVerificationCode()
    {
        if (!WhatsAppEnabled)
            return Result.Failure("WhatsApp must be enabled before requesting verification.");

        if (string.IsNullOrWhiteSpace(WhatsAppPhoneNumber))
            return Result.Failure("Phone number must be set before requesting verification.");

        // Check lockout (auto-resets after 1 hour per D5)
        if (IsLocked)
        {
            var minutesLeft = (int)Math.Ceiling((VerificationLockedUntil!.Value - DateTime.UtcNow).TotalMinutes);
            return Result.Failure($"Too many verification attempts. Please try again in {minutesLeft} minute(s).");
        }

        // Reset lockout if period has expired
        if (VerificationLockedUntil.HasValue && DateTime.UtcNow >= VerificationLockedUntil)
        {
            VerificationAttempts = 0;
            VerificationLockedUntil = null;
        }

        if (VerificationAttempts >= 5)
        {
            VerificationLockedUntil = DateTime.UtcNow.AddHours(1);
            return Result.Failure("Maximum verification attempts reached. Please try again in 1 hour.");
        }

        // Generate cryptographically secure 6-digit code (D4)
        VerificationCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        VerificationCodeExpires = DateTime.UtcNow.AddMinutes(10);
        VerificationAttempts++;

        return Result.Success();
    }

    /// <summary>
    /// Verify the phone number with the provided code.
    /// </summary>
    public Result VerifyPhone(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Result.Failure("Verification code is required.");

        if (VerificationCode == null || VerificationCodeExpires == null)
            return Result.Failure("No verification code found. Please request a new one.");

        if (DateTime.UtcNow > VerificationCodeExpires)
            return Result.Failure("Verification code has expired. Please request a new one.");

        if (VerificationCode != code)
            return Result.Failure("Invalid verification code.");

        PhoneVerified = true;
        PhoneVerifiedAt = DateTime.UtcNow;
        VerificationCode = null;
        VerificationCodeExpires = null;
        VerificationAttempts = 0;
        VerificationLockedUntil = null;

        return Result.Success();
    }

    /// <summary>
    /// Check if user should receive a WhatsApp notification for the given type.
    /// Thin facade over <see cref="EvaluateSkipReason"/> — kept so legacy callers
    /// that only need a boolean do not have to change.
    /// </summary>
    public bool ShouldNotify(WhatsAppNotificationType notificationType)
        => EvaluateSkipReason(notificationType) is null;

    /// <summary>
    /// Returns the single discriminator explaining why a WhatsApp send for this
    /// notification type must be skipped, or <c>null</c> if the send may proceed.
    /// Order matters: we report the root cause, not a downstream symptom
    /// (e.g. a user who turned WhatsApp on but never verified yields
    /// <see cref="WhatsAppSkipReason.PhoneUnverified"/>, not
    /// <see cref="WhatsAppSkipReason.TypeDisabled"/>).
    /// </summary>
    public WhatsAppSkipReason? EvaluateSkipReason(WhatsAppNotificationType notificationType)
    {
        if (!WhatsAppEnabled)
            return WhatsAppSkipReason.WhatsAppDisabled;

        if (!PhoneVerified)
            return WhatsAppSkipReason.PhoneUnverified;

        var typeEnabled = notificationType switch
        {
            WhatsAppNotificationType.EventRegistration => NotifyEventRegistration,
            WhatsAppNotificationType.EventReminder => NotifyEventReminder,
            WhatsAppNotificationType.EventCancellation => NotifyEventCancellation,
            WhatsAppNotificationType.EventUpdate => NotifyEventUpdate,
            WhatsAppNotificationType.SignupCommitment => NotifySignupCommitment,
            WhatsAppNotificationType.Refund => NotifyRefund,
            WhatsAppNotificationType.Newsletter => NotifyNewsletter,
            WhatsAppNotificationType.NewEvent => NotifyNewEvent,
            WhatsAppNotificationType.Payment => NotifyPayment,
            _ => false // Phase 7B.3 additions not yet mapped to dedicated toggles
        };

        return typeEnabled ? null : WhatsAppSkipReason.TypeDisabled;
    }

    /// <summary>
    /// Update per-notification-type preferences.
    /// </summary>
    public Result UpdateNotificationPreferences(
        bool notifyRegistration,
        bool notifyReminder,
        bool notifyCancellation,
        bool notifyUpdate,
        bool notifySignup,
        bool notifyRefund,
        bool notifyNewsletter,
        bool notifyNewEvent,
        bool notifyPayment)
    {
        NotifyEventRegistration = notifyRegistration;
        NotifyEventReminder = notifyReminder;
        NotifyEventCancellation = notifyCancellation;
        NotifyEventUpdate = notifyUpdate;
        NotifySignupCommitment = notifySignup;
        NotifyRefund = notifyRefund;
        NotifyNewsletter = notifyNewsletter;
        NotifyNewEvent = notifyNewEvent;
        NotifyPayment = notifyPayment;

        return Result.Success();
    }

    /// <summary>
    /// Update cultural and language preferences.
    /// </summary>
    public Result UpdateCulturalPreferences(
        string preferredLanguage,
        TimeOnly? quietHoursStart,
        TimeOnly? quietHoursEnd,
        bool respectCulturalTiming)
    {
        if (string.IsNullOrWhiteSpace(preferredLanguage))
            return Result.Failure("Preferred language is required.");

        PreferredLanguage = preferredLanguage;
        QuietHoursStart = quietHoursStart;
        QuietHoursEnd = quietHoursEnd;
        RespectCulturalTiming = respectCulturalTiming;

        return Result.Success();
    }
}
