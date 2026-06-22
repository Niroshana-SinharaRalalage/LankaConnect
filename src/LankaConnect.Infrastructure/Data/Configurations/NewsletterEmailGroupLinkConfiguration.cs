using LankaConnect.Domain.Communications.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for the NewsletterEmailGroupLink junction CLR entity.
/// Wave 5.4.d.1b (2026-06-22). Replaces the Phase 6A.74
/// <c>UsingEntity&lt;Dictionary&lt;string, object&gt;&gt;</c> typed-nav
/// configuration on Newsletter. Same physical table
/// <c>communications.newsletter_email_groups</c>, columns, composite PK,
/// indexes — EF-snapshot-only rebaseline. Mirrors
/// <see cref="EventEmailGroupLinkConfiguration"/>.
/// </summary>
public class NewsletterEmailGroupLinkConfiguration : IEntityTypeConfiguration<NewsletterEmailGroupLink>
{
    public void Configure(EntityTypeBuilder<NewsletterEmailGroupLink> builder)
    {
        builder.ToTable("newsletter_email_groups", "communications");

        builder.HasKey(l => new { l.NewsletterId, l.EmailGroupId });

        builder.Property(l => l.NewsletterId)
            .HasColumnName("newsletter_id");

        builder.Property(l => l.EmailGroupId)
            .HasColumnName("email_group_id");

        builder.Property(l => l.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("CURRENT_TIMESTAMP");

        builder.HasIndex(l => l.NewsletterId);
        builder.HasIndex(l => l.EmailGroupId);
    }
}
