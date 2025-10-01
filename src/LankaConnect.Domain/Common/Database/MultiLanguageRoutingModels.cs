using System;
using System.Collections.Generic;
using LankaConnect.Domain.Communications.ValueObjects;

namespace LankaConnect.Domain.Common.Database.MultiLanguageRoutingModels;

/// <summary>
/// Domain models for Multi-Language Affinity Routing Engine
/// Supporting South Asian diaspora language preferences across generational cohorts
/// Optimized for cultural intelligence and heritage language preservation
/// </summary>

#region Core Enums

/// <summary>
/// South Asian languages supported by the multi-language routing engine
/// Covers primary diaspora languages with cultural intelligence context
/// </summary>
public enum SouthAsianLanguage
{
    // Primary Sri Lankan Languages
    Sinhala,
    Tamil,
    
    // Primary Indian Subcontinent Languages  
    Hindi,
    Urdu,
    Punjabi,
    Bengali,
    Gujarati,
    Marathi,
    Telugu,
    Kannada,
    Malayalam,
    
    // Additional Languages
    English,
    Arabic,
    Persian,
    
    // Sacred/Classical Languages
    Sanskrit,
    Pali,
    
    // Regional Variants
    SriLankanTamil,
    IndianTamil,
    PakistaniUrdu,
    IndianUrdu
}

/// <summary>
/// Generational cohorts for diaspora language pattern analysis
/// Critical for accurate language preference prediction
/// </summary>
public enum GenerationalCohort
{
    /// <summary>First generation - Heritage language dominant (85% heritage, 15% English)</summary>
    FirstGeneration,
    
    /// <summary>Second generation - Balanced languages (45% heritage, 55% English)</summary>
    SecondGeneration,
    
    /// <summary>Third generation+ - English dominant (25% heritage, 75% English)</summary>
    ThirdGenerationPlus,
    
    /// <summary>Recent immigrants - Heritage language exclusive (95% heritage, 5% English)</summary>
    RecentImmigrant,
    
    /// <summary>Mixed heritage - Multiple heritage languages</summary>
    MixedHeritage
}

/// <summary>
/// Cultural contexts that influence language routing decisions
/// Renamed to avoid conflict with CulturalContext class in Communications.ValueObjects
/// </summary>
public enum CulturalContextType
{
    Buddhist,
    Hindu,
    Islamic,
    Sikh,
    Christian,
    Business,
    Social,
    Sacred,
    Educational,
    Political,
    Medical,
    Legal,
    Entertainment,
    News,
    Community
}

/// <summary>
/// Cultural backgrounds for precise community targeting
/// </summary>
public enum CulturalBackground
{
    // Sri Lankan Communities
    SriLankanBuddhist,
    SriLankanTamil,
    SriLankanChristian,
    SriLankanMuslim,
    
    // Indian Communities
    IndianTamil,
    IndianHindu,
    IndianSikh,
    IndianChristian,
    IndianMuslim,
    
    // Pakistani Communities
    PakistaniMuslim,
    PakistaniChristian,
    
    // Bangladeshi Communities
    BengaliMuslim,
    BengaliHindu,
    
    // Multi-Cultural
    MultiCultural,
    SouthAsianGeneric
}

/// <summary>
/// Cultural events that trigger language preference boosts
/// </summary>
public enum CulturalEvent
{
    // Buddhist Events
    Vesak,
    Poyaday,
    BuddhistNewYear,
    
    // Hindu Events
    Diwali,
    Thaipusam,
    HoliPurnima,
    TamilNewYear,
    
    // Islamic Events
    Eid,
    Ramadan,
    MiladUnNabi,
    
    // Sikh Events
    Vaisakhi,
    GuruNanak,
    
    // Regional Events
    SriLankanIndependenceDay,
    IndianIndependenceDay,
    PakistanIndependenceDay,
    BangladeshIndependenceDay,
    
    // General
    CulturalHeritage,
    MultiCultural
}

/// <summary>
/// Intensity levels for cultural events affecting language preferences
/// </summary>
public enum CulturalEventIntensity
{
    Minor,      // 10-20% language boost
    Moderate,   // 30-50% language boost
    Major,      // 60-80% language boost
    Critical    // 90-95% language boost (sacred events)
}

/// <summary>
/// Community participation levels affecting language routing
/// </summary>
public enum CommunityParticipationLevel
{
    Low,
    Medium,
    High,
    VeryHigh
}

/// <summary>
/// Sacred content types requiring specific language validation
/// </summary>
public enum SacredContentType
{
    Buddhist,
    Hindu,
    Islamic,
    Sikh,
    Christian,
    MultiReligious
}

