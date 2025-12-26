using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.ReferenceData.Entities;
using LankaConnect.Domain.ReferenceData.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LankaConnect.Infrastructure.Data.Repositories.ReferenceData;

/// <summary>
/// Repository implementation for reference data entities
/// Phase 6A.47: Database-driven reference data access
/// </summary>
public class ReferenceDataRepository : IReferenceDataRepository
{
    private readonly IApplicationDbContext _context;

    public ReferenceDataRepository(IApplicationDbContext context)
    {
        _context = context;
    }

    // EventCategory operations
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

    // EventStatus operations
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

    // UserRole operations
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
}
