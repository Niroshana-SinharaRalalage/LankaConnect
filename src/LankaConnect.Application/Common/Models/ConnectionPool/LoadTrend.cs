namespace LankaConnect.Application.Common.Models.ConnectionPool;

/// <summary>
/// Represents a load trend analysis for cultural intelligence-aware scaling
/// </summary>
public class LoadTrend
{
    /// <summary>
    /// Timestamp for this trend data point
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Current load level (0.0 to 1.0 where 1.0 is maximum capacity)
    /// </summary>
    public decimal CurrentLoad { get; set; }

    /// <summary>
    /// Predicted load for next period
    /// </summary>
    public decimal PredictedLoad { get; set; }

    /// <summary>
    /// Trend direction (Increasing, Decreasing, Stable)
    /// </summary>
    public string TrendDirection { get; set; } = "Stable";

    /// <summary>
    /// Rate of change in load per minute
    /// </summary>
    public decimal LoadChangeRate { get; set; }

    /// <summary>
    /// Cultural event driving the trend
    /// </summary>
    public string? CulturalEventDriver { get; set; }

    /// <summary>
    /// Geographic region for this trend
    /// </summary>
    public string? GeographicRegion { get; set; }
}