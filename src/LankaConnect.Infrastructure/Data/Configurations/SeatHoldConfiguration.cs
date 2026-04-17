using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for SeatHold entity.
/// Table: seat_holds (events schema)
/// Partial unique index on (seat_id) WHERE status = 'Active' prevents double-holds.
/// </summary>
public class SeatHoldConfiguration : IEntityTypeConfiguration<SeatHold>
{
    public void Configure(EntityTypeBuilder<SeatHold> builder)
    {
        builder.ToTable("seat_holds");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id)
            .ValueGeneratedNever();

        builder.Property(h => h.SeatId)
            .HasColumnName("seat_id")
            .IsRequired();

        builder.Property(h => h.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(h => h.SessionId)
            .HasColumnName("session_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(h => h.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(SeatHoldStatus.Active);

        builder.Property(h => h.HeldAt)
            .HasColumnName("held_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(h => h.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Audit fields
        builder.Property(h => h.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(h => h.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        // CRITICAL: Partial unique index — only ONE active hold per seat
        builder.HasIndex(h => h.SeatId)
            .IsUnique()
            .HasDatabaseName("ix_seat_holds_seat_id_active")
            .HasFilter("status = 'Active'");

        builder.HasIndex(h => h.UserId)
            .HasDatabaseName("ix_seat_holds_user_id");

        builder.HasIndex(h => h.SessionId)
            .HasDatabaseName("ix_seat_holds_session_id");

        builder.HasIndex(h => new { h.Status, h.ExpiresAt })
            .HasDatabaseName("ix_seat_holds_status_expires_at")
            .HasFilter("status = 'Active'");

        // Ignore computed properties
        builder.Ignore(h => h.IsExpired);
    }
}
