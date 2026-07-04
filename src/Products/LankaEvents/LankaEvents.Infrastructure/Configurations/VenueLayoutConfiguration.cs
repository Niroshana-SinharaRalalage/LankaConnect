using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LankaConnect.Products.LankaEvents.Infrastructure.Configurations;

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

        // Wave4.9.2.10b Phase 1.10b (2026-06-09): physical CreatedBy/UpdatedBy.
        builder.Property(v => v.CreatedBy).HasColumnName("created_by").HasColumnType("text");
        builder.Property(v => v.UpdatedBy).HasColumnName("updated_by").HasColumnType("text");

        // Canvas configuration persisted as flat columns (not ToJson() — avoids
        // Phase 6A.130 where OwnsOne().ToJson() cannot deserialize IReadOnlyList
        // collections; OwnsOne flat columns work fine for scalar value objects).
        builder.OwnsOne(v => v.Canvas, canvas =>
        {
            canvas.Property(c => c.Width)
                .HasColumnName("canvas_width")
                .IsRequired()
                .HasDefaultValue(1200);

            canvas.Property(c => c.Height)
                .HasColumnName("canvas_height")
                .IsRequired()
                .HasDefaultValue(800);

            canvas.Property(c => c.Scale)
                .HasColumnName("canvas_scale")
                .IsRequired()
                .HasDefaultValue(1.0);

            canvas.Property(c => c.BackgroundColor)
                .HasColumnName("canvas_bg_color")
                .HasMaxLength(9)
                .IsRequired()
                .HasDefaultValue("#ffffff");
        });
        builder.Navigation(v => v.Canvas).IsRequired();

        // Relationships
        builder.HasMany(v => v.Zones)
            .WithOne()
            .HasForeignKey(z => z.VenueLayoutId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.Tables)
            .WithOne()
            .HasForeignKey(t => t.VenueLayoutId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.Decorations)
            .WithOne()
            .HasForeignKey(d => d.VenueLayoutId)
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
