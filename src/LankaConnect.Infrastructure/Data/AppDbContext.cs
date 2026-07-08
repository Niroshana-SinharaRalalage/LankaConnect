using Microsoft.EntityFrameworkCore;
using LankaConnect.SharedKernel.Money;
using LankaConnect.Infrastructure.Data.Configurations;
// Wave 4.9.5 (2026-06-10): Modules.Forms.Domain.* + Modules.Media.Domain.* usings
// removed -- those types are owned by Forms/MediaDbContext after the W4.3 + W4.2
// extractions and the Wave 4.9.3/4.9.4 schema renames. AppDbContext does not
// configure or reference them.
using Microsoft.Extensions.Logging;
using LankaConnect.Modules.Identity.Domain.Entities;
using LankaConnect.Modules.Identity.Domain.Repositories;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Modules.Identity.Domain.Events;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Modules.Communications.Domain.Community;
// Business/Review/Service tables retained in schema; entity mapping removed Day 5, LankaBusiness product will re-map in Phase B (Consult #12).
using LankaConnect.Modules.Communications.Domain.Entities;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain.Analytics;
using LankaConnect.Products.LankaEvents.Domain.Badges;
using LankaConnect.SharedKernel.Cultural.ReferenceData.Entities;
using LankaConnect.Modules.Communications.Domain.Support;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Infrastructure.Data;
using LankaConnect.Infrastructure.Data.ReferenceData;
// Payments.Infrastructure PR would cycle; StripeCustomer + StripeWebhookEvent
// DbSets removed 4C.d.vii (2026-07-06) — see comment near line 141.
using LankaConnect.Infrastructure.Data.Seeders;
using MediatR;
using LankaConnect.BuildingBlocks.Application.Common;

namespace LankaConnect.Infrastructure.Data;

public class AppDbContext : DbContext, IApplicationDbContext
{
    private readonly IPublisher _publisher;
    private readonly ILogger<AppDbContext> _logger;

