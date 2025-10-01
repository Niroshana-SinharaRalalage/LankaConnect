using System;
using System.Collections.Generic;

namespace LankaConnect.Domain.Common.Performance
{
    /// <summary>
    /// Cost constraints for scaling decisions
    /// </summary>
    public class CostConstraints
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ConstraintId { get; set; } = string.Empty;
        public decimal MaxHourlyCost { get; set; }
        public decimal MaxDailyCost { get; set; }
        public decimal MaxMonthlyCost { get; set; }
        public decimal CostPerInstance { get; set; }
        public string CostModel { get; set; } = string.Empty;
        public Dictionary<string, decimal> ResourceCosts { get; set; } = new();
        public bool IsEnforced { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Cost-aware scaling decision result
    /// </summary>
    public class CostAwareScalingDecision
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string DecisionId { get; set; } = string.Empty;
        public string PerformanceRequirement { get; set; } = string.Empty;
        public CostConstraints CostConstraints { get; set; } = new();
        public bool ScalingApproved { get; set; }
        public int RecommendedInstances { get; set; }
        public decimal EstimatedCost { get; set; }
        public decimal CostBenefit { get; set; }
        public string DecisionReason { get; set; } = string.Empty;
        public List<string> AlternativeOptions { get; set; } = new();
        public DateTimeOffset DecisionTime { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Application tier configuration
    /// </summary>
    public class ApplicationTier
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string TierId { get; set; } = string.Empty;
        public string TierName { get; set; } = string.Empty;
        public string TierType { get; set; } = string.Empty;
        public int Priority { get; set; }
        public Dictionary<string, object> Configuration { get; set; } = new();
        public List<string> Dependencies { get; set; } = new();
        public bool IsActive { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Strategy for coordinating scaling across tiers
    /// </summary>
    public class ScalingCoordinationStrategy
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string StrategyId { get; set; } = string.Empty;
        public string StrategyName { get; set; } = string.Empty;
        public string CoordinationType { get; set; } = string.Empty;
        public Dictionary<string, int> TierPriorities { get; set; } = new();
        public TimeSpan CoordinationDelay { get; set; }
        public bool ParallelExecution { get; set; }
        public List<string> CoordinationRules { get; set; } = new();
        public bool IsEnabled { get; set; } = true;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Multi-tier scaling coordination result
    /// </summary>
    public class MultiTierScalingCoordination
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string CoordinationId { get; set; } = string.Empty;
        public List<ApplicationTier> ApplicationTiers { get; set; } = new();
        public ScalingCoordinationStrategy Strategy { get; set; } = new();
        public Dictionary<string, string> TierStatuses { get; set; } = new();
        public bool IsCoordinated { get; set; }
        public List<string> CoordinationActions { get; set; } = new();
        public string OverallStatus { get; set; } = string.Empty;
        public DateTimeOffset CoordinatedAt { get; set; } = DateTimeOffset.UtcNow;
    }
}