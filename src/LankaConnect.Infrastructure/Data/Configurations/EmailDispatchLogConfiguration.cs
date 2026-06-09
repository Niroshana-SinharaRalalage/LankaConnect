using LankaConnect.Domain.Communications.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// Phase 6A.148.W5.6.B.OBS1 — EF config for the durable email dispatch audit log.
///
/// Schema lives in <c>communications</c> schema (sibling to <c>email_messages</c> /
/// <c>email_templates</c>). Index strategy targets the three queries the operator
/// post-mortem flow needs:
///   1. "What did this refund send?" → (refund_request_id, dispatched_at DESC)
///   2. "Did sponsor X get notified?" → (entity_type, entity_id, dispatched_at DESC)
///   3. "What went to recipient X today?" → (recipient_email, dispatched_at DESC)
///
/// payload_json stored as jsonb so future queries can drill into template params
/// without DDL changes (e.g., "find every refund email with RefundAmount &gt; 100").
/// </summary>
public class EmailDispatchLogConfiguration : IEntityTypeConfiguration<EmailDispatchLog>
{
    public void Configure(EntityTypeBuilder<EmailDispatchLog> builder)
    {
        builder.ToTable("email_dispatch_log", "communications");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.CorrelationId).HasColumnName("correlation_id").IsRequired();

        builder.Property(x => x.RefundRequestId).HasColumnName("refund_request_id");
        builder.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(40);
        builder.Property(x => x.EntityId).HasColumnName("entity_id");

        builder.Property(x => x.TemplateName)
            .HasColumnName("template_name").HasMaxLength(80).IsRequired();
        builder.Property(x => x.RecipientEmail)
            .HasColumnName("recipient_email").HasMaxLength(255).IsRequired();
        builder.Property(x => x.RecipientName)
            .HasColumnName("recipient_name").HasMaxLength(255);
        builder.Property(x => x.SubjectRendered)
            .HasColumnName("subject_rendered");
        builder.Property(x => x.PayloadJson)
            .HasColumnName("payload_json").HasColumnType("jsonb");

        builder.Property(x => x.Suppressed).HasColumnName("suppressed").IsRequired();
        builder.Property(x => x.SuppressionReason)
            .HasColumnName("suppression_reason").HasMaxLength(120);

        builder.Property(x => x.DispatchedAt).HasColumnName("dispatched_at").IsRequired();
        builder.Property(x => x.ProviderMessageId)
            .HasColumnName("provider_message_id").HasMaxLength(120);
        builder.Property(x => x.ProviderStatus)
            .HasColumnName("provider_status").HasMaxLength(40);

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        // Wave4.9.2.7 Phase 1.7 (2026-06-09): physical CreatedBy/UpdatedBy.
        builder.Property(x => x.CreatedBy).HasColumnName("created_by").HasColumnType("text");
        builder.Property(x => x.UpdatedBy).HasColumnName("updated_by").HasColumnType("text");

        builder.HasIndex(x => new { x.RefundRequestId, x.DispatchedAt })
            .HasDatabaseName("ix_email_dispatch_log_rr_dispatched")
            .HasFilter("refund_request_id IS NOT NULL");
        builder.HasIndex(x => new { x.EntityType, x.EntityId, x.DispatchedAt })
            .HasDatabaseName("ix_email_dispatch_log_entity_dispatched")
            .HasFilter("entity_type IS NOT NULL AND entity_id IS NOT NULL");
        builder.HasIndex(x => new { x.RecipientEmail, x.DispatchedAt })
            .HasDatabaseName("ix_email_dispatch_log_recipient_dispatched");
        builder.HasIndex(x => new { x.TemplateName, x.DispatchedAt })
            .HasDatabaseName("ix_email_dispatch_log_template_dispatched");
        builder.HasIndex(x => x.CorrelationId)
            .HasDatabaseName("ix_email_dispatch_log_correlation");
    }
}
