using System.Linq.Expressions;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Modules.Communications.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Context;

namespace LankaConnect.Modules.Communications.Infrastructure.Common;

/// <summary>
/// Consult #21 Cat-A (2026-07-10) — Communications-owned repository base for the Communications
/// bounded context. Structural twin of <see cref="LankaConnect.Products.LankaEvents.Infrastructure.Common.ProductRepositoryBase{T}"/>
/// (introduced at Wave 6.5.f.0), re-parented from AppDbContext to CommunicationsDbContext.
///
/// The 3 Communications repos whose entities migrated to CommunicationsDbContext at 4C.c
/// (<see cref="Data.Repositories.EmailMessageRepository"/>, <see cref="Data.Repositories.EmailTemplateRepository"/>,
/// <see cref="Data.Repositories.UserEmailPreferencesRepository"/>) migrate to this base, dropping
/// their AppDbContext injection. EmailGroupRepository stays on AppDbContext + Repository&lt;T&gt; per
/// Consult #20 OUT-OF-SCOPE ruling (EmailGroup pending Consult #21 relocation to CommunicationsDbContext).
///
/// Deliberately does NOT self-save (Wave 6.5 rule). Commits live at the handler edge via
/// IMultiContextUnitOfWork; repositories stage on ChangeTracker only.
/// </summary>
public class CommunicationsRepositoryBase<T> : IRepository<T> where T : class
{
    private static readonly System.Reflection.PropertyInfo? IdProperty =
        typeof(T).GetProperty("Id");

    private static object? ExtractId(T entity) => IdProperty?.GetValue(entity);

    protected readonly CommunicationsDbContext _context;
    protected readonly DbSet<T> _dbSet;
    protected readonly ILogger _logger;

    protected CommunicationsRepositoryBase(CommunicationsDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
        _logger = Log.ForContext<CommunicationsRepositoryBase<T>>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetById"))
        using (LogContext.PushProperty("EntityType", typeof(T).Name))
        using (LogContext.PushProperty("EntityId", id))
        {
            _logger.Debug("Getting entity {EntityType} by ID {EntityId}", typeof(T).Name, id);
            var result = await _dbSet.FindAsync(new object[] { id }, cancellationToken);
            return result;
        }
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _dbSet.AsNoTracking().ToListAsync(cancellationToken);

    public virtual async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => await _dbSet.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);

    public virtual async Task<T?> FindFirstAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => await _dbSet.AsNoTracking().FirstOrDefaultAsync(predicate, cancellationToken);

    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => await _dbSet.AnyAsync(predicate, cancellationToken);

    public virtual async Task<int> CountAsync(CancellationToken cancellationToken = default)
        => await _dbSet.CountAsync(cancellationToken);

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        => await _dbSet.CountAsync(predicate, cancellationToken);

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "Add"))
        using (LogContext.PushProperty("EntityType", typeof(T).Name))
        using (LogContext.PushProperty("EntityId", ExtractId(entity)))
        {
            _logger.Information("Adding entity {EntityType} with ID {EntityId}", typeof(T).Name, ExtractId(entity));
            await _dbSet.AddAsync(entity, cancellationToken);
        }
    }

    public virtual async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        => await _dbSet.AddRangeAsync(entities, cancellationToken);

    public virtual void Update(T entity) => _dbSet.Update(entity);

    public virtual void UpdateRange(IEnumerable<T> entities) => _dbSet.UpdateRange(entities);

    public virtual void Remove(T entity) => _dbSet.Remove(entity);

    public virtual void RemoveRange(IEnumerable<T> entities) => _dbSet.RemoveRange(entities);

    public virtual async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet.FindAsync(new object[] { id }, cancellationToken);
        if (entity != null)
        {
            _dbSet.Remove(entity);
        }
    }

    public virtual async Task<(IReadOnlyList<T> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        Expression<Func<T, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking();
        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
