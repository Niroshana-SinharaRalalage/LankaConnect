using LankaConnect.Application.ReferenceData.DTOs;
using LankaConnect.Domain.ReferenceData.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.ReferenceData.Services;

/// <summary>
/// Service implementation for accessing reference data with IMemoryCache
/// Phase 6A.47: High-performance reference data access with 1-hour caching
/// </summary>
public class ReferenceDataService : IReferenceDataService
{
    private readonly IReferenceDataRepository _repository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ReferenceDataService> _logger;

    // Cache keys
    private const string CACHE_KEY_EVENT_CATEGORIES = "RefData:EventCategories";
    private const string CACHE_KEY_EVENT_STATUSES = "RefData:EventStatuses";
    private const string CACHE_KEY_USER_ROLES = "RefData:UserRoles";

    // Cache configuration
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(1);
    private static readonly MemoryCacheEntryOptions CacheOptions = new MemoryCacheEntryOptions
    {
        AbsoluteExpirationRelativeToNow = CacheExpiration,
        Priority = CacheItemPriority.High // Reference data rarely changes
    };

    public ReferenceDataService(
        IReferenceDataRepository repository,
        IMemoryCache cache,
        ILogger<ReferenceDataService> logger)
    {
        _repository = repository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IReadOnlyList<EventCategoryRefDto>> GetEventCategoriesAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CACHE_KEY_EVENT_CATEGORIES}:{activeOnly}";

        if (_cache.TryGetValue<IReadOnlyList<EventCategoryRefDto>>(cacheKey, out var cached))
        {
            _logger.LogDebug("Cache HIT for EventCategories (activeOnly={ActiveOnly})", activeOnly);
            return cached!;
        }

        _logger.LogDebug("Cache MISS for EventCategories (activeOnly={ActiveOnly})", activeOnly);

        var entities = await _repository.GetEventCategoriesAsync(activeOnly, cancellationToken);

        var dtos = entities.Select(e => new EventCategoryRefDto
        {
            Id = e.Id,
            Code = e.Code,
            Name = e.Name,
            Description = e.Description,
            IconUrl = e.IconUrl,
            DisplayOrder = e.DisplayOrder,
            IsActive = e.IsActive
        }).ToList();

        _cache.Set(cacheKey, dtos, CacheOptions);

        _logger.LogInformation("Loaded {Count} event categories into cache", dtos.Count);

        return dtos;
    }

    public async Task<IReadOnlyList<EventStatusRefDto>> GetEventStatusesAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CACHE_KEY_EVENT_STATUSES}:{activeOnly}";

        if (_cache.TryGetValue<IReadOnlyList<EventStatusRefDto>>(cacheKey, out var cached))
        {
            _logger.LogDebug("Cache HIT for EventStatuses (activeOnly={ActiveOnly})", activeOnly);
            return cached!;
        }

        _logger.LogDebug("Cache MISS for EventStatuses (activeOnly={ActiveOnly})", activeOnly);

        var entities = await _repository.GetEventStatusesAsync(activeOnly, cancellationToken);

        var dtos = entities.Select(e => new EventStatusRefDto
        {
            Id = e.Id,
            Code = e.Code,
            Name = e.Name,
            Description = e.Description,
            DisplayOrder = e.DisplayOrder,
            IsActive = e.IsActive,
            AllowsRegistration = e.AllowsRegistration,
            IsFinalState = e.IsFinalState
        }).ToList();

        _cache.Set(cacheKey, dtos, CacheOptions);

        _logger.LogInformation("Loaded {Count} event statuses into cache", dtos.Count);

        return dtos;
    }

    public async Task<IReadOnlyList<UserRoleRefDto>> GetUserRolesAsync(
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CACHE_KEY_USER_ROLES}:{activeOnly}";

        if (_cache.TryGetValue<IReadOnlyList<UserRoleRefDto>>(cacheKey, out var cached))
        {
            _logger.LogDebug("Cache HIT for UserRoles (activeOnly={ActiveOnly})", activeOnly);
            return cached!;
        }

        _logger.LogDebug("Cache MISS for UserRoles (activeOnly={ActiveOnly})", activeOnly);

        var entities = await _repository.GetUserRolesAsync(activeOnly, cancellationToken);

        var dtos = entities.Select(e => new UserRoleRefDto
        {
            Id = e.Id,
            Code = e.Code,
            Name = e.Name,
            Description = e.Description,
            DisplayOrder = e.DisplayOrder,
            IsActive = e.IsActive,
            CanManageUsers = e.CanManageUsers,
            CanCreateEvents = e.CanCreateEvents,
            CanModerateContent = e.CanModerateContent,
            CanCreateBusinessProfile = e.CanCreateBusinessProfile,
            CanCreatePosts = e.CanCreatePosts,
            RequiresSubscription = e.RequiresSubscription,
            MonthlyPrice = e.MonthlyPrice,
            RequiresApproval = e.RequiresApproval
        }).ToList();

        _cache.Set(cacheKey, dtos, CacheOptions);

        _logger.LogInformation("Loaded {Count} user roles into cache", dtos.Count);

        return dtos;
    }

    public Task InvalidateCacheAsync(string referenceType, CancellationToken cancellationToken = default)
    {
        var cacheKey = referenceType.ToLowerInvariant() switch
        {
            "eventcategories" or "eventcategory" => CACHE_KEY_EVENT_CATEGORIES,
            "eventstatuses" or "eventstatus" => CACHE_KEY_EVENT_STATUSES,
            "userroles" or "userrole" => CACHE_KEY_USER_ROLES,
            _ => throw new ArgumentException($"Unknown reference type: {referenceType}", nameof(referenceType))
        };

        // Remove both active-only and all-items cache entries
        _cache.Remove($"{cacheKey}:True");
        _cache.Remove($"{cacheKey}:False");

        _logger.LogInformation("Invalidated cache for {ReferenceType}", referenceType);

        return Task.CompletedTask;
    }

    public Task InvalidateAllCachesAsync(CancellationToken cancellationToken = default)
    {
        _cache.Remove($"{CACHE_KEY_EVENT_CATEGORIES}:True");
        _cache.Remove($"{CACHE_KEY_EVENT_CATEGORIES}:False");
        _cache.Remove($"{CACHE_KEY_EVENT_STATUSES}:True");
        _cache.Remove($"{CACHE_KEY_EVENT_STATUSES}:False");
        _cache.Remove($"{CACHE_KEY_USER_ROLES}:True");
        _cache.Remove($"{CACHE_KEY_USER_ROLES}:False");

        _logger.LogInformation("Invalidated all reference data caches");

        return Task.CompletedTask;
    }
}
