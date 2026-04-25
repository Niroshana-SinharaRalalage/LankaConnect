using FluentAssertions;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Infrastructure.WhatsApp.Configuration;
using LankaConnect.Infrastructure.WhatsApp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace LankaConnect.Infrastructure.Tests.WhatsApp;

/// <summary>
/// Phase 7B.4 TDD tests for <see cref="TwilioTemplateSeeder"/>.
/// The seeder is an <c>IHostedService</c> that reads <c>WhatsAppSettings.TwilioContentSids</c>
/// at startup, upserts each <see cref="WhatsAppTemplate"/> row, sets the TwilioContentSid,
/// and marks the template approved via <c>MarkApprovedForTwilio()</c>.
///
/// We do NOT exercise <c>StartAsync</c> (which builds its own scope and requires a real
/// <c>IServiceScopeFactory</c>). Instead we invoke the inner <c>SeedAsync(scope, ct)</c>
/// helper directly — this keeps the test hermetic and covers every logical branch the
/// seeder contains. <c>StartAsync</c> is a thin wrapper tested implicitly by DI wiring.
/// </summary>
public class TwilioTemplateSeederTests
{
    private readonly Mock<IWhatsAppTemplateRepository> _templateRepo = new();

    private TwilioTemplateSeeder CreateSeeder(WhatsAppSettings settings)
    {
        // IServiceScopeFactory is not exercised by SeedAsync — a throwing stub is fine.
        var scopeFactory = new Mock<IServiceScopeFactory>(MockBehavior.Strict).Object;

        return new TwilioTemplateSeeder(
            scopeFactory,
            Options.Create(settings),
            NullLogger<TwilioTemplateSeeder>.Instance);
    }

    private static WhatsAppSettings SettingsWithContentSids(params (string name, string sid)[] entries)
    {
        return new WhatsAppSettings
        {
            Provider = WhatsAppProvider.Twilio,
            Enabled = true,
            TwilioContentSids = entries.ToDictionary(e => e.name, e => e.sid)
        };
    }

    private static WhatsAppTemplate NewExistingTemplate(string name, int paramCount = 1)
    {
        return WhatsAppTemplate.Create(
            templateName: name,
            displayName: name,
            category: WhatsAppTemplateCategory.Utility,
            bodyText: "Hi {{1}}",
            parameterNames: Enumerable.Range(1, paramCount).Select(i => $"p{i}").ToList(),
            language: "en");
    }

    // ─── T10: Provider=Acs short-circuits the seeder ──────────────────────────────

