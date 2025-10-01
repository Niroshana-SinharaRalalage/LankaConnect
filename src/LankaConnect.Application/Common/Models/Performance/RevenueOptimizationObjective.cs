using System;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Models.Performance;

/// <summary>
/// Revenue optimization objective model for performance management
/// TDD Implementation: Defines objectives for revenue optimization
/// </summary>
public class RevenueOptimizationObjective : BaseEntity
{
    public Guid ObjectiveId { get; set; } = Guid.NewGuid();
    public string ObjectiveName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal TargetRevenueIncrease { get; set; } = 0;
    public ObjectiveType Type { get; set; } = ObjectiveType.Performance;
    public DateTime TargetDate { get; set; } = DateTime.UtcNow.AddMonths(3);
    public ObjectivePriority Priority { get; set; } = ObjectivePriority.Medium;
    public List<string> KPIs { get; set; } = new();
    public Dictionary<string, decimal> Metrics { get; set; } = new();
}

public enum ObjectiveType
{
    Performance = 1,
    Quality = 2,
    Efficiency = 3,
    Retention = 4,
    Growth = 5
}

public enum ObjectivePriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}