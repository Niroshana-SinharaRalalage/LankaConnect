namespace LankaConnect.BuildingBlocks.Application.Abstractions;

/// <summary>
/// Per-module unit of work. Wraps a single logical operation in a database
/// transaction; commits on success, rolls back on failure or unhandled exception.
/// </summary>
/// <remarks>
/// <para>
/// Concrete implementation lives in W2.5 <c>BuildingBlocks.Infrastructure</c>
/// and binds to the module's <c>DbContext</c>. Each module gets its own
/// instance (scoped DI) — modules MUST NOT share a UoW because that would
/// couple their transaction boundaries.
/// </para>
/// <para>
/// <see cref="Behaviors.TransactionBehavior{TRequest,TResponse}"/> wraps every
/// <see cref="ICommand{TResponse}"/> in a UoW; queries skip it. The behavior
/// itself does NOT call the underlying ORM's SaveChanges — that's the
/// responsibility of the handler / repository pattern. The UoW commits the
/// changes already persisted in the tracking context.
/// </para>
/// </remarks>
public interface IUnitOfWork
{
    /// <summary>Begins a new transaction. Idempotent — re-entering is a no-op.</summary>
    Task BeginAsync(CancellationToken cancellationToken = default);

    /// <summary>Commits the active transaction and persists pending changes.</summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>Rolls back the active transaction discarding pending changes.</summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