    // REMOVED parameterless constructor to force EF Core DI to inject IPublisher
    // This ensures domain events are properly dispatched via MediatR (Phase 6A.24)

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IPublisher publisher,
        ILogger<AppDbContext> logger) : base(options)
    {
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher),
            "IPublisher must be injected for domain event dispatching");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogInformation("AppDbContext initialized with IPublisher: {PublisherType}",
            publisher.GetType().FullName);
    }

    // Domain Entity Sets
    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<ForumTopic> ForumTopics => Set<ForumTopic>();
    public DbSet<Reply> Replies => Set<Reply>();
    public DbSet<MetroArea> MetroAreas => Set<MetroArea>();
    public DbSet<EventTemplate> EventTemplates => Set<EventTemplate>(); // Phase 6A.8

    // Phase 7F-B: registration-mode conversion audit
    public DbSet<LankaConnect.Products.LankaEvents.Domain.Entities.RegistrationModeConversion> RegistrationModeConversions
        => Set<LankaConnect.Products.LankaEvents.Domain.Entities.RegistrationModeConversion>();
    public DbSet<LankaConnect.Products.LankaEvents.Domain.Entities.RegistrationModeConversionRow> RegistrationModeConversionRows
        => Set<LankaConnect.Products.LankaEvents.Domain.Entities.RegistrationModeConversionRow>();

    // Business/Review/Service DbSets removed Day 5 per Consult #12.
    // Tables retained in schema; LankaBusiness product will re-map in Phase B.

    // Communications Entity Sets
    // Day 4 slot C sub-slice 4C.c (2026-07-06): EmailMessage, EmailTemplate,
    // UserEmailPreferences DbSets relocated to CommunicationsDbContext.
    // EmailDispatchLog stays here until a dedicated relocation pass.
    public DbSet<LankaConnect.Modules.Communications.Domain.Entities.EmailDispatchLog> EmailDispatchLogs => Set<LankaConnect.Modules.Communications.Domain.Entities.EmailDispatchLog>();
    public DbSet<NewsletterSubscriber> NewsletterSubscribers => Set<NewsletterSubscriber>();
    public DbSet<Newsletter> Newsletters => Set<Newsletter>(); // Phase 6A.74: Newsletter/News Alert Feature
    public DbSet<NewsletterEmailHistory> NewsletterEmailHistories => Set<NewsletterEmailHistory>(); // Phase 6A.74 Part 13 Issue #1: Newsletter email send history
    public DbSet<LankaConnect.Products.LankaEvents.Domain.Entities.EventNotificationHistory> EventNotificationHistories => Set<LankaConnect.Products.LankaEvents.Domain.Entities.EventNotificationHistory>(); // Phase 6A.61: Event notification history tracking
    public DbSet<EmailMetricRecord> EmailMetricRecords => Set<EmailMetricRecord>(); // Phase 6A.89: Email metrics persistence
    public DbSet<EmailFailureDetail> EmailFailureDetails => Set<EmailFailureDetail>(); // Phase 6A.99: Email failure details persistence

    // Analytics Entity Sets (Epic 2 Phase 3)
    public DbSet<EventAnalytics> EventAnalytics => Set<EventAnalytics>();
    public DbSet<EventViewRecord> EventViewRecords => Set<EventViewRecord>();

    // W4.0b (2026-06-06): Notification DbSet + EF config moved to NotificationsDbContext
    // owned by Modules.Notifications.Infrastructure. AppDbContext no longer maps the
    // notifications table; NotificationRepository injects NotificationsDbContext directly.

    // Sign-up Management Entity Sets (Phase 6A.16)
    public DbSet<SignUpList> SignUpLists => Set<SignUpList>(); // Phase 6A.16: Required for cascade deletion
    public DbSet<SignUpItem> SignUpItems => Set<SignUpItem>(); // Phase 6A.16: Required for cascade deletion
    public DbSet<SignUpCommitment> SignUpCommitments => Set<SignUpCommitment>(); // Phase 6A.16: Cascade deletion

    // Ticket Entity Sets
    public DbSet<Ticket> Tickets => Set<Ticket>(); // Phase 6A.24: Event tickets with QR codes

    // Phase 6A.148: refund approval workflow tables. RefundRequest is aggregate-internal
    // but exposed as a DbSet so the organizer queue repository can use AsNoTracking
    // projections without round-tripping through Registration.
    public DbSet<RefundRequest> RefundRequests => Set<RefundRequest>();
    public DbSet<RefundRequestLineItem> RefundRequestLineItems => Set<RefundRequestLineItem>();
    public DbSet<TicketTier> TicketTiers => Set<TicketTier>(); // Multi-tier ticketing
    public DbSet<TicketScanLog> TicketScanLogs => Set<TicketScanLog>(); // Phase 6A.141: paid-event check-in audit log
    public DbSet<TierAssignment> TierAssignments => Set<TierAssignment>(); // Slice 4 Release N: polymorphic tier→zone/table mapping

    // Venue Seating Entity Sets (Phase 2: Seat Booking)
    public DbSet<VenueLayout> VenueLayouts => Set<VenueLayout>();
    public DbSet<VenueZone> VenueZones => Set<VenueZone>();
    public DbSet<VenueTable> VenueTables => Set<VenueTable>(); // Slice 2+3A
    public DbSet<VenueDecoration> VenueDecorations => Set<VenueDecoration>(); // Slice 2+3A
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<SeatHold> SeatHolds => Set<SeatHold>();
    public DbSet<SeatReservation> SeatReservations => Set<SeatReservation>();

    // Registration Addition Entity Sets (Add-Only Attendees Feature)
    public DbSet<RegistrationAddition> RegistrationAdditions => Set<RegistrationAddition>(); // Delta payment for adding attendees
    public DbSet<RegistrationPayment> RegistrationPayments => Set<RegistrationPayment>(); // Payment audit trail

    // Donation Entity Set (Standalone Donation System)
    public DbSet<Donation> Donations => Set<Donation>(); // Event donations with Stripe payment lifecycle

    // Financial Feature Entity Sets (Collections, Sponsors, Add-ons)
    public DbSet<Collection> Collections => Set<Collection>(); // Event fund contributions with Stripe payment lifecycle
    public DbSet<Sponsor> Sponsors => Set<Sponsor>(); // Money (Stripe) or item-based sponsorships
    public DbSet<AddOnDefinition> AddOnDefinitions => Set<AddOnDefinition>(); // Organizer-defined purchasable add-on items
    public DbSet<AddOnPurchase> AddOnPurchases => Set<AddOnPurchase>(); // Add-on purchases with Stripe payment lifecycle
    public DbSet<SponsorshipPackage> SponsorshipPackages => Set<SponsorshipPackage>(); // Phase 6A.156 — organizer-defined sponsorship tiers (Gold/Silver/Bronze)

    // Badge Entity Sets (Phase 6A.25)
    public DbSet<Badge> Badges => Set<Badge>(); // Phase 6A.25: Badge Management
    public DbSet<EventBadge> EventBadges => Set<EventBadge>(); // Phase 6A.25: Event-Badge assignments

    // Wave 5.4.d.2 (2026-06-22): EmailGroup moved to Communications.Domain;
    // fully-qualified type ref keeps the DbSet on AppDbContext during the
    // transitional window (W5.4 doesn't carve out CommunicationsDbContext yet).
    public DbSet<LankaConnect.Modules.Communications.Domain.Entities.EmailGroup> EmailGroups => Set<LankaConnect.Modules.Communications.Domain.Entities.EmailGroup>(); // Phase 6A.25: Email Groups Management

    // Stripe Customer + Webhook Event DbSets removed Day 4 slot C sub-slice
    // 4C.d.vii (2026-07-06). Types live in Payments.Infrastructure.Entities;
    // LankaConnect.Infrastructure cannot PR Payments.Infrastructure (cycle —
    // Payments.Infrastructure references LankaConnect.Infrastructure per
    // W4.4.d.2 permanent edge). Physical tables (payments.stripe_customers +
    // payments.stripe_webhook_events) unchanged; ownership moves to
    // PaymentsDbContext in a follow-up wave.

    // Reference Data Entity Sets - Phase 6A.47
    public DbSet<EventCategoryRef> EventCategories => Set<EventCategoryRef>();
    public DbSet<EventStatusRef> EventStatuses => Set<EventStatusRef>();
    public DbSet<UserRoleRef> UserRoles => Set<UserRoleRef>();
    public DbSet<ReferenceValue> ReferenceValues => Set<ReferenceValue>(); // Phase 6A.47: Unified Reference Data

    // Tax Reference Data - Phase 6A.X
    public DbSet<LankaConnect.Modules.Payments.Domain.Tax.StateTaxRate> StateTaxRates => Set<LankaConnect.Modules.Payments.Domain.Tax.StateTaxRate>(); // Phase 6A.X: US State Sales Tax Rates

    // Custom Form Entity Sets (Custom Form/Survey Sign-Up Feature)
    // W4.3 (2026-06-06): Form + FormQuestion + FormResponse + FormAnswer DbSets +
    // EF configs moved to FormsDbContext owned by Modules.Forms.Infrastructure. Tables
    // physically remain on events.event_forms / events.form_questions /
    // events.form_responses / events.form_answers via cross-schema override.

    // Support Entity Sets - Phase 6A.89
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>(); // Phase 6A.89: Support/Feedback System
    public DbSet<AdminAuditLog> AdminAuditLogs => Set<AdminAuditLog>(); // Phase 6A.89: Admin Audit Logging

    // Photo Album Entity Sets (After Event Photo Album Feature)
    // W4.2 (2026-06-06): PhotoAlbum + AlbumPhoto DbSets + EF configs moved to
    // MediaDbContext owned by Modules.Media.Infrastructure. Tables physically
    // remain on events.photo_albums + events.album_photos via cross-schema
    // override per architect ruling 2026-06-06.

    // WhatsApp Entity Sets (Phase 7A: WhatsApp Integration)
    public DbSet<WhatsAppMessageRecord> WhatsAppMessageRecords => Set<WhatsAppMessageRecord>();
    public DbSet<WhatsAppTemplate> WhatsAppTemplates => Set<WhatsAppTemplate>();
    public DbSet<UserWhatsAppPreferences> UserWhatsAppPreferences => Set<UserWhatsAppPreferences>();
    public DbSet<WhatsAppWebhookEvent> WhatsAppWebhookEvents => Set<WhatsAppWebhookEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure NetTopologySuite for PostGIS support (Epic 2 Phase 1)
        // This must be called before applying configurations
        modelBuilder.HasPostgresExtension("postgis");

        // Wave 6.5.e (2026-07-03): The Event-family IEntityTypeConfiguration<T>
        // classes physically relocated to LankaConnect.Products.LankaEvents.Infrastructure.
        // AppDbContext cannot ProjectReference LankaEvents.Infrastructure (that would
        // close the Wave 5 transitional-edge cycle: LankaEvents.Infrastructure already
        // references LankaConnect.Infrastructure for AppDbContext + Repository<T>).
        // Instead, load the assembly by name at OnModelCreating time — the assembly
        // is guaranteed to be in the load closure of any host that starts AppDbContext
        // because LankaConnect.API references LankaEvents.Api → LankaEvents.Infrastructure.
        // Dual mapping is intentional and temporary per architect ruling §6.5.e — the
        // moved configs run under BOTH DbContexts (this sweep, and LankaEventsDbContext's
        // ApplyConfigurationsFromAssembly) so the 20 not-yet-cutover LankaEvents
        // repositories continue to work through the transitional edge until Wave 6.5.f.
        var lankaEventsInfrastructureAssembly = System.Reflection.Assembly.Load(
            new System.Reflection.AssemblyName("LankaConnect.Products.LankaEvents.Infrastructure"));
        modelBuilder.ApplyConfigurationsFromAssembly(lankaEventsInfrastructureAssembly);

        // 4C.e (2026-07-08): UserConfiguration relocated to
        // LankaConnect.Modules.Identity.Infrastructure.Data.Configurations per Consult
        // #14 PASS B. Same Assembly.Load pattern as LankaEvents above — the module
        // Infrastructure assembly is in the load closure because LankaConnect.API →
        // Identity.Api → Identity.Infrastructure. Dual mapping is intentional and
        // temporary; the ~18 callers touching AppDbContext.Users cut over site-by-site
        // in sub-slice 4C.e.2, at which point the explicit ApplyConfiguration line for
        // UserConfiguration can be dropped from this context.
        var identityInfrastructureAssembly = System.Reflection.Assembly.Load(
            new System.Reflection.AssemblyName("LankaConnect.Modules.Identity.Infrastructure"));
        modelBuilder.ApplyConfigurationsFromAssembly(identityInfrastructureAssembly);

        // Wave 6.5.f.5-hotfix (2026-07-04): EventEmailGroupLinkConfiguration relocated
        // to Products.LankaEvents.Infrastructure.Configurations; the ApplyConfigurationsFromAssembly
        // sweep at line 203 picks it up. The class type still needs to be in the
        // configuredEntityTypes whitelist below so the fallback Ignore-unknown pass
        // does not un-map it. Original explicit ApplyConfiguration call removed
        // per architect ruling §2.1.
        // Wave 5.4.d.1b (2026-06-22). Mirror of the EventEmailGroupLink registration
        // for the Newsletter side. Replaces the Phase 6A.74 typed M2M nav.
        modelBuilder.ApplyConfiguration(new NewsletterEmailGroupLinkConfiguration());
        // W5.2.d-hotfix2 (2026-06-28): junction CLR for Newsletter -> MetroArea M2M;
        // replaces broken _metroAreaEntities shadow nav from W5.1.
        modelBuilder.ApplyConfiguration(new NewsletterMetroAreaLinkConfiguration());

        modelBuilder.ApplyConfiguration(new ForumTopicConfiguration());
        modelBuilder.ApplyConfiguration(new ReplyConfiguration());

        // Business entity configurations removed Day 5 per Consult #12 (Phase B territory).

        // Communications entity configurations
        // Day 4 slot C sub-slice 4C.c (2026-07-06): EmailMessage/EmailTemplate/
        // UserEmailPreferences configurations relocated to CommunicationsDbContext.
        // AppDbContext no longer applies these; physical schema unchanged (communications.*).
        modelBuilder.ApplyConfiguration(new EmailDispatchLogConfiguration());
        modelBuilder.ApplyConfiguration(new NewsletterSubscriberConfiguration());
        modelBuilder.ApplyConfiguration(new NewsletterConfiguration()); // Phase 6A.74: Newsletter/News Alert Feature
        modelBuilder.ApplyConfiguration(new NewsletterEmailHistoryConfiguration()); // Phase 6A.74 Part 13 Issue #1: Newsletter email history
        // Wave 6.5.e: EventNotificationHistoryConfiguration moved to LankaEvents.Infrastructure
        // (registered above via the LankaEvents.Infrastructure assembly sweep).
        modelBuilder.ApplyConfiguration(new EmailMetricRecordConfiguration()); // Phase 6A.89: Email metrics persistence
        modelBuilder.ApplyConfiguration(new EmailFailureDetailConfiguration()); // Phase 6A.99: Email failure details persistence

        // Wave 6.5.e: EventAnalytics, EventViewRecord configurations moved to
        // LankaEvents.Infrastructure (registered above via the assembly sweep).

        // Wave 6.5.e: Ticket + TicketScanLog + TicketTier + TierAssignment configurations
        // moved to LankaEvents.Infrastructure (registered above via the assembly sweep).

        // Phase 6A.148 refund configurations are applied via the LankaEvents.Infrastructure
        // sweep above (moved in Wave 6.5.e).

        // Wave 6.5.e: Venue Seating configurations (VenueLayout, VenueZone, VenueTable,
        // VenueDecoration, Seat, SeatHold, SeatReservation) moved to LankaEvents.Infrastructure
        // (registered above via the assembly sweep).

        // Wave 6.5.e: RegistrationAddition + RegistrationPayment configurations moved to
        // LankaEvents.Infrastructure (registered above via the assembly sweep).

        // Wave 6.5.e: Donation + Collection + Sponsor + SponsorshipPackage + AddOnDefinition
        // + AddOnPurchase configurations moved to LankaEvents.Infrastructure (registered
        // above via the assembly sweep).

        // Badge entity configurations (Phase 6A.25)
        modelBuilder.ApplyConfiguration(new BadgeConfiguration());
        // Wave 6.5.f.5-hotfix (2026-07-04): EventBadgeConfiguration relocated
        // to Products.LankaEvents.Infrastructure.Configurations; the sweep at line 203
        // picks it up. Explicit ApplyConfiguration removed per architect ruling §2.2.

        // Wave 6.5.f.5-hotfix2c (2026-07-04): explicit EventBadge → Badge FK override
        // following the Rule 5i.2 pattern (shared/moved config generic; owning DbContext
        // pins module-specific relationship semantics). Hotfix1 §2.2 deleted the
        // HasOne(eb => eb.Badge) block from EventBadgeConfiguration to keep the moved
        // config free of cross-module type references — correct for LankaEventsDbContext
        // (Badge is Ignored there). Incorrect for AppDbContext: EF's RelationshipDiscovery
        // convention still saw EventBadge.Badge navigation and inferred a Cascade FK,
        // silently reversing the intentional Restrict on badges.event_badges. Physical
        // Postgres has FK_event_badges_badges_BadgeId with Restrict since the original
        // creation migration (20251211184730_AddEmailGroups.cs line 61). Restore it here
        // explicitly. AppDbContext already references Badge and EventBadge — no new
        // cross-module reference introduced.
        // Ruling: docs/architect-consults/2026-07-04-wave-6-5-f-hotfix2c-appdbcontext-drift-ruling.md
        modelBuilder.Entity<LankaConnect.Products.LankaEvents.Domain.Entities.EventBadge>()
            .HasOne(eb => eb.Badge)
            .WithMany()
            .HasForeignKey(eb => eb.BadgeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Wave 6.5.e: EventOrganizerContact + EventSlugAlias configurations moved to
        // LankaEvents.Infrastructure (registered above via the assembly sweep).

        // Email Group entity configuration (Phase 6A.25)
        modelBuilder.ApplyConfiguration(new EmailGroupConfiguration());

        // Stripe Customer + Webhook Event configurations removed 4C.d.vii
        // (2026-07-06) — Payments.Infrastructure PR cycle. Types will re-map
        // via PaymentsDbContext in follow-up wave.

        // Reference Data entity configurations (Phase 6A.47)
        modelBuilder.ApplyConfiguration(new EventCategoryRefConfiguration());
        modelBuilder.ApplyConfiguration(new EventStatusRefConfiguration());
        modelBuilder.ApplyConfiguration(new UserRoleRefConfiguration());
        modelBuilder.ApplyConfiguration(new ReferenceValueConfiguration()); // Phase 6A.47: Unified Reference Data

        // Tax entity configurations (Phase 6A.X)
        modelBuilder.ApplyConfiguration(new StateTaxRateConfiguration()); // Phase 6A.X: US State Sales Tax Rates

        // W4.3: All form configurations (Form, FormQuestion, FormResponse, FormAnswer)
        // moved to FormsDbContext (Modules.Forms.Infrastructure).

        // Support entity configurations (Phase 6A.89)
        modelBuilder.ApplyConfiguration(new SupportTicketConfiguration()); // Phase 6A.89: Support/Feedback System
        modelBuilder.ApplyConfiguration(new AdminAuditLogConfiguration()); // Phase 6A.89: Admin Audit Logging

        // Photo Album entity configurations (After Event Photo Album Feature)
        // W4.2: PhotoAlbum + AlbumPhoto configurations moved to MediaDbContext.

        // WhatsApp entity configurations (Phase 7A: WhatsApp Integration)
        modelBuilder.ApplyConfiguration(new WhatsAppMessageRecordConfiguration());
        modelBuilder.ApplyConfiguration(new WhatsAppTemplateConfiguration());
        modelBuilder.ApplyConfiguration(new UserWhatsAppPreferencesConfiguration());
        modelBuilder.ApplyConfiguration(new WhatsAppWebhookEventConfiguration());

        // Configure schemas
        ConfigureSchemas(modelBuilder);

        // Ignore unconfigured monitoring/infrastructure entities (not MVP)
        IgnoreUnconfiguredEntities(modelBuilder);

        // Configure value object conversions
        ConfigureValueObjectConversions(modelBuilder);

        // 2026-06-08 hotfix — Wave 3 LegacyBaseEntity exposes CreatedBy/UpdatedBy as
        // public IAuditable properties, which EF auto-maps and includes in every
        // SELECT. The physical DB columns do not yet exist on most AppDbContext
        // tables (Wave 4.9 Phase 1 will add them per-schema). Until then,
        // unconditionally Ignore these two properties at the AppDbContext model
        // level so EF stops emitting SELECT clauses for non-existent columns.
        // (CreatedAt + UpdatedAt remain mapped because those columns DO exist on
        // every legacy table via the snake_case `created_at`/`updated_at`
        // convention introduced by earlier migrations.)
        //
        // This Ignore is REMOVED per-entity by Phase 1.N migrations as physical
        // CreatedBy/UpdatedBy columns are added (the per-entity config then
        // re-maps the property via .HasColumnName("created_by") explicitly).
        IgnoreAuditByActorPropertiesUntilPhase1(modelBuilder);

        // Note: Seed data is applied via DbInitializer at runtime
        // due to complex value objects and owned entities
    }

    /// <summary>
    /// Walks every entity type in the model and calls
    /// <c>modelBuilder.Entity(t).Ignore("CreatedBy").Ignore("UpdatedBy")</c> if
    /// the entity implements <see cref="LankaConnect.BuildingBlocks.Domain.IAuditable"/>.
    /// Temporary backstop until Wave 4.9 Phase 1 adds the physical columns.
    /// </summary>
    private static void IgnoreAuditByActorPropertiesUntilPhase1(ModelBuilder modelBuilder)
    {
        var iauditableType = typeof(LankaConnect.BuildingBlocks.Domain.IAuditable);

        // Wave4.9.2.1 Phase 1.1 (2026-06-08): per-schema-group rollout begins
        // with identity.users. User has physical created_by/updated_by columns
        // (Phase1_1_AddCreatedByUpdatedByToIdentityUsers); the global Ignore
        // must SKIP User so UserConfiguration's HasColumnName mapping is
        // honored. Phase 1.2-1.10 will each add one type to this allowlist.
        var phase1RelaxedTypes = new HashSet<Type>
        {
            typeof(LankaConnect.Modules.Identity.Domain.Entities.User),                          // Phase 1.1 (Wave4.9.2.1, 2026-06-08): identity.users
            typeof(LankaConnect.Modules.Payments.Domain.Tax.StateTaxRate),                    // Phase 1.2 (Wave4.9.2.2, 2026-06-08): reference_data.state_tax_rates
            typeof(LankaConnect.Products.LankaEvents.Domain.Badges.Badge),                        // Phase 1.3 (Wave4.9.2.3, 2026-06-08): badges.badges
            typeof(LankaConnect.Products.LankaEvents.Domain.Entities.EventBadge),          // Phase 1.3 (Wave4.9.2.3, 2026-06-08): badges.event_badges
            // Business/Service/Review typeof entries removed Day 5 per Consult #12.
            typeof(LankaConnect.Modules.Communications.Domain.Community.ForumTopic),                // Phase 1.5 (Wave4.9.2.5, 2026-06-09): community.topics
            typeof(LankaConnect.Modules.Communications.Domain.Community.Reply),                     // Phase 1.5 (Wave4.9.2.5, 2026-06-09): community.replies
            typeof(LankaConnect.Products.LankaEvents.Domain.Analytics.EventAnalytics),            // Phase 1.6 (Wave4.9.2.6, 2026-06-09): analytics.event_analytics
            // Phase 1.7 (Wave4.9.2.7, 2026-06-09): communications email-side subset (8 entities)
            typeof(LankaConnect.Modules.Communications.Domain.Entities.EmailDispatchLog),
            typeof(LankaConnect.Modules.Communications.Domain.Entities.EmailFailureDetail),
            typeof(LankaConnect.Modules.Communications.Domain.Entities.EmailGroup), // Wave 5.4.d.2: moved to Communications.Domain
            typeof(LankaConnect.Modules.Communications.Domain.Entities.EmailMessage),
            typeof(LankaConnect.Modules.Communications.Domain.Entities.EmailMetricRecord),
            typeof(LankaConnect.Modules.Communications.Domain.Entities.EmailTemplate),
            typeof(LankaConnect.Products.LankaEvents.Domain.Entities.EventNotificationHistory),
            typeof(LankaConnect.Modules.Communications.Domain.Entities.UserEmailPreferences),
            // Phase 1.8 (Wave4.9.2.8, 2026-06-09): communications newsletter subset
            typeof(LankaConnect.Modules.Communications.Domain.Entities.Newsletter),
            typeof(LankaConnect.Modules.Communications.Domain.Entities.NewsletterEmailHistory),
            typeof(LankaConnect.Modules.Communications.Domain.Entities.NewsletterSubscriber),
            // Phase 1.9 (Wave4.9.2.9, 2026-06-09): communications whatsapp subset
            typeof(LankaConnect.Modules.Communications.Domain.Entities.UserWhatsAppPreferences),
            typeof(LankaConnect.Modules.Communications.Domain.Entities.WhatsAppMessageRecord),
            typeof(LankaConnect.Modules.Communications.Domain.Entities.WhatsAppTemplate),
            typeof(LankaConnect.Modules.Communications.Domain.Entities.WhatsAppWebhookEvent),
            // Phase 1.10a (Wave4.9.2.10a, 2026-06-09): events schema - Event aggregate proper (10 entities)
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
            // Phase 1.10b (Wave4.9.2.10b, 2026-06-09): events signups + seats + venue (10 entities)
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
            // Phase 1.10c.c (Wave4.9.2.10c.c, 2026-06-09): events.tickets (1 entity)
            typeof(LankaConnect.Products.LankaEvents.Domain.Entities.Ticket),
            // Phase 1.10d (Wave4.9.2.10d, 2026-06-09): events donations + refunds + addons (10 entities)
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

        foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
        {
            if (!iauditableType.IsAssignableFrom(entityType.ClrType))
            {
                continue;
            }
            if (phase1RelaxedTypes.Contains(entityType.ClrType))
            {
                continue;
            }

            var builder = modelBuilder.Entity(entityType.ClrType);
            if (entityType.FindProperty("CreatedBy") is not null)
            {
                builder.Ignore("CreatedBy");
            }
            if (entityType.FindProperty("UpdatedBy") is not null)
            {
                builder.Ignore("UpdatedBy");
            }
        }
    }

    private static void ConfigureSchemas(ModelBuilder modelBuilder)
    {
        // Identity schema
        modelBuilder.Entity<User>().ToTable("users", "identity");

        // Wave 6.5.f.5-hotfix2 (2026-07-04): LankaEvents entity ToTable overrides
        // relocated INTO their respective IEntityTypeConfiguration files in
        // Products/LankaEvents/LankaEvents.Infrastructure/Configurations per architect
        // ruling Option E + Rule 5i. Applied to both DbContexts via
        // ApplyConfigurationsFromAssembly (line 203). Removed from here so mapping
        // intent lives with the entity in exactly ONE place. Entities affected:
        // Event, Registration, SignUpList, SignUpItem, SignUpCommitment, MetroArea,
        // EventTemplate, EventImage, EventVideo, Ticket, TicketTier, TierAssignment,
        // RefundRequest, RefundRequestLineItem, VenueLayout, VenueZone, VenueTable,
        // VenueDecoration, Seat, SeatHold, SeatReservation, RegistrationAddition,
        // RegistrationPayment, Donation, EventBadge, EventAnalytics, EventViewRecord,
        // EventNotificationHistory (has explicit .ToTable("event_notification_history",
        // "communications") in its moved config file).

        // Community schema
        modelBuilder.Entity<ForumTopic>().ToTable("topics", "community");
        modelBuilder.Entity<Reply>().ToTable("replies", "community");

        // Business schema removed Day 5 per Consult #12 (Phase B territory).

        // Communications schema
        modelBuilder.Entity<EmailMessage>().ToTable("email_messages", "communications");
        modelBuilder.Entity<EmailTemplate>().ToTable("email_templates", "communications");
        modelBuilder.Entity<UserEmailPreferences>().ToTable("user_email_preferences", "communications");
        modelBuilder.Entity<NewsletterSubscriber>().ToTable("newsletter_subscribers", "communications");
        modelBuilder.Entity<Newsletter>().ToTable("newsletters", "communications"); // Phase 6A.74: Newsletter/News Alert Feature
        modelBuilder.Entity<NewsletterEmailHistory>().ToTable("newsletter_email_history", "communications"); // Phase 6A.74 Part 13
        modelBuilder.Entity<EmailMetricRecord>().ToTable("email_metrics", "communications"); // Phase 6A.89
        modelBuilder.Entity<EmailFailureDetail>().ToTable("email_failure_details", "communications"); // Phase 6A.99
        modelBuilder.Entity<WhatsAppMessageRecord>().ToTable("whatsapp_messages", "communications"); // Phase 7A
        modelBuilder.Entity<WhatsAppTemplate>().ToTable("whatsapp_templates", "communications"); // Phase 7A
        modelBuilder.Entity<UserWhatsAppPreferences>().ToTable("user_whatsapp_preferences", "communications"); // Phase 7A
        modelBuilder.Entity<WhatsAppWebhookEvent>().ToTable("whatsapp_webhook_events", "communications"); // Phase 7A

        // Badges schema (Phase 6A.25) — Badge stays here (cross-module principal;
        // owned by LankaConnect.Domain.Badges, mapped exclusively by AppDbContext).
        // EventBadge relocated to Products.LankaEvents (per hotfix2 §3.3.6).
        modelBuilder.Entity<Badge>().ToTable("badges", "badges");

        // Custom Form tables (events schema)
        // W4.3: Forms entity table mappings moved to FormsDbContext.

        // Tax schema (Phase 6A.X)
        // Migration 20260114170149 created in public schema, will be moved to reference_data schema
        modelBuilder.Entity<LankaConnect.Modules.Payments.Domain.Tax.StateTaxRate>().ToTable("state_tax_rates", "reference_data");
    }

    private static void IgnoreUnconfiguredEntities(ModelBuilder modelBuilder)
    {
        // Ignore all entity types from Domain that aren't explicitly configured above
        // This prevents EF Core from trying to map monitoring/infrastructure/database models
        // CRITICAL: Do NOT ignore ValueObject types - they are handled via OwnsOne/OwnsMany
        var configuredEntityTypes = new[]
        {
            typeof(User),
            typeof(Event),
            typeof(EventImage), // Epic 2 Phase 2
            typeof(EventVideo),  // Epic 2 Phase 2
            typeof(Registration),
            typeof(SignUpList), // Sign-up lists
            typeof(SignUpItem), // Sign-up items (category-based)
            typeof(SignUpCommitment), // User commitments
            typeof(MetroArea), // Phase 5C
            typeof(EventTemplate), // Phase 6A.8
            typeof(ForumTopic),
            typeof(Reply),
            // Business/Service/Review removed Day 5 per Consult #12.
            typeof(EmailMessage),
            typeof(EmailTemplate),
            typeof(LankaConnect.Modules.Communications.Domain.Entities.EmailDispatchLog), // Phase 6A.148.W5.6.B.OBS1: durable email dispatch audit log
            typeof(UserEmailPreferences),
            typeof(NewsletterSubscriber), // Phase 5
            typeof(Newsletter), // Phase 6A.74: Newsletter/News Alert Feature
            typeof(NewsletterEmailHistory), // Phase 6A.74 Part 13 Issue #1: Newsletter email send history
            typeof(EventNotificationHistory), // Phase 6A.61: Event notification history tracking
            typeof(EmailMetricRecord), // Phase 6A.89: Email metrics persistence
            typeof(EmailFailureDetail), // Phase 6A.99: Email failure details persistence
            typeof(EventAnalytics), // Epic 2 Phase 3
            typeof(EventViewRecord), // Epic 2 Phase 3
            // W4.0b: Notification removed — owned by NotificationsDbContext.
            typeof(Ticket), // Phase 6A.24
            typeof(TicketScanLog), // Phase 6A.141: paid-event check-in audit log
            typeof(RegistrationAddition), // Add-Only Attendees Feature
            typeof(RegistrationPayment), // Add-Only Attendees Feature
            typeof(Badge), // Phase 6A.25
            typeof(EventBadge), // Phase 6A.25
            typeof(LankaConnect.Modules.Communications.Domain.Entities.EmailGroup), // Phase 6A.25: Email Groups Management (Wave 5.4.d.2: moved to Communications.Domain)
            // StripeCustomer + StripeWebhookEvent typeof-refs removed 4C.d.vii; cycle avoidance.
            typeof(ReferenceValue), // Phase 6A.47: Unified Reference Data
            typeof(LankaConnect.Modules.Payments.Domain.Tax.StateTaxRate), // Phase 6A.X: US State Sales Tax Rates
            typeof(SupportTicket), // Phase 6A.89: Support/Feedback System
            typeof(AdminAuditLog), // Phase 6A.89: Admin Audit Logging
            // W4.3: Form + FormQuestion + FormResponse + FormAnswer moved to FormsDbContext.
            typeof(Donation), // Standalone Donation System
            typeof(Collection), // Event fund contributions (Financial Features)
            typeof(Sponsor), // Money/item sponsorships (Financial Features)
            typeof(AddOnDefinition), // Purchasable add-on items (Financial Features)
            typeof(AddOnPurchase), // Add-on purchases (Financial Features)
            typeof(SponsorshipPackage), // Phase 6A.156: organizer-defined sponsorship packages (Gold/Silver/Bronze)
            typeof(LankaConnect.Products.LankaEvents.Domain.Entities.EventOrganizerContact), // Multiple Organizer Contacts
            typeof(LankaConnect.Products.LankaEvents.Domain.Entities.EventSlugAlias), // Phase 6A.154: retired vanity slug aliases (permanent 301 sources)
            typeof(LankaConnect.Products.LankaEvents.Domain.Entities.EventEmailGroupLink), // Wave 5.4.c.0: explicit junction CLR type replacing the Phase 6A.32 typed M2M nav
            typeof(LankaConnect.Modules.Communications.Domain.Entities.NewsletterEmailGroupLink), // Wave 5.4.d.1b: mirror of EventEmailGroupLink for Newsletter side
            typeof(LankaConnect.Modules.Communications.Domain.Entities.NewsletterMetroAreaLink), // W5.2.d-hotfix2: junction CLR for Newsletter -> MetroArea M2M
            // W4.2: PhotoAlbum + AlbumPhoto moved to MediaDbContext.
            typeof(WhatsAppMessageRecord), // Phase 7A: WhatsApp Integration
            typeof(WhatsAppTemplate), // Phase 7A: WhatsApp Integration
            typeof(UserWhatsAppPreferences), // Phase 7A: WhatsApp Integration
            typeof(WhatsAppWebhookEvent), // Phase 7A: WhatsApp Integration
            typeof(TicketTier), // Multi-tier ticketing
            typeof(VenueLayout), // Phase 2: Seat Booking
            typeof(VenueZone), // Phase 2: Seat Booking
            typeof(VenueTable), // Slice 2+3A
            typeof(VenueDecoration), // Slice 2+3A
            typeof(Seat), // Phase 2: Seat Booking
            typeof(SeatHold), // Phase 2: Seat Booking
            typeof(SeatReservation), // Phase 2: Seat Booking
            typeof(TierAssignment), // Slice 4 Release N: polymorphic tier→zone/table mapping
            typeof(LankaConnect.Products.LankaEvents.Domain.Entities.RegistrationModeConversion), // Phase 7F-B: mode-conversion audit
            typeof(LankaConnect.Products.LankaEvents.Domain.Entities.RegistrationModeConversionRow), // Phase 7F-B: per-row audit detail
            typeof(RefundRequest), // Phase 6A.148: refund approval workflow aggregate-internal entity
            typeof(RefundRequestLineItem) // Phase 6A.148: per-bucket refund line item
            // W5.2.a-fix (2026-06-28): EventPass + PassPurchase removed from whitelist
            // -- feature deleted. See docs/architecture/W52A_TABLE_DRIFT_INVESTIGATION.md.
        };

        // Wave 5.1.a-α.3 (2026-06-27): Event aggregate family moved to Products.LankaEvents.Domain.
        // Sweep must walk that assembly too so its VOs (PassName, PassDescription, Money via
        // [NotMapped] facades, etc.) are properly identified as ValueObjects and skipped from
        // auto-discovery as entity types. Without this, EF Core 8 tries to bind them as
        // shared-type entities and fails on private ctor / missing primary key.
        var productsAssembly = typeof(LankaConnect.Products.LankaEvents.Domain.Entities.TicketTier).Assembly;
        var domainAssembly = typeof(LegacyBaseEntity).Assembly;
        var valueObjectType = typeof(ValueObject);
        var bbValueObjectType = typeof(LankaConnect.BuildingBlocks.Domain.ValueObject);

        var allDomainTypes = domainAssembly.GetTypes()
            .Concat(productsAssembly.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract);

        foreach (var type in allDomainTypes)
        {
            // Skip value objects - they are configured via OwnsOne/OwnsMany in entity configurations
            if (valueObjectType.IsAssignableFrom(type) || bbValueObjectType.IsAssignableFrom(type))
            {
                continue;
            }

            // If it's not in our configured list and EF Core hasn't explicitly configured it, ignore it
            if (!configuredEntityTypes.Contains(type))
            {
                try
                {
                    modelBuilder.Ignore(type);
                }
                catch
                {
                    // Ignore any types that can't be ignored (primitives, etc.)
                }
            }
        }
    }

    private static void ConfigureValueObjectConversions(ModelBuilder modelBuilder)
    {
        // Configure TimeZoneInfo conversion for all properties (especially in CulturalContext)
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(TimeZoneInfo))
                {
                    property.SetValueConverter(
                        new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<TimeZoneInfo, string>(
                            tz => tz.Id,
                            tzId => TimeZoneInfo.FindSystemTimeZoneById(tzId)
                        )
                    );
                }
            }
        }
    }

    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        // Phase 6A.114 DEBUG: Add stack trace to identify caller
        var stackTrace = new System.Diagnostics.StackTrace(true);
        var callerInfo = string.Join(" <- ", stackTrace.GetFrames()
            .Take(5)
            .Select(f => $"{f.GetMethod()?.DeclaringType?.Name}.{f.GetMethod()?.Name}"));

        _logger.LogInformation("[DIAG-10] AppDbContext.CommitAsync START");
        _logger.LogWarning("[DEBUG-STACK] CommitAsync called from: {CallerStack}", callerInfo);

        // DIAGNOSTIC: Log all tracked entities BEFORE DetectChanges
        // Wave3-followup.B (2026-06-28): widened from LegacyBaseEntity to BB.Entity<Guid>
        // so W3C-migrated aggregates (Event, TicketTier, EventPass-was, etc. that derive
        // from BB.Entity<Guid> directly) are also collected. Pre-fix, every domain event
        // raised on a W3C-migrated aggregate was silently swallowed.
        var trackedEntitiesBeforeDetect = ChangeTracker.Entries<LankaConnect.BuildingBlocks.Domain.Entity<Guid>>().ToList();
        _logger.LogInformation(
            "[DIAG-11] Tracked BB.Entity<Guid> count BEFORE DetectChanges: {Count}",
            trackedEntitiesBeforeDetect.Count);

        foreach (var entry in trackedEntitiesBeforeDetect)
        {
            _logger.LogInformation(
                "[DIAG-12] Entity BEFORE DetectChanges - Type: {EntityType}, Id: {EntityId}, State: {State}, DomainEvents: {DomainEventCount}",
                entry.Entity.GetType().Name,
                entry.Entity.Id,
                entry.State,
                entry.Entity.DomainEvents.Count);
        }

        // W3G (2026-06-06): IAuditable + AuditableInterceptor handle CreatedAt/UpdatedAt
        // automatically — the old manual MarkAsUpdated() sweep is gone. Interceptor runs
        // before SaveChangesAsync below; nothing to do here.

        // CRITICAL FIX Phase 6A.24: Force change detection BEFORE collecting domain events
        // Without this, ChangeTracker.Entries<LankaConnect.BuildingBlocks.Domain.Entity<Guid>>() returns empty collection
        // because EF Core only auto-detects changes DURING SaveChangesAsync()
        ChangeTracker.DetectChanges();

        // DIAGNOSTIC: Log all tracked entities AFTER DetectChanges
        var trackedEntitiesAfterDetect = ChangeTracker.Entries<LankaConnect.BuildingBlocks.Domain.Entity<Guid>>().ToList();
        _logger.LogInformation(
            "[DIAG-13] Tracked LegacyBaseEntity count AFTER DetectChanges: {Count}",
            trackedEntitiesAfterDetect.Count);

        foreach (var entry in trackedEntitiesAfterDetect)
        {
            _logger.LogInformation(
                "[DIAG-14] Entity AFTER DetectChanges - Type: {EntityType}, Id: {EntityId}, State: {State}, DomainEvents: {DomainEventCount}, EventTypes: [{EventTypes}]",
                entry.Entity.GetType().Name,
                entry.Entity.Id,
                entry.State,
                entry.Entity.DomainEvents.Count,
                string.Join(", ", entry.Entity.DomainEvents.Select(e => e.GetType().Name)));
        }

        // Collect domain events before saving
        var domainEvents = ChangeTracker.Entries<LankaConnect.BuildingBlocks.Domain.Entity<Guid>>()
            .Where(e => e.Entity.DomainEvents.Any())
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        _logger.LogInformation(
            "[DIAG-15] Domain events collected: {Count}, Types: [{EventTypes}]",
            domainEvents.Count,
            string.Join(", ", domainEvents.Select(e => e.GetType().Name)));

        if (domainEvents.Any())
        {
            _logger.LogInformation(
                "[Phase 6A.24] Found {Count} domain events to dispatch: {EventTypes}",
                domainEvents.Count,
                string.Join(", ", domainEvents.Select(e => e.GetType().Name)));
        }

        // Save changes to database
        var result = await SaveChangesAsync(cancellationToken);
        _logger.LogInformation("[DIAG-16] SaveChangesAsync completed, {Count} entities saved", result);

        // Issue #56 FIX: Clear domain events IMMEDIATELY after save, BEFORE dispatching
        // This prevents nested CommitAsync calls (e.g., from TicketService during PaymentCompletedEventHandler)
        // from re-collecting and re-dispatching the same domain events, which caused duplicate emails.
        // The domain events are already captured in the local 'domainEvents' list, so clearing them
        // from the entities is safe and necessary to prevent double dispatch.
        foreach (var entry in ChangeTracker.Entries<LankaConnect.BuildingBlocks.Domain.Entity<Guid>>())
        {
            entry.Entity.ClearDomainEvents();
        }
        _logger.LogInformation("[Issue #56] Domain events cleared from entities to prevent double dispatch");

        // Dispatch domain events after successful save
        if (domainEvents.Any())
        {
            _logger.LogInformation("[Phase 6A.24] Dispatching {Count} domain events via MediatR", domainEvents.Count);

            foreach (var domainEvent in domainEvents)
            {
                var eventType = domainEvent.GetType();
                _logger.LogInformation("[DIAG-17] About to dispatch domain event: {EventType}", eventType.Name);

                var notificationType = typeof(DomainEventNotification<>).MakeGenericType(eventType);
                var notification = Activator.CreateInstance(notificationType, domainEvent);

                if (notification != null)
                {
                    _logger.LogInformation("[DIAG-18] Publishing notification for: {EventType}", eventType.Name);

                    // Phase 6A.52: Wrap MediatR.Publish in try-catch to prevent handler exceptions from bubbling up
                    // This ensures one handler's failure doesn't prevent other handlers from executing
                    try
                    {
                        await _publisher.Publish(notification, cancellationToken);
                        _logger.LogInformation("[Phase 6A.24] Successfully dispatched domain event: {EventType}", eventType.Name);
                    }
                    catch (Exception handlerException)
                    {
                        // Phase 6A.52: Log handler exceptions but don't re-throw
                        // This prevents handler failures from causing transaction rollback
                        _logger.LogError(handlerException,
                            "[Phase 6A.52] [HANDLER-EXCEPTION] Domain event handler failed - EventType: {EventType}, ExceptionType: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}",
                            eventType.Name, handlerException.GetType().FullName, handlerException.Message, handlerException.StackTrace);
                    }
                }
                else
                {
                    _logger.LogWarning("[Phase 6A.24] Failed to create notification for domain event: {EventType}", eventType.Name);
                }
            }

            _logger.LogInformation("[Phase 6A.24] Successfully dispatched all {Count} domain events", domainEvents.Count);
        }
        else
        {
            _logger.LogInformation("[DIAG-19] No domain events to dispatch - this may indicate an issue!");
        }

        _logger.LogInformation("[DIAG-20] AppDbContext.CommitAsync COMPLETE");
        return result;
    }

    /// <summary>
    /// Phase 6A.114 DEBUG: Override SaveChangesAsync to detect direct calls that bypass CommitAsync
    /// This should ONLY be called from CommitAsync - any other caller indicates a bug
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var stackTrace = new System.Diagnostics.StackTrace(true);
        var callerFrames = stackTrace.GetFrames().Take(10).ToList();
        var callerInfo = string.Join(" <- ", callerFrames
            .Select(f => $"{f.GetMethod()?.DeclaringType?.Name}.{f.GetMethod()?.Name}"));

        // Check if called from CommitAsync
        var isFromCommitAsync = callerFrames.Any(f => f.GetMethod()?.Name == "CommitAsync");

        if (!isFromCommitAsync)
        {
            _logger.LogError(
                "[DEBUG-BYPASS] ⚠️ SaveChangesAsync called DIRECTLY (not from CommitAsync)! " +
                "This bypasses domain event dispatch! Call stack: {CallerStack}",
                callerInfo);
        }
        else
        {
            _logger.LogInformation("[DEBUG-SAVECHANGES] SaveChangesAsync called from CommitAsync (correct flow)");
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
