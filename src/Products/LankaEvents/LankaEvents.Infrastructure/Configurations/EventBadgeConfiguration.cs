using LankaConnect.Products.LankaEvents.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace LankaConnect.Products.LankaEvents.Infrastructure.Configurations;

/// <summary>
/// EF Core configuration for EventBadge join entity.
/// Phase 6A.25: Badge Management System.
/// Phase 6A.28: Duration and expiration fields.
/// Wave 6.5.f.5-hotfix (2026-07-04): physically relocated from
/// <c>LankaConnect.SPLIT_PER_ENTITY.Configurations</c> to Products/LankaEvents
/// per architect ruling §2.2. The <c>HasOne(eb =&gt; eb.Badge)</c> block was
/// removed because it forced <c>Badge</c> (a cross-module principal owned by
/// <c>LankaConnect.Domain.Badges</c>) to be mapped in LankaEventsDbContext.
/// <c>BadgeId</c> stays as a scalar FK column; the CLR navigation property
/// <c>EventBadge.Badge</c> becomes unmapped. Cross-module hydration moves to
/// the application layer via <c>IBadgeRepository</c> per Blueprint §7.8.
/// </summary>
public class EventBadgeConfiguration : IEntityTypeConfiguration<EventBadge>
{
    public void Configure(EntityTypeBuilder<EventBadge> builder)
    {
        // Wave 6.5.f.5-hotfix2: cross-schema fix per Rule 5i. Physical Postgres
        // table is `badges.event_badges` per Phase 6A.25 migration — the config
        // relocation in hotfix1 wrongly resolved to `events.event_badges` via
        // LankaEventsDbContext's HasDefaultSchema("events") default.
        builder.ToTable("event_badges", "badges");

        // Primary key
        builder.HasKey(eb => eb.Id);

        builder.Property(eb => eb.Id)
            .IsRequired()
            .ValueGeneratedNever(); // Domain generates IDs

        // Properties
        builder.Property(eb => eb.EventId)
            .IsRequired();

        builder.Property(eb => eb.BadgeId)
            .IsRequired();

        builder.Property(eb => eb.AssignedAt)
            .IsRequired();

        builder.Property(eb => eb.AssignedByUserId)
            .IsRequired();

        // Phase 6A.28: Duration and expiration fields
        builder.Property(eb => eb.DurationDays)
            .IsRequired(false);

        builder.Property(eb => eb.ExpiresAt)
            .IsRequired(false);

        // Audit fields from BaseEntity
        builder.Property(eb => eb.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(eb => eb.UpdatedAt);

        // Wave4.9.2.3 Phase 1.3 (2026-06-08): physical CreatedBy/UpdatedBy
        // on badges.event_badges per Phase1_3_AddCreatedByUpdatedByToBadges.
        builder.Property(eb => eb.CreatedBy)
            .HasColumnName("created_by")
            .HasColumnType("text");

        builder.Property(eb => eb.UpdatedBy)
            .HasColumnName("updated_by")
            .HasColumnType("text");

        // Indexes
        builder.HasIndex(eb => new { eb.EventId, eb.BadgeId })
            .IsUnique()
            .HasDatabaseName("IX_EventBadges_EventId_BadgeId");

        builder.HasIndex(eb => eb.EventId)
            .HasDatabaseName("IX_EventBadges_EventId");

        builder.HasIndex(eb => eb.BadgeId)
            .HasDatabaseName("IX_EventBadges_BadgeId");

        // Phase 6A.28: Index for expired badge queries
        builder.HasIndex(eb => eb.ExpiresAt)
            .HasDatabaseName("IX_EventBadges_ExpiresAt")
            .HasFilter("\"ExpiresAt\" IS NOT NULL");

        // Wave 6.5.f.5-hotfix: HasOne(eb => eb.Badge) block REMOVED. That block
        // forced Badge (cross-module principal) into LankaEventsDbContext's model,
        // which the un-Ignore of EventBadge would then require. Keeping BadgeId as
        // a scalar FK keeps the module boundary clean per Blueprint §7.8.
        // Note: Event relationship is not configured here because EventBadge
        // is part of the Event aggregate and managed through Event navigation.
    }
}
