namespace LankaConnect.Domain.Common.Database;

/// <summary>
/// Represents cultural activity patterns for backup scheduling and disaster recovery planning
/// </summary>
public class CulturalActivityPattern
{
    /// <summary>
    /// Unique identifier for the activity pattern
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Name of the cultural activity pattern (e.g., "Vesak_Day_Pattern", "Diwali_Week_Pattern")
    /// </summary>
    public string PatternName { get; set; } = string.Empty;

    /// <summary>
    /// Cultural community associated with this pattern
    /// </summary>
    public string CulturalCommunity { get; set; } = string.Empty;

    /// <summary>
    /// Type of cultural activity (Religious, Festival, Cultural, Memorial)
    /// </summary>
    public string ActivityType { get; set; } = string.Empty;

    /// <summary>
    /// Expected frequency of this pattern (Annual, Monthly, Lunar, Solar)
    /// </summary>
    public string Frequency { get; set; } = string.Empty;

    /// <summary>
    /// Peak activity hours during the cultural event
    /// </summary>
    public TimeSpan PeakStartTime { get; set; }

    /// <summary>
    /// Duration of peak activity
    /// </summary>
    public TimeSpan PeakDuration { get; set; }

    /// <summary>
    /// Priority level for backup and disaster recovery (1=Critical, 5=Low)
    /// </summary>
    public int Priority { get; set; } = 3;

    /// <summary>
    /// Geographic regions where this pattern is significant
    /// </summary>
    public List<string> SignificantRegions { get; set; } = new();

    /// <summary>
    /// Whether this pattern affects backup windows
    /// </summary>
    public bool AffectsBackupScheduling { get; set; } = true;
}