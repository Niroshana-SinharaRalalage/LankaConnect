using FluentAssertions;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Infrastructure.Data;
using LankaConnect.Modules.Forms.Infrastructure.Data;
using LankaConnect.Modules.Media.Infrastructure.Data;
using LankaConnect.Modules.Notifications.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LankaConnect.Infrastructure.Tests.Configuration;

/// <summary>
/// Wave4.9.1.3 (2026-06-08): asserts every IAuditable entity across all
/// 4 DbContexts has its CreatedBy + UpdatedBy properties ignored
/// (per <c>IgnoreAuditByActorPropertiesUntilPhase1</c> hotfix). Phase
/// 1.x of Wave4.9.2 will progressively REMOVE the ignore + add the
/// physical columns per schema group — this test will then update to
/// allow snake_case-mapped properties as a valid alternative to ignored.
/// </summary>
/// <remarks>
/// Architect P5 ruling 2026-06-08: use Npgsql model-builder-only (no real
/// DB connection). <c>UseNpgsql("Host=fake;...")</c> lets EF Core build
/// the model graph without connecting; we then inspect <c>IModel</c>
/// metadata via reflection. ~10x faster than Testcontainers + tests the
/// actual production model configuration.
///
/// Per CLAUDE.md §13.1 trigger T4 (EF Core configuration coverage) + T6
/// (DbContext registration assertions).
/// </remarks>
public sealed class IAuditableIgnoreCoverageTests
{
    private const string FakeConn = "Host=fake;Database=fake;Username=fake;Password=fake";

    [Fact]
    public void AppDbContext_Ignores_CreatedBy_For_All_IAuditable_Entities()
    {
        var model = BuildAppDbContextModel();
        AssertNoIAuditableLeak(model, propertyName: "CreatedBy", contextName: nameof(AppDbContext));
    }

    [Fact]
    public void AppDbContext_Ignores_UpdatedBy_For_All_IAuditable_Entities()
    {
        var model = BuildAppDbContextModel();
        AssertNoIAuditableLeak(model, propertyName: "UpdatedBy", contextName: nameof(AppDbContext));
    }

    [Fact]
    public void NotificationsDbContext_Ignores_CreatedBy_For_All_IAuditable_Entities()
    {
        var model = BuildNotificationsDbContextModel();
        AssertNoIAuditableLeak(model, propertyName: "CreatedBy", contextName: nameof(NotificationsDbContext));
    }

    [Fact]
    public void NotificationsDbContext_Ignores_UpdatedBy_For_All_IAuditable_Entities()
    {
        var model = BuildNotificationsDbContextModel();
        AssertNoIAuditableLeak(model, propertyName: "UpdatedBy", contextName: nameof(NotificationsDbContext));
    }

    [Fact]
    public void MediaDbContext_Ignores_CreatedBy_For_All_IAuditable_Entities()
    {
        var model = BuildMediaDbContextModel();
        AssertNoIAuditableLeak(model, propertyName: "CreatedBy", contextName: nameof(MediaDbContext));
    }

    [Fact]
    public void MediaDbContext_Ignores_UpdatedBy_For_All_IAuditable_Entities()
    {
        var model = BuildMediaDbContextModel();
        AssertNoIAuditableLeak(model, propertyName: "UpdatedBy", contextName: nameof(MediaDbContext));
    }

    [Fact]
    public void FormsDbContext_Ignores_CreatedBy_For_All_IAuditable_Entities()
    {
        var model = BuildFormsDbContextModel();
        AssertNoIAuditableLeak(model, propertyName: "CreatedBy", contextName: nameof(FormsDbContext));
    }

    [Fact]
    public void FormsDbContext_Ignores_UpdatedBy_For_All_IAuditable_Entities()
    {
        var model = BuildFormsDbContextModel();
        AssertNoIAuditableLeak(model, propertyName: "UpdatedBy", contextName: nameof(FormsDbContext));
    }

