using LankaConnect.BuildingBlocks.Infrastructure.Idempotency;
using LankaConnect.BuildingBlocks.Infrastructure.Outbox;
using LankaConnect.Products.LankaEvents.Domain.Analytics;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace LankaConnect.Products.LankaEvents.Infrastructure.Data;

/// <summary>
/// Product-owned <see cref="DbContext"/> for the LankaEvents bounded context.
/// Maps the Event aggregate family + operational tables in the <c>events</c>
/// schema. Wave 6.5.e (2026-07-03) prerequisite for the Wave 6.5.f 20-repo
/// cutover — this context is REGISTERED in DI but no repository has been
/// switched to it yet; every LankaEvents repository still injects
/// <c>AppDbContext</c> via the transitional edge from
/// <c>LankaConnect.Products.LankaEvents.Infrastructure.csproj</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Dual mapping (intentional and temporary)</b>: The Event-family entities
/// mapped below are ALSO mapped by <c>AppDbContext</c> — the 22+
/// <see cref="Microsoft.EntityFrameworkCore.IEntityTypeConfiguration{TEntity}"/>
/// classes physically relocated to
/// <c>LankaConnect.Products.LankaEvents.Infrastructure.Configurations</c>
/// remain applied by both DbContexts (AppDbContext via explicit
/// <c>modelBuilder.ApplyConfiguration(new XxxConfiguration())</c> lines, this
/// context via <see cref="ModelBuilder.ApplyConfigurationsFromAssembly"/>).
/// Both DbContexts thus produce the same model shape for the Event family,
/// preventing runtime schema drift while the 20 repositories continue to work
/// unchanged through the transitional edge. Cutover of each repository
/// happens in Wave 6.5.f, at which point the corresponding
/// <c>ApplyConfiguration</c> line in <c>AppDbContext.OnModelCreating</c> can
/// be removed one at a time.
/// </para>
/// <para>
/// <b>Default schema</b>: <c>events</c>. Every Event-family business table
/// physically lives in the <c>events</c> schema (established by earlier Phase
/// 6A migrations). A small handful of entities cross-map into
/// <c>communications</c> (EventNotificationHistory) or <c>analytics</c>
/// (EventAnalytics / EventViewRecord) — the moved config files retain their
/// existing explicit <c>.ToTable(name, "schema")</c> overrides for those
/// exceptions, matching the same overrides that AppDbContext.ConfigureSchemas
/// applies.
/// </para>
/// <para>
/// <b>Operational tables</b>: <c>events.outbox</c>,
/// <c>events.outbox_dead_letter</c>, <c>events.idempotency_keys</c>. Created
/// by migration <c>Wave6_5_e_AddLankaEventsOperationalTables</c>. Consumed by
/// the Wave 6.5 outbox pipeline (<c>AddModuleOutbox&lt;LankaEventsDbContext&gt;()</c>
/// registers the producer + hosted <c>OutboxProcessor</c>).
/// </para>
/// <para>
/// <b>Inheritance</b>: derives directly from <c>DbContext</c> rather than
/// <c>BuildingBlocks.Infrastructure.Persistence.BaseDbContext</c> — the
/// Wave 3 <c>AuditableInterceptor</c> already covers <c>CreatedAt</c> /
/// <c>UpdatedAt</c> for every <see cref="BuildingBlocks.Domain.IAuditable"/>
/// aggregate through the AppDbContext-registered interceptor chain, and
/// this context is dormant with respect to writes until Wave 6.5.f.
/// </para>
/// </remarks>
public sealed class LankaEventsDbContext : DbContext
{
    /// <summary>Postgres schema all Event-family tables map into by default.</summary>
    public const string SchemaName = "events";

    public LankaEventsDbContext(DbContextOptions<LankaEventsDbContext> options)
        : base(options)
    {
    }

    // Event aggregate root + Event-family child aggregates
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<EventTemplate> EventTemplates => Set<EventTemplate>();
    public DbSet<MetroArea> MetroAreas => Set<MetroArea>();

