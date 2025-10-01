using System;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Models.Performance;

/// <summary>
/// Churn risk analysis model for performance impact assessment
/// TDD Implementation: Analyzes customer churn risk from performance issues
/// </summary>
public class ChurnRiskAnalysis : BaseEntity
{
    public Guid AnalysisId { get; set; } = Guid.NewGuid();
    public DateTime AnalysisDate { get; set; } = DateTime.UtcNow;
    public decimal OverallChurnRisk { get; set; } = 0;
    public Dictionary<string, decimal> SegmentRisks { get; set; } = new();
    public List<ChurnRiskFactor> RiskFactors { get; set; } = new();
    public decimal ConfidenceLevel { get; set; } = 0.95m;
    public TimeSpan PredictionWindow { get; set; } = TimeSpan.FromDays(30);
}

public class ChurnRiskFactor
{
    public string FactorName { get; set; } = string.Empty;
    public decimal Impact { get; set; } = 0;
    public decimal Weight { get; set; } = 1.0m;
    public string Description { get; set; } = string.Empty;
}