using Microsoft.EntityFrameworkCore;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;
using Microsoft.Extensions.Logging;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Events;
using LankaConnect.Modules.Media.Domain;
using LankaConnect.Modules.Media.Domain.Entities;
using LankaConnect.Modules.Media.Domain.Enums;
using LankaConnect.Modules.Media.Domain.DomainEvents;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Community;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Analytics;
using LankaConnect.Domain.Badges;
using LankaConnect.Domain.ReferenceData.Entities;
using LankaConnect.Domain.Support;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Infrastructure.Data.Configurations;
using LankaConnect.Infrastructure.Data.Configurations.ReferenceData;
using LankaConnect.Infrastructure.Payments.Entities;
using LankaConnect.Infrastructure.Payments.Configurations;
using LankaConnect.Infrastructure.Data.Seeders;
using MediatR;
using LankaConnect.Application.Common;

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
    public DbSet<LankaConnect.Domain.Events.Entities.RegistrationModeConversion> RegistrationModeConversions
        => Set<LankaConnect.Domain.Events.Entities.RegistrationModeConversion>();
    public DbSet<LankaConnect.Domain.Events.Entities.RegistrationModeConversionRow> RegistrationModeConversionRows
        => Set<LankaConnect.Domain.Events.Entities.RegistrationModeConversionRow>();

    // Business Entity Sets
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Review> Reviews => Set<Review>();
    
    // Communications Entity Sets
    public DbSet<LankaConnect.Domain.Communications.Entities.EmailMessage> EmailMessages => Set<LankaConnect.Domain.Communications.Entities.EmailMessage>();
    public DbSet<LankaConnect.Domain.Communications.Entities.EmailTemplate> EmailTemplates => Set<LankaConnect.Domain.Communications.Entities.EmailTemplate>();
    // Phase 6A.148.W5.6.B.OBS1 — durable email dispatch audit log (operator post-mortem
    // capability without screenshots; queryable by refund_request_id / recipient / template).
    public DbSet<LankaConnect.Domain.Communications.Entities.EmailDispatchLog> EmailDispatchLogs => Set<LankaConnect.Domain.Communications.Entities.EmailDispatchLog>();
    public DbSet<LankaConnect.Domain.Communications.Entities.UserEmailPreferences> UserEmailPreferences => Set<LankaConnect.Domain.Communications.Entities.UserEmailPreferences>();
    public DbSet<NewsletterSubscriber> NewsletterSubscribers => Set<NewsletterSubscriber>();
    public DbSet<Newsletter> Newsletters => Set<Newsletter>(); // Phase 6A.74: Newsletter/News Alert Feature
    public DbSet<NewsletterEmailHistory> NewsletterEmailHistories => Set<NewsletterEmailHistory>(); // Phase 6A.74 Part 13 Issue #1: Newsletter email send history
    public DbSet<LankaConnect.Domain.Events.Entities.EventNotificationHistory> EventNotificationHistories => Set<LankaConnect.Domain.Events.Entities.EventNotificationHistory>(); // Phase 6A.61: Event notification history tracking
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

    // Email Group Entity Set (Phase 6A.25)
    public DbSet<EmailGroup> EmailGroups => Set<EmailGroup>(); // Phase 6A.25: Email Groups Management

    // Stripe Customer Entity Set (Phase 6A.4)
    public DbSet<StripeCustomer> StripeCustomers => Set<StripeCustomer>(); // Phase 6A.4: Stripe Payment Integration

    // Stripe Webhook Event Entity Set (Phase 6A.24)
    public DbSet<LankaConnect.Infrastructure.Payments.Entities.StripeWebhookEvent> StripeWebhookEvents => Set<LankaConnect.Infrastructure.Payments.Entities.StripeWebhookEvent>(); // Phase 6A.24: Webhook idempotency tracking

    // Reference Data Entity Sets - Phase 6A.47
    public DbSet<EventCategoryRef> EventCategories => Set<EventCategoryRef>();
    public DbSet<EventStatusRef> EventStatuses => Set<EventStatusRef>();
    public DbSet<UserRoleRef> UserRoles => Set<UserRoleRef>();
    public DbSet<ReferenceValue> ReferenceValues => Set<ReferenceValue>(); // Phase 6A.47: Unified Reference Data

    // Tax Reference Data - Phase 6A.X
    public DbSet<LankaConnect.Domain.Tax.StateTaxRate> StateTaxRates => Set<LankaConnect.Domain.Tax.StateTaxRate>(); // Phase 6A.X: US State Sales Tax Rates

    // Custom Form Entity Sets (Custom Form/Survey Sign-Up Feature)
    // W4.3 (2026-06-06): EventForm + FormQuestion + FormResponse + FormAnswer DbSets +
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

        // Apply entity configurations
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new TicketTierConfiguration()); // Multi-tier ticketing (must be before EventConfiguration to avoid shared-type Money conflict)
        modelBuilder.ApplyConfiguration(new EventConfiguration());
        // Phase 6A.154: EventSlugAliasConfiguration — order-independent.
        // EventConfiguration declares HasMany(e => e.SlugAliases) as a scalar
        // VanitySlug column (NOT OwnsOne), so EF Core 8 doesn't shadow-map
        // the child during owned-entity discovery. Mirrors EventOrganizerContact
        // (registered later at line 271 with no ordering issues).
        modelBuilder.ApplyConfiguration(new EventSlugAliasConfiguration());
        modelBuilder.ApplyConfiguration(new EventImageConfiguration()); // Epic 2 Phase 2
        modelBuilder.ApplyConfiguration(new EventVideoConfiguration()); // Epic 2 Phase 2

        // Phase 6A.148: Refund approval workflow — MUST apply BEFORE RegistrationConfiguration
        // (which calls HasMany(r => r.RefundRequests)). If RegistrationConfiguration runs first,
        // EF auto-derives RefundRequest as a dependent entity and then ignores the explicit
        // configuration when it's applied later ("first mapped explicitly and then ignored").
        modelBuilder.ApplyConfiguration(new RefundRequestConfiguration());
        modelBuilder.ApplyConfiguration(new RefundRequestLineItemConfiguration());

        modelBuilder.ApplyConfiguration(new RegistrationConfiguration());
        // Phase 7F-B: registration-mode conversion audit (architect-approved 2026-04-30)
        modelBuilder.ApplyConfiguration(new RegistrationModeConversionConfiguration());
        modelBuilder.ApplyConfiguration(new RegistrationModeConversionRowConfiguration());
        modelBuilder.ApplyConfiguration(new SignUpListConfiguration()); // Sign-up lists
        modelBuilder.ApplyConfiguration(new SignUpItemConfiguration()); // Sign-up items (category-based)
        modelBuilder.ApplyConfiguration(new SignUpCommitmentConfiguration()); // User commitments
        modelBuilder.ApplyConfiguration(new ForumTopicConfiguration());
        modelBuilder.ApplyConfiguration(new ReplyConfiguration());
        modelBuilder.ApplyConfiguration(new MetroAreaConfiguration()); // Phase 5
        modelBuilder.ApplyConfiguration(new EventTemplateConfiguration()); // Phase 6A.8

        // Business entity configurations
        modelBuilder.ApplyConfiguration(new BusinessConfiguration());
        modelBuilder.ApplyConfiguration(new ServiceConfiguration());
        modelBuilder.ApplyConfiguration(new ReviewConfiguration());

        // Communications entity configurations
        modelBuilder.ApplyConfiguration(new EmailMessageConfiguration());
        modelBuilder.ApplyConfiguration(new EmailTemplateConfiguration());
        modelBuilder.ApplyConfiguration(new EmailDispatchLogConfiguration());
        modelBuilder.ApplyConfiguration(new UserEmailPreferencesConfiguration());
        modelBuilder.ApplyConfiguration(new NewsletterSubscriberConfiguration());
        modelBuilder.ApplyConfiguration(new NewsletterConfiguration()); // Phase 6A.74: Newsletter/News Alert Feature
        modelBuilder.ApplyConfiguration(new NewsletterEmailHistoryConfiguration()); // Phase 6A.74 Part 13 Issue #1: Newsletter email history
        modelBuilder.ApplyConfiguration(new EventNotificationHistoryConfiguration()); // Phase 6A.61: Event notification history tracking
        modelBuilder.ApplyConfiguration(new EmailMetricRecordConfiguration()); // Phase 6A.89: Email metrics persistence
        modelBuilder.ApplyConfiguration(new EmailFailureDetailConfiguration()); // Phase 6A.99: Email failure details persistence

        // Analytics entity configurations (Epic 2 Phase 3)
        modelBuilder.ApplyConfiguration(new EventAnalyticsConfiguration());
        modelBuilder.ApplyConfiguration(new EventViewRecordConfiguration());

        // Ticket entity configuration (Phase 6A.24)
        modelBuilder.ApplyConfiguration(new TicketConfiguration());

        // Phase 6A.141: Ticket scan audit log
        modelBuilder.ApplyConfiguration(new TicketScanLogConfiguration());

        // Phase 6A.148 refund configurations are applied earlier (before RegistrationConfiguration)
        // to avoid the "first mapped explicitly and then ignored" trap.

        // Venue Seating entity configurations (Phase 2: Seat Booking + Slice 2+3A expansion)
        modelBuilder.ApplyConfiguration(new VenueLayoutConfiguration());
        modelBuilder.ApplyConfiguration(new VenueZoneConfiguration());
        modelBuilder.ApplyConfiguration(new VenueTableConfiguration()); // Slice 2+3A
        modelBuilder.ApplyConfiguration(new VenueDecorationConfiguration()); // Slice 2+3A
        modelBuilder.ApplyConfiguration(new SeatConfiguration());
        modelBuilder.ApplyConfiguration(new SeatHoldConfiguration());
        modelBuilder.ApplyConfiguration(new SeatReservationConfiguration());
        modelBuilder.ApplyConfiguration(new TierAssignmentConfiguration()); // Slice 4 Release N

        // Registration Addition entity configurations (Add-Only Attendees Feature)
        modelBuilder.ApplyConfiguration(new RegistrationAdditionConfiguration());
        modelBuilder.ApplyConfiguration(new RegistrationPaymentConfiguration());

        // Donation entity configuration (Standalone Donation System)
        modelBuilder.ApplyConfiguration(new DonationEntityConfiguration());

        // Financial Feature configurations (Collections, Sponsors, Add-ons)
        modelBuilder.ApplyConfiguration(new CollectionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SponsorEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AddOnDefinitionEntityConfiguration());
        modelBuilder.ApplyConfiguration(new AddOnPurchaseEntityConfiguration());
        modelBuilder.ApplyConfiguration(new SponsorshipPackageEntityConfiguration()); // Phase 6A.156: organizer-defined sponsorship packages

        // Badge entity configurations (Phase 6A.25)
        modelBuilder.ApplyConfiguration(new BadgeConfiguration());
        modelBuilder.ApplyConfiguration(new EventBadgeConfiguration());

        // Organizer Contact entity configuration (Multiple Organizer Contacts)
        modelBuilder.ApplyConfiguration(new EventOrganizerContactConfiguration());

        // Email Group entity configuration (Phase 6A.25)
        modelBuilder.ApplyConfiguration(new EmailGroupConfiguration());

        // Stripe Customer configuration (Phase 6A.4)
        modelBuilder.ApplyConfiguration(new StripeCustomerConfiguration());

        // Stripe Webhook Event configuration (Phase 6A.24)
        modelBuilder.ApplyConfiguration(new StripeWebhookEventConfiguration());

        // Reference Data entity configurations (Phase 6A.47)
        modelBuilder.ApplyConfiguration(new EventCategoryRefConfiguration());
        modelBuilder.ApplyConfiguration(new EventStatusRefConfiguration());
        modelBuilder.ApplyConfiguration(new UserRoleRefConfiguration());
        modelBuilder.ApplyConfiguration(new ReferenceValueConfiguration()); // Phase 6A.47: Unified Reference Data

        // Tax entity configurations (Phase 6A.X)
        modelBuilder.ApplyConfiguration(new StateTaxRateConfiguration()); // Phase 6A.X: US State Sales Tax Rates

        // W4.3: All form configurations (EventForm, FormQuestion, FormResponse, FormAnswer)
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
            typeof(LankaConnect.Domain.Users.User),                          // Phase 1.1 (Wave4.9.2.1, 2026-06-08): identity.users
            typeof(LankaConnect.Domain.Tax.StateTaxRate),                    // Phase 1.2 (Wave4.9.2.2, 2026-06-08): reference_data.state_tax_rates
            typeof(LankaConnect.Domain.Badges.Badge),                        // Phase 1.3 (Wave4.9.2.3, 2026-06-08): badges.badges
            typeof(LankaConnect.Domain.Events.Entities.EventBadge),          // Phase 1.3 (Wave4.9.2.3, 2026-06-08): badges.event_badges
            typeof(LankaConnect.Domain.Business.Business),                   // Phase 1.4 (Wave4.9.2.4, 2026-06-08): business.businesses
            typeof(LankaConnect.Domain.Business.Service),                    // Phase 1.4 (Wave4.9.2.4, 2026-06-08): business.services
            typeof(LankaConnect.Domain.Business.Review),                     // Phase 1.4 (Wave4.9.2.4, 2026-06-08): business.reviews
            typeof(LankaConnect.Domain.Community.ForumTopic),                // Phase 1.5 (Wave4.9.2.5, 2026-06-09): community.topics
            typeof(LankaConnect.Domain.Community.Reply),                     // Phase 1.5 (Wave4.9.2.5, 2026-06-09): community.replies
            typeof(LankaConnect.Domain.Analytics.EventAnalytics),            // Phase 1.6 (Wave4.9.2.6, 2026-06-09): analytics.event_analytics
            // Phase 1.7 (Wave4.9.2.7, 2026-06-09): communications email-side subset (8 entities)
            typeof(LankaConnect.Domain.Communications.Entities.EmailDispatchLog),
            typeof(LankaConnect.Domain.Communications.Entities.EmailFailureDetail),
            typeof(LankaConnect.Domain.Communications.Entities.EmailGroup),
            typeof(LankaConnect.Domain.Communications.Entities.EmailMessage),
            typeof(LankaConnect.Domain.Communications.Entities.EmailMetricRecord),
            typeof(LankaConnect.Domain.Communications.Entities.EmailTemplate),
            typeof(LankaConnect.Domain.Events.Entities.EventNotificationHistory),
            typeof(LankaConnect.Domain.Communications.Entities.UserEmailPreferences),
            // Phase 1.8 (Wave4.9.2.8, 2026-06-09): communications newsletter subset
            typeof(LankaConnect.Domain.Communications.Entities.Newsletter),
            typeof(LankaConnect.Domain.Communications.Entities.NewsletterEmailHistory),
            typeof(LankaConnect.Domain.Communications.Entities.NewsletterSubscriber),
            // Phase 1.9 (Wave4.9.2.9, 2026-06-09): communications whatsapp subset
            typeof(LankaConnect.Domain.Communications.Entities.UserWhatsAppPreferences),
            typeof(LankaConnect.Domain.Communications.Entities.WhatsAppMessageRecord),
            typeof(LankaConnect.Domain.Communications.Entities.WhatsAppTemplate),
            typeof(LankaConnect.Domain.Communications.Entities.WhatsAppWebhookEvent),
            // Phase 1.10a (Wave4.9.2.10a, 2026-06-09): events schema - Event aggregate proper (10 entities)
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
            // Phase 1.10b (Wave4.9.2.10b, 2026-06-09): events signups + seats + venue (10 entities)
            typeof(LankaConnect.Domain.Events.Entities.SignUpList),
            typeof(LankaConnect.Domain.Events.Entities.SignUpItem),
            typeof(LankaConnect.Domain.Events.Entities.SignUpCommitment),
            typeof(LankaConnect.Domain.Events.Entities.Seat),
            typeof(LankaConnect.Domain.Events.Entities.SeatHold),
            typeof(LankaConnect.Domain.Events.Entities.SeatReservation),
            typeof(LankaConnect.Domain.Events.Entities.VenueLayout),
            typeof(LankaConnect.Domain.Events.Entities.VenueZone),
            typeof(LankaConnect.Domain.Events.Entities.VenueTable),
            typeof(LankaConnect.Domain.Events.Entities.VenueDecoration),
            // Phase 1.10c.c (Wave4.9.2.10c.c, 2026-06-09): events.tickets (1 entity)
            typeof(LankaConnect.Domain.Events.Entities.Ticket),
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
        
        // Events schema
        modelBuilder.Entity<Event>().ToTable("events", "events");
        modelBuilder.Entity<Registration>().ToTable("registrations", "events");
        modelBuilder.Entity<SignUpList>().ToTable("sign_up_lists", "events");
        modelBuilder.Entity<SignUpItem>().ToTable("sign_up_items", "events");
        modelBuilder.Entity<SignUpCommitment>().ToTable("sign_up_commitments", "events");
        modelBuilder.Entity<MetroArea>().ToTable("metro_areas", "events");
        modelBuilder.Entity<EventTemplate>().ToTable("event_templates", "events"); // Phase 6A.8
        modelBuilder.Entity<EventImage>().ToTable("EventImages", "events"); // Epic 2 Phase 2
        modelBuilder.Entity<EventVideo>().ToTable("EventVideos", "events"); // Epic 2 Phase 2
        
        // Community schema  
        modelBuilder.Entity<ForumTopic>().ToTable("topics", "community");
        modelBuilder.Entity<Reply>().ToTable("replies", "community");
        
        // Business schema
        modelBuilder.Entity<Business>().ToTable("businesses", "business");
        modelBuilder.Entity<Service>().ToTable("services", "business");
        modelBuilder.Entity<Review>().ToTable("reviews", "business");
        
        // Communications schema
        modelBuilder.Entity<EmailMessage>().ToTable("email_messages", "communications");
        modelBuilder.Entity<EmailTemplate>().ToTable("email_templates", "communications");
        modelBuilder.Entity<UserEmailPreferences>().ToTable("user_email_preferences", "communications");
        modelBuilder.Entity<NewsletterSubscriber>().ToTable("newsletter_subscribers", "communications");
        modelBuilder.Entity<Newsletter>().ToTable("newsletters", "communications"); // Phase 6A.74: Newsletter/News Alert Feature
        modelBuilder.Entity<NewsletterEmailHistory>().ToTable("newsletter_email_history", "communications"); // Phase 6A.74 Part 13: Newsletter email send history
        modelBuilder.Entity<EventNotificationHistory>().ToTable("event_notification_history", "communications"); // Phase 6A.61: Event notification history tracking
        modelBuilder.Entity<EmailMetricRecord>().ToTable("email_metrics", "communications"); // Phase 6A.89: Email metrics persistence
        modelBuilder.Entity<EmailFailureDetail>().ToTable("email_failure_details", "communications"); // Phase 6A.99: Email failure details persistence
        modelBuilder.Entity<WhatsAppMessageRecord>().ToTable("whatsapp_messages", "communications"); // Phase 7A: WhatsApp Integration
        modelBuilder.Entity<WhatsAppTemplate>().ToTable("whatsapp_templates", "communications"); // Phase 7A: WhatsApp Integration
        modelBuilder.Entity<UserWhatsAppPreferences>().ToTable("user_whatsapp_preferences", "communications"); // Phase 7A: WhatsApp Integration
        modelBuilder.Entity<WhatsAppWebhookEvent>().ToTable("whatsapp_webhook_events", "communications"); // Phase 7A: WhatsApp Integration

        // Analytics schema (Epic 2 Phase 3)
        modelBuilder.Entity<EventAnalytics>().ToTable("event_analytics", "analytics");
        modelBuilder.Entity<EventViewRecord>().ToTable("event_view_records", "analytics");

        // W4.0b: Notification table mapping moved to NotificationsDbContext (notifications schema).

        // Tickets schema (Phase 6A.24)
        modelBuilder.Entity<Ticket>().ToTable("tickets", "events");

        // Phase 6A.148: refund approval workflow tables — events schema (matches Registration)
        modelBuilder.Entity<RefundRequest>().ToTable("refund_requests", "events");
        modelBuilder.Entity<RefundRequestLineItem>().ToTable("refund_request_line_items", "events");

        // Venue Seating tables (Phase 2: Seat Booking + Slice 2+3A)
        modelBuilder.Entity<VenueLayout>().ToTable("venue_layouts", "events");
        modelBuilder.Entity<VenueZone>().ToTable("venue_zones", "events");
        modelBuilder.Entity<VenueTable>().ToTable("venue_tables", "events"); // Slice 2+3A
        modelBuilder.Entity<VenueDecoration>().ToTable("venue_decorations", "events"); // Slice 2+3A
        modelBuilder.Entity<Seat>().ToTable("seats", "events");
        modelBuilder.Entity<SeatHold>().ToTable("seat_holds", "events");
        modelBuilder.Entity<SeatReservation>().ToTable("seat_reservations", "events");
        modelBuilder.Entity<TierAssignment>().ToTable("tier_assignments", "events"); // Slice 4 Release N

        // Registration Addition tables (Add-Only Attendees Feature)
        modelBuilder.Entity<RegistrationAddition>().ToTable("registration_additions", "events");
        modelBuilder.Entity<RegistrationPayment>().ToTable("registration_payments", "events");

        // Donation table (Standalone Donation System)
        modelBuilder.Entity<Donation>().ToTable("donations", "events");

        // Badges schema (Phase 6A.25)
        modelBuilder.Entity<Badge>().ToTable("badges", "badges");
        modelBuilder.Entity<EventBadge>().ToTable("event_badges", "badges");

        // Custom Form tables (events schema)
        // W4.3: Forms entity table mappings moved to FormsDbContext (events schema cross-schema overrides).

        // Tax schema (Phase 6A.X)
        // Migration 20260114170149 created in public schema, will be moved to reference_data schema
        modelBuilder.Entity<LankaConnect.Domain.Tax.StateTaxRate>().ToTable("state_tax_rates", "reference_data");
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
            typeof(Business),
            typeof(Service),
            typeof(Review),
            typeof(EmailMessage),
            typeof(EmailTemplate),
            typeof(LankaConnect.Domain.Communications.Entities.EmailDispatchLog), // Phase 6A.148.W5.6.B.OBS1: durable email dispatch audit log
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
            typeof(EmailGroup), // Phase 6A.25: Email Groups Management
            typeof(StripeCustomer), // Phase 6A.4: Stripe Payment Integration
            typeof(LankaConnect.Infrastructure.Payments.Entities.StripeWebhookEvent), // Phase 6A.24: Webhook idempotency tracking
            typeof(ReferenceValue), // Phase 6A.47: Unified Reference Data
            typeof(LankaConnect.Domain.Tax.StateTaxRate), // Phase 6A.X: US State Sales Tax Rates
            typeof(SupportTicket), // Phase 6A.89: Support/Feedback System
            typeof(AdminAuditLog), // Phase 6A.89: Admin Audit Logging
            // W4.3: EventForm + FormQuestion + FormResponse + FormAnswer moved to FormsDbContext.
            typeof(Donation), // Standalone Donation System
            typeof(Collection), // Event fund contributions (Financial Features)
            typeof(Sponsor), // Money/item sponsorships (Financial Features)
            typeof(AddOnDefinition), // Purchasable add-on items (Financial Features)
            typeof(AddOnPurchase), // Add-on purchases (Financial Features)
            typeof(SponsorshipPackage), // Phase 6A.156: organizer-defined sponsorship packages (Gold/Silver/Bronze)
            typeof(LankaConnect.Domain.Events.Entities.EventOrganizerContact), // Multiple Organizer Contacts
            typeof(LankaConnect.Domain.Events.Entities.EventSlugAlias), // Phase 6A.154: retired vanity slug aliases (permanent 301 sources)
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
            typeof(LankaConnect.Domain.Events.Entities.RegistrationModeConversion), // Phase 7F-B: mode-conversion audit
            typeof(LankaConnect.Domain.Events.Entities.RegistrationModeConversionRow), // Phase 7F-B: per-row audit detail
            typeof(RefundRequest), // Phase 6A.148: refund approval workflow aggregate-internal entity
            typeof(RefundRequestLineItem) // Phase 6A.148: per-bucket refund line item
        };

        // Get all types from Domain assembly that aren't in our configured list
        var domainAssembly = typeof(LegacyBaseEntity).Assembly;
        var valueObjectType = typeof(ValueObject);

        var allDomainTypes = domainAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract);

        foreach (var type in allDomainTypes)
        {
            // Skip value objects - they are configured via OwnsOne/OwnsMany in entity configurations
            if (valueObjectType.IsAssignableFrom(type))
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
        var trackedEntitiesBeforeDetect = ChangeTracker.Entries<LegacyBaseEntity>().ToList();
        _logger.LogInformation(
            "[DIAG-11] Tracked LegacyBaseEntity count BEFORE DetectChanges: {Count}",
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
        // Without this, ChangeTracker.Entries<LegacyBaseEntity>() returns empty collection
        // because EF Core only auto-detects changes DURING SaveChangesAsync()
        ChangeTracker.DetectChanges();

        // DIAGNOSTIC: Log all tracked entities AFTER DetectChanges
        var trackedEntitiesAfterDetect = ChangeTracker.Entries<LegacyBaseEntity>().ToList();
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
        var domainEvents = ChangeTracker.Entries<LegacyBaseEntity>()
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
        foreach (var entry in ChangeTracker.Entries<LegacyBaseEntity>())
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