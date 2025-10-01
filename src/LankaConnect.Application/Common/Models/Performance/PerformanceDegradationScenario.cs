using System;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.Models.Performance;

/// <summary>
/// Performance degradation scenario model for revenue impact analysis
/// TDD Implementation: Models performance degradation scenarios
/// </summary>
public class PerformanceDegradationScenario : BaseEntity
{
    public Guid ScenarioId { get; set; } = Guid.NewGuid();
    public string ScenarioName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DegradationType Type { get; set; } = DegradationType.Latency;
    public decimal ImpactPercentage { get; set; } = 0;
    public TimeSpan ExpectedDuration { get; set; } = TimeSpan.FromMinutes(15);
    public List<string> AffectedServices { get; set; } = new();
    public Dictionary<string, decimal> MetricThresholds { get; set; } = new();
}

public enum DegradationType
{
    Latency = 1,
    Throughput = 2,
    ErrorRate = 3,
    Availability = 4,
    ResourceUtilization = 5
}