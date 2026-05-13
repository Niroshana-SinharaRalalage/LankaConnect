namespace LankaConnect.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Per-module outbox for integration events. Events written here are
/// guaranteed to be published downstream — typically by an
/// <c>OutboxProcessor</c> hosted service polling the outbox table in W2.5.
/// </summary>
/// <remarks>
/// <para>
/// <b>The outbox pattern</b>: integration-event publication runs in the SAME
/// database transaction as the state change that caused it. The processor
/// reads pending rows and dispatches; if dispatch succeeds, the row is marked
/// processed; if it fails, the row stays and the processor retries (with
/// exponential backoff + dead-lettering after N attempts).
/// </para>
/// <para>
/// This abstraction stays minimal: write integration events as opaque
/// payloads. The concrete <see cref="System.Object"/> param is intentionally
/// non-generic so any integration-event shape can flow through; the actual
/// type lives in <c>BuildingBlocks.Contracts</c> (W2.7) and is serialized at
/// the persistence boundary.
/// </para>
/// </remarks>
public interface IOutbox
{
    /// <summary>
    /// Enqueues an integration event for later publication. Must be called
    /// from within the active transaction so the write is rolled back if
    /// the command fails.
    /// </summary>
    Task EnqueueAsync(object integrationEvent, CancellationToken cancellationToken = default);
}
