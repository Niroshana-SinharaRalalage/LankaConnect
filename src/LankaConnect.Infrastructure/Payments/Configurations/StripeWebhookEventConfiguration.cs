using LankaConnect.Infrastructure.Payments.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LankaConnect.Infrastructure.Payments.Configurations;

/// <summary>
/// EF Core configuration for StripeWebhookEvent infrastructure entity
/// Phase 6A.4: Stripe Payment Integration - Webhook Idempotency
/// </summary>
public class StripeWebhookEventConfiguration : IEntityTypeConfiguration<StripeWebhookEvent>
{
    public void Configure(EntityTypeBuilder<StripeWebhookEvent> builder)
    {
        builder.ToTable("stripe_webhook_events", "payments");

        builder.HasKey(swe => swe.Id);

        // EventId - Stripe event ID (evt_xxx) - unique for idempotency
        builder.Property(swe => swe.EventId)
            .HasColumnName("event_id")
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(swe => swe.EventId)
            .IsUnique()
            .HasDatabaseName("ix_stripe_webhook_events_event_id");

        // EventType - type of webhook event (e.g., customer.subscription.created)
        builder.Property(swe => swe.EventType)
            .HasColumnName("event_type")
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(swe => swe.EventType)
            .HasDatabaseName("ix_stripe_webhook_events_event_type");

        // Processed flag
        builder.Property(swe => swe.Processed)
            .HasColumnName("processed")
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(swe => swe.Processed)
            .HasDatabaseName("ix_stripe_webhook_events_processed");

        // ProcessedAt - nullable, set when event is processed
        builder.Property(swe => swe.ProcessedAt)
            .HasColumnName("processed_at")
            .IsRequired(false);

        // ErrorMessage - nullable, stores error if processing failed
        builder.Property(swe => swe.ErrorMessage)
            .HasColumnName("error_message")
            .IsRequired(false)
            .HasMaxLength(2000);

        // AttemptCount - tracks retry attempts
        builder.Property(swe => swe.AttemptCount)
            .HasColumnName("attempt_count")
            .IsRequired()
            .HasDefaultValue(0);

        // Base entity properties (CreatedAt, UpdatedAt)
        builder.Property(swe => swe.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(swe => swe.UpdatedAt)
            .HasColumnName("updated_at");

        // Index for querying unprocessed events
        builder.HasIndex(swe => new { swe.Processed, swe.CreatedAt })
            .HasDatabaseName("ix_stripe_webhook_events_processed_created_at");
    }
}
