using Microsoft.EntityFrameworkCore;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Business.Enums;
using LankaConnect.Application.Common.Interfaces;
using System.Linq.Expressions;

namespace LankaConnect.Infrastructure.Data.Repositories;

public class BusinessRepository : Repository<Business>, IBusinessRepository
{
    public BusinessRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Business>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(b => b.OwnerId == ownerId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Business>> GetByCategoryAsync(BusinessCategory category, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(b => b.Category == category)
            .Where(b => b.Status == BusinessStatus.Active)
            .OrderByDescending(b => b.Rating)
            .ThenByDescending(b => b.ReviewCount)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Business>> GetByStatusAsync(BusinessStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(b => b.Status == status)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Business>> GetVerifiedBusinessesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(b => b.IsVerified && b.Status == BusinessStatus.Active)
            .OrderByDescending(b => b.Rating)
            .ThenByDescending(b => b.ReviewCount)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Business>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return Array.Empty<Business>();

        var normalizedSearchTerm = searchTerm.Trim().ToLower();
        
        return await _dbSet
            .AsNoTracking()
            .Where(b => EF.Functions.Like(b.Profile.Name.ToLower(), $"%{normalizedSearchTerm}%"))
            .Where(b => b.Status == BusinessStatus.Active)
            .OrderByDescending(b => b.IsVerified)
            .ThenByDescending(b => b.Rating)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Business>> SearchByLocationAsync(string city, string? state = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(city))
            return Array.Empty<Business>();

        var query = _dbSet
            .AsNoTracking()
            .Where(b => EF.Functions.Like(b.Location.Address.City.ToLower(), $"%{city.Trim().ToLower()}%"))
            .Where(b => b.Status == BusinessStatus.Active);

        if (!string.IsNullOrWhiteSpace(state))
        {
            query = query.Where(b => EF.Functions.Like(b.Location.Address.State.ToLower(), $"%{state.Trim().ToLower()}%"));
        }

        return await query
            .OrderByDescending(b => b.IsVerified)
            .ThenByDescending(b => b.Rating)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Business>> GetNearbyBusinessesAsync(decimal latitude, decimal longitude, double radiusKm, CancellationToken cancellationToken = default)
    {
        // Using simplified distance calculation for PostgreSQL
        // For production, consider using PostGIS for accurate geospatial queries
        return await _dbSet
            .AsNoTracking()
            .Where(b => b.Location.Coordinates != null)
            .Where(b => b.Status == BusinessStatus.Active)
            .Where(b => 
                (6371 * 2 * Math.Asin(Math.Sqrt(
                    Math.Pow(Math.Sin((double)(b.Location.Coordinates!.Latitude - latitude) * Math.PI / 180 / 2), 2) +
                    Math.Cos((double)latitude * Math.PI / 180) * 
                    Math.Cos((double)b.Location.Coordinates!.Latitude * Math.PI / 180) *
                    Math.Pow(Math.Sin((double)(b.Location.Coordinates!.Longitude - longitude) * Math.PI / 180 / 2), 2)
                ))) <= radiusKm)
            .OrderByDescending(b => b.IsVerified)
            .ThenByDescending(b => b.Rating)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Business>> GetBusinessesWithFiltersAsync(
        BusinessCategory? category = null, 
        BusinessStatus? status = null, 
        bool? isVerified = null, 
        decimal? minRating = null, 
        string? city = null, 
        string? state = null, 
        int skip = 0, 
        int take = 20, 
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking();

        if (category.HasValue)
            query = query.Where(b => b.Category == category.Value);

        if (status.HasValue)
            query = query.Where(b => b.Status == status.Value);
        else
            query = query.Where(b => b.Status == BusinessStatus.Active); // Default to active businesses

        if (isVerified.HasValue)
            query = query.Where(b => b.IsVerified == isVerified.Value);

        if (minRating.HasValue)
            query = query.Where(b => b.Rating >= minRating.Value);

        if (!string.IsNullOrWhiteSpace(city))
            query = query.Where(b => EF.Functions.Like(b.Location.Address.City.ToLower(), $"%{city.Trim().ToLower()}%"));

        if (!string.IsNullOrWhiteSpace(state))
            query = query.Where(b => EF.Functions.Like(b.Location.Address.State.ToLower(), $"%{state.Trim().ToLower()}%"));

        return await query
            .OrderByDescending(b => b.IsVerified)
            .ThenByDescending(b => b.Rating)
            .ThenByDescending(b => b.ReviewCount)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Business>> GetTopRatedBusinessesAsync(BusinessCategory? category = null, int take = 10, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Where(b => b.Status == BusinessStatus.Active)
            .Where(b => b.Rating.HasValue && b.ReviewCount > 0);

        if (category.HasValue)
            query = query.Where(b => b.Category == category.Value);

        return await query
            .OrderByDescending(b => b.Rating)
            .ThenByDescending(b => b.ReviewCount)
            .ThenByDescending(b => b.IsVerified)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<Business?> GetWithReviewsAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(b => b.Reviews.Where(r => r.Status == ReviewStatus.Approved))
            .FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);
    }

    public async Task<Business?> GetWithServicesAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(b => b.Services.Where(s => s.IsActive))
            .FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);
    }

    public async Task<Business?> GetWithServicesAndReviewsAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(b => b.Services.Where(s => s.IsActive))
            .Include(b => b.Reviews.Where(r => r.Status == ReviewStatus.Approved))
            .FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);
    }

    public async Task<int> GetBusinessCountByCategoryAsync(BusinessCategory category, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.Category == category)
            .Where(b => b.Status == BusinessStatus.Active)
            .CountAsync(cancellationToken);
    }

    public async Task<int> GetBusinessCountByStatusAsync(BusinessStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.Status == status)
            .CountAsync(cancellationToken);
    }

    public async Task<Dictionary<BusinessCategory, int>> GetBusinessCountByAllCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(b => b.Status == BusinessStatus.Active)
            .GroupBy(b => b.Category)
            .ToDictionaryAsync(g => g.Key, g => g.Count(), cancellationToken);
    }

    public async Task<bool> IsOwnerOfBusinessAsync(Guid businessId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(b => b.Id == businessId && b.OwnerId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<Business>> GetBusinessesByOwnerWithStatusAsync(Guid ownerId, BusinessStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(b => b.OwnerId == ownerId && b.Status == status)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}