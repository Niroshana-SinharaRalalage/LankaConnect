using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Infrastructure.Data.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for TicketTier entity.
/// Table: ticket_tiers
/// </summary>
public class TicketTierConfiguration : IEntityTypeConfiguration<TicketTier>
{
    public void Configure(EntityTypeBuilder<TicketTier> builder)
    {
        builder.ToTable("ticket_tiers");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .ValueGeneratedNever();

        builder.Property(t => t.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(t => t.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        // Configure AdultPrice using MoneyConverter (JSON string column)
        // Using Property + converter instead of ComplexProperty to avoid shared-type entity conflicts
        builder.Property(t => t.AdultPrice)
            .HasColumnName("adult_price")
            .HasConversion(new NonNullableMoneyConverter())
            .HasMaxLength(100)
            .IsRequired();

        // Configure ChildPrice as nullable Money using MoneyConverter (JSON string column)
        builder.Property(t => t.ChildPrice)
            .HasColumnName("child_price")
            .HasConversion(new MoneyConverter())
            .HasMaxLength(100);

        builder.Property(t => t.ChildAgeLimit)
            .HasColumnName("child_age_limit");

        builder.Property(t => t.Capacity)
            .HasColumnName("capacity")
            .IsRequired();

        builder.Property(t => t.ReservedCount)
            .HasColumnName("reserved_count")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(t => t.MaxPerUser)
            .HasColumnName("max_per_user")
            .IsRequired()
            .HasDefaultValue(10);

        builder.Property(t => t.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(t => t.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        // Audit fields
        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(t => t.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        // Indexes
        builder.HasIndex(t => t.EventId)
            .HasDatabaseName("ix_ticket_tiers_event_id");

        builder.HasIndex(t => new { t.EventId, t.Name })
            .IsUnique()
            .HasDatabaseName("ix_ticket_tiers_event_id_name")
            .HasFilter("is_active = true");

        // Slice 4 Release N: polymorphic tier assignments
        builder.HasMany(t => t.Assignments)
            .WithOne()
            .HasForeignKey(a => a.TierId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(t => t.Assignments)
            .HasField("_assignments")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Ignore computed properties
        builder.Ignore(t => t.AvailableQuantity);
        builder.Ignore(t => t.HasChildPricing);
        builder.Ignore(t => t.IsFree);
    }
}
