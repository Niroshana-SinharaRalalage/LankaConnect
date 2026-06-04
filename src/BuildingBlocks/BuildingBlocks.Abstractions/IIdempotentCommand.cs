namespace LankaConnect.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Marker for a command that must be processed at-most-once. The caller
/// supplies an idempotency key (typically a client-generated GUID); the
/// <see cref="Behaviors.IdempotencyBehavior{TRequest,TResponse}"/> checks
/// the key against the per-module store and short-circuits with the prior
/// response if seen before.
/// </summary>
/// <remarks>
/// Use for commands where retries are normal (payment intents, mobile-network
/// flakey RSVPs, webhook redeliveries). Don't use for cheap fire-and-forget
/// commands that have no observable user effect — the store cost isn't worth it.
/// </remarks>
public interface IIdempotentCommand<out TResponse> : ICommand<TResponse>
{
    /// <summary>
    /// Unique key identifying this logical operation. Same key + same request
    /// payload = replay; same key + different payload = caller bug (the
    /// idempotency store may reject).
    /// </summary>
    Guid IdempotencyKey { get; }
}
