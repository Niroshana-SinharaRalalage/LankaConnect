using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.Database;
using LankaConnect.Application.Common.Models.MultiLanguage;
using LankaConnect.Application.Common.Models.Performance;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Revenue Optimization Service Interface
/// Handles language-based revenue opportunities for $25.7M platform optimization
/// Identifies 15-25% engagement increases and new revenue streams through South Asian language targeting
/// Decomposed from IMultiLanguageAffinityRoutingEngine following DDD principles
/// Supports cultural event monetization and premium heritage language content strategies
/// </summary>
public interface IRevenueOptimizationService
{
    /// <summary>
    /// Analyze language-based revenue opportunities for $25.7M platform optimization
    /// Identifies engagement increases (15-25%) and new revenue streams
    /// </summary>
    /// <param name="revenueAnalysisRequest">Revenue analysis parameters</param>
    /// <returns>Revenue optimization opportunities with engagement projections</returns>
    Task<LankaConnect.Application.Common.Models.MultiLanguage.LanguageRevenueAnalysisResult> AnalyzeLanguageBasedRevenueOpportunitiesAsync(LankaConnect.Application.Common.Models.MultiLanguage.LanguageRevenueAnalysisRequest revenueAnalysisRequest);

    /// <summary>
    /// Optimize business directory language matching for improved conversions
    /// Matches users with culturally and linguistically compatible businesses
    /// </summary>
    /// <param name="businessMatchingRequest">Business language matching parameters</param>
    /// <returns>Optimized business recommendations with conversion probability</returns>
    Task<LankaConnect.Application.Common.Models.MultiLanguage.BusinessLanguageMatchingResult> OptimizeBusinessDirectoryLanguageMatchingAsync(LankaConnect.Application.Common.Models.MultiLanguage.BusinessLanguageMatchingRequest businessMatchingRequest);

    /// <summary>
    /// Generate premium content strategies based on language preferences
    /// Creates monetization opportunities through heritage language content
    /// </summary>
    /// <param name="targetLanguages">Languages for premium content development</param>
    /// <param name="contentTypes">Types of premium content for development</param>
    /// <returns>Premium content strategy with revenue projections</returns>
    Task<LankaConnect.Application.Common.Models.MultiLanguage.PremiumContentStrategy> GeneratePremiumLanguageContentStrategyAsync(
        List<SouthAsianLanguage> targetLanguages,
        List<ContentType> contentTypes);

    /// <summary>
    /// Analyze cultural event monetization through language-specific services
    /// Optimizes revenue during cultural celebrations through targeted language services
    /// </summary>
    /// <param name="culturalEvents">Cultural events for monetization analysis</param>
    /// <param name="serviceTypes">Types of language services for revenue generation</param>
    /// <returns>Cultural event monetization strategy with revenue projections</returns>
    Task<LankaConnect.Application.Common.Models.Performance.CulturalEventMonetizationStrategy> AnalyzeCulturalEventLanguageMonetizationAsync(
        List<PerformanceCulturalEvent> culturalEvents,
        List<LankaConnect.Domain.Shared.LanguageServiceType> serviceTypes);
}