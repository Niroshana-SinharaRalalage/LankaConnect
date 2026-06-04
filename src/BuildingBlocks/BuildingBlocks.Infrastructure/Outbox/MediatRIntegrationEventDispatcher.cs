using System.Text.Json;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LankaConnect.BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// AllInOne (single-deployable) implementation of the consume-side
/// <see cref="IIntegrationEventDispatcher"/>. The <see cref="OutboxProcessor{TDbContext}"/>
/// calls <see cref="DispatchAsync"/> for each pending outbox row; this class
/// reconstructs the CLR event type from its AssemblyQualifiedName, deserializes
/// the JSON payload, casts to <see cref="MediatR.INotification"/>, and
/// publishes via <see cref="IPublisher"/> so in-process subscribers receive it.
/// </summary>
/// <remarks>
/// <para>
/// <b>Subscriber pattern</b>: subscribing modules implement
/// <c>INotificationHandler&lt;TIntegrationEvent&gt;</c> for the concrete event
/// they care about. MediatR's assembly-scan picks them up via each module's
/// <c>AddXxxModule</c> extension (per the W3.9 playbook).
/// </para>
/// <para>
/// <b>Type resolution</b>: <c>Type.GetType(assemblyQualifiedName, throwOnError: true)</c>
/// requires the producing module's assembly to be loaded into the consumer's
/// process. In AllInOne, every module assembly is loaded — so this Just Works.
/// In Phase B per-deployable splits, swap this implementation for a Service
/// Bus consumer that doesn't need the producer's assembly.
/// </para>
/// <para>
/// <b>Idempotency</b>: at-least-once delivery is the outbox contract.
/// Subscribers MUST be idempotent (recommended: use
/// <see cref="LankaConnect.BuildingBlocks.Application.Abstractions.IIdempotencyStore"/>
/// keyed on
/// <c>LankaConnect.BuildingBlocks.Contracts.IntegrationEvents.IntegrationEventBase.EventId</c>).
/// </para>
/// </remarks>
public sealed class MediatRIntegrationEventDispatcher : IIntegrationEventDispatcher
{
    private readonly IPublisher _publisher;
    private readonly ILogger<MediatRIntegrationEventDispatcher> _logger;

    public MediatRIntegrationEventDispatcher(
        IPublisher publisher,
        ILogger<MediatRIntegrationEventDispatcher> logger)
    {
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task DispatchAsync(
        string eventType,
        string payload,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        var clrType = Type.GetType(eventType, throwOnError: false)
            ?? throw new InvalidOperationException(
                $"Integration event CLR type '{eventType}' could not be resolved. " +
                "Verify the producing module's assembly is loaded in this process.");

        // Deserialize through the matching JsonOptions the producer used, so
        // both sides agree on null-handling + casing semantics. Every
        // IntegrationEventBase implements MediatR.INotification (per W2.7
        // follow-up wiring) — the deserialized instance can be published directly.
        var notification = (INotification?)JsonSerializer.Deserialize(
            payload,
            clrType,
            OutboxIntegrationEventDispatcher<Microsoft.EntityFrameworkCore.DbContext>.JsonOptions);

        if (notification is null)
        {
            throw new InvalidOperationException(
                $"Integration event '{eventType}' deserialized to null. " +
                "Verify the payload JSON is well-formed.");
        }

        _logger.LogDebug(
            "Dispatching integration event {EventType} via MediatR.IPublisher",
            eventType);

        await _publisher.Publish(notification, cancellationToken);
    }
}
