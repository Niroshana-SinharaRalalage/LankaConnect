using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.ValueObjects;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Application.Common.Models.Configuration;
using LankaConnect.Application.Common.Models.Security;

namespace LankaConnect.Application.Common.Models.Results;

/// <summary>
/// Tier 3 High-Impact Result Types - Critical Operation Results
/// These result types are extensively referenced throughout interfaces and handlers
/// Implementation targets major compilation error reduction breakthrough
/// </summary>

/// <summary>
/// Machine Learning threat detection result for cultural intelligence security
/// Critical for enterprise security monitoring and SOC2 compliance
/// </summary>
public class MLThreatDetectionResult
{
    public required string DetectionId { get; set; }
    public required string ThreatId { get; set; }
    public required MLThreatType ThreatType { get; set; }
    public required double ConfidenceLevel { get; set; }
    public required MLThreatSeverity Severity { get; set; }
    public required CulturalContext AffectedCulturalContext { get; set; }
    public required List<MLThreatIndicator> ThreatIndicators { get; set; }
    public required MLDetectionMethod DetectionMethod { get; set; }
    public required DateTime DetectedAt { get; set; }
    public required string DetectedBy { get; set; }
    public required MLThreatAnalysis Analysis { get; set; }
    public required List<MLRecommendedAction> RecommendedActions { get; set; }
    public required Dictionary<string, object> DetectionMetrics { get; set; }
    public MLThreatStatus Status { get; set; } = MLThreatStatus.Open;
    public DateTime? MitigatedAt { get; set; }
    public string? MitigationNotes { get; set; }
    public List<string> AffectedResources { get; set; } = new();
    public Dictionary<string, string> CustomAttributes { get; set; } = new();
    public bool RequiresImmediateAction { get; set; }
    public TimeSpan EstimatedResolutionTime { get; set; }
}

/// <summary>
/// Security resource optimization result for database and infrastructure security
/// Essential for maintaining security posture while optimizing performance
/// </summary>
public class SecurityResourceOptimizationResult
{
    public required string OptimizationId { get; set; }
    public required SecurityOptimizationType OptimizationType { get; set; }
    public required List<SecurityResource> OptimizedResources { get; set; }
    public required SecurityOptimizationMetrics OptimizationMetrics { get; set; }
    public required SecurityPostureImpact SecurityPostureImpact { get; set; }
    public required PerformanceImpactAssessment PerformanceImpact { get; set; }
    public required List<SecurityOptimizationRecommendation> Recommendations { get; set; }
    public required DateTime OptimizationTimestamp { get; set; }
    public required TimeSpan OptimizationDuration { get; set; }
    public required string OptimizedBy { get; set; }
    public required Dictionary<string, CulturalSecurityOptimization> CulturalOptimizations { get; set; }
    public SecurityOptimizationStatus Status { get; set; } = SecurityOptimizationStatus.InProgress;
    public DateTime? CompletedAt { get; set; }
    public required bool IsRollbackSupported { get; set; }
    public string? RollbackProcedure { get; set; }
    public List<SecurityOptimizationWarning> Warnings { get; set; } = new();
    public Dictionary<string, double> ResourceUtilizationBefore { get; set; } = new();
    public Dictionary<string, double> ResourceUtilizationAfter { get; set; } = new();
}

/// <summary>
/// Security load balancing result for distributed cultural intelligence operations
/// Critical for maintaining security across multiple regions and cultural contexts
/// </summary>
public class SecurityLoadBalancingResult
{
    public required string LoadBalancingId { get; set; }
    public required SecurityLoadBalancingStrategy Strategy { get; set; }
    public required List<SecurityEndpoint> SecurityEndpoints { get; set; }
    public required Dictionary<string, SecurityLoadMetrics> LoadDistribution { get; set; }
    public required SecurityFailoverConfiguration FailoverConfiguration { get; set; }
    public required List<CulturalSecurityZone> CulturalZones { get; set; }
    public required SecurityLoadBalancingHealthCheck HealthCheck { get; set; }
    public required DateTime LoadBalancingTimestamp { get; set; }
    public required TimeSpan LoadBalancingDuration { get; set; }
    public required string LoadBalancedBy { get; set; }
    public SecurityLoadBalancingStatus Status { get; set; } = SecurityLoadBalancingStatus.Active;
    public required Dictionary<string, SecurityPerformanceMetric> PerformanceMetrics { get; set; }
    public List<SecurityLoadBalancingAlert> Alerts { get; set; } = new();
    public required bool IsHighAvailabilityEnabled { get; set; }
    public required SecurityRedundancyLevel RedundancyLevel { get; set; }
    public Dictionary<string, object> LoadBalancingConfiguration { get; set; } = new();
    public List<string> AffectedCulturalGroups { get; set; } = new();
}

