using LankaConnect.Products.LankaEvents.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace LankaConnect.Products.LankaEvents.Infrastructure.Configurations;

/// <summary>
/// EF Core configuration for VenueTable entity (Slice 2+3A).
/// Table: venue_tables (events schema)
/// </summary>
public class VenueTableConfiguration : IEntityTypeConfiguration<VenueTable>
{
    public void Configure(EntityTypeBuilder<VenueTable> builder)
    {
        builder.ToTable("venue_tables", "events"); // Rule 5i: explicit schema

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .ValueGeneratedNever();

        builder.Property(t => t.VenueLayoutId)
            .HasColumnName("venue_layout_id")
            .IsRequired();

        builder.Property(t => t.VenueZoneId)
            .HasColumnName("venue_zone_id");

        builder.Property(t => t.Label)
            .HasColumnName("label")
            .HasMaxLength(VenueTable.MaxLabelLength)
            .IsRequired();

        builder.Property(t => t.Shape)
            .HasColumnName("shape")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Geometry stored as raw JSONB string; no ValueComparer needed because the
        // property is an immutable string (see Phase 6A.129 — ValueComparer only
        // required for mutable collections backed by JSONB).
        builder.Property(t => t.Geometry)
            .HasColumnName("geometry")
            .HasColumnType("jsonb")
            .IsRequired()
            .HasDefaultValue("{}");

        builder.Property(t => t.Capacity)
            .HasColumnName("capacity")
            .IsRequired();

        builder.Property(t => t.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(t => t.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        // Wave4.9.2.10b Phase 1.10b (2026-06-09): physical CreatedBy/UpdatedBy.
        builder.Property(t => t.CreatedBy).HasColumnName("created_by").HasColumnType("text");
        builder.Property(t => t.UpdatedBy).HasColumnName("updated_by").HasColumnType("text");

        // Seats attached to a table have VenueTableId set (and VenueZoneId = null).
        builder.HasMany(t => t.Seats)
            .WithOne()
            .HasForeignKey(s => s.VenueTableId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => t.VenueLayoutId)
            .HasDatabaseName("ix_venue_tables_venue_layout_id");

        builder.HasIndex(t => t.VenueZoneId)
            .HasDatabaseName("ix_venue_tables_venue_zone_id")
            .HasFilter("venue_zone_id IS NOT NULL");

        builder.HasIndex(t => new { t.VenueLayoutId, t.Label })
            .IsUnique()
            .HasDatabaseName("ix_venue_tables_layout_id_label");

        builder.Ignore(t => t.EnabledSeatCount);
    }
}
