using MediatR;

namespace LankaConnect.BuildingBlocks.Contracts.IntegrationEvents;

/// <summary>
/// Abstract base for every cross-module integration event. Concrete events
/// derive from this AND a version marker (e.g. <see cref="IIntegrationEventV1"/>)
/// so the dispatcher can route by schema version without reading the payload.
/// </summary>
/// <remarks>
/// <para>
/// <b>Wire-format ABI</b>: this type lives in <c>BuildingBlocks.Contracts</c>
/// which has zero <c>LankaConnect.*</c> ProjectReferences. Modules publishing
/// or subscribing to integration events only ever see this contract — they
/// never reach across module boundaries for the concrete event type. The
/// processor at the receiving side reconstructs the concrete type via
/// <see cref="EventType"/> (AssemblyQualifiedName).
/// </para>
/// <para>
/// <b>Immutability</b>: integration events are facts about something that
/// already happened — they should never be mutated after construction.
/// Implementing as a positional <c>record</c> in derived classes gives
/// value-equality + immutability + with-expression cloning for free.
/// </para>
/// <para>
/// <b>Versioning</b>: when an integration event schema changes in a
/// non-additive way, declare a NEW event class with the next version marker
/// (<c>IIntegrationEventV2</c> etc) rather than mutating the existing type.
/// Subscribers can implement handlers for both versions during the transition
/// window.
/// </para>
/// </remarks>
public abstract record IntegrationEventBase : INotification
{
    /// <summary>
    /// Stable per-event identifier. Used by subscribers for idempotency
    /// (de-dupe re-deliveries) and by tracing for correlation across hops.
    /// </summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// UTC timestamp of the originating state change. Set by the publishing
    /// module at construction; never overwritten downstream.
    /// </summary>
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Assembly-qualified type name of the concrete event class. Used by the
    /// outbox processor at the subscriber boundary to reconstruct the typed
    /// payload via reflection.
    /// </summary>
    public string EventType => GetType().AssemblyQualifiedName
        ?? throw new InvalidOperationException(
            $"Integration event {GetType().FullName} has no AssemblyQualifiedName.");

    /// <summary>
    /// Schema version derived from the marker interface implemented by the
    /// concrete event. Defaults to 1 when no marker is found (legacy events
    /// implicitly accepted as V1 to keep migration smooth).
    /// </summary>
    public virtual int Version => 1;
}
