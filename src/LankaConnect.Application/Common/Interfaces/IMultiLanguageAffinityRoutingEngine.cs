using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LankaConnect.Application.Common.Models.Performance;
using LankaConnect.Application.Common.Models.Routing;
using LankaConnect.Application.Common.Routing;
using LankaConnect.Domain.Common.Database;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Application.Common.Models.MultiLanguage;
using LankaConnect.Domain.Common.Monitoring;
using DomainDatabase = LankaConnect.Domain.Common.Database;
using MultiLanguageModels = LankaConnect.Domain.Common.Database.MultiLanguageRoutingModels;
// using LankaConnect.Domain.Common.Database.MultiLanguageRoutingModels; // Removed to prevent ambiguous references
using PerformanceCulturalEvent = LankaConnect.Application.Common.Models.Performance.CulturalEvent;
using LankaConnect.Domain.Common.CulturalIntelligence;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Multi-Language Affinity Routing Engine Interface
/// Supports South Asian diaspora language preferences across 6M+ users
/// Handles Sinhala, Tamil, Hindi, Urdu, Punjabi, Bengali, Gujarati with cultural intelligence
/// Optimized for Fortune 500 SLA compliance and cultural event scaling
/// </summary>
public interface IMultiLanguageAffinityRoutingEngine
{
    #region Language Detection and Analysis

    /// <summary>
    /// Detect language preferences from user content with cultural context
    /// Performance target: <100ms for real-time detection
    /// </summary>
    /// <param name="userId">User identifier for profile association</param>
    /// <param name="userContent">Content to analyze for language detection</param>
    /// <returns>Language detection result with confidence scores</returns>
    Task<LankaConnect.Application.Common.Models.Routing.LanguageDetectionResult> DetectLanguagePreferencesAsync(Guid userId, string userContent);

    /// <summary>
    /// Analyze generational language patterns for diaspora communities
    /// Critical for accurate routing across first/second/third generation users
    /// </summary>
    /// <param name="userProfile">User language profile with generational data</param>
    /// <returns>Generational pattern analysis with preferences</returns>
    Task<LankaConnect.Application.Common.Models.Routing.GenerationalPatternAnalysis> AnalyzeGenerationalPatternAsync(LankaConnect.Application.Common.Models.Routing.MultiLanguageUserProfile userProfile);

    /// <summary>
    /// Detect multiple languages in content with priority scoring
    /// Handles code-switching common in diaspora communications
    /// </summary>
    /// <param name="content">Multi-language content for analysis</param>
    /// <returns>Dictionary of detected languages with confidence scores</returns>
    Task<Dictionary<LankaConnect.Domain.Common.Enums.SouthAsianLanguage, decimal>> DetectMultipleLanguagesAsync(string content);

    /// <summary>
    /// Analyze language complexity and script requirements
    /// Optimizes for Sinhala, Tamil, Devanagari, and Arabic scripts
    /// </summary>
    /// <param name="languages">Languages to analyze for complexity</param>
    /// <returns>Script complexity analysis results</returns>
    Task<LanguageComplexityAnalysis> AnalyzeLanguageComplexityAsync(List<LankaConnect.Domain.Common.Enums.SouthAsianLanguage> languages);

    #endregion

    #region Cultural Event Integration

    /// <summary>
    /// Calculate language preference boosts during cultural events
    /// Vesak: 5x boost for Sinhala, Diwali: 4.5x for Hindi/Tamil
    /// </summary>
    /// <param name="userId">User identifier for personalized boost calculation</param>
    /// <param name="eventContext">Cultural event context and intensity</param>
    /// <returns>Event-specific language boost configuration</returns>
    Task<LankaConnect.Application.Common.Models.Performance.CulturalEventLanguageBoost> CalculateCulturalEventLanguageBoostAsync(Guid userId, CulturalEventContext eventContext);

    /// <summary>
    /// Handle multiple overlapping cultural events with conflict resolution
    /// Example: Diwali + Eid overlap requiring multi-cultural content strategy
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="overlappingEvents">List of concurrent cultural events</param>
    /// <returns>Conflict resolution strategy with language priorities</returns>
    Task<MultiCulturalEventResolution> ResolveMultiCulturalEventConflictsAsync(Guid userId, List<PerformanceCulturalEvent> overlappingEvents);

