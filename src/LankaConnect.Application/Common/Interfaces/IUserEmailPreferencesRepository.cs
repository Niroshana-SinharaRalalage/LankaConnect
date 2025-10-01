using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.Monitoring;
using LankaConnect.Domain.Communications;
using UserEmailPreferences = LankaConnect.Domain.Communications.Entities.UserEmailPreferences;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Repository interface for managing user email preferences
/// </summary>
public interface IUserEmailPreferencesRepository : IRepository<UserEmailPreferences>
{
    /// <summary>
    /// Gets user email preferences by user ID
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User email preferences or null if not found</returns>
    Task<UserEmailPreferences?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user email preferences by email address
    /// </summary>
    /// <param name="email">Email address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>User email preferences or null if not found</returns>
    Task<UserEmailPreferences?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    // AddAsync is inherited from IRepository<UserEmailPreferences>
    // UpdateAsync can be added as Update is available in base interface
    // DeleteAsync is inherited from IRepository<UserEmailPreferences>

    /// <summary>
    /// Gets users with specific email preferences (for bulk operations)
    /// </summary>
    /// <param name="preferenceName">Name of the preference to filter by</param>
    /// <param name="isEnabled">Whether the preference should be enabled</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of user email preferences</returns>
    Task<List<UserEmailPreferences>> GetUsersByPreferenceAsync(
        string preferenceName, 
        bool isEnabled, 
        CancellationToken cancellationToken = default);
}