using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.ValueObjects;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.Models.Critical;

/// <summary>
/// Comprehensive implementation of remaining missing types
/// All remaining types from compilation error analysis in one cohesive implementation
/// </summary>

// Recovery and Operations Types
public class MultiRegionRecoveryCoordinationResult
{
    public required string CoordinationId { get; set; }
    public required List<string> ParticipatingRegions { get; set; }
    public required RecoveryCoordinationStatus Status { get; set; }
    public required Dictionary<string, RecoveryRegionStatus> RegionStatuses { get; set; }
    public required DateTime CoordinationStartTime { get; set; }
    public DateTime? CoordinationEndTime { get; set; }
    public Dictionary<string, object> CoordinationMetrics { get; set; } = new();
}

public class OptimizationStrategy
{
    public required string StrategyId { get; set; }
    public required string StrategyName { get; set; }
    public required OptimizationStrategyType StrategyType { get; set; }
    public required Dictionary<string, object> Parameters { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}

// PerformanceCulturalEvent removed - defined in AdditionalMissingTypes.cs

public class RecoveryOperation
{
    public required string OperationId { get; set; }
    public required string OperationName { get; set; }
    public required RecoveryOperationType OperationType { get; set; }
    public required RecoveryOperationStatus Status { get; set; }
    public required DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public Dictionary<string, object> OperationMetrics { get; set; } = new();
}

public class RecoveryOptimizationResult
{
    public required string OptimizationId { get; set; }
    public required RecoveryOptimizationStatus Status { get; set; }
    public required Dictionary<string, double> OptimizationMetrics { get; set; }
    public required DateTime OptimizationTimestamp { get; set; }
    public List<string> OptimizedComponents { get; set; } = new();
}

public class RecoveryPointManagementResult
{
    public required string ManagementId { get; set; }
    public required RecoveryPointManagementStatus Status { get; set; }
    public required Dictionary<string, RecoveryPointInfo> ManagedPoints { get; set; }
    public required DateTime ManagementTimestamp { get; set; }
    public Dictionary<string, object> ManagementMetrics { get; set; } = new();
}

public class RecoveryTimeAdjustmentResult
{
    public required string AdjustmentId { get; set; }
    public required RecoveryTimeAdjustmentStatus Status { get; set; }
    public required TimeSpan OriginalRecoveryTime { get; set; }
    public required TimeSpan AdjustedRecoveryTime { get; set; }
    public required DateTime AdjustmentTimestamp { get; set; }
    public string? AdjustmentReason { get; set; }
}

public class RecoveryTimeManagementResult
{
    public required string ManagementId { get; set; }
    public required RecoveryTimeManagementStatus Status { get; set; }
    public required Dictionary<string, TimeSpan> ManagedRecoveryTimes { get; set; }
    public required DateTime ManagementTimestamp { get; set; }
    public Dictionary<string, object> TimeMetrics { get; set; } = new();
}

public class RecoveryTimeMonitoringResult
{
    public required string MonitoringId { get; set; }
    public required RecoveryTimeMonitoringStatus Status { get; set; }
    public required Dictionary<string, RecoveryTimeMetric> MonitoringResults { get; set; }
    public required DateTime MonitoringTimestamp { get; set; }
    public List<RecoveryTimeAlert> Alerts { get; set; } = new();
}

public class RecoveryTimeTestConfiguration
{
    public required string ConfigurationId { get; set; }
    public required string TestName { get; set; }
    public required RecoveryTimeTestType TestType { get; set; }
    public required Dictionary<string, object> TestParameters { get; set; }
    public required TimeSpan ExpectedRecoveryTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public class RecoveryTimeTestResult
{
    public required string TestId { get; set; }
    public required RecoveryTimeTestConfiguration Configuration { get; set; }
    public required RecoveryTimeTestStatus TestStatus { get; set; }
    public required TimeSpan ActualRecoveryTime { get; set; }
    public required DateTime TestTimestamp { get; set; }
    public required string TestExecutor { get; set; }
    public List<string> TestIssues { get; set; } = new();
    public Dictionary<string, object> TestMetrics { get; set; } = new();
}

public class TieredRecoveryManagementResult
{
    public required string ManagementId { get; set; }
    public required TieredRecoveryManagementStatus Status { get; set; }
    public required Dictionary<string, TieredRecoveryTier> ManagedTiers { get; set; }
    public required DateTime ManagementTimestamp { get; set; }
    public Dictionary<string, object> TierMetrics { get; set; } = new();
}

public class TieredRecoveryStrategy
{
    public required string StrategyId { get; set; }
    public required string StrategyName { get; set; }
    public required List<RecoveryTier> RecoveryTiers { get; set; }
    public required TieredRecoveryStrategyType StrategyType { get; set; }
    public required Dictionary<string, TierConfiguration> TierConfigurations { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;
}

// ServiceTier removed - duplicate exists in FrequentlyMissingTypes.cs

// Supporting Enums
public enum RecoveryCoordinationStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    PartiallyCompleted
}

public enum OptimizationStrategyType
{
    Performance,
    Cost,
    Reliability,
    Security,
    Cultural
}

// PerformanceImpactLevel removed - defined in AdditionalMissingTypes.cs

public enum RecoveryOperationType
{
    Full,
    Partial,
    Incremental,
    Emergency,
    Cultural
}

public enum RecoveryOperationStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Cancelled
}

public enum RecoveryOptimizationStatus
{
    InProgress,
    Completed,
    Failed,
    Optimized,
    NotRequired
}

public enum RecoveryPointManagementStatus
{
    Active,
    Inactive,
    Managing,
    Failed,
    Completed
}

public enum RecoveryTimeAdjustmentStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Cancelled
}

