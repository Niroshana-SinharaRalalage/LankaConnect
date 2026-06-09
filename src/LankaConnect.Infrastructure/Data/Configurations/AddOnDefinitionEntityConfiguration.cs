using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for AddOnDefinition entity.
/// Defines purchasable add-ons available for an event.
/// </summary>
public class AddOnDefinitionEntityConfiguration : IEntityTypeConfiguration<AddOnDefinition>
{
    public void Configure(EntityTypeBuilder<AddOnDefinition> builder)
    {
        builder.ToTable("add_on_definitions", "events");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        // Foreign keys
        builder.Property(a => a.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        // Add-on definition fields
        builder.Property(a => a.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(a => a.Description)
            .HasColumnName("description")
            .HasMaxLength(1000)
            .IsRequired(false);

        // Price as Money owned entity
        builder.OwnsOne(a => a.Price, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("price_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("price_currency")
                .HasConversion<string>()
                .HasMaxLength(3)
                .IsRequired();
        });

        // Quantity tracking
        builder.Property(a => a.QuantityLimit)
            .HasColumnName("quantity_limit")
            .IsRequired(false);

        builder.Property(a => a.QuantitySold)
            .HasColumnName("quantity_sold")
            .IsRequired()
            .HasDefaultValue(0);

        // Active/sort
        builder.Property(a => a.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(a => a.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired()
            .HasDefaultValue(0);

        // Phase 6A.143 — optional add-on image. Both columns nullable; either both
        // populated together or both null. Handler enforces atomic set/clear.
        builder.Property(a => a.ImageUrl)
            .HasColumnName("image_url")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(a => a.ImageBlobName)
            .HasColumnName("image_blob_name")
            .HasColumnType("text")
            .IsRequired(false);

        // Audit fields
        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(a => a.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        // Wave4.9.2.10d Phase 1.10d (2026-06-09): physical CreatedBy/UpdatedBy.
        builder.Property(a => a.CreatedBy).HasColumnName("created_by").HasColumnType("text");
        builder.Property(a => a.UpdatedBy).HasColumnName("updated_by").HasColumnType("text");

        // Indexes
        builder.HasIndex(a => a.EventId)
            .HasDatabaseName("ix_add_on_definitions_event_id");

        builder.HasIndex(a => a.IsActive)
            .HasDatabaseName("ix_add_on_definitions_is_active");

        // Foreign key relationships
        builder.HasOne<Event>()
            .WithMany()
            .HasForeignKey(a => a.EventId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
