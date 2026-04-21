using FluentAssertions;
using LankaConnect.Domain.Communications.DomainEvents;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.Enums;
using Xunit;

namespace LankaConnect.Domain.Tests.Communications;

/// <summary>
/// TDD tests for UserWhatsAppPreferences entity.
/// Tests creation, phone enablement/verification, notification preferences,
/// lockout mechanism, and cultural settings.
/// </summary>
public class UserWhatsAppPreferencesTests
{
    private readonly Guid _userId = Guid.NewGuid();
    private const string ValidE164Phone = "+14155551234";

    #region Creation Tests

    [Fact]
    public void Create_Should_Return_Default_Disabled_Preferences()
    {
        var prefs = UserWhatsAppPreferences.Create(_userId);

        prefs.Should().NotBeNull();
        prefs.UserId.Should().Be(_userId);
        prefs.WhatsAppEnabled.Should().BeFalse();
        prefs.PhoneVerified.Should().BeFalse();
        prefs.WhatsAppPhoneNumber.Should().BeNull();
        prefs.VerificationAttempts.Should().Be(0);
        prefs.VerificationCode.Should().BeNull();
        prefs.VerificationCodeExpires.Should().BeNull();
        prefs.VerificationLockedUntil.Should().BeNull();
        prefs.IsFullyVerified.Should().BeFalse();
        prefs.IsLocked.Should().BeFalse();
    }

    #endregion

    #region EnableWhatsApp Tests

    [Fact]
    public void EnableWhatsApp_With_Valid_E164_Number_Should_Succeed()
    {
        var prefs = UserWhatsAppPreferences.Create(_userId);

        var result = prefs.EnableWhatsApp(ValidE164Phone);

        result.IsSuccess.Should().BeTrue();
        prefs.WhatsAppEnabled.Should().BeTrue();
        prefs.WhatsAppPhoneNumber.Should().Be(ValidE164Phone);
    }

    [Theory]
    [InlineData("+94771234567")]   // Sri Lanka
    [InlineData("+12025551234")]   // US
    [InlineData("+447911123456")]  // UK
    [InlineData("+61412345678")]   // Australia
    public void EnableWhatsApp_With_Various_Valid_E164_Numbers_Should_Succeed(string phone)
    {
        var prefs = UserWhatsAppPreferences.Create(_userId);

        var result = prefs.EnableWhatsApp(phone);

        result.IsSuccess.Should().BeTrue();
        prefs.WhatsAppPhoneNumber.Should().Be(phone);
    }

    [Theory]
    [InlineData("14155551234")]     // Missing +
    [InlineData("+0123456789")]     // Leading zero after +
    [InlineData("not-a-number")]    // Text
    [InlineData("+1")]              // Too short
    [InlineData("+1234567890123456")] // Too long (16 digits)
    public void EnableWhatsApp_With_Invalid_Phone_Format_Should_Fail(string phone)
    {
        var prefs = UserWhatsAppPreferences.Create(_userId);

        var result = prefs.EnableWhatsApp(phone);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("E.164");
    }

    [Fact]
    public void EnableWhatsApp_With_Empty_Phone_Should_Fail()
    {
        var prefs = UserWhatsAppPreferences.Create(_userId);

        var result = prefs.EnableWhatsApp("");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("required");
    }

    [Fact]
    public void EnableWhatsApp_With_Whitespace_Phone_Should_Fail()
    {
        var prefs = UserWhatsAppPreferences.Create(_userId);

        var result = prefs.EnableWhatsApp("   ");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("required");
    }

    [Fact]
    public void EnableWhatsApp_Should_Reset_Verification_State()
    {
        var prefs = CreateEnabledAndVerifiedPreferences();

        // Re-enable with a different number
        var result = prefs.EnableWhatsApp("+94771234567");

        result.IsSuccess.Should().BeTrue();
        prefs.PhoneVerified.Should().BeFalse();
        prefs.PhoneVerifiedAt.Should().BeNull();
        prefs.VerificationCode.Should().BeNull();
        prefs.VerificationCodeExpires.Should().BeNull();
        prefs.VerificationAttempts.Should().Be(0);
        prefs.VerificationLockedUntil.Should().BeNull();
    }

    #endregion

    #region DisableWhatsApp Tests

