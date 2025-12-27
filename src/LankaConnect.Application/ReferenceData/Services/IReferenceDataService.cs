using LankaConnect.Application.ReferenceData.DTOs;

namespace LankaConnect.Application.ReferenceData.Services;

/// <summary>
/// Service interface for accessing unified reference data with caching
/// Phase 6A.47: Unified Reference Data Architecture
/// </summary>
public interface IReferenceDataService
{
    /// <summary>
    /// Get reference values by enum type(s) - unified approach (cached for 1 hour)
    /// Supports multiple enum types in a single call
    /// </summary>
    Task<IReadOnlyList<ReferenceValueDto>> GetByTypesAsync(
        IEnumerable<string> enumTypes,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all event categories (cached for 1 hour)
    /// </summary>
    Task<IReadOnlyList<EventCategoryRefDto>> GetEventCategoriesAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all event statuses (cached for 1 hour)
    /// </summary>
    Task<IReadOnlyList<EventStatusRefDto>> GetEventStatusesAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all user roles (cached for 1 hour)
    /// </summary>
    Task<IReadOnlyList<UserRoleRefDto>> GetUserRolesAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidate cache for specific reference type
    /// </summary>
    Task InvalidateCacheAsync(string referenceType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidate all reference data caches
    /// </summary>
    Task InvalidateAllCachesAsync(CancellationToken cancellationToken = default);
}
