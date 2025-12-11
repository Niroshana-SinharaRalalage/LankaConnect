using LankaConnect.Infrastructure.Payments.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LankaConnect.Infrastructure.Payments.Configurations;

/// <summary>
/// EF Core configuration for StripeCustomer infrastructure entity
/// Phase 6A.4: Stripe Payment Integration
/// </summary>
public class StripeCustomerConfiguration : IEntityTypeConfiguration<StripeCustomer>
{
    public void Configure(EntityTypeBuilder<StripeCustomer> builder)
    {
        builder.ToTable("stripe_customers", "payments");

        builder.HasKey(sc => sc.Id);

        // UserId - foreign key to Users table
        builder.Property(sc => sc.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.HasIndex(sc => sc.UserId)
            .IsUnique()
            .HasDatabaseName("ix_stripe_customers_user_id");

        // StripeCustomerId - unique identifier from Stripe (cus_xxx)
        builder.Property(sc => sc.StripeCustomerId)
            .HasColumnName("stripe_customer_id")
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(sc => sc.StripeCustomerId)
            .IsUnique()
            .HasDatabaseName("ix_stripe_customers_stripe_customer_id");

        // Email
        builder.Property(sc => sc.Email)
            .HasColumnName("email")
            .IsRequired()
            .HasMaxLength(255);

        // Name
        builder.Property(sc => sc.Name)
            .HasColumnName("name")
            .IsRequired()
            .HasMaxLength(255);

        // StripeCreatedAt
        builder.Property(sc => sc.StripeCreatedAt)
            .HasColumnName("stripe_created_at")
            .IsRequired();

        // Base entity properties (CreatedAt, UpdatedAt)
        builder.Property(sc => sc.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(sc => sc.UpdatedAt)
            .HasColumnName("updated_at");
    }
}
