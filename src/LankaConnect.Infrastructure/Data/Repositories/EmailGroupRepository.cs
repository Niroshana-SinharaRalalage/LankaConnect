using Microsoft.EntityFrameworkCore;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Communications.Entities;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for EmailGroup entity
/// Phase 6A.25: Email Groups Management
/// </summary>
public class EmailGroupRepository : Repository<EmailGroup>, IEmailGroupRepository
{
    public EmailGroupRepository(AppDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Gets all email groups owned by a specific user
    /// </summary>
    public async Task<IReadOnlyList<EmailGroup>> GetByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(g => g.OwnerId == ownerId && g.IsActive)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets all active email groups (for admin view)
    /// </summary>
    public async Task<IReadOnlyList<EmailGroup>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(g => g.IsActive)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Checks if a group with the given name already exists for the owner
    /// </summary>
    public async Task<bool> NameExistsForOwnerAsync(
        Guid ownerId,
        string name,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim().ToLowerInvariant();

        var query = _dbSet
            .Where(g => g.OwnerId == ownerId && g.IsActive);

        // Exclude the current group when checking for duplicates (for update scenarios)
        if (excludeId.HasValue)
        {
            query = query.Where(g => g.Id != excludeId.Value);
        }

        return await query
            .AnyAsync(g => g.Name.ToLower() == normalizedName, cancellationToken);
    }

    /// <summary>
    /// Gets multiple email groups by their IDs in a single query
    /// Phase 6A.32: Batch query to prevent N+1 problem (Fix #3)
    /// Used by events to fetch multiple email groups efficiently
    /// PostgreSQL optimizes WHERE id IN (...) queries very well
    /// </summary>
    public async Task<IReadOnlyList<EmailGroup>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        if (ids == null || !ids.Any())
        {
            return Array.Empty<EmailGroup>();
        }

        // Convert to list to avoid multiple enumeration
        var idList = ids.ToList();

        return await _dbSet
            .AsNoTracking()
            .Where(g => idList.Contains(g.Id))
            .ToListAsync(cancellationToken);
    }
}
