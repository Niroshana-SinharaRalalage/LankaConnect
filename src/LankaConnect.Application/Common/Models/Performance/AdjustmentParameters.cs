using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.Models.Performance;

/// <summary>
/// Adjustment parameters for performance optimization and dynamic scaling
/// Defines configurable parameters for automated system adjustments
/// </summary>
public class AdjustmentParameters
{
    public required string ParameterId { get; set; }
    public required string ParameterName { get; set; }
    public required AdjustmentParameterType ParameterType { get; set; }
    public required AdjustmentScope Scope { get; set; }
    public required Dictionary<string, object> ParameterValues { get; set; }
    public required AdjustmentParameterSettings Settings { get; set; }
    public required TimeSpan AdjustmentInterval { get; set; }
    public required AdjustmentDirection Direction { get; set; }
    public required double MinimumValue { get; set; }
    public required double MaximumValue { get; set; }
    public required double StepSize { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? Description { get; set; }
    public List<AdjustmentConstraint> Constraints { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();

    public AdjustmentParameters()
    {
        ParameterValues = new Dictionary<string, object>();
        Constraints = new List<AdjustmentConstraint>();
        Metadata = new Dictionary<string, string>();
    }

    public bool IsWithinBounds(double value)
    {
        return value >= MinimumValue && value <= MaximumValue;
    }

    public double CalculateNextValue(double currentValue, AdjustmentDirection direction)
    {
        var nextValue = direction switch
        {
            AdjustmentDirection.Increase => currentValue + StepSize,
            AdjustmentDirection.Decrease => currentValue - StepSize,
            _ => currentValue
        };

        return Math.Max(MinimumValue, Math.Min(MaximumValue, nextValue));
    }
}

/// <summary>
/// Adjustment parameter types
/// </summary>
public enum AdjustmentParameterType
{
    Performance = 1,
    Capacity = 2,
    Resource = 3,
    Threshold = 4,
    Scaling = 5,
    Configuration = 6
}

/// <summary>
/// Adjustment scope
/// </summary>
public enum AdjustmentScope
{
    System = 1,
    Component = 2,
    Service = 3,
    Database = 4,
    Network = 5,
    Application = 6
}

/// <summary>
/// Adjustment direction
/// </summary>
public enum AdjustmentDirection
{
    Increase = 1,
    Decrease = 2,
    Maintain = 3,
    Auto = 4
}

/// <summary>
/// Adjustment parameter settings
/// </summary>
public class AdjustmentParameterSettings
{
    public required bool AutomaticAdjustment { get; set; }
    public required double SensitivityThreshold { get; set; }
    public required TimeSpan CooldownPeriod { get; set; }
    public required int MaxAdjustmentsPerPeriod { get; set; }
    public bool RequiresApproval { get; set; }
    public string? ApprovalWorkflow { get; set; }
    public List<string> NotificationChannels { get; set; } = new();
}

/// <summary>
/// Adjustment constraint
/// </summary>
public class AdjustmentConstraint
{
    public required string ConstraintId { get; set; }
    public required AdjustmentConstraintType Type { get; set; }
    public required string Description { get; set; }
    public required Dictionary<string, object> ConstraintRules { get; set; }
    public bool IsActive { get; set; } = true;
    public AdjustmentConstraintSeverity Severity { get; set; } = AdjustmentConstraintSeverity.Medium;
}

/// <summary>
/// Adjustment constraint types
/// </summary>
public enum AdjustmentConstraintType
{
    Minimum = 1,
    Maximum = 2,
    Range = 3,
    Dependency = 4,
    Business = 5,
    Technical = 6
}

/// <summary>
/// Adjustment constraint severity
/// </summary>
public enum AdjustmentConstraintSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}