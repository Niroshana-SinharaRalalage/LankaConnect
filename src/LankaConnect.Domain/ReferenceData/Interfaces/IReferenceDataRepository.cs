using LankaConnect.Domain.ReferenceData.Entities;

namespace LankaConnect.Domain.ReferenceData.Interfaces;

/// <summary>
/// Repository interface for unified reference data
/// Phase 6A.47: Unified Reference Data Architecture
/// </summary>
public interface IReferenceDataRepository
{
    // Unified operations - all enum types
    Task<IReadOnlyList<ReferenceValue>> GetByTypeAsync(string enumType, bool activeOnly = true, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReferenceValue>> GetByTypesAsync(IEnumerable<string> enumTypes, bool activeOnly = true, CancellationToken cancellationToken = default);
    Task<ReferenceValue?> GetByTypeAndCodeAsync(string enumType, string code, CancellationToken cancellationToken = default);
    Task<ReferenceValue?> GetByTypeAndIntValueAsync(string enumType, int intValue, CancellationToken cancellationToken = default);
    Task<ReferenceValue?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    // DEPRECATED: Legacy methods for backward compatibility - these will be removed in Phase 6A.48
    [Obsolete("Use GetByTypeAsync(\"EventCategory\") instead")]
    Task<IReadOnlyList<EventCategoryRef>> GetEventCategoriesAsync(bool activeOnly = true, CancellationToken cancellationToken = default);

    [Obsolete("Use GetByIdAsync() instead")]
    Task<EventCategoryRef?> GetEventCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default);

    [Obsolete("Use GetByTypeAndCodeAsync(\"EventCategory\", code) instead")]
    Task<EventCategoryRef?> GetEventCategoryByCodeAsync(string code, CancellationToken cancellationToken = default);

    [Obsolete("Use GetByTypeAsync(\"EventStatus\") instead")]
    Task<IReadOnlyList<EventStatusRef>> GetEventStatusesAsync(bool activeOnly = true, CancellationToken cancellationToken = default);

    [Obsolete("Use GetByIdAsync() instead")]
    Task<EventStatusRef?> GetEventStatusByIdAsync(Guid id, CancellationToken cancellationToken = default);

    [Obsolete("Use GetByTypeAndCodeAsync(\"EventStatus\", code) instead")]
    Task<EventStatusRef?> GetEventStatusByCodeAsync(string code, CancellationToken cancellationToken = default);

    [Obsolete("Use GetByTypeAsync(\"UserRole\") instead")]
    Task<IReadOnlyList<UserRoleRef>> GetUserRolesAsync(bool activeOnly = true, CancellationToken cancellationToken = default);

    [Obsolete("Use GetByIdAsync() instead")]
    Task<UserRoleRef?> GetUserRoleByIdAsync(Guid id, CancellationToken cancellationToken = default);

    [Obsolete("Use GetByTypeAndCodeAsync(\"UserRole\", code) instead")]
    Task<UserRoleRef?> GetUserRoleByCodeAsync(string code, CancellationToken cancellationToken = default);
}
