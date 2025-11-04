using Microsoft.EntityFrameworkCore;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Community;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Infrastructure.Data.Configurations;

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
    
    // Business Entity Sets
    public DbSet<Business> Businesses => Set<Business>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Review> Reviews => Set<Review>();
    
    // Communications Entity Sets
    public DbSet<LankaConnect.Domain.Communications.Entities.EmailMessage> EmailMessages => Set<LankaConnect.Domain.Communications.Entities.EmailMessage>();
    public DbSet<LankaConnect.Domain.Communications.Entities.EmailTemplate> EmailTemplates => Set<LankaConnect.Domain.Communications.Entities.EmailTemplate>();
    public DbSet<LankaConnect.Domain.Communications.Entities.UserEmailPreferences> UserEmailPreferences => Set<LankaConnect.Domain.Communications.Entities.UserEmailPreferences>();

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
        modelBuilder.ApplyConfiguration(new ForumTopicConfiguration());
        modelBuilder.ApplyConfiguration(new ReplyConfiguration());

        // Business entity configurations
        modelBuilder.ApplyConfiguration(new BusinessConfiguration());
        modelBuilder.ApplyConfiguration(new ServiceConfiguration());
        modelBuilder.ApplyConfiguration(new ReviewConfiguration());

        // Communications entity configurations
        modelBuilder.ApplyConfiguration(new EmailMessageConfiguration());
        modelBuilder.ApplyConfiguration(new EmailTemplateConfiguration());
        modelBuilder.ApplyConfiguration(new UserEmailPreferencesConfiguration());

        // Configure schemas
        ConfigureSchemas(modelBuilder);

        // Ignore unconfigured monitoring/infrastructure entities (not MVP)
        IgnoreUnconfiguredEntities(modelBuilder);

        // Configure value object conversions
        ConfigureValueObjectConversions(modelBuilder);
    }

    private static void ConfigureSchemas(ModelBuilder modelBuilder)
    {
        // Identity schema
        modelBuilder.Entity<User>().ToTable("users", "identity");
        
        // Events schema
        modelBuilder.Entity<Event>().ToTable("events", "events");
        modelBuilder.Entity<Registration>().ToTable("registrations", "events");
        
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
            typeof(ForumTopic),
            typeof(Reply),
            typeof(Business),
            typeof(Service),
            typeof(Review),
            typeof(EmailMessage),
            typeof(EmailTemplate),
            typeof(UserEmailPreferences)
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