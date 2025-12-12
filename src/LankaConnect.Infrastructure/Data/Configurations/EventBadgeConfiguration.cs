using LankaConnect.Domain.Events.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for EventBadge join entity
/// Phase 6A.25: Badge Management System
/// </summary>
public class EventBadgeConfiguration : IEntityTypeConfiguration<EventBadge>
{
    public void Configure(EntityTypeBuilder<EventBadge> builder)
    {
        builder.ToTable("event_badges");

        // Primary key
        builder.HasKey(eb => eb.Id);

        builder.Property(eb => eb.Id)
            .IsRequired()
            .ValueGeneratedNever(); // Domain generates IDs

        // Properties
        builder.Property(eb => eb.EventId)
            .IsRequired();

        builder.Property(eb => eb.BadgeId)
            .IsRequired();

        builder.Property(eb => eb.AssignedAt)
            .IsRequired();

        builder.Property(eb => eb.AssignedByUserId)
            .IsRequired();

        // Audit fields from BaseEntity
        builder.Property(eb => eb.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(eb => eb.UpdatedAt);

        // Indexes
        builder.HasIndex(eb => new { eb.EventId, eb.BadgeId })
            .IsUnique()
            .HasDatabaseName("IX_EventBadges_EventId_BadgeId");

        builder.HasIndex(eb => eb.EventId)
            .HasDatabaseName("IX_EventBadges_EventId");

        builder.HasIndex(eb => eb.BadgeId)
            .HasDatabaseName("IX_EventBadges_BadgeId");

        // Relationships
        builder.HasOne(eb => eb.Badge)
            .WithMany()
            .HasForeignKey(eb => eb.BadgeId)
            .OnDelete(DeleteBehavior.Restrict); // Don't cascade delete badges when event badge is deleted

        // Note: Event relationship is not configured here because EventBadge
        // is part of the Event aggregate and managed through Event navigation
    }
}
