using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.Monitoring;
using LankaConnect.Domain.Common.Database;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Enterprise;
using LankaConnect.Domain.Common.Models;
using LankaConnect.Domain.Common.ValueObjects;
using LankaConnect.Domain.Common.Security;
using LankaConnect.Domain.Common.Recovery;
using MultiLanguageModels = LankaConnect.Domain.Common.Database.MultiLanguageRoutingModels;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Cultural intelligence-aware cache service implementing cache-aside pattern
/// Provides enterprise-grade caching for cultural content and analytics
/// </summary>
public interface ICulturalIntelligenceCacheService
{
    /// <summary>
    /// Gets cached value or executes factory method and caches result
    /// </summary>
    /// <typeparam name="T">Type of cached value</typeparam>
    /// <param name="key">Cultural context-aware cache key</param>
    /// <param name="getItem">Factory method to get value if not cached</param>
    /// <param name="expiry">Optional expiry time (overrides default TTL)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cached or newly retrieved value</returns>
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T>> getItem, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets cached value by key
    /// </summary>
    /// <typeparam name="T">Type of cached value</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cached value or null if not found</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sets value in cache with expiry
    /// </summary>
    /// <typeparam name="T">Type of value to cache</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="value">Value to cache</param>
    /// <param name="expiry">Expiry time</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SetAsync<T>(string key, T value, TimeSpan expiry, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes cached value by key
    /// </summary>
    /// <param name="key">Cache key to remove</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes multiple cached values by key pattern
    /// </summary>
    /// <param name="keyPattern">Key pattern for bulk removal</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of keys removed</returns>
    Task<int> RemovePatternAsync(string keyPattern, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Invalidates cultural intelligence cache based on context
    /// </summary>
    /// <param name="cacheContext">Cultural cache context for invalidation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> InvalidateCulturalCacheAsync(CulturalCacheContext cacheContext, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets cache performance metrics for cultural intelligence endpoints
    /// </summary>
    /// <param name="endpoint">Cultural intelligence endpoint</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cache performance metrics</returns>
    Task<CacheMetrics> GetCacheMetricsAsync(CulturalIntelligenceEndpoint endpoint, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Warms cache with frequently accessed cultural data
    /// </summary>
    /// <param name="community">Cultural community context</param>
    /// <param name="strategy">Cache warming strategy</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task WarmCulturalCacheAsync(CulturalCommunity community, CacheWarmingStrategy strategy, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets current cache health status
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cache health status</returns>
    Task<CacheHealthStatus> GetHealthStatusAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Cultural cache context for targeted cache operations
/// </summary>
public class CulturalCacheContext
{
    public string CommunityId { get; set; } = string.Empty;
    public string GeographicRegionName { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public DateTime? LastModified { get; set; }
}

// CulturalIntelligenceEndpoint enum removed - using LankaConnect.Domain.Common.CulturalIntelligenceEndpoint

/// <summary>
/// Cache warming strategy for cultural data
/// </summary>
public enum CacheWarmingStrategy
{
    Immediate,
    Background,
    Scheduled,
    Predictive
}

/// <summary>
/// Cultural community context for cache partitioning
/// </summary>
public enum CulturalCommunity
{
    Buddhist,
    Hindu,
    Tamil,
    Sinhalese,
    Muslim,
    Christian,
    Diaspora,
    MultiCultural
}

/// <summary>
/// Cache performance metrics
/// </summary>
public class CacheMetrics
{
    public double HitRatio { get; set; }
    public double MissRatio { get; set; }
    public TimeSpan AverageResponseTime { get; set; }
    public TimeSpan ResponseTimeImprovement { get; set; }
    public long CacheSizeBytes { get; set; }
    public int TotalRequests { get; set; }
    public int CacheHits { get; set; }
    public int CacheMisses { get; set; }
    public double EvictionRate { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Cache health status
/// </summary>
public class CacheHealthStatus
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = "Unknown";
    public Dictionary<string, object> Details { get; set; } = new();
    public DateTime CheckTime { get; set; } = DateTime.UtcNow;
}