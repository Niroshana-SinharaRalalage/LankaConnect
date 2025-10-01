namespace LankaConnect.Application.Common.Models.Backup;

/// <summary>
/// Result of backup scheduling operations with cultural intelligence awareness
/// </summary>
public class BackupScheduleResult
{
    /// <summary>
    /// Unique identifier for the scheduled backup
    /// </summary>
    public Guid ScheduleId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Whether the scheduling was successful
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Scheduled backup start time
    /// </summary>
    public DateTime ScheduledStartTime { get; set; }

    /// <summary>
    /// Expected duration of the backup
    /// </summary>
    public TimeSpan EstimatedDuration { get; set; }

    /// <summary>
    /// Cultural events that influenced the scheduling
    /// </summary>
    public List<string> CulturalEventConsiderations { get; set; } = new();

    /// <summary>
    /// Backup priority level adjusted for cultural significance
    /// </summary>
    public int CulturalPriorityLevel { get; set; } = 1;

    /// <summary>
    /// Error message if scheduling failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Resources allocated for this backup
    /// </summary>
    public Dictionary<string, string> AllocatedResources { get; set; } = new();

    /// <summary>
    /// Whether this backup can be rescheduled automatically
    /// </summary>
    public bool CanReschedule { get; set; } = true;
}