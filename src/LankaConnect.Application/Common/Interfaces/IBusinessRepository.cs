using LankaConnect.Domain.Common;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Business.Enums;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Enterprise;
using LankaConnect.Domain.Common.Models;
using LankaConnect.Domain.Common.Monitoring;
using LankaConnect.Domain.Common.Security;
using LankaConnect.Domain.Common.Recovery;
using LankaConnect.Domain.Common.Database;
using LankaConnect.Domain.Common.Enums;
using MultiLanguageModels = LankaConnect.Domain.Common.Database.MultiLanguageRoutingModels;

namespace LankaConnect.Application.Common.Interfaces;

public interface IBusinessRepository : IRepository<Business>
{
    // Business-specific queries
    Task<IReadOnlyList<Business>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Business>> GetByCategoryAsync(BusinessCategory category, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Business>> GetByStatusAsync(BusinessStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Business>> GetVerifiedBusinessesAsync(CancellationToken cancellationToken = default);
    
    // Search and filtering
    Task<IReadOnlyList<Business>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Business>> SearchByLocationAsync(string city, string? state = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Business>> GetNearbyBusinessesAsync(decimal latitude, decimal longitude, double radiusKm, CancellationToken cancellationToken = default);
    
    // Advanced filters
    Task<IReadOnlyList<Business>> GetBusinessesWithFiltersAsync(
        BusinessCategory? category = null,
        BusinessStatus? status = null,
        bool? isVerified = null,
        decimal? minRating = null,
        string? city = null,
        string? state = null,
        int skip = 0,
        int take = 20,
        CancellationToken cancellationToken = default);
        
    // Rating and reviews
    Task<IReadOnlyList<Business>> GetTopRatedBusinessesAsync(BusinessCategory? category = null, int take = 10, CancellationToken cancellationToken = default);
    Task<Business?> GetWithReviewsAsync(Guid businessId, CancellationToken cancellationToken = default);
    Task<Business?> GetWithServicesAsync(Guid businessId, CancellationToken cancellationToken = default);
    Task<Business?> GetWithServicesAndReviewsAsync(Guid businessId, CancellationToken cancellationToken = default);
    
    // Statistics
    Task<int> GetBusinessCountByCategoryAsync(BusinessCategory category, CancellationToken cancellationToken = default);
    Task<int> GetBusinessCountByStatusAsync(BusinessStatus status, CancellationToken cancellationToken = default);
    Task<Dictionary<BusinessCategory, int>> GetBusinessCountByAllCategoriesAsync(CancellationToken cancellationToken = default);
    
    // Owner operations
    Task<bool> IsOwnerOfBusinessAsync(Guid businessId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Business>> GetBusinessesByOwnerWithStatusAsync(Guid ownerId, BusinessStatus status, CancellationToken cancellationToken = default);
}