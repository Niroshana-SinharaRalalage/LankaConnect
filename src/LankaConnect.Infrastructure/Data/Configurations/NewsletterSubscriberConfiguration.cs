using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Modules.Communications.Domain.Entities;

namespace LankaConnect.SPLIT_PER_ENTITY.Configurations;

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

        // Phase 6A.64: The _metroAreaIds collection is managed in application code
        // We don't map it to a database column - it's populated from the junction table
        // by the repository when loading entities
        // The junction table relationship is managed via raw SQL in the repository layer

        // Ignore the _metroAreaIds field - it's not a database column
        builder.Ignore(ns => ns.MetroAreaIds);

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

        // Phase 7A.6D: WhatsApp phone number for newsletter notifications
        builder.Property(ns => ns.WhatsAppPhoneNumber)
            .HasColumnName("whatsapp_phone_number")
            .HasMaxLength(20)
            .IsRequired(false);

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

        // Wave4.9.2.8 Phase 1.8 (2026-06-09): physical CreatedBy/UpdatedBy.
        builder.Property(ns => ns.CreatedBy).HasColumnName("created_by").HasColumnType("text");
        builder.Property(ns => ns.UpdatedBy).HasColumnName("updated_by").HasColumnType("text");

        // Configure concurrency token using PostgreSQL's built-in xmin system column
#pragma warning disable CS0618 // UseXminAsConcurrencyToken is the correct PostgreSQL approach
        builder.UseXminAsConcurrencyToken();
#pragma warning restore CS0618

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
