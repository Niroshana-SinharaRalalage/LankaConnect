using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Events;
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
        builder.ToTable("sponsors", "events");

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

        // Phase 6A.145 — optional sponsor image. Any sponsor can attach (no threshold).
        // Both columns nullable; either both populated together or both null. Handler enforces atomicity.
        builder.Property(s => s.ImageUrl)
            .HasColumnName("image_url")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(s => s.ImageBlobName)
            .HasColumnName("image_blob_name")
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

        // Indexes
        builder.HasIndex(s => s.EventId)
            .HasDatabaseName("ix_sponsors_event_id");

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
    }
}
