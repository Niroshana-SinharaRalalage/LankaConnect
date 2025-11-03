using LankaConnect.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for EventImage entity
/// Part of Event aggregate - configured as owned entity with cascade delete
/// </summary>
public class EventImageConfiguration : IEntityTypeConfiguration<EventImage>
{
    public void Configure(EntityTypeBuilder<EventImage> builder)
    {
        builder.ToTable("EventImages");

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

        builder.Property(ei => ei.UploadedAt)
            .IsRequired();

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
