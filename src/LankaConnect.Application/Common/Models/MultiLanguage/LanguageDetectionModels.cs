using System;
using System.Collections.Generic;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.Database;

namespace LankaConnect.Application.Common.Models.MultiLanguage;

/// <summary>
/// Language detection result with confidence scores
/// </summary>
public class LanguageDetectionResult
{
    public Guid UserId { get; set; }
    public SouthAsianLanguage PrimaryLanguage { get; set; }
    public decimal ConfidenceScore { get; set; }
    public Dictionary<SouthAsianLanguage, decimal> LanguageScores { get; set; } = new();
    public DateTime DetectionTimestamp { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public string AnalyzedContent { get; set; } = string.Empty;
}

/// <summary>
/// Generational language pattern analysis for diaspora communities
/// </summary>
public class GenerationalPatternAnalysis
{
    public int Generation { get; set; } // 1st, 2nd, 3rd+ generation
    public SouthAsianLanguage HeritageLanguage { get; set; }
    public SouthAsianLanguage DominantLanguage { get; set; }
    public decimal HeritageLanguageRetention { get; set; }
    public Dictionary<SouthAsianLanguage, decimal> GenerationalPreferences { get; set; } = new();
    public List<string> CulturalContextFactors { get; set; } = new();
    public bool RequiresHeritageLanguageSupport { get; set; }
}

/// <summary>
/// Language complexity analysis for script optimization
/// </summary>
public class LanguageComplexityAnalysis
{
    public Dictionary<SouthAsianLanguage, ScriptComplexity> ScriptComplexities { get; set; } = new();
    public List<string> RequiredFonts { get; set; } = new();
    public bool RequiresRightToLeftSupport { get; set; }
    public bool RequiresComplexScriptRendering { get; set; }
    public decimal OverallComplexityScore { get; set; }
}

/// <summary>
/// Script complexity information
/// </summary>
public class ScriptComplexity
{
    public string ScriptName { get; set; } = string.Empty;
    public bool IsComplexScript { get; set; }
    public bool RequiresShaping { get; set; }
    public bool HasLigatures { get; set; }
    public decimal RenderingComplexity { get; set; }
}

/// <summary>
/// Language detection validation results
/// </summary>
public class LanguageDetectionValidation
{
    public Guid ValidationId { get; set; }
    public Guid UserId { get; set; }
    public bool WasAccurate { get; set; }
    public SouthAsianLanguage OriginalDetection { get; set; }
    public SouthAsianLanguage UserCorrection { get; set; }
    public decimal AccuracyScore { get; set; }
    public string FeedbackNotes { get; set; } = string.Empty;
    public DateTime ValidationTimestamp { get; set; }
}

/// <summary>
/// Language detection performance metrics
/// </summary>
public class LanguageDetectionMetrics
{
    public decimal OverallAccuracy { get; set; }
    public TimeSpan AverageLatency { get; set; }
    public int RequestsPerSecond { get; set; }
    public Dictionary<SouthAsianLanguage, decimal> LanguageSpecificAccuracy { get; set; } = new();
    public decimal SLACompliance { get; set; }
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Multi-language user profile for diaspora communities
/// </summary>
public class MultiLanguageUserProfile
{
    public Guid UserId { get; set; }
    public SouthAsianLanguage PrimaryLanguage { get; set; }
    public List<SouthAsianLanguage> SecondaryLanguages { get; set; } = new();
    public SouthAsianLanguage HeritageLanguage { get; set; }
    public int Generation { get; set; } // 1st, 2nd, 3rd+ generation
    public Dictionary<SouthAsianLanguage, decimal> LanguageProficiency { get; set; } = new();
    public List<string> CulturalContexts { get; set; } = new();
    public DateTime ProfileCreated { get; set; }
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Cultural event language boost configuration
/// </summary>
public class CulturalEventLanguageBoost
{
    public Guid UserId { get; set; }
    public SouthAsianLanguage LanguageToBoost { get; set; }
    public decimal BoostMultiplier { get; set; }
    public required CulturalEvent TriggeringEvent { get; set; }
    public TimeSpan BoostDuration { get; set; }
    public Dictionary<SouthAsianLanguage, decimal> SecondaryBoosts { get; set; } = new();
    public DateTime BoostStartTime { get; set; }
    public DateTime BoostEndTime { get; set; }
}

/// <summary>
/// Multi-cultural event conflict resolution
/// </summary>
public class MultiCulturalEventResolution
{
    public Guid UserId { get; set; }
    public List<CulturalEvent> ConflictingEvents { get; set; } = new();
    public SouthAsianLanguage RecommendedPrimaryLanguage { get; set; }
    public Dictionary<SouthAsianLanguage, decimal> LanguagePriorities { get; set; } = new();
    public List<string> ResolutionStrategies { get; set; } = new();
    public decimal ConflictSeverity { get; set; }
    public bool RequiresManualIntervention { get; set; }
}

/// <summary>
/// Cultural event language prediction
/// </summary>
public class CulturalEventLanguagePrediction
{
    public Guid UserId { get; set; }
    public TimeSpan PredictionPeriod { get; set; }
    public List<PredictedLanguagePreference> PredictedPreferences { get; set; } = new();
    public Dictionary<DateTime, List<CulturalEvent>> EventTimeline { get; set; } = new();
    public decimal PredictionConfidence { get; set; }
    public DateTime PredictionGenerated { get; set; }
}

/// <summary>
/// Predicted language preference for a specific time
/// </summary>
public class PredictedLanguagePreference
{
    public DateTime PredictionDate { get; set; }
    public SouthAsianLanguage PreferredLanguage { get; set; }
    public decimal Confidence { get; set; }
    public CulturalEvent? TriggeringEvent { get; set; }
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Cultural event routing optimization result
/// </summary>
public class CulturalEventRoutingOptimization
{
    public List<CulturalEvent> OptimizedEvents { get; set; } = new();
    public Dictionary<SouthAsianLanguage, string> RoutingStrategies { get; set; } = new();
    public Dictionary<CommunityType, decimal> LoadDistribution { get; set; } = new();
    public decimal ExpectedPerformanceImprovement { get; set; }
    public List<string> OptimizationRecommendations { get; set; } = new();
    public DateTime OptimizationTimestamp { get; set; }
}

/// <summary>
/// Cultural event analysis request
/// </summary>
public class CulturalEventAnalysisRequest
{
    public List<CulturalEvent> EventsToAnalyze { get; set; } = new();
    public TimeSpan AnalysisPeriod { get; set; }
    public List<SouthAsianLanguage> LanguagesOfInterest { get; set; } = new();
    public List<CommunityType> CommunitiesToInclude { get; set; } = new();
    public bool IncludePerformanceMetrics { get; set; }
    public bool IncludeCulturalInsights { get; set; }
}

/// <summary>
/// Cultural event language analytics
/// </summary>
public class CulturalEventLanguageAnalytics
{
    public List<CulturalEvent> AnalyzedEvents { get; set; } = new();
    public Dictionary<SouthAsianLanguage, CulturalEventLanguageUsage> LanguageUsagePatterns { get; set; } = new();
    public Dictionary<CommunityType, decimal> CommunityEngagement { get; set; } = new();
    public List<CulturalInsight> CulturalInsights { get; set; } = new();
    public DateTime AnalysisTimestamp { get; set; }
}

/// <summary>
/// Language usage during cultural events
/// </summary>
public class CulturalEventLanguageUsage
{
    public SouthAsianLanguage Language { get; set; }
    public decimal UsageIncrease { get; set; }
    public TimeSpan PeakUsagePeriod { get; set; }
    public Dictionary<CulturalEvent, decimal> EventSpecificUsage { get; set; } = new();
}

/// <summary>
/// Cultural insight from language analytics
/// </summary>
public class CulturalInsight
{
    public string InsightTitle { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal ConfidenceLevel { get; set; }
    public List<string> SupportingData { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// Cultural event language validation
/// </summary>
public class CulturalEventLanguageValidation
{
    public required CulturalEvent ValidatedEvent { get; set; }
    public decimal PerformanceScore { get; set; }
    public decimal CulturalAccuracyScore { get; set; }
    public decimal SLAComplianceScore { get; set; }
    public List<ValidationIssue> IdentifiedIssues { get; set; } = new();
    public List<string> ImprovementRecommendations { get; set; } = new();
    public DateTime ValidationTimestamp { get; set; }
}

/// <summary>
/// Validation issue identified during cultural event
/// </summary>
public class ValidationIssue
{
    public string IssueType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public SouthAsianLanguage? AffectedLanguage { get; set; }
    public string RecommendedAction { get; set; } = string.Empty;
}

/// <summary>
/// Cultural event language metrics
/// </summary>
public class CulturalEventLanguageMetrics
{
    public List<CulturalEvent> ActiveEvents { get; set; } = new();
    public Dictionary<SouthAsianLanguage, LanguageEventMetrics> LanguageMetrics { get; set; } = new();
    public decimal OverallSLACompliance { get; set; }
    public decimal CulturalAppropriateness { get; set; }
    public DateTime MetricsTimestamp { get; set; }
}

/// <summary>
/// Language-specific event metrics
/// </summary>
public class LanguageEventMetrics
{
    public SouthAsianLanguage Language { get; set; }
    public TimeSpan AverageLatency { get; set; }
    public decimal Accuracy { get; set; }
    public int RequestsPerSecond { get; set; }
    public decimal ErrorRate { get; set; }
}

/// <summary>
/// Cultural event cache warming result
/// </summary>
public class CulturalEventCacheWarmingResult
{
    public List<CulturalEvent> PreWarmedEvents { get; set; } = new();
    public Dictionary<SouthAsianLanguage, bool> LanguageCacheStatus { get; set; } = new();
    public decimal CacheReadinessPercentage { get; set; }
    public TimeSpan WarmupDuration { get; set; }
    public DateTime CacheWarmingTimestamp { get; set; }
    public List<string> WarmingNotes { get; set; } = new();
}

/// <summary>
/// Sacred content validation request for cultural appropriateness
/// </summary>
public class SacredContentRequest
{
    public Guid RequestId { get; set; }
    public string ContentText { get; set; } = string.Empty;
    public SacredContentType ContentType { get; set; }
    public SouthAsianLanguage RequestedLanguage { get; set; }
    public required CulturalBackground UserCulturalBackground { get; set; }
    public DateTime RequestTimestamp { get; set; }
    public bool RequiresStrictValidation { get; set; }
}

/// <summary>
/// Sacred content validation result with cultural appropriateness scoring
/// </summary>
public class SacredContentValidationResult
{
    public Guid ValidationId { get; set; }
    public bool IsApproved { get; set; }
    public decimal AppropriatenessScore { get; set; }
    public List<string> ValidationMessages { get; set; } = new();
    public List<SouthAsianLanguage> RecommendedLanguages { get; set; } = new();
    public List<string> CulturalConcerns { get; set; } = new();
    public DateTime ValidationTimestamp { get; set; }
}

/// <summary>
/// Sacred content type enumeration for cultural validation
/// </summary>
public enum SacredContentType
{
    BuddhistSutras,
    BuddhistPrayers,
    HinduVedas,
    HinduMantras,
    IslamicPrayers,
    SikhGuruGranth,
    GeneralReligiousContent,
    CeremonialContent,
    FestivalContent
}

/// <summary>
/// Cultural background for sacred content validation
/// </summary>
public class CulturalBackground
{
    public SouthAsianLanguage HeritageLanguage { get; set; }
    public List<SouthAsianLanguage> FamiliarLanguages { get; set; } = new();
    public string ReligiousTradition { get; set; } = string.Empty;
    public string CulturalRegion { get; set; } = string.Empty;
    public int GenerationInDiaspora { get; set; }
    public bool PreferTraditionalLanguages { get; set; }
}

/// <summary>
/// Cultural appropriateness validation for sacred content translation
/// </summary>
public class CulturalAppropriatenessValidation
{
    public bool IsAppropriate { get; set; }
    public decimal AppropriatenessScore { get; set; }
    public string ValidationReasoning { get; set; } = string.Empty;
    public List<string> CulturalConsiderations { get; set; } = new();
    public List<string> RecommendedAlternatives { get; set; } = new();
    public bool RequiresExpertReview { get; set; }
    public DateTime ValidationTimestamp { get; set; }
}

/// <summary>
/// Language interaction data for user profile learning
/// </summary>
public class LanguageInteractionData
{
    public Guid InteractionId { get; set; }
    public Guid UserId { get; set; }
    public SouthAsianLanguage LanguageUsed { get; set; }
    public string InteractionType { get; set; } = string.Empty; // "read", "write", "search", etc.
    public TimeSpan InteractionDuration { get; set; }
    public decimal EngagementScore { get; set; }
    public DateTime InteractionTimestamp { get; set; }
    public Dictionary<string, object> ContextData { get; set; } = new();
    public bool WasSuccessful { get; set; }
}