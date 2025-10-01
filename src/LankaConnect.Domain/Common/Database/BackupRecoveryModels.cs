using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.Abstractions;
using GeoRegionEnum = LankaConnect.Domain.Common.Enums.GeographicRegion;

namespace LankaConnect.Domain.Common.Database;

/// <summary>
/// Comprehensive domain models for backup and disaster recovery 
/// with cultural intelligence for LankaConnect platform
/// </summary>

#region Enumerations

/// <summary>
/// Types of backup operations with cultural intelligence awareness
/// </summary>
public enum BackupType
{
    Full = 1,
    Incremental = 2,
    Differential = 3,
    CulturalEventSnapshot = 4,
    SacredDataArchive = 5,
    DiasporaCommunityBackup = 6,
    RealTimeReplication = 7,
    CulturalIntelligenceSnapshot = 8,
    EventDrivenBackup = 9,
    CrossRegionSync = 10,
    BusinessContinuityBackup = 11,
    RevenueDataProtection = 12
}

/// <summary>
/// Recovery strategies for different disaster scenarios
/// </summary>
public enum RecoveryStrategy
{
    PointInTimeRecovery = 1,
    FullSystemRestore = 2,
    PartialDataRecovery = 3,
    CulturalEventRecovery = 4,
    PriorityBasedRecovery = 5,
    MultiRegionFailover = 6,
    HotStandbyActivation = 7,
    ColdStandbyRestore = 8,
    ActiveActiveFailover = 9,
    GeographicFailover = 10,
    BusinessContinuityRestore = 11,
    RevenueStreamRecovery = 12,
    CulturalDataFirst = 13,
    CommunityPriorityRestore = 14
}

/// <summary>
/// Cultural data priority levels for backup and recovery
/// </summary>
public enum CulturalDataPriority
{
    Level10Sacred = 10,      // Highest priority - Sacred events/data
    Level9Religious = 9,     // Religious ceremonies
    Level8Traditional = 8,   // Traditional celebrations
    Level7Cultural = 7,      // Cultural festivals
    Level6Community = 6,     // Community events
    Level5General = 5,       // General cultural content
    Level4Social = 4,        // Social gatherings
    Level3Commercial = 3,    // Commercial events
    Level2Administrative = 2, // Administrative data
    Level1System = 1         // System logs/metadata
}

/// <summary>
/// Disaster types that can affect the platform
/// </summary>
public enum DisasterType
{
    DataCorruption = 1,
    HardwareFailure = 2,
    NetworkOutage = 3,
    CyberAttack = 4,
    NaturalDisaster = 5,
    CloudProviderOutage = 6,
    DatabaseCorruption = 7,
    ApplicationFailure = 8,
    SecurityBreach = 9,
    RegionalOutage = 10,
    CulturalEventOverload = 11,
    MassUserMigration = 12,
    ThirdPartyServiceFailure = 13,
    ConfigurationError = 14,
    DataCenterFailure = 15
}

/// <summary>
/// Recovery validation statuses
/// </summary>
public enum RecoveryValidationStatus
{
    NotStarted = 0,
    InProgress = 1,
    ValidationPassed = 2,
    ValidationFailed = 3,
    PartialValidation = 4,
    CulturalDataValidated = 5,
    BusinessContinuityValidated = 6,
    RevenueStreamValidated = 7,
    UserExperienceValidated = 8,
    DataIntegrityValidated = 9,
    SecurityValidated = 10,
    PerformanceValidated = 11
}

/// <summary>
/// Business continuity states
/// </summary>
public enum BusinessContinuityState
{
    Normal = 1,
    Degraded = 2,
    CriticalFunction = 3,
    EmergencyMode = 4,
    DisasterRecovery = 5,
    BusinessContinuity = 6,
    ReducedService = 7,
    CoreServicesOnly = 8,
    CulturalEventsOnly = 9,
    RevenueProtectionMode = 10,
    CommunityEmergencyMode = 11,
    FullRestoration = 12
}

