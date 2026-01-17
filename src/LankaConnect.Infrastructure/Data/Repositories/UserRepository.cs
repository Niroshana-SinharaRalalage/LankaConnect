using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Domain.Users;
using UserEmail = LankaConnect.Domain.Shared.ValueObjects.Email;
using System.Diagnostics;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    private readonly ILogger<UserRepository> _repoLogger;

    public UserRepository(
        AppDbContext context,
        ILogger<UserRepository> logger) : base(context)
    {
        _repoLogger = logger;
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
    /// Phase 6A.X: Added comprehensive logging with LogContext, Stopwatch, and PostgreSQL SqlState extraction
    /// </summary>
    public override async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetById"))
        using (LogContext.PushProperty("EntityType", "User"))
        using (LogContext.PushProperty("EntityId", id))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByIdAsync START: EntityId={EntityId}", id);

            try
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

                int metroAreaCount = 0;
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
                        metroAreaCount = metroAreaIds.Count;
                        user.SyncPreferredMetroAreaIdsFromEntities(metroAreaIds);
                    }
                }

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByIdAsync COMPLETE: EntityId={EntityId}, Found={Found}, Email={Email}, MetroAreaCount={MetroAreaCount}, Duration={ElapsedMs}ms",
                    id,
                    user != null,
                    user?.Email.Value,
                    metroAreaCount,
                    stopwatch.ElapsedMilliseconds);

                return user;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByIdAsync FAILED: EntityId={EntityId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    id,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<User?> GetByEmailAsync(UserEmail email, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByEmail"))
        using (LogContext.PushProperty("EntityType", "User"))
        using (LogContext.PushProperty("Email", email.Value))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByEmailAsync START: Email={Email}", email.Value);

            try
            {
                // Include RefreshTokens for proper tracking when adding/removing tokens
                // Remove AsNoTracking() to allow EF Core to track changes to RefreshTokens collection
                var result = await _dbSet
                    .Include(u => u.RefreshTokens)
                    .FirstOrDefaultAsync(u => u.Email.Value == email.Value, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByEmailAsync COMPLETE: Email={Email}, Found={Found}, UserId={UserId}, Duration={ElapsedMs}ms",
                    email.Value,
                    result != null,
                    result?.Id,
                    stopwatch.ElapsedMilliseconds);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByEmailAsync FAILED: Email={Email}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    email.Value,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
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
        // CRITICAL FIX: Remove AsNoTracking to enable token rotation
        // RefreshToken endpoint needs to update/revoke old tokens and add new ones
        // AsNoTracking prevents EF Core from tracking these changes
        return await _dbSet
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.RefreshTokens.Any(rt => rt.Token == refreshToken && rt.IsRevoked == false),
                cancellationToken);
    }

    public async Task<User?> GetByEmailVerificationTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        // Phase 6A.53: Remove AsNoTracking to enable email verification updates
        // Email verification endpoint needs to update IsEmailVerified flag
        // AsNoTracking prevents EF Core from tracking these changes
        return await _dbSet
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
    /// Phase 6A.29: Get user full names by their IDs (for badge creator display)
    /// </summary>
    public async Task<Dictionary<Guid, string>> GetUserNamesAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
    {
        var ids = userIds.ToList();
        if (!ids.Any()) return new Dictionary<Guid, string>();

        return await _dbSet
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(
                u => u.Id,
                u => $"{u.FirstName} {u.LastName}",
                cancellationToken);
    }

    /// <summary>
    /// Phase 6A.64: Get user emails by their IDs (bulk query to eliminate N+1 problem in event notifications)
    /// This method replaces the N+1 query pattern where each registration triggers a separate user lookup.
    /// Performance: 50 users with N+1 = 50 queries (~10 seconds). With this method = 1 query (~100ms).
    /// </summary>
    public async Task<Dictionary<Guid, string>> GetEmailsByUserIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default)
    {
        var ids = userIds.ToList();
        if (!ids.Any()) return new Dictionary<Guid, string>();

        return await _dbSet
            .AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(
                u => u.Id,
                u => u.Email.Value,
                cancellationToken);
    }

    /// <summary>
    /// Override to sync shadow navigation for metro areas when adding new user
    /// Phase 6A.9 FIX: When creating a new user with metro areas, the domain's _preferredMetroAreaIds list
    /// contains the metro area GUIDs, but the shadow navigation _preferredMetroAreaEntities needs to be populated
    /// with actual MetroArea entities for EF Core to create the many-to-many junction table rows.
    /// Phase 6A.X: Added comprehensive logging with LogContext, Stopwatch, and PostgreSQL SqlState extraction
    /// </summary>
    public override async Task AddAsync(User entity, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "Add"))
        using (LogContext.PushProperty("EntityType", "User"))
        using (LogContext.PushProperty("EntityId", entity.Id))
        using (LogContext.PushProperty("MetroAreaCount", entity.PreferredMetroAreaIds.Count))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "AddAsync START: EntityId={EntityId}, Email={Email}, MetroAreaCount={MetroAreaCount}",
                entity.Id,
                entity.Email.Value,
                entity.PreferredMetroAreaIds.Count);

            try
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

                    _repoLogger.LogDebug(
                        "AddAsync: Loaded {MetroAreaEntityCount} metro area entities for syncing",
                        metroAreaEntities.Count);

                    // Access shadow navigation using EF Core's Entry API
                    var metroAreasCollection = _context.Entry(entity).Collection("_preferredMetroAreaEntities");

                    // Set the loaded entities into the shadow navigation
                    // EF Core will detect this and create rows in user_preferred_metro_areas junction table
                    metroAreasCollection.CurrentValue = metroAreaEntities;
                }

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "AddAsync COMPLETE: EntityId={EntityId}, Email={Email}, MetroAreasSynced={MetroAreasSynced}, Duration={ElapsedMs}ms",
                    entity.Id,
                    entity.Email.Value,
                    entity.PreferredMetroAreaIds.Any(),
                    stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "AddAsync FAILED: EntityId={EntityId}, Email={Email}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    entity.Id,
                    entity.Email.Value,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }
}