using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LankaConnect.Application.Common.Models.Performance;
using LankaConnect.Application.Common.Models.MultiLanguage;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Performance Monitoring Service Interface
/// Handles real-time monitoring and analytics for multi-language routing performance
/// Performance targets: <100ms standard, <50ms cultural events, Fortune 500 SLA compliance
/// Decomposed from IMultiLanguageAffinityRoutingEngine following DDD principles
/// Supports 5x traffic scaling during major cultural celebrations and continuous optimization
/// </summary>
public interface IPerformanceMonitoringService
{
    /// <summary>
    /// Monitor real-time performance metrics for multi-language routing
    /// Tracks sub-100ms response times and Fortune 500 SLA compliance
    /// </summary>
    /// <returns>Real-time performance dashboard data</returns>
    Task<LanguageRoutingPerformanceMetrics> GetRealTimePerformanceMetricsAsync();

    /// <summary>
    /// Generate comprehensive analytics for language routing patterns
    /// Provides insights for optimization and cultural intelligence enhancement
    /// </summary>
    /// <param name="analyticsRequest">Analytics generation parameters</param>
    /// <returns>Comprehensive routing analytics with pattern insights</returns>
    Task<LanguageRoutingAnalytics> GenerateLanguageRoutingAnalyticsAsync(LanguageRoutingAnalyticsRequest analyticsRequest);

    /// <summary>
    /// Validate system health and accuracy metrics for continuous monitoring
    /// Ensures cultural intelligence and routing accuracy meets quality standards
    /// </summary>
    /// <returns>System health validation with cultural intelligence metrics</returns>
    Task<LankaConnect.Application.Common.Models.Performance.SystemHealthValidation> ValidateSystemHealthAndAccuracyAsync();

    /// <summary>
    /// Benchmark performance against cultural event scaling requirements
    /// Validates 5x traffic handling capability during major cultural celebrations
    /// </summary>
    /// <param name="culturalEventScenarios">Cultural event traffic scenarios for benchmarking</param>
    /// <returns>Performance benchmark results with scaling validation</returns>
    Task<CulturalEventPerformanceBenchmark> BenchmarkCulturalEventScalingAsync(List<LankaConnect.Application.Common.Models.Performance.CulturalEventScenario> culturalEventScenarios);
}