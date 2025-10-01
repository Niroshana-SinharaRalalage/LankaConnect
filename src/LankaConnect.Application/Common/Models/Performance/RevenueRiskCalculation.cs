using System;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Models.Performance;

/// <summary>
/// Revenue risk calculation result for performance monitoring
/// TDD Implementation: Quantifies revenue risk from performance issues
/// </summary>
public class RevenueRiskCalculation : BaseEntity
{
    public Guid CalculationId { get; set; } = Guid.NewGuid();
    public Guid ScenarioId { get; set; }
    public decimal EstimatedRevenueLoss { get; set; } = 0;
    public decimal ConfidenceLevel { get; set; } = 0.95m;
    public RiskLevel RiskLevel { get; set; } = RiskLevel.Medium;
    public Dictionary<string, decimal> ComponentRisks { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan ValidFor { get; set; } = TimeSpan.FromHours(24);
}

public enum RiskLevel
{
    VeryLow = 1,
    Low = 2,
    Medium = 3,
    High = 4,
    Critical = 5
}