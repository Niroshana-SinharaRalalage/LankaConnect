using System;
using System.Collections.Generic;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.Models.Database;

/// <summary>
/// Language routing query for database optimization
/// </summary>
public class LanguageRoutingQuery
{
    public Guid QueryId { get; set; }
    public SouthAsianLanguage TargetLanguage { get; set; }
    public List<SouthAsianLanguage> FallbackLanguages { get; set; } = new();
    public string GeographicRegion { get; set; } = string.Empty;
    public Dictionary<string, object> OptimizationParameters { get; set; } = new();
    public bool UsePartitionAwareness { get; set; } = true;
    public int MaxResults { get; set; } = 1000;
    public TimeSpan QueryTimeout { get; set; } = TimeSpan.FromSeconds(5);
    public DateTime QueryTimestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Language routing query result with performance metrics
/// </summary>
public class LanguageRoutingQueryResult
{
    public Guid QueryId { get; set; }
    public List<object> Results { get; set; } = new();
    public int TotalCount { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public Dictionary<string, decimal> PerformanceMetrics { get; set; } = new();
    public List<string> PartitionsUsed { get; set; } = new();
    public List<string> IndexesUsed { get; set; } = new();
    public decimal CacheHitRatio { get; set; }
    public bool WasOptimized { get; set; }
    public DateTime ResultTimestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Database optimization strategy for cultural events
/// </summary>
public class DatabaseOptimizationStrategy
{
    public Guid StrategyId { get; set; }
    public List<string> OptimizationActions { get; set; } = new();
    public Dictionary<SouthAsianLanguage, string> LanguageSpecificOptimizations { get; set; } = new();
    public List<string> CachePreloadingStrategies { get; set; } = new();
    public Dictionary<string, object> PartitionOptimizations { get; set; } = new();
    public TimeSpan EstimatedPreparationTime { get; set; }
    public decimal ExpectedPerformanceImprovement { get; set; }
    public List<string> ResourceRequirements { get; set; } = new();
    public DateTime StrategyCreated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Database performance analysis with optimization recommendations
/// </summary>
public class DatabasePerformanceAnalysis
{
    public Guid AnalysisId { get; set; }
    public Dictionary<string, decimal> OverallMetrics { get; set; } = new();
    public Dictionary<SouthAsianLanguage, decimal> LanguageSpecificPerformance { get; set; } = new();
    public List<string> PerformanceBottlenecks { get; set; } = new();
    public List<string> OptimizationRecommendations { get; set; } = new();
    public Dictionary<string, decimal> PartitionEfficiency { get; set; } = new();
    public Dictionary<string, decimal> IndexUsageStats { get; set; } = new();
    public decimal AverageCacheHitRate { get; set; }
    public List<string> SlowQueries { get; set; } = new();
    public DateTime AnalysisTimestamp { get; set; } = DateTime.UtcNow;
}