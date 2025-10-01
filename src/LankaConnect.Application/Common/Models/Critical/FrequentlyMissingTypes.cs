using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.ValueObjects;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Shared;

namespace LankaConnect.Application.Common.Models.Critical;

/// <summary>
/// Frequently missing types appearing 2+ times in compilation errors
/// These types are causing multiple compilation failures and need immediate implementation
/// </summary>

/// <summary>
/// Strongly typed identifier base class for type-safe entity IDs
/// Generic type referenced across the application layer
/// </summary>
public abstract class StronglyTypedId<T> : ValueObject where T : IComparable<T>
{
    public T Value { get; }

    protected StronglyTypedId(T value)
    {
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString() ?? string.Empty;

    public static implicit operator T(StronglyTypedId<T> id) => id.Value;
}

/// <summary>
/// Synchronization integrity result for multi-region data synchronization
/// Referenced in disaster recovery and performance monitoring
/// </summary>
public class SynchronizationIntegrityResult
{
    public required string SynchronizationId { get; set; }
    public required SynchronizationStatus Status { get; set; }
    public required List<SynchronizationRegion> SynchronizedRegions { get; set; }
    public required Dictionary<string, SyncIntegrityMetric> IntegrityMetrics { get; set; }
    public required DateTime SynchronizationTimestamp { get; set; }
    public required TimeSpan SynchronizationDuration { get; set; }
    public required List<SynchronizationConflict> Conflicts { get; set; }
    public required SynchronizationResolution Resolution { get; set; }
    public Dictionary<string, object> SyncMetadata { get; set; } = new();
    public bool RequiresManualIntervention { get; set; }
    public string? SyncNotes { get; set; }
}

/// <summary>
/// Security synchronization result for security policy and configuration sync
/// Critical for maintaining consistent security posture across regions
/// </summary>
public class SecuritySynchronizationResult
{
    public required string SecuritySyncId { get; set; }
    public required SecuritySyncType SyncType { get; set; }
    public required SecuritySyncStatus Status { get; set; }
    public required List<SecurityPolicy> SynchronizedPolicies { get; set; }
    public required Dictionary<string, SecuritySyncMetric> SyncMetrics { get; set; }
    public required DateTime SyncTimestamp { get; set; }
    public required TimeSpan SyncDuration { get; set; }
    public required List<SecuritySyncConflict> SecurityConflicts { get; set; }
    public Dictionary<string, object> SecuritySyncDetails { get; set; } = new();
    public bool IsSecurityPostureMaintained { get; set; }
    public string? SecuritySyncNotes { get; set; }
}

/// <summary>
/// Session security result for cultural session management
/// Referenced in security optimization interfaces
/// </summary>
public class SessionSecurityResult
{
    public required string SessionId { get; set; }
    public required SessionSecurityStatus SecurityStatus { get; set; }
    public required SessionSecurityMetrics SecurityMetrics { get; set; }
    public required List<SessionSecurityThreat> DetectedThreats { get; set; }
    public required SessionCulturalContext CulturalContext { get; set; }
    public required DateTime SessionStartTime { get; set; }
    public DateTime? SessionEndTime { get; set; }
    public required SessionSecurityValidation ValidationResult { get; set; }
    public Dictionary<string, object> SessionAttributes { get; set; } = new();
    public bool RequiresSecurityAction { get; set; }
    public string? SecurityNotes { get; set; }
}

/// <summary>
/// Service tier configuration for application service classification
/// Referenced in auto-scaling and performance monitoring
/// </summary>
public class ServiceTier
{
    public required string TierId { get; set; }
    public required string TierName { get; set; }
    public required ServiceTierLevel TierLevel { get; set; }
    public required ServiceTierConfiguration Configuration { get; set; }
    public required Dictionary<string, ServiceTierMetric> TierMetrics { get; set; }
    public required List<string> IncludedServices { get; set; }
    public required ServiceTierSLA SLA { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

/// <summary>
/// Shutdown configuration for graceful service shutdown procedures
/// Referenced in disaster recovery and maintenance operations
/// </summary>
public class ShutdownConfiguration
{
    public required string ConfigurationId { get; set; }
    public required string ConfigurationName { get; set; }
    public required ShutdownType ShutdownType { get; set; }
    public required ShutdownSequence ShutdownSequence { get; set; }
    public required TimeSpan GracefulShutdownTimeout { get; set; }
    public required Dictionary<string, ShutdownStep> ShutdownSteps { get; set; }
    public required List<ShutdownPreCondition> PreConditions { get; set; }
    public required List<ShutdownPostAction> PostActions { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? Description { get; set; }
}

/// <summary>
/// Synthetic transaction for performance monitoring and health checks
/// Referenced in monitoring and alerting interfaces
/// </summary>
public class SyntheticTransaction
{
    public required string TransactionId { get; set; }
    public required string TransactionName { get; set; }
    public required SyntheticTransactionType TransactionType { get; set; }
    public required List<SyntheticStep> TransactionSteps { get; set; }
    public required SyntheticTransactionConfiguration Configuration { get; set; }
    public required TimeSpan ExpectedDuration { get; set; }
    public required Dictionary<string, SyntheticMetric> Metrics { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? Description { get; set; }
}

/// <summary>
/// Synthetic transaction results for monitoring execution results
/// Critical for performance monitoring and alerting
/// </summary>
public class SyntheticTransactionResults
{
    public required string ResultId { get; set; }
    public required SyntheticTransaction Transaction { get; set; }
    public required SyntheticExecutionStatus ExecutionStatus { get; set; }
    public required List<SyntheticStepResult> StepResults { get; set; }
    public required Dictionary<string, SyntheticResultMetric> ResultMetrics { get; set; }
    public required DateTime ExecutionTimestamp { get; set; }
    public required TimeSpan ExecutionDuration { get; set; }
    public required string ExecutedBy { get; set; }
    public List<SyntheticTransactionError> Errors { get; set; } = new();
    public Dictionary<string, object> ExecutionContext { get; set; } = new();
    public string? ExecutionNotes { get; set; }
}

/// <summary>
/// System health monitoring configuration for comprehensive system monitoring
/// Referenced in performance monitoring and disaster recovery
/// </summary>
public class SystemHealthMonitoringConfiguration
{
    public required string ConfigurationId { get; set; }
    public required string ConfigurationName { get; set; }
    public required List<HealthCheckDefinition> HealthChecks { get; set; }
    public required Dictionary<string, HealthCheckThreshold> Thresholds { get; set; }
    public required TimeSpan MonitoringInterval { get; set; }
    public required HealthAlertConfiguration AlertConfiguration { get; set; }
    public required List<string> MonitoredRegions { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

/// <summary>
/// System health monitoring result for ongoing health monitoring
/// Critical for system reliability and alerting
/// </summary>
public class SystemHealthMonitoringResult
{
    public required string MonitoringId { get; set; }
    public required SystemHealthMonitoringConfiguration Configuration { get; set; }
    public required SystemHealthStatus OverallHealthStatus { get; set; }
    public required Dictionary<string, HealthCheckResult> HealthCheckResults { get; set; }
    public required List<HealthAlert> HealthAlerts { get; set; }
    public required DateTime MonitoringTimestamp { get; set; }
    public required TimeSpan MonitoringDuration { get; set; }
    public Dictionary<string, object> HealthMetrics { get; set; } = new();
    public string? HealthSummary { get; set; }
    public DateTime? NextMonitoringScheduled { get; set; }
}

// Supporting Enums

public enum SynchronizationStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    PartiallyCompleted,
    Conflicted
}

public enum SecuritySyncType
{
    PolicySync,
    ConfigurationSync,
    CredentialSync,
    CertificateSync,
    AccessControlSync,
    FullSync
}

public enum SecuritySyncStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    PartiallyCompleted,
    SecurityCompromised
}

public enum SessionSecurityStatus
{
    Secure,
    Warning,
    Compromised,
    Expired,
    Invalid,
    UnderReview
}

public enum ServiceTierLevel
{
    Basic,
    Standard,
    Premium,
    Enterprise,
    Critical
}

public enum ShutdownType
{
    Graceful,
    Immediate,
    Forced,
    Emergency,
    Maintenance
}

public enum SyntheticTransactionType
{
    Performance,
    Availability,
    Functional,
    Security,
    Cultural,
    EndToEnd
}

public enum SyntheticExecutionStatus
{
    Successful,
    Failed,
    PartiallySuccessful,
    Timeout,
    Error,
    Cancelled
}


// Supporting Complex Types

public class SynchronizationRegion
{
    public required string RegionId { get; set; }
    public required string RegionName { get; set; }
    public required SyncRegionStatus Status { get; set; }
    public DateTime LastSyncTime { get; set; }
}

public class SyncIntegrityMetric
{
    public required string MetricName { get; set; }
    public required double Value { get; set; }
    public required SyncMetricStatus Status { get; set; }
}

public class SynchronizationConflict
{
    public required string ConflictId { get; set; }
    public required SyncConflictType ConflictType { get; set; }
    public required string Description { get; set; }
    public required SyncConflictResolution Resolution { get; set; }
}

public class SynchronizationResolution
{
    public required string ResolutionId { get; set; }
    public required SyncResolutionType ResolutionType { get; set; }
    public required List<string> ResolvedConflicts { get; set; }
    public DateTime ResolutionTimestamp { get; set; }
}

public class SecuritySyncConflict
{
    public required string ConflictId { get; set; }
    public required SecurityConflictType ConflictType { get; set; }
    public required string Description { get; set; }
    public required SecurityConflictResolution Resolution { get; set; }
}

public class SecuritySyncMetric
{
    public required string MetricName { get; set; }
    public required double Value { get; set; }
    public required SecurityMetricStatus Status { get; set; }
}

public class SessionSecurityMetrics
{
    public required Dictionary<string, double> SecurityScores { get; set; }
    public required List<string> SecurityIndicators { get; set; }
    public required SessionRiskScore RiskScore { get; set; }
}

public class SessionSecurityThreat
{
    public required string ThreatId { get; set; }
    public required SessionThreatType ThreatType { get; set; }
    public required SessionThreatSeverity Severity { get; set; }
    public required string Description { get; set; }
}

public class SessionCulturalContext
{
    public required string CulturalContextId { get; set; }
    public required List<CulturalEventType> CulturalEvents { get; set; }
    public required Dictionary<string, object> CulturalAttributes { get; set; }
}

public class SessionSecurityValidation
{
    public required string ValidationId { get; set; }
    public required SessionValidationStatus Status { get; set; }
    public required List<SessionValidationCheck> ValidationChecks { get; set; }
}

// Additional supporting enums and placeholder classes
public enum SyncRegionStatus { Active, Inactive, Synchronizing, Failed }
public enum SyncMetricStatus { Normal, Warning, Critical }
public enum SyncConflictType { Data, Configuration, Policy, Cultural }
public enum SyncConflictResolution { Automatic, Manual, Escalated }
public enum SyncResolutionType { Merge, Overwrite, Manual, Priority }
public enum SecurityConflictType { Policy, Configuration, Access, Credential }
public enum SecurityConflictResolution { Automatic, Manual, SecurityEscalated }
public enum SecurityMetricStatus { Secure, Warning, Critical }
public enum SessionThreatType { Hijacking, Impersonation, DataBreach, Tampering }
public enum SessionThreatSeverity { Low, Medium, High, Critical }
public enum SessionValidationStatus { Valid, Invalid, Expired, Compromised }

// Placeholder classes for future detailed implementation
public class SecurityPolicy { }
public class SessionRiskScore { }
public class SessionValidationCheck { }
public class ServiceTierConfiguration { }
public class ServiceTierMetric { }
public class ServiceTierSLA { }
public class ShutdownSequence { }
public class ShutdownStep { }
public class ShutdownPreCondition { }
public class ShutdownPostAction { }
public class SyntheticStep { }
public class SyntheticTransactionConfiguration { }
public class SyntheticMetric { }
public class SyntheticStepResult { }
public class SyntheticResultMetric { }
public class SyntheticTransactionError { }
public class HealthCheckDefinition { }
public class HealthCheckThreshold { }
public class HealthAlertConfiguration { }
public class HealthCheckResult { }
public class HealthAlert { }