using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.DisasterRecovery;

/// <summary>
/// Dynamic recovery adjustment result for adaptive disaster recovery operations
/// Provides real-time adjustment capabilities for recovery processes
/// </summary>
public class DynamicRecoveryAdjustmentResult
{
    public required string AdjustmentId { get; set; }
    public required string RecoveryOperationId { get; set; }
    public required DynamicRecoveryAdjustmentStatus Status { get; set; }
    public required DateTime AdjustmentTimestamp { get; set; }
    public required List<RecoveryAdjustment> AppliedAdjustments { get; set; }
    public required Dictionary<string, RecoveryMetric> AdjustmentMetrics { get; set; }
    public required string AdjustedBy { get; set; }
    public required DynamicRecoveryAdjustmentType AdjustmentType { get; set; }
    public required TimeSpan AdjustmentDuration { get; set; }
    public List<RecoveryImpactAssessment> ImpactAssessments { get; set; } = new();
    public Dictionary<string, object> AdjustmentContext { get; set; } = new();
    public bool RequiresValidation { get; set; }
    public string? AdjustmentSummary { get; set; }
    public List<string> AffectedComponents { get; set; } = new();
    public Dictionary<string, string> ConfigurationChanges { get; set; } = new();

    public DynamicRecoveryAdjustmentResult()
    {
        AppliedAdjustments = new List<RecoveryAdjustment>();
        AdjustmentMetrics = new Dictionary<string, RecoveryMetric>();
        ImpactAssessments = new List<RecoveryImpactAssessment>();
        AdjustmentContext = new Dictionary<string, object>();
        AffectedComponents = new List<string>();
        ConfigurationChanges = new Dictionary<string, string>();
    }

    public bool HasSuccessfulAdjustments()
    {
        return AppliedAdjustments.Any(a => a.Status == RecoveryAdjustmentStatus.Success);
    }

    public int GetTotalAdjustmentCount()
    {
        return AppliedAdjustments.Count;
    }
}

/// <summary>
/// Dynamic recovery adjustment status
/// </summary>
public enum DynamicRecoveryAdjustmentStatus
{
    InProgress = 1,
    Completed = 2,
    Failed = 3,
    PartiallyApplied = 4,
    Rollback = 5,
    Pending = 6
}

/// <summary>
/// Dynamic recovery adjustment types
/// </summary>
public enum DynamicRecoveryAdjustmentType
{
    Performance = 1,
    Capacity = 2,
    Priority = 3,
    Resource = 4,
    Configuration = 5,
    Strategy = 6
}

/// <summary>
/// Recovery adjustment details
/// </summary>
public class RecoveryAdjustment
{
    public required string AdjustmentId { get; set; }
    public required RecoveryAdjustmentType Type { get; set; }
    public required RecoveryAdjustmentStatus Status { get; set; }
    public required string Description { get; set; }
    public required DateTime AppliedAt { get; set; }
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
    public Dictionary<string, object> AdjustmentMetadata { get; set; } = new();
    public string? RollbackInformation { get; set; }
}

/// <summary>
/// Recovery adjustment types
/// </summary>
public enum RecoveryAdjustmentType
{
    ResourceAllocation = 1,
    PriorityAdjustment = 2,
    TimeoutModification = 3,
    RetryConfiguration = 4,
    CapacityScaling = 5,
    StrategyChange = 6
}

/// <summary>
/// Recovery adjustment status
/// </summary>
public enum RecoveryAdjustmentStatus
{
    Success = 1,
    Failed = 2,
    Partial = 3,
    Rollback = 4,
    Pending = 5
}

/// <summary>
/// Recovery metric
/// </summary>
public class RecoveryMetric
{
    public required string MetricName { get; set; }
    public required double Value { get; set; }
    public required string Unit { get; set; }
    public required RecoveryMetricStatus Status { get; set; }
    public DateTime MeasuredAt { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Recovery metric status
/// </summary>
public enum RecoveryMetricStatus
{
    Normal = 1,
    Warning = 2,
    Critical = 3,
    Improving = 4,
    Degrading = 5
}

/// <summary>
/// Recovery impact assessment
/// </summary>
public class RecoveryImpactAssessment
{
    public required string AssessmentId { get; set; }
    public required RecoveryImpactType ImpactType { get; set; }
    public required RecoveryImpactSeverity Severity { get; set; }
    public required string Description { get; set; }
    public required List<string> AffectedComponents { get; set; }
    public DateTime AssessedAt { get; set; }
    public string? MitigationStrategy { get; set; }
    public Dictionary<string, object> ImpactMetrics { get; set; } = new();
}

/// <summary>
/// Recovery impact types
/// </summary>
public enum RecoveryImpactType
{
    Performance = 1,
    Availability = 2,
    Data = 3,
    Security = 4,
    Business = 5,
    Financial = 6
}

/// <summary>
/// Recovery impact severity
/// </summary>
public enum RecoveryImpactSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4,
    Severe = 5
}