using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// Phase 7F-B (architect-approved 2026-04-30): aggregate audit row recorded once per
/// organiser conversion action.
/// </summary>
public class RegistrationModeConversionConfiguration : IEntityTypeConfiguration<RegistrationModeConversion>
{
    public void Configure(EntityTypeBuilder<RegistrationModeConversion> builder)
    {
        builder.ToTable("registration_mode_conversions", "events");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.EventId).HasColumnName("event_id").IsRequired();
        builder.Property(e => e.OrganiserId).HasColumnName("organiser_id").IsRequired();

        builder.Property(e => e.FromMode)
            .HasColumnName("from_mode")
            .HasConversion<short>()
            .IsRequired();
        builder.Property(e => e.ToMode)
            .HasColumnName("to_mode")
            .HasConversion<short>()
            .IsRequired();

        builder.Property(e => e.StartedAt).HasColumnName("started_at").IsRequired();
        builder.Property(e => e.CompletedAt).HasColumnName("completed_at").IsRequired();

        builder.Property(e => e.TotalCount).HasColumnName("total_count").IsRequired();
        builder.Property(e => e.MigratedCount).HasColumnName("migrated_count").IsRequired();
        builder.Property(e => e.SkippedCount).HasColumnName("skipped_count").IsRequired();
        builder.Property(e => e.FailedCount).HasColumnName("failed_count").IsRequired();

        builder.Property(e => e.EventRowVersionSnapshot)
            .HasColumnName("event_row_version_snapshot");

        // Wave4.9.2.10d Phase 1.10d (2026-06-09): physical CreatedBy/UpdatedBy.
        builder.Property(e => e.CreatedBy).HasColumnName("created_by").HasColumnType("text");
        builder.Property(e => e.UpdatedBy).HasColumnName("updated_by").HasColumnType("text");

        // Index on EventId for dashboard queries ("show all conversions on this event").
        builder.HasIndex(e => e.EventId).HasDatabaseName("ix_registration_mode_conversions_event_id");
        builder.HasIndex(e => e.OrganiserId).HasDatabaseName("ix_registration_mode_conversions_organiser_id");
    }
}
