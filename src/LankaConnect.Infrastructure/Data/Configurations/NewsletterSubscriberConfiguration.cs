using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Communications.Entities;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for NewsletterSubscriber aggregate root
/// Phase 6A.64: Updated to map MetroAreaIds collection to junction table
/// </summary>
public class NewsletterSubscriberConfiguration : IEntityTypeConfiguration<NewsletterSubscriber>
{
    public void Configure(EntityTypeBuilder<NewsletterSubscriber> builder)
    {
        builder.ToTable("newsletter_subscribers", "communications");

        builder.HasKey(ns => ns.Id);
        builder.Property(ns => ns.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        // Configure Email value object (OwnsOne pattern)
        builder.OwnsOne(ns => ns.Email, email =>
        {
            email.Property(e => e.Value)
                .HasColumnName("email")
                .HasMaxLength(255)
                .IsRequired();
        });

        // Phase 6A.64: Configure many-to-many relationship with MetroAreas via junction table
        // Maps the private _metroAreaIds field to the newsletter_subscriber_metro_areas table
        builder.HasMany<Domain.Events.MetroArea>()
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "newsletter_subscriber_metro_areas",
                j => j
                    .HasOne<Domain.Events.MetroArea>()
                    .WithMany()
                    .HasForeignKey("metro_area_id")
                    .HasConstraintName("fk_newsletter_subscriber_metro_areas_metro_areas")
                    .OnDelete(DeleteBehavior.Cascade),
                j => j
                    .HasOne<NewsletterSubscriber>()
                    .WithMany()
                    .HasForeignKey("subscriber_id")
                    .HasConstraintName("fk_newsletter_subscriber_metro_areas_newsletter_subscribers")
                    .OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.ToTable("newsletter_subscriber_metro_areas", "communications");
                    j.HasKey("subscriber_id", "metro_area_id");
                    j.Property<DateTime>("created_at")
                        .HasDefaultValueSql("NOW()");
                    j.HasIndex("metro_area_id")
                        .HasDatabaseName("ix_newsletter_subscriber_metro_areas_metro_area_id");
                    j.HasIndex("subscriber_id")
                        .HasDatabaseName("ix_newsletter_subscriber_metro_areas_subscriber_id");
                });

        // Map the private _metroAreaIds field for EF Core to populate
        builder.Property<List<Guid>>("_metroAreaIds")
            .HasField("_metroAreaIds")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Configure flags
        builder.Property(ns => ns.ReceiveAllLocations)
            .HasColumnName("receive_all_locations")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(ns => ns.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(ns => ns.IsConfirmed)
            .HasColumnName("is_confirmed")
            .IsRequired()
            .HasDefaultValue(false);

        // Configure tokens
        builder.Property(ns => ns.ConfirmationToken)
            .HasColumnName("confirmation_token")
            .HasMaxLength(100)
            .IsRequired(false);

        builder.Property(ns => ns.UnsubscribeToken)
            .HasColumnName("unsubscribe_token")
            .HasMaxLength(100)
            .IsRequired();

        // Configure timestamps
        builder.Property(ns => ns.ConfirmationSentAt)
            .HasColumnName("confirmation_sent_at")
            .IsRequired(false);

        builder.Property(ns => ns.ConfirmedAt)
            .HasColumnName("confirmed_at")
            .IsRequired(false);

        builder.Property(ns => ns.UnsubscribedAt)
            .HasColumnName("unsubscribed_at")
            .IsRequired(false);

        builder.Property(ns => ns.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(ns => ns.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired(false);

        // Configure concurrency token
        builder.Property(ns => ns.Version)
            .HasColumnName("version")
            .IsRowVersion();

        // Indexes for performance
        // Note: Unique index on email will be added in migration manually
        // Phase 6A.64: Removed idx_newsletter_subscribers_metro_area_id (column no longer exists)
        builder.HasIndex(ns => ns.ConfirmationToken)
            .HasDatabaseName("idx_newsletter_subscribers_confirmation_token");

        builder.HasIndex(ns => ns.UnsubscribeToken)
            .HasDatabaseName("idx_newsletter_subscribers_unsubscribe_token");

        builder.HasIndex(ns => new { ns.IsActive, ns.IsConfirmed })
            .HasDatabaseName("idx_newsletter_subscribers_active_confirmed");

        // Ignore domain events (not persisted)
        builder.Ignore(ns => ns.DomainEvents);
    }
}
