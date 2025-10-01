using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.DisasterRecovery;

/// <summary>
/// Synchronization integrity validation result for disaster recovery operations
/// Critical for ensuring data consistency across distributed systems
/// </summary>
public class SynchronizationIntegrityResult
{
    public required string ResultId { get; set; }
    public required string SynchronizationId { get; set; }
    public required SynchronizationIntegrityStatus Status { get; set; }
    public required DateTime ValidationTimestamp { get; set; }
    public required TimeSpan ValidationDuration { get; set; }
    public required List<SynchronizationValidationCheck> ValidationChecks { get; set; }
    public required Dictionary<string, SynchronizationMetric> IntegrityMetrics { get; set; }
    public required SynchronizationScope Scope { get; set; }
    public required string ValidatedBy { get; set; }
    public required double IntegrityScore { get; set; }
    public List<SynchronizationIssue> Issues { get; set; } = new();
    public Dictionary<string, object> ValidationContext { get; set; } = new();
    public bool IsIntegrityIntact { get; set; }
    public string? ValidationSummary { get; set; }
    public List<string> AffectedRegions { get; set; } = new();
    public Dictionary<string, DateTime> LastSyncTimestamps { get; set; } = new();

    public SynchronizationIntegrityResult()
    {
        ValidationChecks = new List<SynchronizationValidationCheck>();
        IntegrityMetrics = new Dictionary<string, SynchronizationMetric>();
        Issues = new List<SynchronizationIssue>();
        ValidationContext = new Dictionary<string, object>();
        AffectedRegions = new List<string>();
        LastSyncTimestamps = new Dictionary<string, DateTime>();
    }

    public bool HasCriticalIssues()
    {
        return Issues.Any(i => i.Severity == SynchronizationIssueSeverity.Critical);
    }

    public TimeSpan GetMaxSyncLag()
    {
        if (!LastSyncTimestamps.Any()) return TimeSpan.Zero;
        var oldestSync = LastSyncTimestamps.Values.Min();
        return DateTime.UtcNow - oldestSync;
    }
}

/// <summary>
/// Synchronization integrity status
/// </summary>
public enum SynchronizationIntegrityStatus
{
    Healthy = 1,
    Warning = 2,
    Degraded = 3,
    Critical = 4,
    Failed = 5,
    Unknown = 6
}

/// <summary>
/// Synchronization scope definition
/// </summary>
public enum SynchronizationScope
{
    SingleRegion = 1,
    MultiRegion = 2,
    Global = 3,
    CulturalData = 4,
    SystemWide = 5
}

/// <summary>
/// Synchronization validation check
/// </summary>
public class SynchronizationValidationCheck
{
    public required string CheckId { get; set; }
    public required string CheckName { get; set; }
    public required SynchronizationCheckType CheckType { get; set; }
    public required SynchronizationCheckStatus Status { get; set; }
    public required string Description { get; set; }
    public DateTime ExecutedAt { get; set; }
    public TimeSpan ExecutionDuration { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Synchronization check types
/// </summary>
public enum SynchronizationCheckType
{
    DataConsistency = 1,
    TimestampValidation = 2,
    SchemaValidation = 3,
    ReferenceIntegrity = 4,
    CulturalDataIntegrity = 5
}

/// <summary>
/// Synchronization check status
/// </summary>
public enum SynchronizationCheckStatus
{
    Passed = 1,
    Failed = 2,
    Warning = 3,
    Skipped = 4
}

/// <summary>
/// Synchronization metric
/// </summary>
public class SynchronizationMetric
{
    public required string MetricName { get; set; }
    public required double Value { get; set; }
    public required string Unit { get; set; }
    public required SynchronizationMetricStatus Status { get; set; }
    public DateTime MeasuredAt { get; set; }
}

/// <summary>
/// Synchronization metric status
/// </summary>
public enum SynchronizationMetricStatus
{
    Normal = 1,
    Warning = 2,
    Critical = 3
}

/// <summary>
/// Synchronization issue
/// </summary>
public class SynchronizationIssue
{
    public required string IssueId { get; set; }
    public required SynchronizationIssueType IssueType { get; set; }
    public required SynchronizationIssueSeverity Severity { get; set; }
    public required string Description { get; set; }
    public required DateTime DetectedAt { get; set; }
    public string? RecommendedAction { get; set; }
    public List<string> AffectedComponents { get; set; } = new();
}

/// <summary>
/// Synchronization issue types
/// </summary>
public enum SynchronizationIssueType
{
    DataMismatch = 1,
    TimestampSkew = 2,
    MissingData = 3,
    CorruptedData = 4,
    NetworkIssue = 5
}

/// <summary>
/// Synchronization issue severity
/// </summary>
public enum SynchronizationIssueSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}