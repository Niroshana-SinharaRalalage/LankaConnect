using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LankaConnect.BuildingBlocks.Infrastructure.Idempotency;

/// <summary>
/// Reusable EF Core configuration for <see cref="IdempotencyKey"/>. Each
/// module applies this from its own <c>OnModelCreating</c>.
/// </summary>
/// <remarks>
/// <b>Table name</b>: <c>idempotency_keys</c>.
/// </remarks>
public sealed class IdempotencyKeyConfiguration : IEntityTypeConfiguration<IdempotencyKey>
{
    public void Configure(EntityTypeBuilder<IdempotencyKey> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("idempotency_keys");

        builder.HasKey(k => k.Key);
        builder.Property(k => k.Key).ValueGeneratedNever();

        builder.Property(k => k.SerializedResponse)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(k => k.RecordedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(k => k.ExpiresAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // TTL sweep query: WHERE expires_at < now() ORDER BY expires_at.
        // Partial index would be slightly tighter but the table stays small
        // (24h TTL bounds size) so a plain index keeps things simple.
        builder.HasIndex(k => k.ExpiresAt)
            .HasDatabaseName("ix_idempotency_keys_expires_at");
    }
}
