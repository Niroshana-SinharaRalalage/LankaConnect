using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.BuildingBlocks.Domain.Models;
using LankaConnect.BuildingBlocks.Domain.ValueObjects;
using LankaConnect.BuildingBlocks.Domain.Enums;
namespace LankaConnect.BuildingBlocks.Application.Common.Interfaces;

/// <summary>
/// Marker interface for queries that should be cached using cache-aside pattern
/// </summary>
public interface ICacheableQuery
{
    /// <summary>
    /// Generates cultural context-aware cache key for the query
    /// </summary>
    string GetCacheKey();
    
    /// <summary>
    /// Gets the Time-To-Live (TTL) for the cached result based on cultural data sensitivity
    /// </summary>
    TimeSpan GetCacheTtl();
    
    /// <summary>
    /// Determines if the query result should be cached based on cultural context
    /// </summary>
    bool ShouldCache();
}