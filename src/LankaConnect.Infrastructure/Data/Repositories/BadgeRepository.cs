using Microsoft.EntityFrameworkCore;
using LankaConnect.Domain.Badges;
using Serilog;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data.Repositories;

/// <summary>
/// Repository implementation for Badge aggregate root
/// </summary>
public class BadgeRepository : Repository<Badge>, IBadgeRepository
{
    public BadgeRepository(AppDbContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Badge>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetAllActiveBadges"))
        {
            _logger.Debug("Getting all active badges ordered by display order");

            var badges = await _dbSet
                .Where(b => b.IsActive)
                .OrderBy(b => b.DisplayOrder)
                .ThenBy(b => b.Name)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            _logger.Debug("Retrieved {Count} active badges", badges.Count);
            return badges;
        }
    }

    /// <inheritdoc />
    public async Task<Badge?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetBadgeByName"))
        using (LogContext.PushProperty("BadgeName", name))
        {
            _logger.Debug("Getting badge by name: {BadgeName}", name);

            var badge = await _dbSet
                .Where(b => b.Name.ToLower() == name.ToLower())
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            _logger.Debug("Badge with name {BadgeName} {Result}", name, badge != null ? "found" : "not found");
            return badge;
        }
    }

    /// <inheritdoc />
    public async Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "ExistsBadgeByName"))
        using (LogContext.PushProperty("BadgeName", name))
        {
            _logger.Debug("Checking if badge exists by name: {BadgeName}", name);

            var exists = await _dbSet
                .AnyAsync(b => b.Name.ToLower() == name.ToLower(), cancellationToken);

            _logger.Debug("Badge with name {BadgeName} {Result}", name, exists ? "exists" : "does not exist");
            return exists;
        }
    }

    /// <inheritdoc />
    public async Task<int> GetNextDisplayOrderAsync(CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetNextBadgeDisplayOrder"))
        {
            _logger.Debug("Getting next available display order for badges");

            var maxDisplayOrder = await _dbSet
                .MaxAsync(b => (int?)b.DisplayOrder, cancellationToken) ?? 0;

            var nextOrder = maxDisplayOrder + 1;
            _logger.Debug("Next available display order: {NextOrder}", nextOrder);
            return nextOrder;
        }
    }
}
