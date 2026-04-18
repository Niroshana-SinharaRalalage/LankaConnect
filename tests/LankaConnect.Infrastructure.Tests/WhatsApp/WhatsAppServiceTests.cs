using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Infrastructure.WhatsApp.Configuration;
using LankaConnect.Infrastructure.WhatsApp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace LankaConnect.Infrastructure.Tests.WhatsApp;

/// <summary>
/// Phase 7B.4 TDD tests for WhatsAppService provider-SID wiring.
/// Verifies that WhatsAppService resolves template.TwilioContentSid and passes it
/// through to IWhatsAppSendStrategy.SendTemplateMessageAsync as the providerTemplateId
/// argument. This closes the wiring gap that left Twilio sends dispatching without a
/// ContentSid (Phase 7B.4 gap G2).
/// </summary>
public class WhatsAppServiceTests
{
    private readonly Mock<IWhatsAppSendStrategy> _strategy = new();
    private readonly Mock<IWhatsAppMessageRepository> _messageRepo = new();
    private readonly Mock<IWhatsAppTemplateRepository> _templateRepo = new();
    private readonly Mock<IUserWhatsAppPreferencesRepository> _prefsRepo = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private WhatsAppService CreateService(WhatsAppProvider provider = WhatsAppProvider.Twilio)
    {
        var settings = new WhatsAppSettings
        {
            Provider = provider,
            Enabled = true,
            SenderPhoneNumber = "+15551234567"
        };

        // Default strategy stub — returns a Twilio SID success so the send path completes.
        _strategy
            .Setup(s => s.SendTemplateMessageAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<string>.Success("SM_twilio_id_abc"));

        _messageRepo
            .Setup(r => r.HasRecentDuplicateAsync(
                It.IsAny<Guid>(), It.IsAny<Guid?>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _messageRepo
            .Setup(r => r.AddAsync(It.IsAny<WhatsAppMessageRecord>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _uow.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        return new WhatsAppService(
            _strategy.Object,
            _messageRepo.Object,
            _templateRepo.Object,
            _prefsRepo.Object,
            _uow.Object,
            Options.Create(settings),
            NullLogger<WhatsAppService>.Instance);
    }

    private static UserWhatsAppPreferences CreateVerifiedPrefs(Guid userId, string phone = "+15557654321")
    {
        var prefs = UserWhatsAppPreferences.Create(userId);
        prefs.EnableWhatsApp(phone);
        prefs.GenerateVerificationCode();
        var code = prefs.VerificationCode!;
        prefs.VerifyPhone(code);
        return prefs;
    }

    private static WhatsAppTemplate CreateApprovedTwilioTemplate(
        string templateName, string contentSid, List<string> parameterNames)
    {
        var t = WhatsAppTemplate.Create(
            templateName: templateName,
            displayName: templateName,
            category: WhatsAppTemplateCategory.Utility,
            bodyText: "body {{1}}",
            parameterNames: parameterNames);
        t.SetTwilioContentSid(contentSid);
        t.MarkApprovedForTwilio();
        return t;
    }

    // ─── T6: TwilioContentSid flows through to strategy ────────────────────────

    [Fact]
    public async Task SendTemplateMessageAsync_PassesTemplateContentSid_AsProviderTemplateId()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var contentSid = "HX1234567890abcdef1234567890abcdef";
        var templateName = "event_registration_confirmed";

        _prefsRepo
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateVerifiedPrefs(userId));

        _templateRepo
            .Setup(r => r.GetByNameAsync(templateName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateApprovedTwilioTemplate(
                templateName,
                contentSid,
                new List<string> { "user_name", "event_title" }));

        var service = CreateService(WhatsAppProvider.Twilio);
        var parameters = new Dictionary<string, string>
        {
            { "user_name", "Niro" },
            { "event_title", "Summer Fest" }
        };

        // Act
        var result = await service.SendTemplateMessageAsync(
            userId,
            templateName,
            parameters,
            WhatsAppNotificationType.EventRegistration);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.WasSkipped.Should().BeFalse();

        _strategy.Verify(
            s => s.SendTemplateMessageAsync(
                It.IsAny<string>(),
                templateName,
                It.Is<IReadOnlyList<string>>(v => v.Count == 2 && v[0] == "Niro" && v[1] == "Summer Fest"),
                It.IsAny<string>(),
                contentSid,
                It.IsAny<CancellationToken>()),
            Times.Once,
            "strategy must receive the TwilioContentSid as providerTemplateId");
    }

    [Fact]
    public async Task SendTemplateMessageAsync_Twilio_TemplateWithoutContentSid_ReturnsFailure()
    {
        // Arrange: template is approved (Twilio path) but ContentSid never got set — malformed seed state.
        // WhatsAppService gate (IsApproved) lets it through; the strategy must refuse in prod mode.
        // In this unit test we assert the strategy receives a null/empty providerTemplateId
        // (the failure itself is Twilio-strategy behaviour covered by TwilioWhatsAppStrategyTests.T2).
        var userId = Guid.NewGuid();
        var templateName = "event_registration_confirmed";

        _prefsRepo
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateVerifiedPrefs(userId));

        var tmpl = WhatsAppTemplate.Create(
            templateName, templateName, WhatsAppTemplateCategory.Utility,
            "body {{1}}", new List<string> { "user_name" });
        tmpl.MarkApprovedForTwilio(); // Approved but no ContentSid set.

        _templateRepo
            .Setup(r => r.GetByNameAsync(templateName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tmpl);

        var service = CreateService(WhatsAppProvider.Twilio);

        // Act
        await service.SendTemplateMessageAsync(
            userId,
            templateName,
            new Dictionary<string, string> { { "user_name", "Niro" } },
            WhatsAppNotificationType.EventRegistration);

        // Assert: strategy was invoked with NULL providerTemplateId (the failure behaviour is the
        // strategy's responsibility — tested in TwilioWhatsAppStrategyTests).
        _strategy.Verify(
            s => s.SendTemplateMessageAsync(
                It.IsAny<string>(),
                templateName,
                It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<string>(),
                It.Is<string?>(sid => string.IsNullOrEmpty(sid)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
