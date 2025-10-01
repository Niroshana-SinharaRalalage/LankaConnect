using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.Models.Critical;

/// <summary>
/// Critical types static class for backup and disaster recovery operations
/// Provides static type definitions for system-critical operations
/// </summary>
public static class CriticalTypes
{
    /// <summary>
    /// Critical backup operation types for disaster recovery
    /// </summary>
    public enum CriticalBackupType
    {
        Incremental = 1,
        Differential = 2,
        Full = 3,
        CulturalDataComplete = 4,
        SystemSnapshot = 5
    }

    /// <summary>
    /// Critical recovery operation priorities
    /// </summary>
    public enum CriticalRecoveryPriority
    {
        Low = 1,
        Standard = 2,
        High = 3,
        Critical = 4,
        Emergency = 5
    }

    /// <summary>
    /// Critical system validation modes
    /// </summary>
    public enum CriticalValidationMode
    {
        Basic = 1,
        Standard = 2,
        Comprehensive = 3,
        Cultural = 4,
        Enterprise = 5
    }

    /// <summary>
    /// Critical operation status types
    /// </summary>
    public enum CriticalOperationStatus
    {
        Pending = 1,
        InProgress = 2,
        Completed = 3,
        Failed = 4,
        Cancelled = 5
    }

    /// <summary>
    /// Data integrity validation scope for backup operations
    /// </summary>
    public enum DataIntegrityValidationScope
    {
        Basic = 1,
        Standard = 2,
        Comprehensive = 3,
        Enterprise = 4,
        Cultural = 5
    }

    /// <summary>
    /// Consistency check level for data validation
    /// </summary>
    public enum ConsistencyCheckLevel
    {
        Surface = 1,
        Standard = 2,
        Deep = 3,
        Comprehensive = 4,
        Exhaustive = 5
    }

    /// <summary>
    /// Integrity monitoring configuration for backup validation
    /// </summary>
    public enum IntegrityMonitoringConfiguration
    {
        Basic = 1,
        Standard = 2,
        Advanced = 3,
        Enterprise = 4,
        RealTime = 5
    }
}

/// <summary>
/// Critical operation configuration for backup and recovery
/// </summary>
public class CriticalOperationConfiguration
{
    public required string ConfigurationId { get; set; }
    public required string OperationName { get; set; }
    public required CriticalTypes.CriticalBackupType BackupType { get; set; }
    public required CriticalTypes.CriticalRecoveryPriority Priority { get; set; }
    public required CriticalTypes.CriticalValidationMode ValidationMode { get; set; }
    public required TimeSpan MaxExecutionTime { get; set; }
    public required Dictionary<string, object> OperationParameters { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsEnabled { get; set; } = true;
    public string? Description { get; set; }
}

/// <summary>
/// Critical operation result for tracking system-critical operations
/// </summary>
public class CriticalOperationResult
{
    public required string ResultId { get; set; }
    public required string OperationId { get; set; }
    public required CriticalTypes.CriticalOperationStatus Status { get; set; }
    public required DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public required TimeSpan ExecutionDuration { get; set; } = TimeSpan.Zero;
    public required bool IsSuccessful { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, object> ResultMetadata { get; set; } = new();
    public string? ResultSummary { get; set; }
}