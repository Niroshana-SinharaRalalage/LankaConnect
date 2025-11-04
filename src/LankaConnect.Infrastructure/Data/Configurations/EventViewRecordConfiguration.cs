using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Analytics;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for EventViewRecord entity
/// </summary>
public class EventViewRecordConfiguration : IEntityTypeConfiguration<EventViewRecord>
{
    public void Configure(EntityTypeBuilder<EventViewRecord> builder)
    {
        builder.ToTable("event_view_records");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        // Configure properties
        builder.Property(v => v.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(v => v.UserId)
            .HasColumnName("user_id");

        builder.Property(v => v.IpAddress)
            .HasColumnName("ip_address")
            .HasMaxLength(45) // IPv6 max length
            .IsRequired();

        builder.Property(v => v.UserAgent)
            .HasColumnName("user_agent")
            .HasMaxLength(500);

        builder.Property(v => v.ViewedAt)
            .HasColumnName("viewed_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // Configure indexes for performance
        builder.HasIndex(v => v.EventId)
            .HasDatabaseName("ix_event_view_records_event_id");

        builder.HasIndex(v => v.UserId)
            .HasDatabaseName("ix_event_view_records_user_id");

        builder.HasIndex(v => v.IpAddress)
            .HasDatabaseName("ix_event_view_records_ip_address");

        builder.HasIndex(v => v.ViewedAt)
            .HasDatabaseName("ix_event_view_records_viewed_at");

        // Composite index for deduplication queries
        builder.HasIndex(v => new { v.EventId, v.UserId, v.ViewedAt })
            .HasDatabaseName("ix_event_view_records_dedup_user");

        builder.HasIndex(v => new { v.EventId, v.IpAddress, v.ViewedAt })
            .HasDatabaseName("ix_event_view_records_dedup_ip");
    }
}
