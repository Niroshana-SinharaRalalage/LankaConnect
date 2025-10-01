using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.Models.Performance;

/// <summary>
/// Dynamic adjustment triggers for performance optimization
/// Defines conditions that trigger automatic system adjustments
/// </summary>
public class DynamicAdjustmentTriggers
{
    public required string TriggerId { get; set; }
    public required string TriggerName { get; set; }
    public required List<TriggerCondition> Conditions { get; set; }
    public required TriggerOperator Operator { get; set; }
    public required TimeSpan EvaluationWindow { get; set; }
    public required int MinimumOccurrences { get; set; }
    public required TriggerSeverity Severity { get; set; }
    public required List<string> TargetComponents { get; set; }
    public required Dictionary<string, object> TriggerParameters { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? Description { get; set; }
    public List<string> NotificationTargets { get; set; } = new();

    public DynamicAdjustmentTriggers()
    {
        Conditions = new List<TriggerCondition>();
        TargetComponents = new List<string>();
        TriggerParameters = new Dictionary<string, object>();
        NotificationTargets = new List<string>();
    }

    public bool ShouldTrigger(Dictionary<string, double> currentMetrics)
    {
        return Operator switch
        {
            TriggerOperator.And => Conditions.All(c => c.IsConditionMet(currentMetrics)),
            TriggerOperator.Or => Conditions.Any(c => c.IsConditionMet(currentMetrics)),
            _ => false
        };
    }
}

/// <summary>
/// Trigger condition for dynamic adjustments
/// </summary>
public class TriggerCondition
{
    public required string ConditionId { get; set; }
    public required string MetricName { get; set; }
    public required TriggerComparisonOperator ComparisonOperator { get; set; }
    public required double ThresholdValue { get; set; }
    public required TimeSpan SustainedDuration { get; set; }
    public string? Description { get; set; }

    public bool IsConditionMet(Dictionary<string, double> metrics)
    {
        if (!metrics.ContainsKey(MetricName)) return false;

        var currentValue = metrics[MetricName];
        return ComparisonOperator switch
        {
            TriggerComparisonOperator.GreaterThan => currentValue > ThresholdValue,
            TriggerComparisonOperator.LessThan => currentValue < ThresholdValue,
            TriggerComparisonOperator.Equals => Math.Abs(currentValue - ThresholdValue) < 0.001,
            TriggerComparisonOperator.GreaterThanOrEqual => currentValue >= ThresholdValue,
            TriggerComparisonOperator.LessThanOrEqual => currentValue <= ThresholdValue,
            _ => false
        };
    }
}

/// <summary>
/// Trigger operator for combining conditions
/// </summary>
public enum TriggerOperator
{
    And = 1,
    Or = 2
}

/// <summary>
/// Trigger comparison operators
/// </summary>
public enum TriggerComparisonOperator
{
    GreaterThan = 1,
    LessThan = 2,
    Equals = 3,
    GreaterThanOrEqual = 4,
    LessThanOrEqual = 5
}

/// <summary>
/// Trigger severity levels
/// </summary>
public enum TriggerSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4,
    Emergency = 5
}