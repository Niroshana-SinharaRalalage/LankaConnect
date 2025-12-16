using LankaConnect.Domain.Badges;
using LankaConnect.Domain.Badges.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for Badge entity
/// Phase 6A.25: Badge Management System
/// </summary>
public class BadgeConfiguration : IEntityTypeConfiguration<Badge>
{
    public void Configure(EntityTypeBuilder<Badge> builder)
    {
        builder.ToTable("badges");

        // Primary key
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .IsRequired()
            .ValueGeneratedNever(); // Domain generates IDs

        // Properties
        builder.Property(b => b.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(b => b.ImageUrl)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(b => b.BlobName)
            .HasMaxLength(255)
            .IsRequired();

        // Phase 6A.31a: Keep Position field for backward compatibility during two-phase migration
#pragma warning disable CS0618
        builder.Property(b => b.Position)
            .IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => (BadgePosition)Enum.Parse(typeof(BadgePosition), v))
            .HasMaxLength(20);
#pragma warning restore CS0618

        // Phase 6A.31a: Owned entity mapping for location-specific badge configurations
        // Using individual columns per ADR-1 (not JSON) for type safety and query performance
        builder.OwnsOne(b => b.ListingConfig, cfg =>
        {
            cfg.Property(c => c.PositionX)
                .HasColumnName("position_x_listing")
                .HasColumnType("decimal(5,4)")
                .IsRequired()
                .HasDefaultValue(1.0m); // Default: right edge

            cfg.Property(c => c.PositionY)
                .HasColumnName("position_y_listing")
                .HasColumnType("decimal(5,4)")
                .IsRequired()
                .HasDefaultValue(0.0m); // Default: top edge

            cfg.Property(c => c.SizeWidth)
                .HasColumnName("size_width_listing")
                .HasColumnType("decimal(5,4)")
                .IsRequired()
                .HasDefaultValue(0.26m); // Default: 26% width

            cfg.Property(c => c.SizeHeight)
                .HasColumnName("size_height_listing")
                .HasColumnType("decimal(5,4)")
                .IsRequired()
                .HasDefaultValue(0.26m); // Default: 26% height

            cfg.Property(c => c.Rotation)
                .HasColumnName("rotation_listing")
                .HasColumnType("decimal(5,2)")
                .IsRequired()
                .HasDefaultValue(0.0m); // Default: no rotation
        });

        builder.OwnsOne(b => b.FeaturedConfig, cfg =>
        {
            cfg.Property(c => c.PositionX)
                .HasColumnName("position_x_featured")
                .HasColumnType("decimal(5,4)")
                .IsRequired()
                .HasDefaultValue(1.0m);

            cfg.Property(c => c.PositionY)
                .HasColumnName("position_y_featured")
                .HasColumnType("decimal(5,4)")
                .IsRequired()
                .HasDefaultValue(0.0m);

            cfg.Property(c => c.SizeWidth)
                .HasColumnName("size_width_featured")
                .HasColumnType("decimal(5,4)")
                .IsRequired()
                .HasDefaultValue(0.26m);

            cfg.Property(c => c.SizeHeight)
                .HasColumnName("size_height_featured")
                .HasColumnType("decimal(5,4)")
                .IsRequired()
                .HasDefaultValue(0.26m);

            cfg.Property(c => c.Rotation)
                .HasColumnName("rotation_featured")
                .HasColumnType("decimal(5,2)")
                .IsRequired()
                .HasDefaultValue(0.0m);
        });

        builder.OwnsOne(b => b.DetailConfig, cfg =>
        {
            cfg.Property(c => c.PositionX)
                .HasColumnName("position_x_detail")
                .HasColumnType("decimal(5,4)")
                .IsRequired()
                .HasDefaultValue(1.0m);

            cfg.Property(c => c.PositionY)
                .HasColumnName("position_y_detail")
                .HasColumnType("decimal(5,4)")
                .IsRequired()
                .HasDefaultValue(0.0m);

            cfg.Property(c => c.SizeWidth)
                .HasColumnName("size_width_detail")
                .HasColumnType("decimal(5,4)")
                .IsRequired()
                .HasDefaultValue(0.21m);

            cfg.Property(c => c.SizeHeight)
                .HasColumnName("size_height_detail")
                .HasColumnType("decimal(5,4)")
                .IsRequired()
                .HasDefaultValue(0.21m);

            cfg.Property(c => c.Rotation)
                .HasColumnName("rotation_detail")
                .HasColumnType("decimal(5,2)")
                .IsRequired()
                .HasDefaultValue(0.0m);
        });

        builder.Property(b => b.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(b => b.IsSystem)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(b => b.DisplayOrder)
            .IsRequired();

        builder.Property(b => b.CreatedByUserId);

        // Phase 6A.28: Default duration in days for badge assignments
        builder.Property(b => b.DefaultDurationDays)
            .IsRequired(false);

        // Audit fields from BaseEntity
        builder.Property(b => b.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(b => b.UpdatedAt);

        // Indexes
        builder.HasIndex(b => b.Name)
            .IsUnique()
            .HasDatabaseName("IX_Badges_Name");

        builder.HasIndex(b => b.IsActive)
            .HasDatabaseName("IX_Badges_IsActive");

        builder.HasIndex(b => b.DisplayOrder)
            .HasDatabaseName("IX_Badges_DisplayOrder");

        builder.HasIndex(b => b.IsSystem)
            .HasDatabaseName("IX_Badges_IsSystem");

        // Phase 6A.28: Index for duration queries (removed ExpiresAt index as expiration moved to EventBadge)
        builder.HasIndex(b => b.DefaultDurationDays)
            .HasDatabaseName("IX_Badges_DefaultDurationDays")
            .HasFilter("\"DefaultDurationDays\" IS NOT NULL");
    }
}
