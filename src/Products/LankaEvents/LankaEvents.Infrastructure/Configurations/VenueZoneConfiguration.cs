using LankaConnect.Products.LankaEvents.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LankaConnect.Products.LankaEvents.Infrastructure.Configurations;

/// <summary>
/// EF Core configuration for VenueZone entity.
/// Table: venue_zones (events schema)
/// </summary>
public class VenueZoneConfiguration : IEntityTypeConfiguration<VenueZone>
{
    public void Configure(EntityTypeBuilder<VenueZone> builder)
    {
        builder.ToTable("venue_zones");

        builder.HasKey(z => z.Id);

        builder.Property(z => z.Id)
            .ValueGeneratedNever();

        builder.Property(z => z.VenueLayoutId)
            .HasColumnName("venue_layout_id")
            .IsRequired();

        builder.Property(z => z.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(z => z.Color)
            .HasColumnName("color")
            .HasMaxLength(20)
            .IsRequired();

        // Slice 4 Release N: VenueZone.TicketTierId was removed from the domain model.
        // The DB column stays nullable for Release N (dual-read window); Release N+1 drops it.
        // Shadow-property mapping keeps EF aware of the column so migrations don't propose DROP.
        builder.Property<Guid?>("TicketTierId")
            .HasColumnName("ticket_tier_id");

        builder.Property(z => z.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired()
            .HasDefaultValue(0);

        // Slice 2+3A: canvas shape + geometry
        builder.Property(z => z.Shape)
            .HasColumnName("shape")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(LankaConnect.Products.LankaEvents.Domain.Enums.ZoneShape.Rect);

        // Geometry is an immutable raw JSONB string — no ValueComparer needed.
        builder.Property(z => z.Geometry)
            .HasColumnName("geometry")
            .HasColumnType("jsonb")
            .IsRequired()
            .HasDefaultValue("{}");

        // Audit fields
        builder.Property(z => z.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(z => z.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        // Wave4.9.2.10b Phase 1.10b (2026-06-09): physical CreatedBy/UpdatedBy.
        builder.Property(z => z.CreatedBy).HasColumnName("created_by").HasColumnType("text");
        builder.Property(z => z.UpdatedBy).HasColumnName("updated_by").HasColumnType("text");

        // Relationships
        builder.HasMany(z => z.Seats)
            .WithOne()
            .HasForeignKey(s => s.VenueZoneId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(z => z.VenueLayoutId)
            .HasDatabaseName("ix_venue_zones_venue_layout_id");

        builder.HasIndex(z => new { z.VenueLayoutId, z.Name })
            .IsUnique()
            .HasDatabaseName("ix_venue_zones_layout_id_name");

        // Keep the legacy index via shadow property so migrations don't drop it during Release N.
        builder.HasIndex("TicketTierId")
            .HasDatabaseName("ix_venue_zones_ticket_tier_id")
            .HasFilter("ticket_tier_id IS NOT NULL");

        // Ignore computed properties
        builder.Ignore(z => z.EnabledSeatCount);
    }
}
