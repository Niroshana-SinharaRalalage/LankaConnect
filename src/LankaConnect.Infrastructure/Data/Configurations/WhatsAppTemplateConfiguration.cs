using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Modules.Communications.Domain.Entities;
using LankaConnect.Modules.Communications.Domain.Enums;

namespace LankaConnect.SPLIT_PER_ENTITY.Configurations;

/// <summary>
/// Phase 7A: EF Core configuration for WhatsAppTemplate entity.
/// Maps to communications.whatsapp_templates table.
/// </summary>
public class WhatsAppTemplateConfiguration : IEntityTypeConfiguration<WhatsAppTemplate>
{
    public void Configure(EntityTypeBuilder<WhatsAppTemplate> builder)
    {
        builder.ToTable("whatsapp_templates", "communications");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(e => e.TemplateName)
            .HasColumnName("template_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.DisplayName)
            .HasColumnName("display_name")
            .HasMaxLength(200)
            .IsRequired();

        // Enum properties with string conversion
        builder.Property(e => e.Category)
            .HasColumnName("category")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Language)
            .HasColumnName("language")
            .HasMaxLength(10)
            .IsRequired()
            .HasDefaultValue("en");

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // Template content
        builder.Property(e => e.HeaderText)
            .HasColumnName("header_text")
            .HasMaxLength(500);

        builder.Property(e => e.BodyText)
            .HasColumnName("body_text")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(e => e.FooterText)
            .HasColumnName("footer_text")
            .HasMaxLength(500);

        // JSONB stored as string — no ValueComparer needed (immutable). See Phase 6A.129.
        builder.Property(e => e.ButtonTypes)
            .HasColumnName("button_types")
            .HasColumnType("jsonb");

        // JSONB stored as string — no ValueComparer needed (immutable). See Phase 6A.129.
        builder.Property(e => e.ParameterNames)
            .HasColumnName("parameter_names")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(e => e.ParameterCount)
            .HasColumnName("parameter_count")
            .IsRequired();

        builder.Property(e => e.MetaTemplateId)
            .HasColumnName("meta_template_id")
            .HasMaxLength(200);

        // Phase 7B: Twilio Content API SID
        builder.Property(e => e.TwilioContentSid)
            .HasColumnName("twilio_content_sid")
            .HasMaxLength(200)
            .IsRequired(false);

        // JSONB stored as string — no ValueComparer needed (immutable). See Phase 6A.129.
        builder.Property(e => e.SampleValues)
            .HasColumnName("sample_values")
            .HasColumnType("jsonb");

        // Approval tracking
        builder.Property(e => e.ApprovedAt)
            .HasColumnName("approved_at")
            .IsRequired(false);

        builder.Property(e => e.RejectedAt)
            .HasColumnName("rejected_at")
            .IsRequired(false);

        builder.Property(e => e.RejectionReason)
            .HasColumnName("rejection_reason")
            .HasMaxLength(1000);

        // Audit timestamps from BaseEntity
        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        // Wave4.9.2.9 Phase 1.9 (2026-06-09): physical CreatedBy/UpdatedBy.
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasColumnType("text");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasColumnType("text");

        // Ignore computed properties
        builder.Ignore(e => e.IsApproved);
        builder.Ignore(e => e.IsUsable);

        // Unique index on template_name
        builder.HasIndex(e => e.TemplateName)
            .IsUnique()
            .HasDatabaseName("IX_WhatsAppTemplates_TemplateName_Unique");

        // Performance indexes
        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_WhatsAppTemplates_Status");

        builder.HasIndex(e => e.Category)
            .HasDatabaseName("IX_WhatsAppTemplates_Category");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_WhatsAppTemplates_CreatedAt");
    }
}
