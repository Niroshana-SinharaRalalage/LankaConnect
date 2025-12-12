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

        builder.Property(b => b.Position)
            .IsRequired()
            .HasConversion(
                v => v.ToString(),
                v => (BadgePosition)Enum.Parse(typeof(BadgePosition), v))
            .HasMaxLength(20);

        builder.Property(b => b.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(b => b.IsSystem)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(b => b.DisplayOrder)
            .IsRequired();

        builder.Property(b => b.CreatedByUserId);

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
    }
}
