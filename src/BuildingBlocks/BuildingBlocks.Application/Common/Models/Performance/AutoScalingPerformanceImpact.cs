using System;
using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.BuildingBlocks.Application.Common.Models.Performance;

public class AutoScalingPerformanceImpact : LegacyBaseEntity
{
    public Guid ImpactId { get; set; } = Guid.NewGuid();
    public decimal PerformanceImprovement { get; set; } = 0;
    public decimal CostImpact { get; set; } = 0;
    public TimeSpan ScalingDuration { get; set; } = TimeSpan.FromMinutes(5);
}