    /// <summary>
    /// Predict language preferences based on upcoming cultural events
    /// Enables proactive content preparation and resource allocation
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="predictionPeriod">Timespan for prediction analysis</param>
    /// <returns>Predicted language preferences with event timeline</returns>
    Task<CulturalEventLanguagePrediction> PredictCulturalEventLanguagePreferencesAsync(Guid userId, TimeSpan predictionPeriod);

    #endregion

    #region Sacred Content Management

    /// <summary>
    /// Validate sacred content language requirements with cultural appropriateness
    /// Buddhist content → Sinhala requirement, Hindu content → Tamil/Hindi/Sanskrit
    /// </summary>
    /// <param name="contentRequest">Sacred content validation request</param>
    /// <returns>Validation result with appropriateness scoring</returns>
    Task<LankaConnect.Application.Common.Models.MultiLanguage.SacredContentValidationResult> ValidateSacredContentLanguageRequirementsAsync(LankaConnect.Application.Common.Models.MultiLanguage.SacredContentRequest contentRequest);

    /// <summary>
    /// Generate sacred content language alternatives with cultural sensitivity
    /// Provides appropriate fallback languages while maintaining reverence
    /// </summary>
    /// <param name="primaryLanguage">Primary requested language</param>
    /// <param name="sacredContentType">Type of sacred content</param>
    /// <param name="userCulturalBackground">User's cultural context</param>
    /// <returns>List of culturally appropriate alternative languages</returns>
    Task<List<LankaConnect.Domain.Common.Enums.SouthAsianLanguage>> GenerateSacredContentLanguageAlternativesAsync(
        LankaConnect.Domain.Common.Enums.SouthAsianLanguage primaryLanguage,
        LankaConnect.Application.Common.Models.MultiLanguage.SacredContentType sacredContentType,
        LankaConnect.Application.Common.Models.MultiLanguage.CulturalBackground userCulturalBackground);

    /// <summary>
    /// Verify cultural appropriateness for religious content translation
    /// Prevents inappropriate language mismatches in sacred contexts
    /// </summary>
    /// <param name="sourceLanguage">Original content language</param>
    /// <param name="targetLanguage">Desired translation language</param>
    /// <param name="sacredContentType">Type of sacred content</param>
    /// <returns>Cultural appropriateness score and validation</returns>
    Task<LankaConnect.Application.Common.Models.MultiLanguage.CulturalAppropriatenessValidation> ValidateCulturalAppropriatenessAsync(
        LankaConnect.Domain.Common.Enums.SouthAsianLanguage sourceLanguage,
        LankaConnect.Domain.Common.Enums.SouthAsianLanguage targetLanguage,
        LankaConnect.Application.Common.Models.MultiLanguage.SacredContentType sacredContentType);

    #endregion

    #region Multi-Language Routing

    /// <summary>
    /// Execute comprehensive multi-language routing with performance optimization
    /// Performance target: <100ms standard, <50ms during cultural events
    /// </summary>
    /// <param name="routingRequest">Multi-language routing request with preferences</param>
    /// <returns>Optimized routing response with performance metrics</returns>
    Task<LankaConnect.Application.Common.Models.Routing.MultiLanguageRoutingResponse> ExecuteMultiLanguageRoutingAsync(LankaConnect.Application.Common.Models.Routing.MultiLanguageRoutingRequest routingRequest);

    /// <summary>
    /// Optimize routing for high-concurrency scenarios (cultural event traffic spikes)
    /// Handles 5x traffic increase during major cultural celebrations
    /// </summary>
    /// <param name="concurrentRequests">Batch of routing requests for optimization</param>
    /// <returns>Batch routing results with performance optimization</returns>
    Task<LankaConnect.Application.Common.Models.Routing.BatchMultiLanguageRoutingResponse> ExecuteBatchMultiLanguageRoutingAsync(List<LankaConnect.Application.Common.Models.Routing.MultiLanguageRoutingRequest> concurrentRequests);

