using LankaConnect.Domain.ReferenceData.Entities;

namespace LankaConnect.Domain.ReferenceData.Interfaces;

/// <summary>
/// Repository interface for reference data entities
/// Phase 6A.47: Centralized reference data access
/// </summary>
public interface IReferenceDataRepository
{
    // EventCategory operations
    Task<IReadOnlyList<EventCategoryRef>> GetEventCategoriesAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
    Task<EventCategoryRef?> GetEventCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EventCategoryRef?> GetEventCategoryByCodeAsync(string code, CancellationToken cancellationToken = default);

    // EventStatus operations
    Task<IReadOnlyList<EventStatusRef>> GetEventStatusesAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
    Task<EventStatusRef?> GetEventStatusByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EventStatusRef?> GetEventStatusByCodeAsync(string code, CancellationToken cancellationToken = default);

    // UserRole operations
    Task<IReadOnlyList<UserRoleRef>> GetUserRolesAsync(bool activeOnly = true, CancellationToken cancellationToken = default);
    Task<UserRoleRef?> GetUserRoleByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserRoleRef?> GetUserRoleByCodeAsync(string code, CancellationToken cancellationToken = default);
}
