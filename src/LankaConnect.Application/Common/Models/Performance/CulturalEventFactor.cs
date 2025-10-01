namespace LankaConnect.Application.Common.Models.Performance;

/// <summary>
/// Factor that represents how cultural events influence system performance
/// </summary>
public class CulturalEventFactor
{
    /// <summary>
    /// Name of the cultural event
    /// </summary>
    public string EventName { get; set; } = string.Empty;

    /// <summary>
    /// Factor multiplier (1.0 = no impact, >1.0 = increased load, <1.0 = decreased load)
    /// </summary>
    public decimal FactorMultiplier { get; set; } = 1.0m;

    /// <summary>
    /// Cultural communities affected by this factor
    /// </summary>
    public List<string> AffectedCommunities { get; set; } = new();

    /// <summary>
    /// Geographic regions where this factor applies
    /// </summary>
    public List<string> ApplicableRegions { get; set; } = new();

    /// <summary>
    /// Duration for which this factor is relevant
    /// </summary>
    public TimeSpan FactorDuration { get; set; }

    /// <summary>
    /// Type of performance impact (Load, Response_Time, Memory, CPU)
    /// </summary>
    public string ImpactType { get; set; } = string.Empty;

    /// <summary>
    /// Confidence level of this factor's accuracy
    /// </summary>
    public decimal ConfidenceLevel { get; set; } = 0.8m;
}