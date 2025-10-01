using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.ValueObjects;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.Models.Critical;

/// <summary>
/// Exact missing types identified from compilation error analysis
/// These are the specific types causing immediate compilation failures
/// Targeted implementation for maximum error reduction efficiency
/// </summary>

/// <summary>
/// Checksum algorithm configuration for backup verification
/// Referenced in backup and disaster recovery interfaces
/// </summary>
public class ChecksumAlgorithm
{
    public required string AlgorithmId { get; set; }
    public required string AlgorithmName { get; set; }
    public required ChecksumAlgorithmType AlgorithmType { get; set; }
    public required ChecksumStrength Strength { get; set; }
    public required Dictionary<string, object> AlgorithmConfiguration { get; set; }
    public required bool IsSecure { get; set; }
    public required TimeSpan ComputationTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? Description { get; set; }
}

/// <summary>
/// Checksum validation result for backup integrity verification
/// Critical for ensuring backup data integrity and recoverability
/// </summary>
public class ChecksumValidationResult
{
    public required string ValidationId { get; set; }
    public required ChecksumAlgorithm Algorithm { get; set; }
    public required ChecksumValidationStatus ValidationStatus { get; set; }
    public required string ExpectedChecksum { get; set; }
    public required string ActualChecksum { get; set; }
    public required bool IsValid { get; set; }
    public required DateTime ValidationTimestamp { get; set; }
    public required string ValidatedResource { get; set; }
    public required ChecksumValidationMetrics Metrics { get; set; }
    public List<ChecksumValidationIssue> Issues { get; set; } = new();
    public Dictionary<string, object> ValidationContext { get; set; } = new();
    public string? ValidationNotes { get; set; }
}


/// <summary>
/// Restore point integrity result for backup restoration validation
/// Essential for ensuring restore points are valid and complete
/// </summary>
public class RestorePointIntegrityResult
{
    public required string RestorePointId { get; set; }
    public required string IntegrityCheckId { get; set; }
    public required RestorePointIntegrityStatus IntegrityStatus { get; set; }
    public required List<RestorePointComponent> ValidatedComponents { get; set; }
    public required Dictionary<string, RestorePointMetric> IntegrityMetrics { get; set; }
    public required DateTime ValidationTimestamp { get; set; }
    public required TimeSpan ValidationDuration { get; set; }
    public required string ValidatedBy { get; set; }
    public required double IntegrityScore { get; set; }
    public List<RestorePointIssue> Issues { get; set; } = new();
    public Dictionary<string, object> ValidationDetails { get; set; } = new();
    public bool IsRestoreReady { get; set; }
    public string? ValidationSummary { get; set; }
}




/// <summary>
/// Corruption detection result for data integrity monitoring
/// Critical for identifying and reporting data corruption issues
/// </summary>
public class CorruptionDetectionResult
{
    public required string DetectionId { get; set; }
    public required CorruptionDetectionScope Scope { get; set; }
    public required CorruptionDetectionStatus DetectionStatus { get; set; }
    public required List<CorruptionIncident> DetectedCorruption { get; set; }
    public required Dictionary<string, CorruptionMetric> DetectionMetrics { get; set; }
    public required DateTime DetectionTimestamp { get; set; }
    public required TimeSpan DetectionDuration { get; set; }
    public required string DetectedBy { get; set; }
    public required CorruptionSeverityAssessment SeverityAssessment { get; set; }
    public List<CorruptionRecommendation> Recommendations { get; set; } = new();
    public Dictionary<string, object> DetectionContext { get; set; } = new();
    public bool RequiresImmediateAction { get; set; }
    public string? DetectionSummary { get; set; }
}


