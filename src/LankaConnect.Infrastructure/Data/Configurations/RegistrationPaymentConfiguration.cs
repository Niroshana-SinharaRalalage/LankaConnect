using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Enums;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for RegistrationPayment entity.
/// Part of the Add-Only Attendees with Delta Payment feature.
/// Tracks individual payments for audit trail and reconciliation.
/// </summary>
public class RegistrationPaymentConfiguration : IEntityTypeConfiguration<RegistrationPayment>
{
    public void Configure(EntityTypeBuilder<RegistrationPayment> builder)
    {
        builder.ToTable("registration_payments", "events");

        builder.HasKey(rp => rp.Id);

        builder.Property(rp => rp.Id)
            .ValueGeneratedNever();

        // Foreign key
        builder.Property(rp => rp.RegistrationId)
            .HasColumnName("registration_id")
            .IsRequired();

        // Stripe Payment Intent ID
        builder.Property(rp => rp.StripePaymentIntentId)
            .HasColumnName("stripe_payment_intent_id")
            .HasMaxLength(200)
            .IsRequired();

        // Configure Amount as Money value object
        builder.OwnsOne(rp => rp.Amount, money =>
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

        // Payment type enum
        builder.Property(rp => rp.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Payment status enum
        builder.Property(rp => rp.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Completed timestamp
        builder.Property(rp => rp.CompletedAt)
            .HasColumnName("completed_at")
            .HasColumnType("timestamp with time zone");

        // Optional reference to RegistrationAddition for addition payments
        builder.Property(rp => rp.RegistrationAdditionId)
            .HasColumnName("registration_addition_id");

        // Audit fields
        builder.Property(rp => rp.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(rp => rp.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        // Wave4.9.2.10d Phase 1.10d (2026-06-09): physical CreatedBy/UpdatedBy.
        builder.Property(rp => rp.CreatedBy).HasColumnName("created_by").HasColumnType("text");
        builder.Property(rp => rp.UpdatedBy).HasColumnName("updated_by").HasColumnType("text");

        // Indexes
        builder.HasIndex(rp => rp.RegistrationId)
            .HasDatabaseName("ix_registration_payments_registration_id");

        builder.HasIndex(rp => rp.StripePaymentIntentId)
            .IsUnique()
            .HasDatabaseName("uq_registration_payments_payment_intent");

        builder.HasIndex(rp => rp.RegistrationAdditionId)
            .HasDatabaseName("ix_registration_payments_addition_id");

        builder.HasIndex(rp => new { rp.RegistrationId, rp.Type })
            .HasDatabaseName("ix_registration_payments_registration_type");

        // Foreign key relationships
        builder.HasOne<Registration>()
            .WithMany()
            .HasForeignKey(rp => rp.RegistrationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<RegistrationAddition>()
            .WithMany()
            .HasForeignKey(rp => rp.RegistrationAdditionId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
