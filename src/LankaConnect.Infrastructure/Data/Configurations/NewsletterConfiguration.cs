using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.Enums;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for Newsletter entity
/// Phase 6A.74 Part 3C: Newsletter table and junction tables for email groups and metro areas
/// </summary>
public class NewsletterConfiguration : IEntityTypeConfiguration<Newsletter>
{
    public void Configure(EntityTypeBuilder<Newsletter> builder)
    {
        builder.ToTable("newsletters", "communications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.Id)
            .ValueGeneratedNever()
            .HasColumnName("id");

        // Configure NewsletterTitle value object
        builder.OwnsOne(n => n.Title, title =>
        {
            title.Property(t => t.Value)
                .HasColumnName("title")
                .HasMaxLength(200)
                .IsRequired();
        });

        // Configure NewsletterDescription value object
        builder.OwnsOne(n => n.Description, description =>
        {
            description.Property(d => d.Value)
                .HasColumnName("description")
                .HasMaxLength(5000)
                .IsRequired();
        });

        // Basic properties
        builder.Property(n => n.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.Property(n => n.EventId)
            .HasColumnName("event_id")
            .IsRequired(false); // Nullable - can be event-based or standalone news alert

        // Status enum (stored as string)
        builder.Property(n => n.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(NewsletterStatus.Draft);

        // Timestamps
        builder.Property(n => n.PublishedAt)
            .HasColumnName("published_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(n => n.SentAt)
            .HasColumnName("sent_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(n => n.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        // Recipient configuration flags
        builder.Property(n => n.IncludeNewsletterSubscribers)
            .HasColumnName("include_newsletter_subscribers")
            .IsRequired()
            .HasDefaultValue(false);

        // Phase 6A.74 Enhancement 1: Location targeting
        builder.Property(n => n.TargetAllLocations)
            .HasColumnName("target_all_locations")
            .IsRequired()
            .HasDefaultValue(false);

        // Audit fields
        builder.Property(n => n.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(n => n.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        // Concurrency token (rowversion for optimistic concurrency)
        builder.Property<byte[]>("Version")
            .HasColumnName("version")
            .IsRowVersion();

        // Ignore domain list properties (not persisted directly)
        builder.Ignore(n => n.EmailGroupIds);
        builder.Ignore(n => n.MetroAreaIds);

        // Phase 6A.74: Email Groups - Many-to-Many Relationship
        // Junction table pattern following EventConfiguration
        builder
            .HasMany<EmailGroup>("_emailGroupEntities")
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "newsletter_email_groups",
                j => j
                    .HasOne<EmailGroup>()
                    .WithMany()
                    .HasForeignKey("email_group_id")
                    .OnDelete(DeleteBehavior.Cascade), // Safe with soft delete pattern
                j => j
                    .HasOne<Newsletter>()
                    .WithMany()
                    .HasForeignKey("newsletter_id")
                    .OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.ToTable("newsletter_email_groups", "communications");
                    j.HasKey("newsletter_id", "email_group_id"); // Composite primary key
                    j.Property<DateTime>("created_at")
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    // Indexes for query performance
                    j.HasIndex("newsletter_id");
                    j.HasIndex("email_group_id");
                });

        // Phase 6A.74 Enhancement 1: Metro Areas - Many-to-Many Relationship
        // Junction table for location targeting (non-event newsletters)
        builder
            .HasMany<Domain.Events.MetroArea>("_metroAreaEntities")
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "newsletter_metro_areas",
                j => j
                    .HasOne<Domain.Events.MetroArea>()
                    .WithMany()
                    .HasForeignKey("metro_area_id")
                    .OnDelete(DeleteBehavior.Cascade),
                j => j
                    .HasOne<Newsletter>()
                    .WithMany()
                    .HasForeignKey("newsletter_id")
                    .OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.ToTable("newsletter_metro_areas", "communications");
                    j.HasKey("newsletter_id", "metro_area_id"); // Composite primary key
                    j.Property<DateTime>("created_at")
                        .HasColumnType("timestamp with time zone")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    // Indexes for query performance
                    j.HasIndex("newsletter_id");
                    j.HasIndex("metro_area_id");
                });

        // Foreign key to users table
        builder.HasOne<Domain.Users.User>()
            .WithMany()
            .HasForeignKey(n => n.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict); // Don't cascade delete newsletters when user is deleted

        // Foreign key to events table (nullable)
        builder.HasOne<Domain.Events.Event>()
            .WithMany()
            .HasForeignKey(n => n.EventId)
            .OnDelete(DeleteBehavior.SetNull) // Set to null if event is deleted (preserve newsletter)
            .IsRequired(false);

        // Indexes for query performance
        builder.HasIndex(n => n.CreatedByUserId)
            .HasDatabaseName("ix_newsletters_created_by_user_id");

        builder.HasIndex(n => n.EventId)
            .HasDatabaseName("ix_newsletters_event_id");

        builder.HasIndex(n => n.Status)
            .HasDatabaseName("ix_newsletters_status");

        builder.HasIndex(n => n.ExpiresAt)
            .HasDatabaseName("ix_newsletters_expires_at");

        // Composite index for published newsletters query
        builder.HasIndex(n => new { n.Status, n.PublishedAt })
            .HasDatabaseName("ix_newsletters_status_published_at");
    }
}
