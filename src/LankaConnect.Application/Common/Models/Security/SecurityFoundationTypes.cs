using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.ValueObjects;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.Security;

namespace LankaConnect.Application.Common.Models.Security;

/// <summary>
/// Tier 2 Security Foundation Types - Critical Security Infrastructure
/// These types provide the core security framework for cultural intelligence operations
/// Implementation targets 42% cumulative error reduction
/// </summary>

/// <summary>
/// Cultural security metrics for monitoring and compliance reporting
/// Essential for Fortune 500 security requirements and audit trails
/// </summary>
public class CulturalSecurityMetrics
{
    public required string MetricsId { get; set; }
    public required string CulturalContextId { get; set; }
    public required Dictionary<string, SecurityMetricValue> SecurityMetrics { get; set; }
    public required SecurityComplianceScore OverallSecurityScore { get; set; }
    public required List<SecurityViolation> DetectedViolations { get; set; }
    public required Dictionary<string, CulturalSecurityIndicator> CulturalIndicators { get; set; }
    public required SecurityTrendAnalysis TrendAnalysis { get; set; }
    public required DateTime MetricsTimestamp { get; set; }
    public required TimeSpan MetricsCollectionPeriod { get; set; }
    public required string CollectionRegion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    public Dictionary<string, object> ExtendedMetrics { get; set; } = new();
}

// SecurityViolation moved to Domain layer: LankaConnect.Domain.Common.Security.SecurityViolation
// This follows Clean Architecture principles where domain entities belong in the Domain layer

