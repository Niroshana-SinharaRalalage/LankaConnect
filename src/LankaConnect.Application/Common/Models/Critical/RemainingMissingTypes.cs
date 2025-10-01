using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.ValueObjects;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.Models.Critical;

/// <summary>
/// Remaining missing types from the exact compilation error list
/// Implementing the rest of the critical types identified in error analysis
/// </summary>

/// <summary>
/// Adjustment strategy for dynamic system adjustments
/// Referenced in performance optimization and scaling operations
/// </summary>
public class AdjustmentStrategy
{
    public required string StrategyId { get; set; }
    public required string StrategyName { get; set; }
    public required AdjustmentStrategyType StrategyType { get; set; }
    public required List<AdjustmentRule> AdjustmentRules { get; set; }
    public required Dictionary<string, AdjustmentParameter> Parameters { get; set; }
    public required AdjustmentTrigger Trigger { get; set; }
    public required TimeSpan AdjustmentInterval { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

/// <summary>
/// API access control result for API security management
/// Critical for securing cultural intelligence API endpoints
/// </summary>
public class APIAccessControlResult
{
    public required string AccessControlId { get; set; }
    public required APIAccessRequest AccessRequest { get; set; }
    public required APIAccessStatus AccessStatus { get; set; }
    public required List<APIPermission> GrantedPermissions { get; set; }
    public required APIAccessDecision Decision { get; set; }
    public required DateTime AccessTimestamp { get; set; }
    public required string RequestedBy { get; set; }
    public required APISecurityContext SecurityContext { get; set; }
    public List<APIAccessViolation> AccessViolations { get; set; } = new();
    public Dictionary<string, object> AccessMetrics { get; set; } = new();
    public string? AccessNotes { get; set; }
    public DateTime? AccessExpirationTime { get; set; }
}

/// <summary>
/// API access request for requesting API access permissions
/// Referenced in security optimization interfaces
/// </summary>
public class APIAccessRequest
{
    public required string RequestId { get; set; }
    public required string RequesterId { get; set; }
    public required string RequesterName { get; set; }
    public required List<string> RequestedEndpoints { get; set; }
    public required List<APIPermissionType> RequestedPermissions { get; set; }
    public required APIAccessPurpose Purpose { get; set; }
    public required DateTime RequestTimestamp { get; set; }
    public required TimeSpan RequestedDuration { get; set; }
    public required APIAccessJustification Justification { get; set; }
    public APIAccessRequestStatus Status { get; set; } = APIAccessRequestStatus.Pending;
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public string? RejectionReason { get; set; }
    public Dictionary<string, object> RequestContext { get; set; } = new();
}

/// <summary>
/// Business priority matrix for prioritizing business operations
/// Referenced in disaster recovery and optimization operations
/// </summary>
public class BusinessPriorityMatrix
{
    public required string MatrixId { get; set; }
    public required string MatrixName { get; set; }
    public required Dictionary<string, BusinessPriorityLevel> ServicePriorities { get; set; }
    public required List<BusinessPriorityRule> PriorityRules { get; set; }
    public required BusinessPriorityStrategy Strategy { get; set; }
    public required Dictionary<string, CulturalEventPriority> CulturalEventPriorities { get; set; }
    public required BusinessImpactAssessment ImpactAssessment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

/// <summary>
/// Cross-region coordination strategy for multi-region operations
/// Critical for distributed cultural intelligence coordination
/// </summary>
public class CrossRegionCoordinationStrategy
{
    public required string StrategyId { get; set; }
    public required string StrategyName { get; set; }
    public required CrossRegionCoordinationType CoordinationType { get; set; }
    public required List<CoordinationRegion> ParticipatingRegions { get; set; }
    public required Dictionary<string, CoordinationProtocol> CoordinationProtocols { get; set; }
    public required CoordinationFailoverStrategy FailoverStrategy { get; set; }
    public required TimeSpan CoordinationTimeout { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

/// <summary>
/// Cultural API policy for cultural intelligence API governance
/// Referenced in security optimization interfaces
/// </summary>
public class CulturalAPIPolicy
{
    public required string PolicyId { get; set; }
    public required string PolicyName { get; set; }
    public required List<CulturalEventType> ApplicableCulturalEvents { get; set; }
    public required Dictionary<string, APIAccessRule> AccessRules { get; set; }
    public required CulturalSensitivityLevel SensitivityLevel { get; set; }
    public required List<CulturalAPIRestriction> Restrictions { get; set; }
    public required CulturalDataProtectionPolicy DataProtectionPolicy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

/// <summary>
/// Cultural authentication context for culture-aware authentication
/// Referenced in security optimization interfaces
/// </summary>
public class CulturalAuthenticationContext
{
    public required string ContextId { get; set; }
    public required string UserId { get; set; }
    public required List<CulturalEventType> UserCulturalProfile { get; set; }
    public required Dictionary<string, object> CulturalAttributes { get; set; }
    public required CulturalAuthenticationLevel AuthenticationLevel { get; set; }
    public required List<CulturalSecurityFactor> SecurityFactors { get; set; }
    public required DateTime AuthenticationTimestamp { get; set; }
    public required string AuthenticationRegion { get; set; }
    public CulturalAuthenticationStatus Status { get; set; } = CulturalAuthenticationStatus.Active;
    public Dictionary<string, object> ContextMetadata { get; set; } = new();
    public DateTime? ExpirationTime { get; set; }
    public string? ContextNotes { get; set; }
}

// CulturalEventImportanceMatrix moved to LankaConnect.Application.Common.Models.Performance.CulturalEventImportanceMatrix to resolve CS0104 conflict

/// <summary>
/// Cultural resource policy for managing cultural resources
/// Referenced in security optimization interfaces
/// </summary>
public class CulturalResourcePolicy
{
    public required string PolicyId { get; set; }
    public required string PolicyName { get; set; }
    public required List<CulturalResourceType> CoveredResourceTypes { get; set; }
    public required Dictionary<string, CulturalAccessRule> AccessRules { get; set; }
    public required CulturalResourceProtectionLevel ProtectionLevel { get; set; }
    public required List<CulturalResourceRestriction> Restrictions { get; set; }
    public required CulturalResourceUsagePolicy UsagePolicy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

/// <summary>
/// Cultural session policy for managing cultural sessions
/// Referenced in security optimization interfaces
/// </summary>
public class CulturalSessionPolicy
{
    public required string PolicyId { get; set; }
    public required string PolicyName { get; set; }
    public required Dictionary<CulturalEventType, CulturalSessionRule> SessionRules { get; set; }
    public required CulturalSessionSecurityLevel SecurityLevel { get; set; }
    public required TimeSpan MaxSessionDuration { get; set; }
    public required List<CulturalSessionRestriction> Restrictions { get; set; }
    public required CulturalSessionMonitoringPolicy MonitoringPolicy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

/// <summary>
/// JIT (Just-In-Time) access request for temporary privileged access
/// Referenced in security optimization interfaces
/// </summary>
public class JITAccessRequest
{
    public required string RequestId { get; set; }
    public required string RequesterId { get; set; }
    public required string RequesterName { get; set; }
    public required List<string> RequestedResources { get; set; }
    public required List<JITPermissionType> RequestedPermissions { get; set; }
    public required JITAccessJustification Justification { get; set; }
    public required TimeSpan RequestedDuration { get; set; }
    public required DateTime RequestTimestamp { get; set; }
    public JITAccessRequestStatus Status { get; set; } = JITAccessRequestStatus.Pending;
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? AccessGrantedAt { get; set; }
    public DateTime? AccessRevokedAt { get; set; }
    public string? RequestNotes { get; set; }
    public Dictionary<string, object> RequestContext { get; set; } = new();
}

/// <summary>
/// MFA (Multi-Factor Authentication) configuration
/// Referenced in security optimization interfaces
/// </summary>
public class MFAConfiguration
{
    public required string ConfigurationId { get; set; }
    public required string ConfigurationName { get; set; }
    public required List<MFAFactorType> RequiredFactors { get; set; }
    public required Dictionary<string, MFAFactorConfiguration> FactorConfigurations { get; set; }
    public required MFAPolicy Policy { get; set; }
    public required MFAFallbackStrategy FallbackStrategy { get; set; }
    public required TimeSpan FactorValidityDuration { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

/// <summary>
/// MFA result for multi-factor authentication outcomes
/// Referenced in security optimization interfaces
/// </summary>
public class MFAResult
{
    public required string ResultId { get; set; }
    public required MFAConfiguration Configuration { get; set; }
    public required MFAStatus AuthenticationStatus { get; set; }
    public required List<MFAFactorResult> FactorResults { get; set; }
    public required DateTime AuthenticationTimestamp { get; set; }
    public required string AuthenticatedUser { get; set; }
    public required MFASecurityContext SecurityContext { get; set; }
    public List<MFASecurityAlert> SecurityAlerts { get; set; } = new();
    public Dictionary<string, object> AuthenticationMetrics { get; set; } = new();
    public DateTime? TokenExpirationTime { get; set; }
    public string? AuthenticationNotes { get; set; }
}

// Supporting Enums

public enum AdjustmentStrategyType
{
    Reactive,
    Proactive,
    Predictive,
    Adaptive,
    Manual
}

public enum APIAccessStatus
{
    Granted,
    Denied,
    Pending,
    Expired,
    Suspended,
    Revoked
}

public enum APIAccessDecision
{
    Allow,
    Deny,
    Conditional,
    Escalate,
    Monitor
}

public enum APIPermissionType
{
    Read,
    Write,
    Delete,
    Execute,
    Admin,
    Cultural
}

public enum APIAccessPurpose
{
    Development,
    Testing,
    Production,
    Analytics,
    Integration,
    Cultural
}

public enum APIAccessRequestStatus
{
    Pending,
    Approved,
    Rejected,
    Expired,
    Cancelled
}

public enum BusinessPriorityLevel
{
    Critical,
    High,
    Medium,
    Low,
    Deferred
}

public enum BusinessPriorityStrategy
{
    Impact,
    Cultural,
    Financial,
    Regulatory,
    Hybrid
}

public enum CrossRegionCoordinationType
{
    Synchronous,
    Asynchronous,
    EventDriven,
    Hybrid,
    Cultural
}

public enum CoordinationFailoverStrategy
{
    Primary,
    RoundRobin,
    Priority,
    Cultural,
    Adaptive
}

public enum CulturalSensitivityLevel
{
    Public,
    Sensitive,
    Restricted,
    Sacred,
    Confidential
}

public enum CulturalAuthenticationLevel
{
    Basic,
    Standard,
    Enhanced,
    Cultural,
    Sacred
}

public enum CulturalAuthenticationStatus
{
    Active,
    Inactive,
    Suspended,
    Expired,
    Compromised
}

public enum CulturalResourceType
{
    Content,
    Events,
    Artifacts,
    Ceremonies,
    Traditions
}

public enum CulturalResourceProtectionLevel
{
    Public,
    Community,
    Restricted,
    Sacred,
    Forbidden
}

public enum CulturalSessionSecurityLevel
{
    Standard,
    Enhanced,
    Cultural,
    Sacred,
    Maximum
}

public enum JITPermissionType
{
    Read,
    Write,
    Admin,
    Emergency,
    Cultural
}

public enum JITAccessRequestStatus
{
    Pending,
    Approved,
    Rejected,
    Active,
    Expired,
    Revoked
}

public enum MFAFactorType
{
    Password,
    SMS,
    Email,
    TOTP,
    Biometric,
    Hardware
}

public enum MFAStatus
{
    Success,
    Failure,
    Partial,
    Timeout,
    Error
}

// Supporting Complex Types (Placeholders for future implementation)
public class AdjustmentRule { }
public class AdjustmentParameter { }
public class AdjustmentTrigger { }
public class APIPermission { }
public class APISecurityContext { }
public class APIAccessViolation { }
public class APIAccessJustification { }
public class BusinessPriorityRule { }
// CulturalEventPriority removed - defined as enum in AdditionalMissingTypes.cs
public class BusinessImpactAssessment { }
public class CoordinationRegion { }
public class CoordinationProtocol { }
public class APIAccessRule { }
public class CulturalAPIRestriction { }
public class CulturalDataProtectionPolicy { }
public class CulturalSecurityFactor { }
public class CulturalImportanceScore { }
public class CulturalImportanceRule { }
public class CulturalImportanceStrategy { }
public class RegionalImportanceWeight { }
public class CulturalContextFactors { }
public class CulturalAccessRule { }
public class CulturalResourceRestriction { }
public class CulturalResourceUsagePolicy { }
public class CulturalSessionRule { }
public class CulturalSessionRestriction { }
public class CulturalSessionMonitoringPolicy { }
public class JITAccessJustification { }
public class MFAFactorConfiguration { }
public class MFAPolicy { }
public class MFAFallbackStrategy { }
public class MFAFactorResult { }
public class MFASecurityContext { }
public class MFASecurityAlert { }