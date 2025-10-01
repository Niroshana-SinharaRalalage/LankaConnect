using System;
using System.Collections.Generic;

namespace LankaConnect.Application.Common.Models;

/// <summary>
/// Result of cache invalidation operations
/// </summary>
public record CacheInvalidationResult
{
    /// <summary>
    /// Whether invalidation was successful
    /// </summary>
    public bool IsSuccessful { get; init; }
    
    /// <summary>
    /// Operation identifier
    /// </summary>
    public Guid OperationId { get; init; } = Guid.NewGuid();
    
    /// <summary>
    /// Number of cache entries invalidated
    /// </summary>
    public int InvalidatedCount { get; init; }
    
    /// <summary>
    /// Cache keys that were successfully invalidated
    /// </summary>
    public List<string> SuccessfulInvalidations { get; init; } = new();
    
    /// <summary>
    /// Cache keys that failed to invalidate
    /// </summary>
    public List<string> FailedInvalidations { get; init; } = new();
    
    /// <summary>
    /// Time taken for invalidation operation
    /// </summary>
    public TimeSpan Duration { get; init; }
    
    /// <summary>
    /// Strategy used for invalidation
    /// </summary>
    public string StrategyUsed { get; init; } = string.Empty;
    
    /// <summary>
    /// Number of retry attempts made
    /// </summary>
    public int RetryAttempts { get; init; }
    
    /// <summary>
    /// Invalidation timestamp
    /// </summary>
    public DateTime InvalidatedAt { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Error messages for failed invalidations
    /// </summary>
    public List<string> ErrorMessages { get; init; } = new();
    
    /// <summary>
    /// Invalidation metadata
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}