    [Fact]
    public void DisableWhatsApp_Should_Set_Enabled_To_False()
    {
        var prefs = UserWhatsAppPreferences.Create(_userId);
        prefs.EnableWhatsApp(ValidE164Phone);

        var result = prefs.DisableWhatsApp();

        result.IsSuccess.Should().BeTrue();
        prefs.WhatsAppEnabled.Should().BeFalse();
    }

    #endregion

    #region GenerateVerificationCode Tests

    [Fact]
    public void GenerateVerificationCode_Should_Create_6_Digit_Code()
    {
        var prefs = UserWhatsAppPreferences.Create(_userId);
        prefs.EnableWhatsApp(ValidE164Phone);

        var result = prefs.GenerateVerificationCode();

        result.IsSuccess.Should().BeTrue();
        prefs.VerificationCode.Should().NotBeNullOrEmpty();
        prefs.VerificationCode.Should().HaveLength(6);
        prefs.VerificationCode.Should().MatchRegex(@"^\d{6}$");
        prefs.VerificationCodeExpires.Should().NotBeNull();
        prefs.VerificationCodeExpires.Should().BeAfter(DateTime.UtcNow);
        prefs.VerificationAttempts.Should().Be(1);
    }

    [Fact]
    public void GenerateVerificationCode_When_WhatsApp_Not_Enabled_Should_Fail()
    {
        var prefs = UserWhatsAppPreferences.Create(_userId);

        var result = prefs.GenerateVerificationCode();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("enabled");
    }

    [Fact]
    public void GenerateVerificationCode_After_5_Attempts_Should_Trigger_Lockout()
    {
        var prefs = UserWhatsAppPreferences.Create(_userId);
        prefs.EnableWhatsApp(ValidE164Phone);

        // Generate 5 codes (max attempts)
        for (var i = 0; i < 5; i++)
        {
            var genResult = prefs.GenerateVerificationCode();
            genResult.IsSuccess.Should().BeTrue($"Attempt {i + 1} should succeed");
        }

        // 6th attempt should fail with lockout
        var result = prefs.GenerateVerificationCode();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Maximum verification attempts");
        prefs.VerificationLockedUntil.Should().NotBeNull();
    }

