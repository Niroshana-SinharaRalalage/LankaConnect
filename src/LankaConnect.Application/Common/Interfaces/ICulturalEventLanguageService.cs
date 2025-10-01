using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LankaConnect.Application.Common.Models.MultiLanguage;
using LankaConnect.Application.Common.Models.Performance;
using LankaConnect.Domain.Common.Database;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Cultural Event Language Service Interface
/// Handles language preferences and routing during South Asian cultural events
/// Supports Vesak, Diwali, Eid, Vaisakhi with language-specific cultural intelligence
/// Decomposed from IMultiLanguageAffinityRoutingEngine following DDD principles
/// Performance target: 5x Vesak, 4.5x Diwali, 4x Eid traffic scaling
/// </summary>
public interface ICulturalEventLanguageService
{
    /// <summary>
    /// Calculate language preference boosts during cultural events
    /// Vesak: 5x boost for Sinhala, Diwali: 4.5x for Hindi/Tamil, Eid: 4x for Urdu
    /// </summary>
    /// <param name="userId">User identifier for personalized boost calculation</param>
    /// <param name="eventContext">Cultural event context and intensity</param>
    /// <returns>Event-specific language boost configuration</returns>
    Task<LankaConnect.Application.Common.Models.MultiLanguage.CulturalEventLanguageBoost> CalculateCulturalEventLanguageBoostAsync(Guid userId, CulturalEventContext eventContext);

    /// <summary>
    /// Handle multiple overlapping cultural events with conflict resolution
    /// Example: Diwali + Eid overlap requiring multi-cultural content strategy
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="overlappingEvents">List of concurrent cultural events</param>
    /// <returns>Conflict resolution strategy with language priorities</returns>
    Task<LankaConnect.Application.Common.Models.MultiLanguage.MultiCulturalEventResolution> ResolveMultiCulturalEventConflictsAsync(Guid userId, List<LankaConnect.Domain.Common.Database.CulturalEvent> overlappingEvents);

    /// <summary>
    /// Predict language preferences based on upcoming cultural events
    /// Enables proactive content preparation and resource allocation
    /// </summary>
    /// <param name="userId">User identifier</param>
    /// <param name="predictionPeriod">Timespan for prediction analysis</param>
    /// <returns>Predicted language preferences with event timeline</returns>
    Task<LankaConnect.Application.Common.Models.MultiLanguage.CulturalEventLanguagePrediction> PredictCulturalEventLanguagePreferencesAsync(Guid userId, TimeSpan predictionPeriod);

    /// <summary>
    /// Optimize language routing during cultural event traffic surges
    /// Handles 5x+ traffic increases with cultural sensitivity preservation
    /// </summary>
    /// <param name="culturalEvents">Active cultural events affecting routing</param>
    /// <param name="trafficMultipliers">Expected traffic increases per community</param>
    /// <returns>Optimized routing configuration for cultural events</returns>
    Task<LankaConnect.Application.Common.Models.MultiLanguage.CulturalEventRoutingOptimization> OptimizeCulturalEventLanguageRoutingAsync(
        List<LankaConnect.Domain.Common.Database.CulturalEvent> culturalEvents,
        Dictionary<CommunityType, decimal> trafficMultipliers);

    /// <summary>
    /// Generate cultural event language analytics for community insights
    /// Analyzes language usage patterns during major cultural celebrations
    /// </summary>
    /// <param name="eventAnalysisRequest">Cultural event analysis parameters</param>
    /// <returns>Language analytics specific to cultural events and communities</returns>
    Task<LankaConnect.Application.Common.Models.MultiLanguage.CulturalEventLanguageAnalytics> GenerateCulturalEventLanguageAnalyticsAsync(LankaConnect.Application.Common.Models.MultiLanguage.CulturalEventAnalysisRequest eventAnalysisRequest);

    /// <summary>
    /// Validate cultural event language routing effectiveness
    /// Monitors SLA compliance and cultural appropriateness during events
    /// </summary>
    /// <param name="culturalEvent">Cultural event to validate</param>
    /// <param name="validationPeriod">Period for effectiveness analysis</param>
    /// <returns>Validation metrics including performance and cultural accuracy</returns>
    Task<LankaConnect.Application.Common.Models.MultiLanguage.CulturalEventLanguageValidation> ValidateCulturalEventLanguageEffectivenessAsync(
        LankaConnect.Domain.Common.Database.CulturalEvent culturalEvent,
        TimeSpan validationPeriod);

    /// <summary>
    /// Get real-time cultural event language metrics
    /// Performance monitoring for Fortune 500 SLA compliance during events
    /// </summary>
    /// <param name="activeCulturalEvents">Currently active cultural events</param>
    /// <returns>Real-time metrics including latency, accuracy, and cultural appropriateness</returns>
    Task<LankaConnect.Application.Common.Models.MultiLanguage.CulturalEventLanguageMetrics> GetRealTimeCulturalEventMetricsAsync(List<LankaConnect.Domain.Common.Database.CulturalEvent> activeCulturalEvents);

    /// <summary>
    /// Pre-warm caches for anticipated cultural event language demands
    /// Proactive preparation for Vesak, Diwali, Eid traffic patterns
    /// </summary>
    /// <param name="upcomingEvents">Cultural events requiring cache preparation</param>
    /// <param name="communityDistribution">Expected community engagement patterns</param>
    /// <returns>Cache warming result with readiness status</returns>
    Task<LankaConnect.Application.Common.Models.MultiLanguage.CulturalEventCacheWarmingResult> PreWarmCulturalEventLanguageCachesAsync(
        List<LankaConnect.Domain.Common.Database.CulturalEvent> upcomingEvents,
        Dictionary<CommunityType, decimal> communityDistribution);
}