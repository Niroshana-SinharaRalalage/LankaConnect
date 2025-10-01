using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using LankaConnect.Domain.Common;
using Serilog;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data.Repositories;

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;
    protected readonly ILogger _logger;

    protected Repository(AppDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
        _logger = Log.ForContext<Repository<T>>();
    }

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetById"))
        using (LogContext.PushProperty("EntityType", typeof(T).Name))
        using (LogContext.PushProperty("EntityId", id))
        {
            _logger.Debug("Getting entity {EntityType} by ID {EntityId}", typeof(T).Name, id);
            var result = await _dbSet.FindAsync(new object[] { id }, cancellationToken);
            _logger.Debug("Entity {EntityType} with ID {EntityId} {Result}", typeof(T).Name, id, result != null ? "found" : "not found");
            return result;
        }
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetAll"))
        using (LogContext.PushProperty("EntityType", typeof(T).Name))
        {
            _logger.Debug("Getting all entities of type {EntityType}", typeof(T).Name);
            var result = await _dbSet.AsNoTracking().ToListAsync(cancellationToken);
            _logger.Debug("Retrieved {Count} entities of type {EntityType}", result.Count, typeof(T).Name);
            return result;
        }
    }

    public virtual async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "Find"))
        using (LogContext.PushProperty("EntityType", typeof(T).Name))
        {
            _logger.Debug("Finding entities of type {EntityType} with predicate", typeof(T).Name);
            var result = await _dbSet.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);
            _logger.Debug("Found {Count} entities of type {EntityType}", result.Count, typeof(T).Name);
            return result;
        }
    }

    public virtual async Task<T?> FindFirstAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "FindFirst"))
        using (LogContext.PushProperty("EntityType", typeof(T).Name))
        {
            _logger.Debug("Finding first entity of type {EntityType} with predicate", typeof(T).Name);
            var result = await _dbSet.AsNoTracking().FirstOrDefaultAsync(predicate, cancellationToken);
            _logger.Debug("First entity of type {EntityType} {Result}", typeof(T).Name, result != null ? "found" : "not found");
            return result;
        }
    }

    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "Exists"))
        using (LogContext.PushProperty("EntityType", typeof(T).Name))
        {
            _logger.Debug("Checking existence of entity type {EntityType} with predicate", typeof(T).Name);
            var result = await _dbSet.AnyAsync(predicate, cancellationToken);
            _logger.Debug("Entity of type {EntityType} {Result}", typeof(T).Name, result ? "exists" : "does not exist");
            return result;
        }
    }

    public virtual async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "Count"))
        using (LogContext.PushProperty("EntityType", typeof(T).Name))
        {
            _logger.Debug("Counting entities of type {EntityType}", typeof(T).Name);
            var result = await _dbSet.CountAsync(cancellationToken);
            _logger.Debug("Count of entities of type {EntityType}: {Count}", typeof(T).Name, result);
            return result;
        }
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "CountWithPredicate"))
        using (LogContext.PushProperty("EntityType", typeof(T).Name))
        {
            _logger.Debug("Counting entities of type {EntityType} with predicate", typeof(T).Name);
            var result = await _dbSet.CountAsync(predicate, cancellationToken);
            _logger.Debug("Count of entities of type {EntityType} with predicate: {Count}", typeof(T).Name, result);
            return result;
        }
    }

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "Add"))
        using (LogContext.PushProperty("EntityType", typeof(T).Name))
        using (LogContext.PushProperty("EntityId", entity.Id))
        {
            _logger.Information("Adding entity {EntityType} with ID {EntityId}", typeof(T).Name, entity.Id);
            await _dbSet.AddAsync(entity, cancellationToken);
        }
    }

    public virtual async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        var entitiesList = entities.ToList();
        using (LogContext.PushProperty("Operation", "AddRange"))
        using (LogContext.PushProperty("EntityType", typeof(T).Name))
        using (LogContext.PushProperty("Count", entitiesList.Count))
        {
            _logger.Information("Adding {Count} entities of type {EntityType}", entitiesList.Count, typeof(T).Name);
            await _dbSet.AddRangeAsync(entitiesList, cancellationToken);
        }
    }

    public virtual void Update(T entity)
    {
        using (LogContext.PushProperty("Operation", "Update"))
        using (LogContext.PushProperty("EntityType", typeof(T).Name))
        using (LogContext.PushProperty("EntityId", entity.Id))
        {
            _logger.Information("Updating entity {EntityType} with ID {EntityId}", typeof(T).Name, entity.Id);
            _dbSet.Update(entity);
        }
    }

    public virtual void UpdateRange(IEnumerable<T> entities)
    {
        var entitiesList = entities.ToList();
        using (LogContext.PushProperty("Operation", "UpdateRange"))
        using (LogContext.PushProperty("EntityType", typeof(T).Name))
        using (LogContext.PushProperty("Count", entitiesList.Count))
        {
            _logger.Information("Updating {Count} entities of type {EntityType}", entitiesList.Count, typeof(T).Name);
            _dbSet.UpdateRange(entitiesList);
        }
    }

    public virtual void Remove(T entity)
    {
        using (LogContext.PushProperty("Operation", "Remove"))
        using (LogContext.PushProperty("EntityType", typeof(T).Name))
        using (LogContext.PushProperty("EntityId", entity.Id))
        {
            _logger.Information("Removing entity {EntityType} with ID {EntityId}", typeof(T).Name, entity.Id);
            _dbSet.Remove(entity);
        }
    }

    public virtual void RemoveRange(IEnumerable<T> entities)
    {
        var entitiesList = entities.ToList();
        using (LogContext.PushProperty("Operation", "RemoveRange"))
        using (LogContext.PushProperty("EntityType", typeof(T).Name))
        using (LogContext.PushProperty("Count", entitiesList.Count))
        {
            _logger.Information("Removing {Count} entities of type {EntityType}", entitiesList.Count, typeof(T).Name);
            _dbSet.RemoveRange(entitiesList);
        }
    }

    public virtual async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "Delete"))
        using (LogContext.PushProperty("EntityType", typeof(T).Name))
        using (LogContext.PushProperty("EntityId", id))
        {
            _logger.Information("Deleting entity {EntityType} with ID {EntityId}", typeof(T).Name, id);
            var entity = await _dbSet.FindAsync(new object[] { id }, cancellationToken);
            if (entity != null)
            {
                _dbSet.Remove(entity);
                _logger.Information("Entity {EntityType} with ID {EntityId} marked for deletion", typeof(T).Name, id);
            }
            else
            {
                _logger.Warning("Entity {EntityType} with ID {EntityId} not found for deletion", typeof(T).Name, id);
            }
        }
    }

    public virtual async Task<(IReadOnlyList<T> Items, int TotalCount)> GetPagedAsync(
        int page, 
        int pageSize, 
        Expression<Func<T, bool>>? predicate = null, 
        CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetPaged"))
        using (LogContext.PushProperty("EntityType", typeof(T).Name))
        using (LogContext.PushProperty("Page", page))
        using (LogContext.PushProperty("PageSize", pageSize))
        {
            _logger.Debug("Getting paged entities of type {EntityType}, page {Page}, size {PageSize}", typeof(T).Name, page, pageSize);

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

            _logger.Debug("Retrieved {ItemCount} of {TotalCount} entities of type {EntityType}", items.Count, totalCount, typeof(T).Name);
            return (items, totalCount);
        }
    }
}