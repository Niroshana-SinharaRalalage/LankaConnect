using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Communications.Entities;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// Phase 7A: EF Core configuration for WhatsAppWebhookEvent entity.
/// Maps to communications.whatsapp_webhook_events table.
/// </summary>
public class WhatsAppWebhookEventConfiguration : IEntityTypeConfiguration<WhatsAppWebhookEvent>
{
    public void Configure(EntityTypeBuilder<WhatsAppWebhookEvent> builder)
    {
        builder.ToTable("whatsapp_webhook_events", "communications");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(100)
            .IsRequired();

        // JSONB stored as string — no ValueComparer needed (immutable). See Phase 6A.129.
        builder.Property(e => e.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.AcsMessageId)
            .HasColumnName("acs_message_id")
            .HasMaxLength(200);

        builder.Property(e => e.MetaMessageId)
            .HasColumnName("meta_message_id")
            .HasMaxLength(200);

        builder.Property(e => e.Processed)
            .HasColumnName("processed")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.ProcessedAt)
            .HasColumnName("processed_at")
            .IsRequired(false);

        builder.Property(e => e.ErrorMessage)
            .HasColumnName("error_message")
            .HasMaxLength(1000);

        // Audit timestamps from BaseEntity
        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        // Performance indexes
        builder.HasIndex(e => e.AcsMessageId)
            .HasDatabaseName("IX_WhatsAppWebhookEvents_AcsMessageId");

        builder.HasIndex(e => new { e.Processed, e.CreatedAt })
            .HasDatabaseName("IX_WhatsAppWebhookEvents_Processed_CreatedAt");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_WhatsAppWebhookEvents_CreatedAt");
    }
}
