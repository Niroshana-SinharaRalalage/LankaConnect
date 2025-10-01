namespace LankaConnect.Application.Common.Models.Performance;

/// <summary>
/// Parameters for database cost analysis with cultural intelligence awareness
/// </summary>
public class CostAnalysisParameters
{
    /// <summary>
    /// Time period for cost analysis
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date for cost analysis period
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Cultural regions to include in cost analysis
    /// </summary>
    public List<string> CulturalRegions { get; set; } = new();

    /// <summary>
    /// Types of costs to analyze (Compute, Storage, Bandwidth, Cultural_Intelligence)
    /// </summary>
    public List<string> CostTypes { get; set; } = new();

    /// <summary>
    /// Currency for cost reporting
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Whether to include cultural event cost spikes in analysis
    /// </summary>
    public bool IncludeCulturalEventCosts { get; set; } = true;

    /// <summary>
    /// Granularity of cost breakdown (Hourly, Daily, Weekly, Monthly)
    /// </summary>
    public string Granularity { get; set; } = "Daily";
}