using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Models.MultiLanguage
{
    public class CommunityLanguageProfileUpdate : BaseEntity
    {
        public string CommunityId { get; set; } = string.Empty;
        public string PrimaryLanguage { get; set; } = string.Empty;
        public List<string> SecondaryLanguages { get; set; } = new();
        public decimal ProficiencyLevel { get; set; }
        public string CulturalContext { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public bool IsVerified { get; set; }
        public string UpdatedBy { get; set; } = string.Empty;
    }

    public class LanguageRoutingQuery : BaseEntity
    {
        public string SourceLanguage { get; set; } = string.Empty;
        public string TargetLanguage { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public List<string> PreferredDialects { get; set; } = new();
        public bool RequiresCertification { get; set; }
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public string UserId { get; set; } = string.Empty;
    }

    public class LanguageRoutingQueryResult : BaseEntity
    {
        public string QueryId { get; set; } = string.Empty;
        public List<string> RecommendedTranslators { get; set; } = new();
        public decimal MatchScore { get; set; }
        public TimeSpan EstimatedCompletionTime { get; set; }
        public decimal EstimatedCost { get; set; }
        public List<string> CulturalNotes { get; set; } = new();
        public bool RequiresReview { get; set; }
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }

    public class DatabaseOptimizationStrategy : BaseEntity
    {
        public string StrategyName { get; set; } = string.Empty;
        public string TargetLanguagePair { get; set; } = string.Empty;
        public List<string> OptimizationTechniques { get; set; } = new();
        public decimal ExpectedPerformanceGain { get; set; }
        public string IndexStrategy { get; set; } = string.Empty;
        public bool EnablePartitioning { get; set; }
        public DateTime ImplementationDate { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class DatabasePerformanceAnalysis : BaseEntity
    {
        public string AnalysisId { get; set; } = string.Empty;
        public string LanguagePair { get; set; } = string.Empty;
        public decimal QueryPerformanceScore { get; set; }
        public TimeSpan AverageResponseTime { get; set; }
        public long RecordsProcessed { get; set; }
        public List<string> PerformanceBottlenecks { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
        public bool RequiresOptimization { get; set; }
    }
}