/// <summary>
/// Lineage validation criteria for data lineage verification
/// Referenced in backup and data integrity validation
/// </summary>
public class LineageValidationCriteria
{
    public required string CriteriaId { get; set; }
    public required string CriteriaName { get; set; }
    public required List<LineageValidationRule> ValidationRules { get; set; }
    public required Dictionary<string, LineageValidationThreshold> Thresholds { get; set; }
    public required LineageValidationComplexity Complexity { get; set; }
    public required LineageValidationAccuracy AccuracyRequirement { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? Description { get; set; }
}

/// <summary>
/// Data lineage validation result for data flow verification
/// Critical for ensuring data integrity across processing pipelines
/// </summary>
public class DataLineageValidationResult
{
    public required string ValidationId { get; set; }
    public required DataLineageScope Scope { get; set; }
    public required LineageValidationStatus ValidationStatus { get; set; }
    public required List<DataLineageViolation> LineageViolations { get; set; }
    public required Dictionary<string, LineageValidationMetric> ValidationMetrics { get; set; }
    public required DateTime ValidationTimestamp { get; set; }
    public required TimeSpan ValidationDuration { get; set; }
    public required string ValidatedBy { get; set; }
    public required DataLineageCompleteness Completeness { get; set; }
    public List<DataLineageRecommendation> Recommendations { get; set; } = new();
    public Dictionary<string, object> LineageContext { get; set; } = new();
    public bool IsLineageIntact { get; set; }
    public string? ValidationSummary { get; set; }
}

// Supporting Enums

public enum ChecksumAlgorithmType
{
    MD5,
    SHA1,
    SHA256,
    SHA512,
    CRC32,
    Blake2,
    Custom
}

public enum ChecksumStrength
{
    Low,
    Medium,
    High,
    Cryptographic,
    Military
}

public enum ChecksumValidationStatus
{
    Valid,
    Invalid,
    Corrupted,
    Missing,
    Unknown,
    InProgress
}

public enum IntegrityDepthLevel
{
    Surface,
    Standard,
    Deep,
    Comprehensive,
    Exhaustive
}

public enum IntegrityValidationType
{
    Structural,
    Content,
    Metadata,
    Relationships,
    Business,
    Cultural
}

public enum IntegrityValidationPriority
{
    Low,
    Medium,
    High,
    Critical,
    Emergency
}

public enum RestorePointIntegrityStatus
{
    Valid,
    Invalid,
    Corrupted,
    Incomplete,
    Unknown,
    Validating
}

public enum ValidationModeType
{
    Fast,
    Standard,
    Thorough,
    Custom,
    Cultural
}

public enum ValidationApproach
{
    Automated,
    Manual,
    Hybrid,
    AIAssisted
}

public enum CorruptionDetectionType
{
    Passive,
    Active,
    RealTime,
    Scheduled,
    EventDriven
}

public enum CorruptionSensitivityLevel
{
    Low,
    Medium,
    High,
    Maximum,
    Custom
}

public enum DetectionSensitivityLevel
{
    VeryLow,
    Low,
    Medium,
    High,
    VeryHigh,
    Maximum
}

public enum CorruptionDetectionStatus
{
    Clean,
    Suspicious,
    Corrupted,
    Critical,
    Unknown,
    Scanning
}

public enum DataLineageType
{
    Forward,
    Backward,
    Bidirectional,
    Impact,
    Full
}

public enum DataLineageDepth
{
    Immediate,
    OneLevel,
    MultiLevel,
    Full,
    Custom
}

public enum LineageValidationComplexity
{
    Simple,
    Standard,
    Complex,
    Advanced,
    Enterprise
}

public enum LineageValidationAccuracy
{
    Basic,
    Standard,
    High,
    Precise,
    Perfect
}

public enum LineageValidationStatus
{
    Valid,
    Invalid,
    Incomplete,
    Suspicious,
    Unknown,
    Validating
}

public enum DataLineageCompleteness
{
    Complete,
    Partial,
    Incomplete,
    Missing,
    Unknown
}

// Supporting Complex Types

public class ChecksumValidationMetrics
{
    public required TimeSpan ValidationDuration { get; set; }
    public required long DataSize { get; set; }
    public required double ValidationSpeed { get; set; }
    public required int ValidationAttempts { get; set; }
}

public class ChecksumValidationIssue
{
    public required string IssueId { get; set; }
    public required ChecksumIssueType IssueType { get; set; }
    public required string Description { get; set; }
    public required ChecksumIssueSeverity Severity { get; set; }
}

public class IntegrityCheckConfiguration
{
    public required string ConfigurationName { get; set; }
    public required Dictionary<string, object> Parameters { get; set; }
    public required bool IsEnabled { get; set; }
    public required TimeSpan MaxExecutionTime { get; set; }
}

public class RestorePointComponent
{
    public required string ComponentId { get; set; }
    public required string ComponentName { get; set; }
    public required ComponentType ComponentType { get; set; }
    public required ComponentStatus Status { get; set; }
}

public class RestorePointMetric
{
    public required string MetricName { get; set; }
    public required double Value { get; set; }
    public required RestorePointMetricStatus Status { get; set; }
}

public class RestorePointIssue
{
    public required string IssueId { get; set; }
    public required RestorePointIssueType IssueType { get; set; }
    public required string Description { get; set; }
    public required RestorePointIssueSeverity Severity { get; set; }
}

public class ValidationParameter
{
    public required string ParameterName { get; set; }
    public required object ParameterValue { get; set; }
    public required ValidationParameterType ParameterType { get; set; }
    public required bool IsRequired { get; set; }
}

public class ValidationPerformanceProfile
{
    public required string ProfileName { get; set; }
    public required TimeSpan ExpectedDuration { get; set; }
    public required ValidationPerformanceImpact Impact { get; set; }
    public required Dictionary<string, double> ResourceRequirements { get; set; }
}

// Additional supporting enums and placeholder classes
public enum ChecksumIssueType { Mismatch, Missing, Corrupted, Invalid }
public enum ChecksumIssueSeverity { Low, Medium, High, Critical }
public enum ComponentType { Data, Metadata, Configuration, Index, Log }
public enum ComponentStatus { Valid, Invalid, Missing, Corrupted, Unknown }
public enum RestorePointMetricStatus { Normal, Warning, Critical }
public enum RestorePointIssueType { Missing, Corrupted, Invalid, Incomplete }
public enum RestorePointIssueSeverity { Low, Medium, High, Critical }
public enum ValidationParameterType { String, Number, Boolean, Object, Array }
public enum ValidationPerformanceImpact { Minimal, Low, Medium, High, Severe }

// Placeholder classes for complex types
public class CorruptionDetectionCriteria { }
public class DetectionAccuracy { }
public class DetectionPerformance { }
public class DetectionRule { }
public class CorruptionIncident { }
public class CorruptionMetric { }
public class CorruptionSeverityAssessment { }
public class CorruptionRecommendation { }
public class DataLineageSource { }
public class DataLineageRule { }
public class LineageValidationRule { }
public class LineageValidationThreshold { }
public class DataLineageViolation { }
public class LineageValidationMetric { }
public class DataLineageRecommendation { }