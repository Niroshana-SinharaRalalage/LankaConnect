using LankaConnect.Domain.Events.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for the EventEmailGroupLink junction CLR entity.
/// Wave 5.4.c.0 (2026-06-13). Replaces the Phase 6A.32
/// <c>UsingEntity&lt;Dictionary&lt;string, object&gt;&gt;</c> typed-nav
/// configuration on Event. Same physical table, columns, composite PK,
/// indexes — EF-snapshot-only rebaseline.
/// </summary>
/// <remarks>
/// Entity-level config only. The Event-side relationship
/// (<c>HasMany(e =&gt; e.EmailGroupLinks)</c>) lives in
/// <c>EventConfiguration</c> and follows the standard codebase pattern
/// for backing-field navigations (mirrors Images / Videos / SignUpLists).
/// </remarks>
public class EventEmailGroupLinkConfiguration : IEntityTypeConfiguration<EventEmailGroupLink>
{
    public void Configure(EntityTypeBuilder<EventEmailGroupLink> builder)
    {
        builder.ToTable("event_email_groups");

        builder.HasKey(l => new { l.EventId, l.EmailGroupId });

        builder.Property(l => l.EventId)
            .HasColumnName("event_id");

        builder.Property(l => l.EmailGroupId)
            .HasColumnName("email_group_id");

        builder.Property(l => l.AssignedAt)
            .HasColumnName("assigned_at")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(l => l.EventId);
        builder.HasIndex(l => l.EmailGroupId);
    }
}
