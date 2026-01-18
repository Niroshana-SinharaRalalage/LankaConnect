namespace LankaConnect.Domain.Common;

public interface IUnitOfWork : IDisposable
{
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 6A.61: Clears all tracked entities except those of the specified type.
    /// Used to detach EmailMessage entities before committing EventNotificationHistory.
    /// </summary>
    Task ClearChangeTrackerExceptAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class;
}