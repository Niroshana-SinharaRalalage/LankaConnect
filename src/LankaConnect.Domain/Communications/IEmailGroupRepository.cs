using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.Entities;

namespace LankaConnect.Domain.Communications;

/// <summary>
/// Repository interface for EmailGroup aggregate
/// Phase 6A.25: Email Groups Management
/// </summary>
public interface IEmailGroupRepository : IRepository<EmailGroup>
{
    /// <summary>
    /// Gets all email groups owned by a specific user
    /// </summary>
    Task<IReadOnlyList<EmailGroup>> GetByOwnerAsync(Guid ownerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active email groups (for admin view)
    /// </summary>
    Task<IReadOnlyList<EmailGroup>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a group with the given name already exists for the owner
    /// </summary>
    /// <param name="ownerId">Owner user ID</param>
    /// <param name="name">Group name to check</param>
    /// <param name="excludeId">Optional ID to exclude (for update scenarios)</param>
    Task<bool> NameExistsForOwnerAsync(
        Guid ownerId,
        string name,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);
}
