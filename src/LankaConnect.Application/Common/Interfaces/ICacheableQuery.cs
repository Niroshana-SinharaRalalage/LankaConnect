using LankaConnect.Domain.Common;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Enterprise;
using LankaConnect.Domain.Common.Models;
using LankaConnect.Domain.Common.Monitoring;
using LankaConnect.Domain.Common.ValueObjects;
using LankaConnect.Domain.Common.Security;
using LankaConnect.Domain.Common.Recovery;
using LankaConnect.Domain.Common.Database;
using LankaConnect.Domain.Common.Enums;
using MultiLanguageModels = LankaConnect.Domain.Common.Database.MultiLanguageRoutingModels;

namespace LankaConnect.Application.Common.Interfaces;

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