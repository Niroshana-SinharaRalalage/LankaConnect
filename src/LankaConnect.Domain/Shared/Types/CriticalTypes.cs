using System;
using System.Collections.Generic;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Domain.Shared.Types;

/// <summary>
/// CANONICAL TYPE DEFINITIONS - Single Source of Truth
/// Consolidated from multiple locations to eliminate CS0104 ambiguous reference errors
/// Following Clean Architecture: Domain layer owns business types
/// </summary>

/// <summary>
/// Unified backup priority enumeration
/// Consolidated from Domain.Shared.BackupTypes and Application.Common.Interfaces embedded definitions
/// </summary>
public enum BackupPriority
{
    Low,
    Standard,
    Medium,
    High,
    Critical,
    Emergency
}

/// <summary>
/// Backup frequency enumeration for scheduling operations
/// </summary>
public enum BackupFrequency
{
    Continuous,
    Every15Minutes,
    Hourly,
    Daily,
    Weekly,
    Monthly
}

/// <summary>
/// Data retention policy enumeration
/// </summary>
public enum DataRetentionPolicy
{
    ShortTerm,   // 30 days
    MediumTerm,  // 90 days
    LongTerm,    // 1 year
    Permanent    // Indefinite
}

/// <summary>
/// Unified disaster recovery result
/// Consolidated from multiple DisasterRecoveryResult definitions
/// </summary>
public class DisasterRecoveryResult
{
    public Guid RecoveryId { get; set; }
    public bool IsSuccessful { get; set; }
    public TimeSpan RecoveryDuration { get; set; }
    public string RecoveryLocation { get; set; } = string.Empty;
    public List<string> RecoveredComponents { get; set; } = new();
    public decimal DataIntegrityScore { get; set; }
    public DateTime RecoveryTimestamp { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// Data integrity validation result
/// </summary>
public class DataIntegrityValidationResult
{
    public Guid ValidationId { get; set; }
    public bool IsValid { get; set; }
    public decimal IntegrityScore { get; set; }
    public List<string> ValidationErrors { get; set; } = new();
    public List<string> ValidatedDatasets { get; set; } = new();
    public DateTime ValidationTimestamp { get; set; }
}

/// <summary>
/// Cultural backup configuration
/// Previously embedded in IBackupDisasterRecoveryEngine interface
/// </summary>
public class CulturalBackupConfiguration
{
    public List<Guid> CulturalEventIds { get; set; } = new();
    public List<GeographicRegion> Regions { get; set; } = new();
    public BackupScope Scope { get; set; }
    public CulturalSensitivityLevel SensitivityLevel { get; set; }
    public bool IncludePredictiveModels { get; set; }
    public bool IncludeUserAffinityData { get; set; }
    public DataRetentionPolicy RetentionPolicy { get; set; }
}

/// <summary>
/// Backup operation result
/// Previously embedded in IBackupDisasterRecoveryEngine interface
/// </summary>
public class BackupOperationResult
{
    public Guid BackupId { get; set; }
    public bool IsSuccessful { get; set; }
    public long BackupSizeBytes { get; set; }
    public TimeSpan Duration { get; set; }
    public string BackupLocation { get; set; } = string.Empty;
    public List<string> IncludedDatasets { get; set; } = new();
    public BackupIntegrityStatus IntegrityStatus { get; set; }
}

/// <summary>
/// Additional enums previously embedded in interface
/// </summary>
public enum BackupScope
{
    Minimal,
    Standard,
    Complete,
    Archive
}

public enum CulturalSensitivityLevel
{
    Low,
    Medium,
    High,
    Sacred
}

public enum BackupIntegrityStatus
{
    Unknown,
    Pending,
    Verified,
    Failed,
    Corrupted
}

public enum DisasterScenario
{
    DataCenterFailure,
    NetworkOutage,
    SecurityBreach,
    NaturalDisaster,
    SystemCorruption,
    RegionalOutage
}

public enum FailoverStrategy
{
    Automatic,
    Manual,
    GracefulFailover,
    ImmediateFailover,
    ColdStandby,
    HotStandby
}

/// <summary>
/// Algorithm backup scope for cultural intelligence models
/// </summary>
public enum AlgorithmBackupScope
{
    CulturalIntelligenceOnly,
    FullSystem,
    CriticalComponents,
    UserDataOnly
}

/// <summary>
/// Failback result for disaster recovery operations
/// </summary>
public class FailbackResult
{
    public bool IsSuccessful { get; set; }
    public DateTime FailbackTimestamp { get; set; }
    public TimeSpan FailbackDuration { get; set; }
    public string FailbackLocation { get; set; } = string.Empty;
    public List<string> RestoredServices { get; set; } = new();
    public Dictionary<string, object> ValidationResults { get; set; } = new();
}

/// <summary>
/// Cultural intelligence failover configuration
/// </summary>
public class CulturalIntelligenceFailoverConfiguration
{
    public Guid ConfigurationId { get; set; }
    public List<GeographicRegion> FailoverRegions { get; set; } = new();
    public TimeSpan MaxFailoverTime { get; set; }
    public bool PreserveCulturalContext { get; set; }
    public BackupPriority Priority { get; set; }
}

/// <summary>
/// Cultural intelligence failover result
/// </summary>
public class CulturalIntelligenceFailoverResult
{
    public bool IsSuccessful { get; set; }
    public GeographicRegion ActiveRegion { get; set; }
    public DateTime FailoverTimestamp { get; set; }
    public TimeSpan FailoverDuration { get; set; }
    public bool CulturalContextPreserved { get; set; }
    public List<string> AffectedServices { get; set; } = new();
}

/// <summary>
/// Disaster recovery load profile for capacity planning
/// </summary>
public class DisasterRecoveryLoadProfile
{
    public string ProfileId { get; set; } = string.Empty;
    public Dictionary<string, double> ExpectedLoadMetrics { get; set; } = new();
    public TimeSpan PeakLoadDuration { get; set; }
    public List<string> CriticalServices { get; set; } = new();
    public double ConcurrentUserLimit { get; set; }
}

/// <summary>
/// Result of capacity scaling operations
/// </summary>
public class CapacityScalingResult
{
    public bool ScalingSuccessful { get; set; }
    public int NewCapacity { get; set; }
    public TimeSpan ScalingDuration { get; set; }
    public double ResourceUtilization { get; set; }
    public List<string> ScaledComponents { get; set; } = new();
}

/// <summary>
/// Result of community failover operations
/// </summary>
public class CommunityFailoverResult
{
    public bool FailoverSuccessful { get; set; }
    public string TargetCommunity { get; set; } = string.Empty;
    public DateTime FailoverTimestamp { get; set; }
    public List<string> MigratedServices { get; set; } = new();
    public bool DataIntegrityMaintained { get; set; }
}

/// <summary>
/// Synchronization strategy for disaster recovery
/// </summary>
public enum SynchronizationStrategy
{
    RealTime,
    Scheduled,
    EventDriven,
    Manual
}

/// <summary>
/// Business continuity assessment scope
/// </summary>
public enum BusinessContinuityScope
{
    FullSystem,
    CriticalServices,
    CulturalServices,
    UserData,
    BusinessLogic
}

/// <summary>
/// Business continuity assessment result
/// </summary>
public class BusinessContinuityAssessment
{
    public string AssessmentId { get; set; } = string.Empty;
    public bool ContinuityViable { get; set; }
    public double RiskScore { get; set; }
    public List<string> CriticalVulnerabilities { get; set; } = new();
    public Dictionary<string, string> Recommendations { get; set; } = new();
}

/// <summary>
/// Reason for continuity activation
/// </summary>
public enum ContinuityActivationReason
{
    DisasterDetected,
    MaintenanceWindow,
    PerformanceDegradation,
    SecurityBreach,
    ManualActivation
}

/// <summary>
/// Granular recovery configuration
/// </summary>
public class GranularRecoveryConfiguration
{
    public string ConfigurationId { get; set; } = string.Empty;
    public List<string> RecoveryTargets { get; set; } = new();
    public Dictionary<string, object> GranularitySettings { get; set; } = new();
    public string RecoveryStrategy { get; set; } = string.Empty;
    public TimeSpan RecoveryTimeObjective { get; set; }
}

/// <summary>
/// Granular recovery result
/// </summary>
public class GranularRecoveryResult
{
    public bool RecoverySuccessful { get; set; }
    public List<string> RecoveredComponents { get; set; } = new();
    public Dictionary<string, string> ComponentStatus { get; set; } = new();
    public TimeSpan TotalRecoveryTime { get; set; }
}

/// <summary>
/// Time travel recovery scope
/// </summary>
public class TimeTravelRecoveryScope
{
    public string ScopeId { get; set; } = string.Empty;
    public DateTime TargetTimestamp { get; set; }
    public List<string> IncludedComponents { get; set; } = new();
    public string RecoveryBoundary { get; set; } = string.Empty;
}

/// <summary>
/// Time travel recovery result
/// </summary>
public class TimeTravelRecoveryResult
{
    public bool RecoverySuccessful { get; set; }
    public DateTime RecoveredToTimestamp { get; set; }
    public List<string> RestoredComponents { get; set; } = new();
    public TimeSpan RecoveryDuration { get; set; }
}

/// <summary>
/// Critical system component
/// </summary>
public class CriticalSystemComponent
{
    public string ComponentId { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public string ComponentType { get; set; } = string.Empty;
    public int CriticalityLevel { get; set; }
    public List<string> Dependencies { get; set; } = new();
}

/// <summary>
/// Partial recovery strategy
/// </summary>
public class PartialRecoveryStrategy
{
    public string StrategyId { get; set; } = string.Empty;
    public string StrategyName { get; set; } = string.Empty;
    public List<string> RecoveryPhases { get; set; } = new();
    public TimeSpan MaxRecoveryTime { get; set; }
}

/// <summary>
/// Partial system recovery result
/// </summary>
public class PartialSystemRecoveryResult
{
    public bool RecoveryInitiated { get; set; }
    public List<string> RecoveredPhases { get; set; } = new();
    public string CurrentRecoveryState { get; set; } = string.Empty;
    public double CompletionPercentage { get; set; }
}

/// <summary>
/// Hot standby configuration
/// </summary>
public class HotStandbyConfiguration
{
    public string ConfigurationId { get; set; } = string.Empty;
    public List<string> StandbyInstances { get; set; } = new();
    public string SynchronizationMode { get; set; } = string.Empty;
    public TimeSpan SyncInterval { get; set; }
    public bool AutoFailoverEnabled { get; set; }
}

/// <summary>
/// Hot standby activation result
/// </summary>
public class HotStandbyActivationResult
{
    public bool ActivationSuccessful { get; set; }
    public string ActivatedStandby { get; set; } = string.Empty;
    public DateTime ActivationTimestamp { get; set; }
    public TimeSpan ActivationDuration { get; set; }
}

/// <summary>
/// Differential recovery configuration
/// </summary>
public class DifferentialRecoveryConfiguration
{
    public string ConfigurationId { get; set; } = string.Empty;
    public DateTime BaselineTimestamp { get; set; }
    public List<string> DifferentialSources { get; set; } = new();
    public string MergeStrategy { get; set; } = string.Empty;
}

/// <summary>
/// Differential recovery result
/// </summary>
public class DifferentialRecoveryResult
{
    public bool RecoveryCompleted { get; set; }
    public List<string> ConflictsResolved { get; set; } = new();
    public DateTime RecoveryTimestamp { get; set; }
    public string FinalDataState { get; set; } = string.Empty;
}

/// <summary>
/// Cross platform recovery configuration
/// </summary>
public class CrossPlatformRecoveryConfiguration
{
    public string ConfigurationId { get; set; } = string.Empty;
    public List<string> SourcePlatforms { get; set; } = new();
    public string TargetPlatform { get; set; } = string.Empty;
    public List<string> DataTransformations { get; set; } = new();
}

/// <summary>
/// Cross platform recovery result
/// </summary>
public class CrossPlatformRecoveryResult
{
    public bool RecoverySuccessful { get; set; }
    public string TargetPlatformStatus { get; set; } = string.Empty;
    public List<string> MigratedComponents { get; set; } = new();
    public DateTime MigrationTimestamp { get; set; }
}