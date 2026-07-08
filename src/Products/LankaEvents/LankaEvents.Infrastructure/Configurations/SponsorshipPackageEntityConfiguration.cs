using Microsoft.EntityFrameworkCore;
using LankaConnect.SharedKernel.Money;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
namespace LankaConnect.Products.LankaEvents.Infrastructure.Configurations;

/// <summary>
/// Phase 6A.156 — EF Core configuration for <see cref="SponsorshipPackage"/>.
/// Mirrors <see cref="AddOnDefinitionEntityConfiguration"/> with three additions:
/// <list type="bullet">
///   <item><c>tier</c> — nullable varchar(100) free-text label.</item>
///   <item><c>perks</c> — Postgres <c>text[]</c> array column (Npgsql maps
///         <see cref="List{T}"/> of string directly to text[]).</item>
///   <item><c>included_ticket_count</c> — non-null int default 0.</item>
/// </list>
///
/// Schema: <c>events.sponsorship_packages</c>. FK to <c>events.events</c> with
/// <c>OnDelete(Cascade)</c> — when an event is deleted, its packages go with it
/// (no historical reporting need for orphan packages of deleted events).
/// </summary>
public class SponsorshipPackageEntityConfiguration : IEntityTypeConfiguration<SponsorshipPackage>
{
    public void Configure(EntityTypeBuilder<SponsorshipPackage> builder)
    {
        builder.ToTable("sponsorship_packages", "events");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        // Event linkage
        builder.Property(p => p.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        // Package details
        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(SponsorshipPackage.MAX_NAME_LENGTH)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasColumnName("description")
            .HasMaxLength(SponsorshipPackage.MAX_DESCRIPTION_LENGTH)
            .IsRequired(false);

        // Price as Money owned entity (same pattern as AddOnDefinition)
        builder.OwnsOne(p => p.Price, money =>
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

        // Stock tracking — quantity_sold is managed by atomic SQL in the repository,
        // not EF Core change tracking. Do NOT mutate via domain methods.
        builder.Property(p => p.QuantityLimit)
            .HasColumnName("quantity_limit")
            .IsRequired(false);

        builder.Property(p => p.QuantitySold)
            .HasColumnName("quantity_sold")
            .IsRequired()
            .HasDefaultValue(0);

        // Active / sort
        builder.Property(p => p.IsActive)
            .HasColumnName("is_active")
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(p => p.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired()
            .HasDefaultValue(0);

        // Optional image (nullable pair, atomic set via domain method)
        builder.Property(p => p.ImageUrl)
            .HasColumnName("image_url")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(p => p.ImageBlobName)
            .HasColumnName("image_blob_name")
            .HasColumnType("text")
            .IsRequired(false);

        // Phase 6A.156 — package-specific axes
        builder.Property(p => p.Tier)
            .HasColumnName("tier")
            .HasMaxLength(SponsorshipPackage.MAX_TIER_LENGTH)
            .IsRequired(false);

        // Postgres text[] array via Npgsql. Domain enforces count + per-entry length.
        builder.Property(p => p.Perks)
            .HasColumnName("perks")
            .HasColumnType("text[]")
            .IsRequired(false);

        builder.Property(p => p.IncludedTicketCount)
            .HasColumnName("included_ticket_count")
            .IsRequired()
            .HasDefaultValue(0);

        // Audit fields
        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        // Wave4.9.2.10a Phase 1.10a (2026-06-09): physical CreatedBy/UpdatedBy.
        builder.Property(p => p.CreatedBy).HasColumnName("created_by").HasColumnType("text");
        builder.Property(p => p.UpdatedBy).HasColumnName("updated_by").HasColumnType("text");

        // Indexes — narrow event-scoped queries are by far the hot path
        builder.HasIndex(p => p.EventId)
            .HasDatabaseName("ix_sponsorship_packages_event_id");

        builder.HasIndex(p => new { p.EventId, p.IsActive, p.SortOrder })
            .HasDatabaseName("ix_sponsorship_packages_event_active_sort");

        // FK to Event — cascade on delete because packages have no historical
        // reporting value once the event is gone (no payout reconciliation pulls
        // from this table — sponsor revenue lives on the Sponsor aggregate which
        // has its own FK semantics).
        builder.HasOne<Event>()
            .WithMany()
            .HasForeignKey(p => p.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
