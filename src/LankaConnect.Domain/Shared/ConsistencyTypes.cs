using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Domain.Shared;

/// <summary>
/// Cross-region consistency metrics for cultural intelligence
/// </summary>
public class CrossRegionConsistencyMetrics
{
    public required string MetricsId { get; set; }
    public required DateTime TimestampUtc { get; set; }
    public required Dictionary<string, decimal> RegionConsistencyScores { get; set; } = new();
    public required decimal OverallConsistencyScore { get; set; }
    public required List<string> InconsistentRegions { get; set; } = new();
    public required int DataConflictsDetected { get; set; }
    public required int ConflictsResolved { get; set; }
    public required TimeSpan AverageResolutionTime { get; set; }
    public List<string> ConsistencyWarnings { get; set; } = new();
    public Dictionary<string, object> RegionSpecificMetrics { get; set; } = new();
}

/// <summary>
/// Consistency validation request
/// </summary>
public class ConsistencyValidationRequest
{
    public required string ValidationId { get; set; }
    public required List<string> TargetRegions { get; set; } = new();
    public required List<CulturalDataType> DataTypesToValidate { get; set; } = new();
    public required DateTime RequestTimestamp { get; set; }
    public bool DeepValidation { get; set; }
    public required string RequestingService { get; set; }
    public Dictionary<string, object> ValidationParameters { get; set; } = new();
}

/// <summary>
/// Consistency validation result
/// </summary>
public class ConsistencyValidationResult
{
    public required string ValidationId { get; set; }
    public bool IsConsistent { get; set; }
    public required DateTime ValidationTimestamp { get; set; }
    public required List<string> ValidatedRegions { get; set; } = new();
    public required Dictionary<string, bool> RegionValidationResults { get; set; } = new();
    public required List<ConsistencyIssue> DetectedIssues { get; set; } = new();
    public required decimal OverallConsistencyScore { get; set; }
    public string? RecommendedAction { get; set; }
}

/// <summary>
/// Consistency issue details
/// </summary>
public class ConsistencyIssue
{
    public required string IssueId { get; set; }
    public required string IssueType { get; set; }
    public required string Description { get; set; }
    public required string AffectedRegion { get; set; }
    public required CulturalDataType DataType { get; set; }
    public required string Severity { get; set; }
    public required DateTime DetectedAt { get; set; }
    public string? ResolutionSuggestion { get; set; }
}

/// <summary>
/// Failover request for disaster recovery
/// </summary>
public class FailoverRequest
{
    public required string RequestId { get; set; }
    public required string SourceRegion { get; set; }
    public required string TargetRegion { get; set; }
    public required string FailoverTrigger { get; set; }
    public required DateTime RequestTimestamp { get; set; }
    public required List<string> ServicesToFailover { get; set; } = new();
    public bool EmergencyMode { get; set; }
    public Dictionary<string, object> FailoverParameters { get; set; } = new();
}