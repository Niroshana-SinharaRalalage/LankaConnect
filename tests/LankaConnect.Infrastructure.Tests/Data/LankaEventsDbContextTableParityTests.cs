using FluentAssertions;
using LankaConnect.Domain.Analytics;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LankaConnect.Infrastructure.Tests.Data;

/// <summary>
/// Wave 6.5.f.5-hotfix2 acceptance criterion §4.1 (architect ruling 2026-07-04 follow-up).
///
/// Guards against the physical-schema divergence bug that produced 94 API smoke failures
/// on hotfix1 staging (up from 46 pre-hotfix). Root cause: LankaEventsDbContext has
/// HasDefaultSchema("events") + ApplyConfigurationsFromAssembly, but several swept EF
/// configs (EventConfiguration, SignUpListConfiguration, SignUpItemConfiguration,
/// SignUpCommitmentConfiguration) had NO .ToTable() call at all — EF defaulted the
/// table name to the DbSet property name (PascalCase). Others had .ToTable("<name>")
/// without an explicit schema, silently relying on HasDefaultSchema even when the
/// physical table lives in a different schema (EventBadge → badges,
/// EventAnalytics/EventViewRecord → analytics).
///
/// AppDbContext.ConfigureSchemas() is the PHYSICAL-SCHEMA AUTHORITY — its explicit
/// .ToTable("<snake_case>", "<schema>") overrides mirror the physical Postgres schema.
/// These tests assert LankaEventsDbContext produces the identical (schema, tableName)
/// pair for every entity that both contexts map.
///
/// Rule 5e (revised 2026-07-04): parity tests must assert (1) mapping presence,
/// (2) schema parity, (3) table-name parity. This class is the specific test that
/// would have caught the 94-failure hotfix1 regression at build time. Wave 6.5.f.7
/// promotes this pattern to permanent parity tests for all 6 module DbContexts.
///
/// Rule 5i (new 2026-07-04): any DbContext with HasDefaultSchema requires every
/// swept config to use two-arg ToTable(name, schema) form. The tests below enforce
/// that rule by asserting the LankaEventsDbContext-produced mapping matches the
/// physical Postgres schema (derived from AppDbContext.ConfigureSchemas() + migrations).
/// </summary>
public sealed class LankaEventsDbContextTableParityTests
{
    private static LankaEventsDbContext CreateLankaEventsContext()
    {
        var options = new DbContextOptionsBuilder<LankaEventsDbContext>()
            .UseInMemoryDatabase("parity-lankaevents")
            .Options;
        return new LankaEventsDbContext(options);
    }

