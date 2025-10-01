using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Shared;
using SouthAsianLanguage = LankaConnect.Domain.Common.Enums.SouthAsianLanguage;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Cultural data validator interface for ensuring cultural accuracy and integrity
/// </summary>
public interface ICulturalDataValidator
{
    /// <summary>
    /// Validates cultural data for accuracy and integrity
    /// </summary>
    Task<Result<CulturalDataValidationResult>> ValidateDataAsync(
        CulturalIntelligenceData data,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates sacred content for religious accuracy
    /// </summary>
    Task<Result<bool>> ValidateSacredContentAsync(
        string content,
        SacredContentType contentType,
        SouthAsianLanguage language,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates cultural event information
    /// </summary>
    Task<Result<bool>> ValidateCulturalEventAsync(
        CulturalEvent culturalEvent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets validation rules for a specific cultural context
    /// </summary>
    Task<Result<Dictionary<string, object>>> GetValidationRulesAsync(
        string culturalContext,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if content requires expert review
    /// </summary>
    Task<Result<bool>> RequiresExpertReviewAsync(
        CulturalIntelligenceData data,
        CancellationToken cancellationToken = default);
}