/// <summary>
/// Data integrity check types
/// </summary>
public enum DataIntegrityCheckType
{
    Checksum = 1,
    HashValidation = 2,
    CrossReference = 3,
    CulturalDataConsistency = 4,
    UserProfileIntegrity = 5,
    EventDataValidation = 6,
    BusinessDataIntegrity = 7,
    RevenueDataValidation = 8,
    CommunityDataConsistency = 9,
    GeographicDataIntegrity = 10,
    TemporalConsistency = 11,
    RelationalIntegrity = 12
}

#endregion

#region Value Objects

/// <summary>
/// Recovery Time Objective (RTO) value object
/// </summary>
public record RecoveryTimeObjective
{
    public TimeSpan MaximumDowntime { get; init; }
    public TimeSpan CulturalEventRTO { get; init; }
    public TimeSpan BusinessContinuityRTO { get; init; }
    public TimeSpan RevenueStreamRTO { get; init; }
    public CulturalDataPriority Priority { get; init; }

    public static RecoveryTimeObjective Create(
        TimeSpan maximumDowntime,
        TimeSpan culturalEventRTO,
        TimeSpan businessContinuityRTO,
        TimeSpan revenueStreamRTO,
        CulturalDataPriority priority)
    {
        if (maximumDowntime <= TimeSpan.Zero)
            throw new ArgumentException("Maximum downtime must be positive");

        return new RecoveryTimeObjective
        {
            MaximumDowntime = maximumDowntime,
            CulturalEventRTO = culturalEventRTO,
            BusinessContinuityRTO = businessContinuityRTO,
            RevenueStreamRTO = revenueStreamRTO,
            Priority = priority
        };
    }
}

/// <summary>
/// Recovery Point Objective (RPO) value object
/// </summary>
public record RecoveryPointObjective
{
    public TimeSpan MaximumDataLoss { get; init; }
    public TimeSpan CulturalEventRPO { get; init; }
    public TimeSpan BusinessDataRPO { get; init; }
    public TimeSpan RevenueDataRPO { get; init; }
    public CulturalDataPriority Priority { get; init; }

    public static RecoveryPointObjective Create(
        TimeSpan maximumDataLoss,
        TimeSpan culturalEventRPO,
        TimeSpan businessDataRPO,
        TimeSpan revenueDataRPO,
        CulturalDataPriority priority)
    {
        if (maximumDataLoss <= TimeSpan.Zero)
            throw new ArgumentException("Maximum data loss must be positive");

        return new RecoveryPointObjective
        {
            MaximumDataLoss = maximumDataLoss,
            CulturalEventRPO = culturalEventRPO,
            BusinessDataRPO = businessDataRPO,
            RevenueDataRPO = revenueDataRPO,
            Priority = priority
        };
    }
}

/// <summary>
/// Geographic region details for multi-region coordination (renamed to avoid conflict with GeoRegionEnum enum)
/// </summary>
public record GeoRegionEnumDetails
{
    public string RegionCode { get; init; } = string.Empty;
    public string RegionName { get; init; } = string.Empty;
    public string CloudProvider { get; init; } = string.Empty;
    public string DataCenter { get; init; } = string.Empty;
    public bool IsPrimaryRegion { get; init; }
    public List<string> SupportedCultures { get; init; } = new();
    public TimeZoneInfo TimeZone { get; init; } = TimeZoneInfo.Utc;

    public static GeoRegionEnumDetails Create(
        string regionCode,
        string regionName,
        string cloudProvider,
        string dataCenter,
        bool isPrimaryRegion,
        List<string> supportedCultures,
        TimeZoneInfo timeZone)
    {
        if (string.IsNullOrWhiteSpace(regionCode))
            throw new ArgumentException("Region code cannot be empty");

        return new GeoRegionEnumDetails
        {
            RegionCode = regionCode,
            RegionName = regionName,
            CloudProvider = cloudProvider,
            DataCenter = dataCenter,
            IsPrimaryRegion = isPrimaryRegion,
            SupportedCultures = supportedCultures ?? new List<string>(),
            TimeZone = timeZone
        };
    }
}