    // Entities that both DbContexts map (shared during the dual-mapping window).
    // Wave 6.5.f.6 will eventually eliminate the AppDbContext side of this list.
    // Values sourced from AppDbContext.ConfigureSchemas() (lines 437-517) + migrations.
    public static IEnumerable<object[]> SharedEntities()
    {
        // Aggregate roots
        yield return new object[] { typeof(Event), "events", "events" };
        yield return new object[] { typeof(Registration), "events", "registrations" };

        // Sign-up lists (event-scoped)
        yield return new object[] { typeof(SignUpList), "events", "sign_up_lists" };
        yield return new object[] { typeof(SignUpItem), "events", "sign_up_items" };
        yield return new object[] { typeof(SignUpCommitment), "events", "sign_up_commitments" };

        // Ticketing
        yield return new object[] { typeof(Ticket), "events", "tickets" };
        yield return new object[] { typeof(TicketTier), "events", "ticket_tiers" };
        yield return new object[] { typeof(TierAssignment), "events", "tier_assignments" };

        // Venue seating
        yield return new object[] { typeof(VenueLayout), "events", "venue_layouts" };
        yield return new object[] { typeof(VenueZone), "events", "venue_zones" };
        yield return new object[] { typeof(VenueTable), "events", "venue_tables" };
        yield return new object[] { typeof(VenueDecoration), "events", "venue_decorations" };
        yield return new object[] { typeof(Seat), "events", "seats" };
        yield return new object[] { typeof(SeatHold), "events", "seat_holds" };
        yield return new object[] { typeof(SeatReservation), "events", "seat_reservations" };

        // Finance
        yield return new object[] { typeof(AddOnDefinition), "events", "add_on_definitions" };
        yield return new object[] { typeof(AddOnPurchase), "events", "add_on_purchases" };
        yield return new object[] { typeof(Donation), "events", "donations" };
        yield return new object[] { typeof(Collection), "events", "collections" };
        yield return new object[] { typeof(Sponsor), "events", "sponsors" };
        yield return new object[] { typeof(SponsorshipPackage), "events", "sponsorship_packages" };
        yield return new object[] { typeof(RegistrationAddition), "events", "registration_additions" };
        yield return new object[] { typeof(RegistrationPayment), "events", "registration_payments" };

        // Registration mode conversions
        yield return new object[] { typeof(RegistrationModeConversion), "events", "registration_mode_conversions" };
        yield return new object[] { typeof(RegistrationModeConversionRow), "events", "registration_mode_conversion_rows" };

        // Event descendants + media (note: EventImages / EventVideos preserve PascalCase per AppDbContext)
        yield return new object[] { typeof(EventTemplate), "events", "event_templates" };
        yield return new object[] { typeof(MetroArea), "events", "metro_areas" };

        // Cross-schema entities
        yield return new object[] { typeof(EventAnalytics), "analytics", "event_analytics" };
        yield return new object[] { typeof(EventViewRecord), "analytics", "event_view_records" };

        // Junction (Products-owned; physical schema = events per Phase 6A.32 migration)
        yield return new object[] { typeof(EventEmailGroupLink), "events", "event_email_groups" };
    }

    /// <summary>
    /// The core parity assertion per revised Rule 5e (2026-07-04): LankaEventsDbContext's
    /// model must produce the SAME (schema, tableName) pair for a given entity type that
    /// the physical Postgres schema uses. This is Rule 5e assertions (2) + (3) codified
    /// in test form.
    ///
    /// Why hard-code the (schema, tableName) constants instead of comparing against
    /// AppDbContext.Model directly: instantiating AppDbContext with InMemory would drag
    /// in the runtime Assembly.Load() call in OnModelCreating and the full module
    /// DbContext registration graph. Constants sourced from grep of AppDbContext.
    /// ConfigureSchemas() (lines 437-517) + physical migration files are simpler.
    /// </summary>
    [Theory]
    [MemberData(nameof(SharedEntities))]
    public void SharedEntity_HasCorrectSchemaAndTableName_InLankaEventsDbContext(
        Type entityType,
        string expectedSchema,
        string expectedTableName)
    {
        using var ctx = CreateLankaEventsContext();

        var mapping = ctx.Model.FindEntityType(entityType);

        mapping.Should().NotBeNull(
            $"{entityType.Name} must be mapped in LankaEventsDbContext (mapping presence — Rule 5e.1)");

        mapping!.GetSchema().Should().Be(expectedSchema,
            $"Rule 5e.2 physical schema parity for {entityType.Name}. " +
            $"LankaEventsDbContext resolved '{mapping!.GetSchema()}' but physical Postgres " +
            $"uses '{expectedSchema}'. Fix: add explicit .ToTable(\"{expectedTableName}\", " +
            $"\"{expectedSchema}\") to {entityType.Name}Configuration.Configure() per Rule 5i.");

        mapping!.GetTableName().Should().Be(expectedTableName,
            $"Rule 5e.3 physical table-name parity for {entityType.Name}. " +
            $"LankaEventsDbContext resolved '{mapping!.GetTableName()}' but physical Postgres " +
            $"uses '{expectedTableName}'. Fix: add explicit .ToTable(\"{expectedTableName}\", " +
            $"\"{expectedSchema}\") to {entityType.Name}Configuration.Configure() per Rule 5i.");
    }
}