    /// <summary>
    /// For every entity in the model that implements <see cref="IAuditable"/>,
    /// fail if its EF property mapping still includes the named audit-by-actor
    /// property in an invalid state. The invariant adapts per-phase:
    /// <list type="bullet">
    ///   <item>Pre-Wave4.9.2: ALL IAuditable entities must Ignore() CreatedBy + UpdatedBy.</item>
    ///   <item>Post-Phase 1.1 (Wave4.9.2.1, 2026-06-08): entities in
    ///         <see cref="Phase1RelaxedTypes"/> MUST instead map to
    ///         <c>created_by</c>/<c>updated_by</c> snake_case columns
    ///         (HasColumnName invariant). All other IAuditable entities
    ///         continue to require Ignore().</item>
    /// </list>
    /// </summary>
    private static readonly HashSet<Type> Phase1RelaxedTypes = new()
    {
        // Wave4.9.2.1 Phase 1.1 (2026-06-08): identity.users
        typeof(LankaConnect.Domain.Users.User),
        // Wave4.9.2.2 Phase 1.2 (2026-06-08): reference_data.state_tax_rates
        typeof(LankaConnect.Domain.Tax.StateTaxRate),
        // Wave4.9.2.3 Phase 1.3 (2026-06-08): badges.badges + badges.event_badges
        typeof(LankaConnect.Domain.Badges.Badge),
        typeof(LankaConnect.Domain.Events.Entities.EventBadge),
        // Wave4.9.2.4 Phase 1.4 (2026-06-08): business.businesses + services + reviews
        typeof(LankaConnect.Domain.Business.Business),
        typeof(LankaConnect.Domain.Business.Service),
        typeof(LankaConnect.Domain.Business.Review),
        // Wave4.9.2.5 Phase 1.5 (2026-06-09): community.topics + community.replies
        typeof(LankaConnect.Domain.Community.ForumTopic),
        typeof(LankaConnect.Domain.Community.Reply),
        // Wave4.9.2.6 Phase 1.6 (2026-06-09): analytics.event_analytics
        typeof(LankaConnect.Domain.Analytics.EventAnalytics),
        // Wave4.9.2.7 Phase 1.7 (2026-06-09): communications email-side subset (8 entities)
        typeof(LankaConnect.Domain.Communications.Entities.EmailDispatchLog),
        typeof(LankaConnect.Domain.Communications.Entities.EmailFailureDetail),
        typeof(LankaConnect.Domain.Communications.Entities.EmailGroup),
        typeof(LankaConnect.Domain.Communications.Entities.EmailMessage),
        typeof(LankaConnect.Domain.Communications.Entities.EmailMetricRecord),
        typeof(LankaConnect.Domain.Communications.Entities.EmailTemplate),
        typeof(LankaConnect.Domain.Events.Entities.EventNotificationHistory),
        typeof(LankaConnect.Domain.Communications.Entities.UserEmailPreferences),
        // Wave4.9.2.8 Phase 1.8 (2026-06-09): communications newsletter subset
        typeof(LankaConnect.Domain.Communications.Entities.Newsletter),
        typeof(LankaConnect.Domain.Communications.Entities.NewsletterEmailHistory),
        typeof(LankaConnect.Domain.Communications.Entities.NewsletterSubscriber),
        // Wave4.9.2.9 Phase 1.9 (2026-06-09): communications whatsapp subset
        typeof(LankaConnect.Domain.Communications.Entities.UserWhatsAppPreferences),
        typeof(LankaConnect.Domain.Communications.Entities.WhatsAppMessageRecord),
        typeof(LankaConnect.Domain.Communications.Entities.WhatsAppTemplate),
        typeof(LankaConnect.Domain.Communications.Entities.WhatsAppWebhookEvent),
        // Wave4.9.2.10a Phase 1.10a (2026-06-09): events schema - Event aggregate proper (10 entities)
        typeof(LankaConnect.Domain.Events.Event),
        typeof(LankaConnect.Domain.Events.Registration),
        typeof(LankaConnect.Domain.Events.Sponsor),
        typeof(LankaConnect.Domain.Events.SponsorshipPackage),
        typeof(LankaConnect.Domain.Events.Entities.EventOrganizerContact),
        typeof(LankaConnect.Domain.Events.Entities.EventSlugAlias),
        typeof(LankaConnect.Domain.Events.EventTemplate),
        typeof(LankaConnect.Domain.Events.EventImage),
        typeof(LankaConnect.Domain.Events.EventVideo),
        typeof(LankaConnect.Domain.Events.MetroArea),
    };

