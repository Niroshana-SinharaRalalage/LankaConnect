using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Notifications;
using LankaConnect.Domain.Notifications.Enums;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for Notification entity
/// Phase 6A.6: Notification System
/// </summary>
public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Id)
            .ValueGeneratedNever();

        // Configure UserId (required FK to User)
        builder.Property(n => n.UserId)
            .IsRequired();

        // Create index on UserId for efficient querying
        builder.HasIndex(n => n.UserId)
            .HasDatabaseName("ix_notifications_user_id");

        // Create composite index on UserId and IsRead for efficient unread queries
        builder.HasIndex(n => new { n.UserId, n.IsRead })
            .HasDatabaseName("ix_notifications_user_id_is_read");

        // Configure Title
        builder.Property(n => n.Title)
            .HasMaxLength(200)
            .IsRequired();

        // Configure Message
        builder.Property(n => n.Message)
            .HasMaxLength(1000)
            .IsRequired();

        // Configure Type (enum stored as integer)
        builder.Property(n => n.Type)
            .HasConversion<int>()
            .IsRequired();

        // Configure IsRead
        builder.Property(n => n.IsRead)
            .IsRequired()
            .HasDefaultValue(false);

        // Configure ReadAt (optional)
        builder.Property(n => n.ReadAt)
            .IsRequired(false);

        // Configure RelatedEntityId (optional)
        builder.Property(n => n.RelatedEntityId)
            .HasMaxLength(100)
            .IsRequired(false);

        // Configure RelatedEntityType (optional)
        builder.Property(n => n.RelatedEntityType)
            .HasMaxLength(100)
            .IsRequired(false);

        // Configure timestamps
        builder.Property(n => n.CreatedAt)
            .IsRequired();

        builder.Property(n => n.UpdatedAt)
            .IsRequired(false);

        // Create index on CreatedAt for efficient sorting
        builder.HasIndex(n => n.CreatedAt)
            .HasDatabaseName("ix_notifications_created_at");
    }
}
