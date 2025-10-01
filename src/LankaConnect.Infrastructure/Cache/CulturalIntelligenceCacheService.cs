using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Monitoring;

namespace LankaConnect.Infrastructure.Cache;

/// <summary>
/// Redis-based implementation of cultural intelligence cache service
/// Provides enterprise-grade cache-aside pattern with cultural context awareness
/// </summary>
public class CulturalIntelligenceCacheService : ICulturalIntelligenceCacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<CulturalIntelligenceCacheService> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly Dictionary<CulturalIntelligenceEndpoint, CacheMetrics> _metricsCache = new();
    private readonly SemaphoreSlim _metricsSemaphore = new(1, 1);

    public CulturalIntelligenceCacheService(
        IDistributedCache distributedCache,
        IConnectionMultiplexer redis,
        ILogger<CulturalIntelligenceCacheService> logger)
    {
        _distributedCache = distributedCache;
        _redis = redis;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> getItem, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Cache key cannot be null or empty", nameof(key));
        
        if (getItem == null)
            throw new ArgumentNullException(nameof(getItem));

        try
        {
            // Try to get from cache first
            var cachedValue = await GetAsync<T>(key, cancellationToken);
            if (cachedValue != null)
            {
                _logger.LogTrace("Cache hit for key: {CacheKey}", key);
                return cachedValue;
            }

            _logger.LogTrace("Cache miss for key: {CacheKey}", key);

            // Get the value using the factory method
            var value = await getItem();
            if (value != null)
            {
                var ttl = expiry ?? GetDefaultTtlForKey(key);
                await SetAsync(key, value, ttl, cancellationToken);
            }

            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetOrSetAsync for key: {CacheKey}", key);
            
            // On cache failure, still try to get the value directly
            try
            {
                return await getItem();
            }
            catch (Exception innerEx)
            {
                _logger.LogError(innerEx, "Failed to execute fallback operation for key: {CacheKey}", key);
                throw;
            }
        }
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return default;

        try
        {
            var cached = await _distributedCache.GetStringAsync(key, cancellationToken);
            if (string.IsNullOrEmpty(cached))
                return default;

            return JsonSerializer.Deserialize<T>(cached, _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get cached value for key: {CacheKey}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiry, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key) || value == null)
            return;

        try
        {
            var serialized = JsonSerializer.Serialize(value, _jsonOptions);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry
            };

            await _distributedCache.SetStringAsync(key, serialized, options, cancellationToken);
            _logger.LogTrace("Successfully cached value for key: {CacheKey} with TTL: {TTL}", key, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache value for key: {CacheKey}", key);
            // Don't throw - cache failures shouldn't break the application
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
            return;

        try
        {
            await _distributedCache.RemoveAsync(key, cancellationToken);
            _logger.LogTrace("Successfully removed cached value for key: {CacheKey}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove cached value for key: {CacheKey}", key);
        }
    }

    public async Task<int> RemovePatternAsync(string keyPattern, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyPattern))
            return 0;

        try
        {
            var database = _redis.GetDatabase();
            var server = _redis.GetServer(_redis.GetEndPoints().First());
            
            // Find keys matching pattern
            var keys = server.Keys(database: database.Database, pattern: keyPattern).ToArray();
            
            if (keys.Length == 0)
            {
                _logger.LogTrace("No keys found for pattern: {KeyPattern}", keyPattern);
                return 0;
            }

            // Remove keys in batches
            await database.KeyDeleteAsync(keys);
            
            _logger.LogDebug("Removed {KeyCount} keys matching pattern: {KeyPattern}", keys.Length, keyPattern);
            return keys.Length;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove keys for pattern: {KeyPattern}", keyPattern);
            return 0;
        }
    }

    public async Task<Result> InvalidateCulturalCacheAsync(CulturalCacheContext cacheContext, CancellationToken cancellationToken = default)
    {
        try
        {
            var patterns = GenerateInvalidationPatterns(cacheContext);
            var totalRemoved = 0;

            foreach (var pattern in patterns)
            {
                var removed = await RemovePatternAsync(pattern, cancellationToken);
                totalRemoved += removed;
            }

            _logger.LogInformation(
                "Cultural cache invalidation completed. Removed {TotalKeys} keys for context: {Context}",
                totalRemoved,
                JsonSerializer.Serialize(cacheContext, _jsonOptions));

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to invalidate cultural cache for context: {Context}", 
                JsonSerializer.Serialize(cacheContext, _jsonOptions));
            return Result.Failure($"Cache invalidation failed: {ex.Message}");
        }
    }

    public async Task<CacheMetrics> GetCacheMetricsAsync(CulturalIntelligenceEndpoint endpoint, CancellationToken cancellationToken = default)
    {
        await _metricsSemaphore.WaitAsync(cancellationToken);
        
        try
        {
            if (_metricsCache.TryGetValue(endpoint, out var cachedMetrics))
            {
                // Return cached metrics if they're recent (within last minute)
                if (DateTime.UtcNow - cachedMetrics.LastUpdated < TimeSpan.FromMinutes(1))
                {
                    return cachedMetrics;
                }
            }

            // Calculate fresh metrics
            var metrics = await CalculateFreshMetrics(endpoint, cancellationToken);
            _metricsCache[endpoint] = metrics;
            
            return metrics;
        }
        finally
        {
            _metricsSemaphore.Release();
        }
    }

    public async Task WarmCulturalCacheAsync(CulturalCommunity community, CacheWarmingStrategy strategy, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting cache warming for community: {Community} with strategy: {Strategy}", 
            community, strategy);

        try
        {
            var warmingTasks = strategy switch
            {
                CacheWarmingStrategy.Immediate => await GetImmediateWarmingTasks(community, cancellationToken),
                CacheWarmingStrategy.Background => await GetBackgroundWarmingTasks(community, cancellationToken),
                CacheWarmingStrategy.Scheduled => await GetScheduledWarmingTasks(community, cancellationToken),
                CacheWarmingStrategy.Predictive => await GetPredictiveWarmingTasks(community, cancellationToken),
                _ => new List<Task>()
            };

            if (strategy == CacheWarmingStrategy.Immediate)
            {
                await Task.WhenAll(warmingTasks);
            }
            else
            {
                // For other strategies, start tasks without waiting
                _ = Task.WhenAll(warmingTasks).ContinueWith(t =>
                {
                    if (t.Exception != null)
                    {
                        _logger.LogWarning(t.Exception, "Some cache warming tasks failed for community: {Community}", community);
                    }
                    else
                    {
                        _logger.LogInformation("Cache warming completed for community: {Community}", community);
                    }
                }, TaskScheduler.Default);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache warming failed for community: {Community}", community);
        }
    }

    public async Task<CacheHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        var healthStatus = new CacheHealthStatus();
        
        try
        {
            // Test Redis connectivity
            var database = _redis.GetDatabase();
            var testKey = $"health_check_{Guid.NewGuid()}";
            var testValue = "health_test";
            
            // Perform basic operations to test health
            await database.StringSetAsync(testKey, testValue, TimeSpan.FromSeconds(10));
            var retrievedValue = await database.StringGetAsync(testKey);
            await database.KeyDeleteAsync(testKey);
            
            var isHealthy = retrievedValue == testValue;
            
            healthStatus.IsHealthy = isHealthy;
            healthStatus.Status = isHealthy ? "Healthy" : "Unhealthy";
            healthStatus.Details["redis_connectivity"] = isHealthy;
            healthStatus.Details["test_operation_success"] = retrievedValue == testValue;
            
            // Add more health metrics
            var info = await database.ExecuteAsync("INFO", "memory");
            healthStatus.Details["redis_memory_info"] = info.ToString();
            
        }
        catch (Exception ex)
        {
            healthStatus.IsHealthy = false;
            healthStatus.Status = "Unhealthy";
            healthStatus.Details["error"] = ex.Message;
            _logger.LogWarning(ex, "Cache health check failed");
        }
        
        return healthStatus;
    }

    private TimeSpan GetDefaultTtlForKey(string key)
    {
        var prefix = key.Split(':').FirstOrDefault()?.ToLowerInvariant();
        
        return prefix switch
        {
            "cal_buddhist" or "cal_hindu" => TimeSpan.FromDays(30), // Calendar data is relatively stable
            "cultural_score" => TimeSpan.FromHours(4), // Cultural scoring may change with model updates
            "diaspora_analytics" => TimeSpan.FromHours(1), // Analytics data should be relatively fresh
            "event_recommendations" => TimeSpan.FromMinutes(30), // Recommendations should be fresh
            "cultural_content" => TimeSpan.FromHours(12), // Content doesn't change frequently
            "business_directory" => TimeSpan.FromHours(6), // Business data may change during business hours
            "community_engagement" => TimeSpan.FromMinutes(15), // Engagement data should be very fresh
            _ => TimeSpan.FromHours(1) // Default TTL
        };
    }

    private List<string> GenerateInvalidationPatterns(CulturalCacheContext context)
    {
        var patterns = new List<string>();
        
        if (!string.IsNullOrWhiteSpace(context.DataType))
        {
            patterns.Add($"*:{context.CommunityId}:{context.GeographicRegion}:*:{context.DataType}:*");
        }
        
        if (!string.IsNullOrWhiteSpace(context.Language))
        {
            patterns.Add($"*:{context.CommunityId}:{context.GeographicRegion}:{context.Language}:*:*");
        }
        
        // Add more specific patterns based on context
        patterns.Add($"*:{context.CommunityId}:*:*:*:*");
        
        return patterns.Distinct().ToList();
    }

    private async Task<CacheMetrics> CalculateFreshMetrics(CulturalIntelligenceEndpoint endpoint, CancellationToken cancellationToken)
    {
        // This would typically come from a metrics store or monitoring system
        // For now, return sample metrics
        return await Task.FromResult(new CacheMetrics
        {
            HitRatio = 0.85, // 85% hit ratio
            MissRatio = 0.15,
            AverageResponseTime = TimeSpan.FromMilliseconds(50),
            ResponseTimeImprovement = TimeSpan.FromMilliseconds(200),
            CacheSizeBytes = 1024 * 1024, // 1MB
            TotalRequests = 1000,
            CacheHits = 850,
            CacheMisses = 150,
            EvictionRate = 0.05,
            LastUpdated = DateTime.UtcNow
        });
    }

    private async Task<List<Task>> GetImmediateWarmingTasks(CulturalCommunity community, CancellationToken cancellationToken)
    {
        // Implementation would create tasks to warm commonly accessed cultural data
        return await Task.FromResult(new List<Task>());
    }

    private async Task<List<Task>> GetBackgroundWarmingTasks(CulturalCommunity community, CancellationToken cancellationToken)
    {
        return await Task.FromResult(new List<Task>());
    }

    private async Task<List<Task>> GetScheduledWarmingTasks(CulturalCommunity community, CancellationToken cancellationToken)
    {
        return await Task.FromResult(new List<Task>());
    }

    private async Task<List<Task>> GetPredictiveWarmingTasks(CulturalCommunity community, CancellationToken cancellationToken)
    {
        return await Task.FromResult(new List<Task>());
    }
}