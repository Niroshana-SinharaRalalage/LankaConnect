using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.ReferenceData.Entities;

namespace LankaConnect.Infrastructure.Data.Configurations.ReferenceData;

/// <summary>
/// EF Core configuration for EventCategoryRef entity
/// Maps to reference_data.event_categories table
/// Phase 6A.47: Reference data migration from hardcoded enums
/// </summary>
public class EventCategoryRefConfiguration : IEntityTypeConfiguration<EventCategoryRef>
{
    public void Configure(EntityTypeBuilder<EventCategoryRef> builder)
    {
        builder.ToTable("event_categories", "reference_data");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever(); // UUIDs are generated externally

        builder.Property(e => e.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(e => e.IconUrl)
            .HasColumnName("icon_url")
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(e => e.DisplayOrder)
            .HasColumnName("display_order")
            .IsRequired();

        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired(false);

        // Indexes
        builder.HasIndex(e => e.Code)
            .IsUnique()
            .HasDatabaseName("idx_event_categories_code");

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("idx_event_categories_is_active");

        builder.HasIndex(e => e.DisplayOrder)
            .HasDatabaseName("idx_event_categories_display_order");

        builder.HasIndex(e => e.Name)
            .HasDatabaseName("idx_event_categories_name");

        // Ignore domain events
        builder.Ignore(e => e.DomainEvents);
    }
}
