using System;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Models.Performance;

/// <summary>
/// Revenue calculation model for performance impact analysis
/// TDD Implementation: Calculates revenue impact from performance issues
/// </summary>
public class RevenueCalculationModel : BaseEntity
{
    public Guid ModelId { get; set; } = Guid.NewGuid();
    public string ModelName { get; set; } = string.Empty;
    public decimal BaselineRevenue { get; set; } = 0;
    public decimal RevenuePerUserHour { get; set; } = 0;
    public decimal ChurnRateImpact { get; set; } = 0;
    public Dictionary<DegradationType, decimal> ImpactMultipliers { get; set; } = new();
    public List<RevenueStream> RevenueStreams { get; set; } = new();
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}

public class RevenueStream
{
    public string StreamName { get; set; } = string.Empty;
    public decimal Value { get; set; } = 0;
    public RevenueCategory Category { get; set; } = RevenueCategory.Core;
    public decimal PerformanceSensitivity { get; set; } = 0.5m;
}

public enum RevenueCategory
{
    Core = 1,
    Premium = 2,
    Advertisement = 3,
    Transaction = 4,
    Subscription = 5
}