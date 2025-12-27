using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.ReferenceData.Entities;
using LankaConnect.Domain.ReferenceData.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LankaConnect.Infrastructure.Data.Repositories.ReferenceData;

/// <summary>
/// Repository implementation for unified reference data
/// Phase 6A.47: Unified Reference Data Architecture
/// </summary>
public class ReferenceDataRepository : IReferenceDataRepository
{
    private readonly IApplicationDbContext _context;

    public ReferenceDataRepository(IApplicationDbContext context)
    {
        _context = context;
    }

    // Unified operations
    public async Task<IReadOnlyList<ReferenceValue>> GetByTypeAsync(
        string enumType,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = _context.ReferenceValues
            .Where(rv => rv.EnumType == enumType);

        if (activeOnly)
        {
            query = query.Where(rv => rv.IsActive);
        }

        return await query
            .OrderBy(rv => rv.DisplayOrder)
            .ThenBy(rv => rv.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ReferenceValue>> GetByTypesAsync(
        IEnumerable<string> enumTypes,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var typeList = enumTypes.ToList();
        var query = _context.ReferenceValues
            .Where(rv => typeList.Contains(rv.EnumType));

        if (activeOnly)
        {
            query = query.Where(rv => rv.IsActive);
        }

        return await query
            .OrderBy(rv => rv.EnumType)
            .ThenBy(rv => rv.DisplayOrder)
            .ThenBy(rv => rv.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<ReferenceValue?> GetByTypeAndCodeAsync(
        string enumType,
        string code,
        CancellationToken cancellationToken = default)
    {
        return await _context.ReferenceValues
            .AsNoTracking()
            .FirstOrDefaultAsync(rv => rv.EnumType == enumType && rv.Code == code, cancellationToken);
    }

    public async Task<ReferenceValue?> GetByTypeAndIntValueAsync(
        string enumType,
        int intValue,
        CancellationToken cancellationToken = default)
    {
        return await _context.ReferenceValues
            .AsNoTracking()
            .FirstOrDefaultAsync(rv => rv.EnumType == enumType && rv.IntValue == intValue, cancellationToken);
    }

    public async Task<ReferenceValue?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.ReferenceValues
            .AsNoTracking()
            .FirstOrDefaultAsync(rv => rv.Id == id, cancellationToken);
    }

    // DEPRECATED: Legacy methods for backward compatibility
    // NOTE: These methods are no longer implemented because the old tables (event_categories, event_statuses, user_roles) were dropped
    // The service layer now uses GetByTypeAsync("EventCategory"/"EventStatus"/"UserRole") and maps to legacy DTOs
    #pragma warning disable CS0618 // Type or member is obsolete
    public Task<IReadOnlyList<EventCategoryRef>> GetEventCategoriesAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Legacy repository methods are no longer supported. Old tables were dropped in Phase 6A.47. Use service layer instead.");
    }

    public Task<EventCategoryRef?> GetEventCategoryByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Legacy repository methods are no longer supported. Old tables were dropped in Phase 6A.47. Use service layer instead.");
    }

    public Task<EventCategoryRef?> GetEventCategoryByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Legacy repository methods are no longer supported. Old tables were dropped in Phase 6A.47. Use service layer instead.");
    }

    public Task<IReadOnlyList<EventStatusRef>> GetEventStatusesAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Legacy repository methods are no longer supported. Old tables were dropped in Phase 6A.47. Use service layer instead.");
    }

    public Task<EventStatusRef?> GetEventStatusByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Legacy repository methods are no longer supported. Old tables were dropped in Phase 6A.47. Use service layer instead.");
    }

    public Task<EventStatusRef?> GetEventStatusByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Legacy repository methods are no longer supported. Old tables were dropped in Phase 6A.47. Use service layer instead.");
    }

    public Task<IReadOnlyList<UserRoleRef>> GetUserRolesAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Legacy repository methods are no longer supported. Old tables were dropped in Phase 6A.47. Use service layer instead.");
    }

    public Task<UserRoleRef?> GetUserRoleByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Legacy repository methods are no longer supported. Old tables were dropped in Phase 6A.47. Use service layer instead.");
    }

    public Task<UserRoleRef?> GetUserRoleByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Legacy repository methods are no longer supported. Old tables were dropped in Phase 6A.47. Use service layer instead.");
    }
    #pragma warning restore CS0618 // Type or member is obsolete
}
