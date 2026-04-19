using LankaConnect.Domain.Events.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for Seat entity.
/// Table: seats (events schema)
/// </summary>
public class SeatConfiguration : IEntityTypeConfiguration<Seat>
{
    public void Configure(EntityTypeBuilder<Seat> builder)
    {
        builder.ToTable("seats");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        // Slice 2+3A: a seat belongs to EITHER a zone OR a table (XOR). Both columns
        // are nullable; a DB CHECK constraint enforces XOR at the database layer —
        // see AddSeatingDomainExpansion migration.
        builder.Property(s => s.VenueZoneId)
            .HasColumnName("venue_zone_id");

        builder.Property(s => s.VenueTableId)
            .HasColumnName("venue_table_id");

        builder.Property(s => s.AngleDeg)
            .HasColumnName("angle_deg");

        builder.Property(s => s.Row)
            .HasColumnName("row")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(s => s.Number)
            .HasColumnName("number")
            .IsRequired();

        builder.Property(s => s.Label)
            .HasColumnName("label")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(s => s.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(s => s.IsEnabled)
            .HasColumnName("is_enabled")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(s => s.IsAccessible)
            .HasColumnName("is_accessible")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(s => s.X)
            .HasColumnName("x");

        builder.Property(s => s.Y)
            .HasColumnName("y");

        // Audit fields
        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        // Indexes
        builder.HasIndex(s => s.VenueZoneId)
            .HasDatabaseName("ix_seats_venue_zone_id")
            .HasFilter("venue_zone_id IS NOT NULL");

        builder.HasIndex(s => s.VenueTableId)
            .HasDatabaseName("ix_seats_venue_table_id")
            .HasFilter("venue_table_id IS NOT NULL");

        // Unique label within a zone — partial index so it only applies to
        // zone-based seats (table-based seats share labels like "T1-S1" / "T2-S1").
        builder.HasIndex(s => new { s.VenueZoneId, s.Label })
            .IsUnique()
            .HasDatabaseName("ix_seats_zone_id_label")
            .HasFilter("venue_zone_id IS NOT NULL");

        // Unique label within a table.
        builder.HasIndex(s => new { s.VenueTableId, s.Label })
            .IsUnique()
            .HasDatabaseName("ix_seats_table_id_label")
            .HasFilter("venue_table_id IS NOT NULL");

        builder.Ignore(s => s.IsZoneSeat);
        builder.Ignore(s => s.IsTableSeat);
    }
}
