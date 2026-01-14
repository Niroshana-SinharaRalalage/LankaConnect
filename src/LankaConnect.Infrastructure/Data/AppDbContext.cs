using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Community;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Analytics;
using LankaConnect.Domain.Notifications;
using LankaConnect.Domain.Badges;
using LankaConnect.Domain.ReferenceData.Entities;
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

    // Business Entity Sets
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Review> Reviews => Set<Review>();
    
    // Communications Entity Sets
    public DbSet<LankaConnect.Domain.Communications.Entities.EmailMessage> EmailMessages => Set<LankaConnect.Domain.Communications.Entities.EmailMessage>();
    public DbSet<LankaConnect.Domain.Communications.Entities.EmailTemplate> EmailTemplates => Set<LankaConnect.Domain.Communications.Entities.EmailTemplate>();
    public DbSet<LankaConnect.Domain.Communications.Entities.UserEmailPreferences> UserEmailPreferences => Set<LankaConnect.Domain.Communications.Entities.UserEmailPreferences>();
    public DbSet<NewsletterSubscriber> NewsletterSubscribers => Set<NewsletterSubscriber>();
    public DbSet<Newsletter> Newsletters => Set<Newsletter>(); // Phase 6A.74: Newsletter/News Alert Feature
    public DbSet<LankaConnect.Domain.Events.Entities.EventNotificationHistory> EventNotificationHistories => Set<LankaConnect.Domain.Events.Entities.EventNotificationHistory>(); // Phase 6A.61: Event notification history tracking

    // Analytics Entity Sets (Epic 2 Phase 3)
    public DbSet<EventAnalytics> EventAnalytics => Set<EventAnalytics>();
    public DbSet<EventViewRecord> EventViewRecords => Set<EventViewRecord>();

    // Notification Entity Set (Phase 6A.6)
    public DbSet<Notification> Notifications => Set<Notification>();

    // Sign-up Management Entity Sets (Phase 6A.16)
    public DbSet<SignUpList> SignUpLists => Set<SignUpList>(); // Phase 6A.16: Required for cascade deletion
    public DbSet<SignUpItem> SignUpItems => Set<SignUpItem>(); // Phase 6A.16: Required for cascade deletion
    public DbSet<SignUpCommitment> SignUpCommitments => Set<SignUpCommitment>(); // Phase 6A.16: Cascade deletion

    // Ticket Entity Set (Phase 6A.24)
    public DbSet<Ticket> Tickets => Set<Ticket>(); // Phase 6A.24: Event tickets with QR codes

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure NetTopologySuite for PostGIS support (Epic 2 Phase 1)
        // This must be called before applying configurations
        modelBuilder.HasPostgresExtension("postgis");

        // Apply entity configurations
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new EventConfiguration());
        modelBuilder.ApplyConfiguration(new EventImageConfiguration()); // Epic 2 Phase 2
        modelBuilder.ApplyConfiguration(new EventVideoConfiguration()); // Epic 2 Phase 2
        modelBuilder.ApplyConfiguration(new RegistrationConfiguration());
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
        modelBuilder.ApplyConfiguration(new UserEmailPreferencesConfiguration());
        modelBuilder.ApplyConfiguration(new NewsletterSubscriberConfiguration());
        modelBuilder.ApplyConfiguration(new NewsletterConfiguration()); // Phase 6A.74: Newsletter/News Alert Feature
        modelBuilder.ApplyConfiguration(new EventNotificationHistoryConfiguration()); // Phase 6A.61: Event notification history tracking

        // Analytics entity configurations (Epic 2 Phase 3)
        modelBuilder.ApplyConfiguration(new EventAnalyticsConfiguration());
        modelBuilder.ApplyConfiguration(new EventViewRecordConfiguration());

        // Notification entity configuration (Phase 6A.6)
        modelBuilder.ApplyConfiguration(new NotificationConfiguration());

        // Ticket entity configuration (Phase 6A.24)
        modelBuilder.ApplyConfiguration(new TicketConfiguration());

        // Badge entity configurations (Phase 6A.25)
        modelBuilder.ApplyConfiguration(new BadgeConfiguration());
        modelBuilder.ApplyConfiguration(new EventBadgeConfiguration());

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

        // Configure schemas
        ConfigureSchemas(modelBuilder);

        // Ignore unconfigured monitoring/infrastructure entities (not MVP)
        IgnoreUnconfiguredEntities(modelBuilder);

        // Configure value object conversions
        ConfigureValueObjectConversions(modelBuilder);

        // Note: Seed data is applied via DbInitializer at runtime
        // due to complex value objects and owned entities
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
        modelBuilder.Entity<EventNotificationHistory>().ToTable("event_notification_history", "communications"); // Phase 6A.61: Event notification history tracking

        // Analytics schema (Epic 2 Phase 3)
        modelBuilder.Entity<EventAnalytics>().ToTable("event_analytics", "analytics");
        modelBuilder.Entity<EventViewRecord>().ToTable("event_view_records", "analytics");

        // Notifications schema (Phase 6A.6)
        modelBuilder.Entity<Notification>().ToTable("notifications", "notifications");

        // Tickets schema (Phase 6A.24)
        modelBuilder.Entity<Ticket>().ToTable("tickets", "events");

        // Badges schema (Phase 6A.25)
        modelBuilder.Entity<Badge>().ToTable("badges", "badges");
        modelBuilder.Entity<EventBadge>().ToTable("event_badges", "badges");
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
            typeof(UserEmailPreferences),
            typeof(NewsletterSubscriber), // Phase 5
            typeof(Newsletter), // Phase 6A.74: Newsletter/News Alert Feature
            typeof(EventNotificationHistory), // Phase 6A.61: Event notification history tracking
            typeof(EventAnalytics), // Epic 2 Phase 3
            typeof(EventViewRecord), // Epic 2 Phase 3
            typeof(Notification), // Phase 6A.6
            typeof(Ticket), // Phase 6A.24
            typeof(Badge), // Phase 6A.25
            typeof(EventBadge), // Phase 6A.25
            typeof(EmailGroup), // Phase 6A.25: Email Groups Management
            typeof(StripeCustomer), // Phase 6A.4: Stripe Payment Integration
            typeof(LankaConnect.Infrastructure.Payments.Entities.StripeWebhookEvent), // Phase 6A.24: Webhook idempotency tracking
            typeof(ReferenceValue) // Phase 6A.47: Unified Reference Data
        };

        // Get all types from Domain assembly that aren't in our configured list
        var domainAssembly = typeof(BaseEntity).Assembly;
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
        _logger.LogInformation("[DIAG-10] AppDbContext.CommitAsync START");

        // DIAGNOSTIC: Log all tracked entities BEFORE DetectChanges
        var trackedEntitiesBeforeDetect = ChangeTracker.Entries<BaseEntity>().ToList();
        _logger.LogInformation(
            "[DIAG-11] Tracked BaseEntity count BEFORE DetectChanges: {Count}",
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

        // Update timestamps before saving
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    // CreatedAt is set in constructor
                    break;
                case EntityState.Modified:
                    entry.Entity.MarkAsUpdated();
                    break;
            }
        }

        // CRITICAL FIX Phase 6A.24: Force change detection BEFORE collecting domain events
        // Without this, ChangeTracker.Entries<BaseEntity>() returns empty collection
        // because EF Core only auto-detects changes DURING SaveChangesAsync()
        ChangeTracker.DetectChanges();

        // DIAGNOSTIC: Log all tracked entities AFTER DetectChanges
        var trackedEntitiesAfterDetect = ChangeTracker.Entries<BaseEntity>().ToList();
        _logger.LogInformation(
            "[DIAG-13] Tracked BaseEntity count AFTER DetectChanges: {Count}",
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
        var domainEvents = ChangeTracker.Entries<BaseEntity>()
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

            // Clear domain events after publishing
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                entry.Entity.ClearDomainEvents();
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
}