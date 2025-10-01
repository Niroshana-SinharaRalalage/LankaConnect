using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Communications.Services;

/// <summary>
/// Cultural Intelligence WhatsApp Service interface
/// Provides cultural awareness, timing optimization, and appropriateness validation for WhatsApp messaging
/// </summary>
public interface ICulturalWhatsAppService
{
    /// <summary>
    /// Validates cultural appropriateness of WhatsApp message content
    /// Considers religious sensitivities, cultural context, and festival timing
    /// </summary>
    Task<Result<double>> ValidateCulturalAppropriatenessAsync(WhatsAppMessage message);

    /// <summary>
    /// Optimizes message timing based on Buddhist/Hindu calendar and diaspora time zones
    /// </summary>
    Task<Result<DateTime>> OptimizeMessageTimingAsync(WhatsAppMessage message, DateTime requestedTime);

    /// <summary>
    /// Determines optimal diaspora region targeting based on message content and cultural context
    /// </summary>
    Task<Result<IEnumerable<string>>> GetOptimalDiasporaRegionsAsync(WhatsAppMessage message);

    /// <summary>
    /// Selects appropriate language for message based on recipient demographics and cultural preferences
    /// </summary>
    Task<Result<string>> SelectOptimalLanguageAsync(WhatsAppMessage message, IEnumerable<string> recipientPhoneNumbers);

    /// <summary>
    /// Generates culturally appropriate message content for festivals and religious observances
    /// </summary>
    Task<Result<string>> GenerateCulturallyAwareContentAsync(string baseContent, WhatsAppCulturalContext culturalContext, string language);

    /// <summary>
    /// Validates message timing against religious observance restrictions (e.g., Poyaday quiet periods)
    /// </summary>
    Task<Result<bool>> IsTimingReligiouslyAppropriateAsync(DateTime messageTime, WhatsAppCulturalContext culturalContext);

    /// <summary>
    /// Gets recommended cultural metadata for message enhancement
    /// </summary>
    Task<Result<Dictionary<string, string>>> GetRecommendedCulturalMetadataAsync(WhatsAppMessage message);
}

/// <summary>
/// Diaspora Notification Service interface for geographic community targeting
/// </summary>
public interface IDiasporaNotificationService
{
    /// <summary>
    /// Identifies optimal diaspora communities for message distribution
    /// Based on geographic clustering, cultural preferences, and community engagement
    /// </summary>
    Task<Result<IEnumerable<DiasporaCluster>>> GetTargetDiasporaCommunitiesAsync(WhatsAppMessage message);

    /// <summary>
    /// Optimizes broadcast timing across multiple time zones for global diaspora reach
    /// </summary>
    Task<Result<Dictionary<string, DateTime>>> OptimizeBroadcastTimingByRegionAsync(
        WhatsAppMessage message, 
        IEnumerable<DiasporaCluster> targetClusters);

    /// <summary>
    /// Estimates message reach and engagement based on diaspora community analytics
    /// </summary>
    Task<Result<DiasporaReachEstimate>> EstimateMessageReachAsync(WhatsAppMessage message, IEnumerable<DiasporaCluster> targetClusters);

    /// <summary>
    /// Gets localized message content for different diaspora regions
    /// </summary>
    Task<Result<Dictionary<string, string>>> GetLocalizedContentByRegionAsync(
        string baseContent, 
        IEnumerable<DiasporaCluster> targetClusters);
}

/// <summary>
/// Cultural Timing Optimizer interface for Buddhist/Hindu calendar integration
/// </summary>
public interface ICulturalTimingOptimizer
{
    /// <summary>
    /// Checks if the specified time conflicts with Buddhist observance periods (Poyaday, meditation hours)
    /// </summary>
    Task<Result<bool>> IsBuddhistObservanceTimeAsync(DateTime dateTime, string timeZone);

