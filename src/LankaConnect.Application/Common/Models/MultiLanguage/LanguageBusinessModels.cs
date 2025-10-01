using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Models.MultiLanguage
{
    public class LanguageRevenueAnalysisResult : BaseEntity
    {
        public string AnalysisId { get; set; } = string.Empty;
        public string LanguagePair { get; set; } = string.Empty;
        public decimal TotalRevenue { get; set; }
        public decimal RevenueGrowth { get; set; }
        public int TransactionsProcessed { get; set; }
        public decimal AverageTransactionValue { get; set; }
        public string TopPerformingServices { get; set; } = string.Empty;
        public List<string> RevenueOptimizationRecommendations { get; set; } = new();
        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Completed";
    }

    public class BusinessLanguageMatchingRequest : BaseEntity
    {
        public string BusinessId { get; set; } = string.Empty;
        public string BusinessType { get; set; } = string.Empty;
        public List<string> RequiredLanguages { get; set; } = new();
        public string ServiceCategory { get; set; } = string.Empty;
        public string TargetMarket { get; set; } = string.Empty;
        public string ComplianceRequirements { get; set; } = string.Empty;
        public decimal BudgetRange { get; set; }
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public string RequestedBy { get; set; } = string.Empty;
    }

    public class BusinessLanguageMatchingResult : BaseEntity
    {
        public string RequestId { get; set; } = string.Empty;
        public List<string> RecommendedTranslators { get; set; } = new();
        public List<string> RecommendedAgencies { get; set; } = new();
        public decimal MatchScore { get; set; }
        public List<string> CertificationRecommendations { get; set; } = new();
        public decimal EstimatedCost { get; set; }
        public TimeSpan EstimatedTimeframe { get; set; }
        public string ComplianceVerification { get; set; } = string.Empty;
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }

    public class PremiumContentStrategy : BaseEntity
    {
        public string StrategyName { get; set; } = string.Empty;
        public string TargetLanguage { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public List<string> PremiumFeatures { get; set; } = new();
        public decimal PricingTier { get; set; }
        public string QualityStandards { get; set; } = string.Empty;
        public List<string> CertificationRequirements { get; set; } = new();
        public bool RequiresHumanReview { get; set; } = true;
        public new DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}