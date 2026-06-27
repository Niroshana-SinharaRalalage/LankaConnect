using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for <see cref="TierAssignment"/> — the polymorphic junction between
/// a <see cref="TicketTier"/> and either a <see cref="VenueZone"/> or a <see cref="VenueTable"/>.
/// Table: events.tier_assignments (Slice 4 Release N).
/// Composite primary key: (tier_id, assignable_kind, assignable_id).
/// </summary>
public class TierAssignmentConfiguration : IEntityTypeConfiguration<TierAssignment>
{
    public void Configure(EntityTypeBuilder<TierAssignment> builder)
    {
        builder.ToTable("tier_assignments");

        // Composite PK — no BaseEntity.Id; uniqueness is (tier, kind, target).
        builder.HasKey(a => new { a.TierId, a.AssignableKind, a.AssignableId });

        builder.Property(a => a.TierId)
            .HasColumnName("tier_id")
            .IsRequired();

        builder.Property(a => a.AssignableKind)
            .HasColumnName("assignable_kind")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(a => a.AssignableId)
            .HasColumnName("assignable_id")
            .IsRequired();

        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        // Reverse-lookup index: "who owns this zone/table?" — queries polymorphically by target.
        builder.HasIndex(a => new { a.AssignableKind, a.AssignableId })
            .HasDatabaseName("ix_tier_assignments_assignable");
    }
}
