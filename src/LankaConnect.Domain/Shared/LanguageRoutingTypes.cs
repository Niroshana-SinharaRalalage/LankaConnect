using System;
using System.Collections.Generic;
using CulturalContextType = LankaConnect.Domain.Common.Database.MultiLanguageRoutingModels.CulturalContextType;

namespace LankaConnect.Domain.Shared;

/// <summary>
/// Language detection result with cultural context
/// </summary>
public class LanguageDetectionResult
{
    public SouthAsianLanguage PrimaryLanguage { get; set; }
    public decimal LanguageConfidence { get; set; }
    public CulturalContextType CulturalContextType { get; set; }
    public LanguageFallbackStrategy FallbackStrategy { get; set; }
    public bool RequiresManualReview { get; set; }
    public List<SouthAsianLanguage> AlternativeLanguages { get; set; } = new();
}

/// <summary>
/// Multi-language user profile for routing optimization
/// </summary>
public class MultiLanguageUserProfile
{
    public Guid UserId { get; set; }
    public GenerationalCohort GenerationalCohort { get; set; }
    public Dictionary<SouthAsianLanguage, decimal> NativeLanguages { get; set; } = new();
    public Dictionary<SouthAsianLanguage, decimal> HeritageLanguages { get; set; } = new();
    public CulturalBackground CulturalBackground { get; set; }
    public List<CulturalEvent> PrimaryCulturalEvents { get; set; } = new();
    public DateTime LastUpdated { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Generational pattern analysis for cultural intelligence
/// </summary>
public class GenerationalPatternAnalysis
{
    public decimal HeritageLanguagePreference { get; set; }
    public decimal EnglishPreference { get; set; }
    public decimal CulturalEventBoostFactor { get; set; }
    public SouthAsianLanguage SacredContentLanguageRequirement { get; set; }
    public bool BilingualContentPreference { get; set; }
    public bool HeritageLanguageLearningRecommendation { get; set; }
    public bool IntergenerationalBridgingContent { get; set; }
}

/// <summary>
/// Cultural event language boost calculation
/// </summary>
public class CulturalEventLanguageBoost
{
    public decimal BoostFactor { get; set; }
    public SouthAsianLanguage PrimaryLanguage { get; set; }
    public bool SacredContentRequirement { get; set; }
    public bool MultiCulturalContent { get; set; }
    public string ConflictResolutionStrategy { get; set; } = string.Empty;
    public List<SouthAsianLanguage> LanguageAlternatives { get; set; } = new();
}

/// <summary>
/// Multi-cultural event resolution for overlapping events
/// </summary>
public class MultiCulturalEventResolution
{
    public bool RequiresMultiCulturalContent { get; set; }
    public Dictionary<CulturalEvent, decimal> EventWeights { get; set; } = new();
    public List<SouthAsianLanguage> PriorityLanguages { get; set; } = new();
    public string ResolutionStrategy { get; set; } = string.Empty;
}

/// <summary>
/// Cultural event context for routing decisions
/// </summary>
public class CulturalEventContext
{
    public CulturalEvent? CurrentEvent { get; set; }
    public CulturalEventIntensity EventIntensity { get; set; }
    public List<CulturalEvent> OverlappingEvents { get; set; } = new();
    public DateTime EventStartTime { get; set; }
    public DateTime EventEndTime { get; set; }
    public CulturalRegion PrimaryRegion { get; set; }
}

/// <summary>
/// Cultural event language prediction
/// </summary>
public class CulturalEventLanguagePrediction
{
    public List<CulturalEvent> UpcomingEvents { get; set; } = new();
    public Dictionary<DateTime, SouthAsianLanguage> PredictedLanguagePreferences { get; set; } = new();
    public Dictionary<CulturalEvent, decimal> EventImpactScores { get; set; } = new();
    public TimeSpan PredictionPeriod { get; set; }
}

/// <summary>
/// Sacred content validation request
/// </summary>
public class SacredContentRequest
{
    public SacredContentType ContentType { get; set; }
    public SouthAsianLanguage RequestedLanguage { get; set; }
    public string ContentSummary { get; set; } = string.Empty;
    public CulturalBackground UserCulturalBackground { get; set; }
}

/// <summary>
/// Sacred content validation result
/// </summary>
public class SacredContentValidationResult
{
    public bool IsValid { get; set; }
    public List<SouthAsianLanguage> AcceptableAlternatives { get; set; } = new();
    public SouthAsianLanguage RequiredLanguage { get; set; }
    public decimal CulturalAppropriatenessScore { get; set; }
    public SouthAsianLanguage RecommendedLanguage { get; set; }
    public bool CulturalAppropriatenessValidation { get; set; }
}

/// <summary>
/// Cultural appropriateness validation
/// </summary>
public class CulturalAppropriatenessValidation
{
    public bool IsAppropriate { get; set; }
    public decimal AppropriatenessScore { get; set; }
    public List<string> ConcernAreas { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}