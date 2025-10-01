using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Database;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.ValueObjects;

namespace LankaConnect.Application.Common.Models;

#region Connection Pool and Failover Types

public class FailoverConfiguration
{
    public string PrimaryPoolId { get; set; } = string.Empty;
    public string BackupPoolId { get; set; } = string.Empty;
    public TimeSpan FailoverTimeout { get; set; }
    public double FailoverThreshold { get; set; }
    public bool EnableAutomaticFailback { get; set; }
    public Dictionary<string, object> FailoverSettings { get; set; } = new();
}

public class FailoverResult
{
    public bool IsSuccessful { get; set; }
    public string ActivePoolId { get; set; } = string.Empty;
    public DateTime FailoverTime { get; set; }
    public string FailoverReason { get; set; } = string.Empty;
    public TimeSpan FailoverDuration { get; set; }
    public IEnumerable<string> Warnings { get; set; } = new List<string>();
    public IEnumerable<string> Errors { get; set; } = new List<string>();
}

public class CulturalRoutingContext
{
    public string Region { get; set; } = string.Empty;
    public CulturalEventType EventType { get; set; }
    public List<string> PreferredLanguages { get; set; } = new();
    public Dictionary<string, object> RoutingMetadata { get; set; } = new();
}

public class RoutingStrategy
{
    public string StrategyName { get; set; } = string.Empty;
    public RoutingAlgorithm Algorithm { get; set; }
    public Dictionary<string, double> Weights { get; set; } = new();
    public List<string> ConstraintRules { get; set; } = new();
}

public class RoutingOptimizationResult
{
    public bool IsSuccessful { get; set; }
    public string OptimalRoute { get; set; } = string.Empty;
    public double RoutingScore { get; set; }
    public List<string> OptimizationSteps { get; set; } = new();
    public Dictionary<string, object> RouteMetrics { get; set; } = new();
    public IEnumerable<string> Warnings { get; set; } = new List<string>();
}

public class WarmupConfiguration
{
    public int InitialConnections { get; set; }
    public TimeSpan WarmupDuration { get; set; }
    public double TargetUtilization { get; set; }
    public List<string> WarmupQueries { get; set; } = new();
    public Dictionary<string, object> WarmupSettings { get; set; } = new();
}

public class WarmupResult
{
    public bool IsSuccessful { get; set; }
    public int ConnectionsWarmedUp { get; set; }
    public TimeSpan ActualWarmupDuration { get; set; }
    public double AchievedUtilization { get; set; }
    public List<string> WarmupActions { get; set; } = new();
    public IEnumerable<string> Issues { get; set; } = new List<string>();
}

public class AdaptivePoolingConfiguration
{
    public string PoolingAlgorithm { get; set; } = string.Empty;
    public double AdaptationThreshold { get; set; }
    public TimeSpan AdaptationInterval { get; set; }
    public int MinPoolSize { get; set; }
    public int MaxPoolSize { get; set; }
    public Dictionary<string, object> AdaptationRules { get; set; } = new();
}

public class AdaptivePoolingResult
{
    public bool IsSuccessful { get; set; }
    public int NewPoolSize { get; set; }
    public string AdaptationReason { get; set; } = string.Empty;
    public double PerformanceImprovement { get; set; }
    public List<string> AdaptationActions { get; set; } = new();
    public Dictionary<string, object> AdaptationMetrics { get; set; } = new();
}

#endregion

#region Performance Monitoring Types

public class MetricsConfiguration
{
    public TimeSpan CollectionInterval { get; set; }
    public List<string> MetricTypes { get; set; } = new();
    public Dictionary<string, double> ThresholdLevels { get; set; } = new();
    public bool EnableRealTimeMetrics { get; set; }
    public string MetricsStorageLocation { get; set; } = string.Empty;
}

public class CulturalPerformanceAlert
{
    public string AlertId { get; set; } = string.Empty;
    public CulturalEventType EventType { get; set; }
    public required AlertSeverity Severity { get; set; }
    public string MetricName { get; set; } = string.Empty;
    public double ThresholdValue { get; set; }
    public string AlertCondition { get; set; } = string.Empty;
    public List<string> NotificationChannels { get; set; } = new();
}

public class AlertConfigurationResult
{
    public bool IsSuccessful { get; set; }
    public int AlertsConfigured { get; set; }
    public List<string> ConfiguredAlertIds { get; set; } = new();
    public Dictionary<string, object> ConfigurationMetrics { get; set; } = new();
    public IEnumerable<string> Warnings { get; set; } = new List<string>();
    public IEnumerable<string> Errors { get; set; } = new List<string>();
}

public class TrendAnalysisParameters
{
    public TimeSpan AnalysisPeriod { get; set; }
    public List<string> MetricsToAnalyze { get; set; } = new();
    public double TrendSignificanceThreshold { get; set; }
    public bool IncludeSeasonalAnalysis { get; set; }
    public Dictionary<string, object> AnalysisSettings { get; set; } = new();
}

