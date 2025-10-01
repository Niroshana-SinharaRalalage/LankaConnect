using Microsoft.EntityFrameworkCore;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Infrastructure.Data.Repositories;

public class ServiceRepository : Repository<Service>, IServiceRepository
{
    public ServiceRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Service>> GetByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(s => s.BusinessId == businessId)
            .OrderByDescending(s => s.IsActive)
            .ThenBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Service>> GetActiveByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(s => s.BusinessId == businessId && s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Service?> GetByBusinessIdAndNameAsync(Guid businessId, string serviceName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(serviceName))
            return null;

        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.BusinessId == businessId && 
                                    s.Name.ToLower() == serviceName.Trim().ToLower(), 
                                    cancellationToken);
    }

    public async Task<IReadOnlyList<Service>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return Array.Empty<Service>();

        var normalizedSearchTerm = searchTerm.Trim().ToLower();

        return await _dbSet
            .AsNoTracking()
            .Where(s => EF.Functions.Like(s.Name.ToLower(), $"%{normalizedSearchTerm}%"))
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Service>> SearchByDescriptionAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return Array.Empty<Service>();

        var normalizedSearchTerm = searchTerm.Trim().ToLower();

        return await _dbSet
            .AsNoTracking()
            .Where(s => EF.Functions.Like(s.Description.ToLower(), $"%{normalizedSearchTerm}%"))
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Service>> GetByPriceRangeAsync(Money minPrice, Money maxPrice, CancellationToken cancellationToken = default)
    {
        if (minPrice.Currency != maxPrice.Currency)
            return Array.Empty<Service>();

        return await _dbSet
            .AsNoTracking()
            .Where(s => s.Price != null && 
                       s.Price.Currency == minPrice.Currency &&
                       s.Price.Amount >= minPrice.Amount && 
                       s.Price.Amount <= maxPrice.Amount)
            .Where(s => s.IsActive)
            .OrderBy(s => s.Price!.Amount)
            .ThenBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Service>> GetFreeServicesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(s => s.Price == null || s.Price.Amount == 0)
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetServiceCountByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.BusinessId == businessId)
            .CountAsync(cancellationToken);
    }

    public async Task<int> GetActiveServiceCountByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(s => s.BusinessId == businessId && s.IsActive)
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Service>> GetServicesByBusinessIdsAsync(IEnumerable<Guid> businessIds, CancellationToken cancellationToken = default)
    {
        var businessIdsList = businessIds.ToList();
        if (!businessIdsList.Any())
            return Array.Empty<Service>();

        return await _dbSet
            .AsNoTracking()
            .Where(s => businessIdsList.Contains(s.BusinessId))
            .Where(s => s.IsActive)
            .OrderBy(s => s.BusinessId)
            .ThenBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task DeactivateAllByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        var services = await _dbSet
            .Where(s => s.BusinessId == businessId && s.IsActive)
            .ToListAsync(cancellationToken);

        foreach (var service in services)
        {
            service.Deactivate();
        }

        await _context.CommitAsync(cancellationToken);
    }
}