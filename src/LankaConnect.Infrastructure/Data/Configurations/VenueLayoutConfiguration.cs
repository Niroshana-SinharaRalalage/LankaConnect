using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for VenueLayout aggregate root.
/// Table: venue_layouts (events schema)
/// </summary>
public class VenueLayoutConfiguration : IEntityTypeConfiguration<VenueLayout>
{
    public void Configure(EntityTypeBuilder<VenueLayout> builder)
    {
        builder.ToTable("venue_layouts");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Id)
            .ValueGeneratedNever();

        builder.Property(v => v.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(v => v.EventId)
            .HasColumnName("event_id");

        builder.Property(v => v.LayoutType)
            .HasColumnName("layout_type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(v => v.IsTemplate)
            .HasColumnName("is_template")
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(v => v.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        // Optimistic concurrency via PostgreSQL xmin system column
        builder.Property(v => v.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        // Audit fields
        builder.Property(v => v.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(v => v.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        // Relationships
        builder.HasMany(v => v.Zones)
            .WithOne()
            .HasForeignKey(z => z.VenueLayoutId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(v => v.EventId)
            .HasDatabaseName("ix_venue_layouts_event_id")
            .HasFilter("event_id IS NOT NULL");

        builder.HasIndex(v => v.CreatedByUserId)
            .HasDatabaseName("ix_venue_layouts_created_by_user_id");

        builder.HasIndex(v => new { v.EventId, v.Name })
            .IsUnique()
            .HasDatabaseName("ix_venue_layouts_event_id_name")
            .HasFilter("event_id IS NOT NULL");

        // Ignore computed properties
        builder.Ignore(v => v.TotalCapacity);
    }
}