/// <summary>
/// Performance requirements for routing decisions
/// </summary>
public enum PerformanceRequirement
{
    Standard,           // <500ms
    FastRouting,        // <200ms
    FortuneToOSLA,      // <100ms
    CulturalEventSpike  // <50ms during events
}

/// <summary>
/// Content types for language routing optimization
/// </summary>
public enum ContentType
{
    CommunityDiscussion,
    CulturalEvent,
    SacredContent,
    BusinessDirectory,
    Educational,
    Entertainment,
    News,
    Government,
    Healthcare,
    Legal
}

/// <summary>
/// Database performance optimization modes
/// </summary>
public enum DatabasePerformanceMode
{
    StandardQuery,
    OptimalRouting,
    CacheOptimized,
    PartitionOptimized
}

/// <summary>
/// Language fallback strategies
/// </summary>
public enum LanguageFallbackStrategy
{
    DefaultToEnglish,
    UseGenerationalDefault,
    UseCommunityMajority,
    UseCulturalDefault,
    RequireManualReview
}

/// <summary>
/// Database failover modes
/// </summary>
public enum DatabaseFailoverMode
{
    LocalCache,
    CrossRegion,
    BackupInstance,
    EmergencyMode
}

/// <summary>
/// Language bridging strategies for intergenerational content
/// </summary>
public enum LanguageBridgingStrategy
{
    GradualTransition,
    BilingualPresentation,
    LearningOpportunity,
    CulturalBridge
}

/// <summary>
/// Revenue streams for language-based optimization
/// </summary>
public enum LanguageRevenueStream
{
    PremiumContent,
    CulturalEvents,
    BusinessDirectory,
    LanguageTutoring,
    TranslationServices
}

/// <summary>
/// Business categories for language matching
/// </summary>
public enum BusinessCategory
{
    Restaurant,
    Temple,
    CulturalCenter,
    Education,
    Healthcare,
    Legal,
    Financial,
    Retail,
    Entertainment
}

/// <summary>
/// Cultural preferences for business matching
/// </summary>
public enum CulturalPreference
{
    AuthenticCuisine,
    TraditionalServices,
    HeritageLanguageSupport,
    CulturalSensitivity,
    CommunityOwned
}

/// <summary>
/// Cultural regions for geographic routing
/// </summary>
public enum LanguageCulturalRegion
{
    SriLankanDiaspora,
    IndianDiaspora,
    PakistaniDiaspora,
    BangladeshiDiaspora,
    SouthAsianGeneral
}

#endregion

#region Core Request/Response Models

