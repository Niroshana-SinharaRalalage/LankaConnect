using LankaConnect.BuildingBlocks.Application.Abstractions;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Users.Enums;

namespace LankaConnect.Domain.Users;

/// <summary>
/// Repository for the <see cref="User"/> aggregate. W3B (2026-06-05) migrated from
/// the legacy generic <c>IRepository&lt;T&gt;</c> base to
/// <see cref="IAggregateRepository{TAggregate, TId}"/> per ADR-010 — generic
/// predicate-based query methods are forbidden; each method declares explicit
/// intent.
/// </summary>
public interface IUserRepository : IAggregateRepository<User, Guid>
{
    // ---------- Base CRUD (previously inherited from IRepository<T>) ----------
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    void Update(User user);
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    // ---------- Named query methods ----------
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);
    Task<bool> ExistsWithEmailAsync(Email email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 6A.133: Search active users by name, email, or phone for co-organizer linking.
    /// Returns max 10 results, excludes specified user ID (typically current user).
    /// </summary>
    Task<IReadOnlyList<User>> SearchUsersAsync(string searchTerm, Guid? excludeUserId = null, int maxResults = 10, CancellationToken cancellationToken = default);

    // Authentication-related methods
    Task<User?> GetByRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailVerificationTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<User?> GetByPasswordResetTokenAsync(string token, CancellationToken cancellationToken = default);

    // Entra External ID authentication methods
    Task<User?> GetByExternalProviderIdAsync(string externalProviderId, CancellationToken cancellationToken = default);

    // Phase 6A.5: Admin approval workflow
    Task<IReadOnlyList<User>> GetUsersWithPendingRoleUpgradesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 6A.29: Get user full names by their IDs (for badge creator display)
    /// </summary>
    Task<Dictionary<Guid, string>> GetUserNamesAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 6A.64: Get user emails by their IDs (bulk query to eliminate N+1 problem in event notifications)
    /// Used by EventCancelledEventHandler to fetch all recipient emails in a single query
    /// </summary>
    Task<Dictionary<Guid, string>> GetEmailsByUserIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);

    // Phase 6A.90: Admin user management methods

    /// <summary>
    /// Phase 6A.90: Get paginated list of users with filtering for admin management
    /// </summary>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="searchTerm">Optional search term for name/email</param>
    /// <param name="roleFilter">Optional filter by role</param>
    /// <param name="isActiveFilter">Optional filter by active status</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple of items and total count for pagination</returns>
    Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? searchTerm = null,
        UserRole? roleFilter = null,
        bool? isActiveFilter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 6A.90: Get user counts grouped by role for admin statistics
    /// </summary>
    Task<Dictionary<UserRole, int>> GetUserCountsByRoleAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 6A.90: Get total count of active users
    /// </summary>
    Task<int> GetActiveUsersCountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 6A.90: Get total count of locked accounts
    /// </summary>
    Task<int> GetLockedAccountsCountAsync(CancellationToken cancellationToken = default);
}