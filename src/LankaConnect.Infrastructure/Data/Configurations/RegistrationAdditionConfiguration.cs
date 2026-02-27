using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// EF Core configuration for RegistrationAddition entity.
/// Part of the Add-Only Attendees with Delta Payment feature.
/// </summary>
public class RegistrationAdditionConfiguration : IEntityTypeConfiguration<RegistrationAddition>
{
    public void Configure(EntityTypeBuilder<RegistrationAddition> builder)
    {
        builder.ToTable("registration_additions", "events");

        builder.HasKey(ra => ra.Id);

        builder.Property(ra => ra.Id)
            .ValueGeneratedNever();

        // Foreign keys
        builder.Property(ra => ra.RegistrationId)
            .HasColumnName("registration_id")
            .IsRequired();

        builder.Property(ra => ra.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        // Configure NewAttendees as JSONB array
        builder.OwnsMany(ra => ra.NewAttendees, attendeesBuilder =>
        {
            attendeesBuilder.ToJson("new_attendees");

            attendeesBuilder.Property(a => a.Name).HasColumnName("name");
            attendeesBuilder.Property(a => a.AgeCategory)
                .HasColumnName("age_category")
                .HasConversion<string>();
            attendeesBuilder.Property(a => a.Gender)
                .HasColumnName("gender")
                .HasConversion<string?>()
                .IsRequired(false);
        });

        // Configure PreviousTotalPrice as Money value object
        builder.OwnsOne(ra => ra.PreviousTotalPrice, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("previous_total_price_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("previous_total_price_currency")
                .HasConversion<string>()
                .HasMaxLength(3)
                .IsRequired();
        });

        // Configure NewTotalPrice as Money value object
        builder.OwnsOne(ra => ra.NewTotalPrice, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("new_total_price_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("new_total_price_currency")
                .HasConversion<string>()
                .HasMaxLength(3)
                .IsRequired();
        });

        // Configure AdditionalAmount as Money value object
        builder.OwnsOne(ra => ra.AdditionalAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("additional_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("additional_amount_currency")
                .HasConversion<string>()
                .HasMaxLength(3)
                .IsRequired();
        });

        // Payment tracking
        builder.Property(ra => ra.StripeCheckoutSessionId)
            .HasColumnName("stripe_checkout_session_id")
            .HasMaxLength(500);

        builder.Property(ra => ra.StripePaymentIntentId)
            .HasColumnName("stripe_payment_intent_id")
            .HasMaxLength(200);

        // Status enum
        builder.Property(ra => ra.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        // Timestamps
        builder.Property(ra => ra.CheckoutExpiresAt)
            .HasColumnName("checkout_expires_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(ra => ra.PaymentCompletedAt)
            .HasColumnName("payment_completed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(ra => ra.MergedAt)
            .HasColumnName("merged_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(ra => ra.FailedAt)
            .HasColumnName("failed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(ra => ra.AbandonedAt)
            .HasColumnName("abandoned_at")
            .HasColumnType("timestamp with time zone");

        // Audit fields
        builder.Property(ra => ra.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(ra => ra.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        // Indexes
        builder.HasIndex(ra => ra.RegistrationId)
            .HasDatabaseName("ix_registration_additions_registration_id");

        builder.HasIndex(ra => ra.EventId)
            .HasDatabaseName("ix_registration_additions_event_id");

        builder.HasIndex(ra => ra.Status)
            .HasDatabaseName("ix_registration_additions_status");

        builder.HasIndex(ra => ra.StripeCheckoutSessionId)
            .HasDatabaseName("ix_registration_additions_checkout_session");

        // Unique constraint: Only one pending addition per registration at a time
        builder.HasIndex(ra => new { ra.RegistrationId, ra.Status })
            .HasFilter("status = 'Pending'")
            .IsUnique()
            .HasDatabaseName("uq_registration_additions_one_pending_per_registration");

        // Foreign key relationships
        builder.HasOne<Registration>()
            .WithMany()
            .HasForeignKey(ra => ra.RegistrationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Event>()
            .WithMany()
            .HasForeignKey(ra => ra.EventId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
