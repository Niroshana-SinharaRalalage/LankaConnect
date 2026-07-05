using LankaConnect.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Contracts.IntegrationEvents;
using LankaConnect.BuildingBlocks.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
namespace LankaConnect.BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Wave 6.5.a: concrete impl of <see cref="IIntegrationEventOutbox{TDbContext}"/>
/// delegating to the module-scoped <see cref="OutboxIntegrationEventDispatcher{TDbContext}"/>.
/// Adapter layer keeps handler dependencies pointed at
/// <see cref="LankaConnect.Application.Common.Interfaces"/> (module-agnostic)
/// while the concrete <see cref="OutboxIntegrationEventDispatcher{TDbContext}"/>
/// remains a BuildingBlocks-level primitive.
/// </summary>
/// <typeparam name="TDbContext">Module DbContext hosting the outbox table.</typeparam>
public sealed class IntegrationEventOutbox<TDbContext> : IIntegrationEventOutbox<TDbContext>
    where TDbContext : DbContext
{
    private readonly OutboxIntegrationEventDispatcher<TDbContext> _dispatcher;

    public IntegrationEventOutbox(OutboxIntegrationEventDispatcher<TDbContext> dispatcher)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    }

    /// <inheritdoc />
    public Task EnqueueAsync(IntegrationEventBase integrationEvent, CancellationToken cancellationToken = default)
    {
        // OutboxIntegrationEventDispatcher.PublishAsync stages the row on the
        // module DbContext's change tracker without SaveChangesAsync — the
        // caller's UoW commits it atomically with the state change. This
        // adapter preserves those semantics unchanged.
        return _dispatcher.PublishAsync(integrationEvent, cancellationToken);
    }
}