/// <summary>
/// Compliance scoring system for security assessment and reporting
/// Provides quantitative measurement of security posture across cultural contexts
/// </summary>
public class ComplianceScore
{
    public required string ScoreId { get; set; }
    public required ComplianceFramework Framework { get; set; }
    public required double OverallScore { get; set; }
    public required Dictionary<string, double> CategoryScores { get; set; }
    public required List<ComplianceGap> IdentifiedGaps { get; set; }
    public required ComplianceAssessmentLevel AssessmentLevel { get; set; }
    public required DateTime AssessmentDate { get; set; }
    public required string AssessedBy { get; set; }
    public required TimeSpan ValidityPeriod { get; set; }
    public required List<string> AssessedRegions { get; set; }
    public required Dictionary<string, CulturalComplianceScore> CulturalScores { get; set; }
    public DateTime NextAssessmentDue { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsValid { get; set; } = true;
    public string? AssessmentNotes { get; set; }
    public List<ComplianceRecommendation> Recommendations { get; set; } = new();
}

/// <summary>
/// Specific compliance requirements for cultural intelligence platform
/// Maps to regulatory frameworks and internal security policies
/// </summary>
public class ComplianceRequirement
{
    public required string RequirementId { get; set; }
    public required string RequirementName { get; set; }
    public required ComplianceFramework Framework { get; set; }
    public required string RequirementSection { get; set; }
    public required string Description { get; set; }
    public required ComplianceMandatory IsMandatory { get; set; }
    public required ComplianceRequirementType RequirementType { get; set; }
    public required List<string> ApplicableRegions { get; set; }
    public required List<CulturalEventType> ApplicableCulturalContexts { get; set; }
    public required ComplianceImplementationStatus ImplementationStatus { get; set; }
    public required DateTime EffectiveDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public required List<ComplianceControl> Controls { get; set; }
    public required ComplianceValidationCriteria ValidationCriteria { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ImplementationNotes { get; set; }
    public List<string> Dependencies { get; set; } = new();
}

/// <summary>
/// SOC2 Trust Services Criteria compliance framework
/// Essential for enterprise customer requirements and audit compliance
/// </summary>
public class SOC2TrustServicesCriteria
{
    public required string CriteriaId { get; set; }
    public required SOC2TrustService TrustService { get; set; }
    public required string CriteriaNumber { get; set; }
    public required string CriteriaDescription { get; set; }
    public required SOC2ComplianceLevel ComplianceLevel { get; set; }
    public required List<SOC2Control> ImplementedControls { get; set; }
    public required SOC2EvidencePackage Evidence { get; set; }
    public required DateTime LastAuditDate { get; set; }
    public required string AuditorFirm { get; set; }
    public required SOC2AuditOpinion AuditOpinion { get; set; }
    public DateTime NextAuditDate { get; set; }
    public required List<string> ResponsiblePersonnel { get; set; }
    public required Dictionary<string, CulturalSOC2Adaptation> CulturalAdaptations { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsValid { get; set; } = true;
    public string? AuditNotes { get; set; }
    public List<SOC2Exception> Exceptions { get; set; } = new();
    public required SOC2ReportingPeriod ReportingPeriod { get; set; }
}

// Supporting Security Enums

public enum SecurityViolationType
{
    DataBreach,
    UnauthorizedAccess,
    PolicyViolation,
    CulturalSensitivityBreach,
    EncryptionFailure,
    AccessControlBypass,
    AuditLogTampering,
    ComplianceViolation
}

public enum SecuritySeverityLevel
{
    Informational,
    Low,
    Medium,
    High,
    Critical,
    Emergency
}

public enum SecurityViolationStatus
{
    Open,
    InProgress,
    UnderReview,
    Resolved,
    Closed,
    Escalated
}

public enum ComplianceFramework
{
    SOC2,
    ISO27001,
    GDPR,
    CCPA,
    HIPAA,
    PCI_DSS,
    CulturalDataProtection,
    EnterpriseCustom
}

public enum ComplianceAssessmentLevel
{
    Basic,
    Standard,
    Comprehensive,
    Enterprise,
    Regulatory
}

public enum ComplianceMandatory
{
    Required,
    Recommended,
    Optional,
    ConditionallyRequired
}

public enum ComplianceRequirementType
{
    Technical,
    Administrative,
    Physical,
    Cultural,
    Hybrid
}

public enum ComplianceImplementationStatus
{
    NotStarted,
    InProgress,
    Implemented,
    Verified,
    NonCompliant,
    PartiallyImplemented
}

public enum SOC2TrustService
{
    Security,
    Availability,
    ProcessingIntegrity,
    Confidentiality,
    PrivacyProtection
}

public enum SOC2ComplianceLevel
{
    FullyCompliant,
    SubstantiallyCompliant,
    PartiallyCompliant,
    NonCompliant,
    NotApplicable
}

public enum SOC2AuditOpinion
{
    Unqualified,
    Qualified,
    Adverse,
    Disclaimer,
    InProgress
}

// Supporting Complex Security Types

public class SecurityMetricValue
{
    public required string MetricName { get; set; }
    public required double Value { get; set; }
    public required string Unit { get; set; }
    public required DateTime MeasuredAt { get; set; }
    public SecurityMetricStatus Status { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class SecurityComplianceScore
{
    public required double Score { get; set; }
    public required double MaximumPossibleScore { get; set; }
    public required string ScaleType { get; set; }
    public required ComplianceFramework Framework { get; set; }
    public DateTime CalculatedAt { get; set; }
}

public class CulturalSecurityIndicator
{
    public required string IndicatorName { get; set; }
    public required CulturalEventType CulturalContext { get; set; }
    public required SecurityIndicatorValue Value { get; set; }
    public required SecurityIndicatorTrend Trend { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class SecurityTrendAnalysis
{
    public required Dictionary<string, SecurityTrend> TrendData { get; set; }
    public required TimeSpan AnalysisPeriod { get; set; }
    public required List<SecurityTrendPrediction> Predictions { get; set; }
    public DateTime AnalysisTimestamp { get; set; }
}

public class SecurityViolationEvidence
{
    public required string EvidenceId { get; set; }
    public required SecurityEvidenceType EvidenceType { get; set; }
    public required string Description { get; set; }
    public required DateTime CollectedAt { get; set; }
    public required string CollectedBy { get; set; }
    public byte[]? EvidenceData { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class SecurityRemediationPlan
{
    public required string PlanId { get; set; }
    public required List<SecurityRemediationStep> Steps { get; set; }
    public required DateTime PlannedStartDate { get; set; }
    public required DateTime PlannedEndDate { get; set; }
    public required string ResponsibleParty { get; set; }
    public SecurityRemediationStatus Status { get; set; }
}

public class ComplianceGap
{
    public required string GapId { get; set; }
    public required string GapDescription { get; set; }
    public required ComplianceRequirement Requirement { get; set; }
    public required ComplianceGapSeverity Severity { get; set; }
    public required DateTime IdentifiedAt { get; set; }
    public DateTime? TargetResolutionDate { get; set; }
}

public class CulturalComplianceScore
{
    public required CulturalEventType CulturalContext { get; set; }
    public required double Score { get; set; }
    public required List<string> SpecificRequirements { get; set; }
    public required ComplianceStatus Status { get; set; }
}

public class ComplianceRecommendation
{
    public required string RecommendationId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required CompliancePriority Priority { get; set; }
    public required List<string> ActionItems { get; set; }
    public required TimeSpan EstimatedImplementationTime { get; set; }
}

public class ComplianceControl
{
    public required string ControlId { get; set; }
    public required string ControlDescription { get; set; }
    public required ComplianceControlType ControlType { get; set; }
    public required ComplianceControlStatus Status { get; set; }
    public required DateTime LastTested { get; set; }
    public DateTime NextTestDate { get; set; }
}

public class ComplianceValidationCriteria
{
    public required List<string> ValidationPoints { get; set; }
    public required Dictionary<string, string> AcceptanceCriteria { get; set; }
    public required ComplianceValidationFrequency ValidationFrequency { get; set; }
    public required string ValidationMethod { get; set; }
}

public class SOC2Control
{
    public required string ControlId { get; set; }
    public required string ControlDescription { get; set; }
    public required SOC2ControlType ControlType { get; set; }
    public required SOC2ControlFrequency TestingFrequency { get; set; }
    public required DateTime LastTested { get; set; }
    public SOC2ControlStatus Status { get; set; }
}

public class SOC2EvidencePackage
{
    public required List<SOC2Evidence> Evidence { get; set; }
    public required DateTime PackageDate { get; set; }
    public required string PreparedBy { get; set; }
    public required SOC2EvidenceCompleteness Completeness { get; set; }
}

public class CulturalSOC2Adaptation
{
    public required CulturalEventType CulturalContext { get; set; }
    public required string AdaptationDescription { get; set; }
    public required List<string> SpecificControls { get; set; }
    public required string Justification { get; set; }
}

public class SOC2Exception
{
    public required string ExceptionId { get; set; }
    public required string ExceptionDescription { get; set; }
    public required SOC2ExceptionType ExceptionType { get; set; }
    public required DateTime IdentifiedDate { get; set; }
    public DateTime? ResolvedDate { get; set; }
}

public class SOC2ReportingPeriod
{
    public required DateTime StartDate { get; set; }
    public required DateTime EndDate { get; set; }
    public required SOC2ReportType ReportType { get; set; }
    public required string AuditFirm { get; set; }
}

// Additional supporting enums
public enum SecurityMetricStatus { Normal, Warning, Critical, Unknown }
public enum SecurityIndicatorValue { Low, Medium, High, Critical }
public enum SecurityIndicatorTrend { Improving, Stable, Degrading, Unknown }
public enum SecurityEvidenceType { Log, Screenshot, Configuration, Document, Recording }
public enum SecurityRemediationStatus { Planned, InProgress, Completed, Verified, Failed }
public enum ComplianceGapSeverity { Low, Medium, High, Critical }
public enum ComplianceStatus { Compliant, NonCompliant, PartiallyCompliant, UnderReview }
public enum CompliancePriority { Low, Medium, High, Critical, Immediate }
public enum ComplianceControlType { Preventive, Detective, Corrective, Administrative }
public enum ComplianceControlStatus { Implemented, PartiallyImplemented, NotImplemented, Disabled }
public enum ComplianceValidationFrequency { Daily, Weekly, Monthly, Quarterly, Annually, EventBased }
public enum SOC2ControlType { Manual, Automated, Hybrid }
public enum SOC2ControlFrequency { Daily, Weekly, Monthly, Quarterly, Annually }
public enum SOC2ControlStatus { Effective, Ineffective, NotTested, Exception }
public enum SOC2EvidenceCompleteness { Complete, Incomplete, UnderReview }
public enum SOC2ExceptionType { Design, OperatingEffectiveness, Scope }
public enum SOC2ReportType { TypeI, TypeII }

// Placeholder classes for future implementation
public class SecurityTrend { }
public class SecurityTrendPrediction { }
public class SecurityRemediationStep { }
public class SOC2Evidence { }