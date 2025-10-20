using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Communications.ValueObjects;

namespace LankaConnect.Domain.Shared;

/// <summary>
/// Sacred event snapshot for backup operations
/// </summary>
public class SacredEventSnapshot
{
    public required string SnapshotId { get; set; }
    public required string EventId { get; set; }
    public required string EventName { get; set; }
    public required DateTime EventDate { get; set; }
    public required LankaConnect.Domain.Common.Database.CulturalDataPriority PriorityLevel { get; set; }
    public required List<SouthAsianLanguage> Languages { get; set; } = new();
    public required Dictionary<string, object> EventData { get; set; } = new();
    public required DateTime SnapshotTimestamp { get; set; }
    public bool IsReligiousEvent { get; set; }
    public string? CulturalCommunity { get; set; }
    public Dictionary<string, object> RegionalVariations { get; set; } = new();
}

/// <summary>
/// Backup result for general backup operations
/// </summary>
public class BackupResult
{
    public bool Success { get; set; }
    public required string BackupId { get; set; }
    public required DateTime BackupTimestamp { get; set; }
    public required long BackupSizeBytes { get; set; }
    public required TimeSpan BackupDuration { get; set; }
    public required List<string> BackedUpComponents { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public required string BackupLocation { get; set; }
    public Dictionary<string, object> BackupMetadata { get; set; } = new();
}

/// <summary>
/// Backup data container for cultural intelligence data
/// </summary>
public class BackupData
{
    public required string DataId { get; set; }
    public required string DataType { get; set; }
    public required DateTime DataTimestamp { get; set; }
    public required Dictionary<string, object> Data { get; set; } = new();
    public required long DataSizeBytes { get; set; }
    public required string Checksum { get; set; }
    public bool IsEncrypted { get; set; }
    public List<string> DataTags { get; set; } = new();
    public Dictionary<string, object> DataMetadata { get; set; } = new();
}

/// <summary>
/// Cultural intelligence backup configuration
/// </summary>
public class CulturalIntelligenceBackupConfiguration
{
    public required string ConfigurationId { get; set; }
    public required string ConfigurationName { get; set; }
    public required List<CulturalDataType> DataTypesToBackup { get; set; } = new();
    public required TimeSpan BackupFrequency { get; set; }
    public required int RetentionDays { get; set; }
    public required string BackupLocation { get; set; }
    public bool EncryptBackups { get; set; } = true;
    public bool CrossRegionReplication { get; set; }
    public required List<string> BackupRegions { get; set; } = new();
    public Dictionary<string, object> BackupParameters { get; set; } = new();
}

/// <summary>
/// Cultural intelligence backup result
/// </summary>
public class CulturalIntelligenceBackupResult
{
    public bool Success { get; set; }
    public required string BackupId { get; set; }
    public required DateTime BackupTimestamp { get; set; }
    public required List<CulturalDataType> BackedUpDataTypes { get; set; } = new();
    public required long TotalBackupSizeBytes { get; set; }
    public required int RecordsBackedUp { get; set; }
    public required decimal CulturalIntegrityScore { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> BackupWarnings { get; set; } = new();
    public Dictionary<string, object> BackupMetadata { get; set; } = new();
}

// CulturalIntelligenceBackupStatus is now an enum imported from Domain.Common.Enums

/// <summary>
/// Detailed cultural intelligence backup information
/// </summary>
public class CulturalIntelligenceBackupInfo
{
    public required string BackupId { get; set; }
    public required CulturalIntelligenceBackupStatus Status { get; set; }
    public required DateTime StartTime { get; set; }
    public DateTime? CompletionTime { get; set; }
    public required decimal ProgressPercentage { get; set; }
    public required string CurrentOperation { get; set; }
    public List<string> CompletedOperations { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public Dictionary<string, object> StatusMetadata { get; set; } = new();
}

/// <summary>
/// Cultural intelligence data container
/// </summary>
public class CulturalIntelligenceData
{
    public required string DataId { get; set; }
    public required CulturalDataType DataType { get; set; }
    public required DateTime DataTimestamp { get; set; }
    public required Dictionary<string, object> Data { get; set; } = new();
    public required List<SouthAsianLanguage> Languages { get; set; } = new();
    public required LankaConnect.Domain.Common.Database.CulturalDataPriority Priority { get; set; }
    public bool IsVerified { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Cultural data validation result
/// </summary>
public class CulturalDataValidationResult
{
    public bool IsValid { get; set; }
    public required string ValidationId { get; set; }
    public required DateTime ValidationTimestamp { get; set; }
    public required decimal ValidationScore { get; set; }
    public required List<string> ValidationErrors { get; set; } = new();
    public required List<string> ValidationWarnings { get; set; } = new();
    public string? ValidatorNotes { get; set; }
    public Dictionary<string, object> ValidationMetadata { get; set; } = new();
    public decimal CulturalIntegrityScore { get; set; }
}

/// <summary>
/// Sacred event for cultural intelligence backup operations
/// </summary>
public class SacredEvent
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string EventType { get; set; }
    public required string CulturalCommunity { get; set; }
    public required LankaConnect.Domain.Common.Database.CulturalDataPriority SacredPriorityLevel { get; set; }
    public required DateTime StartDate { get; set; }
    public required DateTime EndDate { get; set; }
    public List<string> RegionalVariations { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Cultural backup result for cultural intelligence operations
/// </summary>
public class CulturalBackupResult
{
    public required CulturalContext CulturalContext { get; set; }
    public required CulturalBackupStrategy BackupStrategy { get; set; }
    public required CulturalDataValidationResult DataIntegrity { get; set; }
    public bool Success { get; set; }
    public required DateTime CompletionTime { get; set; }
    public required string BackupId { get; set; }
}

/// <summary>
/// Sacred event backup result for high-priority cultural events
/// </summary>
public class SacredEventBackupResult
{
    public required SacredEvent SacredEvent { get; set; }
    public required CulturalBackupStrategy BackupStrategy { get; set; }
    public required CulturalDataValidationResult SacredContentIntegrity { get; set; }
    public required SacredEventSnapshot Snapshot { get; set; }
    public bool Success { get; set; }
    public required DateTime CompletionTime { get; set; }
}

/// <summary>
/// Cultural backup strategy for cultural intelligence operations
/// </summary>
public class CulturalBackupStrategy
{
    public BackupPriority Priority { get; set; }
    public BackupType Type { get; set; }
    public BackupFrequency Frequency { get; set; }
    public RetentionPolicy RetentionPolicy { get; set; }
    public CulturalContext? CulturalContext { get; set; }
    public SacredEvent? SacredEvent { get; set; }
    public required TimeSpan TargetRTO { get; set; }
    public required TimeSpan TargetRPO { get; set; }
    public List<string> SpecialRequirements { get; set; } = new();
}

/// <summary>
/// Cultural backup schedule for planned backup operations
/// </summary>
public class CulturalBackupSchedule
{
    public required DateTime ScheduledTime { get; set; }
    public required CulturalEvent CulturalEvent { get; set; }
    public required BackupType BackupType { get; set; }
    public required BackupPriority Priority { get; set; }
    public required string Description { get; set; }
    public bool IsPreEvent { get; set; }
    public bool IsDuringEvent { get; set; }
    public bool IsPostEvent { get; set; }
    public BackupFrequency Frequency { get; set; }
}

/// <summary>
/// Backup priority enumeration
/// </summary>
public enum BackupPriority
{
    Standard,
    Medium,
    High,
    Critical
}

/// <summary>
/// Backup type enumeration
/// </summary>
public enum BackupType
{
    Full,
    Incremental,
    Differential
}

/// <summary>
/// Backup frequency enumeration
/// </summary>
public enum BackupFrequency
{
    Continuous,
    Every15Minutes,
    Every30Minutes,
    Hourly,
    Daily,
    Weekly
}

/// <summary>
/// Retention policy enumeration
/// </summary>
public enum RetentionPolicy
{
    Standard,
    LongTerm,
    Permanent
}