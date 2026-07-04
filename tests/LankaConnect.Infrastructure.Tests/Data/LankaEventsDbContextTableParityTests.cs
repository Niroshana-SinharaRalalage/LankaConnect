using FluentAssertions;
using LankaConnect.Domain.Analytics;
using LankaConnect.Infrastructure.Data;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LankaConnect.Infrastructure.Tests.Data;

/// <summary>
/// Wave 6.5.f.5-hotfix2b acceptance criterion §6.1 + §6.2 (architect ruling 2026-07-04 #3).
///
/// REFINED per Rule 5e.2 (2026-07-04): expected (schema, tableName) values are
/// derived from <see cref="AppDbContext.Model"/> — the PHYSICAL-SCHEMA AUTHORITY per
/// §3.4 of the follow-up ruling — via an InMemory AppDbContext instance, NOT from
/// hand-transcribed constants. Hand-transcribed constants were divergence-blind by
/// construction and missed three physical-schema divergences in hotfix2
/// (TicketTier/TicketScanLog/EventEmailGroupLink all mapped to "events" when
/// physical was public/null).
///
/// The refined pattern here asserts LankaEventsDbContext.Model produces IDENTICAL
/// (schema, tableName) pairs to AppDbContext.Model for every shared entity. If both
/// contexts agree, and AppDbContext is the physical-schema authority, then
/// LankaEventsDbContext is physical-correct by transitivity.
///
/// The companion snapshot-parity test (per §6.2) is left to Wave 6.5.f.7, which
/// promotes this pattern to permanent parity tests for all 6 module DbContexts and
/// adds the migration-derived-truth check for the ~12 curated entities in
/// AppDbContext.ConfigureSchemas().
/// </summary>
public sealed class LankaEventsDbContextTableParityTests
{
    // Both DbContexts use Npgsql provider (matches runtime) with a bogus connection
    // string. Model-build does NOT require an actual DB connection — EF Core caches
    // the model on first .Model access without any query. Neither InMemoryDatabase
    // nor Sqlite work here because both provider-specific validators reject
    // `BusinessImage.Metadata: Dictionary<string,string>` (which Npgsql handles
    // natively as jsonb). Npgsql-with-fake-connection-string is the same pattern
    // used by DesignTimeDbContextFactory for `dotnet ef migrations add` — model
    // built without touching the DB.
    private const string FakeNpgsqlConnection =
        "Host=localhost;Port=5432;Database=fake;Username=fake;Password=fake";

    private static LankaEventsDbContext CreateLankaEventsContext()
    {
        var options = new DbContextOptionsBuilder<LankaEventsDbContext>()
            .UseNpgsql(FakeNpgsqlConnection)
            .Options;
        return new LankaEventsDbContext(options);
    }

    private static AppDbContext CreateAppDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(FakeNpgsqlConnection)
            .Options;

