using System;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Models.Performance;

/// <summary>
/// Performance impact threshold model for revenue risk analysis
/// TDD Implementation: Defines thresholds for performance impact assessment
/// </summary>
public class PerformanceImpactThreshold : BaseEntity
{
    public Guid ThresholdId { get; set; } = Guid.NewGuid();
    public string ThresholdName { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public decimal WarningThreshold { get; set; } = 0;
    public decimal CriticalThreshold { get; set; } = 0;
    public ThresholdType Type { get; set; } = ThresholdType.Latency;
    public string Unit { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}

public enum ThresholdType
{
    Latency = 1,
    Throughput = 2,
    ErrorRate = 3,
    ResponseTime = 4,
    ResourceUtilization = 5
}