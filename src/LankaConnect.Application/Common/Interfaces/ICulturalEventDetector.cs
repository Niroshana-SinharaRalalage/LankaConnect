using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Shared;
using SouthAsianLanguage = LankaConnect.Domain.Common.Enums.SouthAsianLanguage;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Cultural event detector interface for identifying and classifying cultural events
/// </summary>
public interface ICulturalEventDetector
{
    /// <summary>
    /// Detects cultural events in the provided data
    /// </summary>
    Task<Result<List<CulturalEvent>>> DetectEventsAsync(
        string dataSource,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Classifies the priority level of a cultural event
    /// </summary>
    Task<Result<SacredPriorityLevel>> ClassifyEventPriorityAsync(
        CulturalEvent culturalEvent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates cultural event data for accuracy
    /// </summary>
    Task<Result<bool>> ValidateEventDataAsync(
        CulturalEvent culturalEvent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets supported languages for cultural event detection
    /// </summary>
    Task<Result<List<SouthAsianLanguage>>> GetSupportedLanguagesAsync(
        CancellationToken cancellationToken = default);
}