        // AppDbContext depends on IPublisher + ILogger; neither is exercised during
        // model-build. Supply null-object stubs.
        var publisher = Mock.Of<IPublisher>();
        return new AppDbContext(options, publisher, NullLogger<AppDbContext>.Instance);
    }

    // Type-only list. Expected values derived at test time from AppDbContext.Model.
    public static IEnumerable<object[]> SharedEntities()
    {
        // Aggregate roots
        yield return new object[] { typeof(Event) };
        yield return new object[] { typeof(Registration) };

        // Sign-up lists
        yield return new object[] { typeof(SignUpList) };
        yield return new object[] { typeof(SignUpItem) };
        yield return new object[] { typeof(SignUpCommitment) };

        // Ticketing
        yield return new object[] { typeof(Ticket) };
        yield return new object[] { typeof(TicketTier) };
        yield return new object[] { typeof(TicketScanLog) };
        yield return new object[] { typeof(TierAssignment) };

        // Venue seating
        yield return new object[] { typeof(VenueLayout) };
        yield return new object[] { typeof(VenueZone) };
        yield return new object[] { typeof(VenueTable) };
        yield return new object[] { typeof(VenueDecoration) };
        yield return new object[] { typeof(Seat) };
        yield return new object[] { typeof(SeatHold) };
        yield return new object[] { typeof(SeatReservation) };

        // Finance
        yield return new object[] { typeof(AddOnDefinition) };
        yield return new object[] { typeof(AddOnPurchase) };
        yield return new object[] { typeof(Donation) };
        yield return new object[] { typeof(Collection) };
        yield return new object[] { typeof(Sponsor) };
        yield return new object[] { typeof(SponsorshipPackage) };
        yield return new object[] { typeof(RegistrationAddition) };
        yield return new object[] { typeof(RegistrationPayment) };

        // Registration mode conversions
        yield return new object[] { typeof(RegistrationModeConversion) };
        yield return new object[] { typeof(RegistrationModeConversionRow) };

        // Event descendants + media
        yield return new object[] { typeof(EventTemplate) };
        yield return new object[] { typeof(MetroArea) };

        // Cross-schema entities
        yield return new object[] { typeof(EventAnalytics) };
        yield return new object[] { typeof(EventViewRecord) };

        // Junction (Products-owned)
        yield return new object[] { typeof(EventEmailGroupLink) };
    }

    /// <summary>
    /// Companion snapshot-parity test per architect ruling §6.2. Catches the case where
    /// BOTH DbContexts agree on a WRONG (schema, tableName) — the per-DbContext parity
    /// test above cannot catch this because it asserts context1 == context2. This test
    /// asserts context1 == physical-Postgres-truth (derived from the CREATION migration
    /// for each table). Rule 5i.1 (2026-07-04): tables created in the default schema
    /// (no `schema:` arg in the migration's CreateTable) must resolve to `null` schema
    /// in the DbContext model — NOT "events" via HasDefaultSchema.
    ///
    /// Curated list scoped to the entities AppDbContext.ConfigureSchemas() previously
    /// overrode PLUS the three Rule 5i.1 exceptions (ticket_tiers, TicketScanLogs,
    /// event_email_groups) whose physical schema is null. Values sourced from the
    /// creation migration for each table (grep the Migrations folder for CreateTable).
    /// </summary>
    public static IEnumerable<object?[]> PhysicallyGroundedEntities()
    {
        // Rule 5i.1 exceptions: physical schema is null (public / default connection)
        yield return new object?[] { typeof(TicketTier), null, "ticket_tiers" };
        yield return new object?[] { typeof(TicketScanLog), null, "TicketScanLogs" };
        yield return new object?[] { typeof(EventEmailGroupLink), null, "event_email_groups" };

        // Cross-schema entities (physical schema != "events")
        yield return new object?[] { typeof(EventAnalytics), "analytics", "event_analytics" };
        yield return new object?[] { typeof(EventViewRecord), "analytics", "event_view_records" };
        yield return new object?[] { typeof(EventBadge), "badges", "event_badges" };

        // Aggregate roots + core Event-family entities (physical schema = "events")
        yield return new object?[] { typeof(Event), "events", "events" };
        yield return new object?[] { typeof(Registration), "events", "registrations" };
        yield return new object?[] { typeof(Ticket), "events", "tickets" };
        yield return new object?[] { typeof(SignUpList), "events", "sign_up_lists" };
        yield return new object?[] { typeof(SignUpItem), "events", "sign_up_items" };
        yield return new object?[] { typeof(SignUpCommitment), "events", "sign_up_commitments" };
    }

    [Theory]
    [MemberData(nameof(PhysicallyGroundedEntities))]
    public void PhysicallyGroundedEntity_MatchesMigrationTruth_InAppDbContext(
        Type entityType,
        string? expectedSchema,
        string expectedTableName)
    {
        using var appCtx = CreateAppDbContext();
        var mapping = appCtx.Model.FindEntityType(entityType);
        mapping.Should().NotBeNull($"{entityType.Name} must be mapped in AppDbContext");

        mapping!.GetSchema().Should().Be(expectedSchema,
            $"Rule 5e.2 companion — physical-schema truth check for {entityType.Name}. " +
            $"AppDbContext resolved schema '{mapping!.GetSchema() ?? "(null)"}' but the CREATION " +
            $"migration puts it in '{expectedSchema ?? "(null)"}'. " +
            $"If expected is (null), the fix per Rule 5i.1 is: " +
            $".ToTable(\"{expectedTableName}\", (string?)null) in {entityType.Name}Configuration.");

        mapping!.GetTableName().Should().Be(expectedTableName,
            $"Physical table-name truth check for {entityType.Name}. " +
            $"AppDbContext resolved '{mapping!.GetTableName()}' but physical Postgres uses '{expectedTableName}'.");
    }

    /// <summary>
    /// Rule 5e.2: LankaEventsDbContext.Model produces IDENTICAL (schema, tableName)
    /// pairs to AppDbContext.Model for every shared entity. AppDbContext is the
    /// physical-schema authority (its ConfigureSchemas() overrides mirror physical
    /// Postgres; its runtime model also incorporates the moved LankaEvents configs
    /// via the runtime Assembly.Load sweep at line 201-203). If both contexts agree,
    /// LankaEventsDbContext is physical-correct by transitivity.
    /// </summary>
    [Theory]
    [MemberData(nameof(SharedEntities))]
    public void SharedEntity_HasIdenticalSchemaAndTableName_AcrossBothDbContexts(Type entityType)
    {
        using var appCtx = CreateAppDbContext();
        using var eventsCtx = CreateLankaEventsContext();

        var appMap = appCtx.Model.FindEntityType(entityType);
        var eventsMap = eventsCtx.Model.FindEntityType(entityType);

        appMap.Should().NotBeNull(
            $"{entityType.Name} must be mapped in AppDbContext (physical-schema authority per Rule 5e.2)");
        eventsMap.Should().NotBeNull(
            $"{entityType.Name} must be mapped in LankaEventsDbContext");

        eventsMap!.GetSchema().Should().Be(appMap!.GetSchema(),
            $"Rule 5e.2 physical schema parity for {entityType.Name}. " +
            $"LankaEventsDbContext resolved schema '{eventsMap!.GetSchema() ?? "(null)"}' but " +
            $"AppDbContext resolved '{appMap!.GetSchema() ?? "(null)"}'. " +
            $"Fix: adjust {entityType.Name}Configuration.ToTable(...) — if AppDbContext " +
            "resolves null, use two-arg .ToTable(\"<name>\", (string?)null) per Rule 5i.1.");

        eventsMap!.GetTableName().Should().Be(appMap!.GetTableName(),
            $"Rule 5e.3 physical table-name parity for {entityType.Name}. " +
            $"LankaEventsDbContext resolved '{eventsMap!.GetTableName()}' but " +
            $"AppDbContext resolved '{appMap!.GetTableName()}'.");
    }
}
