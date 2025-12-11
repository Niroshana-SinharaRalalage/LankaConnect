using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// Phase 6A.8: Event Template System
/// EF Core configuration for EventTemplate entity
/// Maps to events.event_templates table
/// </summary>
public class EventTemplateConfiguration : IEntityTypeConfiguration<EventTemplate>
{
    public void Configure(EntityTypeBuilder<EventTemplate> builder)
    {
        builder.ToTable("event_templates", "events");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever(); // UUIDs are generated externally for seeding

        builder.Property(e => e.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.Category)
            .HasColumnName("category")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.ThumbnailSvg)
            .HasColumnName("thumbnail_svg")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(e => e.TemplateDataJson)
            .HasColumnName("template_data_json")
            .HasColumnType("jsonb") // Use PostgreSQL JSONB for better query performance
            .IsRequired();

        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.DisplayOrder)
            .HasColumnName("display_order")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired(false);

        // Indexes for efficient querying
        builder.HasIndex(e => e.Category)
            .HasDatabaseName("idx_event_templates_category");

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("idx_event_templates_is_active");

        builder.HasIndex(e => e.DisplayOrder)
            .HasDatabaseName("idx_event_templates_display_order");

        // Composite index for filtering active templates by category
        builder.HasIndex(e => new { e.IsActive, e.Category, e.DisplayOrder })
            .HasDatabaseName("idx_event_templates_active_category_order");

        // Ignore domain events (templates are relatively static, minimal events)
        builder.Ignore(e => e.DomainEvents);
    }
}
