using LankaConnect.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for EventVideo entity
/// Part of Event aggregate - configured as owned entity with cascade delete
/// </summary>
public class EventVideoConfiguration : IEntityTypeConfiguration<EventVideo>
{
    public void Configure(EntityTypeBuilder<EventVideo> builder)
    {
        builder.ToTable("EventVideos");

        // Primary key
        builder.HasKey(ev => ev.Id);

        builder.Property(ev => ev.Id)
            .IsRequired()
            .ValueGeneratedNever(); // Domain generates IDs

        // Properties
        builder.Property(ev => ev.EventId)
            .IsRequired();

        builder.Property(ev => ev.VideoUrl)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(ev => ev.BlobName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(ev => ev.ThumbnailUrl)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(ev => ev.ThumbnailBlobName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(ev => ev.Duration)
            .IsRequired(false); // Nullable TimeSpan

        builder.Property(ev => ev.Format)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(ev => ev.FileSizeBytes)
            .IsRequired();

        builder.Property(ev => ev.DisplayOrder)
            .IsRequired();

        builder.Property(ev => ev.UploadedAt)
            .IsRequired();

        // Audit fields from BaseEntity
        builder.Property(ev => ev.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(ev => ev.UpdatedAt);

        // Indexes
        builder.HasIndex(ev => new { ev.EventId, ev.DisplayOrder })
            .IsUnique()
            .HasDatabaseName("IX_EventVideos_EventId_DisplayOrder");

        builder.HasIndex(ev => ev.EventId)
            .HasDatabaseName("IX_EventVideos_EventId");

        // Relationship to Event (cascade delete)
        // Navigation is configured in EventConfiguration (one-to-many)
    }
}
