using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.Enums;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// Phase 7A: EF Core configuration for WhatsAppMessageRecord entity.
/// Maps to communications.whatsapp_messages table.
/// </summary>
public class WhatsAppMessageRecordConfiguration : IEntityTypeConfiguration<WhatsAppMessageRecord>
{
    public void Configure(EntityTypeBuilder<WhatsAppMessageRecord> builder)
    {
        builder.ToTable("whatsapp_messages", "communications");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        // Phone number properties
        builder.Property(e => e.FromPhoneNumber)
            .HasColumnName("from_phone_number")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.ToPhoneNumber)
            .HasColumnName("to_phone_number")
            .HasMaxLength(20)
            .IsRequired();

        // Enum properties with string conversion
        builder.Property(e => e.MessageType)
            .HasColumnName("message_type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        // Template properties
        builder.Property(e => e.TemplateName)
            .HasColumnName("template_name")
            .HasMaxLength(100);

        // JSONB stored as string — no ValueComparer needed (immutable). See Phase 6A.129.
        builder.Property(e => e.TemplateParameters)
            .HasColumnName("template_parameters")
            .HasColumnType("jsonb");

        builder.Property(e => e.MessageContent)
            .HasColumnName("message_content")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(e => e.Language)
            .HasColumnName("language")
            .HasMaxLength(10)
            .IsRequired()
            .HasDefaultValue("en");

        // Cultural intelligence properties
        // JSONB stored as string — no ValueComparer needed (immutable). See Phase 6A.129.
        builder.Property(e => e.CulturalContext)
            .HasColumnName("cultural_context")
            .HasColumnType("jsonb");

        builder.Property(e => e.CulturalAppropriatenessScore)
            .HasColumnName("cultural_appropriateness_score")
            .IsRequired();

        builder.Property(e => e.RequiresCulturalValidation)
            .HasColumnName("requires_cultural_validation")
            .IsRequired();

        // Geographic properties
        builder.Property(e => e.DiasporaRegion)
            .HasColumnName("diaspora_region")
            .HasMaxLength(100);

        builder.Property(e => e.TimeZone)
            .HasColumnName("timezone")
            .HasMaxLength(100);

        // Delivery tracking timestamps
        builder.Property(e => e.SentAt)
            .HasColumnName("sent_at")
            .IsRequired(false);

        builder.Property(e => e.DeliveredAt)
            .HasColumnName("delivered_at")
            .IsRequired(false);

        builder.Property(e => e.ReadAt)
            .HasColumnName("read_at")
            .IsRequired(false);

        builder.Property(e => e.FailedAt)
            .HasColumnName("failed_at")
            .IsRequired(false);

        builder.Property(e => e.ScheduledFor)
            .HasColumnName("scheduled_for")
            .IsRequired(false);

        // Error tracking
        builder.Property(e => e.ErrorMessage)
            .HasColumnName("error_message")
            .HasMaxLength(1000);

        builder.Property(e => e.RetryCount)
            .HasColumnName("retry_count")
            .IsRequired();

        builder.Property(e => e.MaxRetries)
            .HasColumnName("max_retries")
            .IsRequired()
            .HasDefaultValue(3);

        // Provider/Meta tracking
        builder.Property(e => e.AcsMessageId)
            .HasColumnName("acs_message_id")
            .HasMaxLength(200);

        builder.Property(e => e.MetaMessageId)
            .HasColumnName("meta_message_id")
            .HasMaxLength(200);

        // Phase 7B: BSP provider tracking
        builder.Property(e => e.Provider)
            .HasColumnName("provider")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired(false);

        // Foreign keys (nullable)
        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired(false);

        builder.Property(e => e.EventId)
            .HasColumnName("event_id")
            .IsRequired(false);

        // Audit-only references — NO FK constraints (deliberate per architect C5)
        builder.Property(e => e.RegistrationId)
            .HasColumnName("registration_id")
            .IsRequired(false);

        builder.Property(e => e.NewsletterId)
            .HasColumnName("newsletter_id")
            .IsRequired(false);

        // Audit timestamps from BaseEntity
        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        // Ignore computed properties
        builder.Ignore(e => e.IsRead);
        builder.Ignore(e => e.IsFailed);
        builder.Ignore(e => e.IsDelivered);
        builder.Ignore(e => e.CanRetry);

        // Foreign key relationships
        builder.HasOne<LankaConnect.Domain.Users.User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("FK_WhatsAppMessages_Users_UserId");

        builder.HasOne<LankaConnect.Domain.Events.Event>()
            .WithMany()
            .HasForeignKey(e => e.EventId)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("FK_WhatsAppMessages_Events_EventId");

        // Performance indexes
        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_WhatsAppMessages_UserId");

        builder.HasIndex(e => e.EventId)
            .HasDatabaseName("IX_WhatsAppMessages_EventId");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_WhatsAppMessages_Status");

        builder.HasIndex(e => new { e.Status, e.ScheduledFor })
            .HasDatabaseName("IX_WhatsAppMessages_Status_ScheduledFor");

        builder.HasIndex(e => e.TemplateName)
            .HasDatabaseName("IX_WhatsAppMessages_TemplateName");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_WhatsAppMessages_CreatedAt");

        builder.HasIndex(e => e.ToPhoneNumber)
            .HasDatabaseName("IX_WhatsAppMessages_ToPhoneNumber");

        builder.HasIndex(e => e.AcsMessageId)
            .HasDatabaseName("IX_WhatsAppMessages_AcsMessageId");
    }
}
