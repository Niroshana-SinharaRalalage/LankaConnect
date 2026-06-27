using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;

namespace LankaConnect.Infrastructure.Data.Configurations;

public class PassPurchaseConfiguration : IEntityTypeConfiguration<PassPurchase>
{
    public void Configure(EntityTypeBuilder<PassPurchase> builder)
    {
        builder.ToTable("pass_purchases");

        builder.HasKey(pp => pp.Id);

        builder.Property(pp => pp.Id)
            .ValueGeneratedNever();

        builder.Property(pp => pp.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(pp => pp.EventId)
            .HasColumnName("event_id")
            .IsRequired();

        builder.Property(pp => pp.EventPassId)
            .HasColumnName("event_pass_id")
            .IsRequired();

        builder.Property(pp => pp.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        // Configure TotalPrice as owned Money value object using ComplexProperty
        builder.ComplexProperty(pp => pp.TotalPrice, price =>
        {
            price.Property(p => p.Amount)
                .HasColumnName("total_price_amount")
                .HasPrecision(18, 2)
                .IsRequired();

            price.Property(p => p.Currency)
                .HasColumnName("total_price_currency")
                .HasConversion<string>()
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(pp => pp.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired()
            .HasDefaultValue(PassPurchaseStatus.Pending);

        builder.Property(pp => pp.QRCode)
            .HasColumnName("qr_code")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(pp => pp.ConfirmedAt)
            .HasColumnName("confirmed_at")
            .HasColumnType("timestamp with time zone");

        builder.Property(pp => pp.CancelledAt)
            .HasColumnName("cancelled_at")
            .HasColumnType("timestamp with time zone");

        // Configure audit fields
        builder.Property(pp => pp.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(pp => pp.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone");

        // Indexes for common queries
        builder.HasIndex(pp => pp.UserId)
            .HasDatabaseName("ix_pass_purchases_user_id");

        builder.HasIndex(pp => pp.EventId)
            .HasDatabaseName("ix_pass_purchases_event_id");

        builder.HasIndex(pp => pp.EventPassId)
            .HasDatabaseName("ix_pass_purchases_event_pass_id");

        builder.HasIndex(pp => pp.QRCode)
            .IsUnique()
            .HasDatabaseName("ix_pass_purchases_qr_code");

        builder.HasIndex(pp => new { pp.EventId, pp.UserId, pp.Status })
            .HasDatabaseName("ix_pass_purchases_event_user_status");
    }
}
