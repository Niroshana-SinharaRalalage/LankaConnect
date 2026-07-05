using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
namespace LankaConnect.Products.LankaEvents.Infrastructure.Configurations;

/// <summary>
/// EF Core configuration for MetroArea entity
/// Maps to events.metro_areas table (read-only)
/// </summary>
public class MetroAreaConfiguration : IEntityTypeConfiguration<MetroArea>
{
    public void Configure(EntityTypeBuilder<MetroArea> builder)
    {
        builder.ToTable("metro_areas", "events");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id)
            .HasColumnName("id")
            .ValueGeneratedNever(); // UUIDs are generated externally

        builder.Property(m => m.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(m => m.State)
            .HasColumnName("state")
            .HasMaxLength(2)
            .IsRequired();

        builder.Property(m => m.CenterLatitude)
            .HasColumnName("center_latitude")
            .HasPrecision(10, 8)
            .IsRequired();

        builder.Property(m => m.CenterLongitude)
            .HasColumnName("center_longitude")
            .HasPrecision(11, 8)
            .IsRequired();

        builder.Property(m => m.RadiusMiles)
            .HasColumnName("radius_miles")
            .IsRequired();

        builder.Property(m => m.IsStateLevelArea)
            .HasColumnName("is_state_level_area")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(m => m.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(m => m.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(m => m.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired(false);

        // Wave4.9.2.10a Phase 1.10a (2026-06-09): physical CreatedBy/UpdatedBy.
        builder.Property(m => m.CreatedBy).HasColumnName("created_by").HasColumnType("text");
        builder.Property(m => m.UpdatedBy).HasColumnName("updated_by").HasColumnType("text");

        // Indexes
        builder.HasIndex(m => m.State)
            .HasDatabaseName("idx_metro_areas_state");

        builder.HasIndex(m => m.IsActive)
            .HasDatabaseName("idx_metro_areas_is_active");

        builder.HasIndex(m => m.Name)
            .HasDatabaseName("idx_metro_areas_name");

        // Ignore domain events (read-only entity, no domain events)
        builder.Ignore(m => m.DomainEvents);
    }
}
