namespace LankaConnect.Application.Common.Models.Performance;

/// <summary>
/// Represents the impact of cultural events on system load and performance
/// </summary>
public class CulturalLoadImpact
{
    /// <summary>
    /// Cultural event causing the load impact
    /// </summary>
    public string CulturalEventName { get; set; } = string.Empty;

    /// <summary>
    /// Load multiplier factor (e.g., 3.5 for 3.5x normal load)
    /// </summary>
    public decimal LoadMultiplier { get; set; }

    /// <summary>
    /// Affected regions or communities
    /// </summary>
    public List<string> AffectedRegions { get; set; } = new();

    /// <summary>
    /// Duration of the impact
    /// </summary>
    public TimeSpan ImpactDuration { get; set; }

    /// <summary>
    /// Peak impact timestamp
    /// </summary>
    public DateTime PeakImpactTime { get; set; }

    /// <summary>
    /// Type of cultural impact (Festival, Religious, Memorial, Celebration)
    /// </summary>
    public string ImpactType { get; set; } = string.Empty;

    /// <summary>
    /// Confidence level of the impact prediction
    /// </summary>
    public decimal ConfidenceLevel { get; set; }

    /// <summary>
    /// Mitigation strategies recommended for this impact
    /// </summary>
    public List<string> MitigationStrategies { get; set; } = new();
}