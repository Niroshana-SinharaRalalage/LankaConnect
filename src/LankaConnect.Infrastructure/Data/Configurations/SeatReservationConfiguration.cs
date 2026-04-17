using LankaConnect.Domain.Events.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for SeatReservation entity.
/// Table: seat_reservations (events schema)
/// Unique index on seat_id prevents double-booking.
/// </summary>
public class SeatReservationConfiguration : IEntityTypeConfiguration<SeatReservation>
{
    public void Configure(EntityTypeBuilder<SeatReservation> builder)
    {
        builder.ToTable("seat_reservations");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedNever();

        builder.Property(r => r.SeatId)
            .HasColumnName("seat_id")
            .IsRequired();

        builder.Property(r => r.RegistrationId)
            .HasColumnName("registration_id")
            .IsRequired();

        builder.Property(r => r.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(r => r.AttendeeIndex)
            .HasColumnName("attendee_index")
            .IsRequired();

        // Audit fields
        builder.Property(r => r.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(r => r.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        // CRITICAL: Unique index on seat_id — prevents double-booking
        builder.HasIndex(r => r.SeatId)
            .IsUnique()
            .HasDatabaseName("ix_seat_reservations_seat_id");

        builder.HasIndex(r => r.RegistrationId)
            .HasDatabaseName("ix_seat_reservations_registration_id");

        builder.HasIndex(r => r.EventId)
            .HasDatabaseName("ix_seat_reservations_event_id");
    }
}
