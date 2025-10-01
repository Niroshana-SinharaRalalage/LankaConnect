using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.Monitoring;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Application.Common.Interfaces;

public interface IServiceRepository : IRepository<Service>
{
    // Service-specific queries
    Task<IReadOnlyList<Service>> GetByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Service>> GetActiveByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default);
    Task<Service?> GetByBusinessIdAndNameAsync(Guid businessId, string serviceName, CancellationToken cancellationToken = default);
    
    // Search operations
    Task<IReadOnlyList<Service>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Service>> SearchByDescriptionAsync(string searchTerm, CancellationToken cancellationToken = default);
    
    // Price-based queries
    Task<IReadOnlyList<Service>> GetByPriceRangeAsync(Money minPrice, Money maxPrice, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Service>> GetFreeServicesAsync(CancellationToken cancellationToken = default);
    
    // Business service management
    Task<int> GetServiceCountByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default);
    Task<int> GetActiveServiceCountByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default);
    
    // Bulk operations
    Task<IReadOnlyList<Service>> GetServicesByBusinessIdsAsync(IEnumerable<Guid> businessIds, CancellationToken cancellationToken = default);
    Task DeactivateAllByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default);
}