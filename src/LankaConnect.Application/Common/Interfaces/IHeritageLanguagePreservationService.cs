using System;
using System.Threading.Tasks;
using LankaConnect.Application.Common.Models.MultiLanguage;
using LankaConnect.Domain.Common.Enums;
// Note: Some types are temporarily embedded in IMultiLanguageAffinityRoutingEngine.cs (architectural violation to fix)

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Heritage Language Preservation Service Interface
/// Handles critical cultural intelligence and community engagement for South Asian diaspora
/// Supports intergenerational language bridging and cultural education pathways
/// Decomposed from IMultiLanguageAffinityRoutingEngine following DDD principles
/// Drives heritage language preservation through targeted recommendations and cultural content
/// </summary>
public interface IHeritageLanguagePreservationService
{
    /// <summary>
    /// Analyze heritage language preservation patterns within diaspora communities
    /// Critical for cultural intelligence and community engagement strategies
    /// </summary>
    /// <param name="preservationRequest">Heritage language preservation analysis request</param>
    /// <returns>Comprehensive preservation analysis with recommendations</returns>
    Task<LankaConnect.Application.Common.Models.MultiLanguage.HeritageLanguagePreservationResult> AnalyzeHeritageLanguagePreservationAsync(LankaConnect.Application.Common.Models.MultiLanguage.HeritageLanguagePreservationRequest preservationRequest);

    /// <summary>
    /// Generate intergenerational content bridging different language preferences
    /// Connects first-generation heritage speakers with English-dominant younger generations
    /// </summary>
    /// <param name="contentRequest">Intergenerational content generation request</param>
    /// <returns>Bilingual content strategy with cultural bridging elements</returns>
    Task<LankaConnect.Application.Common.Models.MultiLanguage.IntergenerationalContentResult> GenerateIntergenerationalContentAsync(LankaConnect.Application.Common.Models.MultiLanguage.IntergenerationalContentRequest contentRequest);

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
        CulturalBackground culturalBackground,
        LanguageProficiencyLevel currentLanguageLevel);
}