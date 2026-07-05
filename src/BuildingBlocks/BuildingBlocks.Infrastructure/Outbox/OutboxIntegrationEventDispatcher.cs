using System.Text.Json;
using ContractsDispatcher = LankaConnect.BuildingBlocks.Contracts.IntegrationEvents.IIntegrationEventDispatcher;
using LankaConnect.BuildingBlocks.Contracts.IntegrationEvents;
using Microsoft.EntityFrameworkCore;
namespace LankaConnect.BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// AllInOne (single-deployable) concrete implementation of the Contracts-level
/// <see cref="IIntegrationEventDispatcher"/>. Serializes the typed event +
/// writes one <see cref="OutboxMessage"/> row to the calling module's DbContext.
/// </summary>
/// <typeparam name="TDbContext">The module-specific DbContext that hosts the
/// module's <c>outbox</c> table (e.g. <c>NotificationsDbContext</c>,
/// <c>CommunicationsDbContext</c>).</typeparam>
/// <remarks>
/// <para>
/// <b>Transaction semantics</b>: the row is ADDED to the context's
/// <c>DbSet&lt;OutboxMessage&gt;</c> but <see cref="DbContext.SaveChangesAsync"/>
/// is NOT called here. The caller's command handler / UoW commits as part of
/// the same transaction that effected the state change — outbox row + state
/// change land atomically (the outbox pattern's core guarantee).
/// </para>
/// <para>
/// <b>Phase B swap-out</b>: when modules deploy independently, swap the DI
/// registration to a Service Bus / Kafka publisher that writes directly to the
/// transport. Module code (which depends only on
/// <see cref="IIntegrationEventDispatcher"/>) does not change.
/// </para>
/// <para>
/// <b>Pickup</b>: the hosted <see cref="OutboxProcessor{TDbContext}"/> polls
/// the outbox table and dispatches via <see cref="IIntegrationEventDispatcher"/>
/// (the legacy Infrastructure-level string-based one — see
/// <see cref="MediatRIntegrationEventDispatcher"/>).
/// </para>
/// </remarks>
public sealed class OutboxIntegrationEventDispatcher<TDbContext> : ContractsDispatcher
    where TDbContext : DbContext
{
    /// <summary>
    /// JSON serializer options matching the OutboxProcessor's deserializer.
    /// Pinned here so producer and consumer stay format-compatible across
    /// future System.Text.Json version bumps.
    /// </summary>
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    private readonly TDbContext _dbContext;

    public OutboxIntegrationEventDispatcher(TDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc />
    public Task PublishAsync(
        IntegrationEventBase integrationEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        // Serialize via the concrete runtime type so derived record fields
        // (not declared on IntegrationEventBase) survive the round-trip.
        var payload = JsonSerializer.Serialize(
            integrationEvent,
            integrationEvent.GetType(),
            JsonOptions);

        var row = OutboxMessage.Create(
            eventType: integrationEvent.EventType,
            payload: payload,
            occurredAtUtc: integrationEvent.OccurredOnUtc.UtcDateTime);

        _dbContext.Set<OutboxMessage>().Add(row);

        // No SaveChangesAsync — caller's UoW commits atomically with the
        // originating state change. The cancellation token is intentionally
        // unused: this method only stages an entity in the change tracker.
        _ = cancellationToken;
        return Task.CompletedTask;
    }
}
