using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.DisasterRecovery;

/// <summary>
/// Recovery compliance report result for regulatory and audit requirements
/// Provides comprehensive compliance reporting for disaster recovery operations
/// </summary>
public class RecoveryComplianceReportResult
{
    public required string ReportId { get; set; }
    public required string ReportName { get; set; }
    public required ComplianceReportingScope ReportingScope { get; set; }
    public required RecoveryComplianceReportStatus Status { get; set; }
    public required DateTime GeneratedAt { get; set; }
    public required TimeSpan GenerationDuration { get; set; }
    public required string GeneratedBy { get; set; }
    public required List<ComplianceViolation> Violations { get; set; }
    public required Dictionary<string, ComplianceMetric> ComplianceMetrics { get; set; }
    public required ComplianceScore OverallComplianceScore { get; set; }
    public List<ComplianceRecommendation> Recommendations { get; set; } = new();
    public Dictionary<string, object> ReportContext { get; set; } = new();
    public string? ExecutiveSummary { get; set; }
    public List<string> CoveredComponents { get; set; } = new();
    public Dictionary<string, ComplianceAssessment> ComponentAssessments { get; set; } = new();
    public List<ComplianceEvidence> Evidence { get; set; } = new();

    public RecoveryComplianceReportResult()
    {
        Violations = new List<ComplianceViolation>();
        ComplianceMetrics = new Dictionary<string, ComplianceMetric>();
        Recommendations = new List<ComplianceRecommendation>();
        ReportContext = new Dictionary<string, object>();
        CoveredComponents = new List<string>();
        ComponentAssessments = new Dictionary<string, ComplianceAssessment>();
        Evidence = new List<ComplianceEvidence>();
    }

    public bool HasCriticalViolations()
    {
        return Violations.Any(v => v.Severity == ComplianceViolationSeverity.Critical);
    }

    public int GetViolationCount(ComplianceViolationSeverity severity)
    {
        return Violations.Count(v => v.Severity == severity);
    }

    public bool IsCompliant()
    {
        return !HasCriticalViolations() && OverallComplianceScore.Score >= 0.8;
    }
}

/// <summary>
/// Recovery compliance report status
/// </summary>
public enum RecoveryComplianceReportStatus
{
    Draft = 1,
    InProgress = 2,
    Completed = 3,
    Reviewed = 4,
    Approved = 5,
    Failed = 6
}

/// <summary>
/// Compliance violation
/// </summary>
public class ComplianceViolation
{
    public required string ViolationId { get; set; }
    public required ComplianceViolationType Type { get; set; }
    public required ComplianceViolationSeverity Severity { get; set; }
    public required string Description { get; set; }
    public required string AffectedComponent { get; set; }
    public required DateTime DetectedAt { get; set; }
    public required string RequirementReference { get; set; }
    public string? RecommendedAction { get; set; }
    public ComplianceViolationStatus Status { get; set; } = ComplianceViolationStatus.Open;
    public Dictionary<string, object> ViolationContext { get; set; } = new();
    public string? RemediationPlan { get; set; }
}

/// <summary>
/// Compliance violation types
/// </summary>
public enum ComplianceViolationType
{
    AccessControl = 1,
    DataProtection = 2,
    AuditTrail = 3,
    BackupPolicy = 4,
    RecoveryTime = 5,
    SecurityConfiguration = 6,
    DocumentationMissing = 7,
    ProcessViolation = 8
}

/// <summary>
/// Compliance violation severity
/// </summary>
public enum ComplianceViolationSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

/// <summary>
/// Compliance violation status
/// </summary>
public enum ComplianceViolationStatus
{
    Open = 1,
    InProgress = 2,
    Resolved = 3,
    Accepted = 4,
    Deferred = 5
}

/// <summary>
/// Compliance metric
/// </summary>
public class ComplianceMetric
{
    public required string MetricName { get; set; }
    public required double Value { get; set; }
    public required string Unit { get; set; }
    public required ComplianceMetricStatus Status { get; set; }
    public DateTime MeasuredAt { get; set; }
    public string? Description { get; set; }
    public double? TargetValue { get; set; }
    public double? ThresholdValue { get; set; }
}

