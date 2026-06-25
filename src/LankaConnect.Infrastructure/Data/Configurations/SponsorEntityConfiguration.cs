using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for Sponsor entity.
/// Supports dual mode: monetary sponsorship (via Stripe) and item sponsorship.
/// </summary>
public class SponsorEntityConfiguration : IEntityTypeConfiguration<Sponsor>
{
    public void Configure(EntityTypeBuilder<Sponsor> builder)
    {
        // Phase 6A.156 — DB-level CHECK constraint enforces the
        // "package-set => snapshots populated" invariant captured in the
        // domain factory (Sponsor.CreatePackageSponsor lands in 6A.157). A
        // null sponsorship_package_id short-circuits the check, so all
        // existing rows (generic-flow sponsors with NULL FK) remain valid
        // without backfill.
        builder.ToTable("sponsors", "events", t =>
        {
            t.HasCheckConstraint(
                "chk_sponsors_package_snapshot",
                "(sponsorship_package_id IS NULL) OR (package_name_snapshot IS NOT NULL AND package_price_amount_snapshot IS NOT NULL)");
        });

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        // Foreign keys
        builder.Property(s => s.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        // Sponsor type (Money or Item)
        builder.Property(s => s.Type)
            .HasColumnName("sponsor_type")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        // Sponsor information
        builder.Property(s => s.SponsorUserId)
            .HasColumnName("sponsor_user_id")
            .IsRequired(false);

        builder.Property(s => s.SponsorName)
            .HasColumnName("sponsor_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.SponsorEmail)
            .HasColumnName("sponsor_email")
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(s => s.SponsorPhone)
            .HasColumnName("sponsor_phone")
            .HasMaxLength(30)
            .IsRequired(false);

        builder.Property(s => s.SponsorOrganization)
            .HasColumnName("sponsor_organization")
            .HasMaxLength(300)
            .IsRequired(false);

        builder.Property(s => s.SponsorNotes)
            .HasColumnName("sponsor_notes")
            .HasMaxLength(1000)
            .IsRequired(false);

        // Sponsor amount as Money owned entity (nullable - not applicable for item sponsors)
        builder.OwnsOne(s => s.Amount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("amount")
                .HasPrecision(18, 2);

            money.Property(m => m.Currency)
                .HasColumnName("amount_currency")
                .HasConversion<string>()
                .HasMaxLength(3);
        });

        // Status enum
        builder.Property(s => s.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        // Stripe payment fields (for monetary sponsors)
        builder.Property(s => s.StripeCheckoutSessionId)
            .HasColumnName("stripe_checkout_session_id")
            .HasMaxLength(500);

        builder.Property(s => s.StripePaymentIntentId)
            .HasColumnName("stripe_payment_intent_id")
            .HasMaxLength(200);

        // Revenue breakdown as Money owned entities (nullable - populated after payment)
        builder.OwnsOne(s => s.StripeFeeAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("stripe_fee_amount")
                .HasPrecision(18, 2);

            money.Property(m => m.Currency)
                .HasColumnName("stripe_fee_currency")
                .HasConversion<string>()
                .HasMaxLength(3);
        });

        builder.OwnsOne(s => s.PlatformCommissionAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("platform_commission_amount")
                .HasPrecision(18, 2);

            money.Property(m => m.Currency)
                .HasColumnName("platform_commission_currency")
                .HasConversion<string>()
                .HasMaxLength(3);
        });

        builder.OwnsOne(s => s.OrganizerPayoutAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("organizer_payout_amount")
                .HasPrecision(18, 2);

            money.Property(m => m.Currency)
                .HasColumnName("organizer_payout_currency")
                .HasConversion<string>()
                .HasMaxLength(3);
        });

