using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.DisasterRecovery;

/// <summary>
/// Restore point integrity validation result for backup restoration operations
/// Essential for ensuring restore points are valid and complete for disaster recovery
/// </summary>
public class RestorePointIntegrityResult
{
    public required string ResultId { get; set; }
    public required string RestorePointId { get; set; }
    public required string IntegrityCheckId { get; set; }
    public required RestorePointIntegrityStatus Status { get; set; }
    public required DateTime ValidationTimestamp { get; set; }
    public required TimeSpan ValidationDuration { get; set; }
    public required List<RestorePointComponent> ValidatedComponents { get; set; }
    public required Dictionary<string, RestorePointMetric> IntegrityMetrics { get; set; }
    public required string ValidatedBy { get; set; }
    public required double IntegrityScore { get; set; }
    public List<RestorePointIssue> Issues { get; set; } = new();
    public Dictionary<string, object> ValidationDetails { get; set; } = new();
    public bool IsRestoreReady { get; set; }
    public string? ValidationSummary { get; set; }
    public DateTime RestorePointCreatedAt { get; set; }
    public long RestorePointSize { get; set; }
    public List<string> ValidatedChecksums { get; set; } = new();

    public RestorePointIntegrityResult()
    {
        ValidatedComponents = new List<RestorePointComponent>();
        IntegrityMetrics = new Dictionary<string, RestorePointMetric>();
        Issues = new List<RestorePointIssue>();
        ValidationDetails = new Dictionary<string, object>();
        ValidatedChecksums = new List<string>();
    }

    public bool HasCriticalIssues()
    {
        return Issues.Any(i => i.Severity == RestorePointIssueSeverity.Critical);
    }

    public bool IsComponentValid(string componentId)
    {
        var component = ValidatedComponents.FirstOrDefault(c => c.ComponentId == componentId);
        return component?.Status == RestorePointComponentStatus.Valid;
    }
}

/// <summary>
/// Restore point integrity status
/// </summary>
public enum RestorePointIntegrityStatus
{
    Valid = 1,
    Invalid = 2,
    Corrupted = 3,
    Incomplete = 4,
    Unknown = 5,
    Validating = 6
}

/// <summary>
/// Restore point component information
/// </summary>
public class RestorePointComponent
{
    public required string ComponentId { get; set; }
    public required string ComponentName { get; set; }
    public required RestorePointComponentType ComponentType { get; set; }
    public required RestorePointComponentStatus Status { get; set; }
    public required long Size { get; set; }
    public required string Checksum { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ValidationMessage { get; set; }
    public Dictionary<string, object> ComponentMetadata { get; set; } = new();
}

/// <summary>
/// Restore point component types
/// </summary>
public enum RestorePointComponentType
{
    DatabaseDump = 1,
    FileSystem = 2,
    Configuration = 3,
    Metadata = 4,
    CulturalData = 5,
    SystemState = 6
}

/// <summary>
/// Restore point component status
/// </summary>
public enum RestorePointComponentStatus
{
    Valid = 1,
    Invalid = 2,
    Missing = 3,
    Corrupted = 4,
    Unknown = 5
}

/// <summary>
/// Restore point metric
/// </summary>
public class RestorePointMetric
{
    public required string MetricName { get; set; }
    public required double Value { get; set; }
    public required string Unit { get; set; }
    public required RestorePointMetricStatus Status { get; set; }
    public DateTime MeasuredAt { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Restore point metric status
/// </summary>
public enum RestorePointMetricStatus
{
    Normal = 1,
    Warning = 2,
    Critical = 3
}

/// <summary>
/// Restore point issue
/// </summary>
public class RestorePointIssue
{
    public required string IssueId { get; set; }
    public required RestorePointIssueType IssueType { get; set; }
    public required RestorePointIssueSeverity Severity { get; set; }
    public required string Description { get; set; }
    public required string AffectedComponent { get; set; }
    public DateTime DetectedAt { get; set; }
    public string? RecommendedAction { get; set; }
    public Dictionary<string, object> IssueContext { get; set; } = new();
}

/// <summary>
/// Restore point issue types
/// </summary>
public enum RestorePointIssueType
{
    Missing = 1,
    Corrupted = 2,
    Invalid = 3,
    Incomplete = 4,
    ChecksumMismatch = 5,
    AccessDenied = 6
}

/// <summary>
/// Restore point issue severity
/// </summary>
public enum RestorePointIssueSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}