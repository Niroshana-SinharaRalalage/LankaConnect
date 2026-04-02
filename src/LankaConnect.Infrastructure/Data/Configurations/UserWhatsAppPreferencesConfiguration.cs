using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Communications.Entities;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// Phase 7A: EF Core configuration for UserWhatsAppPreferences entity.
/// Maps to communications.user_whatsapp_preferences table.
/// </summary>
public class UserWhatsAppPreferencesConfiguration : IEntityTypeConfiguration<UserWhatsAppPreferences>
{
    public void Configure(EntityTypeBuilder<UserWhatsAppPreferences> builder)
    {
        builder.ToTable("user_whatsapp_preferences", "communications");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        // Configure UserId with foreign key to Users table
        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(e => e.WhatsAppEnabled)
            .HasColumnName("whatsapp_enabled")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.WhatsAppPhoneNumber)
            .HasColumnName("whatsapp_phone_number")
            .HasMaxLength(20);

        builder.Property(e => e.PhoneVerified)
            .HasColumnName("phone_verified")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.PhoneVerifiedAt)
            .HasColumnName("phone_verified_at")
            .IsRequired(false);

        builder.Property(e => e.VerificationCode)
            .HasColumnName("verification_code")
            .HasMaxLength(10);

        builder.Property(e => e.VerificationCodeExpires)
            .HasColumnName("verification_code_expires")
            .IsRequired(false);

        builder.Property(e => e.VerificationAttempts)
            .HasColumnName("verification_attempts")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.VerificationLockedUntil)
            .HasColumnName("verification_locked_until")
            .IsRequired(false);

        // Per-notification preferences
        builder.Property(e => e.NotifyEventRegistration)
            .HasColumnName("notify_event_registration")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.NotifyEventReminder)
            .HasColumnName("notify_event_reminder")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.NotifyEventCancellation)
            .HasColumnName("notify_event_cancellation")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.NotifyEventUpdate)
            .HasColumnName("notify_event_update")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.NotifySignupCommitment)
            .HasColumnName("notify_signup_commitment")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.NotifyRefund)
            .HasColumnName("notify_refund")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.NotifyNewsletter)
            .HasColumnName("notify_newsletter")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.NotifyNewEvent)
            .HasColumnName("notify_new_event")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.NotifyPayment)
            .HasColumnName("notify_payment")
            .IsRequired()
            .HasDefaultValue(true);

        // Cultural preferences
        builder.Property(e => e.PreferredLanguage)
            .HasColumnName("preferred_language")
            .HasMaxLength(10)
            .IsRequired()
            .HasDefaultValue("en");

        // TimeOnly properties — store as TIME type in PostgreSQL
        builder.Property(e => e.QuietHoursStart)
            .HasColumnName("quiet_hours_start")
            .HasColumnType("time")
            .IsRequired(false);

        builder.Property(e => e.QuietHoursEnd)
            .HasColumnName("quiet_hours_end")
            .HasColumnType("time")
            .IsRequired(false);

        builder.Property(e => e.RespectCulturalTiming)
            .HasColumnName("respect_cultural_timing")
            .IsRequired()
            .HasDefaultValue(true);

        // Audit timestamps from BaseEntity
        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        // Ignore computed properties
        builder.Ignore(e => e.IsFullyVerified);
        builder.Ignore(e => e.IsLocked);

        // Foreign key relationship to Users table with CASCADE delete
        builder.HasOne<LankaConnect.Domain.Users.User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_UserWhatsAppPreferences_Users_UserId");

        // Unique constraint — one preference record per user
        builder.HasIndex(e => e.UserId)
            .IsUnique()
            .HasDatabaseName("IX_UserWhatsAppPreferences_UserId_Unique");

        // Partial index on phone where enabled=true AND verified=true
        // Useful for finding verified users for notification dispatch
        builder.HasIndex(e => e.WhatsAppPhoneNumber)
            .HasDatabaseName("IX_UserWhatsAppPreferences_Phone_EnabledVerified")
            .HasFilter("whatsapp_enabled = true AND phone_verified = true");

        // Performance indexes
        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_UserWhatsAppPreferences_CreatedAt");
    }
}