        // Timestamps (for monetary sponsors via Stripe)
        builder.Property(s => s.CheckoutExpiresAt)
            .HasColumnName("checkout_expires_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(s => s.PaymentCompletedAt)
            .HasColumnName("payment_completed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(s => s.FailedAt)
            .HasColumnName("failed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(s => s.AbandonedAt)
            .HasColumnName("abandoned_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(s => s.RefundedAt)
            .HasColumnName("refunded_at")
            .HasColumnType("timestamp with time zone");

        // Item sponsorship fields
        builder.Property(s => s.ItemName)
            .HasColumnName("item_name")
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(s => s.ItemDescription)
            .HasColumnName("item_description")
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(s => s.EstimatedValue)
            .HasColumnName("estimated_value")
            .HasPrecision(18, 2)
            .IsRequired(false);

        builder.Property(s => s.RecordedAt)
            .HasColumnName("recorded_at")
            .HasColumnType("timestamp with time zone");

        // Phase 6A.145 — optional sponsor image (LOGO). Any sponsor can attach (no threshold).
        // Both columns nullable; either both populated together or both null. Handler enforces atomicity.
        builder.Property(s => s.ImageUrl)
            .HasColumnName("image_url")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(s => s.ImageBlobName)
            .HasColumnName("image_blob_name")
            .HasColumnType("text")
            .IsRequired(false);

        // Phase 6A.162 — optional sponsor BROCHURE / FLYER (sibling slot to logo).
        // Same atomic pair semantics as the logo columns; the two slots are orthogonal
        // (touching one MUST NOT mutate the other — pinned by SponsorTests independence
        // invariants). Migration Phase6A162_AddSponsorBrochure adds both columns nullable.
        builder.Property(s => s.BrochureUrl)
            .HasColumnName("brochure_url")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(s => s.BrochureBlobName)
            .HasColumnName("brochure_blob_name")
            .HasColumnType("text")
            .IsRequired(false);

        // Audit fields
        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        // Wave4.9.2.10a Phase 1.10a (2026-06-09): physical CreatedBy/UpdatedBy.
        builder.Property(s => s.CreatedBy).HasColumnName("created_by").HasColumnType("text");
        builder.Property(s => s.UpdatedBy).HasColumnName("updated_by").HasColumnType("text");

        // Phase 6A.151 — human-edit audit columns. Distinct from UpdatedAt
        // which also fires on lifecycle transitions (CompletePayment etc.).
        // Both null until the first content edit (UpdateContactFields /
        // UpdateAmount / UpdateItemDetails / UpdateName); set together by
        // Sponsor.MarkEdited().
        builder.Property(s => s.LastEditedAt)
            .HasColumnName("last_edited_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(s => s.LastEditedBy)
            .HasColumnName("last_edited_by")
            .HasColumnType("uuid")
            .IsRequired(false);

        // Phase 6A.156 — Sponsorship Package linkage (nullable, additive).
        // Generic sponsorship = SponsorshipPackageId IS NULL; packaged
        // sponsorship (6A.157+) populates the FK + snapshot columns. CHECK
        // constraint at table level enforces the "package set => snapshots
        // populated" invariant.
        builder.Property(s => s.SponsorshipPackageId)
            .HasColumnName("sponsorship_package_id")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(s => s.RegistrationId)
            .HasColumnName("registration_id")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(s => s.PackageNameSnapshot)
            .HasColumnName("package_name_snapshot")
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(s => s.PackageTierSnapshot)
            .HasColumnName("package_tier_snapshot")
            .HasMaxLength(100)
            .IsRequired(false);

        // Package price snapshot as nullable Money owned entity (mirrors the
        // existing Amount / StripeFeeAmount / etc. pattern). Two columns:
        // package_price_amount_snapshot + package_price_currency_snapshot.
        builder.OwnsOne(s => s.PackagePriceSnapshot, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("package_price_amount_snapshot")
                .HasPrecision(18, 2);

            money.Property(m => m.Currency)
                .HasColumnName("package_price_currency_snapshot")
                .HasConversion<string>()
                .HasMaxLength(3);
        });

        builder.Property(s => s.IncludedTicketCountSnapshot)
            .HasColumnName("included_ticket_count_snapshot")
            .IsRequired(false);

        // Indexes
        builder.HasIndex(s => s.EventId)
            .HasDatabaseName("ix_sponsors_event_id");

        // Phase 6A.156 — filter indexes for the new FKs. Partial indexes
        // (WHERE column IS NOT NULL) keep the index slim because the vast
        // majority of sponsor rows are generic-flow (NULL FK).
        builder.HasIndex(s => s.SponsorshipPackageId)
            .HasDatabaseName("ix_sponsors_sponsorship_package_id")
            .HasFilter("\"sponsorship_package_id\" IS NOT NULL");

        builder.HasIndex(s => s.RegistrationId)
            .HasDatabaseName("ix_sponsors_registration_id")
            .HasFilter("\"registration_id\" IS NOT NULL");

        builder.HasIndex(s => s.SponsorUserId)
            .HasDatabaseName("ix_sponsors_sponsor_user_id");

        builder.HasIndex(s => s.Status)
            .HasDatabaseName("ix_sponsors_status");

        builder.HasIndex(s => s.Type)
            .HasDatabaseName("ix_sponsors_sponsor_type");

        builder.HasIndex(s => s.StripeCheckoutSessionId)
            .HasDatabaseName("ix_sponsors_checkout_session");

        builder.HasIndex(s => s.StripePaymentIntentId)
            .HasDatabaseName("ix_sponsors_payment_intent");

        // Foreign key relationships
        builder.HasOne<Event>()
            .WithMany()
            .HasForeignKey(s => s.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        // Phase 6A.156 — FK to SponsorshipPackage with ON DELETE SET NULL so
        // that organizer-deleted packages preserve historical sponsor receipts
        // (snapshots already carry the name/tier/price at purchase time).
        builder.HasOne<SponsorshipPackage>()
            .WithMany()
            .HasForeignKey(s => s.SponsorshipPackageId)
            .OnDelete(DeleteBehavior.SetNull);

        // Phase 6A.156 — FK to Registration with ON DELETE SET NULL mirroring
        // the AddOnPurchase.RegistrationId pattern (events/add_on_purchases).
        // A canceled / refunded registration drops the link but keeps the
        // sponsor record intact for revenue history.
        builder.HasOne<Registration>()
            .WithMany()
            .HasForeignKey(s => s.RegistrationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