    /// <summary>
    /// Generate intelligent routing fallback strategies for service continuity
    /// Ensures 99.99% uptime during database partition failures
    /// </summary>
    /// <param name="primaryRoutingFailure">Primary routing failure context</param>
    /// <param name="userProfile">User profile for personalized fallback</param>
    /// <returns>Intelligent fallback routing strategy</returns>
    Task<RoutingFallbackStrategy> GenerateIntelligentRoutingFallbackAsync(
        RoutingFailureContext primaryRoutingFailure, 
        LankaConnect.Application.Common.Models.Routing.MultiLanguageUserProfile userProfile);

    #endregion

    #region User Profile Management

    /// <summary>
    /// Store comprehensive multi-language user profile with optimization
    /// Supports complex language hierarchies and cultural preferences
    /// </summary>
    /// <param name="userProfile">Complete user language profile</param>
    /// <returns>Storage success confirmation</returns>
    Task<bool> StoreMultiLanguageProfileAsync(LankaConnect.Application.Common.Models.Routing.MultiLanguageUserProfile userProfile);

    /// <summary>
    /// Retrieve multi-language user profile with cache optimization
    /// Performance target: <50ms with L1/L2 caching strategy
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <returns>Complete user language profile or null if not found</returns>
    Task<LankaConnect.Application.Common.Models.Routing.MultiLanguageUserProfile?> GetMultiLanguageProfileAsync(Guid userId);

    /// <summary>
    /// Update user language preferences with incremental learning
    /// Adapts to changing language preferences over time
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="languageInteractions">Recent language interaction data</param>
    /// <returns>Updated profile with learning integration</returns>
    Task<LankaConnect.Application.Common.Models.Routing.MultiLanguageUserProfile> UpdateLanguagePreferencesAsync(Guid userId, List<LanguageInteractionData> languageInteractions);

    /// <summary>
    /// Bulk update user profiles for community-wide language pattern changes
    /// Efficient for cultural event preparation and community migrations
    /// </summary>
    /// <param name="communityUpdates">Community language profile updates</param>
    /// <returns>Bulk update result summary</returns>
    Task<BulkProfileUpdateResult> BulkUpdateCommunityLanguageProfilesAsync(List<CommunityLanguageProfileUpdate> communityUpdates);

    #endregion

    #region Database Query Optimization

    /// <summary>
    /// Execute optimized language routing queries with partition awareness
    /// Leverages language-aware database partitioning for <50ms queries
    /// </summary>
    /// <param name="query">Language routing query with optimization parameters</param>
    /// <returns>Query results with performance metrics</returns>
    Task<LanguageRoutingQueryResult> QueryLanguageRoutingDataAsync(LanguageRoutingQuery query);

    /// <summary>
    /// Optimize database queries for cultural event traffic patterns
    /// Pre-loads and caches data for predicted cultural event surges
    /// </summary>
    /// <param name="culturalEvents">Upcoming cultural events for optimization</param>
    /// <param name="optimizationPeriod">Time period for optimization preparation</param>
    /// <returns>Database optimization strategy and cache preparation</returns>
    Task<DatabaseOptimizationStrategy> OptimizeDatabaseForCulturalEventsAsync(
        List<PerformanceCulturalEvent> culturalEvents, 
        TimeSpan optimizationPeriod);

    /// <summary>
    /// Monitor and analyze query performance for continuous optimization
    /// Tracks partition efficiency, index usage, and cache hit rates
    /// </summary>
    /// <returns>Database performance analysis with optimization recommendations</returns>
    Task<DatabasePerformanceAnalysis> AnalyzeDatabasePerformanceAsync();

    #endregion

    #region Heritage Language Preservation

    /// <summary>
    /// Analyze heritage language preservation patterns within diaspora communities
    /// Critical for cultural intelligence and community engagement strategies
    /// </summary>
    /// <param name="preservationRequest">Heritage language preservation analysis request</param>
    /// <returns>Comprehensive preservation analysis with recommendations</returns>
    Task<HeritageLanguagePreservationResult> AnalyzeHeritageLanguagePreservationAsync(HeritageLanguagePreservationRequest preservationRequest);

