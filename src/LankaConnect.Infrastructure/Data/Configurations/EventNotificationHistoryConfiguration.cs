using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Events.Entities;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for EventNotificationHistory entity
/// Phase 6A.61: Manual event notification history tracking
/// </summary>
public class EventNotificationHistoryConfiguration : IEntityTypeConfiguration<EventNotificationHistory>
{
    public void Configure(EntityTypeBuilder<EventNotificationHistory> builder)
    {
        builder.ToTable("event_notification_history", "communications");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id)
            .ValueGeneratedNever()
            .HasColumnName("id");

        builder.Property(h => h.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(h => h.SentByUserId)
            .HasColumnName("sent_by_user_id")
            .IsRequired();

        builder.Property(h => h.SentAt)
            .HasColumnName("sent_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(h => h.RecipientCount)
            .HasColumnName("recipient_count")
            .IsRequired();

        builder.Property(h => h.SuccessfulSends)
            .HasColumnName("successful_sends")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(h => h.FailedSends)
            .HasColumnName("failed_sends")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(h => h.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(h => h.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        // Foreign key to events table (CASCADE delete - if event is deleted, history should be deleted)
        builder.HasOne<Domain.Events.Event>()
            .WithMany()
            .HasForeignKey(h => h.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        // Foreign key to users table (RESTRICT delete - preserve history even if user is deleted)
        builder.HasOne<Domain.Users.User>()
            .WithMany()
            .HasForeignKey(h => h.SentByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for query performance
        builder.HasIndex(h => h.EventId)
            .HasDatabaseName("ix_event_notification_history_event_id");

        builder.HasIndex(h => h.SentAt)
            .IsDescending()
            .HasDatabaseName("ix_event_notification_history_sent_at_desc");
    }
}
