using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Products.LankaEvents.Infrastructure.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace LankaConnect.Products.LankaEvents.Infrastructure.Configurations;

/// <summary>
/// EF Core configuration for TicketTier entity.
/// Table: ticket_tiers
/// </summary>
/// <remarks>
/// Wave 6.5.e (2026-07-03): the <c>Money</c>-VO ValueConverter references were
/// re-pointed from <c>LankaConnect.Infrastructure.Data.Converters</c> to the
/// LankaEvents-local <c>Products.LankaEvents.Infrastructure.Converters</c>
/// twin. The converter code is byte-identical; the local copy avoids taking a
/// transitional dependency on the legacy <c>LankaConnect.Infrastructure.Data</c>
/// namespace, which would have required expanding the Rule 12 baseline JSON
/// beyond the architect-approved 20-repo set.
/// </remarks>
public class TicketTierConfiguration : IEntityTypeConfiguration<TicketTier>
{
    public void Configure(EntityTypeBuilder<TicketTier> builder)
    {
        // Wave 6.5.f.5-hotfix2b (2026-07-04, Rule 5i.1 REVISED per fourth architect
        // consult): physical Postgres table lives in `public` schema (per creation
        // migration 20260415203751_AddTicketTiers). Single-arg ToTable resolves to
        // null schema — no `HasDefaultSchema("events")` on LankaEventsDbContext to
        // override. Ruling: docs/architect-consults/2026-07-04-wave-6-5-f-hotfix2b-hasdefaultschema-override-ruling.md.
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

        // Wave4.9.2.10a Phase 1.10a (2026-06-09): physical CreatedBy/UpdatedBy.
        // Sprint Day 1 hotfix (2026-07-04): TicketTier was missed in the original sweep,
        // causing 42703 "column t.CreatedBy does not exist" on any Event query that
        // .Include(e => e.TicketTiers). Root cause of ~31 Events smoke failures.
        builder.Property(t => t.CreatedBy).HasColumnName("created_by").HasColumnType("text");
        builder.Property(t => t.UpdatedBy).HasColumnName("updated_by").HasColumnType("text");

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
