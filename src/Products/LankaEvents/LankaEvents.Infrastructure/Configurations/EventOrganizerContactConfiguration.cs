using LankaConnect.Products.LankaEvents.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LankaConnect.Products.LankaEvents.Infrastructure.Configurations;

/// <summary>
/// EF Core configuration for EventOrganizerContact entity
/// Supports multiple organizer contacts per event with primary contact designation
/// </summary>
public class EventOrganizerContactConfiguration : IEntityTypeConfiguration<EventOrganizerContact>
{
    public void Configure(EntityTypeBuilder<EventOrganizerContact> builder)
    {
        builder.ToTable("event_organizer_contacts", "events");

        // Primary key
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .IsRequired()
            .ValueGeneratedNever();

        // Properties
        builder.Property(c => c.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(c => c.ContactName)
            .HasColumnName("contact_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.ContactEmail)
            .HasColumnName("contact_email")
            .HasMaxLength(255)
            .IsRequired(false);

        builder.Property(c => c.ContactPhone)
            .HasColumnName("contact_phone")
            .HasMaxLength(20)
            .IsRequired(false);

        builder.Property(c => c.IsPrimary)
            .HasColumnName("is_primary")
            .HasDefaultValue(false);

        builder.Property(c => c.LinkedUserId)
            .HasColumnName("linked_user_id")
            .IsRequired(false);

        builder.Property(c => c.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0);

        // Audit fields from BaseEntity
        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at");

        // Wave4.9.2.10a Phase 1.10a (2026-06-09): physical CreatedBy/UpdatedBy.
        builder.Property(c => c.CreatedBy).HasColumnName("created_by").HasColumnType("text");
        builder.Property(c => c.UpdatedBy).HasColumnName("updated_by").HasColumnType("text");

        // Indexes
        builder.HasIndex(c => c.EventId)
            .HasDatabaseName("ix_event_organizer_contacts_event_id");

        // Phase 6A.133: Partial index for co-organizer lookups
        // Enables efficient "find all events where user is a co-organizer" queries
        builder.HasIndex(c => c.LinkedUserId)
            .HasDatabaseName("ix_event_organizer_contacts_linked_user_id")
            .HasFilter("linked_user_id IS NOT NULL");
    }
}