    // Registration audit trails
    public DbSet<RegistrationModeConversion> RegistrationModeConversions => Set<RegistrationModeConversion>();
    public DbSet<RegistrationModeConversionRow> RegistrationModeConversionRows => Set<RegistrationModeConversionRow>();
    public DbSet<RegistrationAddition> RegistrationAdditions => Set<RegistrationAddition>();
    public DbSet<RegistrationPayment> RegistrationPayments => Set<RegistrationPayment>();

    // Sign-up lists (event-scoped)
    public DbSet<SignUpList> SignUpLists => Set<SignUpList>();
    public DbSet<SignUpItem> SignUpItems => Set<SignUpItem>();
    public DbSet<SignUpCommitment> SignUpCommitments => Set<SignUpCommitment>();

    // Ticketing
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketTier> TicketTiers => Set<TicketTier>();
    public DbSet<TicketScanLog> TicketScanLogs => Set<TicketScanLog>();
    public DbSet<TierAssignment> TierAssignments => Set<TierAssignment>();

    // Venue seating
    public DbSet<VenueLayout> VenueLayouts => Set<VenueLayout>();
    public DbSet<VenueZone> VenueZones => Set<VenueZone>();
    public DbSet<VenueTable> VenueTables => Set<VenueTable>();
    public DbSet<VenueDecoration> VenueDecorations => Set<VenueDecoration>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<SeatHold> SeatHolds => Set<SeatHold>();
    public DbSet<SeatReservation> SeatReservations => Set<SeatReservation>();

    // Consult #20 (2026-07-10): Badge + EventBadge relocated from AppDbContext.
    // Physical schema unchanged (badges.badges + badges.event_badges).
    public DbSet<LankaConnect.Products.LankaEvents.Domain.Badges.Badge> Badges => Set<LankaConnect.Products.LankaEvents.Domain.Badges.Badge>();
    public DbSet<LankaConnect.Products.LankaEvents.Domain.Entities.EventBadge> EventBadges => Set<LankaConnect.Products.LankaEvents.Domain.Entities.EventBadge>();

    // Financial sub-aggregates
    public DbSet<Donation> Donations => Set<Donation>();
    public DbSet<Collection> Collections => Set<Collection>();
    public DbSet<Sponsor> Sponsors => Set<Sponsor>();
    public DbSet<SponsorshipPackage> SponsorshipPackages => Set<SponsorshipPackage>();
    public DbSet<AddOnDefinition> AddOnDefinitions => Set<AddOnDefinition>();
    public DbSet<AddOnPurchase> AddOnPurchases => Set<AddOnPurchase>();

    // Refund workflow (Registration aggregate children)
    public DbSet<RefundRequest> RefundRequests => Set<RefundRequest>();
    public DbSet<RefundRequestLineItem> RefundRequestLineItems => Set<RefundRequestLineItem>();

    // Event notifications (cross-schema — physically in communications schema)
    public DbSet<EventNotificationHistory> EventNotificationHistories => Set<EventNotificationHistory>();

    // Analytics (cross-schema — physically in analytics schema)
    public DbSet<EventAnalytics> EventAnalytics => Set<EventAnalytics>();
    public DbSet<EventViewRecord> EventViewRecords => Set<EventViewRecord>();

    /// <summary>Per-module outbox table (<c>events.outbox</c>).</summary>
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();

    /// <summary>Per-module dead-letter table (<c>events.outbox_dead_letter</c>).</summary>
    public DbSet<DeadLetterMessage> OutboxDeadLetter => Set<DeadLetterMessage>();

