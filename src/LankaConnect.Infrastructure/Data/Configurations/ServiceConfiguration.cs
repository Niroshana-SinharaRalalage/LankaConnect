using LankaConnect.Domain.Business;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LankaConnect.Infrastructure.Data.Configurations;

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.ToTable("Services");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.Description)
            .HasMaxLength(1000)
            .IsRequired();

        // Price (Owned Entity)
        builder.OwnsOne(s => s.Price, p =>
        {
            p.Property(price => price.Amount)
                .HasColumnName("PriceAmount")
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            p.Property(price => price.Currency)
                .HasColumnName("PriceCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(s => s.Duration)
            .HasMaxLength(100);

        builder.Property(s => s.IsActive)
            .HasDefaultValue(true);

        builder.Property(s => s.BusinessId)
            .IsRequired();

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(s => s.Name)
            .HasDatabaseName("IX_Service_Name");

        builder.HasIndex(s => s.IsActive)
            .HasDatabaseName("IX_Service_IsActive");

        builder.HasIndex(s => s.BusinessId)
            .HasDatabaseName("IX_Service_BusinessId");

        // Price indexing will be added after migration
    }
}