namespace LankaConnect.Application.Common.Models.Performance;

/// <summary>
/// Recommended preparation actions for cultural events and performance optimization
/// </summary>
public class RecommendedPreparation
{
    /// <summary>
    /// Unique identifier for this preparation recommendation
    /// </summary>
    public Guid PreparationId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Cultural event this preparation is for
    /// </summary>
    public string CulturalEvent { get; set; } = string.Empty;

    /// <summary>
    /// Type of preparation (Scale_Up, Cache_Warm, Database_Optimize, Content_Prepare)
    /// </summary>
    public string PreparationType { get; set; } = string.Empty;

    /// <summary>
    /// Detailed preparation steps
    /// </summary>
    public List<string> PreparationSteps { get; set; } = new();

    /// <summary>
    /// How far in advance this preparation should be done
    /// </summary>
    public TimeSpan AdvanceNotice { get; set; }

    /// <summary>
    /// Priority level for this preparation
    /// </summary>
    public int Priority { get; set; } = 1;

    /// <summary>
    /// Expected resource requirements for preparation
    /// </summary>
    public Dictionary<string, string> ResourceRequirements { get; set; } = new();

    /// <summary>
    /// Success criteria for measuring preparation effectiveness
    /// </summary>
    public List<string> SuccessCriteria { get; set; } = new();

    /// <summary>
    /// Estimated cost of this preparation
    /// </summary>
    public decimal EstimatedCost { get; set; }
}