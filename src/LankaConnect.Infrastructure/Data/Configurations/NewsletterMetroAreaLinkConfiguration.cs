using LankaConnect.Modules.Communications.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LankaConnect.SPLIT_PER_ENTITY.Configurations;

/// <summary>
/// EF Core configuration for the NewsletterMetroAreaLink junction CLR entity.
/// Wave 5.2.d-hotfix2 (2026-06-28). Replaces the Phase 6A.74 typed-nav
/// configuration on Newsletter._metroAreaEntities that broke at runtime
/// after the W5.1 MetroArea move to Products.LankaEvents.Domain. Same physical
/// table <c>communications.newsletter_metro_areas</c>, columns, composite PK,
/// indexes -- EF-snapshot-only rebaseline. Mirrors
/// <see cref="NewsletterEmailGroupLinkConfiguration"/>.
/// </summary>
public class NewsletterMetroAreaLinkConfiguration : IEntityTypeConfiguration<NewsletterMetroAreaLink>
{
    public void Configure(EntityTypeBuilder<NewsletterMetroAreaLink> builder)
    {
        builder.ToTable("newsletter_metro_areas", "communications");

        builder.HasKey(l => new { l.NewsletterId, l.MetroAreaId });

        builder.Property(l => l.NewsletterId)
            .HasColumnName("newsletter_id");

        builder.Property(l => l.MetroAreaId)
            .HasColumnName("metro_area_id");

        builder.Property(l => l.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(l => l.NewsletterId);
        builder.HasIndex(l => l.MetroAreaId);
    }
}
