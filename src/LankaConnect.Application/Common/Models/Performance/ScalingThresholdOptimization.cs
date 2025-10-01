using System;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Models.Performance;

public class ScalingThresholdOptimization : BaseEntity
{
    public Guid OptimizationId { get; set; } = Guid.NewGuid();
    public Dictionary<string, decimal> OptimalThresholds { get; set; } = new();
    public decimal EfficiencyGain { get; set; } = 0;
}