/// <summary>
/// Compliance metric status
/// </summary>
public enum ComplianceMetricStatus
{
    Compliant = 1,
    NonCompliant = 2,
    Warning = 3,
    Unknown = 4
}

/// <summary>
/// Compliance score
/// </summary>
public class ComplianceScore
{
    public required double Score { get; set; }
    public required ComplianceScoreLevel Level { get; set; }
    public required DateTime CalculatedAt { get; set; }
    public required Dictionary<string, double> ComponentScores { get; set; }
    public string? ScoreDescription { get; set; }
    public List<string> ScoreFactors { get; set; } = new();
}

/// <summary>
/// Compliance score levels
/// </summary>
public enum ComplianceScoreLevel
{
    Poor = 1,
    Below = 2,
    Acceptable = 3,
    Good = 4,
    Excellent = 5
}

/// <summary>
/// Compliance recommendation
/// </summary>
public class ComplianceRecommendation
{
    public required string RecommendationId { get; set; }
    public required ComplianceRecommendationType Type { get; set; }
    public required ComplianceRecommendationPriority Priority { get; set; }
    public required string Description { get; set; }
    public required List<string> ActionItems { get; set; }
    public TimeSpan EstimatedEffort { get; set; }
    public string? Impact { get; set; }
    public string? RelatedViolationId { get; set; }
}

/// <summary>
/// Compliance recommendation types
/// </summary>
public enum ComplianceRecommendationType
{
    Policy = 1,
    Process = 2,
    Technical = 3,
    Training = 4,
    Documentation = 5,
    Monitoring = 6
}

/// <summary>
/// Compliance recommendation priority
/// </summary>
public enum ComplianceRecommendationPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Urgent = 4,
    Critical = 5
}

/// <summary>
/// Compliance assessment
/// </summary>
public class ComplianceAssessment
{
    public required string ComponentId { get; set; }
    public required ComplianceAssessmentStatus Status { get; set; }
    public required double ComplianceScore { get; set; }
    public required List<ComplianceCheck> ComplianceChecks { get; set; }
    public DateTime AssessedAt { get; set; }
    public string? AssessmentNotes { get; set; }
}

/// <summary>
/// Compliance assessment status
/// </summary>
public enum ComplianceAssessmentStatus
{
    Compliant = 1,
    NonCompliant = 2,
    PartiallyCompliant = 3,
    NotAssessed = 4,
    InProgress = 5
}

/// <summary>
/// Compliance check
/// </summary>
public class ComplianceCheck
{
    public required string CheckId { get; set; }
    public required string CheckName { get; set; }
    public required ComplianceCheckResult Result { get; set; }
    public required string RequirementReference { get; set; }
    public DateTime CheckedAt { get; set; }
    public string? CheckDetails { get; set; }
    public Dictionary<string, object> CheckMetadata { get; set; } = new();
}

/// <summary>
/// Compliance check results
/// </summary>
public enum ComplianceCheckResult
{
    Pass = 1,
    Fail = 2,
    Warning = 3,
    NotApplicable = 4,
    Manual = 5
}

/// <summary>
/// Compliance evidence
/// </summary>
public class ComplianceEvidence
{
    public required string EvidenceId { get; set; }
    public required ComplianceEvidenceType Type { get; set; }
    public required string Description { get; set; }
    public required string Source { get; set; }
    public DateTime CollectedAt { get; set; }
    public string? FilePath { get; set; }
    public Dictionary<string, object> EvidenceMetadata { get; set; } = new();
}

/// <summary>
/// Compliance evidence types
/// </summary>
public enum ComplianceEvidenceType
{
    Document = 1,
    Screenshot = 2,
    LogFile = 3,
    Configuration = 4,
    Report = 5,
    Certificate = 6
}