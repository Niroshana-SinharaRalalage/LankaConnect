using LankaConnect.Products.LankaEvents.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LankaConnect.Products.LankaEvents.Infrastructure.Configurations;

/// <summary>
/// Phase 7F-B (architect-approved 2026-04-30): per-registration audit detail row joined
/// to <see cref="RegistrationModeConversion"/> via <see cref="RegistrationModeConversionRow.AggregateConversionId"/>.
/// </summary>
public class RegistrationModeConversionRowConfiguration : IEntityTypeConfiguration<RegistrationModeConversionRow>
{
    public void Configure(EntityTypeBuilder<RegistrationModeConversionRow> builder)
    {
        builder.ToTable("registration_mode_conversion_rows", "events");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.AggregateConversionId).HasColumnName("aggregate_conversion_id").IsRequired();
        builder.Property(e => e.RegistrationId).HasColumnName("registration_id").IsRequired();

        builder.Property(e => e.ConversionOutcome)
            .HasColumnName("conversion_outcome")
            .HasConversion<short>()
            .IsRequired();

        builder.Property(e => e.OutcomeReason)
            .HasColumnName("outcome_reason")
            .HasMaxLength(2000);

        builder.Property(e => e.RegistrationRowVersionSnapshot)
            .HasColumnName("registration_row_version_snapshot");

        // BeforeShape / AfterShape stored as jsonb. Persisted as serialised string from
        // System.Text.Json — handler builds the JSON before insert (no EF ValueConverter
        // because shapes are heterogeneous: AttendeeDetails[] vs HeadCountBreakdown).
        builder.Property(e => e.BeforeShape)
            .HasColumnName("before_shape")
            .HasColumnType("jsonb");
        builder.Property(e => e.AfterShape)
            .HasColumnName("after_shape")
            .HasColumnType("jsonb");

        builder.Property(e => e.ConvertedAt).HasColumnName("converted_at").IsRequired();

        // Wave4.9.2.10d Phase 1.10d (2026-06-09): physical CreatedBy/UpdatedBy.
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasColumnType("text");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasColumnType("text");

        builder.HasIndex(e => e.AggregateConversionId)
            .HasDatabaseName("ix_registration_mode_conversion_rows_aggregate_id");
        builder.HasIndex(e => e.RegistrationId)
            .HasDatabaseName("ix_registration_mode_conversion_rows_registration_id");

        // FK to registration_mode_conversions with cascade-delete on the parent.
        builder.HasOne<RegistrationModeConversion>()
            .WithMany()
            .HasForeignKey(e => e.AggregateConversionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