/// <summary>
/// Request for multi-language routing with performance requirements
/// </summary>
public class MultiLanguageRoutingRequest
{
    public Guid UserId { get; set; }
    public ContentType ContentType { get; set; }
    public List<SouthAsianLanguage> RequestedLanguages { get; set; } = new();
    public PerformanceRequirement PerformanceRequirement { get; set; } = PerformanceRequirement.Standard;
    public DatabaseFailoverMode FailoverMode { get; set; } = DatabaseFailoverMode.LocalCache;
    public CulturalContextType? CulturalContextType { get; set; }
    public DateTime RequestTimestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Response from multi-language routing with performance metrics
/// </summary>
public class MultiLanguageRoutingResponse
{
    public SouthAsianLanguage PrimaryLanguage { get; set; }
    public List<SouthAsianLanguage> AlternativeLanguages { get; set; } = new();
    public decimal RoutingAccuracy { get; set; }
    public decimal CacheHitRate { get; set; }
    public int DatabaseQueriesCount { get; set; }
    public bool DatabaseFailoverUsed { get; set; }
    public decimal PerformanceDegradation { get; set; }
    public bool ServiceContinuity { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public string? PerformanceNotes { get; set; }
}

/// <summary>
/// Language detection result with cultural context
/// </summary>
public class LanguageDetectionResult
{
    public SouthAsianLanguage PrimaryLanguage { get; set; }
    public decimal LanguageConfidence { get; set; }
    public CulturalContextType CulturalContextType { get; set; }
    public List<SouthAsianLanguage> DetectedLanguages { get; set; } = new();
    public Dictionary<SouthAsianLanguage, decimal> LanguageConfidenceScores { get; set; } = new();
    public LanguageFallbackStrategy? FallbackStrategy { get; set; }
    public bool RequiresManualReview { get; set; }
    public string? DetectionNotes { get; set; }
}

/// <summary>
/// Generational pattern analysis result
/// </summary>
public class GenerationalPatternAnalysis
{
    public decimal HeritageLanguagePreference { get; set; }
    public decimal EnglishPreference { get; set; }
    public decimal CulturalEventBoostFactor { get; set; }
    public SouthAsianLanguage? SacredContentLanguageRequirement { get; set; }
    public bool BilingualContentPreference { get; set; }
    public bool HeritageLanguageLearningRecommendation { get; set; }
    public bool IntergenerationalBridgingContent { get; set; }
    public Dictionary<CulturalContextType, decimal> ContextualPreferences { get; set; } = new();
}

/// <summary>
/// Cultural event context for language boost calculations
/// </summary>
public class LanguageBoostCulturalEventContext
{
    public CulturalEvent? CurrentEvent { get; set; }
    public List<CulturalEvent>? OverlappingEvents { get; set; }
    public CulturalEventIntensity EventIntensity { get; set; }
    public int DaysUntilEvent { get; set; }
    public CommunityParticipationLevel CommunityParticipationLevel { get; set; }
    public Dictionary<string, object>? EventMetadata { get; set; }
}

/// <summary>
/// Cultural event language boost result
/// </summary>
public class CulturalEventLanguageBoost
{
    public SouthAsianLanguage PrimaryLanguage { get; set; }
    public decimal BoostFactor { get; set; }
    public bool SacredContentRequirement { get; set; }
    public List<SouthAsianLanguage> LanguageAlternatives { get; set; } = new();
    public bool MultiCulturalContent { get; set; }
    public string? ConflictResolutionStrategy { get; set; }
    public Dictionary<CulturalEvent, decimal>? EventSpecificBoosts { get; set; }
}

/// <summary>
/// Sacred content language validation request
/// </summary>
public class SacredContentRequest
{
    public SacredContentType ContentType { get; set; }
    public SouthAsianLanguage RequestedLanguage { get; set; }
    public CulturalBackground UserCulturalBackground { get; set; }
    public CulturalContextType CulturalContextType { get; set; }
    public bool RequireStrictValidation { get; set; } = true;
}

/// <summary>
/// Sacred content language validation result
/// </summary>
public class SacredContentValidationResult
{
    public bool IsValid { get; set; }
    public SouthAsianLanguage? RequiredLanguage { get; set; }
    public List<SouthAsianLanguage> AcceptableAlternatives { get; set; } = new();
    public List<SouthAsianLanguage> AlternativeLanguages { get; set; } = new();
    public SouthAsianLanguage? RecommendedLanguage { get; set; }
    public decimal CulturalAppropriatenessScore { get; set; }
    public bool CulturalAppropriatenessValidation { get; set; }
    public string? ValidationNotes { get; set; }
}

#endregion

#region Profile and User Models

/// <summary>
/// Comprehensive multi-language user profile
/// </summary>
public class MultiLanguageUserProfile
{
    public Guid UserId { get; set; }
    public Dictionary<SouthAsianLanguage, decimal> NativeLanguages { get; set; } = new();
    public Dictionary<SouthAsianLanguage, decimal> HeritageLanguages { get; set; } = new();
    public GenerationalCohort GenerationalCohort { get; set; }
    public Dictionary<CulturalContext, SouthAsianLanguage> CulturalLanguagePreferences { get; set; } = new();
    public Dictionary<CulturalContext, SouthAsianLanguage> SacredLanguageRequirements { get; set; } = new();
    public CulturalBackground CulturalBackground { get; set; }
    public List<CulturalEvent> PrimaryCulturalEvents { get; set; } = new();
    public Dictionary<string, object>? LanguageAffinityCache { get; set; }
    public DateTime? CacheExpiry { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}

#endregion

#region Query and Database Models

/// <summary>
/// Language routing database query optimization
/// </summary>
public class LanguageRoutingQuery
{
    public List<SouthAsianLanguage> Languages { get; set; } = new();
    public List<CulturalRegion> CulturalRegions { get; set; } = new();
    public DatabasePerformanceMode PerformanceMode { get; set; } = DatabasePerformanceMode.StandardQuery;
    public GenerationalCohort? GenerationalFilter { get; set; }
    public List<CulturalBackground>? CulturalBackgroundFilter { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? Limit { get; set; }
    public int? Offset { get; set; }
}

/// <summary>
/// Language routing query result with performance metrics
/// </summary>
public class LanguageRoutingQueryResult
{
    public List<MultiLanguageUserProfile> Results { get; set; } = new();
    public int TotalCount { get; set; }
    public string? PartitionHit { get; set; }
    public List<string> IndexUsage { get; set; } = new();
    public TimeSpan QueryTime { get; set; }
    public bool CacheHit { get; set; }
    public Dictionary<string, object>? QueryMetadata { get; set; }
}

#endregion

#region Heritage Language and Community Models

/// <summary>
/// Heritage language preservation analysis request
/// </summary>
public class HeritageLanguagePreservationRequest
{
    public Guid CommunityId { get; set; }
    public SouthAsianLanguage TargetLanguage { get; set; }
    public bool GenerationalAnalysis { get; set; } = true;
    public bool PreservationStrategies { get; set; } = true;
    public TimeSpan AnalysisPeriod { get; set; } = TimeSpan.FromDays(365);
    public List<CulturalEvent>? FocusEvents { get; set; }
}

/// <summary>
/// Heritage language preservation analysis result
/// </summary>
public class HeritageLanguagePreservationResult
{
    public decimal LanguageVitality { get; set; }
    public Dictionary<GenerationalCohort, decimal> GenerationalDecline { get; set; } = new();
    public List<string> PreservationRecommendations { get; set; } = new();
    public List<string> CommunityEngagementOpportunities { get; set; } = new();
    public decimal YouthEngagement { get; set; }
    public decimal ElderKnowledgeTransfer { get; set; }
    public List<CulturalEvent> LanguageRevitalizationEvents { get; set; } = new();
}

/// <summary>
/// Intergenerational content request for language bridging
/// </summary>
public class IntergenerationalContentRequest
{
    public SouthAsianLanguage FirstGenerationLanguage { get; set; }
    public SouthAsianLanguage YoungerGenerationLanguage { get; set; }
    public ContentType ContentType { get; set; }
    public LanguageBridgingStrategy BridgingStrategy { get; set; }
    public List<CulturalContextType>? CulturalContexts { get; set; }
    public bool IncludeLearningOpportunities { get; set; } = true;
}

/// <summary>
/// Intergenerational content result
/// </summary>
public class IntergenerationalContentResult
{
    public bool BilingualContent { get; set; }
    public List<string> LanguageLearningOpportunities { get; set; } = new();
    public List<string> CulturalConnectionPoints { get; set; } = new();
    public decimal GenerationalEngagement { get; set; }
    public Dictionary<SouthAsianLanguage, decimal> LanguageBalance { get; set; } = new();
    public List<string> BridgingElements { get; set; } = new();
}

#endregion

#region Revenue and Business Models

/// <summary>
/// Language-based revenue analysis request
/// </summary>
public class LanguageRevenueAnalysisRequest
{
    public List<SouthAsianLanguage> TargetLanguages { get; set; } = new();
    public List<RevenueStream> RevenueStreams { get; set; } = new();
    public TimeSpan AnalysisPeriod { get; set; } = TimeSpan.FromDays(90);
    public List<CulturalEvent>? FocusEvents { get; set; }
    public List<GenerationalCohort>? TargetCohorts { get; set; }
}

/// <summary>
/// Language-based revenue analysis result
/// </summary>
public class LanguageRevenueAnalysisResult
{
    public decimal EngagementIncrease { get; set; }
    public decimal RevenueMultiplier { get; set; }
    public List<string> NewRevenueStreams { get; set; } = new();
    public Dictionary<SouthAsianLanguage, decimal> LanguageRevenueImpact { get; set; } = new();
    public BusinessDirectoryOptimizationResult? BusinessDirectoryOptimization { get; set; }
    public Dictionary<RevenueStream, decimal> StreamOptimization { get; set; } = new();
}

/// <summary>
/// Business language matching request for directory optimization
/// </summary>
public class BusinessLanguageMatchingRequest
{
    public MultiLanguageUserProfile UserLanguageProfile { get; set; } = new();
    public BusinessCategory BusinessCategory { get; set; }
    public List<CulturalPreference> CulturalPreferences { get; set; } = new();
    public CulturalRegion? PreferredRegion { get; set; }
    public int MaxResults { get; set; } = 20;
}

/// <summary>
/// Business language matching result
/// </summary>
public class BusinessLanguageMatchingResult
{
    public decimal LanguageMatchScore { get; set; }
    public decimal CulturalRelevanceScore { get; set; }
    public decimal ConversionProbability { get; set; }
    public List<BusinessDirectoryEntry> RecommendedBusinesses { get; set; } = new();
    public Dictionary<SouthAsianLanguage, decimal> LanguageCompatibility { get; set; } = new();
}

/// <summary>
/// Business directory entry with language support
/// </summary>
public class BusinessDirectoryEntry
{
    public Guid BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public BusinessCategory Category { get; set; }
    public List<SouthAsianLanguage> SupportedLanguages { get; set; } = new();
    public CulturalBackground OwnerCulturalBackground { get; set; }
    public List<CulturalPreference> CulturalFeatures { get; set; } = new();
    public decimal LanguageMatchScore { get; set; }
    public string? Location { get; set; }
}

/// <summary>
/// Business directory optimization result
/// </summary>
public class BusinessDirectoryOptimizationResult
{
    public decimal ConversionImprovement { get; set; }
    public Dictionary<BusinessCategory, decimal> CategoryOptimization { get; set; } = new();
    public List<string> LanguageGaps { get; set; } = new();
    public List<string> OpportunityAreas { get; set; } = new();
}

#endregion

