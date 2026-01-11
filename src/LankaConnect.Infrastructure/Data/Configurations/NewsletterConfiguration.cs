using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.Enums;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for Newsletter aggregate root
/// Phase 6A.74: Newsletter/News Alert Feature
/// </summary>
public class NewsletterConfiguration : IEntityTypeConfiguration<Newsletter>
{
    public void Configure(EntityTypeBuilder<Newsletter> builder)
    {
        builder.ToTable("newsletters", "communications");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        // Configure NewsletterTitle value object (OwnsOne pattern)
        builder.OwnsOne(n => n.Title, title =>
        {
            title.Property(t => t.Value)
                .HasColumnName("title")
                .HasMaxLength(200)
                .IsRequired();
        });

        // Configure NewsletterDescription value object (OwnsOne pattern)
        builder.OwnsOne(n => n.Description, description =>
        {
            description.Property(d => d.Value)
                .HasColumnName("description")
                .HasMaxLength(5000)
                .IsRequired();
        });

        // Configure basic properties
        builder.Property(n => n.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.Property(n => n.EventId)
            .HasColumnName("event_id")
            .IsRequired(false); // Nullable - newsletter may not be linked to event

        // Configure Status enum (store as string for readability)
        builder.Property(n => n.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired()
            .HasDefaultValue(NewsletterStatus.Draft);

        // Configure timestamps
        builder.Property(n => n.PublishedAt)
            .HasColumnName("published_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false); // Nullable - only set when published

        builder.Property(n => n.SentAt)
            .HasColumnName("sent_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false); // Nullable - only set when sent

        builder.Property(n => n.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false); // Nullable - only set when published/reactivated

        builder.Property(n => n.IncludeNewsletterSubscribers)
            .HasColumnName("include_newsletter_subscribers")
            .IsRequired()
            .HasDefaultValue(true);

        // Configure audit fields
        builder.Property(n => n.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(n => n.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        // Configure concurrency token
        builder.Property(n => n.Version)
            .HasColumnName("version")
            .IsRowVersion();

        // The _emailGroupIds collection is managed in application code
        // We don't map it to a database column - it's populated from the junction table
        // by the repository when loading entities
        builder.Ignore(n => n.EmailGroupIds);

        // Phase 6A.74: Email Groups - Many-to-Many Relationship via Junction Table
        // Follows the same pattern as Event-EmailGroup relationship (Phase 6A.32)
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
                    .OnDelete(DeleteBehavior.Cascade), // Safe with soft delete pattern
                j =>
                {
                    j.ToTable("newsletter_email_groups", "communications");
                    j.HasKey("newsletter_id", "email_group_id"); // Composite primary key
                    j.Property<DateTime>("assigned_at")
                        .HasDefaultValueSql("CURRENT_TIMESTAMP");

                    // Indexes for query performance
                    j.HasIndex("newsletter_id");
                    j.HasIndex("email_group_id");
                });

        // Indexes for performance
        builder.HasIndex(n => n.Status)
            .HasDatabaseName("idx_newsletters_status");

        builder.HasIndex(n => n.CreatedByUserId)
            .HasDatabaseName("idx_newsletters_created_by");

        builder.HasIndex(n => n.EventId)
            .HasDatabaseName("idx_newsletters_event_id")
            .HasFilter("event_id IS NOT NULL"); // Partial index - only when event linked

        // Index for auto-deactivation job (finds expired newsletters)
        builder.HasIndex(n => n.ExpiresAt)
            .HasDatabaseName("idx_newsletters_expires_at")
            .HasFilter("status = 'Active'"); // Partial index - only active newsletters

        // Ignore domain events (not persisted)
        builder.Ignore(n => n.DomainEvents);
    }
}
