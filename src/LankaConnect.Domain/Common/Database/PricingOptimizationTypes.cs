using System;
using System.Collections.Generic;

namespace LankaConnect.Domain.Common.Database
{
    public class PricingOptimizationConfiguration
    {
        public Guid Id { get; set; }
        public string PricingStrategy { get; set; } = string.Empty;
        public Dictionary<string, decimal> PricingRules { get; set; } = new();
        public List<string> OptimizationCriteria { get; set; } = new();
        public DateTime ConfigurationDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class PricingOptimizationResult
    {
        public Guid Id { get; set; }
        public bool IsSuccessful { get; set; }
        public Dictionary<string, decimal> OptimizedPrices { get; set; } = new();
        public decimal ExpectedRevenueIncrease { get; set; }
        public List<string> PricingRecommendations { get; set; } = new();
        public DateTime OptimizationDate { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class DynamicPricingConfiguration
    {
        public Guid Id { get; set; }
        public string PricingModel { get; set; } = string.Empty;
        public Dictionary<string, object> DynamicFactors { get; set; } = new();
        public TimeSpan UpdateInterval { get; set; }
        public decimal MinPrice { get; set; }
        public decimal MaxPrice { get; set; }
        public bool IsEnabled { get; set; }
    }

    public class DynamicPricingResult
    {
        public Guid Id { get; set; }
        public Dictionary<string, decimal> AdjustedPrices { get; set; } = new();
        public string AdjustmentReason { get; set; } = string.Empty;
        public DateTime AdjustmentTimestamp { get; set; }
        public double ConfidenceScore { get; set; }
        public List<string> MarketFactorsConsidered { get; set; } = new();
    }

    public class ROICalculationParameters
    {
        public Guid Id { get; set; }
        public decimal InitialInvestment { get; set; }
        public DateTime CalculationPeriod { get; set; }
        public List<string> CostFactors { get; set; } = new();
        public List<string> BenefitFactors { get; set; } = new();
        public Dictionary<string, decimal> AdditionalMetrics { get; set; } = new();
    }

    public class ROICalculationResult
    {
        public Guid Id { get; set; }
        public decimal ROIPercentage { get; set; }
        public decimal NetProfit { get; set; }
        public decimal TotalCosts { get; set; }
        public decimal TotalBenefits { get; set; }
        public DateTime CalculationDate { get; set; }
        public List<string> ROIBreakdown { get; set; } = new();
    }
}