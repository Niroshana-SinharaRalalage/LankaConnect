using System;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Models.Performance;

public class AutoScalingPerformanceImpact : BaseEntity
{
    public Guid ImpactId { get; set; } = Guid.NewGuid();
    public decimal PerformanceImprovement { get; set; } = 0;
    public decimal CostImpact { get; set; } = 0;
    public TimeSpan ScalingDuration { get; set; } = TimeSpan.FromMinutes(5);
}