public enum RecoveryTimeManagementStatus
{
    Active,
    Inactive,
    Managing,
    Failed,
    Optimized
}

public enum RecoveryTimeMonitoringStatus
{
    Active,
    Inactive,
    Monitoring,
    Failed,
    AlertTriggered
}

public enum RecoveryTimeTestType
{
    Automated,
    Manual,
    Simulated,
    Production,
    Cultural
}

public enum RecoveryTimeTestStatus
{
    Scheduled,
    Running,
    Completed,
    Failed,
    Cancelled
}

public enum TieredRecoveryManagementStatus
{
    Active,
    Inactive,
    Managing,
    Failed,
    Optimized
}

public enum TieredRecoveryStrategyType
{
    Priority,
    Impact,
    Cultural,
    Business,
    Hybrid
}

// ServiceTierLevel removed - duplicate exists in FrequentlyMissingTypes.cs

// Supporting Complex Types
public class RecoveryRegionStatus
{
    public required string RegionId { get; set; }
    public required RecoveryStatus Status { get; set; }
    public DateTime LastUpdate { get; set; }
}

public class RecoveryPointInfo
{
    public required string PointId { get; set; }
    public required DateTime CreationTime { get; set; }
    public required RecoveryPointStatus Status { get; set; }
    public long DataSize { get; set; }
}

public class RecoveryTimeMetric
{
    public required string MetricName { get; set; }
    public required TimeSpan Value { get; set; }
    public required RecoveryTimeMetricStatus Status { get; set; }
}

public class RecoveryTimeAlert
{
    public required string AlertId { get; set; }
    public required RecoveryTimeAlertType AlertType { get; set; }
    public required string Message { get; set; }
    public required RecoveryTimeAlertSeverity Severity { get; set; }
    public DateTime AlertTimestamp { get; set; }
}

public class TieredRecoveryTier
{
    public required string TierId { get; set; }
    public required string TierName { get; set; }
    public required int Priority { get; set; }
    public required TimeSpan RecoveryTimeObjective { get; set; }
    public required Dictionary<string, object> TierConfiguration { get; set; }
}

