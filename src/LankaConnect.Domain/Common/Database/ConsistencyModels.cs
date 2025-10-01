using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Communications.ValueObjects;

namespace LankaConnect.Domain.Common;

public enum CulturalEventSyncType
{
    SacredEvent,
    CommunityContent,
    DiasporaNews,
    BusinessListing,
    UserProfile,
    CalendarEvent,
    CulturalFestival
}

public enum ConsistencyCheckStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    PartiallyConsistent,
    Inconsistent
}

public enum CulturalConflictType
{
    CalendarDiscrepancy,
    EventTimingConflict,
    CulturalAppropriateness,
    LanguageTranslation,
    CommunityPriority,
    RegionalVariation,
    AuthorityDispute
}

public enum ConflictResolutionStrategy
{
    CulturalSignificancePriority,
    SourceRegionAuthority,
    MajorityConsensus,
    TimestampBased,
    CulturalExpertReview,
    AutomatedResolution
}

public enum SynchronizationPriority
{
    Low,
    Medium,
    High,
    Critical,
    Sacred
}

public enum CulturalAuthorityType
{
    ReligiousCouncil,
    CommunityLeaders,
    AcademicInstitution,
    GovernmentAgency,
    CulturalOrganization,
    DiasporaAssociation
}

public enum FailoverTriggerType
{
    RegionOutage,
    NetworkPartition,
    PerformanceDegradation,
    MaintenanceWindow,
    DisasterRecovery,
    LoadRebalancing
}

public class CulturalDataConsistencyCheck
{
    public string CheckId { get; set; } = Guid.NewGuid().ToString();
    public CulturalDataType DataType { get; set; } = CulturalDataType.CommunityInsights;
    public string SourceRegion { get; set; } = string.Empty;
    public List<string> TargetRegions { get; set; } = new();
    public ConsistencyLevel ConsistencyLevel { get; set; } = ConsistencyLevel.BoundedStaleness;
    public DateTime CheckTimestamp { get; set; } = DateTime.UtcNow;
    public double ConsistencyScore { get; set; }
    public bool IsConsistent { get; set; }
    public ConsistencyCheckStatus Status { get; set; } = ConsistencyCheckStatus.Pending;
    public Dictionary<string, object> CheckDetails { get; set; } = new();
}

public class CulturalConflictResolution
{
    public string ConflictId { get; set; } = Guid.NewGuid().ToString();
    public CulturalConflictType ConflictType { get; set; } = CulturalConflictType.CalendarDiscrepancy;
    public List<string> ConflictingRegions { get; set; } = new();
    public ConflictResolutionStrategy ResolutionStrategy { get; set; } = ConflictResolutionStrategy.CulturalSignificancePriority;
    public DateTime ResolutionTimestamp { get; set; } = DateTime.UtcNow;
    public string ResolutionDetails { get; set; } = string.Empty;
    public string AuthoritySource { get; set; } = string.Empty;
    public double ResolutionConfidence { get; set; }
    public bool AutomatedResolution { get; set; } = true;
}

public class CrossRegionSynchronizationResult
{
    public string SynchronizationId { get; set; } = Guid.NewGuid().ToString();
    public string SourceRegion { get; set; } = string.Empty;
    public List<string> TargetRegions { get; set; } = new();
    public CulturalDataType DataType { get; set; }
    public TimeSpan SynchronizationDuration { get; set; }
    public bool ConsistencyAchieved { get; set; }
    public double ConsistencyScore { get; set; }
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    public List<string> SynchronizationLogs { get; set; } = new();
}

public class CulturalDataSynchronizationRequest
{
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    public CulturalDataType DataType { get; set; } = CulturalDataType.CommunityInsights;
    public string SourceRegion { get; set; } = string.Empty;
    public List<string> TargetRegions { get; set; } = new();
    public ConsistencyLevel ConsistencyLevel { get; set; } = ConsistencyLevel.BoundedStaleness;
    public DateTime RequestTimestamp { get; set; } = DateTime.UtcNow;
    public SynchronizationPriority Priority { get; set; } = SynchronizationPriority.Medium;
    public CulturalContext? CulturalContext { get; set; }
    public Dictionary<string, object> AdditionalParameters { get; set; } = new();
}

