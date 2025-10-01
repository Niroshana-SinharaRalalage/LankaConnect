using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LankaConnect.Application.Common.Models.Performance;
using LankaConnect.Application.Common.Models.Database;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Database Query Optimization Service Interface
/// Handles language-aware database optimization for South Asian diaspora routing
/// Performance targets: <50ms queries with partition awareness
/// Decomposed from IMultiLanguageAffinityRoutingEngine following DDD principles
/// Supports cultural event traffic preparation and continuous performance monitoring
/// </summary>
public interface IDatabaseQueryOptimizationService
{
    /// <summary>
    /// Execute optimized language routing queries with partition awareness
    /// Leverages language-aware database partitioning for <50ms queries
    /// </summary>
    /// <param name="query">Language routing query with optimization parameters</param>
    /// <returns>Query results with performance metrics</returns>
    Task<LankaConnect.Application.Common.Models.Database.LanguageRoutingQueryResult> QueryLanguageRoutingDataAsync(LankaConnect.Application.Common.Models.Database.LanguageRoutingQuery query);

    /// <summary>
    /// Optimize database queries for cultural event traffic patterns
    /// Pre-loads and caches data for predicted cultural event surges
    /// </summary>
    /// <param name="culturalEvents">Upcoming cultural events for optimization</param>
    /// <param name="optimizationPeriod">Time period for optimization preparation</param>
    /// <returns>Database optimization strategy and cache preparation</returns>
    Task<LankaConnect.Application.Common.Models.Database.DatabaseOptimizationStrategy> OptimizeDatabaseForCulturalEventsAsync(
        List<LankaConnect.Domain.Common.Database.PerformanceCulturalEvent> culturalEvents,
        TimeSpan optimizationPeriod);

    /// <summary>
    /// Monitor and analyze query performance for continuous optimization
    /// Tracks partition efficiency, index usage, and cache hit rates
    /// </summary>
    /// <returns>Database performance analysis with optimization recommendations</returns>
    Task<LankaConnect.Application.Common.Models.Database.DatabasePerformanceAnalysis> AnalyzeDatabasePerformanceAsync();
}