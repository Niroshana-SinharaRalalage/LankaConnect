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
    #pragma warning disable CS0618 // Type or member is obsolete
    public async Task<IReadOnlyList<EventCategoryRef>> GetEventCategoriesAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = _context.EventCategories.AsQueryable();

        if (activeOnly)
        {
            query = query.Where(c => c.IsActive);
        }

        return await query
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<EventCategoryRef?> GetEventCategoryByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.EventCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<EventCategoryRef?> GetEventCategoryByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        return await _context.EventCategories
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code == code, cancellationToken);
    }

    public async Task<IReadOnlyList<EventStatusRef>> GetEventStatusesAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = _context.EventStatuses.AsQueryable();

        if (activeOnly)
        {
            query = query.Where(s => s.IsActive);
        }

        return await query
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<EventStatusRef?> GetEventStatusByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.EventStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<EventStatusRef?> GetEventStatusByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        return await _context.EventStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Code == code, cancellationToken);
    }

    public async Task<IReadOnlyList<UserRoleRef>> GetUserRolesAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = _context.UserRoles.AsQueryable();

        if (activeOnly)
        {
            query = query.Where(r => r.IsActive);
        }

        return await query
            .OrderBy(r => r.DisplayOrder)
            .ThenBy(r => r.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<UserRoleRef?> GetUserRoleByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.UserRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<UserRoleRef?> GetUserRoleByCodeAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        return await _context.UserRoles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Code == code, cancellationToken);
    }
    #pragma warning restore CS0618 // Type or member is obsolete
}
