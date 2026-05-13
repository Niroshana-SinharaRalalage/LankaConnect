using LankaConnect.BuildingBlocks.Application.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LankaConnect.BuildingBlocks.Application.Behaviors;

/// <summary>
/// Forwards integration events captured by the handler to the outbox so they
/// will be published by the W2.5 <c>OutboxProcessor</c> hosted service after
/// the transaction commits.
/// </summary>
/// <remarks>
/// <para>
/// Handlers raise integration events via an <see cref="IIntegrationEventBuffer"/>
/// (a scoped DI service that this behavior reads from). Storing them in the
/// outbox happens INSIDE the active <see cref="IUnitOfWork"/> transaction —
/// so events and state change either both persist or both roll back.
/// </para>
/// <para>
/// Concrete <c>IIntegrationEventBuffer</c> implementation lives with the
/// <c>BaseDbContext</c> in W2.5 — it collects integration events raised on
/// aggregate roots during the request scope.
/// </para>
/// </remarks>
public sealed class OutboxBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
    private readonly IIntegrationEventBuffer _buffer;
    private readonly IOutbox _outbox;
    private readonly ILogger<OutboxBehavior<TRequest, TResponse>> _logger;

    public OutboxBehavior(
        IIntegrationEventBuffer buffer,
        IOutbox outbox,
        ILogger<OutboxBehavior<TRequest, TResponse>> logger)
    {
        _buffer = buffer;
        _outbox = outbox;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        var response = await next();

        // Drain any integration events the handler raised and enqueue them
        // in the outbox. This runs BEFORE TransactionBehavior commits so the
        // outbox-row write is part of the same Postgres transaction as the
        // state change — atomic by construction.
        var events = _buffer.DrainEvents();
        if (events.Count > 0)
        {
            foreach (var evt in events)
            {
                await _outbox.EnqueueAsync(evt, cancellationToken);
            }

            _logger.LogInformation(
                "Outbox: enqueued {EventCount} integration events for {RequestName}",
                events.Count,
                typeof(TRequest).Name);
        }

        return response;
    }
}

/// <summary>
/// Scoped buffer of integration events raised by aggregate roots during a
/// request. Concrete implementation lives with <c>BaseDbContext</c> (W2.5)
/// which can collect events from tracked entities on SaveChanges.
/// </summary>
public interface IIntegrationEventBuffer
{
    /// <summary>
    /// Returns + clears the buffered events. Subsequent calls within the same
    /// scope return empty.
    /// </summary>
    IReadOnlyList<object> DrainEvents();
}
