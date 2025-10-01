namespace LankaConnect.Application.Common.Models.ConnectionPool;

/// <summary>
/// Result of cultural intelligence-aware scaling prediction
/// </summary>
public class ScalingPredictionResult
{
    /// <summary>
    /// Recommended number of instances for optimal performance
    /// </summary>
    public int RecommendedInstanceCount { get; set; }

    /// <summary>
    /// Confidence level of the prediction (0.0 to 1.0)
    /// </summary>
    public decimal PredictionConfidence { get; set; }

    /// <summary>
    /// Cultural event that triggered this prediction
    /// </summary>
    public string? CulturalEventTrigger { get; set; }

    /// <summary>
    /// Time window for which this prediction is valid
    /// </summary>
    public TimeSpan PredictionValidityWindow { get; set; }
}