public class RecoveryTier
{
    public required string TierId { get; set; }
    public required string TierName { get; set; }
    public required int Priority { get; set; }
    public required List<string> IncludedServices { get; set; }
}

public class TierConfiguration
{
    public required Dictionary<string, object> Configuration { get; set; }
    public required List<string> Requirements { get; set; }
    public bool IsEnabled { get; set; } = true;
}

// Additional supporting enums
public enum RecoveryStatus { Active, Inactive, Failed, Recovering, Completed }
public enum RecoveryPointStatus { Valid, Invalid, Corrupted, Creating, Available }
public enum RecoveryTimeMetricStatus { Normal, Warning, Critical, Unknown }
public enum RecoveryTimeAlertType { Threshold, Failure, Performance, Configuration }
public enum RecoveryTimeAlertSeverity { Low, Medium, High, Critical }

// Additional missing types that might be needed
public class PerformanceAlert
{
    public required string AlertId { get; set; }
    public required string AlertName { get; set; }
    public required PerformanceAlertType AlertType { get; set; }
    public required PerformanceAlertSeverity Severity { get; set; }
    public required string Description { get; set; }
    public required DateTime AlertTimestamp { get; set; }
}

public class PerformanceMetric
{
    public required string MetricId { get; set; }
    public required string MetricName { get; set; }
    public required double Value { get; set; }
    public required string Unit { get; set; }
    public required PerformanceMetricType MetricType { get; set; }
    public required DateTime MeasurementTimestamp { get; set; }
}

public class CulturalPerformanceMetric
{
    public required string MetricId { get; set; }
    public required CulturalEventType CulturalContext { get; set; }
    public required Dictionary<string, double> PerformanceValues { get; set; }
    public required DateTime MeasurementTimestamp { get; set; }
}

public class PerformanceTrendAnalysis
{
    public required string AnalysisId { get; set; }
    public required Dictionary<string, PerformanceTrend> Trends { get; set; }
    public required TimeSpan AnalysisPeriod { get; set; }
    public required DateTime AnalysisTimestamp { get; set; }
}

// CONSOLIDATED: Use LankaConnect.Domain.Common.ValueObjects.PerformanceThreshold

public class ResourceUtilization
{
    public required string ResourceId { get; set; }
    public required string ResourceType { get; set; }
    public required double UtilizationPercentage { get; set; }
    public required Dictionary<string, double> UtilizationMetrics { get; set; }
    public required DateTime MeasurementTimestamp { get; set; }
}

public class PerformanceOptimizationSuggestion
{
    public required string SuggestionId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required OptimizationPriority Priority { get; set; }
    public required List<string> ActionItems { get; set; }
}

public class PerformanceBaseline
{
    public required string BaselineId { get; set; }
    public required Dictionary<string, double> BaselineMetrics { get; set; }
    public required DateTime BaselineTimestamp { get; set; }
    public required TimeSpan BaselinePeriod { get; set; }
}

// Additional enums
public enum PerformanceAlertType { Threshold, Anomaly, Degradation, Failure }
public enum PerformanceAlertSeverity { Information, Warning, Error, Critical }
// PerformanceMetricType removed - defined in AdditionalMissingTypes.cs
public enum PerformanceThresholdType { Upper, Lower, Range, Deviation }
public enum OptimizationPriority { Low, Medium, High, Critical }

// Placeholder supporting classes
public class PerformanceTrend 
{ 
    public required string TrendDirection { get; set; }
    public required double TrendValue { get; set; }
    public required PerformanceTrendConfidence Confidence { get; set; }
}

public enum PerformanceTrendConfidence { Low, Medium, High, VeryHigh }

// Any remaining simple placeholder types that might be missing
public class ComplianceValidationResult
{
    public required string ValidationId { get; set; }
    public required ComplianceValidationStatus Status { get; set; }
    public required List<string> ValidationResults { get; set; }
    public required DateTime ValidationTimestamp { get; set; }
}

public enum ComplianceValidationStatus { Valid, Invalid, Pending, Error }