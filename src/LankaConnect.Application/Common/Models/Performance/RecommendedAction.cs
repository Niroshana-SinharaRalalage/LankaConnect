namespace LankaConnect.Application.Common.Models.Performance;

/// <summary>
/// Represents a recommended action for performance optimization with cultural intelligence
/// </summary>
public class RecommendedAction
{
    /// <summary>
    /// Unique identifier for the recommendation
    /// </summary>
    public Guid ActionId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Type of action (Scale_Up, Scale_Down, Optimize_Query, Cache_Warm, Cultural_Prepare)
    /// </summary>
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// Priority level of the action (Low, Medium, High, Critical)
    /// </summary>
    public string Priority { get; set; } = "Medium";

    /// <summary>
    /// Description of the recommended action
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Expected impact of implementing this action
    /// </summary>
    public string ExpectedImpact { get; set; } = string.Empty;

    /// <summary>
    /// Estimated cost of implementing the action
    /// </summary>
    public decimal EstimatedCost { get; set; }

    /// <summary>
    /// Cultural context that triggered this recommendation
    /// </summary>
    public string? CulturalContext { get; set; }

    /// <summary>
    /// Recommended implementation timeline
    /// </summary>
    public TimeSpan ImplementationTimeline { get; set; }

    /// <summary>
    /// Dependencies that must be addressed before implementing
    /// </summary>
    public List<string> Dependencies { get; set; } = new();
}