    /// <summary>
    /// Generate intergenerational content bridging different language preferences
    /// Connects first-generation heritage speakers with English-dominant younger generations
    /// </summary>
    /// <param name="contentRequest">Intergenerational content generation request</param>
    /// <returns>Bilingual content strategy with cultural bridging elements</returns>
    Task<IntergenerationalContentResult> GenerateIntergenerationalContentAsync(IntergenerationalContentRequest contentRequest);

    /// <summary>
    /// Recommend heritage language learning opportunities based on community patterns
    /// Drives engagement and cultural preservation through targeted recommendations
    /// </summary>
    /// <param name="userId">User identifier for personalized recommendations</param>
    /// <param name="targetLanguage">Heritage language for learning focus</param>
    /// <returns>Personalized heritage language learning pathway</returns>
    Task<HeritageLanguageLearningRecommendations> GenerateHeritageLanguageLearningRecommendationsAsync(Guid userId, SouthAsianLanguage targetLanguage);

    /// <summary>
    /// Create cultural education content pathways with language progression
    /// Supports gradual heritage language acquisition through cultural engagement
    /// </summary>
    /// <param name="culturalBackground">User's cultural background context</param>
    /// <param name="currentLanguageLevel">Current proficiency in heritage language</param>
    /// <returns>Structured cultural education pathway with language progression</returns>
    Task<CulturalEducationPathway> CreateCulturalEducationLanguagePathwayAsync(
        LankaConnect.Application.Common.Models.MultiLanguage.CulturalBackground culturalBackground, 
        LanguageProficiencyLevel currentLanguageLevel);

    #endregion

    #region Revenue and Business Optimization

    /// <summary>
    /// Analyze language-based revenue opportunities for $25.7M platform optimization
    /// Identifies engagement increases (15-25%) and new revenue streams
    /// </summary>
    /// <param name="revenueAnalysisRequest">Revenue analysis parameters</param>
    /// <returns>Revenue optimization opportunities with engagement projections</returns>
    Task<LanguageRevenueAnalysisResult> AnalyzeLanguageBasedRevenueOpportunitiesAsync(LanguageRevenueAnalysisRequest revenueAnalysisRequest);

    /// <summary>
    /// Optimize business directory language matching for improved conversions
    /// Matches users with culturally and linguistically compatible businesses
    /// </summary>
    /// <param name="businessMatchingRequest">Business language matching parameters</param>
    /// <returns>Optimized business recommendations with conversion probability</returns>
    Task<BusinessLanguageMatchingResult> OptimizeBusinessDirectoryLanguageMatchingAsync(BusinessLanguageMatchingRequest businessMatchingRequest);

    /// <summary>
    /// Generate premium content strategies based on language preferences
    /// Creates monetization opportunities through heritage language content
    /// </summary>
    /// <param name="targetLanguages">Languages for premium content development</param>
    /// <param name="contentTypes">Types of premium content for development</param>
    /// <returns>Premium content strategy with revenue projections</returns>
    Task<PremiumContentStrategy> GeneratePremiumLanguageContentStrategyAsync(
        List<LankaConnect.Domain.Common.Enums.SouthAsianLanguage> targetLanguages,
        List<LankaConnect.Domain.Common.Enums.ContentType> contentTypes);

    /// <summary>
    /// Analyze cultural event monetization through language-specific services
    /// Optimizes revenue during cultural celebrations through targeted language services
    /// </summary>
    /// <param name="culturalEvents">Cultural events for monetization analysis</param>
    /// <param name="serviceTypes">Types of language services for revenue generation</param>
    /// <returns>Cultural event monetization strategy with revenue projections</returns>
    Task<CulturalEventMonetizationStrategy> AnalyzeCulturalEventLanguageMonetizationAsync(
        List<PerformanceCulturalEvent> culturalEvents, 
        List<LanguageServiceType> serviceTypes);

    #endregion

    #region Performance and Monitoring

    /// <summary>
    /// Monitor real-time performance metrics for multi-language routing
    /// Tracks sub-100ms response times and Fortune 500 SLA compliance
    /// </summary>
    /// <returns>Real-time performance dashboard data</returns>
    Task<LanguageRoutingPerformanceMetrics> GetRealTimePerformanceMetricsAsync();

