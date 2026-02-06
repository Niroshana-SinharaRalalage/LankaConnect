using LankaConnect.Application.ReferenceData.DTOs;
using LankaConnect.Domain.ReferenceData.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.ReferenceData.Services;

/// <summary>
/// Service implementation for accessing unified reference data with IMemoryCache
/// Phase 6A.47: Unified Reference Data Architecture with 1-hour caching
/// </summary>
public class ReferenceDataService : IReferenceDataService
{
    private readonly IReferenceDataRepository _repository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ReferenceDataService> _logger;

    // Cache keys
    private const string CACHE_KEY_PREFIX = "RefData";
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

    // Unified method for all enum types
    public async Task<IReadOnlyList<ReferenceValueDto>> GetByTypesAsync(
        IEnumerable<string> enumTypes,
        bool activeOnly = true,
        CancellationToken cancellationToken = default)
    {
        var typeList = enumTypes.ToList();
        var cacheKey = $"{CACHE_KEY_PREFIX}:Unified:{string.Join(",", typeList.OrderBy(t => t))}:{activeOnly}";

        if (_cache.TryGetValue<IReadOnlyList<ReferenceValueDto>>(cacheKey, out var cached))
        {
            _logger.LogInformation("Cache HIT for unified reference data (types={Types}, activeOnly={ActiveOnly})",
                string.Join(",", typeList), activeOnly);
            return cached!;
        }

        _logger.LogInformation("Cache MISS for unified reference data (types={Types}, activeOnly={ActiveOnly})",
            string.Join(",", typeList), activeOnly);

        var entities = await _repository.GetByTypesAsync(typeList, activeOnly, cancellationToken);

        var dtos = entities.Select(e => new ReferenceValueDto
        {
            Id = e.Id,
            EnumType = e.EnumType,
            Code = e.Code,
            IntValue = e.IntValue,
            Name = e.Name,
            Description = e.Description,
            DisplayOrder = e.DisplayOrder,
            IsActive = e.IsActive,
            Metadata = e.Metadata
        }).ToList();

        _cache.Set(cacheKey, dtos, CacheOptions);

        _logger.LogInformation("Loaded {Count} reference values into cache (types={Types})",
            dtos.Count, string.Join(",", typeList));

        return dtos;
    }


    public Task InvalidateCacheAsync(string referenceType, CancellationToken cancellationToken = default)
    {
        // Invalidate all cache entries containing this reference type
        // Since we use unified cache keys like "RefData:Unified:EventCategory:True"
        // We need to remove all combinations
        var normalizedType = NormalizeReferenceType(referenceType);

        // Remove unified cache entries (single type)
        _cache.Remove($"{CACHE_KEY_PREFIX}:Unified:{normalizedType}:True");
        _cache.Remove($"{CACHE_KEY_PREFIX}:Unified:{normalizedType}:False");

        _logger.LogInformation("Invalidated cache for {ReferenceType}", referenceType);

        return Task.CompletedTask;
    }

    private static string NormalizeReferenceType(string referenceType)
    {
        return referenceType.ToLowerInvariant() switch
        {
            "eventcategories" or "eventcategory" => "EventCategory",
            "eventstatuses" or "eventstatus" => "EventStatus",
            "userroles" or "userrole" => "UserRole",
            _ => referenceType
        };
    }

    public Task InvalidateAllCachesAsync(CancellationToken cancellationToken = default)
    {
        // Invalidate unified cache entries for all common types
        _cache.Remove($"{CACHE_KEY_PREFIX}:Unified:EventCategory:True");
        _cache.Remove($"{CACHE_KEY_PREFIX}:Unified:EventCategory:False");
        _cache.Remove($"{CACHE_KEY_PREFIX}:Unified:EventStatus:True");
        _cache.Remove($"{CACHE_KEY_PREFIX}:Unified:EventStatus:False");
        _cache.Remove($"{CACHE_KEY_PREFIX}:Unified:UserRole:True");
        _cache.Remove($"{CACHE_KEY_PREFIX}:Unified:UserRole:False");

        _logger.LogInformation("Invalidated all reference data caches");

        return Task.CompletedTask;
    }
}
