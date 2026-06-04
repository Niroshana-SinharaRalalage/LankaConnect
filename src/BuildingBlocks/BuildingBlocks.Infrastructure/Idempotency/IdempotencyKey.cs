namespace LankaConnect.BuildingBlocks.Infrastructure.Idempotency;

/// <summary>
/// Per-module idempotency row. One row per (key, recorded response) pair.
/// Backs the <see cref="LankaConnect.BuildingBlocks.Application.Abstractions.IIdempotencyStore"/>
/// abstraction: a replayed command short-circuits with the stored response
/// instead of re-executing the handler.
/// </summary>
/// <remarks>
/// <para>
/// <b>Per-module table</b>: each module hosts its own <c>idempotency_keys</c>
/// table in its own Postgres schema. The CLR type is shared via BuildingBlocks
/// so the entity shape stays consistent across modules; the physical tables
/// are isolated so a noisy producer in one module can't grow another module's
/// hot path.
/// </para>
/// <para>
/// <b>TTL</b>: rows expire after <see cref="ExpiresAt"/> (typical: 24h after
/// <see cref="RecordedAt"/>). Expiry sweep is the concrete store's
/// responsibility; this entity is intentionally minimal.
/// </para>
/// </remarks>
public sealed class IdempotencyKey
{
    /// <summary>Idempotency key — primary identifier; provided by the caller.</summary>
    public Guid Key { get; private set; }

    /// <summary>JSON-serialized response captured from the first successful handler invocation.</summary>
    public string SerializedResponse { get; private set; } = string.Empty;

    /// <summary>UTC timestamp when the row was first recorded.</summary>
    public DateTime RecordedAt { get; private set; }

    /// <summary>UTC timestamp after which the row is considered expired and may be purged.</summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>EF Core constructor — DO NOT call from application code.</summary>
    private IdempotencyKey() { }

    /// <summary>
    /// Records a new idempotency entry with explicit timestamps. The caller is
    /// responsible for deciding the TTL window (<paramref name="expiresAtUtc"/>
    /// minus <paramref name="recordedAtUtc"/>) — usually 24 hours.
    /// </summary>
    public static IdempotencyKey Create(
        Guid key,
        string serializedResponse,
        DateTime recordedAtUtc,
        DateTime expiresAtUtc)
    {
        if (key == Guid.Empty)
        {
            throw new ArgumentException("Idempotency key cannot be empty.", nameof(key));
        }
        ArgumentException.ThrowIfNullOrWhiteSpace(serializedResponse);
        if (expiresAtUtc <= recordedAtUtc)
        {
            throw new ArgumentException(
                "ExpiresAt must be strictly after RecordedAt.",
                nameof(expiresAtUtc));
        }

        return new IdempotencyKey
        {
            Key = key,
            SerializedResponse = serializedResponse,
            RecordedAt = recordedAtUtc,
            ExpiresAt = expiresAtUtc,
        };
    }
}