    /// <summary>
    /// Generate comprehensive analytics for language routing patterns
    /// Provides insights for continuous optimization and cultural intelligence enhancement
    /// </summary>
    /// <param name="analyticsRequest">Analytics parameters and time periods</param>
    /// <returns>Comprehensive language routing analytics</returns>
    Task<LanguageRoutingAnalytics> GenerateLanguageRoutingAnalyticsAsync(LanguageRoutingAnalyticsRequest analyticsRequest);

    /// <summary>
    /// Validate system health and cultural intelligence accuracy
    /// Ensures 95%+ accuracy in cultural context detection and language routing
    /// </summary>
    /// <returns>System health validation with cultural intelligence metrics</returns>
    Task<SystemHealthValidation> ValidateSystemHealthAndAccuracyAsync();

    /// <summary>
    /// Benchmark performance against cultural event scaling requirements
    /// Validates 5x traffic handling capability during major cultural celebrations
    /// </summary>
    /// <param name="culturalEventScenarios">Cultural event traffic scenarios for benchmarking</param>
    /// <returns>Performance benchmark results with scaling validation</returns>
    Task<CulturalEventPerformanceBenchmark> BenchmarkCulturalEventScalingAsync(List<CulturalEventScenario> culturalEventScenarios);

    #endregion

    #region Cache and Performance Optimization

    /// <summary>
    /// Optimize multi-level caching strategy for language routing performance
    /// Implements L1 memory cache and L2 distributed cache for <100ms responses
    /// </summary>
    /// <param name="cacheOptimizationRequest">Cache optimization parameters</param>
    /// <returns>Cache optimization strategy with performance improvements</returns>
    Task<CacheOptimizationResult> OptimizeMultiLevelCachingAsync(CacheOptimizationRequest cacheOptimizationRequest);

    /// <summary>
    /// Pre-warm caches for predicted cultural event traffic patterns
    /// Proactive cache preparation for major cultural celebrations
    /// </summary>
    /// <param name="culturalEvents">Upcoming cultural events</param>
    /// <param name="expectedTrafficMultiplier">Traffic increase multiplier (e.g., 5x for Vesak)</param>
    /// <returns>Cache pre-warming strategy and status</returns>
    Task<CachePreWarmingResult> PreWarmCachesForCulturalEventsAsync(
        List<PerformanceCulturalEvent> culturalEvents, 
        decimal expectedTrafficMultiplier);

    /// <summary>
    /// Invalidate and refresh cache strategically during profile updates
    /// Maintains cache consistency while minimizing performance impact
    /// </summary>
    /// <param name="affectedUserIds">User IDs affected by profile changes</param>
    /// <param name="cacheInvalidationStrategy">Strategy for cache invalidation</param>
    /// <returns>Cache invalidation and refresh status</returns>
    Task<CacheInvalidationResult> RefreshLanguageRoutingCachesAsync(
        List<Guid> affectedUserIds, 
        CacheInvalidationStrategy cacheInvalidationStrategy);

    #endregion

    #region Disaster Recovery and Failover

    /// <summary>
    /// Execute cross-region language routing failover for disaster recovery
    /// Maintains <60 second failover time as per Cultural Intelligence ADR
    /// </summary>
    /// <param name="failoverContext">Disaster recovery failover context</param>
    /// <returns>Failover execution status with cultural intelligence preservation</returns>
    Task<LanguageRoutingFailoverResult> ExecuteCrossRegionLanguageRoutingFailoverAsync(DisasterRecoveryFailoverContext failoverContext);

    /// <summary>
    /// Preserve cultural intelligence state during disaster recovery scenarios
    /// Ensures sacred event continuity and diaspora community service preservation
    /// </summary>
    /// <param name="culturalIntelligenceState">Current cultural intelligence state</param>
    /// <param name="targetRegion">Target region for state replication</param>
    /// <returns>Cultural intelligence preservation status</returns>
    Task<CulturalIntelligencePreservationResult> PreserveCulturalIntelligenceStateAsync(
        CulturalIntelligenceState culturalIntelligenceState, 
        CulturalRegion targetRegion);

