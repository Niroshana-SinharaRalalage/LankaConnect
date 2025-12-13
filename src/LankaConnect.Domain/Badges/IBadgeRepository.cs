using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Badges;

/// <summary>
/// Repository interface for Badge aggregate root
/// Extends IRepository with Badge-specific query methods
/// </summary>
public interface IBadgeRepository : IRepository<Badge>
{
    /// <summary>
    /// Gets all active badges ordered by display order
    /// </summary>
    Task<IEnumerable<Badge>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a badge by name (case-insensitive)
    /// </summary>
    Task<Badge?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a badge with the given name already exists
    /// </summary>
    Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the next available display order
    /// </summary>
    Task<int> GetNextDisplayOrderAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 6A.27: Gets all expired badges (ExpiresAt is not null and is in the past)
    /// </summary>
    Task<IEnumerable<Badge>> GetExpiredBadgesAsync(CancellationToken cancellationToken = default);
}
