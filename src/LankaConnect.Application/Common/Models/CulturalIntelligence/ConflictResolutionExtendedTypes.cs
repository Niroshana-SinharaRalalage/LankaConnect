namespace LankaConnect.Application.Common.Models.CulturalIntelligence;

public class AdaptiveResolutionStrategies
{
    public required string StrategySetId { get; set; }
    public required List<string> AvailableStrategies { get; set; }
    public required Dictionary<string, double> StrategyEffectiveness { get; set; }
    public required string AdaptationCriteria { get; set; }
    public required Dictionary<string, object> ContextualFactors { get; set; }
    public bool MachineLearningOptimized { get; set; }
    public Dictionary<string, List<string>> StrategyDependencies { get; set; } = new();
}

public class AdaptiveStrategyRequest
{
    public required string RequestId { get; set; }
    public required string ConflictContext { get; set; }
    public required Dictionary<string, object> ContextualData { get; set; }
    public required List<string> PreferredStrategies { get; set; }
    public required string AdaptationLevel { get; set; }
    public DateTime RequestTimestamp { get; set; }
    public Dictionary<string, double> ConstraintWeights { get; set; } = new();
}

public class BridgeBuildingRecommendations
{
    public required string RecommendationId { get; set; }
    public required List<string> BridgeStrategies { get; set; }
    public required Dictionary<string, object> ImplementationSteps { get; set; }
    public required double SuccessProbability { get; set; }
    public required List<string> RequiredResources { get; set; }
    public required TimeSpan EstimatedTimeframe { get; set; }
    public Dictionary<string, string> CulturalConsiderations { get; set; } = new();
    public List<string> PotentialRisks { get; set; } = new();
}

public class BridgeBuildingRequest
{
    public required string RequestId { get; set; }
    public required List<string> InvolvedCommunities { get; set; }
    public required Dictionary<string, object> ConflictContext { get; set; }
    public required string BridgeType { get; set; }
    public required List<string> DesiredOutcomes { get; set; }
    public DateTime RequestTimestamp { get; set; }
    public Dictionary<string, double> CommunityReadiness { get; set; } = new();
    public string? PreferredMediationStyle { get; set; }
}

public class CommunitySentimentAnalysisRequest
{
    public required string AnalysisId { get; set; }
    public required List<string> TargetCommunities { get; set; }
    public required string AnalysisScope { get; set; }
    public required DateTime AnalysisPeriod { get; set; }
    public required List<string> SentimentDimensions { get; set; }
    public Dictionary<string, object> AnalysisParameters { get; set; } = new();
    public List<string> DataSources { get; set; } = new();
}

public class CommunitySentimentAnalysisResult
{
    public required string AnalysisId { get; set; }
    public required Dictionary<string, double> SentimentScores { get; set; }
    public required Dictionary<string, object> SentimentTrends { get; set; }
    public required DateTime AnalysisTimestamp { get; set; }
    public required List<string> KeySentimentDrivers { get; set; }
    public Dictionary<string, string> CommunityInsights { get; set; } = new();
    public List<string> RecommendedActions { get; set; } = new();
}

public class RevenueAnalysisParameters
{
    public required string AnalysisId { get; set; }
    public required List<string> RevenueStreams { get; set; }
    public required DateTime AnalysisPeriodStart { get; set; }
    public required DateTime AnalysisPeriodEnd { get; set; }
    public required Dictionary<string, double> RevenueWeights { get; set; }
    public Dictionary<string, object> MarketFactors { get; set; } = new();
    public List<string> AnalysisDimensions { get; set; } = new();
}

public class RevenueImpactAnalysis
{
    public required string AnalysisId { get; set; }
    public required Dictionary<string, double> ImpactMetrics { get; set; }
    public required double TotalRevenueImpact { get; set; }
    public required DateTime AnalysisTimestamp { get; set; }
    public required List<string> ImpactFactors { get; set; }
    public Dictionary<string, double> RevenueProjections { get; set; } = new();
    public string? RiskAssessment { get; set; }
}

public class RevenueOpportunityPrediction
{
    public required string PredictionId { get; set; }
    public required Dictionary<string, double> OpportunityScores { get; set; }
    public required List<string> PredictedOpportunities { get; set; }
    public required DateTime PredictionTimestamp { get; set; }
    public required double ConfidenceLevel { get; set; }
    public Dictionary<string, object> OpportunityDetails { get; set; } = new();
    public TimeSpan OpportunityTimeframe { get; set; }
}

public class RevenueOpportunityPredictionRequest
{
    public required string RequestId { get; set; }
    public required List<string> MarketSegments { get; set; }
    public required Dictionary<string, double> CurrentMetrics { get; set; }
    public required TimeSpan PredictionHorizon { get; set; }
    public required string PredictionModel { get; set; }
    public DateTime RequestTimestamp { get; set; }
    public Dictionary<string, object> MarketContext { get; set; } = new();
}

public class RevenueOptimizationParameters
{
    public required string OptimizationId { get; set; }
    public required Dictionary<string, double> CurrentRevenue { get; set; }
    public required Dictionary<string, double> TargetRevenue { get; set; }
    public required List<string> OptimizationLevers { get; set; }
    public required string OptimizationStrategy { get; set; }
    public Dictionary<string, object> Constraints { get; set; } = new();
    public TimeSpan OptimizationTimeframe { get; set; }
}

public class RevenueOptimizationResult
{
    public required bool OptimizationSuccessful { get; set; }
    public required Dictionary<string, double> OptimizedRevenue { get; set; }
    public required double RevenueIncrease { get; set; }
    public required DateTime OptimizationTimestamp { get; set; }
    public required List<string> OptimizationActions { get; set; }
    public Dictionary<string, object> PerformanceMetrics { get; set; } = new();
    public string? OptimizationLimitations { get; set; }
}
