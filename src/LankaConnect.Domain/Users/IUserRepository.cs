using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared.ValueObjects;

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
}