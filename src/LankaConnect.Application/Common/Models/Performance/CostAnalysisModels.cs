using System;
using System.Collections.Generic;

namespace LankaConnect.Application.Common.Models.Performance;

/// <summary>
/// Cost Analysis Models - Application Layer
/// Moved from Stage5MissingTypes.cs to correct architectural layer
/// </summary>

#region Cost Analysis

/// <summary>
/// Cost analysis parameters for financial monitoring
/// </summary>
public class CostAnalysisParameters
{
    public string AnalysisId { get; set; } = string.Empty;
    public DateTime AnalysisPeriodStart { get; set; }
    public DateTime AnalysisPeriodEnd { get; set; }
    public List<string> CostCategories { get; set; } = new();
    public string CurrencyCode { get; set; } = "USD";
}

/// <summary>
/// Cost performance analysis result
/// </summary>
public class CostPerformanceAnalysis
{
    public string AnalysisId { get; set; } = string.Empty;
    public decimal TotalCost { get; set; }
    public double PerformanceScore { get; set; }
    public double CostEfficiencyRatio { get; set; }
    public List<string> Recommendations { get; set; } = new();
}

#endregion

#region Revenue Analysis

/// <summary>
/// Revenue calculation model for financial projections
/// </summary>
public class RevenueCalculationModel
{
    public string ModelId { get; set; } = string.Empty;
    public string ModelType { get; set; } = string.Empty;
    public Dictionary<string, decimal> RevenueStreams { get; set; } = new();
    public decimal ProjectedRevenue { get; set; }
}

/// <summary>
/// Revenue risk calculation result
/// </summary>
public class RevenueRiskCalculation
{
    public string CalculationId { get; set; } = string.Empty;
    public decimal RevenueAtRisk { get; set; }
    public double RiskProbability { get; set; }
    public List<string> RiskFactors { get; set; } = new();
}

#endregion

#region Scaling and Optimization

/// <summary>
/// Scaling threshold optimization result
/// </summary>
public class ScalingThresholdOptimization
{
    public string OptimizationId { get; set; } = string.Empty;
    public Dictionary<string, double> OptimizedThresholds { get; set; } = new();
    public double ImprovementPercentage { get; set; }
    public List<string> ChangedThresholds { get; set; } = new();
}

/// <summary>
/// Market position analysis for competitive intelligence
/// </summary>
public class MarketPositionAnalysis
{
    public string AnalysisId { get; set; } = string.Empty;
    public double MarketShare { get; set; }
    public int CompetitiveRank { get; set; }
    public List<string> Strengths { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
}

#endregion
