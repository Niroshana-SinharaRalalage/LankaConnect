using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.ValueObjects;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.Models.Critical;

/// <summary>
/// Most frequently referenced missing types causing compilation failures
/// These are the highest impact types that appear multiple times in error logs
/// Priority implementation for maximum error reduction per type
/// </summary>

/// <summary>
/// Data integrity validation scope for backup and recovery operations
/// High-frequency type referenced across disaster recovery interfaces
/// </summary>
public class DataIntegrityValidationScope
{
    public required string ScopeId { get; set; }
    public required string ScopeName { get; set; }
    public required DataIntegrityValidationType ValidationType { get; set; }
    public required List<string> IncludedDataTypes { get; set; }
    public required List<string> ExcludedDataTypes { get; set; }
    public required List<string> ValidatedRegions { get; set; }
    public required DataIntegrityDepth ValidationDepth { get; set; }
    public required Dictionary<string, ValidationCriteria> ValidationCriteria { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

/// <summary>
/// Data integrity validation result for comprehensive backup validation
/// Critical for disaster recovery and compliance requirements
/// </summary>
public class DataIntegrityValidationResult
{
    public required string ValidationId { get; set; }
    public required DataIntegrityValidationScope Scope { get; set; }
    public required DataIntegrityStatus ValidationStatus { get; set; }
    public required List<DataIntegrityIssue> IdentifiedIssues { get; set; }
    public required Dictionary<string, ValidationMetric> ValidationMetrics { get; set; }
    public required DateTime ValidationStartTime { get; set; }
    public DateTime? ValidationEndTime { get; set; }
    public required string ValidatedBy { get; set; }
    public required double IntegrityScore { get; set; }
    public required List<DataIntegrityRecommendation> Recommendations { get; set; }
    public Dictionary<string, object> ValidationDetails { get; set; } = new();
    public bool RequiresRemediation { get; set; }
    public string? ValidationNotes { get; set; }
}

/// <summary>
/// Backup verification result for backup integrity and recoverability
/// Frequently referenced in disaster recovery and backup interfaces
/// </summary>
public class BackupVerificationResult
{
    public required string VerificationId { get; set; }
    public required string BackupId { get; set; }
    public required BackupVerificationStatus VerificationStatus { get; set; }
    public required List<BackupVerificationCheck> VerificationChecks { get; set; }
    public required BackupIntegrityMetrics IntegrityMetrics { get; set; }
    public required DateTime VerificationTimestamp { get; set; }
    public required TimeSpan VerificationDuration { get; set; }
    public required string VerifiedBy { get; set; }
    public required bool IsRecoverable { get; set; }
    public required List<BackupVerificationIssue> Issues { get; set; }
    public Dictionary<string, object> VerificationMetadata { get; set; } = new();
    public string? VerificationNotes { get; set; }
    public DateTime? NextVerificationDate { get; set; }
}

/// <summary>
/// Consistency check level for data consistency validation
/// Referenced across backup and disaster recovery operations
/// </summary>
public class ConsistencyCheckLevel
{
    public required string LevelId { get; set; }
    public required string LevelName { get; set; }
    public required ConsistencyCheckType CheckType { get; set; }
    public required ConsistencyCheckSeverity Severity { get; set; }
    public required List<ConsistencyCheckRule> CheckRules { get; set; }
    public required Dictionary<string, object> CheckConfiguration { get; set; }
    public required TimeSpan MaxExecutionTime { get; set; }
    public required int Priority { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? Description { get; set; }
}

/// <summary>
/// Consistency validation result for data consistency checks
/// Critical for maintaining data integrity across regions
/// </summary>
public class ConsistencyValidationResult
{
    public required string ValidationId { get; set; }
    public required ConsistencyCheckLevel CheckLevel { get; set; }
    public required ConsistencyStatus ValidationStatus { get; set; }
    public required List<ConsistencyViolation> ConsistencyViolations { get; set; }
    public required Dictionary<string, ConsistencyMetric> ConsistencyMetrics { get; set; }
    public required DateTime ValidationTimestamp { get; set; }
    public required TimeSpan ValidationDuration { get; set; }
    public required string ValidatedBy { get; set; }
    public required double ConsistencyScore { get; set; }
    public required List<ConsistencyRecommendation> Recommendations { get; set; }
    public Dictionary<string, object> ValidationContext { get; set; } = new();
    public bool RequiresImmediateAction { get; set; }
    public string? ValidationSummary { get; set; }
}

/// <summary>
/// Integrity monitoring configuration for ongoing data integrity monitoring
/// Referenced in backup and disaster recovery monitoring interfaces
/// </summary>
public class IntegrityMonitoringConfiguration
{
    public required string ConfigurationId { get; set; }
    public required string ConfigurationName { get; set; }
    public required IntegrityMonitoringMode MonitoringMode { get; set; }
    public required TimeSpan MonitoringInterval { get; set; }
    public required List<IntegrityCheck> EnabledChecks { get; set; }
    public required Dictionary<string, AlertThreshold> AlertThresholds { get; set; }
    public required List<string> MonitoredRegions { get; set; }
    public required IntegrityNotificationSettings NotificationSettings { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

/// <summary>
/// Integrity monitoring result for ongoing monitoring operations
/// Critical for continuous data integrity assurance
/// </summary>
public class IntegrityMonitoringResult
{
    public required string MonitoringId { get; set; }
    public required IntegrityMonitoringConfiguration Configuration { get; set; }
    public required IntegrityMonitoringStatus MonitoringStatus { get; set; }
    public required List<IntegrityAlert> IntegrityAlerts { get; set; }
    public required Dictionary<string, IntegrityMetric> MonitoringMetrics { get; set; }
    public required DateTime MonitoringTimestamp { get; set; }
    public required TimeSpan MonitoringPeriod { get; set; }
    public required List<IntegrityIncident> Incidents { get; set; }
    public required double OverallIntegrityScore { get; set; }
    public Dictionary<string, object> MonitoringDetails { get; set; } = new();
    public string? MonitoringSummary { get; set; }
    public DateTime? NextMonitoringScheduled { get; set; }
}

/// <summary>
/// Community data integrity result for cultural community data validation
/// Specialized for LankaConnect's cultural intelligence data protection
/// </summary>
public class CommunityDataIntegrityResult
{
    public required string IntegrityId { get; set; }
    public required string CommunityId { get; set; }
    public required CommunityDataType DataType { get; set; }
    public required CommunityIntegrityStatus IntegrityStatus { get; set; }
    public required List<CommunityDataIssue> DataIssues { get; set; }
    public required Dictionary<string, CulturalDataMetric> CulturalMetrics { get; set; }
    public required DateTime ValidationTimestamp { get; set; }
    public required string ValidatedBy { get; set; }
    public required double CommunityDataScore { get; set; }
    public required List<CommunityDataRecommendation> Recommendations { get; set; }
    public Dictionary<string, object> CommunityContext { get; set; } = new();
    public bool RequiresCulturalReview { get; set; }
    public string? CulturalNotes { get; set; }
}

// Supporting Enums

public enum DataIntegrityValidationType
{
    Full,
    Incremental,
    Spot,
    Cultural,
    Compliance,
    Security
}

public enum DataIntegrityDepth
{
    Basic,
    Standard,
    Comprehensive,
    Deep,
    Exhaustive
}

public enum DataIntegrityStatus
{
    Valid,
    Warning,
    Invalid,
    Corrupted,
    Unknown,
    InProgress
}

public enum BackupVerificationStatus
{
    Verified,
    PartiallyVerified,
    Failed,
    InProgress,
    NotVerified,
    Corrupted
}

public enum ConsistencyCheckType
{
    Referential,
    Transactional,
    Cultural,
    Regional,
    Temporal,
    Business
}

public enum ConsistencyCheckSeverity
{
    Information,
    Warning,
    Error,
    Critical,
    Fatal
}

public enum ConsistencyStatus
{
    Consistent,
    Inconsistent,
    PartiallyConsistent,
    Unknown,
    InProgress
}

public enum IntegrityMonitoringMode
{
    RealTime,
    Scheduled,
    EventDriven,
    Hybrid,
    OnDemand
}

public enum IntegrityMonitoringStatus
{
    Active,
    Inactive,
    Paused,
    Failed,
    Maintenance
}

public enum CommunityDataType
{
    CulturalEvents,
    CommunityProfiles,
    CulturalContent,
    BusinessListings,
    UserInteractions,
    CulturalArtifacts
}

public enum CommunityIntegrityStatus
{
    Verified,
    Pending,
    Compromised,
    UnderReview,
    Approved,
    Rejected
}

// Supporting Complex Types

public class ValidationCriteria
{
    public required string CriteriaName { get; set; }
    public required ValidationRuleType RuleType { get; set; }
    public required Dictionary<string, object> Parameters { get; set; }
    public required bool IsRequired { get; set; }
}

public class DataIntegrityIssue
{
    public required string IssueId { get; set; }
    public required DataIntegrityIssueType IssueType { get; set; }
    public required DataIntegritySeverity Severity { get; set; }
    public required string Description { get; set; }
    public required string AffectedResource { get; set; }
    public DateTime DetectedAt { get; set; }
}

public class ValidationMetric
{
    public required string MetricName { get; set; }
    public required double Value { get; set; }
    public required string Unit { get; set; }
    public required ValidationMetricStatus Status { get; set; }
}

public class DataIntegrityRecommendation
{
    public required string RecommendationId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required RecommendationPriority Priority { get; set; }
    public required List<string> ActionItems { get; set; }
}

public class BackupVerificationCheck
{
    public required string CheckName { get; set; }
    public required BackupCheckType CheckType { get; set; }
    public required BackupCheckStatus Status { get; set; }
    public required string CheckResult { get; set; }
    public DateTime CheckTimestamp { get; set; }
}

public class BackupIntegrityMetrics
{
    public required double IntegrityScore { get; set; }
    public required Dictionary<string, double> ComponentScores { get; set; }
    public required List<string> VerifiedComponents { get; set; }
    public required List<string> FailedComponents { get; set; }
}

public class BackupVerificationIssue
{
    public required string IssueId { get; set; }
    public required BackupIssueType IssueType { get; set; }
    public required BackupIssueSeverity Severity { get; set; }
    public required string Description { get; set; }
    public required string Component { get; set; }
}

public class ConsistencyCheckRule
{
    public required string RuleName { get; set; }
    public required ConsistencyRuleType RuleType { get; set; }
    public required Dictionary<string, object> RuleParameters { get; set; }
    public required bool IsEnabled { get; set; }
}

public class ConsistencyViolation
{
    public required string ViolationId { get; set; }
    public required ConsistencyViolationType ViolationType { get; set; }
    public required ConsistencyViolationSeverity Severity { get; set; }
    public required string Description { get; set; }
    public required List<string> AffectedRecords { get; set; }
}

public class ConsistencyMetric
{
    public required string MetricName { get; set; }
    public required double Value { get; set; }
    public required ConsistencyMetricType MetricType { get; set; }
    public required ConsistencyMetricStatus Status { get; set; }
}

public class ConsistencyRecommendation
{
    public required string RecommendationId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required ConsistencyRecommendationPriority Priority { get; set; }
    public required List<string> ActionItems { get; set; }
}

// Additional supporting enums and classes
public enum ValidationRuleType { Structural, Business, Cultural, Security, Compliance }
public enum DataIntegrityIssueType { Corruption, Missing, Inconsistent, Invalid, Duplicate }
public enum DataIntegritySeverity { Low, Medium, High, Critical }
public enum ValidationMetricStatus { Pass, Fail, Warning, Unknown }
public enum RecommendationPriority { Low, Medium, High, Critical, Immediate }
public enum BackupCheckType { Structural, Content, Metadata, Security, Compliance }
public enum BackupCheckStatus { Pass, Fail, Warning, Skipped }
public enum BackupIssueType { Corruption, Missing, Invalid, Incomplete }
public enum BackupIssueSeverity { Low, Medium, High, Critical }
public enum ConsistencyRuleType { Referential, Business, Cultural, Temporal }
public enum ConsistencyViolationType { ReferentialIntegrity, BusinessRule, CulturalRule, DataFormat }
public enum ConsistencyViolationSeverity { Low, Medium, High, Critical }
public enum ConsistencyMetricType { Count, Percentage, Score, Boolean }
public enum ConsistencyMetricStatus { Normal, Warning, Critical }
public enum ConsistencyRecommendationPriority { Low, Medium, High, Critical }

// Placeholder classes for complex types
public class IntegrityCheck { }
public class AlertThreshold { }
public class IntegrityNotificationSettings { }
public class IntegrityAlert { }
public class IntegrityMetric { }
public class IntegrityIncident { }
public class CommunityDataIssue { }
public class CulturalDataMetric { }
public class CommunityDataRecommendation { }