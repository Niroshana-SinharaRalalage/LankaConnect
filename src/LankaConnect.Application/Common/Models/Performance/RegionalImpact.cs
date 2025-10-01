namespace LankaConnect.Application.Common.Models.Performance;

/// <summary>
/// Represents the impact of cultural events on specific geographic regions
/// </summary>
public class RegionalImpact
{
    /// <summary>
    /// Geographic region identifier
    /// </summary>
    public string RegionId { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable region name
    /// </summary>
    public string RegionName { get; set; } = string.Empty;

    /// <summary>
    /// Primary cultural communities in this region
    /// </summary>
    public List<string> CulturalCommunities { get; set; } = new();

    /// <summary>
    /// Impact severity level (1-10 scale)
    /// </summary>
    public int ImpactSeverity { get; set; }

    /// <summary>
    /// Expected load increase percentage
    /// </summary>
    public decimal LoadIncreasePercentage { get; set; }

    /// <summary>
    /// Duration of the impact in this region
    /// </summary>
    public TimeSpan ImpactDuration { get; set; }

    /// <summary>
    /// Mitigation strategies specific to this region
    /// </summary>
    public List<string> MitigationStrategies { get; set; } = new();

    /// <summary>
    /// Cultural events driving the impact
    /// </summary>
    public List<string> DrivingEvents { get; set; } = new();

    /// <summary>
    /// Population affected in this region
    /// </summary>
    public long AffectedPopulation { get; set; }
}