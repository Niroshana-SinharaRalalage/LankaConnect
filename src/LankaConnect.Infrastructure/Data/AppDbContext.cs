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

        // Apply entity configurations
        modelBuilder.ApplyConfiguration(new UserConfiguration());
        modelBuilder.ApplyConfiguration(new EventConfiguration());
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

    private static void ConfigureValueObjectConversions(ModelBuilder modelBuilder)
    {
        // Configure value object conversions will be added in separate configurations
        // This keeps the DbContext clean and focused
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