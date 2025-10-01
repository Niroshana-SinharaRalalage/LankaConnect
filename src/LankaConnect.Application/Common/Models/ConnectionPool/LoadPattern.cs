namespace LankaConnect.Application.Common.Models.ConnectionPool;

/// <summary>
/// Represents a load pattern for cultural intelligence-aware scaling
/// </summary>
public class LoadPattern
{
    /// <summary>
    /// Unique identifier for the load pattern
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Name of the load pattern (e.g., "Vesak_Peak", "Diwali_Surge")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Cultural event associated with this pattern
    /// </summary>
    public string? CulturalEvent { get; set; }

    /// <summary>
    /// Expected traffic multiplier (e.g., 5.0 for 5x normal load)
    /// </summary>
    public decimal TrafficMultiplier { get; set; }

    /// <summary>
    /// Duration of the pattern
    /// </summary>
    public TimeSpan Duration { get; set; }

    /// <summary>
    /// Time when this pattern typically starts
    /// </summary>
    public TimeSpan StartOffset { get; set; }

    /// <summary>
    /// Confidence level of the pattern prediction
    /// </summary>
    public decimal ConfidenceLevel { get; set; }
}