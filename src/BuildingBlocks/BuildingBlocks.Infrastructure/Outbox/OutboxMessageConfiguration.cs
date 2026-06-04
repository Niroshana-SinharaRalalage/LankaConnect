using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LankaConnect.BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Reusable EF Core configuration for <see cref="OutboxMessage"/>. Each module
/// applies this from its own <c>OnModelCreating</c> so the outbox table shape
/// stays identical across modules (but the physical table lives in each
/// module's own schema — set via <c>HasDefaultSchema(...)</c> on the
/// per-module DbContext, no schema override here).
/// </summary>
/// <remarks>
/// <para>
/// <b>Table name</b>: <c>outbox</c> (snake_case, single word — module schema
/// already disambiguates from other modules' outboxes).
/// </para>
/// <para>
/// <b>Index</b>: partial index on <c>processed_at IS NULL</c> matches the
/// processor's hot-path query (only pending rows are scanned per tick).
/// </para>
/// </remarks>
public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("outbox");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.EventType)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(m => m.Payload)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(m => m.OccurredAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(m => m.ProcessedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(m => m.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(m => m.LastError)
            .HasMaxLength(2000)
            .IsRequired(false);

        // Hot-path partial index — the processor scans only rows where
        // processed_at IS NULL. Partial index keeps the index small even
        // when the outbox accumulates millions of processed rows.
        builder.HasIndex(m => m.OccurredAt)
            .HasDatabaseName("ix_outbox_pending_occurred_at")
            .HasFilter("\"ProcessedAt\" IS NULL");
    }
}