    /// <summary>Per-module idempotency-keys table (<c>events.idempotency_keys</c>).</summary>
    public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // Wave 6.5.f.5-hotfix2b (2026-07-04, Rule 5i.1 REVISED + Rule 5i.2 per fourth
        // architect consult): HasDefaultSchema("events") REMOVED. It's a model-level
        // annotation applied at ModelFinalizingConvention time and cannot be overridden
        // per-entity in EF Core 8 — it overwrote `.ToTable(name, null)` and
        // `SetSchema(null)` for the three public-schema entities (TicketTier,
        // TicketScanLog, EventEmailGroupLink) whose physical table lives in `public`.
        // Aligns LankaEventsDbContext with the AppDbContext.ConfigureSchemas pattern
        // (per-entity two-arg ToTable, no HasDefaultSchema). All 22 non-exception
        // configs already carry explicit .ToTable(name, "<schema>") per Rule 5i (see
        // hotfix2 commit f25003a1). The three exception configs use single-arg
        // .ToTable(name) — resolves to null schema, matching physical.
        // Ruling: docs/architect-consults/2026-07-04-wave-6-5-f-hotfix2b-hasdefaultschema-override-ruling.md.

        // Apply every IEntityTypeConfiguration<T> in the LankaEvents.Infrastructure
        // assembly. Wave 6.5.e physically relocated ~36 config classes.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LankaEventsDbContext).Assembly);

        // Module-owned operational tables. Rule 5i.2: shared BuildingBlocks configs
        // (OutboxMessageConfiguration, DeadLetterMessageConfiguration,
        // IdempotencyKeyConfiguration) use single-arg .ToTable(name) inside the config
        // for cross-module reuse. The OWNING per-module DbContext pins the schema
        // explicitly with modelBuilder.Entity<T>().ToTable(name, moduleSchema)
        // immediately AFTER ApplyConfiguration.
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new DeadLetterMessageConfiguration());
        modelBuilder.ApplyConfiguration(new IdempotencyKeyConfiguration());
        modelBuilder.Entity<OutboxMessage>().ToTable("outbox", SchemaName);
        modelBuilder.Entity<DeadLetterMessage>().ToTable("outbox_dead_letter", SchemaName);
        modelBuilder.Entity<IdempotencyKey>().ToTable("idempotency_keys", SchemaName);

        // Cross-module reference cleanup. Several moved configs
        // (EventNotificationHistory, Ticket, TicketScanLog) declare
        // `HasOne<User>()` unconfigured principal navigations to establish
        // the FK column. When run under LankaEventsDbContext (which does NOT
        // own the Identity module), EF Core 8 pulls in User + all of its
        // properties (Email VO, `List<RefreshToken>`, etc.) and fails to map
        // them — those types are owned elsewhere. AppDbContext already maps
        // User via UserConfiguration; here we tell EF to keep the FK column
        // as a scalar and ignore the principal entity + its owned graph. The
        // Ignore calls MUST run AFTER ApplyConfigurationsFromAssembly — EF
        // Core 8 respects the last mapping directive per type, so any earlier
        // HasOne<User>() from the moved configs is neutralised here.
        modelBuilder.Ignore<LankaConnect.Modules.Identity.Domain.Entities.User>();
        modelBuilder.Ignore<LankaConnect.Modules.Identity.Domain.ValueObjects.RefreshToken>();

        // Wave 6.5.f.5-hotfix (2026-07-04): EventEmailGroupLink + EventBadge are
        // Products-owned junction entities; their configs were physically relocated
        // to Products/LankaEvents/LankaEvents.Infrastructure/Configurations in the
        // same commit and are now picked up by ApplyConfigurationsFromAssembly at
        // line 146. Removing the two Ignore<>() calls that were installed by 6.5.e
        // — they collided with EventConfiguration.HasMany(e => e.EmailGroupLinks)
        // and HasMany(e => e.Badges) and produced 46 API smoke failures on staging
        // before the hotfix. Architect ruling docs/architect-consults/2026-07-04-*.
        //
        // The far-principal Communications.EmailGroup and Badges.Badge STAY Ignored —
        // those are cross-module principals owned by other bounded contexts. Junction
        // FKs (EmailGroupId, BadgeId) remain scalar. Cross-module hydration happens
        // at the application layer via IEmailGroupRepository / IBadgeRepository per
        // Blueprint §7.8.
        modelBuilder.Ignore<LankaConnect.Modules.Communications.Domain.Entities.EmailGroup>();
        modelBuilder.Ignore<LankaConnect.Products.LankaEvents.Domain.Badges.Badge>();

        base.OnModelCreating(modelBuilder);
    }
}