    /// <summary>
    /// Checks if the specified time conflicts with Hindu observance periods (prayer times, fasting days)
    /// </summary>
    Task<Result<bool>> IsHinduObservanceTimeAsync(DateTime dateTime, string timeZone);

    /// <summary>
    /// Finds the next optimal time for cultural message delivery, avoiding religious conflicts
    /// </summary>
    Task<Result<DateTime>> FindNextOptimalDeliveryTimeAsync(DateTime requestedTime, WhatsAppCulturalContext culturalContext, string timeZone);

    /// <summary>
    /// Gets culturally appropriate greeting time windows for different festivals
    /// </summary>
    Task<Result<TimeWindow>> GetFestivalGreetingWindowAsync(string festivalName, DateTime festivalDate, string timeZone);

    /// <summary>
    /// Validates that message timing respects cultural sensitivity periods
    /// </summary>
    Task<Result<bool>> ValidateMessageTimingSensitivityAsync(DateTime messageTime, WhatsAppCulturalContext culturalContext, string timeZone);
}

/// <summary>
/// Cultural Message Validator interface for content appropriateness scoring
/// </summary>
public interface ICulturalMessageValidator
{
    /// <summary>
    /// Analyzes message content for cultural appropriateness and sensitivity
    /// Returns score from 0 (inappropriate) to 1 (highly appropriate)
    /// </summary>
    Task<Result<CulturalValidationResult>> ValidateMessageContentAsync(string content, WhatsAppCulturalContext culturalContext, string language);

    /// <summary>
    /// Checks if message content contains potentially offensive or inappropriate cultural references
    /// </summary>
    Task<Result<IEnumerable<string>>> DetectCulturalSensitivityIssuesAsync(string content, string language);

    /// <summary>
    /// Suggests improvements to make message content more culturally appropriate
    /// </summary>
    Task<Result<IEnumerable<string>>> GetCulturalImprovementSuggestionsAsync(string content, WhatsAppCulturalContext culturalContext, string language);

    /// <summary>
    /// Validates that festival greetings are appropriate for the specific religious tradition
    /// </summary>
    Task<Result<bool>> ValidateFestivalGreetingAppropriatnessAsync(string greeting, string festivalName, string religion);
}

// Supporting value objects and DTOs

/// <summary>
/// Represents a diaspora community cluster for targeting
/// </summary>
public class DiasporaCluster
{
    public string Region { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string TimeZone { get; init; } = string.Empty;
    public int EstimatedPopulation { get; init; }
    public double EngagementScore { get; init; }
    public IEnumerable<string> PreferredLanguages { get; init; } = Enumerable.Empty<string>();
    public IEnumerable<string> DominantReligions { get; init; } = Enumerable.Empty<string>();
    public Dictionary<string, double> CulturalPreferences { get; init; } = new();
}

/// <summary>
/// Estimated reach and engagement for diaspora messaging
/// </summary>
public class DiasporaReachEstimate
{
    public int TotalReach { get; init; }
    public double ExpectedEngagementRate { get; init; }
    public Dictionary<string, int> ReachByRegion { get; init; } = new();
    public Dictionary<string, double> EngagementByLanguage { get; init; } = new();
    public double CulturalRelevanceScore { get; init; }
}

/// <summary>
/// Time window for cultural activities
/// </summary>
public class TimeWindow
{
    public DateTime StartTime { get; init; }
    public DateTime EndTime { get; init; }
    public string Description { get; init; } = string.Empty;
    public bool IsOptimal { get; init; }
}

/// <summary>
/// Result of cultural validation analysis
/// </summary>
public class CulturalValidationResult
{
    public double AppropriatnessScore { get; init; }
    public bool IsAcceptable { get; init; }
    public IEnumerable<string> Issues { get; init; } = Enumerable.Empty<string>();
    public IEnumerable<string> Suggestions { get; init; } = Enumerable.Empty<string>();
    public Dictionary<string, double> DetailedScores { get; init; } = new();
}