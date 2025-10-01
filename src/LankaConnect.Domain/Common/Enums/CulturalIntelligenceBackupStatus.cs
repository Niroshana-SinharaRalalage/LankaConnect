namespace LankaConnect.Domain.Common.Enums;

/// <summary>
/// Status of cultural intelligence backup operations
/// </summary>
public enum CulturalIntelligenceBackupStatus
{
    /// <summary>
    /// Backup operation is pending
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Backup operation is in progress
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// Backup completed successfully
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Backup failed with errors
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Backup is partially completed with warnings
    /// </summary>
    PartiallyCompleted = 4,

    /// <summary>
    /// Backup was cancelled by user or system
    /// </summary>
    Cancelled = 5,

    /// <summary>
    /// Backup is scheduled for future execution
    /// </summary>
    Scheduled = 6,

    /// <summary>
    /// Backup requires validation before completion
    /// </summary>
    ValidationRequired = 7,

    /// <summary>
    /// Backup contains sacred content requiring special handling
    /// </summary>
    SacredContentPending = 8,

    /// <summary>
    /// Backup archived and moved to long-term storage
    /// </summary>
    Archived = 9
}