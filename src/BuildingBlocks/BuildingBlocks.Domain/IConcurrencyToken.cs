namespace LankaConnect.BuildingBlocks.Domain;

/// <summary>
/// Opt-in marker for entities that require optimistic concurrency control.
/// <c>BaseDbContext.OnModelCreating</c> in W1B auto-applies
/// <c>HasRowVersion()</c> to the <see cref="RowVersion"/> property so EF Core
/// includes it in every UPDATE's WHERE clause and throws
/// <c>DbUpdateConcurrencyException</c> on stale-write conflicts.
/// </summary>
/// <remarks>
/// <para>
/// <b>When to use</b>: entities whose state transitions must NOT be lost under
/// concurrent writes — Payments (charge state transitions: Pending → Authorized
/// → Captured/Failed), SeatHold (two users grabbing the last seat), Inventory
/// (Phase 3 Commerce stock decrements). Without this, last-writer-wins
/// silently corrupts state under load (phantom double-charges, oversold
/// inventory).
/// </para>
/// <para>
/// <b>When NOT to use</b>: high-frequency append-only state (Outbox, Audit
/// events, Notifications). Concurrency tokens add UPDATE overhead and aren't
/// meaningful when each row is written-once.
/// </para>
/// <para>
/// Setter is public so EF Core can update it; domain code should treat
/// <see cref="RowVersion"/> as read-only.
/// </para>
/// </remarks>
public interface IConcurrencyToken
{
    /// <summary>
    /// PostgreSQL <c>xmin</c>-backed concurrency token. EF Core maps this to
    /// the system column and updates it automatically on every write. The
    /// underlying type is <c>byte[]</c> to match EF Core's RowVersion convention
    /// (PostgreSQL provider transparently maps xmin to/from it).
    /// </summary>
    byte[] RowVersion { get; set; }
}
