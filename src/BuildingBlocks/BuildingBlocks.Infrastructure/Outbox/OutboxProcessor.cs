using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LankaConnect.BuildingBlocks.Infrastructure.Outbox;

/// <summary>
/// Hosted-service that polls an outbox table on an interval and dispatches
/// pending messages via <see cref="IIntegrationEventDispatcher"/>. On dispatch
/// failure, records the error and increments retry count; rows that exceed
/// <see cref="OutboxMessage.MaxRetries"/> are moved to
/// <see cref="DeadLetterMessage"/>.
/// </summary>
/// <typeparam name="TDbContext">
/// Module-specific DbContext exposing <c>DbSet&lt;OutboxMessage&gt;</c> and
/// <c>DbSet&lt;DeadLetterMessage&gt;</c>. The processor is generic over the
/// context so each module gets its own (different schemas, different polling
/// budgets later).
/// </typeparam>
/// <remarks>
/// <para>
/// <b>Idempotency</b>: dispatching the same outbox row twice is safe because
/// the row is marked <see cref="OutboxMessage.ProcessedAt"/> on success;
/// subsequent polls skip rows where ProcessedAt is non-null. Downstream
/// handlers are expected to be idempotent (driven via the
/// <c>BuildingBlocks.Application.Behaviors.IdempotencyBehavior</c> if they
/// need it).
/// </para>
/// <para>
/// <b>Batching</b>: pulls up to <see cref="BatchSize"/> pending rows per tick;
/// dispatches sequentially within the batch. Parallel dispatch is intentionally
/// avoided because (a) handlers may share scoped dependencies, (b) ordering
/// matters for many integration-event flows.
/// </para>
/// </remarks>
public sealed class OutboxProcessor<TDbContext> : BackgroundService
    where TDbContext : DbContext
{
    /// <summary>Maximum rows pulled per poll tick.</summary>
    public const int BatchSize = 50;

    /// <summary>Default poll interval — 5 seconds; module-specific contexts may override via DI.</summary>
    public static readonly TimeSpan DefaultPollInterval = TimeSpan.FromSeconds(5);

    private readonly IServiceProvider _services;
    private readonly ILogger<OutboxProcessor<TDbContext>> _logger;
    private readonly TimeSpan _pollInterval;

    public OutboxProcessor(
        IServiceProvider services,
        ILogger<OutboxProcessor<TDbContext>> logger,
        TimeSpan? pollInterval = null)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _pollInterval = pollInterval ?? DefaultPollInterval;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "OutboxProcessor<{Context}> starting (interval {Interval}, batch {BatchSize})",
            typeof(TDbContext).Name,
            _pollInterval,
            BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Catch-all to keep the hosted service alive across transient
                // DB outages / dispatch crashes — next tick will retry.
                _logger.LogError(ex, "OutboxProcessor<{Context}> tick failed", typeof(TDbContext).Name);
            }

            try
            {
                await Task.Delay(_pollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // graceful shutdown
                break;
            }
        }

        _logger.LogInformation("OutboxProcessor<{Context}> stopped", typeof(TDbContext).Name);
    }

    /// <summary>
    /// Processes one batch — exposed for tests so they can drive ticks
    /// synchronously without waiting on the interval timer.
    /// </summary>
    public async Task ProcessBatchOnceAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TDbContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<IIntegrationEventDispatcher>();

        var pending = await db.Set<OutboxMessage>()
            .Where(m => m.ProcessedAt == null)
            .OrderBy(m => m.OccurredAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (pending.Count == 0)
        {
            return;
        }

        _logger.LogDebug(
            "OutboxProcessor<{Context}>: dispatching batch of {Count}",
            typeof(TDbContext).Name,
            pending.Count);

        foreach (var message in pending)
        {
            try
            {
                await dispatcher.DispatchAsync(message.EventType, message.Payload, cancellationToken);
                message.MarkProcessed(DateTime.UtcNow);
                _logger.LogInformation(
                    "OutboxProcessor: dispatched {EventType} (id {OutboxId})",
                    message.EventType,
                    message.Id);
            }
            catch (Exception ex)
            {
                message.RecordFailure(ex.Message);
                _logger.LogWarning(
                    ex,
                    "OutboxProcessor: dispatch FAILED for {EventType} (id {OutboxId}, retry {RetryCount}/{Max})",
                    message.EventType,
                    message.Id,
                    message.RetryCount,
                    OutboxMessage.MaxRetries);

                if (message.ShouldDeadLetter)
                {
                    db.Set<DeadLetterMessage>().Add(DeadLetterMessage.FromOutboxMessage(message, DateTime.UtcNow));
                    db.Set<OutboxMessage>().Remove(message);
                    _logger.LogError(
                        "OutboxProcessor: DEAD-LETTERED {EventType} (id {OutboxId}) after {RetryCount} retries",
                        message.EventType,
                        message.Id,
                        message.RetryCount);
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
