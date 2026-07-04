using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LankaConnect.Products.LankaEvents.Infrastructure.Configurations;

/// <summary>
/// EF Core configuration for EventImage entity
/// Part of Event aggregate - configured as owned entity with cascade delete
/// </summary>
public class EventImageConfiguration : IEntityTypeConfiguration<EventImage>
{
    public void Configure(EntityTypeBuilder<EventImage> builder)
    {
        builder.ToTable("EventImages", "events"); // Rule 5i: explicit schema

        // Primary key
        builder.HasKey(ei => ei.Id);

        builder.Property(ei => ei.Id)
            .IsRequired()
            .ValueGeneratedNever(); // Domain generates IDs

        // Properties
        builder.Property(ei => ei.EventId)
            .IsRequired();

        builder.Property(ei => ei.ImageUrl)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(ei => ei.BlobName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(ei => ei.DisplayOrder)
            .IsRequired();

        // Phase 6A.13: Primary image flag
        builder.Property(ei => ei.IsPrimary)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(ei => ei.UploadedAt)
            .IsRequired();

        // Audit fields from BaseEntity
        builder.Property(ei => ei.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(ei => ei.UpdatedAt);

        // Wave4.9.2.10a Phase 1.10a (2026-06-09): physical CreatedBy/UpdatedBy.
        builder.Property(ei => ei.CreatedBy).HasColumnName("created_by").HasColumnType("text");
        builder.Property(ei => ei.UpdatedBy).HasColumnName("updated_by").HasColumnType("text");

        // Indexes
        builder.HasIndex(ei => new { ei.EventId, ei.DisplayOrder })
            .IsUnique()
            .HasDatabaseName("IX_EventImages_EventId_DisplayOrder");

        builder.HasIndex(ei => ei.EventId)
            .HasDatabaseName("IX_EventImages_EventId");

        // Relationship to Event (cascade delete)
        // Navigation is configured in EventConfiguration (one-to-many)
    }
}
