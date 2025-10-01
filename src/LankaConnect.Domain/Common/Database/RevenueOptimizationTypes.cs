using System;
using System.Collections.Generic;

namespace LankaConnect.Domain.Common.Database
{
    public class RevenueOptimizationParameters
    {
        public Guid Id { get; set; }
        public DateTime OptimizationPeriod { get; set; }
        public decimal TargetRevenue { get; set; }
        public List<string> OptimizationFactors { get; set; } = new();
        public string Region { get; set; } = string.Empty;
        public Dictionary<string, object> AdditionalParameters { get; set; } = new();
    }

    public class RevenueOptimizationResult
    {
        public Guid Id { get; set; }
        public bool IsSuccessful { get; set; }
        public decimal ProjectedRevenue { get; set; }
        public decimal ActualRevenue { get; set; }
        public double OptimizationPercentage { get; set; }
        public List<string> RecommendedActions { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class RevenueAnalysisParameters
    {
        public Guid Id { get; set; }
        public DateTime AnalysisStartDate { get; set; }
        public DateTime AnalysisEndDate { get; set; }
        public List<string> AnalysisMetrics { get; set; } = new();
        public string AnalysisScope { get; set; } = string.Empty;
        public Dictionary<string, decimal> ThresholdValues { get; set; } = new();
    }

    public class RevenueImpactAnalysis
    {
        public Guid Id { get; set; }
        public decimal CurrentRevenue { get; set; }
        public decimal ProjectedImpact { get; set; }
        public double ImpactPercentage { get; set; }
        public List<string> ImpactFactors { get; set; } = new();
        public string RiskAssessment { get; set; } = string.Empty;
        public DateTime AnalysisDate { get; set; }
    }

    public class RevenueOpportunityPredictionRequest
    {
        public Guid Id { get; set; }
        public string MarketSegment { get; set; } = string.Empty;
        public DateTime PredictionPeriod { get; set; }
        public List<string> OpportunityTypes { get; set; } = new();
        public Dictionary<string, object> MarketData { get; set; } = new();
    }

    public class RevenueOpportunityPrediction
    {
        public Guid Id { get; set; }
        public List<string> IdentifiedOpportunities { get; set; } = new();
        public decimal PotentialRevenue { get; set; }
        public double ConfidenceScore { get; set; }
        public string RecommendedStrategy { get; set; } = string.Empty;
        public DateTime PredictionDate { get; set; }
    }
}