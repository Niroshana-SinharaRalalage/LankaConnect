using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Events.Entities;

namespace LankaConnect.Infrastructure.Data.Configurations;

public class EventPassConfiguration : IEntityTypeConfiguration<EventPass>
{
    public void Configure(EntityTypeBuilder<EventPass> builder)
    {
        builder.ToTable("event_passes");

        builder.HasKey(ep => ep.Id);

        builder.Property(ep => ep.Id)
            .ValueGeneratedNever();

        // Configure PassName value object using ComplexProperty (EF Core 8)
        builder.ComplexProperty(ep => ep.Name, name =>
        {
            name.Property(n => n.Value)
                .HasColumnName("name")
                .HasMaxLength(100)
                .IsRequired();
        });

        // Configure PassDescription value object using ComplexProperty
        builder.ComplexProperty(ep => ep.Description, description =>
        {
            description.Property(d => d.Value)
                .HasColumnName("description")
                .HasMaxLength(500)
                .IsRequired();
        });

        // Configure Price as owned Money value object using ComplexProperty
        builder.ComplexProperty(ep => ep.Price, price =>
        {
            price.Property(p => p.Amount)
                .HasColumnName("price_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            price.Property(p => p.Currency)
                .HasColumnName("price_currency")
                .HasConversion<string>()
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(ep => ep.TotalQuantity)
            .HasColumnName("total_quantity")
            .IsRequired();

        builder.Property(ep => ep.ReservedQuantity)
            .HasColumnName("reserved_quantity")
            .IsRequired()
            .HasDefaultValue(0);

        // Foreign key (shadow property from relationship in EventConfiguration)
        builder.Property<Guid>("EventId")
            .HasColumnName("event_id")
            .IsRequired();

        // Configure audit fields
        builder.Property(ep => ep.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(ep => ep.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        // Indexes
        builder.HasIndex("EventId")
            .HasDatabaseName("ix_event_passes_event_id");

        // Note: Complex properties (Name, Description) cannot be directly indexed
        // If needed, create raw SQL indexes in migrations
    }
}
