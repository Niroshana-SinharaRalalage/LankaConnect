using System.Text.Json;
using LankaConnect.BuildingBlocks.Application.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LankaConnect.BuildingBlocks.Application.Behaviors;

/// <summary>
/// Short-circuits <see cref="IIdempotentCommand{TResponse}"/> replays by
/// returning the stored response for a previously-seen idempotency key —
/// without re-executing the handler.
/// </summary>
/// <remarks>
/// <para>
/// First invocation: handler runs; response serialized via
/// <see cref="JsonSerializer"/>; stored under the command's
/// <see cref="IIdempotentCommand{TResponse}.IdempotencyKey"/>. Subsequent
/// invocations with the same key bypass the handler and deserialize the
/// stored payload.
/// </para>
/// <para>
/// Deserialization failure (e.g. response shape changed mid-deployment) falls
/// through to a fresh handler invocation — better to occasionally double-run
/// a command than serve a stale incompatible response. The store implementation
/// in W2.5 should also expire keys after a TTL so the table doesn't grow
/// without bound.
/// </para>
/// </remarks>
public sealed class IdempotencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IIdempotentCommand<TResponse>
{
    private readonly IIdempotencyStore _store;
    private readonly ILogger<IdempotencyBehavior<TRequest, TResponse>> _logger;

    public IdempotencyBehavior(
        IIdempotencyStore store,
        ILogger<IdempotencyBehavior<TRequest, TResponse>> logger)
    {
        _store = store;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        var key = request.IdempotencyKey;
        var existing = await _store.TryGetAsync(key, cancellationToken);

        if (existing is not null)
        {
            try
            {
                var cached = JsonSerializer.Deserialize<TResponse>(existing);
                if (cached is not null)
                {
                    _logger.LogInformation(
                        "Idempotency hit for {RequestName} (key {IdempotencyKey}) — returning cached response",
                        typeof(TRequest).Name,
                        key);
                    return cached;
                }
            }
            catch (JsonException ex)
            {
                // Shape drift between requests — fall through and re-run.
                _logger.LogWarning(
                    ex,
                    "Idempotency cache deserialize FAILED for {RequestName} (key {IdempotencyKey}); re-executing handler",
                    typeof(TRequest).Name,
                    key);
            }
        }

        var response = await next();

        try
        {
            var serialized = JsonSerializer.Serialize(response);
            await _store.PutAsync(key, serialized, cancellationToken);
        }
        catch (Exception ex)
        {
            // Audit-log style: storage failure must NOT roll back the business
            // operation that already succeeded. Future replays just won't
            // benefit from the cache.
            _logger.LogError(
                ex,
                "Idempotency cache PutAsync FAILED for {RequestName} (key {IdempotencyKey}) — replays will re-execute",
                typeof(TRequest).Name,
                key);
        }

        return response;
    }
}
