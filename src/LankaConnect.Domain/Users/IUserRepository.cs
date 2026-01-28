using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Users.Enums;

namespace LankaConnect.Domain.Users;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);
    Task<bool> ExistsWithEmailAsync(Email email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default);
    
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