    #endregion
}

#region Supporting Models and Types

/// <summary>
/// Language complexity analysis for script optimization
/// </summary>
public class LanguageComplexityAnalysis
{
    public Dictionary<SouthAsianLanguage, ScriptComplexity> ScriptComplexities { get; set; } = new();
    public List<string> OptimizationRecommendations { get; set; } = new();
    public Dictionary<SouthAsianLanguage, RenderingRequirements> RenderingRequirements { get; set; } = new();
}

/// <summary>
/// Script complexity enumeration
/// </summary>
public enum ScriptComplexity
{
    Low,    // Latin-based scripts
    Medium, // Single-direction complex scripts (Devanagari)
    High,   // Bi-directional or highly complex scripts (Arabic, Sinhala)
    VeryHigh // Combined complexity scripts
}

/// <summary>
/// Rendering requirements for script optimization
/// </summary>
public class RenderingRequirements
{
    public bool RequiresComplexShaping { get; set; }
    public bool RequiresBidirectionalText { get; set; }
    public bool RequiresAdvancedFontFeatures { get; set; }
    public List<string> RecommendedFonts { get; set; } = new();
}

/// <summary>
/// Multi-cultural event resolution for overlapping celebrations
/// </summary>
public class MultiCulturalEventResolution
{
    public List<SouthAsianLanguage> PriorityLanguages { get; set; } = new();
    public Dictionary<DomainDatabase.CulturalEvent, decimal> EventWeights { get; set; } = new();
    public string ResolutionStrategy { get; set; } = string.Empty;
    public bool RequiresMultiCulturalContent { get; set; }
}

/// <summary>
/// Cultural event language prediction
/// </summary>
public class CulturalEventLanguagePrediction
{
    public Dictionary<DateTime, SouthAsianLanguage> PredictedLanguagePreferences { get; set; } = new();
    public List<PerformanceCulturalEvent> UpcomingEvents { get; set; } = new();
    public Dictionary<DomainDatabase.CulturalEvent, decimal> EventImpactScores { get; set; } = new();
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

/// <summary>
/// Language interaction data for learning
/// </summary>
public class LanguageInteractionData
{
    public SouthAsianLanguage Language { get; set; }
    public ContentType ContentType { get; set; }
    public TimeSpan InteractionDuration { get; set; }
    public decimal EngagementScore { get; set; }
    public DateTime InteractionTimestamp { get; set; }
}

/// <summary>
/// Batch routing response
/// </summary>
public class BatchMultiLanguageRoutingResponse
{
    public List<LankaConnect.Application.Common.Models.Routing.MultiLanguageRoutingResponse> IndividualResponses { get; set; } = new();
    public decimal AverageResponseTime { get; set; }
    public decimal BatchOptimizationGain { get; set; }
    public int TotalRequests { get; set; }
    public int SuccessfulRoutes { get; set; }
}

/// <summary>
/// Language proficiency levels
/// </summary>
public enum LanguageProficiencyLevel
{
    None,
    Basic,
    Intermediate,
    Advanced,
    Native
}

/// <summary>
/// Heritage language learning recommendations
/// </summary>
public class HeritageLanguageLearningRecommendations
{
    public List<string> RecommendedCourses { get; set; } = new();
    public List<PerformanceCulturalEvent> LearningOpportunityEvents { get; set; } = new();
    public Dictionary<string, decimal> LearningPathProgress { get; set; } = new();
    public List<string> CommunityLearningOpportunities { get; set; } = new();
}

/// <summary>
/// Cultural education pathway
/// </summary>
public class CulturalEducationPathway
{
    public List<string> EducationModules { get; set; } = new();
    public Dictionary<string, LanguageProficiencyLevel> LanguageProgression { get; set; } = new();
    public List<PerformanceCulturalEvent> CulturalMilestones { get; set; } = new();
    public TimeSpan EstimatedCompletionTime { get; set; }
}

/// <summary>
/// Language service types for monetization
/// </summary>
public enum LanguageServiceType
{
    Translation,
    Interpretation,
    CulturalConsulting,
    LanguageTutoring,
    ContentLocalization
}

#endregion