#endregion

#region Domain Models

/// <summary>
/// Backup request model with cultural intelligence
/// </summary>
public class BackupRequest : BaseEntity
{
    public BackupType BackupType { get; private set; }
    public CulturalDataPriority Priority { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public DateTime ScheduledFor { get; private set; }
    public GeoRegionEnumDetails SourceRegion { get; private set; } = null!;
    public List<GeoRegionEnumDetails> TargetRegions { get; private set; } = new();
    public List<string> CulturalContexts { get; private set; } = new();
    public List<string> DataCategories { get; private set; } = new();
    public Dictionary<string, object> CulturalMetadata { get; private set; } = new();
    public bool IncludeUserData { get; private set; }
    public bool IncludeBusinessData { get; private set; }
    public bool IncludeRevenueData { get; private set; }
    public bool IncludeCulturalIntelligence { get; private set; }
    public string? Comments { get; private set; }

    private BackupRequest() { } // EF Core

    public static BackupRequest Create(
        BackupType backupType,
        CulturalDataPriority priority,
        DateTime scheduledFor,
        GeoRegionEnumDetails sourceRegion,
        List<GeoRegionEnumDetails> targetRegions,
        List<string> culturalContexts,
        List<string> dataCategories,
        bool includeUserData = true,
        bool includeBusinessData = true,
        bool includeRevenueData = true,
        bool includeCulturalIntelligence = true,
        string? comments = null)
    {
        return new BackupRequest
        {
            BackupType = backupType,
            Priority = priority,
            RequestedAt = DateTime.UtcNow,
            ScheduledFor = scheduledFor,
            SourceRegion = sourceRegion,
            TargetRegions = targetRegions,
            CulturalContexts = culturalContexts,
            DataCategories = dataCategories,
            IncludeUserData = includeUserData,
            IncludeBusinessData = includeBusinessData,
            IncludeRevenueData = includeRevenueData,
            IncludeCulturalIntelligence = includeCulturalIntelligence,
            Comments = comments,
        };
    }

    public void UpdateSchedule(DateTime newScheduledTime)
    {
        ScheduledFor = newScheduledTime;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddCulturalContext(string culturalContext)
    {
        if (!CulturalContexts.Contains(culturalContext))
        {
            CulturalContexts.Add(culturalContext);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void AddTargetRegion(GeoRegionEnumDetails region)
    {
        if (!TargetRegions.Any(r => r == region))
        {
            TargetRegions.Add(region);
            UpdatedAt = DateTime.UtcNow;
        }
    }
}

/// <summary>
/// Backup response and status tracking
/// </summary>
public class BackupResponse : BaseEntity
{
    public Guid BackupRequestId { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public bool IsSuccessful { get; private set; }
    public string? ErrorMessage { get; private set; }
    public long BackupSizeBytes { get; private set; }
    public TimeSpan Duration => CompletedAt?.Subtract(StartedAt) ?? DateTime.UtcNow.Subtract(StartedAt);
    public Dictionary<string, long> CategorySizes { get; private set; } = new();
    public Dictionary<string, object> CulturalMetrics { get; private set; } = new();
    public List<string> WarningMessages { get; private set; } = new();
    public string BackupLocation { get; private set; } = string.Empty;
    public string BackupChecksum { get; private set; } = string.Empty;
    public Dictionary<string, string> RegionBackupLocations { get; private set; } = new();

    private BackupResponse() { } // EF Core

    public static BackupResponse Create(Guid backupRequestId, string backupLocation)
    {
        var response = new BackupResponse
        {
            BackupRequestId = backupRequestId,
            StartedAt = DateTime.UtcNow,
            BackupLocation = backupLocation
        };
        return response;
    }

    public void CompleteSuccessfully(long backupSizeBytes, string checksum, Dictionary<string, long> categorySizes)
    {
        CompletedAt = DateTime.UtcNow;
        IsSuccessful = true;
        BackupSizeBytes = backupSizeBytes;
        BackupChecksum = checksum;
        CategorySizes = categorySizes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void CompleteWithError(string errorMessage)
    {
        CompletedAt = DateTime.UtcNow;
        IsSuccessful = false;
        ErrorMessage = errorMessage;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddWarning(string warning)
    {
        WarningMessages.Add(warning);
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddRegionBackupLocation(string regionCode, string location)
    {
        RegionBackupLocations[regionCode] = location;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Disaster recovery configuration and coordination
/// </summary>
public class DisasterRecoveryConfiguration : BaseEntity
{
    public string ConfigurationName { get; private set; } = string.Empty;
    public DisasterType DisasterType { get; private set; }
    public RecoveryStrategy PrimaryStrategy { get; private set; }
    public RecoveryStrategy FallbackStrategy { get; private set; }
    public RecoveryTimeObjective RTO { get; private set; } = null!;
    public RecoveryPointObjective RPO { get; private set; } = null!;
    public GeoRegionEnum PrimaryRegion { get; private set; }
    public List<GeoRegionEnum> SecondaryRegions { get; private set; } = new();
    public Dictionary<CulturalDataPriority, RecoveryStrategy> PriorityStrategies { get; private set; } = new();
    public Dictionary<string, object> CulturalRecoveryRules { get; private set; } = new();
    public bool AutomaticFailover { get; private set; }
    public bool RequireManualApproval { get; private set; }
    public List<string> NotificationContacts { get; private set; } = new();
    public Dictionary<string, string> RecoveryProcedures { get; private set; } = new();

    private DisasterRecoveryConfiguration() { } // EF Core

    public static DisasterRecoveryConfiguration Create(
        string configurationName,
        DisasterType disasterType,
        RecoveryStrategy primaryStrategy,
        RecoveryStrategy fallbackStrategy,
        RecoveryTimeObjective rto,
        RecoveryPointObjective rpo,
        GeoRegionEnum primaryRegion,
        bool automaticFailover = false,
        bool requireManualApproval = true)
    {
        return new DisasterRecoveryConfiguration
        {
            ConfigurationName = configurationName,
            DisasterType = disasterType,
            PrimaryStrategy = primaryStrategy,
            FallbackStrategy = fallbackStrategy,
            RTO = rto,
            RPO = rpo,
            PrimaryRegion = primaryRegion,
            AutomaticFailover = automaticFailover,
            RequireManualApproval = requireManualApproval,
        };
    }

    public void AddSecondaryRegion(GeoRegionEnum region)
    {
        if (!SecondaryRegions.Any(r => r == region))
        {
            SecondaryRegions.Add(region);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void SetPriorityStrategy(CulturalDataPriority priority, RecoveryStrategy strategy)
    {
        PriorityStrategies[priority] = strategy;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddCulturalRecoveryRule(string culturalContext, object rule)
    {
        CulturalRecoveryRules[culturalContext] = rule;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddNotificationContact(string contact)
    {
        if (!NotificationContacts.Contains(contact))
        {
            NotificationContacts.Add(contact);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void SetRecoveryProcedure(string step, string procedure)
    {
        RecoveryProcedures[step] = procedure;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Cultural event backup analysis and metadata
/// </summary>
public class CulturalEventBackupAnalysis : BaseEntity
{
    public Guid EventId { get; private set; }
    public string EventName { get; private set; } = string.Empty;
    public CulturalDataPriority Priority { get; private set; }
    public DateTime EventDate { get; private set; }
    public string CulturalContext { get; private set; } = string.Empty;
    public string GeographicRelevance { get; private set; } = string.Empty;
    public long DataSizeBytes { get; private set; }
    public int UserParticipants { get; private set; }
    public int BusinessParticipants { get; private set; }
    public decimal ExpectedRevenue { get; private set; }
    public DateTime LastBackupDate { get; private set; }
    public BackupType RecommendedBackupType { get; private set; }
    public List<string> DataCategories { get; private set; } = new();
    public Dictionary<string, object> CulturalAttributes { get; private set; } = new();
    public Dictionary<string, long> CategorySizes { get; private set; } = new();
    public bool IsRecurringEvent { get; private set; }
    public string? RecurrencePattern { get; private set; }

    private CulturalEventBackupAnalysis() { } // EF Core

    public static CulturalEventBackupAnalysis Create(
        Guid eventId,
        string eventName,
        CulturalDataPriority priority,
        DateTime eventDate,
        string culturalContext,
        string geographicRelevance)
    {
        return new CulturalEventBackupAnalysis
        {
            EventId = eventId,
            EventName = eventName,
            Priority = priority,
            EventDate = eventDate,
            CulturalContext = culturalContext,
            GeographicRelevance = geographicRelevance,
        };
    }

    public void UpdateDataMetrics(long dataSizeBytes, int userParticipants, int businessParticipants, decimal expectedRevenue)
    {
        DataSizeBytes = dataSizeBytes;
        UserParticipants = userParticipants;
        BusinessParticipants = businessParticipants;
        ExpectedRevenue = expectedRevenue;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetBackupRecommendation(BackupType backupType, DateTime lastBackupDate)
    {
        RecommendedBackupType = backupType;
        LastBackupDate = lastBackupDate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddDataCategory(string category, long sizeBytes)
    {
        if (!DataCategories.Contains(category))
        {
            DataCategories.Add(category);
        }
        CategorySizes[category] = sizeBytes;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetRecurrence(bool isRecurring, string? pattern = null)
    {
        IsRecurringEvent = isRecurring;
        RecurrencePattern = pattern;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Multi-region coordination for disaster recovery
/// </summary>
public class MultiRegionCoordination : BaseEntity
{
    public string CoordinationName { get; private set; } = string.Empty;
    public GeoRegionEnum PrimaryRegion { get; private set; }
    public List<GeoRegionEnum> SecondaryRegions { get; private set; } = new();
    public Dictionary<string, string> RegionStatus { get; private set; } = new();
    public Dictionary<string, DateTime> LastSyncTimes { get; private set; } = new();
    public Dictionary<string, long> DataLagBytes { get; private set; } = new();
    public Dictionary<string, TimeSpan> ReplicationDelays { get; private set; } = new();
    public DateTime LastHealthCheck { get; private set; }
    public bool IsActive { get; private set; }
    public List<string> FailoverHistory { get; private set; } = new();
    public Dictionary<string, object> CrossRegionMetrics { get; private set; } = new();
    public List<string> CulturalAffinityRules { get; private set; } = new();

    private MultiRegionCoordination() { } // EF Core

    public static MultiRegionCoordination Create(
        string coordinationName,
        GeoRegionEnum primaryRegion)
    {
        return new MultiRegionCoordination
        {
            CoordinationName = coordinationName,
            PrimaryRegion = primaryRegion,
            IsActive = true,
            LastHealthCheck = DateTime.UtcNow,
        };
    }

    public void AddSecondaryRegion(GeoRegionEnum region)
    {
        if (!SecondaryRegions.Any(r => r == region))
        {
            SecondaryRegions.Add(region);
            RegionStatus[region.ToString()] = "Active";
            LastSyncTimes[region.ToString()] = DateTime.UtcNow;
            DataLagBytes[region.ToString()] = 0;
            ReplicationDelays[region.ToString()] = TimeSpan.Zero;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void UpdateRegionStatus(string regionCode, string status)
    {
        RegionStatus[regionCode] = status;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateSyncTime(string regionCode, long dataLagBytes, TimeSpan replicationDelay)
    {
        LastSyncTimes[regionCode] = DateTime.UtcNow;
        DataLagBytes[regionCode] = dataLagBytes;
        ReplicationDelays[regionCode] = replicationDelay;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordFailover(string fromRegion, string toRegion, string reason)
    {
        var failoverRecord = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} - Failover from {fromRegion} to {toRegion}: {reason}";
        FailoverHistory.Add(failoverRecord);
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateHealthCheck()
    {
        LastHealthCheck = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddCulturalAffinityRule(string rule)
    {
        if (!CulturalAffinityRules.Contains(rule))
        {
            CulturalAffinityRules.Add(rule);
            UpdatedAt = DateTime.UtcNow;
        }
    }
}

/// <summary>
/// Business continuity plan and execution model
/// </summary>
public class BusinessContinuityPlan : BaseEntity
{
    public string PlanName { get; private set; } = string.Empty;
    public BusinessContinuityState CurrentState { get; private set; }
    public DisasterType TriggeringDisaster { get; private set; }
    public DateTime ActivatedAt { get; private set; }
    public DateTime? DeactivatedAt { get; private set; }
    public List<string> CriticalFunctions { get; private set; } = new();
    public List<string> CulturalServices { get; private set; } = new();
    public List<string> RevenueStreams { get; private set; } = new();
    public Dictionary<string, string> ServiceLevel { get; private set; } = new();
    public Dictionary<string, object> CulturalContinuityRules { get; private set; } = new();
    public List<string> ExecutionSteps { get; private set; } = new();
    public Dictionary<string, bool> StepCompletion { get; private set; } = new();
    public List<string> StakeholderNotifications { get; private set; } = new();
    public string? EstimatedRecoveryTime { get; private set; }
    public decimal EstimatedRevenueLoss { get; private set; }

    private BusinessContinuityPlan() { } // EF Core

    public static BusinessContinuityPlan Create(
        string planName,
        DisasterType triggeringDisaster,
        List<string> criticalFunctions,
        List<string> culturalServices,
        List<string> revenueStreams)
    {
        return new BusinessContinuityPlan
        {
            PlanName = planName,
            TriggeringDisaster = triggeringDisaster,
            CurrentState = BusinessContinuityState.Normal,
            CriticalFunctions = criticalFunctions,
            CulturalServices = culturalServices,
            RevenueStreams = revenueStreams,
        };
    }

    public void Activate()
    {
        CurrentState = BusinessContinuityState.BusinessContinuity;
        ActivatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        CurrentState = BusinessContinuityState.Normal;
        DeactivatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetState(BusinessContinuityState state)
    {
        CurrentState = state;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddExecutionStep(string step)
    {
        if (!ExecutionSteps.Contains(step))
        {
            ExecutionSteps.Add(step);
            StepCompletion[step] = false;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void CompleteStep(string step)
    {
        if (ExecutionSteps.Contains(step))
        {
            StepCompletion[step] = true;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void SetServiceLevel(string service, string level)
    {
        ServiceLevel[service] = level;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddCulturalContinuityRule(string culturalContext, object rule)
    {
        CulturalContinuityRules[culturalContext] = rule;
        UpdatedAt = DateTime.UtcNow;
    }

    public void EstimateImpact(string recoveryTime, decimal revenueLoss)
    {
        EstimatedRecoveryTime = recoveryTime;
        EstimatedRevenueLoss = revenueLoss;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Data integrity validation and metrics
/// </summary>
public class DataIntegrityMetrics : BaseEntity
{
    public string ValidationName { get; private set; } = string.Empty;
    public DataIntegrityCheckType CheckType { get; private set; }
    public DateTime ValidationStarted { get; private set; }
    public DateTime? ValidationCompleted { get; private set; }
    public RecoveryValidationStatus Status { get; private set; }
    public long TotalRecordsValidated { get; private set; }
    public long RecordsWithIssues { get; private set; }
    public long CulturalDataRecords { get; private set; }
    public long UserDataRecords { get; private set; }
    public long BusinessDataRecords { get; private set; }
    public long RevenueDataRecords { get; private set; }
    public Dictionary<string, long> CategoryValidation { get; private set; } = new();
    public Dictionary<string, string> ValidationErrors { get; private set; } = new();
    public Dictionary<string, object> CulturalIntegrityMetrics { get; private set; } = new();
    public List<string> IntegrityIssues { get; private set; } = new();
    public string? RecommendedActions { get; private set; }

    private DataIntegrityMetrics() { } // EF Core

    public static DataIntegrityMetrics Create(
        string validationName,
        DataIntegrityCheckType checkType)
    {
        return new DataIntegrityMetrics
        {
            ValidationName = validationName,
            CheckType = checkType,
            ValidationStarted = DateTime.UtcNow,
            Status = RecoveryValidationStatus.InProgress,
        };
    }

    public void CompleteValidation(
        long totalRecords,
        long recordsWithIssues,
        long culturalRecords,
        long userRecords,
        long businessRecords,
        long revenueRecords)
    {
        ValidationCompleted = DateTime.UtcNow;
        TotalRecordsValidated = totalRecords;
        RecordsWithIssues = recordsWithIssues;
        CulturalDataRecords = culturalRecords;
        UserDataRecords = userRecords;
        BusinessDataRecords = businessRecords;
        RevenueDataRecords = revenueRecords;
        Status = recordsWithIssues == 0 ? RecoveryValidationStatus.ValidationPassed : RecoveryValidationStatus.PartialValidation;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddValidationError(string category, string error)
    {
        ValidationErrors[category] = error;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddIntegrityIssue(string issue)
    {
        IntegrityIssues.Add(issue);
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetCategoryValidation(string category, long validatedRecords)
    {
        CategoryValidation[category] = validatedRecords;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetRecommendedActions(string actions)
    {
        RecommendedActions = actions;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string reason)
    {
        Status = RecoveryValidationStatus.ValidationFailed;
        ValidationCompleted = DateTime.UtcNow;
        AddValidationError("FAILURE", reason);
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Recovery procedure execution tracking
/// </summary>
public class RecoveryProcedure : BaseEntity
{
    public string ProcedureName { get; private set; } = string.Empty;
    public RecoveryStrategy Strategy { get; private set; }
    public DisasterType DisasterType { get; private set; }
    public DateTime InitiatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public RecoveryValidationStatus Status { get; private set; }
    public GeoRegionEnum SourceRegion { get; private set; }
    public GeoRegionEnum TargetRegion { get; private set; }
    public List<string> ExecutionSteps { get; private set; } = new();
    public Dictionary<string, DateTime> StepTimestamps { get; private set; } = new();
    public Dictionary<string, bool> StepCompletion { get; private set; } = new();
    public Dictionary<string, string> StepResults { get; private set; } = new();
    public Dictionary<CulturalDataPriority, bool> PriorityRecoveryStatus { get; private set; } = new();
    public List<string> CulturalContextsRecovered { get; private set; } = new();
    public Dictionary<string, object> RecoveryMetrics { get; private set; } = new();
    public string? ErrorMessage { get; private set; }
    public decimal DataRecoveredPercentage { get; private set; }

    private RecoveryProcedure() { } // EF Core

    public static RecoveryProcedure Create(
        string procedureName,
        RecoveryStrategy strategy,
        DisasterType disasterType,
        GeoRegionEnum sourceRegion,
        GeoRegionEnum targetRegion)
    {
        return new RecoveryProcedure
        {
            ProcedureName = procedureName,
            Strategy = strategy,
            DisasterType = disasterType,
            SourceRegion = sourceRegion,
            TargetRegion = targetRegion,
            InitiatedAt = DateTime.UtcNow,
            Status = RecoveryValidationStatus.InProgress,
        };
    }

    public void AddExecutionStep(string step)
    {
        if (!ExecutionSteps.Contains(step))
        {
            ExecutionSteps.Add(step);
            StepCompletion[step] = false;
            StepTimestamps[step] = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void CompleteStep(string step, string result = "Success")
    {
        if (ExecutionSteps.Contains(step))
        {
            StepCompletion[step] = true;
            StepResults[step] = result;
            StepTimestamps[step] = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void SetPriorityRecoveryStatus(CulturalDataPriority priority, bool isRecovered)
    {
        PriorityRecoveryStatus[priority] = isRecovered;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddCulturalContextRecovered(string culturalContext)
    {
        if (!CulturalContextsRecovered.Contains(culturalContext))
        {
            CulturalContextsRecovered.Add(culturalContext);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void Complete(decimal dataRecoveredPercentage)
    {
        CompletedAt = DateTime.UtcNow;
        DataRecoveredPercentage = dataRecoveredPercentage;
        Status = dataRecoveredPercentage >= 95m ? RecoveryValidationStatus.ValidationPassed : RecoveryValidationStatus.PartialValidation;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Fail(string errorMessage)
    {
        CompletedAt = DateTime.UtcNow;
        Status = RecoveryValidationStatus.ValidationFailed;
        ErrorMessage = errorMessage;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetRecoveryMetric(string metricName, object value)
    {
        RecoveryMetrics[metricName] = value;
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Disaster recovery metrics and monitoring
/// </summary>
public class DisasterRecoveryMetrics : BaseEntity
{
    public DateTime MetricTimestamp { get; private set; }
    public TimeSpan ActualRTO { get; private set; }
    public TimeSpan ActualRPO { get; private set; }
    public TimeSpan TargetRTO { get; private set; }
    public TimeSpan TargetRPO { get; private set; }
    public decimal AvailabilityPercentage { get; private set; }
    public decimal DataRecoveredPercentage { get; private set; }
    public int TotalDisasterEvents { get; private set; }
    public int SuccessfulRecoveries { get; private set; }
    public int FailedRecoveries { get; private set; }
    public Dictionary<DisasterType, int> DisasterTypeCounts { get; private set; } = new();
    public Dictionary<CulturalDataPriority, decimal> PriorityRecoveryRates { get; private set; } = new();
    public Dictionary<string, TimeSpan> RegionRecoveryTimes { get; private set; } = new();
    public Dictionary<string, object> CulturalRecoveryMetrics { get; private set; } = new();
    public decimal EstimatedRevenueLoss { get; private set; }
    public decimal ActualRevenueLoss { get; private set; }
    public List<string> LessonsLearned { get; private set; } = new();

    private DisasterRecoveryMetrics() { } // EF Core

    public static DisasterRecoveryMetrics Create(
        TimeSpan targetRTO,
        TimeSpan targetRPO)
    {
        return new DisasterRecoveryMetrics
        {
            MetricTimestamp = DateTime.UtcNow,
            TargetRTO = targetRTO,
            TargetRPO = targetRPO,
        };
    }

    public void RecordDisasterEvent(
        DisasterType disasterType,
        TimeSpan actualRTO,
        TimeSpan actualRPO,
        bool wasSuccessful,
        decimal dataRecoveredPercentage)
    {
        ActualRTO = actualRTO;
        ActualRPO = actualRPO;
        DataRecoveredPercentage = dataRecoveredPercentage;
        TotalDisasterEvents++;

        if (wasSuccessful)
            SuccessfulRecoveries++;
        else
            FailedRecoveries++;

        DisasterTypeCounts[disasterType] = DisasterTypeCounts.GetValueOrDefault(disasterType, 0) + 1;
        MetricTimestamp = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAvailabilityPercentage(decimal percentage)
    {
        AvailabilityPercentage = percentage;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPriorityRecoveryRate(CulturalDataPriority priority, decimal rate)
    {
        PriorityRecoveryRates[priority] = rate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetRegionRecoveryTime(string regionCode, TimeSpan recoveryTime)
    {
        RegionRecoveryTimes[regionCode] = recoveryTime;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordRevenueLoss(decimal estimated, decimal actual)
    {
        EstimatedRevenueLoss = estimated;
        ActualRevenueLoss = actual;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddLessonLearned(string lesson)
    {
        if (!LessonsLearned.Contains(lesson))
        {
            LessonsLearned.Add(lesson);
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void SetCulturalRecoveryMetric(string metricName, object value)
    {
        CulturalRecoveryMetrics[metricName] = value;
        UpdatedAt = DateTime.UtcNow;
    }
}

#endregion