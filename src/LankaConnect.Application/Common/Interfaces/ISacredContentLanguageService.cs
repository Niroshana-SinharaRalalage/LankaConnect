using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LankaConnect.Application.Common.Models.MultiLanguage;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Sacred Content Language Service Interface
/// Handles culturally appropriate language validation for religious and sacred content
/// Ensures Buddhist content → Sinhala requirements, Hindu content → Tamil/Hindi/Sanskrit
/// Decomposed from IMultiLanguageAffinityRoutingEngine following DDD principles
/// Cultural sensitivity: Prevents inappropriate language mismatches in sacred contexts
/// </summary>
public interface ISacredContentLanguageService
{
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
    Task<List<SouthAsianLanguage>> GenerateSacredContentLanguageAlternativesAsync(
        SouthAsianLanguage primaryLanguage,
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
        SouthAsianLanguage sourceLanguage,
        SouthAsianLanguage targetLanguage,
        LankaConnect.Application.Common.Models.MultiLanguage.SacredContentType sacredContentType);
}