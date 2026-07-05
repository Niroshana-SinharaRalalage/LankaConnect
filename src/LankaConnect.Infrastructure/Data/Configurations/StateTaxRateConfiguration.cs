using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Modules.Payments.Domain.Tax;

namespace LankaConnect.SPLIT_PER_ENTITY.Configurations;

/// <summary>
/// Phase 6A.X: EF Core configuration for StateTaxRate entity
/// </summary>
public class StateTaxRateConfiguration : IEntityTypeConfiguration<StateTaxRate>
{
    public void Configure(EntityTypeBuilder<StateTaxRate> builder)
    {
        // Phase 6A.X: Explicitly specify reference_data schema (matches other reference data entities)
        builder.ToTable("state_tax_rates", "reference_data");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id")  // Phase 6A.X: PostgreSQL uses lowercase column names
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

        // Wave4.9.2.2 Phase 1.2 (2026-06-08): physical CreatedBy/UpdatedBy
        // columns landed on reference_data.state_tax_rates via
        // Phase1_2_AddCreatedByUpdatedByToReferenceDataStateTaxRates migration.
        // Snake_case per the audit-by-actor column convention. AppDbContext
        // does not yet wire an AuditableInterceptor (Phase 1.10 scope) so
        // these columns persist as NULL on writes until then.
        // Closing the loop: StateTaxRate was the entity that produced the
        // Phase 3 "column s.CreatedBy does not exist" 42703 error - the
        // exact regression class this rollout is designed to prevent.
        builder.Property(r => r.CreatedBy)
            .HasColumnName("created_by")
            .HasColumnType("text");

        builder.Property(r => r.UpdatedBy)
            .HasColumnName("updated_by")
            .HasColumnType("text");

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
