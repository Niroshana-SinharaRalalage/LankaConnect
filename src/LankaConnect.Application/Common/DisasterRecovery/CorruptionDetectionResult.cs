using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Application.Common.Models.Critical;

namespace LankaConnect.Application.Common.DisasterRecovery;

/// <summary>
/// Corruption detection result for data integrity monitoring
/// Critical for identifying and reporting data corruption issues in disaster recovery
/// </summary>
public class CorruptionDetectionResult
{
    public required string DetectionId { get; set; }
    public required string ScanId { get; set; }
    public required CorruptionDetectionStatus Status { get; set; }
    public required DateTime DetectionTimestamp { get; set; }
    public required TimeSpan ScanDuration { get; set; }
    public required CorruptionDetectionScope Scope { get; set; }
    public required List<CorruptionIncident> DetectedCorruption { get; set; }
    public required Dictionary<string, CorruptionMetric> DetectionMetrics { get; set; }
    public required string DetectedBy { get; set; }
    public required CorruptionSeverityLevel OverallSeverity { get; set; }
    public List<CorruptionRecommendation> Recommendations { get; set; } = new();
    public Dictionary<string, object> ScanContext { get; set; } = new();
    public bool RequiresImmediateAction { get; set; }
    public string? DetectionSummary { get; set; }
    public List<string> ScannedResources { get; set; } = new();
    public Dictionary<string, int> CorruptionCounts { get; set; } = new();

    public CorruptionDetectionResult()
    {
        DetectedCorruption = new List<CorruptionIncident>();
        DetectionMetrics = new Dictionary<string, CorruptionMetric>();
        Recommendations = new List<CorruptionRecommendation>();
        ScanContext = new Dictionary<string, object>();
        ScannedResources = new List<string>();
        CorruptionCounts = new Dictionary<string, int>();
    }

    public bool HasCriticalCorruption()
    {
        return DetectedCorruption.Any(c => c.Severity == CorruptionSeverityLevel.Critical);
    }

    public int GetTotalCorruptionCount()
    {
        return DetectedCorruption.Count;
    }
}

/// <summary>
/// Corruption detection status
/// </summary>
public enum CorruptionDetectionStatus
{
    Clean = 1,
    Suspicious = 2,
    Corrupted = 3,
    Critical = 4,
    Unknown = 5,
    Scanning = 6
}

/// <summary>
/// Corruption severity level
/// </summary>
public enum CorruptionSeverityLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4,
    Emergency = 5
}

/// <summary>
/// Corruption incident details
/// </summary>
public class CorruptionIncident
{
    public required string IncidentId { get; set; }
    public required CorruptionType CorruptionType { get; set; }
    public required CorruptionSeverityLevel Severity { get; set; }
    public required string AffectedResource { get; set; }
    public required string Description { get; set; }
    public required DateTime DetectedAt { get; set; }
    public string? CorruptionSignature { get; set; }
    public Dictionary<string, object> IncidentMetadata { get; set; } = new();
    public string? RecommendedAction { get; set; }
}

/// <summary>
/// Types of corruption detected
/// </summary>
public enum CorruptionType
{
    DataIntegrity = 1,
    ChecksumMismatch = 2,
    StructuralDamage = 3,
    ReferenceCorruption = 4,
    CulturalDataCorruption = 5
}

/// <summary>
/// Corruption detection metric
/// </summary>
public class CorruptionMetric
{
    public required string MetricName { get; set; }
    public required double Value { get; set; }
    public required string Unit { get; set; }
    public required CorruptionMetricStatus Status { get; set; }
    public DateTime MeasuredAt { get; set; }
}

/// <summary>
/// Corruption metric status
/// </summary>
public enum CorruptionMetricStatus
{
    Normal = 1,
    Warning = 2,
    Critical = 3
}

/// <summary>
/// Corruption remediation recommendation
/// </summary>
public class CorruptionRecommendation
{
    public required string RecommendationId { get; set; }
    public required CorruptionRecommendationType Type { get; set; }
    public required string Description { get; set; }
    public required CorruptionRecommendationPriority Priority { get; set; }
    public required List<string> ActionSteps { get; set; }
    public TimeSpan EstimatedEffort { get; set; }
    public string? Impact { get; set; }
}

/// <summary>
/// Corruption recommendation types
/// </summary>
public enum CorruptionRecommendationType
{
    Repair = 1,
    Restore = 2,
    Isolate = 3,
    Replace = 4,
    Monitor = 5
}

/// <summary>
/// Corruption recommendation priority
/// </summary>
public enum CorruptionRecommendationPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Urgent = 4,
    Emergency = 5
}