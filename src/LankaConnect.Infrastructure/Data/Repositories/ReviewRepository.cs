using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Business.Enums;
using LankaConnect.Application.Common.Interfaces;
using System.Diagnostics;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data.Repositories;

public class ReviewRepository : Repository<Review>, IReviewRepository
{
    private readonly ILogger<ReviewRepository> _repoLogger;

    public ReviewRepository(
        AppDbContext context,
        ILogger<ReviewRepository> logger) : base(context)
    {
        _repoLogger = logger;
    }

    public async Task<IReadOnlyList<Review>> GetByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByBusinessId"))
        using (LogContext.PushProperty("EntityType", "Review"))
        using (LogContext.PushProperty("BusinessId", businessId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByBusinessIdAsync START: BusinessId={BusinessId}", businessId);

            try
            {
                var reviews = await _dbSet
                    .AsNoTracking()
                    .Where(r => r.BusinessId == businessId)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByBusinessIdAsync COMPLETE: BusinessId={BusinessId}, Count={Count}, Duration={ElapsedMs}ms",
                    businessId,
                    reviews.Count,
                    stopwatch.ElapsedMilliseconds);

                return reviews;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByBusinessIdAsync FAILED: BusinessId={BusinessId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    businessId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<Review>> GetApprovedByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetApprovedByBusinessId"))
        using (LogContext.PushProperty("EntityType", "Review"))
        using (LogContext.PushProperty("BusinessId", businessId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetApprovedByBusinessIdAsync START: BusinessId={BusinessId}", businessId);

            try
            {
                var reviews = await _dbSet
                    .AsNoTracking()
                    .Where(r => r.BusinessId == businessId && r.Status == ReviewStatus.Approved)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetApprovedByBusinessIdAsync COMPLETE: BusinessId={BusinessId}, Count={Count}, Duration={ElapsedMs}ms",
                    businessId,
                    reviews.Count,
                    stopwatch.ElapsedMilliseconds);

                return reviews;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetApprovedByBusinessIdAsync FAILED: BusinessId={BusinessId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    businessId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<Review>> GetByReviewerIdAsync(Guid reviewerId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByReviewerId"))
        using (LogContext.PushProperty("EntityType", "Review"))
        using (LogContext.PushProperty("ReviewerId", reviewerId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByReviewerIdAsync START: ReviewerId={ReviewerId}", reviewerId);

            try
            {
                var reviews = await _dbSet
                    .AsNoTracking()
                    .Where(r => r.ReviewerId == reviewerId)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByReviewerIdAsync COMPLETE: ReviewerId={ReviewerId}, Count={Count}, Duration={ElapsedMs}ms",
                    reviewerId,
                    reviews.Count,
                    stopwatch.ElapsedMilliseconds);

                return reviews;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByReviewerIdAsync FAILED: ReviewerId={ReviewerId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    reviewerId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<Review?> GetByBusinessAndReviewerAsync(Guid businessId, Guid reviewerId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByBusinessAndReviewer"))
        using (LogContext.PushProperty("EntityType", "Review"))
        using (LogContext.PushProperty("BusinessId", businessId))
        using (LogContext.PushProperty("ReviewerId", reviewerId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByBusinessAndReviewerAsync START: BusinessId={BusinessId}, ReviewerId={ReviewerId}",
                businessId, reviewerId);

            try
            {
                var review = await _dbSet
                    .FirstOrDefaultAsync(r => r.BusinessId == businessId && r.ReviewerId == reviewerId, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByBusinessAndReviewerAsync COMPLETE: BusinessId={BusinessId}, ReviewerId={ReviewerId}, Found={Found}, Duration={ElapsedMs}ms",
                    businessId,
                    reviewerId,
                    review != null,
                    stopwatch.ElapsedMilliseconds);

                return review;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByBusinessAndReviewerAsync FAILED: BusinessId={BusinessId}, ReviewerId={ReviewerId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    businessId,
                    reviewerId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<Review>> GetByStatusAsync(ReviewStatus status, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByStatus"))
        using (LogContext.PushProperty("EntityType", "Review"))
        using (LogContext.PushProperty("Status", status))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByStatusAsync START: Status={Status}", status);

            try
            {
                var reviews = await _dbSet
                    .AsNoTracking()
                    .Where(r => r.Status == status)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByStatusAsync COMPLETE: Status={Status}, Count={Count}, Duration={ElapsedMs}ms",
                    status,
                    reviews.Count,
                    stopwatch.ElapsedMilliseconds);

                return reviews;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByStatusAsync FAILED: Status={Status}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    status,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<Review>> GetPendingReviewsAsync(CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetPendingReviews"))
        using (LogContext.PushProperty("EntityType", "Review"))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetPendingReviewsAsync START");

            try
            {
                var reviews = await GetByStatusAsync(ReviewStatus.Pending, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetPendingReviewsAsync COMPLETE: Count={Count}, Duration={ElapsedMs}ms",
                    reviews.Count,
                    stopwatch.ElapsedMilliseconds);

                return reviews;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetPendingReviewsAsync FAILED: Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<Review>> GetReportedReviewsAsync(CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetReportedReviews"))
        using (LogContext.PushProperty("EntityType", "Review"))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetReportedReviewsAsync START");

            try
            {
                var reviews = await GetByStatusAsync(ReviewStatus.Reported, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetReportedReviewsAsync COMPLETE: Count={Count}, Duration={ElapsedMs}ms",
                    reviews.Count,
                    stopwatch.ElapsedMilliseconds);

                return reviews;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetReportedReviewsAsync FAILED: Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<Review>> GetByRatingAsync(int rating, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByRating"))
        using (LogContext.PushProperty("EntityType", "Review"))
        using (LogContext.PushProperty("Rating", rating))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByRatingAsync START: Rating={Rating}", rating);

            try
            {
                var reviews = await _dbSet
                    .AsNoTracking()
                    .Where(r => r.Rating.Value == rating)
                    .Where(r => r.Status == ReviewStatus.Approved)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByRatingAsync COMPLETE: Rating={Rating}, Count={Count}, Duration={ElapsedMs}ms",
                    rating,
                    reviews.Count,
                    stopwatch.ElapsedMilliseconds);

                return reviews;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByRatingAsync FAILED: Rating={Rating}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    rating,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<Review>> GetByRatingRangeAsync(int minRating, int maxRating, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByRatingRange"))
        using (LogContext.PushProperty("EntityType", "Review"))
        using (LogContext.PushProperty("MinRating", minRating))
        using (LogContext.PushProperty("MaxRating", maxRating))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByRatingRangeAsync START: MinRating={MinRating}, MaxRating={MaxRating}",
                minRating, maxRating);

            try
            {
                var reviews = await _dbSet
                    .AsNoTracking()
                    .Where(r => r.Rating.Value >= minRating && r.Rating.Value <= maxRating)
                    .Where(r => r.Status == ReviewStatus.Approved)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByRatingRangeAsync COMPLETE: MinRating={MinRating}, MaxRating={MaxRating}, Count={Count}, Duration={ElapsedMs}ms",
                    minRating,
                    maxRating,
                    reviews.Count,
                    stopwatch.ElapsedMilliseconds);

                return reviews;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByRatingRangeAsync FAILED: MinRating={MinRating}, MaxRating={MaxRating}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    minRating,
                    maxRating,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<Review>> SearchByContentAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "SearchByContent"))
        using (LogContext.PushProperty("EntityType", "Review"))
        using (LogContext.PushProperty("SearchTerm", searchTerm))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("SearchByContentAsync START: SearchTerm={SearchTerm}", searchTerm);

            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    stopwatch.Stop();
                    _repoLogger.LogInformation(
                        "SearchByContentAsync COMPLETE: SearchTerm empty, Count=0, Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                    return Array.Empty<Review>();
                }

                var normalizedSearchTerm = searchTerm.Trim().ToLower();

                var reviews = await _dbSet
                    .AsNoTracking()
                    .Where(r => EF.Functions.Like(r.Content.Content.ToLower(), $"%{normalizedSearchTerm}%"))
                    .Where(r => r.Status == ReviewStatus.Approved)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "SearchByContentAsync COMPLETE: SearchTerm={SearchTerm}, Count={Count}, Duration={ElapsedMs}ms",
                    searchTerm,
                    reviews.Count,
                    stopwatch.ElapsedMilliseconds);

                return reviews;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "SearchByContentAsync FAILED: SearchTerm={SearchTerm}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    searchTerm,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<Review>> SearchByTitleAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "SearchByTitle"))
        using (LogContext.PushProperty("EntityType", "Review"))
        using (LogContext.PushProperty("SearchTerm", searchTerm))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("SearchByTitleAsync START: SearchTerm={SearchTerm}", searchTerm);

            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    stopwatch.Stop();
                    _repoLogger.LogInformation(
                        "SearchByTitleAsync COMPLETE: SearchTerm empty, Count=0, Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                    return Array.Empty<Review>();
                }

                var normalizedSearchTerm = searchTerm.Trim().ToLower();

                var reviews = await _dbSet
                    .AsNoTracking()
                    .Where(r => EF.Functions.Like(r.Content.Title.ToLower(), $"%{normalizedSearchTerm}%"))
                    .Where(r => r.Status == ReviewStatus.Approved)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "SearchByTitleAsync COMPLETE: SearchTerm={SearchTerm}, Count={Count}, Duration={ElapsedMs}ms",
                    searchTerm,
                    reviews.Count,
                    stopwatch.ElapsedMilliseconds);

                return reviews;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "SearchByTitleAsync FAILED: SearchTerm={SearchTerm}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    searchTerm,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<Review>> GetApprovedByBusinessIdPaginatedAsync(Guid businessId, int skip = 0, int take = 20, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetApprovedByBusinessIdPaginated"))
        using (LogContext.PushProperty("EntityType", "Review"))
        using (LogContext.PushProperty("BusinessId", businessId))
        using (LogContext.PushProperty("Skip", skip))
        using (LogContext.PushProperty("Take", take))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetApprovedByBusinessIdPaginatedAsync START: BusinessId={BusinessId}, Skip={Skip}, Take={Take}",
                businessId, skip, take);

            try
            {
                var reviews = await _dbSet
                    .AsNoTracking()
                    .Where(r => r.BusinessId == businessId && r.Status == ReviewStatus.Approved)
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetApprovedByBusinessIdPaginatedAsync COMPLETE: BusinessId={BusinessId}, Skip={Skip}, Take={Take}, Count={Count}, Duration={ElapsedMs}ms",
                    businessId,
                    skip,
                    take,
                    reviews.Count,
                    stopwatch.ElapsedMilliseconds);

                return reviews;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetApprovedByBusinessIdPaginatedAsync FAILED: BusinessId={BusinessId}, Skip={Skip}, Take={Take}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    businessId,
                    skip,
                    take,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<Review>> GetByBusinessIdOrderedByRatingAsync(Guid businessId, bool descending = true, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByBusinessIdOrderedByRating"))
        using (LogContext.PushProperty("EntityType", "Review"))
        using (LogContext.PushProperty("BusinessId", businessId))
        using (LogContext.PushProperty("Descending", descending))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByBusinessIdOrderedByRatingAsync START: BusinessId={BusinessId}, Descending={Descending}",
                businessId, descending);

            try
            {
                var query = _dbSet
                    .AsNoTracking()
                    .Where(r => r.BusinessId == businessId && r.Status == ReviewStatus.Approved);

                var reviews = descending
                    ? await query.OrderByDescending(r => r.Rating.Value).ThenByDescending(r => r.CreatedAt).ToListAsync(cancellationToken)
                    : await query.OrderBy(r => r.Rating.Value).ThenByDescending(r => r.CreatedAt).ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByBusinessIdOrderedByRatingAsync COMPLETE: BusinessId={BusinessId}, Descending={Descending}, Count={Count}, Duration={ElapsedMs}ms",
                    businessId,
                    descending,
                    reviews.Count,
                    stopwatch.ElapsedMilliseconds);

                return reviews;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByBusinessIdOrderedByRatingAsync FAILED: BusinessId={BusinessId}, Descending={Descending}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    businessId,
                    descending,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<Review>> GetRecentByBusinessIdAsync(Guid businessId, int take = 10, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetRecentByBusinessId"))
        using (LogContext.PushProperty("EntityType", "Review"))
        using (LogContext.PushProperty("BusinessId", businessId))
        using (LogContext.PushProperty("Take", take))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetRecentByBusinessIdAsync START: BusinessId={BusinessId}, Take={Take}",
                businessId, take);

            try
            {
                var reviews = await _dbSet
                    .AsNoTracking()
                    .Where(r => r.BusinessId == businessId && r.Status == ReviewStatus.Approved)
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(take)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetRecentByBusinessIdAsync COMPLETE: BusinessId={BusinessId}, Take={Take}, Count={Count}, Duration={ElapsedMs}ms",
                    businessId,
                    take,
                    reviews.Count,
                    stopwatch.ElapsedMilliseconds);

                return reviews;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetRecentByBusinessIdAsync FAILED: BusinessId={BusinessId}, Take={Take}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    businessId,
                    take,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<int> GetReviewCountByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetReviewCountByBusinessId"))
        using (LogContext.PushProperty("EntityType", "Review"))
        using (LogContext.PushProperty("BusinessId", businessId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetReviewCountByBusinessIdAsync START: BusinessId={BusinessId}", businessId);

            try
            {
                var count = await _dbSet
                    .Where(r => r.BusinessId == businessId)
                    .CountAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetReviewCountByBusinessIdAsync COMPLETE: BusinessId={BusinessId}, Count={Count}, Duration={ElapsedMs}ms",
                    businessId,
                    count,
                    stopwatch.ElapsedMilliseconds);

                return count;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetReviewCountByBusinessIdAsync FAILED: BusinessId={BusinessId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    businessId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<int> GetApprovedReviewCountByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetApprovedReviewCountByBusinessId"))
        using (LogContext.PushProperty("EntityType", "Review"))
        using (LogContext.PushProperty("BusinessId", businessId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetApprovedReviewCountByBusinessIdAsync START: BusinessId={BusinessId}", businessId);

            try
            {
                var count = await _dbSet
                    .Where(r => r.BusinessId == businessId && r.Status == ReviewStatus.Approved)
                    .CountAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetApprovedReviewCountByBusinessIdAsync COMPLETE: BusinessId={BusinessId}, Count={Count}, Duration={ElapsedMs}ms",
                    businessId,
                    count,
                    stopwatch.ElapsedMilliseconds);

                return count;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetApprovedReviewCountByBusinessIdAsync FAILED: BusinessId={BusinessId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    businessId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<decimal?> GetAverageRatingByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetAverageRatingByBusinessId"))
        using (LogContext.PushProperty("EntityType", "Review"))
        using (LogContext.PushProperty("BusinessId", businessId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetAverageRatingByBusinessIdAsync START: BusinessId={BusinessId}", businessId);

            try
            {
                var reviews = await _dbSet
                    .AsNoTracking()
                    .Where(r => r.BusinessId == businessId && r.Status == ReviewStatus.Approved)
                    .Select(r => r.Rating.Value)
                    .ToListAsync(cancellationToken);

                var averageRating = reviews.Any() ? (decimal)reviews.Average() : (decimal?)null;

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetAverageRatingByBusinessIdAsync COMPLETE: BusinessId={BusinessId}, ReviewCount={ReviewCount}, AverageRating={AverageRating}, Duration={ElapsedMs}ms",
                    businessId,
                    reviews.Count,
                    averageRating,
                    stopwatch.ElapsedMilliseconds);

                return averageRating;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetAverageRatingByBusinessIdAsync FAILED: BusinessId={BusinessId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    businessId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<Dictionary<int, int>> GetRatingDistributionByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetRatingDistributionByBusinessId"))
        using (LogContext.PushProperty("EntityType", "Review"))
        using (LogContext.PushProperty("BusinessId", businessId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetRatingDistributionByBusinessIdAsync START: BusinessId={BusinessId}", businessId);

            try
            {
                var distribution = await _dbSet
                    .AsNoTracking()
                    .Where(r => r.BusinessId == businessId && r.Status == ReviewStatus.Approved)
                    .GroupBy(r => r.Rating.Value)
                    .ToDictionaryAsync(g => g.Key, g => g.Count(), cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetRatingDistributionByBusinessIdAsync COMPLETE: BusinessId={BusinessId}, UniqueRatings={UniqueRatings}, TotalReviews={TotalReviews}, Duration={ElapsedMs}ms",
                    businessId,
                    distribution.Count,
                    distribution.Values.Sum(),
                    stopwatch.ElapsedMilliseconds);

                return distribution;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetRatingDistributionByBusinessIdAsync FAILED: BusinessId={BusinessId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    businessId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<int> GetReviewCountByReviewerIdAsync(Guid reviewerId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetReviewCountByReviewerId"))
        using (LogContext.PushProperty("EntityType", "Review"))
        using (LogContext.PushProperty("ReviewerId", reviewerId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetReviewCountByReviewerIdAsync START: ReviewerId={ReviewerId}", reviewerId);

            try
            {
                var count = await _dbSet
                    .Where(r => r.ReviewerId == reviewerId)
                    .CountAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetReviewCountByReviewerIdAsync COMPLETE: ReviewerId={ReviewerId}, Count={Count}, Duration={ElapsedMs}ms",
                    reviewerId,
                    count,
                    stopwatch.ElapsedMilliseconds);

                return count;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetReviewCountByReviewerIdAsync FAILED: ReviewerId={ReviewerId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    reviewerId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<bool> HasReviewerReviewedBusinessAsync(Guid reviewerId, Guid businessId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "HasReviewerReviewedBusiness"))
        using (LogContext.PushProperty("EntityType", "Review"))
        using (LogContext.PushProperty("ReviewerId", reviewerId))
        using (LogContext.PushProperty("BusinessId", businessId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("HasReviewerReviewedBusinessAsync START: ReviewerId={ReviewerId}, BusinessId={BusinessId}",
                reviewerId, businessId);

            try
            {
                var hasReviewed = await _dbSet
                    .AnyAsync(r => r.ReviewerId == reviewerId && r.BusinessId == businessId, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "HasReviewerReviewedBusinessAsync COMPLETE: ReviewerId={ReviewerId}, BusinessId={BusinessId}, HasReviewed={HasReviewed}, Duration={ElapsedMs}ms",
                    reviewerId,
                    businessId,
                    hasReviewed,
                    stopwatch.ElapsedMilliseconds);

                return hasReviewed;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "HasReviewerReviewedBusinessAsync FAILED: ReviewerId={ReviewerId}, BusinessId={BusinessId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    reviewerId,
                    businessId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<Review>> GetReviewsNeedingModerationAsync(int take = 50, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetReviewsNeedingModeration"))
        using (LogContext.PushProperty("EntityType", "Review"))
        using (LogContext.PushProperty("Take", take))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetReviewsNeedingModerationAsync START: Take={Take}", take);

            try
            {
                var reviews = await _dbSet
                    .AsNoTracking()
                    .Where(r => r.Status == ReviewStatus.Pending || r.Status == ReviewStatus.Reported)
                    .OrderByDescending(r => r.Status == ReviewStatus.Reported)
                    .ThenBy(r => r.CreatedAt)
                    .Take(take)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetReviewsNeedingModerationAsync COMPLETE: Take={Take}, Count={Count}, Duration={ElapsedMs}ms",
                    take,
                    reviews.Count,
                    stopwatch.ElapsedMilliseconds);

                return reviews;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetReviewsNeedingModerationAsync FAILED: Take={Take}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    take,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<int> GetPendingReviewCountAsync(CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetPendingReviewCount"))
        using (LogContext.PushProperty("EntityType", "Review"))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetPendingReviewCountAsync START");

            try
            {
                var count = await _dbSet
                    .Where(r => r.Status == ReviewStatus.Pending)
                    .CountAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetPendingReviewCountAsync COMPLETE: Count={Count}, Duration={ElapsedMs}ms",
                    count,
                    stopwatch.ElapsedMilliseconds);

                return count;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetPendingReviewCountAsync FAILED: Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }
}