public class PerformanceTrendAnalysis
{
    public string AnalysisId { get; set; } = string.Empty;
    public DateTime AnalysisDate { get; set; }
    public List<TrendMetric> TrendMetrics { get; set; } = new();
    public List<string> Insights { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public Dictionary<string, object> TrendData { get; set; } = new();
}

public class TrendMetric
{
    public string MetricName { get; set; } = string.Empty;
    public double CurrentValue { get; set; }
    public double PreviousValue { get; set; }
    public double PercentageChange { get; set; }
    public TrendDirection Direction { get; set; }
    public double SignificanceScore { get; set; }
}

#endregion

#region Database Security and AI Model Types

public class CulturalIntelligenceModel
{
    public string ModelId { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public ModelType Type { get; set; }
    public string Version { get; set; } = string.Empty;
    public List<string> SupportedLanguages { get; set; } = new();
    public Dictionary<string, object> ModelParameters { get; set; } = new();
}

public class ModelSecurityCriteria
{
    public string CriteriaId { get; set; } = string.Empty;
    public SecurityLevel RequiredSecurityLevel { get; set; }
    public List<string> SecurityRequirements { get; set; } = new();
    public Dictionary<string, object> ValidationRules { get; set; } = new();
    public bool RequireEncryption { get; set; }
}

public class ModelSecurityResult
{
    public bool IsSuccessful { get; set; }
    public string ModelId { get; set; } = string.Empty;
    public SecurityStatus SecurityStatus { get; set; }
    public List<string> SecurityMeasuresApplied { get; set; } = new();
    public Dictionary<string, object> SecurityMetrics { get; set; } = new();
    public IEnumerable<string> SecurityWarnings { get; set; } = new List<string>();
    public IEnumerable<string> SecurityErrors { get; set; } = new List<string>();
}

#endregion

#region Connection Pool Management Types

public class ConnectionPoolConfiguration
{
    public string PoolId { get; set; } = string.Empty;
    public int MinimumConnections { get; set; }
    public int MaximumConnections { get; set; }
    public TimeSpan ConnectionTimeout { get; set; }
    public TimeSpan IdleTimeout { get; set; }
    public string DatabaseConnectionString { get; set; } = string.Empty;
    public Dictionary<string, object> PoolSettings { get; set; } = new();
}

public class ConnectionPoolInitializationResult
{
    public bool IsSuccessful { get; set; }
    public string PoolId { get; set; } = string.Empty;
    public int InitializedConnections { get; set; }
    public DateTime InitializationTime { get; set; }
    public TimeSpan InitializationDuration { get; set; }
    public List<string> InitializationSteps { get; set; } = new();
    public IEnumerable<string> Warnings { get; set; } = new List<string>();
    public IEnumerable<string> Errors { get; set; } = new List<string>();
}

public class PoolOptimizationStrategy
{
    public string StrategyName { get; set; } = string.Empty;
    public OptimizationGoal Goal { get; set; }
    public Dictionary<string, double> OptimizationTargets { get; set; } = new();
    public List<string> OptimizationConstraints { get; set; } = new();
    public TimeSpan OptimizationInterval { get; set; }
}

public class PoolOptimizationResult
{
    public bool IsSuccessful { get; set; }
    public string PoolId { get; set; } = string.Empty;
    public int PreviousSize { get; set; }
    public int NewSize { get; set; }
    public double PerformanceImprovement { get; set; }
    public List<string> OptimizationActions { get; set; } = new();
    public Dictionary<string, object> OptimizationMetrics { get; set; } = new();
}

public class HealthCheckParameters
{
    public TimeSpan CheckInterval { get; set; }
    public List<string> HealthChecksToRun { get; set; } = new();
    public Dictionary<string, double> HealthThresholds { get; set; } = new();
    public bool EnableDetailedReporting { get; set; }
    public int RetryCount { get; set; }
}

public class PoolHealthMetrics
{
    public string PoolId { get; set; } = string.Empty;
    public HealthStatus Status { get; set; }
    public double HealthScore { get; set; }
    public int ActiveConnections { get; set; }
    public int IdleConnections { get; set; }
    public double AverageResponseTime { get; set; }
    public DateTime LastHealthCheck { get; set; }
    public List<string> HealthIssues { get; set; } = new();
}

#endregion

#region Supporting Enums

public enum RoutingAlgorithm
{
    RoundRobin,
    WeightedRoundRobin,
    LeastConnections,
    CulturalAffinity,
    GeographicProximity
}


public enum TrendDirection
{
    Increasing,
    Decreasing,
    Stable,
    Volatile
}

public enum ModelType
{
    LanguageModel,
    CulturalAnalysis,
    RecommendationEngine,
    SentimentAnalysis,
    TranslationModel
}

// Note: SecurityLevel enum moved to canonical location: LankaConnect.Domain.Common.Database.DatabaseSecurityModels.SecurityLevel
// Use Domain enum instead for consistency

public enum OptimizationGoal
{
    Performance,
    CostEfficiency,
    ResourceUtilization,
    LatencyOptimization,
    ThroughputMaximization
}

public enum HealthStatus
{
    Healthy,
    Warning,
    Critical,
    Down,
    Unknown
}

#endregion