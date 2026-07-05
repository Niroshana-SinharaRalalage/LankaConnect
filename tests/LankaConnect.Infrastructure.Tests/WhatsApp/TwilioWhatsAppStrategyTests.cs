using FluentAssertions;
using LankaConnect.Modules.Communications.Domain.Enums;
using LankaConnect.Modules.Communications.Infrastructure.WhatsApp.Configuration;
using LankaConnect.Modules.Communications.Infrastructure.WhatsApp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace LankaConnect.Infrastructure.Tests.WhatsApp;

/// <summary>
/// Phase 7B.4 TDD tests for TwilioWhatsAppStrategy.
/// Focuses on pre-SDK failure paths (testable without mocking Twilio's static client)
/// and the content-variable serialization helper. End-to-end SDK behaviour is covered
/// by the Phase D staging test, not unit tests.
/// </summary>
public class TwilioWhatsAppStrategyTests
{
    private static TwilioWhatsAppStrategy CreateStrategy(bool sandboxMode = false)
    {
        var settings = new WhatsAppSettings
        {
            Provider = WhatsAppProvider.Twilio,
            Enabled = true,
            SandboxMode = sandboxMode,
            // Fake-but-well-formed Twilio credentials so the pre-SDK path doesn't blow up
            // on config validation. These aren't used because the tests exercise paths that
            // fail before the SDK is touched.
            TwilioAccountSid = "ACtest00000000000000000000000000",
            TwilioAuthToken = "test-auth-token",
            TwilioWhatsAppNumber = "+15551234567",
            MaxRetries = 0,
            RetryDelayBaseSeconds = 1
        };

        return new TwilioWhatsAppStrategy(
            Options.Create(settings),
            NullLogger<TwilioWhatsAppStrategy>.Instance);
    }

    // ─── T2: ContentSid-required guard (prod mode) ──────────────────────────────

    [Fact]
    public async Task SendTemplate_WithNullContentSid_ProdMode_ReturnsFailure_WithoutCallingSdk()
    {
        var strategy = CreateStrategy(sandboxMode: false);

        var result = await strategy.SendTemplateMessageAsync(
            toPhoneNumber: "+15557654321",
            templateName: "event_registration_confirmed",
            parameterValues: new[] { "Niro", "Summer Fest" },
            language: "en",
            providerTemplateId: null);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("TwilioContentSid");
        result.Error.Should().Contain("event_registration_confirmed");
    }

    [Fact]
    public async Task SendTemplate_WithEmptyContentSid_ProdMode_ReturnsFailure_WithoutCallingSdk()
    {
        var strategy = CreateStrategy(sandboxMode: false);

        var result = await strategy.SendTemplateMessageAsync(
            toPhoneNumber: "+15557654321",
            templateName: "event_reminder",
            parameterValues: new[] { "Niro" },
            language: "en",
            providerTemplateId: "");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("TwilioContentSid");
    }

    [Fact]
    public async Task SendTemplate_WithWhitespaceContentSid_ProdMode_ReturnsFailure_WithoutCallingSdk()
    {
        var strategy = CreateStrategy(sandboxMode: false);

        var result = await strategy.SendTemplateMessageAsync(
            toPhoneNumber: "+15557654321",
            templateName: "event_reminder",
            parameterValues: new[] { "Niro" },
            language: "en",
            providerTemplateId: "   ");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("TwilioContentSid");
    }

    // ─── T4: Content variables are 1-indexed ────────────────────────────────────

    [Fact]
    public void BuildContentVariables_Keys_Are_1Indexed()
    {
        var values = new[] { "first", "second", "third" };

        var json = TwilioWhatsAppStrategy.BuildContentVariables(values);

        json.Should().Contain("\"1\":\"first\"");
        json.Should().Contain("\"2\":\"second\"");
        json.Should().Contain("\"3\":\"third\"");
        json.Should().NotContain("\"0\":");
    }

    [Fact]
    public void BuildContentVariables_EmptyList_ReturnsEmptyJsonObject()
    {
        var json = TwilioWhatsAppStrategy.BuildContentVariables(Array.Empty<string>());

        json.Should().Be("{}");
    }

    [Fact]
    public void BuildContentVariables_EscapesQuotesInValues()
    {
        var values = new[] { "has \"quote\" inside" };

        var json = TwilioWhatsAppStrategy.BuildContentVariables(values);

        // System.Text.Json escapes " as \" — the JSON string must remain valid.
        // Parsing it back must not throw and value[0] must round-trip.
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        doc.RootElement.GetProperty("1").GetString().Should().Be("has \"quote\" inside");
    }

    // ─── T5: >99 params returns Failure, never throws ──────────────────────────

    [Fact]
    public async Task SendTemplate_MoreThan99Params_ReturnsFailure_DoesNotThrow()
    {
        var strategy = CreateStrategy(sandboxMode: false);
        var tooMany = Enumerable.Range(1, 100).Select(i => $"val{i}").ToArray();

        Func<Task> act = async () => await strategy.SendTemplateMessageAsync(
            toPhoneNumber: "+15557654321",
            templateName: "some_template",
            parameterValues: tooMany,
            language: "en",
            providerTemplateId: "HX1234567890abcdef1234567890abcdef");

        // Must not throw
        await act.Should().NotThrowAsync();

        var result = await strategy.SendTemplateMessageAsync(
            toPhoneNumber: "+15557654321",
            templateName: "some_template",
            parameterValues: tooMany,
            language: "en",
            providerTemplateId: "HX1234567890abcdef1234567890abcdef");

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("99");
    }

    [Fact]
    public async Task SendTemplate_Exactly99Params_DoesNotRejectOnCountCheck()
    {
        // Sending 99 params must NOT trip the >99 guard.
        // It may still fail downstream for other reasons (no real Twilio creds),
        // but the failure message must not mention the "99" limit.
        var strategy = CreateStrategy(sandboxMode: false);
        var ninetyNine = Enumerable.Range(1, 99).Select(i => $"val{i}").ToArray();

        var result = await strategy.SendTemplateMessageAsync(
            toPhoneNumber: "+15557654321",
            templateName: "some_template",
            parameterValues: ninetyNine,
            language: "en",
            providerTemplateId: "HX1234567890abcdef1234567890abcdef");

        // The test creds aren't real, so we accept failure — we just require that the
        // failure is NOT from our 99-cap guard.
        if (!result.IsSuccess)
        {
            result.Error.Should().NotContain("max 99");
            result.Error.Should().NotContain("got 99");
        }
    }
}
