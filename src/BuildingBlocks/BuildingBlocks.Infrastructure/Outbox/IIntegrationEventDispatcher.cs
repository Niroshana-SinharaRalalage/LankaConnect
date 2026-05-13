namespace LankaConnect.BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Dispatches deserialized integration events to in-process handlers (AllInOne
/// deployment) or to an external bus (Azure Service Bus / Kafka later). The
/// <see cref="OutboxProcessor"/> consumes this abstraction so the dispatch
/// mechanism is pluggable per ADR-002.
/// </summary>
/// <remarks>
/// <para>
/// AllInOne implementation: cast to MediatR <c>INotification</c> via reflection
/// and publish via <c>IPublisher</c>; subscriber handlers in the same process
/// (other modules) receive the event.
/// </para>
/// <para>
/// Service Bus implementation (post Phase A): serialize the event payload to
/// a queue / topic; a worker in a different deployable consumes + handles.
/// Same abstraction; different concrete service registration at composition root.
/// </para>
/// </remarks>
public interface IIntegrationEventDispatcher
{
    /// <summary>
    /// Dispatches an integration event reconstructed from <paramref name="eventType"/>
    /// (AssemblyQualifiedName) + <paramref name="payload"/> (JSON).
    /// </summary>
    /// <exception cref="InvalidOperationException">If the event type can't be resolved.</exception>
    /// <exception cref="System.Text.Json.JsonException">If payload deserialization fails.</exception>
    Task DispatchAsync(string eventType, string payload, CancellationToken cancellationToken = default);
}
