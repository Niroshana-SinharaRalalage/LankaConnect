using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LankaConnect.Products.LankaEvents.Infrastructure.Configurations;

/// <summary>
/// Phase 6A.148 — EF config for refund_request_line_items table.
///
/// One row per (Type, ReferenceId) on the parent RefundRequest. The Money value object
/// is stored as two columns each (Amount + Currency code) following the project's existing
/// pattern (RegistrationConfiguration.TotalPrice OwnsOne mapping).
/// </summary>
public class RefundRequestLineItemConfiguration : IEntityTypeConfiguration<RefundRequestLineItem>
{
    public void Configure(EntityTypeBuilder<RefundRequestLineItem> builder)
    {
        builder.ToTable("refund_request_line_items", "events"); // Rule 5i: explicit schema

        builder.HasKey(li => li.Id);
        builder.Property(li => li.Id).ValueGeneratedNever();

        builder.Property(li => li.RefundRequestId)
            .HasColumnName("refund_request_id")
            .IsRequired();

        builder.Property(li => li.Type)
            .HasColumnName("type")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(li => li.ReferenceId)
            .HasColumnName("reference_id")
            .IsRequired();

        // RequestedAmount (Money) — Amount + Currency columns. Non-nullable: every line
        // item carries the amount the caller asked to refund.
        builder.OwnsOne(li => li.RequestedAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("requested_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("requested_currency")
                .HasConversion<string>()
                .HasMaxLength(3)
                .IsRequired();
        });
        builder.Navigation(li => li.RequestedAmount).IsRequired();

        // ApprovedAmount (Money?) — null until organizer reviews. Stored in nullable columns.
        builder.OwnsOne(li => li.ApprovedAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("approved_amount")
                .HasPrecision(18, 2);

            money.Property(m => m.Currency)
                .HasColumnName("approved_currency")
                .HasConversion<string>()
                .HasMaxLength(3);
        });

        builder.Property(li => li.Status)
            .HasColumnName("status")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(li => li.StripeRefundId)
            .HasColumnName("stripe_refund_id")
            .HasMaxLength(64);

        builder.Property(li => li.StripeChargeId)
            .HasColumnName("stripe_charge_id")
            .HasMaxLength(64);

        builder.Property(li => li.ProcessedAt)
            .HasColumnName("processed_at");

        builder.Property(li => li.FailureReason)
            .HasColumnName("failure_reason")
            .HasMaxLength(500);

        builder.Property(li => li.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(li => li.UpdatedAt)
            .HasColumnName("updated_at");

        // Wave4.9.2.10d Phase 1.10d (2026-06-09): physical CreatedBy/UpdatedBy.
        builder.Property(li => li.CreatedBy).HasColumnName("created_by").HasColumnType("text");
        builder.Property(li => li.UpdatedBy).HasColumnName("updated_by").HasColumnType("text");

        // Indices.
        builder.HasIndex(li => li.RefundRequestId)
            .HasDatabaseName("ix_refund_line_items_refund_request_id");
        builder.HasIndex(li => li.StripeRefundId)
            .HasDatabaseName("ix_refund_line_items_stripe_refund_id");
    }
}
