namespace LankaConnect.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Per-module idempotency-key store. Records (key, response) pairs so a
/// replayed command short-circuits with the original response instead of
/// re-executing the handler.
/// </summary>
/// <remarks>
/// <para>
/// Concrete implementation in W2.5 backs onto a per-module
/// <c>idempotency_keys</c> Postgres table with a unique constraint on
/// <c>(module, key)</c>. The serialized response is stored opaquely so the
/// store doesn't need to know the response type.
/// </para>
/// <para>
/// Keys expire after a TTL (typical: 24h) to bound table size. Expiry policy
/// is the concrete store's responsibility; this abstraction is intentionally
/// minimal.
/// </para>
/// </remarks>
public interface IIdempotencyStore
{
    /// <summary>
    /// Returns the previously recorded response for <paramref name="key"/>,
    /// or <c>null</c> if the key has not been seen (or has expired).
    /// </summary>
    Task<string?> TryGetAsync(Guid key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records (<paramref name="key"/>, <paramref name="serializedResponse"/>)
    /// so a subsequent call to <see cref="TryGetAsync"/> with the same key
    /// returns the stored response.
    /// </summary>
    Task PutAsync(Guid key, string serializedResponse, CancellationToken cancellationToken = default);
}
