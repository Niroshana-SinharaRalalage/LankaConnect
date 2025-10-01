using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LankaConnect.Application.Common.Models.Performance;
using LankaConnect.Application.Common.Models.MultiLanguage;
using LankaConnect.Application.Common.Models;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Cache Optimization Service Interface
/// Handles multi-level caching strategy for language routing performance optimization
/// Performance targets: <100ms responses with L1/L2 caching strategy, proactive cultural event preparation
/// Decomposed from IMultiLanguageAffinityRoutingEngine following DDD principles
/// Supports 5x traffic scaling during major cultural celebrations and cache consistency maintenance
/// </summary>
public interface ICacheOptimizationService
{
    /// <summary>
    /// Optimize multi-level caching strategy for language routing performance
    /// Implements L1 memory cache and L2 distributed cache for <100ms responses
    /// </summary>
    /// <param name="cacheOptimizationRequest">Cache optimization parameters</param>
    /// <returns>Cache optimization strategy with performance improvements</returns>
    Task<CacheOptimizationResult> OptimizeMultiLevelCachingAsync(CacheOptimizationRequest cacheOptimizationRequest);

    /// <summary>
    /// Pre-warm caches for predicted cultural event traffic patterns
    /// Proactive cache preparation for major cultural celebrations
    /// </summary>
    /// <param name="culturalEvents">Upcoming cultural events</param>
    /// <param name="expectedTrafficMultiplier">Traffic increase multiplier (e.g., 5x for Vesak)</param>
    /// <returns>Cache pre-warming strategy and status</returns>
    Task<LankaConnect.Application.Common.Models.MultiLanguage.CachePreWarmingResult> PreWarmCachesForCulturalEventsAsync(
        List<LankaConnect.Domain.Common.Database.PerformanceCulturalEvent> culturalEvents,
        decimal expectedTrafficMultiplier);

    /// <summary>
    /// Invalidate and refresh cache strategically during profile updates
    /// Maintains cache consistency while minimizing performance impact
    /// </summary>
    /// <param name="affectedUserIds">User IDs affected by profile changes</param>
    /// <param name="cacheInvalidationStrategy">Strategy for cache invalidation</param>
    /// <returns>Cache invalidation and refresh status</returns>
    Task<LankaConnect.Application.Common.Models.CacheInvalidationResult> RefreshLanguageRoutingCachesAsync(
        List<Guid> affectedUserIds,
        LankaConnect.Application.Common.Models.CacheInvalidationStrategy cacheInvalidationStrategy);
}