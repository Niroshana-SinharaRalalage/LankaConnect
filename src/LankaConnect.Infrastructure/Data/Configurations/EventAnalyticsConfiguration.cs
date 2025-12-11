using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Analytics;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for EventAnalytics aggregate
/// </summary>
public class EventAnalyticsConfiguration : IEntityTypeConfiguration<EventAnalytics>
{
    public void Configure(EntityTypeBuilder<EventAnalytics> builder)
    {
        builder.ToTable("event_analytics");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        // Configure properties
        builder.Property(a => a.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(a => a.TotalViews)
            .HasColumnName("total_views")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(a => a.UniqueViewers)
            .HasColumnName("unique_viewers")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(a => a.RegistrationCount)
            .HasColumnName("registration_count")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(a => a.ShareCount)
            .HasColumnName("share_count")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(a => a.LastViewedAt)
            .HasColumnName("last_viewed_at")
            .HasColumnType("timestamp with time zone");

        // Configure audit fields
        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(a => a.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        // Ignore calculated property (not stored in database)
        builder.Ignore(a => a.ConversionRate);

        // Configure indexes
        builder.HasIndex(a => a.EventId)
            .IsUnique()
            .HasDatabaseName("ix_event_analytics_event_id_unique");

        builder.HasIndex(a => a.TotalViews)
            .HasDatabaseName("ix_event_analytics_total_views");

        builder.HasIndex(a => a.LastViewedAt)
            .HasDatabaseName("ix_event_analytics_last_viewed_at");
    }
}