/// <summary>
/// Disaster recovery security result for comprehensive backup and recovery operations
/// Essential for business continuity and Fortune 500 compliance requirements
/// </summary>
public class DisasterRecoverySecurityResult
{
    public required string DisasterRecoveryId { get; set; }
    public required DisasterRecoveryType RecoveryType { get; set; }
    public required DisasterRecoveryTrigger TriggerEvent { get; set; }
    public required List<SecurityBackupLocation> BackupLocations { get; set; }
    public required DisasterRecoverySecurityValidation SecurityValidation { get; set; }
    public required Dictionary<string, RecoveryTimeObjective> RTOMetrics { get; set; }
    public required Dictionary<string, RecoveryPointObjective> RPOMetrics { get; set; }
    public required List<CulturalDataRecoveryResult> CulturalDataRecovery { get; set; }
    public required SecurityEncryptionValidation EncryptionValidation { get; set; }
    public required DateTime RecoveryStartTime { get; set; }
    public DateTime? RecoveryCompletionTime { get; set; }
    public required string InitiatedBy { get; set; }
    public DisasterRecoveryStatus Status { get; set; } = DisasterRecoveryStatus.InProgress;
    public required List<DisasterRecoverySecurityCheck> SecurityChecks { get; set; }
    public List<DisasterRecoveryAlert> Alerts { get; set; } = new();
    public required bool IsDataIntegrityVerified { get; set; }
    public required Dictionary<string, ComplianceValidationResult> ComplianceValidation { get; set; }
    public string? RecoveryNotes { get; set; }
    public List<string> FailedComponents { get; set; } = new();
}

/// <summary>
/// Performance monitoring result for comprehensive system and cultural event monitoring
/// Core result type for performance tracking and optimization decisions
/// </summary>
public class PerformanceMonitoringResult
{
    public required string MonitoringId { get; set; }
    public required PerformanceMonitoringType MonitoringType { get; set; }
    public required Dictionary<string, PerformanceMetric> PerformanceMetrics { get; set; }
    public required PerformanceHealthStatus OverallHealthStatus { get; set; }
    public required List<PerformanceAlert> PerformanceAlerts { get; set; }
    public required Dictionary<string, CulturalPerformanceMetric> CulturalMetrics { get; set; }
    public required PerformanceTrendAnalysis TrendAnalysis { get; set; }
    public required DateTime MonitoringTimestamp { get; set; }
    public required TimeSpan MonitoringPeriod { get; set; }
    public required string MonitoringRegion { get; set; }
    public required List<PerformanceThreshold> ThresholdViolations { get; set; }
    public required Dictionary<string, ResourceUtilization> ResourceUtilization { get; set; }
    public List<PerformanceOptimizationSuggestion> OptimizationSuggestions { get; set; } = new();
    public required bool IsRealTimeMonitoring { get; set; }
    public PerformanceMonitoringStatus Status { get; set; } = PerformanceMonitoringStatus.Active;
    public Dictionary<string, object> ExtendedMetrics { get; set; } = new();
    public List<string> MonitoredServices { get; set; } = new();
    public required PerformanceBaseline Baseline { get; set; }
}

// Supporting Enums for Result Types

public enum MLThreatType
{
    CulturalDataBreach,
    UnauthorizedAccess,
    AnomalousActivity,
    DataExfiltration,
    PrivacyViolation,
    ComplianceViolation,
    SystemIntrusion,
    CulturalContentTampering
}

public enum MLThreatSeverity
{
    Informational,
    Low,
    Medium,
    High,
    Critical,
    Emergency
}

public enum MLThreatStatus
{
    Open,
    InProgress,
    UnderAnalysis,
    Mitigated,
    Resolved,
    FalsePositive,
    Escalated
}

public enum SecurityOptimizationType
{
    ResourceAllocation,
    AccessControl,
    EncryptionOptimization,
    NetworkSecurity,
    DatabaseSecurity,
    CulturalDataProtection,
    ComplianceAlignment,
    ThreatPrevention
}

public enum SecurityOptimizationStatus
{
    Planned,
    InProgress,
    Completed,
    Failed,
    RolledBack,
    PartiallyCompleted
}

public enum SecurityLoadBalancingStrategy
{
    RoundRobin,
    WeightedRoundRobin,
    LeastConnections,
    CulturalAffinity,
    GeographicProximity,
    SecurityPriority,
    Adaptive
}

public enum SecurityLoadBalancingStatus
{
    Active,
    Inactive,
    Degraded,
    Failed,
    Maintenance,
    Recovering
}

public enum SecurityRedundancyLevel
{
    None,
    Basic,
    Standard,
    High,
    Critical,
    Maximum
}

public enum DisasterRecoveryType
{
    Partial,
    Complete,
    Selective,
    CulturalSpecific,
    GeographicSpecific,
    ServiceSpecific
}

public enum DisasterRecoveryStatus
{
    Planned,
    InProgress,
    Completed,
    Failed,
    PartiallyCompleted,
    Aborted,
    Verified
}

public enum PerformanceMonitoringType
{
    RealTime,
    Batch,
    EventDriven,
    CulturalEvent,
    ComplianceMonitoring,
    SecurityMonitoring,
    Hybrid
}

public enum PerformanceHealthStatus
{
    Healthy,
    Warning,
    Critical,
    Degraded,
    Failed,
    Unknown
}

public enum PerformanceMonitoringStatus
{
    Active,
    Inactive,
    Paused,
    Failed,
    Maintenance,
    Initializing
}

