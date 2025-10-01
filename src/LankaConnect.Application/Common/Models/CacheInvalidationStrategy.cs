using System;
using System.Collections.Generic;

namespace LankaConnect.Application.Common.Models;

/// <summary>
/// Strategy for cache invalidation operations
/// </summary>
public record CacheInvalidationStrategy
{
    /// <summary>
    /// Invalidation strategy type
    /// </summary>
    public string StrategyType { get; init; } = string.Empty;
    
    /// <summary>
    /// Cache keys to invalidate
    /// </summary>
    public List<string> CacheKeys { get; init; } = new();
    
    /// <summary>
    /// Cache patterns to match for invalidation
    /// </summary>
    public List<string> CachePatterns { get; init; } = new();
    
    /// <summary>
    /// Invalidation priority (1-10, higher is more urgent)
    /// </summary>
    public int Priority { get; init; } = 5;
    
    /// <summary>
    /// Time to live for new cache entries after invalidation
    /// </summary>
    public TimeSpan? NewTtl { get; init; }
    
    /// <summary>
    /// Whether to cascade invalidation to related caches
    /// </summary>
    public bool CascadeInvalidation { get; init; } = false;
    
    /// <summary>
    /// Tags for grouping invalidation operations
    /// </summary>
    public List<string> Tags { get; init; } = new();
    
    /// <summary>
    /// Delay before executing invalidation
    /// </summary>
    public TimeSpan Delay { get; init; } = TimeSpan.Zero;
    
    /// <summary>
    /// Maximum retry attempts if invalidation fails
    /// </summary>
    public int MaxRetries { get; init; } = 3;
    
    /// <summary>
    /// Strategy metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}