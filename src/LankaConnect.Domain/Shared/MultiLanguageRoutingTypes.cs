using System;
using System.Collections.Generic;

namespace LankaConnect.Domain.Shared;

/// <summary>
/// Multi-language routing request
/// </summary>
public class MultiLanguageRoutingRequest
{
    public Guid UserId { get; set; }
    public List<SouthAsianLanguage> RequestedLanguages { get; set; } = new();
    public CulturalEventContext? EventContext { get; set; }
    public DatabaseFailoverMode FailoverMode { get; set; }
    public bool RequiresCulturalValidation { get; set; }
    public CulturalRegion UserRegion { get; set; }
}

/// <summary>
/// Multi-language routing response
/// </summary>
public class MultiLanguageRoutingResponse
{
    public SouthAsianLanguage PrimaryLanguage { get; set; }
    public List<SouthAsianLanguage> AlternativeLanguages { get; set; } = new();
    public decimal RoutingAccuracy { get; set; }
    public decimal CacheHitRate { get; set; }
    public int DatabaseQueriesCount { get; set; }
    public bool DatabaseFailoverUsed { get; set; }
    public decimal PerformanceDegradation { get; set; }
    public bool ServiceContinuity { get; set; }
    public TimeSpan ResponseTime { get; set; }
}

/// <summary>
/// Batch multi-language routing response
/// </summary>
public class BatchMultiLanguageRoutingResponse
{
    public int TotalRequests { get; set; }
    public int SuccessfulRoutes { get; set; }
    public List<MultiLanguageRoutingResponse> IndividualResponses { get; set; } = new();
    public decimal AverageResponseTime { get; set; }
    public decimal BatchOptimizationGain { get; set; }
}

/// <summary>
/// Language interaction data for preference learning
/// </summary>
public class LanguageInteractionData
{
    public SouthAsianLanguage Language { get; set; }
    public decimal EngagementScore { get; set; }
    public TimeSpan InteractionDuration { get; set; }
    public DateTime InteractionTime { get; set; }
    public string InteractionType { get; set; } = string.Empty;
}

/// <summary>
/// Language routing query
/// </summary>
public class LanguageRoutingQuery
{
    public List<SouthAsianLanguage> Languages { get; set; } = new();
    public CulturalRegion Region { get; set; }
    public GenerationalCohort? GenerationalFilter { get; set; }
    public DateTime? EventDateFilter { get; set; }
    public int MaxResults { get; set; } = 100;
}

/// <summary>
/// Language routing query result
/// </summary>
public class LanguageRoutingQueryResult
{
    public string PartitionHit { get; set; } = string.Empty;
    public List<string> IndexUsage { get; set; } = new();
    public TimeSpan QueryTime { get; set; }
    public bool CacheHit { get; set; }
    public List<MultiLanguageUserProfile> Results { get; set; } = new();
    public int TotalCount { get; set; }
}

/// <summary>
/// Language routing performance metrics
/// </summary>
public class LanguageRoutingPerformanceMetrics
{
    public TimeSpan AverageResponseTime { get; set; }
    public decimal CacheHitRate { get; set; }
    public decimal RoutingAccuracy { get; set; }
    public int ConcurrentRequests { get; set; }
    public SystemHealthStatus SystemHealth { get; set; }
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Language routing analytics request
/// </summary>
public class LanguageRoutingAnalyticsRequest
{
    public TimeSpan AnalysisPeriod { get; set; }
    public List<SouthAsianLanguage> TargetLanguages { get; set; } = new();
    public List<CulturalEvent> TargetEvents { get; set; } = new();
    public CulturalRegion? RegionFilter { get; set; }
}

/// <summary>
/// Language routing analytics result
/// </summary>
public class LanguageRoutingAnalytics
{
    public TimeSpan AnalysisPeriod { get; set; }
    public int TotalRoutingRequests { get; set; }
    public Dictionary<SouthAsianLanguage, decimal> LanguageDistribution { get; set; } = new();
    public Dictionary<CulturalEvent, decimal> CulturalEventImpact { get; set; } = new();
    public Dictionary<CulturalRegion, int> RegionalDistribution { get; set; } = new();
}

/// <summary>
/// System health validation result
/// </summary>
public class SystemHealthValidation
{
    public SystemHealthStatus OverallHealth { get; set; }
    public decimal CulturalIntelligenceAccuracy { get; set; }
    public decimal LanguageDetectionAccuracy { get; set; }
    public decimal ResponseTimeCompliance { get; set; }
    public decimal UpTime { get; set; }
    public DateTime ValidationTimestamp { get; set; }
    public List<string> HealthIssues { get; set; } = new();
}

/// <summary>
/// Cultural event scenario for benchmarking
/// </summary>
public class CulturalEventScenario
{
    public CulturalEvent Event { get; set; }
    public CulturalEventIntensity Intensity { get; set; }
    public int ExpectedConcurrentUsers { get; set; }
    public TimeSpan Duration { get; set; }
    public List<CulturalRegion> AffectedRegions { get; set; } = new();
}

/// <summary>
/// Cultural event performance benchmark
/// </summary>
public class CulturalEventPerformanceBenchmark
{
    public decimal ScalingCapability { get; set; }
    public int PeakTrafficHandled { get; set; }
    public TimeSpan ResponseTimeDuringPeak { get; set; }
    public decimal SystemStability { get; set; }
    public DateTime BenchmarkTimestamp { get; set; }
    public List<string> PerformanceInsights { get; set; } = new();
}