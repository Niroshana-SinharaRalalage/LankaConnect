namespace LankaConnect.Application.Common.Models.Performance;

/// <summary>
/// Represents a performance metric with cultural intelligence context
/// </summary>
public class PerformanceMetric
{
    /// <summary>
    /// Name of the metric (CPU_Usage, Memory_Usage, Response_Time, Cultural_Load)
    /// </summary>
    public string MetricName { get; set; } = string.Empty;

    /// <summary>
    /// Current value of the metric
    /// </summary>
    public decimal CurrentValue { get; set; }

    /// <summary>
    /// Unit of measurement (Percentage, Milliseconds, Count, MB)
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when metric was recorded
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Baseline value for comparison
    /// </summary>
    public decimal BaselineValue { get; set; }

    /// <summary>
    /// Threshold value for alerting
    /// </summary>
    public decimal AlertThreshold { get; set; }

    /// <summary>
    /// Cultural event that may be influencing this metric
    /// </summary>
    public string? CulturalEventContext { get; set; }

    /// <summary>
    /// Geographic region where this metric was recorded
    /// </summary>
    public string? GeographicRegion { get; set; }

    /// <summary>
    /// Trend direction (Increasing, Decreasing, Stable)
    /// </summary>
    public string TrendDirection { get; set; } = "Stable";
}