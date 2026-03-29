using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for AddOnPurchase entity.
/// Tracks individual purchases of event add-ons.
/// </summary>
public class AddOnPurchaseEntityConfiguration : IEntityTypeConfiguration<AddOnPurchase>
{
    public void Configure(EntityTypeBuilder<AddOnPurchase> builder)
    {
        builder.ToTable("add_on_purchases", "events");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        // Foreign keys
        builder.Property(p => p.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(p => p.AddOnDefinitionId)
            .HasColumnName("add_on_definition_id")
            .IsRequired();

        builder.Property(p => p.RegistrationId)
            .HasColumnName("registration_id")
            .IsRequired(false);

        // Buyer information
        builder.Property(p => p.BuyerUserId)
            .HasColumnName("buyer_user_id")
            .IsRequired(false);

        builder.Property(p => p.BuyerName)
            .HasColumnName("buyer_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.BuyerEmail)
            .HasColumnName("buyer_email")
            .HasMaxLength(320)
            .IsRequired();

        builder.Property(p => p.BuyerPhone)
            .HasColumnName("buyer_phone")
            .HasMaxLength(30)
            .IsRequired(false);

        // Quantity
        builder.Property(p => p.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        // Unit price as Money owned entity
        builder.OwnsOne(p => p.UnitPrice, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("unit_price_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("unit_price_currency")
                .HasConversion<string>()
                .HasMaxLength(3)
                .IsRequired();
        });

        // Total amount as Money owned entity
        builder.OwnsOne(p => p.TotalAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("total_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("total_amount_currency")
                .HasConversion<string>()
                .HasMaxLength(3)
                .IsRequired();
        });

        // Status enum
        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        // Stripe payment fields
        builder.Property(p => p.StripeCheckoutSessionId)
            .HasColumnName("stripe_checkout_session_id")
            .HasMaxLength(500);

        builder.Property(p => p.StripePaymentIntentId)
            .HasColumnName("stripe_payment_intent_id")
            .HasMaxLength(200);

        // Revenue breakdown as Money owned entities (nullable - populated after payment)
        builder.OwnsOne(p => p.StripeFeeAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("stripe_fee_amount")
                .HasPrecision(18, 2);

            money.Property(m => m.Currency)
                .HasColumnName("stripe_fee_currency")
                .HasConversion<string>()
                .HasMaxLength(3);
        });

        builder.OwnsOne(p => p.PlatformCommissionAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("platform_commission_amount")
                .HasPrecision(18, 2);

            money.Property(m => m.Currency)
                .HasColumnName("platform_commission_currency")
                .HasConversion<string>()
                .HasMaxLength(3);
        });

        builder.OwnsOne(p => p.OrganizerPayoutAmount, money =>
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
        builder.Property(p => p.CheckoutExpiresAt)
            .HasColumnName("checkout_expires_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(p => p.PaymentCompletedAt)
            .HasColumnName("payment_completed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(p => p.FailedAt)
            .HasColumnName("failed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(p => p.AbandonedAt)
            .HasColumnName("abandoned_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(p => p.RefundedAt)
            .HasColumnName("refunded_at")
            .HasColumnType("timestamp with time zone");

        // Audit fields
        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        // Indexes
        builder.HasIndex(p => p.EventId)
            .HasDatabaseName("ix_add_on_purchases_event_id");

        builder.HasIndex(p => p.AddOnDefinitionId)
            .HasDatabaseName("ix_add_on_purchases_add_on_definition_id");

        builder.HasIndex(p => p.RegistrationId)
            .HasDatabaseName("ix_add_on_purchases_registration_id");

        builder.HasIndex(p => p.BuyerUserId)
            .HasDatabaseName("ix_add_on_purchases_buyer_user_id");

        builder.HasIndex(p => p.Status)
            .HasDatabaseName("ix_add_on_purchases_status");

        builder.HasIndex(p => p.StripeCheckoutSessionId)
            .HasDatabaseName("ix_add_on_purchases_checkout_session");

        builder.HasIndex(p => p.StripePaymentIntentId)
            .HasDatabaseName("ix_add_on_purchases_payment_intent");

        // Foreign key relationships
        builder.HasOne<Event>()
            .WithMany()
            .HasForeignKey(p => p.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AddOnDefinition>()
            .WithMany()
            .HasForeignKey(p => p.AddOnDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Phase 6A.137F-Fix4: Add Registration FK with SetNull on delete.
        // Previously missing — caused orphaned add-on purchases when registrations were deleted,
        // which inflated refund counts and caused charge_already_refunded errors.
        builder.HasOne<Registration>()
            .WithMany()
            .HasForeignKey(p => p.RegistrationId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
