using LankaConnect.Products.LankaEvents.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for VenueDecoration entity (Slice 2+3A).
/// Table: venue_decorations (events schema)
/// </summary>
public class VenueDecorationConfiguration : IEntityTypeConfiguration<VenueDecoration>
{
    public void Configure(EntityTypeBuilder<VenueDecoration> builder)
    {
        builder.ToTable("venue_decorations");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .ValueGeneratedNever();

        builder.Property(d => d.VenueLayoutId)
            .HasColumnName("venue_layout_id")
            .IsRequired();

        builder.Property(d => d.Kind)
            .HasColumnName("kind")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(d => d.Label)
            .HasColumnName("label")
            .HasMaxLength(VenueDecoration.MaxLabelLength);

        // Geometry / Properties are raw JSONB strings (immutable), so no
        // ValueComparer is needed (Phase 6A.129 rule applies to mutable collections).
        builder.Property(d => d.Geometry)
            .HasColumnName("geometry")
            .HasColumnType("jsonb")
            .IsRequired()
            .HasDefaultValue("{}");

        builder.Property(d => d.Properties)
            .HasColumnName("properties")
            .HasColumnType("jsonb")
            .IsRequired()
            .HasDefaultValue("{}");

        builder.Property(d => d.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(d => d.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(d => d.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        // Wave4.9.2.10b Phase 1.10b (2026-06-09): physical CreatedBy/UpdatedBy.
        builder.Property(d => d.CreatedBy).HasColumnName("created_by").HasColumnType("text");
        builder.Property(d => d.UpdatedBy).HasColumnName("updated_by").HasColumnType("text");

        builder.HasIndex(d => d.VenueLayoutId)
            .HasDatabaseName("ix_venue_decorations_venue_layout_id");
    }
}
