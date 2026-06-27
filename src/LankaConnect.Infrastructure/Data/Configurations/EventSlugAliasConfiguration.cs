using LankaConnect.Domain.Events.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// Phase 6A.154: EF Core configuration for retired vanity slug aliases.
///
/// Table: <c>events.event_slug_aliases</c> (matches the `events` schema all
/// other event-owned tables use). Append-only — aliases are never deleted
/// except via cascade when the parent event row is hard-deleted (which is
/// rare; events soft-delete via Cancelled status).
/// </summary>
public class EventSlugAliasConfiguration : IEntityTypeConfiguration<EventSlugAlias>
{
    public void Configure(EntityTypeBuilder<EventSlugAlias> builder)
    {
        builder.ToTable("event_slug_aliases", "events");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        // Alias values share the slug shape (lowercase ASCII, 3–80 chars).
        // We don't repeat the VO validation here — values are written only
        // via Event.SetVanitySlug, which receives a pre-validated VO.
        builder.Property(a => a.Alias)
            .HasColumnName("alias")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(a => a.ActivatedAt)
            .HasColumnName("activated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(a => a.RetiredAt)
            .HasColumnName("retired_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Unique on alias — an alias can only ever belong to ONE event. The
        // SSR slug-route lookup hits this index too (lookup by alias →
        // resolve canonical → emit 301).
        builder.HasIndex(a => a.Alias)
            .IsUnique()
            .HasDatabaseName("ix_event_slug_aliases_alias_unique");

        // Secondary index for the organizer-side "show my retired slugs"
        // query — fetches a single event's alias history. Cheap; aliases
        // table is write-light.
        builder.HasIndex(a => a.EventId)
            .HasDatabaseName("ix_event_slug_aliases_event_id");

        // Inherited BaseEntity audit columns
        builder.Property(a => a.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(a => a.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        // Wave4.9.2.10a Phase 1.10a (2026-06-09): physical CreatedBy/UpdatedBy.
        builder.Property(a => a.CreatedBy).HasColumnName("created_by").HasColumnType("text");
        builder.Property(a => a.UpdatedBy).HasColumnName("updated_by").HasColumnType("text");
    }
}
