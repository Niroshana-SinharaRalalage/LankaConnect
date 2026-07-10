using FluentAssertions;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Modules.Communications.Domain.Enums;
using LankaConnect.Modules.Communications.Infrastructure.WhatsApp.Configuration;
using LankaConnect.Modules.Communications.Infrastructure.WhatsApp.Services;
using LankaConnect.Modules.Communications.Contracts.WhatsApp.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace LankaConnect.Infrastructure.Tests.WhatsApp;

/// <summary>
/// Phase 7B.4 bug-fix TDD: <see cref="TwilioPhoneVerificationService"/> must send the
/// verification code via the Twilio <b>WhatsApp</b> Content API using the
/// <c>phone_verification</c> template — NOT via SMS. The prior SMS-based impl failed
/// with Twilio error 21660 because the WhatsApp sender number (+12343513717) has no
/// SMS capability. The durable fix reuses <see cref="IWhatsAppSendStrategy"/> and the
/// already-provisioned <c>phone_verification</c> ContentSid.
/// </summary>
public class TwilioPhoneVerificationServiceTests
{
    private readonly Mock<IWhatsAppSendStrategy> _strategy = new(MockBehavior.Strict);

    private TwilioPhoneVerificationService CreateService(WhatsAppSettings settings)
    {
        return new TwilioPhoneVerificationService(
            Options.Create(settings),
            _strategy.Object,
            NullLogger<TwilioPhoneVerificationService>.Instance);
    }

    private static WhatsAppSettings DefaultSettings(string? contentSid = "HXabcdef0123456789abcdef0123456789")
    {
        var settings = new WhatsAppSettings
        {
            Provider = WhatsAppProvider.Twilio,
            Enabled = true,
            TwilioAccountSid = "ACtest00000000000000000000000000",
            TwilioAuthToken = "test-auth-token",
            TwilioWhatsAppNumber = "+12343513717",
            TwilioContentSids = new Dictionary<string, string>()
        };
        if (contentSid != null)
        {
            settings.TwilioContentSids[WhatsAppTemplateContract.TemplateNames.PhoneVerification] = contentSid;
        }
        return settings;
    }

    // ─── Happy path: sends via IWhatsAppSendStrategy with the right template/ContentSid ──

    [Fact]
    public async Task SendVerificationCode_Enabled_CallsStrategyWithPhoneVerificationTemplate()
    {
        const string phone = "+18609780124";
        const string code = "123456";
        const string contentSid = "HX67ba357847341b891d999b583fc6fa27";

        _strategy
            .Setup(s => s.SendTemplateMessageAsync(
                phone,
                WhatsAppTemplateContract.TemplateNames.PhoneVerification,
                It.Is<IReadOnlyList<string>>(p => p.Count == 1 && p[0] == code),
                "en",
                contentSid,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("MMtest000000000000000000000000"));

        var service = CreateService(DefaultSettings(contentSid));

        var result = await service.SendVerificationCodeAsync(phone, code);

        result.IsSuccess.Should().BeTrue();
        _strategy.VerifyAll();
    }

    // ─── Durable failure: ContentSid not configured (seeder missed or config drift) ──

    [Fact]
    public async Task SendVerificationCode_MissingContentSidInConfig_ReturnsFailure_WithoutCallingStrategy()
    {
        var settings = DefaultSettings(contentSid: null);
        var service = CreateService(settings);

        var result = await service.SendVerificationCodeAsync("+18609780124", "123456");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("phone_verification");
        _strategy.Verify(s => s.SendTemplateMessageAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(),
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── Guard: WhatsApp globally disabled ─────────────────────────────────────────

    [Fact]
    public async Task SendVerificationCode_WhatsAppDisabled_ReturnsFailure_WithoutCallingStrategy()
    {
        var settings = DefaultSettings();
        settings.Enabled = false;
        var service = CreateService(settings);

        var result = await service.SendVerificationCodeAsync("+18609780124", "123456");

        result.IsSuccess.Should().BeFalse();
        _strategy.Verify(s => s.SendTemplateMessageAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(),
            It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── Input guards ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task SendVerificationCode_EmptyPhone_ReturnsFailure()
    {
        var service = CreateService(DefaultSettings());

        var result = await service.SendVerificationCodeAsync("", "123456");

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task SendVerificationCode_EmptyCode_ReturnsFailure()
    {
        var service = CreateService(DefaultSettings());

        var result = await service.SendVerificationCodeAsync("+18609780124", "");

        result.IsSuccess.Should().BeFalse();
    }

    // ─── Propagates strategy failure so caller sees the real reason ─────────────

    [Fact]
    public async Task SendVerificationCode_StrategyFails_ReturnsFailure()
    {
        const string phone = "+18609780124";
        const string code = "123456";
        const string contentSid = "HX67ba357847341b891d999b583fc6fa27";

        _strategy
            .Setup(s => s.SendTemplateMessageAsync(
                phone,
                WhatsAppTemplateContract.TemplateNames.PhoneVerification,
                It.IsAny<IReadOnlyList<string>>(),
                "en",
                contentSid,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Failure("Twilio API error: 63015"));

        var service = CreateService(DefaultSettings(contentSid));

        var result = await service.SendVerificationCodeAsync(phone, code);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("63015");
    }
}
