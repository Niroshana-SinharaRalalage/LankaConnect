using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for Collection entity.
/// Part of the standalone Collection system for events.
/// </summary>
public class CollectionEntityConfiguration : IEntityTypeConfiguration<Collection>
{
    public void Configure(EntityTypeBuilder<Collection> builder)
    {
        builder.ToTable("collections", "events");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        // Foreign keys
        builder.Property(c => c.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        // Contributor information
        builder.Property(c => c.ContributorUserId)
            .HasColumnName("contributor_user_id")
            .IsRequired(false);

        builder.Property(c => c.ContributorName)
            .HasColumnName("contributor_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.ContributorEmail)
            .HasColumnName("contributor_email")
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(c => c.ContributorPhone)
            .HasColumnName("contributor_phone")
            .HasMaxLength(30)
            .IsRequired(false);

        builder.Property(c => c.ContributorNotes)
            .HasColumnName("contributor_notes")
            .HasMaxLength(1000)
            .IsRequired(false);

        // Collection amount as Money owned entity
        builder.OwnsOne(c => c.Amount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("amount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("amount_currency")
                .HasConversion<string>()
                .HasMaxLength(3)
                .IsRequired();
        });

        // Status enum
        builder.Property(c => c.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        // Stripe payment fields
        builder.Property(c => c.StripeCheckoutSessionId)
            .HasColumnName("stripe_checkout_session_id")
            .HasMaxLength(500);

        builder.Property(c => c.StripePaymentIntentId)
            .HasColumnName("stripe_payment_intent_id")
            .HasMaxLength(200);

        // Revenue breakdown as Money owned entities (nullable - populated after payment)
        builder.OwnsOne(c => c.StripeFeeAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("stripe_fee_amount")
                .HasPrecision(18, 2);

            money.Property(m => m.Currency)
                .HasColumnName("stripe_fee_currency")
                .HasConversion<string>()
                .HasMaxLength(3);
        });

        builder.OwnsOne(c => c.PlatformCommissionAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("platform_commission_amount")
                .HasPrecision(18, 2);

            money.Property(m => m.Currency)
                .HasColumnName("platform_commission_currency")
                .HasConversion<string>()
                .HasMaxLength(3);
        });

        builder.OwnsOne(c => c.OrganizerPayoutAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("organizer_payout_amount")
                .HasPrecision(18, 2);

            money.Property(m => m.Currency)
                .HasColumnName("organizer_payout_currency")
                .HasConversion<string>()
                .HasMaxLength(3);
        });

        // Timestamps
        builder.Property(c => c.CheckoutExpiresAt)
            .HasColumnName("checkout_expires_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(c => c.PaymentCompletedAt)
            .HasColumnName("payment_completed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(c => c.FailedAt)
            .HasColumnName("failed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(c => c.AbandonedAt)
            .HasColumnName("abandoned_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(c => c.RefundedAt)
            .HasColumnName("refunded_at")
            .HasColumnType("timestamp with time zone");

        // Audit fields
        builder.Property(c => c.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(c => c.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        // Indexes
        builder.HasIndex(c => c.EventId)
            .HasDatabaseName("ix_collections_event_id");

        builder.HasIndex(c => c.ContributorUserId)
            .HasDatabaseName("ix_collections_contributor_user_id");

        builder.HasIndex(c => c.Status)
            .HasDatabaseName("ix_collections_status");

        builder.HasIndex(c => c.StripeCheckoutSessionId)
            .HasDatabaseName("ix_collections_checkout_session");

        builder.HasIndex(c => c.StripePaymentIntentId)
            .HasDatabaseName("ix_collections_payment_intent");

        // Foreign key relationships
        builder.HasOne<Event>()
            .WithMany()
            .HasForeignKey(c => c.EventId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
