using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Business.Enums;
using LankaConnect.Application.Common.Interfaces;
using System.Linq.Expressions;
using System.Diagnostics;
using Serilog.Context;

namespace LankaConnect.Infrastructure.Data.Repositories;

public class BusinessRepository : Repository<Business>, IBusinessRepository
{
    private readonly ILogger<BusinessRepository> _repoLogger;

    public BusinessRepository(
        AppDbContext context,
        ILogger<BusinessRepository> logger) : base(context)
    {
        _repoLogger = logger;
    }

    public async Task<IReadOnlyList<Business>> GetByOwnerIdAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByOwnerId"))
        using (LogContext.PushProperty("EntityType", "Business"))
        using (LogContext.PushProperty("OwnerId", ownerId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByOwnerIdAsync START: OwnerId={OwnerId}", ownerId);

            try
            {
                var businesses = await _dbSet
                    .AsNoTracking()
                    .Where(b => b.OwnerId == ownerId)
                    .OrderByDescending(b => b.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByOwnerIdAsync COMPLETE: OwnerId={OwnerId}, Count={Count}, Duration={ElapsedMs}ms",
                    ownerId,
                    businesses.Count,
                    stopwatch.ElapsedMilliseconds);

                return businesses;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByOwnerIdAsync FAILED: OwnerId={OwnerId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    ownerId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<Business>> GetByCategoryAsync(BusinessCategory category, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByCategory"))
        using (LogContext.PushProperty("EntityType", "Business"))
        using (LogContext.PushProperty("Category", category))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByCategoryAsync START: Category={Category}", category);

            try
            {
                var businesses = await _dbSet
                    .AsNoTracking()
                    .Where(b => b.Category == category)
                    .Where(b => b.Status == BusinessStatus.Active)
                    .OrderByDescending(b => b.Rating)
                    .ThenByDescending(b => b.ReviewCount)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByCategoryAsync COMPLETE: Category={Category}, Count={Count}, Duration={ElapsedMs}ms",
                    category,
                    businesses.Count,
                    stopwatch.ElapsedMilliseconds);

                return businesses;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetByCategoryAsync FAILED: Category={Category}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    category,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<Business>> GetByStatusAsync(BusinessStatus status, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetByStatus"))
        using (LogContext.PushProperty("EntityType", "Business"))
        using (LogContext.PushProperty("Status", status))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetByStatusAsync START: Status={Status}", status);

            try
            {
                var businesses = await _dbSet
                    .AsNoTracking()
                    .Where(b => b.Status == status)
                    .OrderByDescending(b => b.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetByStatusAsync COMPLETE: Status={Status}, Count={Count}, Duration={ElapsedMs}ms",
                    status,
                    businesses.Count,
                    stopwatch.ElapsedMilliseconds);

                return businesses;
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

    public async Task<IReadOnlyList<Business>> GetVerifiedBusinessesAsync(CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetVerifiedBusinesses"))
        using (LogContext.PushProperty("EntityType", "Business"))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetVerifiedBusinessesAsync START");

            try
            {
                var businesses = await _dbSet
                    .AsNoTracking()
                    .Where(b => b.IsVerified && b.Status == BusinessStatus.Active)
                    .OrderByDescending(b => b.Rating)
                    .ThenByDescending(b => b.ReviewCount)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetVerifiedBusinessesAsync COMPLETE: Count={Count}, Duration={ElapsedMs}ms",
                    businesses.Count,
                    stopwatch.ElapsedMilliseconds);

                return businesses;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetVerifiedBusinessesAsync FAILED: Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<Business>> SearchByNameAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "SearchByName"))
        using (LogContext.PushProperty("EntityType", "Business"))
        using (LogContext.PushProperty("SearchTerm", searchTerm))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("SearchByNameAsync START: SearchTerm={SearchTerm}", searchTerm);

            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    stopwatch.Stop();
                    _repoLogger.LogInformation(
                        "SearchByNameAsync COMPLETE: SearchTerm=Empty, Count=0, Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                    return Array.Empty<Business>();
                }

                var normalizedSearchTerm = searchTerm.Trim().ToLower();

                var businesses = await _dbSet
                    .AsNoTracking()
                    .Where(b => EF.Functions.Like(b.Profile.Name.ToLower(), $"%{normalizedSearchTerm}%"))
                    .Where(b => b.Status == BusinessStatus.Active)
                    .OrderByDescending(b => b.IsVerified)
                    .ThenByDescending(b => b.Rating)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "SearchByNameAsync COMPLETE: SearchTerm={SearchTerm}, Count={Count}, Duration={ElapsedMs}ms",
                    searchTerm,
                    businesses.Count,
                    stopwatch.ElapsedMilliseconds);

                return businesses;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "SearchByNameAsync FAILED: SearchTerm={SearchTerm}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    searchTerm,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<Business>> SearchByLocationAsync(string city, string? state = null, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "SearchByLocation"))
        using (LogContext.PushProperty("EntityType", "Business"))
        using (LogContext.PushProperty("City", city))
        using (LogContext.PushProperty("State", state))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("SearchByLocationAsync START: City={City}, State={State}", city, state);

            try
            {
                if (string.IsNullOrWhiteSpace(city))
                {
                    stopwatch.Stop();
                    _repoLogger.LogInformation(
                        "SearchByLocationAsync COMPLETE: City=Empty, Count=0, Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                    return Array.Empty<Business>();
                }

                var query = _dbSet
                    .AsNoTracking()
                    .Where(b => EF.Functions.Like(b.Location.Address.City.ToLower(), $"%{city.Trim().ToLower()}%"))
                    .Where(b => b.Status == BusinessStatus.Active);

                if (!string.IsNullOrWhiteSpace(state))
                {
                    query = query.Where(b => EF.Functions.Like(b.Location.Address.State.ToLower(), $"%{state.Trim().ToLower()}%"));
                }

                var businesses = await query
                    .OrderByDescending(b => b.IsVerified)
                    .ThenByDescending(b => b.Rating)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "SearchByLocationAsync COMPLETE: City={City}, State={State}, Count={Count}, Duration={ElapsedMs}ms",
                    city,
                    state,
                    businesses.Count,
                    stopwatch.ElapsedMilliseconds);

                return businesses;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "SearchByLocationAsync FAILED: City={City}, State={State}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    city,
                    state,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<Business>> GetNearbyBusinessesAsync(decimal latitude, decimal longitude, double radiusKm, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetNearbyBusinesses"))
        using (LogContext.PushProperty("EntityType", "Business"))
        using (LogContext.PushProperty("Latitude", latitude))
        using (LogContext.PushProperty("Longitude", longitude))
        using (LogContext.PushProperty("RadiusKm", radiusKm))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetNearbyBusinessesAsync START: Latitude={Latitude}, Longitude={Longitude}, RadiusKm={RadiusKm}",
                latitude, longitude, radiusKm);

            try
            {
                // Using simplified distance calculation for PostgreSQL
                // For production, consider using PostGIS for accurate geospatial queries
                var businesses = await _dbSet
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

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetNearbyBusinessesAsync COMPLETE: Latitude={Latitude}, Longitude={Longitude}, RadiusKm={RadiusKm}, Count={Count}, Duration={ElapsedMs}ms",
                    latitude,
                    longitude,
                    radiusKm,
                    businesses.Count,
                    stopwatch.ElapsedMilliseconds);

                return businesses;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetNearbyBusinessesAsync FAILED: Latitude={Latitude}, Longitude={Longitude}, RadiusKm={RadiusKm}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    latitude,
                    longitude,
                    radiusKm,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
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
        using (LogContext.PushProperty("Operation", "GetBusinessesWithFilters"))
        using (LogContext.PushProperty("EntityType", "Business"))
        using (LogContext.PushProperty("Category", category))
        using (LogContext.PushProperty("Status", status))
        using (LogContext.PushProperty("IsVerified", isVerified))
        using (LogContext.PushProperty("MinRating", minRating))
        using (LogContext.PushProperty("City", city))
        using (LogContext.PushProperty("State", state))
        using (LogContext.PushProperty("Skip", skip))
        using (LogContext.PushProperty("Take", take))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug(
                "GetBusinessesWithFiltersAsync START: Category={Category}, Status={Status}, IsVerified={IsVerified}, MinRating={MinRating}, City={City}, State={State}, Skip={Skip}, Take={Take}",
                category, status, isVerified, minRating, city, state, skip, take);

            try
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

                var businesses = await query
                    .OrderByDescending(b => b.IsVerified)
                    .ThenByDescending(b => b.Rating)
                    .ThenByDescending(b => b.ReviewCount)
                    .Skip(skip)
                    .Take(take)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetBusinessesWithFiltersAsync COMPLETE: Category={Category}, Status={Status}, IsVerified={IsVerified}, MinRating={MinRating}, City={City}, State={State}, Skip={Skip}, Take={Take}, Count={Count}, Duration={ElapsedMs}ms",
                    category, status, isVerified, minRating, city, state, skip, take,
                    businesses.Count,
                    stopwatch.ElapsedMilliseconds);

                return businesses;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetBusinessesWithFiltersAsync FAILED: Category={Category}, Status={Status}, IsVerified={IsVerified}, MinRating={MinRating}, City={City}, State={State}, Skip={Skip}, Take={Take}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    category, status, isVerified, minRating, city, state, skip, take,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<Business>> GetTopRatedBusinessesAsync(BusinessCategory? category = null, int take = 10, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetTopRatedBusinesses"))
        using (LogContext.PushProperty("EntityType", "Business"))
        using (LogContext.PushProperty("Category", category))
        using (LogContext.PushProperty("Take", take))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetTopRatedBusinessesAsync START: Category={Category}, Take={Take}", category, take);

            try
            {
                var query = _dbSet
                    .AsNoTracking()
                    .Where(b => b.Status == BusinessStatus.Active)
                    .Where(b => b.Rating.HasValue && b.ReviewCount > 0);

                if (category.HasValue)
                    query = query.Where(b => b.Category == category.Value);

                var businesses = await query
                    .OrderByDescending(b => b.Rating)
                    .ThenByDescending(b => b.ReviewCount)
                    .ThenByDescending(b => b.IsVerified)
                    .Take(take)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetTopRatedBusinessesAsync COMPLETE: Category={Category}, Take={Take}, Count={Count}, Duration={ElapsedMs}ms",
                    category,
                    take,
                    businesses.Count,
                    stopwatch.ElapsedMilliseconds);

                return businesses;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetTopRatedBusinessesAsync FAILED: Category={Category}, Take={Take}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    category,
                    take,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<Business?> GetWithReviewsAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetWithReviews"))
        using (LogContext.PushProperty("EntityType", "Business"))
        using (LogContext.PushProperty("BusinessId", businessId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetWithReviewsAsync START: BusinessId={BusinessId}", businessId);

            try
            {
                var business = await _dbSet
                    .Include(b => b.Reviews.Where(r => r.Status == ReviewStatus.Approved))
                    .FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetWithReviewsAsync COMPLETE: BusinessId={BusinessId}, Found={Found}, ReviewCount={ReviewCount}, Duration={ElapsedMs}ms",
                    businessId,
                    business != null,
                    business?.Reviews?.Count ?? 0,
                    stopwatch.ElapsedMilliseconds);

                return business;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetWithReviewsAsync FAILED: BusinessId={BusinessId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    businessId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<Business?> GetWithServicesAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetWithServices"))
        using (LogContext.PushProperty("EntityType", "Business"))
        using (LogContext.PushProperty("BusinessId", businessId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetWithServicesAsync START: BusinessId={BusinessId}", businessId);

            try
            {
                var business = await _dbSet
                    .Include(b => b.Services.Where(s => s.IsActive))
                    .FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetWithServicesAsync COMPLETE: BusinessId={BusinessId}, Found={Found}, ServiceCount={ServiceCount}, Duration={ElapsedMs}ms",
                    businessId,
                    business != null,
                    business?.Services?.Count ?? 0,
                    stopwatch.ElapsedMilliseconds);

                return business;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetWithServicesAsync FAILED: BusinessId={BusinessId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    businessId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<Business?> GetWithServicesAndReviewsAsync(Guid businessId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetWithServicesAndReviews"))
        using (LogContext.PushProperty("EntityType", "Business"))
        using (LogContext.PushProperty("BusinessId", businessId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetWithServicesAndReviewsAsync START: BusinessId={BusinessId}", businessId);

            try
            {
                var business = await _dbSet
                    .Include(b => b.Services.Where(s => s.IsActive))
                    .Include(b => b.Reviews.Where(r => r.Status == ReviewStatus.Approved))
                    .FirstOrDefaultAsync(b => b.Id == businessId, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetWithServicesAndReviewsAsync COMPLETE: BusinessId={BusinessId}, Found={Found}, ServiceCount={ServiceCount}, ReviewCount={ReviewCount}, Duration={ElapsedMs}ms",
                    businessId,
                    business != null,
                    business?.Services?.Count ?? 0,
                    business?.Reviews?.Count ?? 0,
                    stopwatch.ElapsedMilliseconds);

                return business;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetWithServicesAndReviewsAsync FAILED: BusinessId={BusinessId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    businessId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<int> GetBusinessCountByCategoryAsync(BusinessCategory category, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetBusinessCountByCategory"))
        using (LogContext.PushProperty("EntityType", "Business"))
        using (LogContext.PushProperty("Category", category))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetBusinessCountByCategoryAsync START: Category={Category}", category);

            try
            {
                var count = await _dbSet
                    .Where(b => b.Category == category)
                    .Where(b => b.Status == BusinessStatus.Active)
                    .CountAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetBusinessCountByCategoryAsync COMPLETE: Category={Category}, Count={Count}, Duration={ElapsedMs}ms",
                    category,
                    count,
                    stopwatch.ElapsedMilliseconds);

                return count;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetBusinessCountByCategoryAsync FAILED: Category={Category}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    category,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<int> GetBusinessCountByStatusAsync(BusinessStatus status, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetBusinessCountByStatus"))
        using (LogContext.PushProperty("EntityType", "Business"))
        using (LogContext.PushProperty("Status", status))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetBusinessCountByStatusAsync START: Status={Status}", status);

            try
            {
                var count = await _dbSet
                    .Where(b => b.Status == status)
                    .CountAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetBusinessCountByStatusAsync COMPLETE: Status={Status}, Count={Count}, Duration={ElapsedMs}ms",
                    status,
                    count,
                    stopwatch.ElapsedMilliseconds);

                return count;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetBusinessCountByStatusAsync FAILED: Status={Status}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    status,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<Dictionary<BusinessCategory, int>> GetBusinessCountByAllCategoriesAsync(CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetBusinessCountByAllCategories"))
        using (LogContext.PushProperty("EntityType", "Business"))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetBusinessCountByAllCategoriesAsync START");

            try
            {
                var counts = await _dbSet
                    .Where(b => b.Status == BusinessStatus.Active)
                    .GroupBy(b => b.Category)
                    .ToDictionaryAsync(g => g.Key, g => g.Count(), cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetBusinessCountByAllCategoriesAsync COMPLETE: CategoryCount={CategoryCount}, Duration={ElapsedMs}ms",
                    counts.Count,
                    stopwatch.ElapsedMilliseconds);

                return counts;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetBusinessCountByAllCategoriesAsync FAILED: Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<bool> IsOwnerOfBusinessAsync(Guid businessId, Guid userId, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "IsOwnerOfBusiness"))
        using (LogContext.PushProperty("EntityType", "Business"))
        using (LogContext.PushProperty("BusinessId", businessId))
        using (LogContext.PushProperty("UserId", userId))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("IsOwnerOfBusinessAsync START: BusinessId={BusinessId}, UserId={UserId}", businessId, userId);

            try
            {
                var isOwner = await _dbSet
                    .AnyAsync(b => b.Id == businessId && b.OwnerId == userId, cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "IsOwnerOfBusinessAsync COMPLETE: BusinessId={BusinessId}, UserId={UserId}, IsOwner={IsOwner}, Duration={ElapsedMs}ms",
                    businessId,
                    userId,
                    isOwner,
                    stopwatch.ElapsedMilliseconds);

                return isOwner;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "IsOwnerOfBusinessAsync FAILED: BusinessId={BusinessId}, UserId={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    businessId,
                    userId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }

    public async Task<IReadOnlyList<Business>> GetBusinessesByOwnerWithStatusAsync(Guid ownerId, BusinessStatus status, CancellationToken cancellationToken = default)
    {
        using (LogContext.PushProperty("Operation", "GetBusinessesByOwnerWithStatus"))
        using (LogContext.PushProperty("EntityType", "Business"))
        using (LogContext.PushProperty("OwnerId", ownerId))
        using (LogContext.PushProperty("Status", status))
        {
            var stopwatch = Stopwatch.StartNew();

            _repoLogger.LogDebug("GetBusinessesByOwnerWithStatusAsync START: OwnerId={OwnerId}, Status={Status}", ownerId, status);

            try
            {
                var businesses = await _dbSet
                    .AsNoTracking()
                    .Where(b => b.OwnerId == ownerId && b.Status == status)
                    .OrderByDescending(b => b.CreatedAt)
                    .ToListAsync(cancellationToken);

                stopwatch.Stop();

                _repoLogger.LogInformation(
                    "GetBusinessesByOwnerWithStatusAsync COMPLETE: OwnerId={OwnerId}, Status={Status}, Count={Count}, Duration={ElapsedMs}ms",
                    ownerId,
                    status,
                    businesses.Count,
                    stopwatch.ElapsedMilliseconds);

                return businesses;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _repoLogger.LogError(ex,
                    "GetBusinessesByOwnerWithStatusAsync FAILED: OwnerId={OwnerId}, Status={Status}, Duration={ElapsedMs}ms, Error={ErrorMessage}, SqlState={SqlState}",
                    ownerId,
                    status,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message,
                    (ex as Npgsql.NpgsqlException)?.SqlState ?? "N/A");

                throw;
            }
        }
    }
}