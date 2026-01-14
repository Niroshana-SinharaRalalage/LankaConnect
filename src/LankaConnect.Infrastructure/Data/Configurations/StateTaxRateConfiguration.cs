using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Tax;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// Phase 6A.X: EF Core configuration for StateTaxRate entity
/// </summary>
public class StateTaxRateConfiguration : IEntityTypeConfiguration<StateTaxRate>
{
    public void Configure(EntityTypeBuilder<StateTaxRate> builder)
    {
        builder.ToTable("state_tax_rates");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedNever();

        builder.Property(r => r.StateCode)
            .IsRequired()
            .HasMaxLength(2)
            .HasColumnName("state_code");

        builder.Property(r => r.StateName)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnName("state_name");

        builder.Property(r => r.TaxRate)
            .IsRequired()
            .HasPrecision(5, 4)
            .HasColumnName("tax_rate");

        builder.Property(r => r.EffectiveDate)
            .IsRequired()
            .HasColumnName("effective_date");

        builder.Property(r => r.IsActive)
            .IsRequired()
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(r => r.DataSource)
            .HasMaxLength(200)
            .HasColumnName("data_source");

        // Audit fields
        builder.Property(r => r.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("NOW()")
            .HasColumnName("created_at");

        builder.Property(r => r.UpdatedAt)
            .HasColumnName("updated_at");

        // Indexes
        builder.HasIndex(r => r.StateCode)
            .HasDatabaseName("ix_state_tax_rates_state_code");

        builder.HasIndex(r => new { r.StateCode, r.IsActive })
            .HasDatabaseName("ix_state_tax_rates_state_code_is_active");

        builder.HasIndex(r => r.EffectiveDate)
            .HasDatabaseName("ix_state_tax_rates_effective_date");

        // Unique constraint: one active rate per state at any given time
        builder.HasIndex(r => new { r.StateCode, r.EffectiveDate })
            .IsUnique()
            .HasDatabaseName("uq_state_tax_rates_state_code_effective_date");
    }
}
