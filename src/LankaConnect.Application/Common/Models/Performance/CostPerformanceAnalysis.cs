namespace LankaConnect.Application.Common.Models.Performance;

/// <summary>
/// Result of cost-performance analysis with cultural intelligence insights
/// </summary>
public class CostPerformanceAnalysis
{
    /// <summary>
    /// Unique identifier for this analysis
    /// </summary>
    public Guid AnalysisId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Total cost for the analysis period
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Cost broken down by service type
    /// </summary>
    public Dictionary<string, decimal> CostByService { get; set; } = new();

    /// <summary>
    /// Performance metrics corresponding to costs
    /// </summary>
    public Dictionary<string, decimal> PerformanceMetrics { get; set; } = new();

    /// <summary>
    /// Cost efficiency ratio (performance per dollar)
    /// </summary>
    public decimal CostEfficiencyRatio { get; set; }

    /// <summary>
    /// Cultural event cost impact analysis
    /// </summary>
    public Dictionary<string, decimal> CulturalEventCostImpact { get; set; } = new();

    /// <summary>
    /// Recommendations for cost optimization
    /// </summary>
    public List<string> OptimizationRecommendations { get; set; } = new();

    /// <summary>
    /// Timestamp when analysis was performed
    /// </summary>
    public DateTime AnalysisTimestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Forecasted costs for next period based on cultural intelligence
    /// </summary>
    public decimal ForecastedCost { get; set; }
}