    [Fact]
    public async Task Seed_ProviderIsAcs_SkipsEntirely()
    {
        var settings = new WhatsAppSettings
        {
            Provider = WhatsAppProvider.Acs,
            TwilioContentSids = new Dictionary<string, string>
            {
                ["event_registration_confirmed"] = "HX00000000000000000000000000000001"
            }
        };
        var seeder = CreateSeeder(settings);

        var summary = await seeder.SeedAsync(_templateRepo.Object, CancellationToken.None);

        summary.Seeded.Should().Be(0);
        summary.Updated.Should().Be(0);
        summary.Skipped.Should().Be(0);
        summary.Reason.Should().Contain("Provider=Acs");
        _templateRepo.Verify(
            r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ─── T11: Empty TwilioContentSids dict logs info but does not throw ─────────

    [Fact]
    public async Task Seed_EmptyTwilioContentSidsDict_LogsInfo_DoesNotThrow()
    {
        var settings = new WhatsAppSettings
        {
            Provider = WhatsAppProvider.Twilio,
            TwilioContentSids = new Dictionary<string, string>()
        };
        var seeder = CreateSeeder(settings);

        var summary = await seeder.SeedAsync(_templateRepo.Object, CancellationToken.None);

        summary.Seeded.Should().Be(0);
        summary.Updated.Should().Be(0);
        summary.Reason.Should().Contain("no TwilioContentSids");
        _templateRepo.Verify(
            r => r.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ─── T9a: Existing template is updated (ContentSid set, status=Approved) ────

    [Fact]
    public async Task Seed_ExistingTemplate_UpdatesContentSid_AndMarksApproved()
    {
        const string templateName = "event_registration_confirmed";
        const string sid = "HX00000000000000000000000000000001";
        var existing = NewExistingTemplate(templateName, paramCount: 7);
        existing.Status.Should().Be(WhatsAppTemplateStatus.Pending);
        existing.TwilioContentSid.Should().BeNullOrEmpty();

        _templateRepo
            .Setup(r => r.GetByNameAsync(templateName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var seeder = CreateSeeder(SettingsWithContentSids((templateName, sid)));

        var summary = await seeder.SeedAsync(_templateRepo.Object, CancellationToken.None);

        summary.Updated.Should().Be(1);
        summary.Seeded.Should().Be(0);
        existing.TwilioContentSid.Should().Be(sid);
        existing.Status.Should().Be(WhatsAppTemplateStatus.Approved);
        existing.ApprovedAt.Should().NotBeNull();
        _templateRepo.Verify(r => r.AddAsync(It.IsAny<WhatsAppTemplate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ─── T9b: Missing template is created from built-in catalog and approved ─────

    [Fact]
    public async Task Seed_MissingTemplate_CreatesFromCatalog_AndMarksApproved()
    {
        // event_approved is a Phase 7B.3 template — added by seeder, not in Phase 7A migration.
        const string templateName = "event_approved";
        const string sid = "HX00000000000000000000000000000002";

        _templateRepo
            .Setup(r => r.GetByNameAsync(templateName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WhatsAppTemplate?)null);

        WhatsAppTemplate? added = null;
        _templateRepo
            .Setup(r => r.AddAsync(It.IsAny<WhatsAppTemplate>(), It.IsAny<CancellationToken>()))
            .Callback<WhatsAppTemplate, CancellationToken>((t, _) => added = t)
            .Returns(Task.CompletedTask);

        var seeder = CreateSeeder(SettingsWithContentSids((templateName, sid)));

        var summary = await seeder.SeedAsync(_templateRepo.Object, CancellationToken.None);

        summary.Seeded.Should().Be(1);
        summary.Updated.Should().Be(0);
        added.Should().NotBeNull();
        added!.TemplateName.Should().Be(templateName);
        added.TwilioContentSid.Should().Be(sid);
        added.Status.Should().Be(WhatsAppTemplateStatus.Approved);
    }

    // ─── T9: Running the seeder a second time produces zero new rows ────────────

    [Fact]
    public async Task Seed_IdempotentOnSecondRun_NoDuplicates()
    {
        const string templateName = "event_registration_confirmed";
        const string sid = "HX00000000000000000000000000000001";
        var existing = NewExistingTemplate(templateName);

        _templateRepo
            .Setup(r => r.GetByNameAsync(templateName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var seeder = CreateSeeder(SettingsWithContentSids((templateName, sid)));

        var first = await seeder.SeedAsync(_templateRepo.Object, CancellationToken.None);
        var second = await seeder.SeedAsync(_templateRepo.Object, CancellationToken.None);

        first.Updated.Should().Be(1);
        first.Seeded.Should().Be(0);
        second.Updated.Should().Be(1); // still an update (same SID, idempotent setter)
        second.Seeded.Should().Be(0);

        _templateRepo.Verify(
            r => r.AddAsync(It.IsAny<WhatsAppTemplate>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ─── T12: Unknown (typo) template name in config is skipped with warning ────

    [Fact]
    public async Task Seed_ConfigHasUnknownTemplateName_SkipsRow_DoesNotCreate()
    {
        // This name is NOT in the Phase 7A migration and NOT in the built-in 7B.3 catalog.
        const string bogusName = "event_regstration_confimred"; // typo
        const string sid = "HX000000000000000000000000000000FF";

        _templateRepo
            .Setup(r => r.GetByNameAsync(bogusName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((WhatsAppTemplate?)null);

        var seeder = CreateSeeder(SettingsWithContentSids((bogusName, sid)));

        var summary = await seeder.SeedAsync(_templateRepo.Object, CancellationToken.None);

        summary.Skipped.Should().Be(1);
        summary.Seeded.Should().Be(0);
        summary.Updated.Should().Be(0);
        _templateRepo.Verify(
            r => r.AddAsync(It.IsAny<WhatsAppTemplate>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ─── Resilience: a single bad row should not abort the whole run ────────────

    [Fact]
    public async Task Seed_OneTemplateThrows_OthersStillProcessed()
    {
        const string goodName = "event_registration_confirmed";
        const string badName = "event_ticket_confirmation";
        var good = NewExistingTemplate(goodName);

        _templateRepo
            .Setup(r => r.GetByNameAsync(goodName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(good);
        _templateRepo
            .Setup(r => r.GetByNameAsync(badName, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db blew up"));

        var seeder = CreateSeeder(SettingsWithContentSids(
            (goodName, "HX00000000000000000000000000000001"),
            (badName, "HX00000000000000000000000000000002")));

        var summary = await seeder.SeedAsync(_templateRepo.Object, CancellationToken.None);

        summary.Updated.Should().Be(1);
        summary.Failed.Should().Be(1);
        good.Status.Should().Be(WhatsAppTemplateStatus.Approved);
    }
}
