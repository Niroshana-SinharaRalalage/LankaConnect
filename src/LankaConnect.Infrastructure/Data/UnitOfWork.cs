using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using Serilog;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork, IMultiContextUnitOfWork
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

    /// <summary>
    /// Wave 6.5.a: multi-context atomic commit. Opens ONE transaction on
    /// <see cref="AppDbContext"/>, enrolls each module context via
    /// <c>Database.UseTransaction</c>, saves each in order, then commits.
    /// Any throw between saves rolls back all enrolled contexts.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Retires the "self-saving repository" pattern (ADR-006 transitional).
    /// See F30a (commit <c>f0b684a0</c>) for the production data-loss failure
    /// that motivated this API. See ADR-005 for the rejection of
    /// <c>System.Transactions.TransactionScope</c>.
    /// </para>
    /// <para>
    /// <b>Domain event dispatch</b>: <see cref="AppDbContext.CommitAsync"/>
    /// dispatches domain events on entities tracked by <see cref="AppDbContext"/>
    /// after its own <c>SaveChangesAsync</c>. Module contexts do NOT dispatch
    /// domain events on their own tracked entities in this overload — that
    /// coordination is deferred to per-module <c>OnSaveChanges</c> interceptors
    /// authored during Wave 6.5.b-d migrations. This overload's responsibility
    /// is atomicity of PERSISTENCE across contexts; dispatch is a module concern.
    /// </para>
    /// <para>
    /// <b>Zero-context call</b>: passing an empty <paramref name="moduleContexts"/>
    /// array is equivalent to <see cref="CommitAsync(CancellationToken)"/> — the
    /// single-context call site continues to work for handlers not yet migrated.
    /// </para>
    /// </remarks>
    public async Task<int> CommitAsync(DbContext[] moduleContexts, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(moduleContexts);

        // Fast path: no module contexts — behave like the single-context overload.
        if (moduleContexts.Length == 0)
        {
            return await CommitAsync(cancellationToken);
        }

        using (LogContext.PushProperty("Operation", "CommitMultiContext"))
        {
            _logger.Information(
                "[Wave 6.5.a] Multi-context CommitAsync starting: {ModuleContextCount} module context(s) enrolled",
                moduleContexts.Length);

            // Open a transaction on AppDbContext and enroll each module context.
            // If AppDbContext already has an ambient transaction (e.g. from
            // BeginTransactionAsync above), reuse it; otherwise open one.
            var ownedTransaction = _transaction is null;
            var transaction = _transaction ?? await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                // Enroll each module context in the AppDbContext transaction.
                // context.Database.UseTransaction is the supported Npgsql pattern
                // for cross-DbContext atomicity within a single connection.
                foreach (var moduleContext in moduleContexts)
                {
                    ArgumentNullException.ThrowIfNull(moduleContext);
                    await moduleContext.Database.UseTransactionAsync(
                        transaction.GetDbTransaction(),
                        cancellationToken);
                }

                // Save AppDbContext first — its CommitAsync dispatches domain
                // events for entities tracked there.
                var appChanges = await _context.CommitAsync(cancellationToken);

                // Save each module context in order. Module contexts' own
                // domain-event dispatch is deferred to their per-module
                // interceptors (Wave 6.5.b-d).
                int totalChanges = appChanges;
                foreach (var moduleContext in moduleContexts)
                {
                    var moduleChanges = await moduleContext.SaveChangesAsync(cancellationToken);
                    totalChanges += moduleChanges;

                    _logger.Debug(
                        "[Wave 6.5.a] Module context {ContextType} saved {ChangeCount} changes",
                        moduleContext.GetType().Name,
                        moduleChanges);
                }

                // Commit only if we opened the transaction here. If the caller
                // wrapped this in BeginTransactionAsync/CommitTransactionAsync
                // themselves, they own the commit.
                if (ownedTransaction)
                {
                    await transaction.CommitAsync(cancellationToken);
                }

                _logger.Information(
                    "[Wave 6.5.a] Multi-context CommitAsync completed: {TotalChanges} total changes across {ContextCount} context(s)",
                    totalChanges,
                    moduleContexts.Length + 1);

                return totalChanges;
            }
            catch (Exception ex)
            {
                _logger.Error(
                    ex,
                    "[Wave 6.5.a] Multi-context CommitAsync FAILED; rolling back {ContextCount} context(s)",
                    moduleContexts.Length + 1);

                if (ownedTransaction)
                {
                    try
                    {
                        await transaction.RollbackAsync(cancellationToken);
                    }
                    catch (Exception rollbackEx)
                    {
                        _logger.Error(rollbackEx, "[Wave 6.5.a] Rollback itself failed");
                    }
                }

                throw;
            }
            finally
            {
                if (ownedTransaction)
                {
                    await transaction.DisposeAsync();
                }
            }
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