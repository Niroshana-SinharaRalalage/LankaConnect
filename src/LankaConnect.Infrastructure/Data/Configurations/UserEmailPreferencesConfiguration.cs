using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Communications.Entities;

namespace LankaConnect.Infrastructure.Data.Configurations;

public class UserEmailPreferencesConfiguration : IEntityTypeConfiguration<UserEmailPreferences>
{
    public void Configure(EntityTypeBuilder<UserEmailPreferences> builder)
    {
        builder.ToTable("user_email_preferences", "communications");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        // Configure UserId with foreign key to Users table
        builder.Property(e => e.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        // Configure preference flags with meaningful defaults
        builder.Property(e => e.AllowMarketing)
            .HasColumnName("allow_marketing")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(e => e.AllowNotifications)
            .HasColumnName("allow_notifications")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.AllowNewsletters)
            .HasColumnName("allow_newsletters")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.AllowTransactional)
            .HasColumnName("allow_transactional")
            .IsRequired()
            .HasDefaultValue(true);

        // Configure language and timezone preferences
        builder.Property(e => e.PreferredLanguage)
            .HasColumnName("preferred_language")
            .HasMaxLength(10)
            .HasDefaultValue("en-US");

        // TimeZone configuration - store as string for cross-platform compatibility
        builder.Property(e => e.TimeZone)
            .HasColumnName("timezone")
            .HasMaxLength(100)
            .HasConversion(
                tz => tz != null ? tz.Id : null,
                tzId => tzId != null ? TimeZoneInfo.FindSystemTimeZoneById(tzId) : null
            );

        // Configure audit fields from BaseEntity
        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        // Foreign key relationship to Users table
        builder.HasOne<LankaConnect.Domain.Users.User>()
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_UserEmailPreferences_Users_UserId");

        // Unique constraint - one preference record per user
        builder.HasIndex(e => e.UserId)
            .IsUnique()
            .HasDatabaseName("IX_UserEmailPreferences_UserId_Unique");

        // Performance indexes for common query patterns
        builder.HasIndex(e => e.AllowMarketing)
            .HasDatabaseName("IX_UserEmailPreferences_AllowMarketing");

        builder.HasIndex(e => e.AllowNotifications)
            .HasDatabaseName("IX_UserEmailPreferences_AllowNotifications");

        builder.HasIndex(e => e.PreferredLanguage)
            .HasDatabaseName("IX_UserEmailPreferences_PreferredLanguage");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_UserEmailPreferences_CreatedAt");
    }
}