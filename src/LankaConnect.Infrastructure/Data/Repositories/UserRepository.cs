using Microsoft.EntityFrameworkCore;
using LankaConnect.Domain.Users;
using UserEmail = LankaConnect.Domain.Shared.ValueObjects.Email;

namespace LankaConnect.Infrastructure.Data.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(AppDbContext context) : base(context)
    {
    }

    /// <summary>
    /// Override to include Epic 1 Phase 3 navigation properties (CulturalInterests, Languages)
    /// Base Repository uses FindAsync which doesn't load OwnsMany collections
    /// Using AsSplitQuery() + explicit Include() loads OwnsMany collections and tracks changes
    /// CRITICAL: Do NOT use AsNoTracking() - we need tracking for UPDATE operations
    /// </summary>
    public override async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsSplitQuery()
            .Include(u => u.CulturalInterests)
            .Include(u => u.Languages)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(UserEmail email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email.Value == email.Value, cancellationToken);
    }

    public async Task<bool> ExistsWithEmailAsync(UserEmail email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(u => u.Email.Value == email.Value, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(u => u.IsActive)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<User>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return new List<User>();

        var normalizedSearchTerm = searchTerm.Trim().ToLower();

        return await _dbSet
            .AsNoTracking()
            .Where(u => u.IsActive && 
                       (u.FirstName.ToLower().Contains(normalizedSearchTerm) ||
                        u.LastName.ToLower().Contains(normalizedSearchTerm) ||
                        (u.FirstName + " " + u.LastName).ToLower().Contains(normalizedSearchTerm)))
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .ToListAsync(cancellationToken);
    }

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(rt => rt.Token == refreshToken && rt.IsRevoked == false),
                cancellationToken);
    }

    public async Task<User?> GetByEmailVerificationTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.EmailVerificationToken == token, cancellationToken);
    }

    public async Task<User?> GetByPasswordResetTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.PasswordResetToken == token, cancellationToken);
    }

    public async Task<User?> GetByExternalProviderIdAsync(string externalProviderId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.ExternalProviderId == externalProviderId, cancellationToken);
    }

    /// <summary>
    /// Phase 6A.5: Get all users with pending role upgrade requests awaiting admin approval
    /// </summary>
    public async Task<IReadOnlyList<User>> GetUsersWithPendingRoleUpgradesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(u => u.PendingUpgradeRole != null)
            .OrderBy(u => u.UpgradeRequestedAt)
            .ToListAsync(cancellationToken);
    }
}