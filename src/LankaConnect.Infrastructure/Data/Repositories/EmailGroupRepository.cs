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
}
