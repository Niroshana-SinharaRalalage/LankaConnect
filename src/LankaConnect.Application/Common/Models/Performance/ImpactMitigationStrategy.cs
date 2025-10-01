namespace LankaConnect.Application.Common.Models.Performance;

/// <summary>
/// Strategy for mitigating cultural load impact on system performance
/// </summary>
public class ImpactMitigationStrategy
{
    /// <summary>
    /// Unique identifier for the mitigation strategy
    /// </summary>
    public Guid StrategyId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Name of the mitigation strategy
    /// </summary>
    public string StrategyName { get; set; } = string.Empty;

    /// <summary>
    /// Type of mitigation (Scaling, Caching, Load_Balancing, Cultural_Scheduling)
    /// </summary>
    public string MitigationType { get; set; } = string.Empty;

    /// <summary>
    /// Priority level for implementing this strategy
    /// </summary>
    public int Priority { get; set; } = 1;

    /// <summary>
    /// Cultural events this strategy is designed for
    /// </summary>
    public List<string> TargetCulturalEvents { get; set; } = new();

    /// <summary>
    /// Implementation steps for the strategy
    /// </summary>
    public List<string> ImplementationSteps { get; set; } = new();

    /// <summary>
    /// Expected effectiveness percentage
    /// </summary>
    public decimal EffectivenessPercentage { get; set; }

    /// <summary>
    /// Cost of implementing this strategy
    /// </summary>
    public decimal ImplementationCost { get; set; }

    /// <summary>
    /// Time required to implement the strategy
    /// </summary>
    public TimeSpan ImplementationTime { get; set; }

    /// <summary>
    /// Success criteria for measuring effectiveness
    /// </summary>
    public List<string> SuccessCriteria { get; set; } = new();
}