// Supporting Complex Types for Results

public class MLThreatIndicator
{
    public required string IndicatorType { get; set; }
    public required string IndicatorValue { get; set; }
    public required double Weight { get; set; }
    public required DateTime DetectedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class MLThreatAnalysis
{
    public required string AnalysisId { get; set; }
    public required List<string> AttackVectors { get; set; }
    public required double RiskScore { get; set; }
    public required List<string> AffectedAssets { get; set; }
    public required Dictionary<string, object> AnalysisDetails { get; set; }
}

public class MLRecommendedAction
{
    public required string ActionType { get; set; }
    public required string Description { get; set; }
    public required ActionPriority Priority { get; set; }
    public required TimeSpan EstimatedDuration { get; set; }
    public List<string> Prerequisites { get; set; } = new();
}

public class SecurityResource
{
    public required string ResourceId { get; set; }
    public required string ResourceType { get; set; }
    public required SecurityResourceStatus Status { get; set; }
    public required Dictionary<string, object> Configuration { get; set; }
    public Dictionary<string, double> Metrics { get; set; } = new();
}

public class SecurityOptimizationMetrics
{
    public required double SecurityScoreImprovement { get; set; }
    public required double PerformanceImpact { get; set; }
    public required Dictionary<string, double> ResourceSavings { get; set; }
    public required TimeSpan OptimizationTime { get; set; }
}

public class SecurityPostureImpact
{
    public required double SecurityPostureBefore { get; set; }
    public required double SecurityPostureAfter { get; set; }
    public required List<string> ImpactedControls { get; set; }
    public required Dictionary<string, SecurityControlImpact> ControlImpacts { get; set; }
}

public class PerformanceImpactAssessment
{
    public required double PerformanceChangePct { get; set; }
    public required Dictionary<string, double> MetricChanges { get; set; }
    public required List<string> ImpactedServices { get; set; }
    public PerformanceImpactSeverity Severity { get; set; }
}

public class SecurityOptimizationRecommendation
{
    public required string RecommendationId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required OptimizationPriority Priority { get; set; }
    public required List<string> ActionItems { get; set; }
}

public class CulturalSecurityOptimization
{
    public required CulturalEventType CulturalContext { get; set; }
    public required List<string> OptimizedSecurityControls { get; set; }
    public required double SecurityImpact { get; set; }
    public required Dictionary<string, object> CulturalConsiderations { get; set; }
}

public class SecurityEndpoint
{
    public required string EndpointId { get; set; }
    public required string EndpointUrl { get; set; }
    public required string Region { get; set; }
    public required SecurityEndpointStatus Status { get; set; }
    public required SecurityEndpointMetrics Metrics { get; set; }
}

public class SecurityLoadMetrics
{
    public required double CurrentLoad { get; set; }
    public required double MaxCapacity { get; set; }
    public required double UtilizationPercentage { get; set; }
    public required List<SecurityLoadAlert> LoadAlerts { get; set; }
}

public class CulturalSecurityZone
{
    public required string ZoneId { get; set; }
    public required List<CulturalEventType> CulturalContexts { get; set; }
    public required SecurityZoneConfiguration Configuration { get; set; }
    public required List<string> AssignedEndpoints { get; set; }
}

public class SecurityPerformanceMetric
{
    public required string MetricName { get; set; }
    public required double Value { get; set; }
    public required string Unit { get; set; }
    public required DateTime Timestamp { get; set; }
    public required SecurityMetricThreshold Threshold { get; set; }
}

// Additional supporting types and enums
public enum ActionPriority { Low, Medium, High, Critical, Immediate }
public enum SecurityResourceStatus { Active, Inactive, Degraded, Failed, Maintenance }
public enum PerformanceImpactSeverity { Minimal, Low, Medium, High, Severe }
public enum OptimizationPriority { Low, Medium, High, Critical }
public enum SecurityEndpointStatus { Healthy, Warning, Critical, Offline }

// Placeholder classes for complex types that will be detailed in future iterations
public class MLDetectionMethod { }
public class SecurityOptimizationWarning { }
public class SecurityFailoverConfiguration { }
public class SecurityLoadBalancingHealthCheck { }
public class SecurityLoadBalancingAlert { }
public class DisasterRecoveryTrigger { }
public class SecurityBackupLocation { }
public class DisasterRecoverySecurityValidation { }
public class RecoveryTimeObjective { }
public class RecoveryPointObjective { }
public class CulturalDataRecoveryResult { }
public class SecurityEncryptionValidation { }
public class DisasterRecoverySecurityCheck { }
public class DisasterRecoveryAlert { }
public class ComplianceValidationResult { }
public class PerformanceMetric { }
public class PerformanceAlert { }
public class CulturalPerformanceMetric { }
public class PerformanceTrendAnalysis { }
public class PerformanceThreshold { }
public class ResourceUtilization { }
public class PerformanceOptimizationSuggestion { }
public class PerformanceBaseline { }
public class SecurityControlImpact { }
public class SecurityEndpointMetrics { }
public class SecurityLoadAlert { }
public class SecurityZoneConfiguration { }
public class SecurityMetricThreshold { }