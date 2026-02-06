using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using LankaConnect.Domain.Common;
using Serilog;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;
    private readonly ILogger _logger;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
        _logger = Log.ForContext<UnitOfWork>();
    }

    public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "Commit"))
        {
            _logger.Information("[Phase 6A.74 RCA] UnitOfWork.CommitAsync called - forwarding to AppDbContext.CommitAsync");
            var changes = await _context.CommitAsync(cancellationToken);
            _logger.Information("[Phase 6A.74 RCA] UnitOfWork.CommitAsync completed - {ChangeCount} changes committed", changes);
            return changes;
        }
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "BeginTransaction"))
        {
            _logger.Information("Beginning database transaction");
            _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            
            using (LogContext.PushProperty("TransactionId", _transaction.TransactionId))
            {
                _logger.Information("Database transaction started with ID {TransactionId}", _transaction.TransactionId);
            }
        }
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
            throw new InvalidOperationException("No transaction has been started.");

        using (LogContext.PushProperty("Operation", "CommitTransaction"))
        using (LogContext.PushProperty("TransactionId", _transaction.TransactionId))
        {
            _logger.Information("Committing database transaction {TransactionId}", _transaction.TransactionId);

            try
            {
                var changes = await _context.CommitAsync(cancellationToken);
                await _transaction.CommitAsync(cancellationToken);
                
                _logger.Information("Successfully committed database transaction {TransactionId} with {ChangeCount} changes", 
                    _transaction.TransactionId, changes);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to commit database transaction {TransactionId}, rolling back", _transaction.TransactionId);
                await RollbackTransactionAsync(cancellationToken);
                throw;
            }
            finally
            {
                _transaction.Dispose();
                _transaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
            throw new InvalidOperationException("No transaction has been started.");

        using (LogContext.PushProperty("Operation", "RollbackTransaction"))
        using (LogContext.PushProperty("TransactionId", _transaction.TransactionId))
        {
            _logger.Warning("Rolling back database transaction {TransactionId}", _transaction.TransactionId);

            try
            {
                await _transaction.RollbackAsync(cancellationToken);
                _logger.Information("Successfully rolled back database transaction {TransactionId}", _transaction.TransactionId);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error occurred while rolling back database transaction {TransactionId}", _transaction.TransactionId);
            }
            finally
            {
                _transaction.Dispose();
                _transaction = null;
            }
        }
    }

    /// <summary>
    /// Phase 6A.61: Clears all tracked entities except those of the specified type.
    /// Used to detach EmailMessage entities before committing EventNotificationHistory to avoid concurrency conflicts.
    /// </summary>
    public async Task ClearChangeTrackerExceptAsync<TEntity>(CancellationToken cancellationToken = default) where TEntity : class
    {
        using (LogContext.PushProperty("Operation", "ClearChangeTrackerExcept"))
        {
            var entitiesToDetach = _context.ChangeTracker.Entries()
                .Where(e => !(e.Entity is TEntity))
                .ToList();

            _logger.Debug("[Phase 6A.61] Detaching {Count} non-{EntityType} entities from ChangeTracker",
                entitiesToDetach.Count, typeof(TEntity).Name);

            foreach (var entry in entitiesToDetach)
            {
                entry.State = EntityState.Detached;
            }

            _logger.Information("[Phase 6A.61] Successfully detached {Count} entities, keeping only {EntityType} entities tracked",
                entitiesToDetach.Count, typeof(TEntity).Name);

            await Task.CompletedTask;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}