namespace LankaConnect.BuildingBlocks.Contracts.IntegrationEvents;

/// <summary>
/// Cross-module integration-event publish API. Module application code
/// injects this and calls <see cref="PublishAsync"/> to broadcast a fact
/// that downstream subscribers (other modules) may react to.
/// </summary>
/// <remarks>
/// <para>
/// <b>Where this fits</b>: in the AllInOne deployment, the concrete
/// implementation in <c>BuildingBlocks.Infrastructure</c> enqueues the event
/// to the per-module outbox table inside the active EF transaction (so the
/// publish is rolled back if the command fails). A hosted
/// <c>OutboxProcessor&lt;TDbContext&gt;</c> polls the outbox and routes
/// pending events to in-process subscribers via MediatR
/// <c>IPublisher</c>.
/// </para>
/// <para>
/// In the Phase B split deployment, the same publish API serializes the
/// event onto a Service Bus topic / Kafka stream; subscribers in different
/// deployables consume it. Module code is identical across both modes —
/// only the composition-root registration differs.
/// </para>
/// <para>
/// <b>Idempotency contract</b>: publishing the same event twice (same
/// <see cref="IntegrationEventBase.EventId"/>) is the caller's concern at
/// the at-source side — the outbox/processor pair guarantees at-least-once
/// downstream delivery, so subscribers MUST be idempotent.
/// </para>
/// </remarks>
public interface IIntegrationEventDispatcher
{
    /// <summary>
    /// Enqueues an integration event for publication to all subscribed
    /// modules. Must be invoked inside the active transaction so the
    /// publish is rolled back atomically with the originating state change.
    /// </summary>
    /// <param name="integrationEvent">The event to publish.</param>
    /// <param name="cancellationToken">Cooperative cancellation.</param>
    Task PublishAsync(
        IntegrationEventBase integrationEvent,
        CancellationToken cancellationToken = default);
}
