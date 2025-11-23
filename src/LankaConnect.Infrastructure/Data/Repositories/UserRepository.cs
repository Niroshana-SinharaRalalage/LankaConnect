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
    /// + Phase 6A.9: Include _preferredMetroAreaEntities shadow navigation for many-to-many persistence
    /// Base Repository uses FindAsync which doesn't load OwnsMany collections
    /// Using AsSplitQuery() + explicit Include() loads OwnsMany collections and tracks changes
    /// CRITICAL: Do NOT use AsNoTracking() - we need tracking for UPDATE operations
    /// ARCHITECTURE NOTE: OwnsMany collections with nested OwnsOne value objects (like Languages.Language)
    /// are automatically loaded by EF Core due to AutoInclude() configuration in UserConfiguration.cs.
    /// However, we keep explicit Include() for clarity and to ensure proper split query optimization.
    /// The nested LanguageCode owned entity is loaded automatically - no ThenInclude() needed.
    /// Phase 6A.9 FIX: After loading, sync shadow navigation entities to domain's metro area ID list
    /// </summary>
    public override async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _dbSet
            .AsSplitQuery()
            .Include(u => u.CulturalInterests)
            .Include(u => u.Languages)
            .Include(u => u.ExternalLogins)
            // CRITICAL FIX Phase 6A.9: Load shadow navigation for metro areas many-to-many per ADR-009
            // This populates _preferredMetroAreaEntities so EF Core can track changes to junction table
            .Include("_preferredMetroAreaEntities")  // String-based for shadow property
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        // Phase 6A.9 FIX: Sync loaded shadow navigation entities to domain's metro area ID list
        // This bridges the gap between EF Core's entity references and domain's business logic IDs
        if (user != null)
        {
            // Access the shadow navigation using EF Core's Entry API
            var metroAreasCollection = _context.Entry(user).Collection("_preferredMetroAreaEntities");
            var metroAreaEntities = metroAreasCollection.CurrentValue as IEnumerable<Domain.Events.MetroArea>;

            if (metroAreaEntities != null)
            {
                // Extract IDs and sync to domain's _preferredMetroAreaIds list
                var metroAreaIds = metroAreaEntities.Select(m => m.Id).ToList();
                user.SyncPreferredMetroAreaIdsFromEntities(metroAreaIds);
            }
        }

        return user;
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

    /// <summary>
    /// Override to sync shadow navigation for metro areas when adding new user
    /// Phase 6A.9 FIX: When creating a new user with metro areas, the domain's _preferredMetroAreaIds list
    /// contains the metro area GUIDs, but the shadow navigation _preferredMetroAreaEntities needs to be populated
    /// with actual MetroArea entities for EF Core to create the many-to-many junction table rows.
    /// </summary>
    public override async Task AddAsync(User entity, CancellationToken cancellationToken = default)
    {
        // Call base implementation to add entity to DbSet
        await base.AddAsync(entity, cancellationToken);

        // Sync metro areas from domain list to shadow navigation for persistence
        // This bridges the gap between domain's List<Guid> and EF Core's ICollection<MetroArea>
        if (entity.PreferredMetroAreaIds.Any())
        {
            // Load the MetroArea entities from the database based on the domain's ID list
            var metroAreaEntities = await _context.Set<Domain.Events.MetroArea>()
                .Where(m => entity.PreferredMetroAreaIds.Contains(m.Id))
                .ToListAsync(cancellationToken);

            // Access shadow navigation using EF Core's Entry API
            var metroAreasCollection = _context.Entry(entity).Collection("_preferredMetroAreaEntities");

            // Set the loaded entities into the shadow navigation
            // EF Core will detect this and create rows in user_preferred_metro_areas junction table
            metroAreasCollection.CurrentValue = metroAreaEntities;
        }
    }
}