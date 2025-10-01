using LankaConnect.Domain.Common;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Business.Enums;
using LankaConnect.Domain.Enterprise;
using LankaConnect.Domain.Common.Models;
using LankaConnect.Domain.Common.Monitoring;
using LankaConnect.Domain.Common.ValueObjects;
using LankaConnect.Domain.Common.Security;
using LankaConnect.Domain.Common.Recovery;
using LankaConnect.Domain.Common.Database;
using MultiLanguageModels = LankaConnect.Domain.Common.Database.MultiLanguageRoutingModels;

namespace LankaConnect.Application.Common.Interfaces;

public interface IReviewRepository : IRepository<Review>
{
    // Review-specific queries
    Task<IReadOnlyList<Review>> GetByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Review>> GetApprovedByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Review>> GetByReviewerIdAsync(Guid reviewerId, CancellationToken cancellationToken = default);
    Task<Review?> GetByBusinessAndReviewerAsync(Guid businessId, Guid reviewerId, CancellationToken cancellationToken = default);
    
    // Status-based queries
    Task<IReadOnlyList<Review>> GetByStatusAsync(ReviewStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Review>> GetPendingReviewsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Review>> GetReportedReviewsAsync(CancellationToken cancellationToken = default);
    
    // Rating-based queries
    Task<IReadOnlyList<Review>> GetByRatingAsync(int rating, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Review>> GetByRatingRangeAsync(int minRating, int maxRating, CancellationToken cancellationToken = default);
    
    // Search operations
    Task<IReadOnlyList<Review>> SearchByContentAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Review>> SearchByTitleAsync(string searchTerm, CancellationToken cancellationToken = default);
    
    // Pagination and sorting
    Task<IReadOnlyList<Review>> GetApprovedByBusinessIdPaginatedAsync(
        Guid businessId, 
        int skip = 0, 
        int take = 20, 
        CancellationToken cancellationToken = default);
        
    Task<IReadOnlyList<Review>> GetByBusinessIdOrderedByRatingAsync(
        Guid businessId, 
        bool descending = true, 
        CancellationToken cancellationToken = default);
        
    Task<IReadOnlyList<Review>> GetRecentByBusinessIdAsync(
        Guid businessId, 
        int take = 10, 
        CancellationToken cancellationToken = default);
    
    // Statistics
    Task<int> GetReviewCountByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default);
    Task<int> GetApprovedReviewCountByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default);
    Task<decimal?> GetAverageRatingByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default);
    Task<Dictionary<int, int>> GetRatingDistributionByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default);
    
    // Reviewer statistics
    Task<int> GetReviewCountByReviewerIdAsync(Guid reviewerId, CancellationToken cancellationToken = default);
    Task<bool> HasReviewerReviewedBusinessAsync(Guid reviewerId, Guid businessId, CancellationToken cancellationToken = default);
    
    // Moderation operations
    Task<IReadOnlyList<Review>> GetReviewsNeedingModerationAsync(int take = 50, CancellationToken cancellationToken = default);
    Task<int> GetPendingReviewCountAsync(CancellationToken cancellationToken = default);
}