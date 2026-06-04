using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LankaConnect.BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Reusable EF Core configuration for <see cref="DeadLetterMessage"/>. Each
/// module applies this from its own <c>OnModelCreating</c>.
/// </summary>
/// <remarks>
/// <b>Table name</b>: <c>outbox_dead_letter</c> (snake_case, sibling to
/// <c>outbox</c> in the same module schema).
/// </remarks>
public sealed class DeadLetterMessageConfiguration : IEntityTypeConfiguration<DeadLetterMessage>
{
    public void Configure(EntityTypeBuilder<DeadLetterMessage> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("outbox_dead_letter");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.OriginalOutboxId)
            .IsRequired();

        builder.Property(m => m.EventType)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(m => m.Payload)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(m => m.OccurredAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(m => m.DeadLetteredAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(m => m.RetryCount).IsRequired();

        builder.Property(m => m.LastError)
            .HasMaxLength(2000)
            .IsRequired(false);

        // Ops dashboards look up dead-letter rows by their original outbox id
        // when manually replaying — make that lookup cheap.
        builder.HasIndex(m => m.OriginalOutboxId)
            .HasDatabaseName("ix_outbox_dead_letter_original_outbox_id");

        // Alert query: count rows dead-lettered in the last N minutes.
        builder.HasIndex(m => m.DeadLetteredAt)
            .HasDatabaseName("ix_outbox_dead_letter_dead_lettered_at");
    }
}
