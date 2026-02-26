using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for Donation entity.
/// Part of the standalone Donation system for events.
/// </summary>
public class DonationEntityConfiguration : IEntityTypeConfiguration<Donation>
{
    public void Configure(EntityTypeBuilder<Donation> builder)
    {
        builder.ToTable("donations", "events");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .ValueGeneratedNever();

        // Foreign keys
        builder.Property(d => d.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(d => d.RegistrationId)
            .HasColumnName("registration_id")
            .IsRequired(false);

        // Donor information
        builder.Property(d => d.DonorUserId)
            .HasColumnName("donor_user_id")
            .IsRequired(false);

        builder.Property(d => d.DonorName)
            .HasColumnName("donor_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(d => d.DonorEmail)
            .HasColumnName("donor_email")
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(d => d.DonorPhone)
            .HasColumnName("donor_phone")
            .HasMaxLength(30)
            .IsRequired(false);

        builder.Property(d => d.DonorNotes)
            .HasColumnName("donor_notes")
            .HasMaxLength(1000)
            .IsRequired(false);

        // Donation amount as Money owned entity
        builder.OwnsOne(d => d.Amount, money =>
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
        builder.Property(d => d.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        // Stripe payment fields
        builder.Property(d => d.StripeCheckoutSessionId)
            .HasColumnName("stripe_checkout_session_id")
            .HasMaxLength(200);

        builder.Property(d => d.StripePaymentIntentId)
            .HasColumnName("stripe_payment_intent_id")
            .HasMaxLength(200);

        // Revenue breakdown as Money owned entities (nullable - populated after payment)
        builder.OwnsOne(d => d.StripeFeeAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("stripe_fee_amount")
                .HasPrecision(18, 2);

            money.Property(m => m.Currency)
                .HasColumnName("stripe_fee_currency")
                .HasConversion<string>()
                .HasMaxLength(3);
        });

        builder.OwnsOne(d => d.PlatformCommissionAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("platform_commission_amount")
                .HasPrecision(18, 2);

            money.Property(m => m.Currency)
                .HasColumnName("platform_commission_currency")
                .HasConversion<string>()
                .HasMaxLength(3);
        });

        builder.OwnsOne(d => d.OrganizerPayoutAmount, money =>
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
        builder.Property(d => d.CheckoutExpiresAt)
            .HasColumnName("checkout_expires_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(d => d.PaymentCompletedAt)
            .HasColumnName("payment_completed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(d => d.FailedAt)
            .HasColumnName("failed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(d => d.AbandonedAt)
            .HasColumnName("abandoned_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(d => d.RefundedAt)
            .HasColumnName("refunded_at")
            .HasColumnType("timestamp with time zone");

        // Audit fields
        builder.Property(d => d.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(d => d.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        // Indexes
        builder.HasIndex(d => d.EventId)
            .HasDatabaseName("ix_donations_event_id");

        builder.HasIndex(d => d.DonorUserId)
            .HasDatabaseName("ix_donations_donor_user_id");

        builder.HasIndex(d => d.RegistrationId)
            .HasDatabaseName("ix_donations_registration_id");

        builder.HasIndex(d => d.Status)
            .HasDatabaseName("ix_donations_status");

        builder.HasIndex(d => d.StripeCheckoutSessionId)
            .HasDatabaseName("ix_donations_checkout_session");

        builder.HasIndex(d => d.StripePaymentIntentId)
            .HasDatabaseName("ix_donations_payment_intent");

        // Foreign key relationships
        builder.HasOne<Event>()
            .WithMany()
            .HasForeignKey(d => d.EventId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