    private static void AssertNoIAuditableLeak(IModel model, string propertyName, string contextName)
    {
        var iauditableType = typeof(IAuditable);
        var expectedColumnName = propertyName == "CreatedBy" ? "created_by" : "updated_by";
        var leaks = new List<string>();
        var auditableEntityCount = 0;

        foreach (var et in model.GetEntityTypes())
        {
            if (!iauditableType.IsAssignableFrom(et.ClrType))
            {
                continue;
            }
            auditableEntityCount++;

            var prop = et.FindProperty(propertyName);
            if (prop is null)
            {
                // Property is Ignore()'d — original invariant holds.
                continue;
            }

            if (Phase1RelaxedTypes.Contains(et.ClrType))
            {
                // Property is mapped — accept ONLY if it maps to the
                // expected snake_case column name. Anything else is a leak.
                var actualColumnName = prop.GetColumnName();
                if (actualColumnName == expectedColumnName)
                {
                    continue;
                }
                leaks.Add($"{et.ClrType.FullName} (mapped to '{actualColumnName}', expected '{expectedColumnName}')");
            }
            else
            {
                // Entity is NOT in the relaxed allowlist — property should
                // be Ignore()'d but is mapped. Leak.
                leaks.Add(et.ClrType.FullName ?? et.ClrType.Name);
            }
        }

        // Sanity check: we must find at least one IAuditable entity per context
        // (otherwise the test silently passes via empty-set tautology).
        auditableEntityCount.Should().BeGreaterThan(0,
            because: $"{contextName} should contain at least one IAuditable-implementing entity; the IAuditable bridge across the codebase guarantees this. If 0, either the model didn't load or IAuditable was severed.");

        leaks.Should().BeEmpty(
            because: $"the hotfix at {contextName}.IgnoreAuditByActorPropertiesUntilPhase1 + per-entity Ignore() must apply to EVERY IAuditable entity that does NOT yet have physical {expectedColumnName} columns. Post-Wave4.9.2.x entities in Phase1RelaxedTypes MUST be mapped to the expected snake_case column name. Leaks in {contextName}: {string.Join(", ", leaks)}");
    }

    private static IModel BuildAppDbContextModel()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(FakeConn)
            .Options;
        var publisher = new Mock<IPublisher>().Object;
        var logger = NullLogger<AppDbContext>.Instance;
        using var ctx = new AppDbContext(options, publisher, logger);
        return ctx.Model;
    }

    private static IModel BuildNotificationsDbContextModel()
    {
        var options = new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseNpgsql(FakeConn)
            .Options;
        using var ctx = new NotificationsDbContext(options);
        return ctx.Model;
    }

    private static IModel BuildMediaDbContextModel()
    {
        var options = new DbContextOptionsBuilder<MediaDbContext>()
            .UseNpgsql(FakeConn)
            .Options;
        using var ctx = new MediaDbContext(options);
        return ctx.Model;
    }

    private static IModel BuildFormsDbContextModel()
    {
        var options = new DbContextOptionsBuilder<FormsDbContext>()
            .UseNpgsql(FakeConn)
            .Options;
        using var ctx = new FormsDbContext(options);
        return ctx.Model;
    }
}
