using Microsoft.EntityFrameworkCore;
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
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Infrastructure.Data.Configurations;
using LankaConnect.Infrastructure.Data.Seeders;

namespace LankaConnect.Infrastructure.Data;

public class AppDbContext : DbContext, IApplicationDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
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
            typeof(EventAnalytics), // Epic 2 Phase 3
            typeof(EventViewRecord), // Epic 2 Phase 3
            typeof(Notification), // Phase 6A.6
            typeof(Ticket), // Phase 6A.24
            typeof(Badge), // Phase 6A.25
            typeof(EventBadge), // Phase 6A.25
            typeof(EmailGroup) // Phase 6A.25: Email Groups Management
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

        return await SaveChangesAsync(cancellationToken);
    }
}