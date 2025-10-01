using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Cultural intelligence specific backup priority levels
/// </summary>
public enum CulturalBackupPriority
{
    Low,
    Medium,
    High,
    Critical,
    Sacred
}

/// <summary>
/// Interface for cultural intelligence-aware backup operations
/// </summary>
public interface ICulturalIntelligenceBackupEngine
{
    /// <summary>
    /// Performs cultural intelligence backup operations
    /// </summary>
    /// <param name="backupConfiguration">Configuration for the backup operation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the backup operation</returns>
    Task<Result<CulturalIntelligenceBackupResult>> PerformBackupAsync(
        CulturalIntelligenceBackupConfiguration backupConfiguration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates cultural data before backup
    /// </summary>
    /// <param name="culturalData">Cultural data to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<Result<CulturalDataValidationResult>> ValidateCulturalDataAsync(
        CulturalIntelligenceData culturalData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves backup status for cultural intelligence operations
    /// </summary>
    /// <param name="backupId">Backup identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Backup status result</returns>
    Task<Result<CulturalIntelligenceBackupDetails>> GetBackupStatusAsync(
        string backupId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Configuration for cultural intelligence backup operations
/// </summary>
public class CulturalIntelligenceBackupConfiguration
{
    public required string BackupId { get; set; }
    public required string BackupName { get; set; }
    public required List<string> CulturalDataSources { get; set; }
    public required CulturalBackupPriority Priority { get; set; }
    public required bool IncludeSacredContent { get; set; }
    public required Dictionary<string, object> BackupMetadata { get; set; }
    public DateTime ScheduledTime { get; set; }
    public TimeSpan RetentionPeriod { get; set; }
    public string? BackupLocation { get; set; }
}

/// <summary>
/// Result of cultural intelligence backup operation
/// </summary>
public class CulturalIntelligenceBackupResult
{
    public required string BackupId { get; set; }
    public required bool IsSuccessful { get; set; }
    public required DateTime BackupTimestamp { get; set; }
    public required TimeSpan BackupDuration { get; set; }
    public required long BackupSizeBytes { get; set; }
    public required int CulturalRecordsBackedUp { get; set; }
    public List<string> BackupErrors { get; set; } = new();
    public Dictionary<string, object> BackupMetrics { get; set; } = new();
    public string? BackupLocation { get; set; }
}

/// <summary>
/// Cultural intelligence data for backup operations
/// </summary>
public class CulturalIntelligenceData
{
    public required string DataId { get; set; }
    public required string DataType { get; set; }
    public required Dictionary<string, object> CulturalAttributes { get; set; }
    public required bool IsSacredContent { get; set; }
    public required List<string> CulturalTags { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CulturalContext { get; set; }
}

/// <summary>
/// Result of cultural data validation
/// </summary>
public class CulturalDataValidationResult
{
    public required bool IsValid { get; set; }
    public required List<string> ValidationErrors { get; set; }
    public required List<string> ValidationWarnings { get; set; }
    public required Dictionary<string, object> ValidationMetrics { get; set; }
    public DateTime ValidationTimestamp { get; set; }
}

/// <summary>
/// Details of cultural intelligence backup operation
/// </summary>
public class CulturalIntelligenceBackupDetails
{
    public required string BackupId { get; set; }
    public required CulturalIntelligenceBackupStatus Status { get; set; }
    public required DateTime StartTime { get; set; }
    public required double ProgressPercentage { get; set; }
    public required string CurrentOperation { get; set; }
    public DateTime? CompletionTime { get; set; }
    public TimeSpan? EstimatedTimeRemaining { get; set; }
    public List<string> StatusMessages { get; set; } = new();
}


/// <summary>
/// Backup operation status
/// </summary>
public enum BackupStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Cancelled,
    Paused
}