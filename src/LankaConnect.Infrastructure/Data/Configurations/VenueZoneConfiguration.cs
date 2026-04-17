using LankaConnect.Domain.Events.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LankaConnect.Infrastructure.Data.Configurations;

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

        builder.Property(z => z.TicketTierId)
            .HasColumnName("ticket_tier_id");

        builder.Property(z => z.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired()
            .HasDefaultValue(0);

        // Audit fields
        builder.Property(z => z.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(z => z.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

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

        builder.HasIndex(z => z.TicketTierId)
            .HasDatabaseName("ix_venue_zones_ticket_tier_id")
            .HasFilter("ticket_tier_id IS NOT NULL");

        // Ignore computed properties
        builder.Ignore(z => z.EnabledSeatCount);
    }
}
