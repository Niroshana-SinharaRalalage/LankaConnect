using LankaConnect.Modules.Communications.Domain.Entities;
using FluentAssertions;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.SPLIT_PER_ENTITY;
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
        typeof(LankaConnect.Modules.Identity.Domain.Entities.User),
        // Wave4.9.2.2 Phase 1.2 (2026-06-08): reference_data.state_tax_rates
        typeof(LankaConnect.Modules.Payments.Domain.Tax.StateTaxRate),
        // Wave4.9.2.3 Phase 1.3 (2026-06-08): badges.badges + badges.event_badges
        typeof(LankaConnect.Products.LankaEvents.Domain.Badges.Badge),
        typeof(LankaConnect.Products.LankaEvents.Domain.Entities.EventBadge),
        // Wave4.9.2.4 Phase 1.4 (2026-06-08): business.businesses + services + reviews
        typeof(LankaConnect.Domain.Business.Business),
        typeof(LankaConnect.Domain.Business.Service),
        typeof(LankaConnect.Domain.Business.Review),
        // Wave4.9.2.5 Phase 1.5 (2026-06-09): community.topics + community.replies
        typeof(LankaConnect.Modules.Communications.Domain.Community.ForumTopic),
        typeof(LankaConnect.Modules.Communications.Domain.Community.Reply),
        // Wave4.9.2.6 Phase 1.6 (2026-06-09): analytics.event_analytics
        typeof(LankaConnect.Products.LankaEvents.Domain.Analytics.EventAnalytics),
        // Wave4.9.2.7 Phase 1.7 (2026-06-09): communications email-side subset (8 entities)
        typeof(LankaConnect.Modules.Communications.Domain.Entities.EmailDispatchLog),
        typeof(LankaConnect.Modules.Communications.Domain.Entities.EmailFailureDetail),
        typeof(LankaConnect.Modules.Communications.Domain.Entities.EmailGroup),
        typeof(LankaConnect.Modules.Communications.Domain.Entities.EmailMessage),
        typeof(LankaConnect.Modules.Communications.Domain.Entities.EmailMetricRecord),
        typeof(LankaConnect.Modules.Communications.Domain.Entities.EmailTemplate),
        typeof(LankaConnect.Products.LankaEvents.Domain.Entities.EventNotificationHistory),
        typeof(LankaConnect.Modules.Communications.Domain.Entities.UserEmailPreferences),
        // Wave4.9.2.8 Phase 1.8 (2026-06-09): communications newsletter subset
        typeof(LankaConnect.Modules.Communications.Domain.Entities.Newsletter),
        typeof(LankaConnect.Modules.Communications.Domain.Entities.NewsletterEmailHistory),
        typeof(LankaConnect.Modules.Communications.Domain.Entities.NewsletterSubscriber),
        // Wave4.9.2.9 Phase 1.9 (2026-06-09): communications whatsapp subset
        typeof(LankaConnect.Modules.Communications.Domain.Entities.UserWhatsAppPreferences),
        typeof(LankaConnect.Modules.Communications.Domain.Entities.WhatsAppMessageRecord),
        typeof(LankaConnect.Modules.Communications.Domain.Entities.WhatsAppTemplate),
        typeof(LankaConnect.Modules.Communications.Domain.Entities.WhatsAppWebhookEvent),
        // Wave4.9.2.10a Phase 1.10a (2026-06-09): events schema - Event aggregate proper (10 entities)
        typeof(LankaConnect.Products.LankaEvents.Domain.Event),
        typeof(LankaConnect.Products.LankaEvents.Domain.Registration),
        typeof(LankaConnect.Products.LankaEvents.Domain.Sponsor),
        typeof(LankaConnect.Products.LankaEvents.Domain.SponsorshipPackage),
        typeof(LankaConnect.Products.LankaEvents.Domain.Entities.EventOrganizerContact),
        typeof(LankaConnect.Products.LankaEvents.Domain.Entities.EventSlugAlias),
        typeof(LankaConnect.Products.LankaEvents.Domain.EventTemplate),
        typeof(LankaConnect.Products.LankaEvents.Domain.EventImage),
        typeof(LankaConnect.Products.LankaEvents.Domain.EventVideo),
        typeof(LankaConnect.Products.LankaEvents.Domain.MetroArea),
        // Wave4.9.2.10b Phase 1.10b (2026-06-09): events signups + seats + venue (10 entities)
        typeof(LankaConnect.Products.LankaEvents.Domain.Entities.SignUpList),
        typeof(LankaConnect.Products.LankaEvents.Domain.Entities.SignUpItem),
        typeof(LankaConnect.Products.LankaEvents.Domain.Entities.SignUpCommitment),
        typeof(LankaConnect.Products.LankaEvents.Domain.Entities.Seat),
        typeof(LankaConnect.Products.LankaEvents.Domain.Entities.SeatHold),
        typeof(LankaConnect.Products.LankaEvents.Domain.Entities.SeatReservation),
        typeof(LankaConnect.Products.LankaEvents.Domain.Entities.VenueLayout),
        typeof(LankaConnect.Products.LankaEvents.Domain.Entities.VenueZone),
        typeof(LankaConnect.Products.LankaEvents.Domain.Entities.VenueTable),
        typeof(LankaConnect.Products.LankaEvents.Domain.Entities.VenueDecoration),
        // Wave4.9.2.10c.a Phase 1.10c.a (2026-06-09): Forms via FormsDbContext (4 entities)
        typeof(LankaConnect.Modules.Forms.Domain.Form),
        typeof(LankaConnect.Modules.Forms.Domain.Entities.FormQuestion),
        typeof(LankaConnect.Modules.Forms.Domain.Entities.FormResponse),
        typeof(LankaConnect.Modules.Forms.Domain.Entities.FormAnswer),
        // Wave4.9.2.10c.b Phase 1.10c.b (2026-06-09): Media via MediaDbContext (2 entities)
        typeof(LankaConnect.Modules.Media.Domain.PhotoAlbum),
        typeof(LankaConnect.Modules.Media.Domain.Entities.AlbumPhoto),
        // Wave4.9.2.10c.c Phase 1.10c.c (2026-06-09): events.tickets via AppDbContext
        typeof(LankaConnect.Products.LankaEvents.Domain.Entities.Ticket),
        // Wave4.9.2.10d Phase 1.10d (2026-06-09): events donations + refunds + addons (10 entities)
        typeof(LankaConnect.Products.LankaEvents.Domain.Donation),
        typeof(LankaConnect.Products.LankaEvents.Domain.Entities.RefundRequest),
        typeof(LankaConnect.Products.LankaEvents.Domain.Entities.RefundRequestLineItem),
        typeof(LankaConnect.Products.LankaEvents.Domain.RegistrationAddition),
        typeof(LankaConnect.Products.LankaEvents.Domain.RegistrationPayment),
        typeof(LankaConnect.Products.LankaEvents.Domain.AddOnDefinition),
        typeof(LankaConnect.Products.LankaEvents.Domain.AddOnPurchase),
        typeof(LankaConnect.Products.LankaEvents.Domain.Collection),
        typeof(LankaConnect.Products.LankaEvents.Domain.Entities.RegistrationModeConversion),
        typeof(LankaConnect.Products.LankaEvents.Domain.Entities.RegistrationModeConversionRow),
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