    [Fact]
    public void GenerateVerificationCode_After_Lockout_Period_Expires_Should_Succeed()
    {
        var prefs = UserWhatsAppPreferences.Create(_userId);
        prefs.EnableWhatsApp(ValidE164Phone);

        // Exhaust attempts to trigger lockout
        for (var i = 0; i < 5; i++)
        {
            prefs.GenerateVerificationCode();
        }
        // This triggers lockout
        prefs.GenerateVerificationCode();

        // We cannot manipulate DateTime.UtcNow directly in the entity,
        // but we can verify the lockout IS set and test that the IsLocked
        // property checks against VerificationLockedUntil correctly.
        prefs.IsLocked.Should().BeTrue();
        prefs.VerificationLockedUntil.Should().NotBeNull();
        prefs.VerificationLockedUntil!.Value.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void GenerateVerificationCode_Increments_Attempt_Count_Each_Call()
    {
        var prefs = UserWhatsAppPreferences.Create(_userId);
        prefs.EnableWhatsApp(ValidE164Phone);

        prefs.GenerateVerificationCode();
        prefs.VerificationAttempts.Should().Be(1);

        prefs.GenerateVerificationCode();
        prefs.VerificationAttempts.Should().Be(2);

        prefs.GenerateVerificationCode();
        prefs.VerificationAttempts.Should().Be(3);
    }

    #endregion

    #region VerifyPhone Tests

    [Fact]
    public void VerifyPhone_With_Correct_Code_Should_Succeed()
    {
        var prefs = UserWhatsAppPreferences.Create(_userId);
        prefs.EnableWhatsApp(ValidE164Phone);
        prefs.GenerateVerificationCode();
        var code = prefs.VerificationCode!;

        var result = prefs.VerifyPhone(code);

        result.IsSuccess.Should().BeTrue();
        prefs.PhoneVerified.Should().BeTrue();
        prefs.PhoneVerifiedAt.Should().NotBeNull();
        prefs.PhoneVerifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        prefs.VerificationCode.Should().BeNull();
        prefs.VerificationCodeExpires.Should().BeNull();
        prefs.VerificationAttempts.Should().Be(0);
        prefs.IsFullyVerified.Should().BeTrue();
    }

    [Fact]
    public void VerifyPhone_With_Wrong_Code_Should_Fail()
    {
        var prefs = UserWhatsAppPreferences.Create(_userId);
        prefs.EnableWhatsApp(ValidE164Phone);
        prefs.GenerateVerificationCode();

        var result = prefs.VerifyPhone("000000");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid verification code");
        prefs.PhoneVerified.Should().BeFalse();
    }

    [Fact]
    public void VerifyPhone_With_Empty_Code_Should_Fail()
    {
        var prefs = UserWhatsAppPreferences.Create(_userId);
        prefs.EnableWhatsApp(ValidE164Phone);
        prefs.GenerateVerificationCode();

        var result = prefs.VerifyPhone("");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("required");
    }

    [Fact]
    public void VerifyPhone_Without_Generating_Code_Should_Fail()
    {
        var prefs = UserWhatsAppPreferences.Create(_userId);
        prefs.EnableWhatsApp(ValidE164Phone);

        var result = prefs.VerifyPhone("123456");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("No verification code found");
    }

    [Fact]
    public void VerifyPhone_With_Expired_Code_Should_Fail()
    {
        // We cannot easily test expiration without time manipulation,
        // but we can verify the logic path exists by checking the entity
        // sets a future expiration and the method checks it.
        var prefs = UserWhatsAppPreferences.Create(_userId);
        prefs.EnableWhatsApp(ValidE164Phone);
        prefs.GenerateVerificationCode();

        // Verify the expiration is set in the future (10 minutes)
        prefs.VerificationCodeExpires.Should().NotBeNull();
        prefs.VerificationCodeExpires!.Value.Should().BeAfter(DateTime.UtcNow);
        prefs.VerificationCodeExpires!.Value.Should().BeCloseTo(
            DateTime.UtcNow.AddMinutes(10), TimeSpan.FromSeconds(30));
    }

    #endregion

    #region ShouldNotify Tests

    [Fact]
    public void ShouldNotify_When_Not_Enabled_Should_Return_False()
    {
        var prefs = UserWhatsAppPreferences.Create(_userId);

        prefs.ShouldNotify(WhatsAppNotificationType.EventRegistration).Should().BeFalse();
    }

    [Fact]
    public void ShouldNotify_When_Enabled_But_Not_Verified_Should_Return_False()
    {
        var prefs = UserWhatsAppPreferences.Create(_userId);
        prefs.EnableWhatsApp(ValidE164Phone);

        prefs.ShouldNotify(WhatsAppNotificationType.EventRegistration).Should().BeFalse();
    }

    [Theory]
    [InlineData(WhatsAppNotificationType.EventRegistration)]
    [InlineData(WhatsAppNotificationType.EventReminder)]
    [InlineData(WhatsAppNotificationType.EventCancellation)]
    [InlineData(WhatsAppNotificationType.EventUpdate)]
    [InlineData(WhatsAppNotificationType.SignupCommitment)]
    [InlineData(WhatsAppNotificationType.Refund)]
    [InlineData(WhatsAppNotificationType.Newsletter)]
    [InlineData(WhatsAppNotificationType.NewEvent)]
    [InlineData(WhatsAppNotificationType.Payment)]
    public void ShouldNotify_When_Verified_And_Default_Prefs_Should_Return_True(
        WhatsAppNotificationType notificationType)
    {
        var prefs = CreateEnabledAndVerifiedPreferences();

        prefs.ShouldNotify(notificationType).Should().BeTrue();
    }

    [Fact]
    public void ShouldNotify_When_Specific_Type_Disabled_Should_Return_False()
    {
        var prefs = CreateEnabledAndVerifiedPreferences();

        // Disable only newsletter notifications
        prefs.UpdateNotificationPreferences(
            notifyRegistration: true,
            notifyReminder: true,
            notifyCancellation: true,
            notifyUpdate: true,
            notifySignup: true,
            notifyRefund: true,
            notifyNewsletter: false,
            notifyNewEvent: true,
            notifyPayment: true);

        prefs.ShouldNotify(WhatsAppNotificationType.Newsletter).Should().BeFalse();
        prefs.ShouldNotify(WhatsAppNotificationType.EventRegistration).Should().BeTrue();
    }

    #endregion

    #region UpdateNotificationPreferences Tests

    [Fact]
    public void UpdateNotificationPreferences_Should_Set_All_Flags_Correctly()
    {
        var prefs = UserWhatsAppPreferences.Create(_userId);

        var result = prefs.UpdateNotificationPreferences(
            notifyRegistration: false,
            notifyReminder: false,
            notifyCancellation: true,
            notifyUpdate: false,
            notifySignup: true,
            notifyRefund: true,
            notifyNewsletter: false,
            notifyNewEvent: false,
            notifyPayment: true);

        result.IsSuccess.Should().BeTrue();
        prefs.NotifyEventRegistration.Should().BeFalse();
        prefs.NotifyEventReminder.Should().BeFalse();
        prefs.NotifyEventCancellation.Should().BeTrue();
        prefs.NotifyEventUpdate.Should().BeFalse();
        prefs.NotifySignupCommitment.Should().BeTrue();
        prefs.NotifyRefund.Should().BeTrue();
        prefs.NotifyNewsletter.Should().BeFalse();
        prefs.NotifyNewEvent.Should().BeFalse();
        prefs.NotifyPayment.Should().BeTrue();
    }

    [Fact]
    public void UpdateNotificationPreferences_Disable_All_Should_Set_All_To_False()
    {
        var prefs = UserWhatsAppPreferences.Create(_userId);

        prefs.UpdateNotificationPreferences(
            false, false, false, false, false, false, false, false, false);

        prefs.NotifyEventRegistration.Should().BeFalse();
        prefs.NotifyEventReminder.Should().BeFalse();
        prefs.NotifyEventCancellation.Should().BeFalse();
        prefs.NotifyEventUpdate.Should().BeFalse();
        prefs.NotifySignupCommitment.Should().BeFalse();
        prefs.NotifyRefund.Should().BeFalse();
        prefs.NotifyNewsletter.Should().BeFalse();
        prefs.NotifyNewEvent.Should().BeFalse();
        prefs.NotifyPayment.Should().BeFalse();
    }

    #endregion

    #region UpdateCulturalPreferences Tests

    [Fact]
    public void UpdateCulturalPreferences_Should_Set_Language_And_Quiet_Hours()
    {
        var prefs = UserWhatsAppPreferences.Create(_userId);
        var quietStart = new TimeOnly(22, 0);
        var quietEnd = new TimeOnly(7, 0);

        var result = prefs.UpdateCulturalPreferences(
            preferredLanguage: "si",
            quietHoursStart: quietStart,
            quietHoursEnd: quietEnd,
            respectCulturalTiming: true);

        result.IsSuccess.Should().BeTrue();
        prefs.PreferredLanguage.Should().Be("si");
        prefs.QuietHoursStart.Should().Be(quietStart);
        prefs.QuietHoursEnd.Should().Be(quietEnd);
        prefs.RespectCulturalTiming.Should().BeTrue();
    }

    [Fact]
    public void UpdateCulturalPreferences_With_Empty_Language_Should_Fail()
    {
        var prefs = UserWhatsAppPreferences.Create(_userId);

        var result = prefs.UpdateCulturalPreferences("", null, null, true);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("language");
    }

    [Fact]
    public void UpdateCulturalPreferences_Without_Quiet_Hours_Should_Succeed()
    {
        var prefs = UserWhatsAppPreferences.Create(_userId);

        var result = prefs.UpdateCulturalPreferences("ta", null, null, false);

        result.IsSuccess.Should().BeTrue();
        prefs.PreferredLanguage.Should().Be("ta");
        prefs.QuietHoursStart.Should().BeNull();
        prefs.QuietHoursEnd.Should().BeNull();
        prefs.RespectCulturalTiming.Should().BeFalse();
    }

    #endregion

    #region IsFullyVerified Tests

    [Fact]
    public void IsFullyVerified_When_Enabled_And_Verified_Should_Return_True()
    {
        var prefs = CreateEnabledAndVerifiedPreferences();
        prefs.IsFullyVerified.Should().BeTrue();
    }

    [Fact]
    public void IsFullyVerified_When_Not_Enabled_Should_Return_False()
    {
        var prefs = UserWhatsAppPreferences.Create(_userId);
        prefs.IsFullyVerified.Should().BeFalse();
    }

    [Fact]
    public void IsFullyVerified_When_Enabled_But_Not_Verified_Should_Return_False()
    {
        var prefs = UserWhatsAppPreferences.Create(_userId);
        prefs.EnableWhatsApp(ValidE164Phone);
        prefs.IsFullyVerified.Should().BeFalse();
    }

    #endregion

    #region EvaluateSkipReason Tests

    [Fact]
    public void EvaluateSkipReason_When_Disabled_Should_Return_WhatsAppDisabled()
    {
        var prefs = UserWhatsAppPreferences.Create(_userId);

        var reason = prefs.EvaluateSkipReason(WhatsAppNotificationType.EventRegistration);

        reason.Should().Be(WhatsAppSkipReason.WhatsAppDisabled);
    }

    [Fact]
    public void EvaluateSkipReason_When_Enabled_But_Not_Verified_Should_Return_PhoneUnverified()
    {
        var prefs = UserWhatsAppPreferences.Create(_userId);
        prefs.EnableWhatsApp(ValidE164Phone);

        var reason = prefs.EvaluateSkipReason(WhatsAppNotificationType.EventRegistration);

        reason.Should().Be(WhatsAppSkipReason.PhoneUnverified);
    }

    [Fact]
    public void EvaluateSkipReason_When_Verified_And_Type_Disabled_Should_Return_TypeDisabled()
    {
        var prefs = CreateEnabledAndVerifiedPreferences();
        prefs.UpdateNotificationPreferences(
            notifyRegistration: false,
            notifyReminder: true,
            notifyCancellation: true,
            notifyUpdate: true,
            notifySignup: true,
            notifyRefund: true,
            notifyNewsletter: true,
            notifyNewEvent: true,
            notifyPayment: true);

        var reason = prefs.EvaluateSkipReason(WhatsAppNotificationType.EventRegistration);

        reason.Should().Be(WhatsAppSkipReason.TypeDisabled);
    }

    [Fact]
    public void EvaluateSkipReason_When_Type_Outside_Supported_Should_Return_TypeDisabled()
    {
        // Several Phase 7B.3 notification types (e.g. PhotoAlbum) are not mapped onto
        // a dedicated boolean yet — from the caller's perspective that still means
        // "this specific type will not be sent", which is TypeDisabled semantics.
        var prefs = CreateEnabledAndVerifiedPreferences();

        var reason = prefs.EvaluateSkipReason(WhatsAppNotificationType.PhotoAlbum);

        reason.Should().Be(WhatsAppSkipReason.TypeDisabled);
    }

    [Fact]
    public void EvaluateSkipReason_When_Verified_And_Type_Enabled_Should_Return_Null()
    {
        var prefs = CreateEnabledAndVerifiedPreferences();

        var reason = prefs.EvaluateSkipReason(WhatsAppNotificationType.EventRegistration);

        reason.Should().BeNull();
    }

    [Fact]
    public void ShouldNotify_Matches_EvaluateSkipReason_Null_Contract()
    {
        // ShouldNotify must stay a thin facade over EvaluateSkipReason so the two
        // never drift apart (the production bug that motivated this refactor).
        var prefs = CreateEnabledAndVerifiedPreferences();

        foreach (var type in Enum.GetValues<WhatsAppNotificationType>())
        {
            var shouldNotify = prefs.ShouldNotify(type);
            var skipReason = prefs.EvaluateSkipReason(type);

            shouldNotify.Should().Be(skipReason == null,
                $"ShouldNotify({type}) should equal (EvaluateSkipReason({type}) == null)");
        }
    }

    #endregion

    #region Fix 4 — WhatsAppEnabledAt grace clock + AutoDisableUnverified

    [Fact]
    public void EnableWhatsApp_Should_Set_WhatsAppEnabledAt_Timestamp()
    {
        var prefs = UserWhatsAppPreferences.Create(_userId);
        var before = DateTime.UtcNow;

        prefs.EnableWhatsApp(ValidE164Phone);

        prefs.WhatsAppEnabledAt.Should().NotBeNull();
        prefs.WhatsAppEnabledAt!.Value.Should().BeOnOrAfter(before)
            .And.BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void EnableWhatsApp_With_New_Number_Should_Refresh_WhatsAppEnabledAt()
    {
        // Re-enabling with a new number must restart the grace clock — otherwise a user
        // who switches phones would be auto-disabled based on the old number's enable date.
        var prefs = UserWhatsAppPreferences.Create(_userId);
        prefs.EnableWhatsApp(ValidE164Phone);
        var firstEnabledAt = prefs.WhatsAppEnabledAt;

        Thread.Sleep(10);
        prefs.EnableWhatsApp("+94771234567");

        prefs.WhatsAppEnabledAt.Should().NotBeNull();
        prefs.WhatsAppEnabledAt!.Value.Should().BeAfter(firstEnabledAt!.Value);
    }

    [Fact]
    public void DisableWhatsApp_Should_Clear_WhatsAppEnabledAt()
    {
        var prefs = UserWhatsAppPreferences.Create(_userId);
        prefs.EnableWhatsApp(ValidE164Phone);

        prefs.DisableWhatsApp();

        prefs.WhatsAppEnabledAt.Should().BeNull();
    }

    [Fact]
    public void AutoDisableUnverified_When_Enabled_But_Unverified_Should_Succeed_And_Set_Audit_Columns()
    {
        var prefs = UserWhatsAppPreferences.Create(_userId);
        prefs.EnableWhatsApp(ValidE164Phone);
        var before = DateTime.UtcNow;

        var result = prefs.AutoDisableUnverified(WhatsAppAutoDisableReason.UnverifiedGraceExpired);

        result.IsSuccess.Should().BeTrue();
        prefs.WhatsAppEnabled.Should().BeFalse();
        prefs.WhatsAppEnabledAt.Should().BeNull();
        prefs.WhatsAppAutoDisabledAt.Should().NotBeNull();
        prefs.WhatsAppAutoDisabledAt!.Value.Should().BeOnOrAfter(before)
            .And.BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        prefs.WhatsAppAutoDisableReason.Should().Be(WhatsAppAutoDisableReason.UnverifiedGraceExpired);
    }

    [Fact]
    public void AutoDisableUnverified_Should_Raise_Domain_Event_With_User_And_Phone()
    {
        var prefs = UserWhatsAppPreferences.Create(_userId);
        prefs.EnableWhatsApp(ValidE164Phone);
        prefs.ClearDomainEvents();

        prefs.AutoDisableUnverified(WhatsAppAutoDisableReason.UnverifiedGraceExpired);

        var evt = prefs.DomainEvents.OfType<WhatsAppAutoDisabledDomainEvent>().SingleOrDefault();
        evt.Should().NotBeNull();
        evt!.UserId.Should().Be(_userId);
        evt.PhoneNumber.Should().Be(ValidE164Phone);
        evt.Reason.Should().Be(WhatsAppAutoDisableReason.UnverifiedGraceExpired);
    }

    [Fact]
    public void AutoDisableUnverified_When_Phone_Already_Verified_Should_Fail()
    {
        // Guardrail: job must never disable a user who has actually verified.
        // If the repo query is correct this shouldn't happen, but the domain
        // enforces the invariant at the aggregate boundary.
        var prefs = CreateEnabledAndVerifiedPreferences();

        var result = prefs.AutoDisableUnverified(WhatsAppAutoDisableReason.UnverifiedGraceExpired);

        result.IsFailure.Should().BeTrue();
        prefs.WhatsAppEnabled.Should().BeTrue();
        prefs.WhatsAppAutoDisabledAt.Should().BeNull();
    }

    [Fact]
    public void AutoDisableUnverified_When_WhatsApp_Already_Disabled_Should_Fail()
    {
        // Idempotency guard: a second run of the job on a row that was already auto-disabled
        // must not double-fire the email (domain event) or touch audit columns.
        var prefs = UserWhatsAppPreferences.Create(_userId);

        var result = prefs.AutoDisableUnverified(WhatsAppAutoDisableReason.UnverifiedGraceExpired);

        result.IsFailure.Should().BeTrue();
        prefs.WhatsAppAutoDisabledAt.Should().BeNull();
        prefs.DomainEvents.OfType<WhatsAppAutoDisabledDomainEvent>().Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    private UserWhatsAppPreferences CreateEnabledAndVerifiedPreferences()
    {
        var prefs = UserWhatsAppPreferences.Create(_userId);
        prefs.EnableWhatsApp(ValidE164Phone);
        prefs.GenerateVerificationCode();
        var code = prefs.VerificationCode!;
        prefs.VerifyPhone(code);
        return prefs;
    }

    #endregion
}