public class CulturalAuthoritySource
{
    public string AuthorityId { get; set; } = string.Empty;
    public string AuthorityName { get; set; } = string.Empty;
    public CulturalAuthorityType AuthorityType { get; set; }
    public string GeographicRegion { get; set; } = string.Empty;
    public CulturalDataType CulturalDomain { get; set; }
    public double AuthorityWeight { get; set; } = 1.0;
    public bool IsVerified { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> AuthorityMetadata { get; set; } = new();
}

public class CulturalDataConflict
{
    public string ConflictId { get; set; } = Guid.NewGuid().ToString();
    public CulturalConflictType ConflictType { get; set; } = CulturalConflictType.CalendarDiscrepancy;
    public List<string> ConflictingRegions { get; set; } = new();
    public CulturalDataType DataType { get; set; } = CulturalDataType.CommunityInsights;
    public CulturalSignificance CulturalSignificance { get; set; } = CulturalSignificance.Medium;
    public DateTime ConflictDetectedAt { get; set; } = DateTime.UtcNow;
    public string ConflictDescription { get; set; } = string.Empty;
    public Dictionary<string, object> ConflictingValues { get; set; } = new();
}

public class CrossRegionFailoverRequest
{
    public string RequestId { get; set; } = Guid.NewGuid().ToString();
    public string SourceRegion { get; set; } = string.Empty;
    public string TargetRegion { get; set; } = string.Empty;
    public FailoverTriggerType TriggerType { get; set; } = FailoverTriggerType.RegionOutage;
    public DateTime RequestTimestamp { get; set; } = DateTime.UtcNow;
    public bool PreserveCulturalConsistency { get; set; } = true;
    public TimeSpan MaxFailoverDuration { get; set; } = TimeSpan.FromSeconds(60);
    public List<CulturalDataType> AffectedDataTypes { get; set; } = new();
}

public class CrossRegionFailoverResult
{
    public string FailoverId { get; set; } = Guid.NewGuid().ToString();
    public string SourceRegion { get; set; } = string.Empty;
    public string TargetRegion { get; set; } = string.Empty;
    public string FailoverReason { get; set; } = string.Empty;
    public DateTime FailoverStartTime { get; set; } = DateTime.UtcNow;
    public TimeSpan FailoverDuration { get; set; }
    public bool Success { get; set; } = true;
    public bool DataConsistencyMaintained { get; set; } = true;
    public List<string> AffectedServices { get; set; } = new();
    public Dictionary<string, object> FailoverMetrics { get; set; } = new();
}

public class CulturalConsistencyMetrics
{
    public string MetricsId { get; set; } = Guid.NewGuid().ToString();
    public double OverallConsistencyScore { get; set; }
    public Dictionary<string, double> RegionConsistencyScores { get; set; } = new();
    public Dictionary<CulturalDataType, double> DataTypeConsistencyScores { get; set; } = new();
    public double ConflictResolutionSuccessRate { get; set; }
    public TimeSpan AverageSynchronizationTime { get; set; }
    public DateTime MetricsCollectionTimestamp { get; set; } = DateTime.UtcNow;
    public double SacredEventConsistencyScore { get; set; }
    public int TotalSynchronizationRequests { get; set; }
    public int SuccessfulSynchronizations { get; set; }
    public int FailedSynchronizations { get; set; }
}

public class CrossRegionConsistencyOptions
{
    public bool EnableStrongConsistencyForSacredEvents { get; set; } = true;
    public int MaxSacredEventSyncTimeMs { get; set; } = 500;
    public int MaxCommunityContentSyncTimeMs { get; set; } = 50;
    public ConsistencyLevel DefaultConsistencyLevel { get; set; } = ConsistencyLevel.BoundedStaleness;
    public ConsistencyLevel SacredEventConsistencyLevel { get; set; } = ConsistencyLevel.LinearizableStrong;
    public int CrossRegionFailoverTimeoutSeconds { get; set; } = 60;
    public double ConsistencyScoreThreshold { get; set; } = 0.95;
    public bool EnableCulturalConflictResolution { get; set; } = true;
    public ConflictResolutionStrategy DefaultConflictResolution { get; set; } = ConflictResolutionStrategy.CulturalSignificancePriority;
    public Dictionary<string, string> RegionConnectionStrings { get; set; } = new();
}

public class CulturalEventSynchronizationStatus
{
    public string StatusId { get; set; } = Guid.NewGuid().ToString();
    public CulturalEventSyncType EventType { get; set; }
    public string EventId { get; set; } = string.Empty;
    public List<string> SynchronizedRegions { get; set; } = new();
    public List<string> PendingSynchronizationRegions { get; set; } = new();
    public double SynchronizationProgress { get; set; }
    public DateTime LastSynchronizationAttempt { get; set; } = DateTime.UtcNow;
    public bool IsSynchronized { get; set; }
    public List<string> SynchronizationErrors { get; set; } = new();
}

public class CulturalConsistencyAlert
{
    public string AlertId { get; set; } = Guid.NewGuid().ToString();
    public CulturalConflictType AlertType { get; set; }
    public List<string> AffectedRegions { get; set; } = new();
    public CulturalDataType DataType { get; set; }
    public string AlertMessage { get; set; } = string.Empty;
    public DateTime AlertTimestamp { get; set; } = DateTime.UtcNow;
    public ConsistencyLevel RequiredConsistencyLevel { get; set; }
    public double CurrentConsistencyScore { get; set; }
    public bool RequiresImmediateAction { get; set; }
}

public class RegionalCulturalProfile
{
    public string ProfileId { get; set; } = Guid.NewGuid().ToString();
    public string RegionName { get; set; } = string.Empty;
    public List<string> DominantCommunities { get; set; } = new();
    public Dictionary<CulturalDataType, CulturalAuthoritySource> AuthoritativeSources { get; set; } = new();
    public TimeZoneInfo RegionTimeZone { get; set; } = TimeZoneInfo.Utc;
    public List<CulturalEventType> PrimaryCulturalEvents { get; set; } = new();
    public double CommunityEngagementScore { get; set; }
    public DateTime ProfileLastUpdated { get; set; } = DateTime.UtcNow;
}