namespace LankaConnect.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Audit-log writer. Captures structured records of every command execution
/// to <c>platform.audit_events</c> (W2.4 / W2.5).
/// </summary>
/// <remarks>
/// <para>
/// Per architect review of ADR-002, audit logging is the platform-wide
/// observability backbone — every state-changing operation produces an
/// audit record with the actor, the operation, the affected entity (if
/// any), and a timestamp.
/// </para>
/// <para>
/// Concrete implementation in W2.5 backs onto a Postgres
/// <c>platform.audit_events</c> table outside any single module schema —
/// cross-module audit reporting is a single-table query.
/// </para>
/// </remarks>
public interface IAuditLogger
{
    /// <summary>
    /// Records a single audit event. The behavior should NOT block the
    /// caller on audit failure — log it locally and continue. Audit-write
    /// failure must not roll back the business transaction.
    /// </summary>
    Task LogAsync(AuditEntry entry, CancellationToken cancellationToken = default);
}

/// <summary>
/// A single audit-log record. Persisted opaquely by <see cref="IAuditLogger"/>
/// to a cross-module audit table.
/// </summary>
/// <param name="OperationName">
/// Logical operation name — typically the command type name (e.g.
/// <c>"CreateEventCommand"</c>).
/// </param>
/// <param name="ActorId">
/// Authenticated user / service principal performing the operation, or
/// <c>null</c> for anonymous operations.
/// </param>
/// <param name="OccurredAt">UTC timestamp when the operation ran.</param>
/// <param name="Outcome">"Success" or "Failure" — drives audit dashboards.</param>
/// <param name="DetailsJson">
/// Optional JSON-serialized command parameters / response summary. Should NOT
/// contain PII without explicit per-field consideration (passwords, tokens,
/// payment card data must never appear here).
/// </param>
public sealed record AuditEntry(
    string OperationName,
    string? ActorId,
    DateTime OccurredAt,
    string Outcome,
    string? DetailsJson);
