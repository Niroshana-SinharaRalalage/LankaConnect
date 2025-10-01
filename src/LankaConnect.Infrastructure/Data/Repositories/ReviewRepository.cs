using Microsoft.EntityFrameworkCore;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Business.Enums;
using LankaConnect.Application.Common.Interfaces;

namespace LankaConnect.Infrastructure.Data.Repositories;

public class ReviewRepository : Repository<Review>, IReviewRepository
{
    public ReviewRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Review>> GetByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(r => r.BusinessId == businessId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Review>> GetApprovedByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(r => r.BusinessId == businessId && r.Status == ReviewStatus.Approved)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Review>> GetByReviewerIdAsync(Guid reviewerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(r => r.ReviewerId == reviewerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Review?> GetByBusinessAndReviewerAsync(Guid businessId, Guid reviewerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(r => r.BusinessId == businessId && r.ReviewerId == reviewerId, cancellationToken);
    }

    public async Task<IReadOnlyList<Review>> GetByStatusAsync(ReviewStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(r => r.Status == status)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Review>> GetPendingReviewsAsync(CancellationToken cancellationToken = default)
    {
        return await GetByStatusAsync(ReviewStatus.Pending, cancellationToken);
    }

    public async Task<IReadOnlyList<Review>> GetReportedReviewsAsync(CancellationToken cancellationToken = default)
    {
        return await GetByStatusAsync(ReviewStatus.Reported, cancellationToken);
    }

    public async Task<IReadOnlyList<Review>> GetByRatingAsync(int rating, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(r => r.Rating.Value == rating)
            .Where(r => r.Status == ReviewStatus.Approved)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Review>> GetByRatingRangeAsync(int minRating, int maxRating, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(r => r.Rating.Value >= minRating && r.Rating.Value <= maxRating)
            .Where(r => r.Status == ReviewStatus.Approved)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Review>> SearchByContentAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return Array.Empty<Review>();

        var normalizedSearchTerm = searchTerm.Trim().ToLower();

        return await _dbSet
            .AsNoTracking()
            .Where(r => EF.Functions.Like(r.Content.Content.ToLower(), $"%{normalizedSearchTerm}%"))
            .Where(r => r.Status == ReviewStatus.Approved)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Review>> SearchByTitleAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return Array.Empty<Review>();

        var normalizedSearchTerm = searchTerm.Trim().ToLower();

        return await _dbSet
            .AsNoTracking()
            .Where(r => EF.Functions.Like(r.Content.Title.ToLower(), $"%{normalizedSearchTerm}%"))
            .Where(r => r.Status == ReviewStatus.Approved)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Review>> GetApprovedByBusinessIdPaginatedAsync(Guid businessId, int skip = 0, int take = 20, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(r => r.BusinessId == businessId && r.Status == ReviewStatus.Approved)
            .OrderByDescending(r => r.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Review>> GetByBusinessIdOrderedByRatingAsync(Guid businessId, bool descending = true, CancellationToken cancellationToken = default)
    {
        var query = _dbSet
            .AsNoTracking()
            .Where(r => r.BusinessId == businessId && r.Status == ReviewStatus.Approved);

        return descending
            ? await query.OrderByDescending(r => r.Rating.Value).ThenByDescending(r => r.CreatedAt).ToListAsync(cancellationToken)
            : await query.OrderBy(r => r.Rating.Value).ThenByDescending(r => r.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Review>> GetRecentByBusinessIdAsync(Guid businessId, int take = 10, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(r => r.BusinessId == businessId && r.Status == ReviewStatus.Approved)
            .OrderByDescending(r => r.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetReviewCountByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.BusinessId == businessId)
            .CountAsync(cancellationToken);
    }

    public async Task<int> GetApprovedReviewCountByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.BusinessId == businessId && r.Status == ReviewStatus.Approved)
            .CountAsync(cancellationToken);
    }

    public async Task<decimal?> GetAverageRatingByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        var reviews = await _dbSet
            .AsNoTracking()
            .Where(r => r.BusinessId == businessId && r.Status == ReviewStatus.Approved)
            .Select(r => r.Rating.Value)
            .ToListAsync(cancellationToken);

        return reviews.Any() ? (decimal)reviews.Average() : null;
    }

    public async Task<Dictionary<int, int>> GetRatingDistributionByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(r => r.BusinessId == businessId && r.Status == ReviewStatus.Approved)
            .GroupBy(r => r.Rating.Value)
            .ToDictionaryAsync(g => g.Key, g => g.Count(), cancellationToken);
    }

    public async Task<int> GetReviewCountByReviewerIdAsync(Guid reviewerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.ReviewerId == reviewerId)
            .CountAsync(cancellationToken);
    }

    public async Task<bool> HasReviewerReviewedBusinessAsync(Guid reviewerId, Guid businessId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(r => r.ReviewerId == reviewerId && r.BusinessId == businessId, cancellationToken);
    }

    public async Task<IReadOnlyList<Review>> GetReviewsNeedingModerationAsync(int take = 50, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(r => r.Status == ReviewStatus.Pending || r.Status == ReviewStatus.Reported)
            .OrderByDescending(r => r.Status == ReviewStatus.Reported)
            .ThenBy(r => r.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetPendingReviewCountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.Status == ReviewStatus.Pending)
            .CountAsync(cancellationToken);
    }
}