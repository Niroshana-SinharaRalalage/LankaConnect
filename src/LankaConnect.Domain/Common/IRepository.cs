using System.Linq.Expressions;

namespace LankaConnect.Domain.Common;

// W3B (2026-06-05): constraint relaxed from `T : BaseEntity` to `T : class`
// — see Repository<T> notes in Infrastructure for rationale. The interface +
// implementation move in lockstep; both retire alongside per-aggregate
// hand-rolled repos during Wave 4 capability extraction (ADR-010).
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<T?> FindFirstAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
    Task<int> CountAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    
    void Update(T entity);
    void UpdateRange(IEnumerable<T> entities);
    
    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    
    // Paging support
    Task<(IReadOnlyList<T> Items, int TotalCount)> GetPagedAsync(
        int page, 
        int pageSize, 
        Expression<Func<T, bool>>? predicate = null, 
        CancellationToken cancellationToken = default);
}