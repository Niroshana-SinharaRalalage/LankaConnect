using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using LankaConnect.Domain.Communications.Entities;

namespace LankaConnect.Infrastructure.Data.Configurations;

/// <summary>
/// Phase 6A.89: EF Core configuration for EmailMetricRecord entity.
///
/// Table: communications.email_metrics
/// Purpose: Persist email metrics for dashboard that survives container restarts
/// </summary>
public class EmailMetricRecordConfiguration : IEntityTypeConfiguration<EmailMetricRecord>
{
    public void Configure(EntityTypeBuilder<EmailMetricRecord> builder)
    {
        builder.ToTable("email_metrics", "communications");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        // Metric date (daily granularity)
        builder.Property(e => e.MetricDate)
            .HasColumnName("metric_date")
            .IsRequired();

        // Template name (nullable for global aggregates)
        builder.Property(e => e.TemplateName)
            .HasColumnName("template_name")
            .HasMaxLength(100);

        // Counts
        builder.Property(e => e.TotalSent)
            .HasColumnName("total_sent")
            .HasDefaultValue(0);

        builder.Property(e => e.Successful)
            .HasColumnName("successful")
            .HasDefaultValue(0);

        builder.Property(e => e.Failed)
            .HasColumnName("failed")
            .HasDefaultValue(0);

        // Duration metrics
        builder.Property(e => e.AverageDurationMs)
            .HasColumnName("avg_duration_ms")
            .HasDefaultValue(0);

        builder.Property(e => e.TotalDurationMs)
            .HasColumnName("total_duration_ms")
            .HasDefaultValue(0L);

        // Error tracking
        builder.Property(e => e.ValidationFailures)
            .HasColumnName("validation_failures")
            .HasDefaultValue(0);

        builder.Property(e => e.TemplateNotFoundCount)
            .HasColumnName("template_not_found_count")
            .HasDefaultValue(0);

        // Audit fields from BaseEntity
        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired()
            .HasDefaultValueSql("NOW()");

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at");

        // Unique constraint: one record per date/template combination
        builder.HasIndex(e => new { e.MetricDate, e.TemplateName })
            .IsUnique()
            .HasDatabaseName("IX_EmailMetrics_Date_Template");

        // Performance indexes
        builder.HasIndex(e => e.MetricDate)
            .HasDatabaseName("IX_EmailMetrics_Date");

        builder.HasIndex(e => e.TemplateName)
            .HasDatabaseName("IX_EmailMetrics_Template");

        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_EmailMetrics_CreatedAt");
    }
}
