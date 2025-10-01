namespace LankaConnect.Application.Common.Models.Performance;

/// <summary>
/// A specific data point in load prediction analysis
/// </summary>
public class LoadPredictionPoint
{
    /// <summary>
    /// Timestamp for this prediction point
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Predicted load value (0.0 to 1.0 where 1.0 is maximum capacity)
    /// </summary>
    public decimal PredictedLoad { get; set; }

    /// <summary>
    /// Confidence level of this prediction (0.0 to 1.0)
    /// </summary>
    public decimal ConfidenceLevel { get; set; }

    /// <summary>
    /// Cultural event influencing this prediction
    /// </summary>
    public string? CulturalEventInfluence { get; set; }

    /// <summary>
    /// Historical data points that contributed to this prediction
    /// </summary>
    public List<string> ContributingFactors { get; set; } = new();

    /// <summary>
    /// Geographic region this prediction applies to
    /// </summary>
    public string? GeographicRegion { get; set; }

    /// <summary>
    /// Variance range for the prediction
    /// </summary>
    public decimal PredictionVariance { get; set; }

    /// <summary>
    /// Recommended actions based on this prediction point
    /// </summary>
    public List<string> RecommendedActions { get; set; } = new();
}