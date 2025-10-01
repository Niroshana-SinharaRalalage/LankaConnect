using LankaConnect.Domain.Events.ValueObjects.Recommendations;
using CulturalAppropriateness = LankaConnect.Domain.Communications.ValueObjects.CulturalAppropriateness;
using CommunityCluster = LankaConnect.Domain.Events.ValueObjects.Recommendations.CommunityCluster;
using CulturalSensitivityLevel = LankaConnect.Domain.Events.ValueObjects.Recommendations.CulturalSensitivityLevel;

namespace LankaConnect.Domain.Events.Services;

/// <summary>
/// Event Recommendation Engine - Domain Service for sophisticated event recommendation algorithms
/// Implements multi-criteria decision analysis with cultural intelligence, geographic optimization,
/// user preference learning, and advanced scoring mechanisms for Sri Lankan diaspora community.
/// </summary>
public interface IEventRecommendationEngine
{
    #region Core Recommendation Methods

    /// <summary>
    /// Get personalized event recommendations for a user
    /// </summary>
    Task<IEnumerable<EventRecommendation>> GetRecommendations(Guid userId, IEnumerable<Event> events);

    /// <summary>
    /// Get event recommendations for a specific date with cultural calendar integration
    /// </summary>
    Task<IEnumerable<EventRecommendation>> GetRecommendationsForDate(Guid userId, IEnumerable<Event> events, DateTime date);

    #endregion

    #region Cultural Intelligence Methods

    /// <summary>
    /// Get culturally filtered recommendations based on user preferences
    /// </summary>
    Task<IEnumerable<EventRecommendation>> GetCulturallyFilteredRecommendations(Guid userId, IEnumerable<Event> events);

    /// <summary>
    /// Get diaspora-optimized recommendations for expatriate Sri Lankan communities
    /// </summary>
    Task<IEnumerable<EventRecommendation>> GetDiasporaOptimizedRecommendations(Guid userId, IEnumerable<Event> events);

    /// <summary>
    /// Get festival-optimized recommendations aligned with traditional timing
    /// </summary>
    Task<IEnumerable<EventRecommendation>> GetFestivalOptimizedRecommendations(Guid userId, IEnumerable<Event> events, string festivalName);

    /// <summary>
    /// Get categorized recommendations by event nature (Religious/Cultural/Secular)
    /// </summary>
    Task<IEnumerable<EventRecommendation>> GetCategorizedRecommendations(Guid userId, IEnumerable<Event> events);

    /// <summary>
    /// Get calendar-validated recommendations ensuring cultural appropriateness
    /// </summary>
    Task<IEnumerable<EventRecommendation>> GetCalendarValidatedRecommendations(Guid userId, IEnumerable<Event> events);

    /// <summary>
    /// Calculate cultural appropriateness score for specific event and user
    /// </summary>
    Task<CulturalScore> CalculateCulturalScore(Guid userId, Event @event);

    #endregion

    #region Geographic Algorithm Methods

    /// <summary>
    /// Get cluster-optimized recommendations based on Sri Lankan community density
    /// </summary>
    Task<IEnumerable<EventRecommendation>> GetClusterOptimizedRecommendations(Guid userId, IEnumerable<Event> events);

    /// <summary>
    /// Get distance-filtered recommendations within user's travel preferences
    /// </summary>
    Task<IEnumerable<EventRecommendation>> GetDistanceFilteredRecommendations(Guid userId, IEnumerable<Event> events);

    /// <summary>
    /// Get regionally optimized recommendations based on community patterns
    /// </summary>
    Task<IEnumerable<EventRecommendation>> GetRegionalOptimizedRecommendations(Guid userId, IEnumerable<Event> events);

    /// <summary>
    /// Get accessibility-optimized recommendations considering transportation options
    /// </summary>
    Task<IEnumerable<EventRecommendation>> GetAccessibilityOptimizedRecommendations(Guid userId, IEnumerable<Event> events);

    /// <summary>
    /// Get proximity-optimized recommendations handling multi-location events
    /// </summary>
    Task<IEnumerable<EventRecommendation>> GetProximityOptimizedRecommendations(Guid userId, IEnumerable<Event> events);

    /// <summary>
    /// Get location edge case recommendations handling special geographic scenarios
    /// </summary>
    Task<IEnumerable<EventRecommendation>> GetLocationEdgeCaseRecommendations(Guid userId, IEnumerable<Event> events);

    #endregion

    #region User Preference Analysis Methods

    /// <summary>
    /// Get history-based recommendations analyzing user's attendance patterns
    /// </summary>
    Task<IEnumerable<EventRecommendation>> GetHistoryBasedRecommendations(Guid userId, IEnumerable<Event> events);

    /// <summary>
    /// Get adaptive recommendations using machine learning preference evolution
    /// </summary>
    Task<IEnumerable<EventRecommendation>> GetAdaptiveRecommendations(Guid userId, IEnumerable<Event> events);

    /// <summary>
    /// Get time-optimized recommendations based on user's schedule preferences
    /// </summary>
    Task<IEnumerable<EventRecommendation>> GetTimeOptimizedRecommendations(Guid userId, IEnumerable<Event> events);

    /// <summary>
    /// Get family-optimized recommendations considering family composition
    /// </summary>
    Task<IEnumerable<EventRecommendation>> GetFamilyOptimizedRecommendations(Guid userId, IEnumerable<Event> events);

    /// <summary>
    /// Get age-optimized recommendations correlated with age group preferences
    /// </summary>
    Task<IEnumerable<EventRecommendation>> GetAgeOptimizedRecommendations(Guid userId, IEnumerable<Event> events);

    /// <summary>
    /// Get language-optimized recommendations supporting multilingual preferences
    /// </summary>
    Task<IEnumerable<EventRecommendation>> GetLanguageOptimizedRecommendations(Guid userId, IEnumerable<Event> events);

    /// <summary>
    /// Get involvement-optimized recommendations matching community engagement levels
    /// </summary>
    Task<IEnumerable<EventRecommendation>> GetInvolvementOptimizedRecommendations(Guid userId, IEnumerable<Event> events);

    /// <summary>
    /// Record user interaction for machine learning and preference adaptation
    /// </summary>
    Task RecordUserInteraction(Guid userId, Event @event, UserInteraction interaction);

    #endregion

    #region Recommendation Scoring Methods

    /// <summary>
    /// Get scored recommendations using multi-criteria decision analysis
    /// </summary>
    Task<IEnumerable<EventRecommendation>> GetScoredRecommendations(Guid userId, IEnumerable<Event> events);

    /// <summary>
    /// Calculate personalized score using user-specific weighting preferences
    /// </summary>
    Task<PersonalizedEventScore> CalculatePersonalizedScore(Guid userId, Event @event, BaseEventScore baseScore);

    /// <summary>
    /// Get conflict-resolved recommendations handling scheduling and cultural conflicts
    /// </summary>
    Task<IEnumerable<ConflictResolvedRecommendation>> GetConflictResolvedRecommendations(Guid userId, IEnumerable<Event> events);

    /// <summary>
    /// Get edge case handled recommendations with robust error handling
    /// </summary>
    Task<IEnumerable<EventRecommendation>> GetEdgeCaseHandledRecommendations(Guid userId, IEnumerable<Event> events);

    /// <summary>
    /// Get normalized recommendations with score standardization across criteria
    /// </summary>
    Task<IEnumerable<NormalizedEventRecommendation>> GetNormalizedRecommendations(Guid userId, IEnumerable<Event> events, List<RawEventScores> rawScores);

    /// <summary>
    /// Get tie-broken recommendations using hierarchical tie-breaking logic
    /// </summary>
    Task<IEnumerable<EventRecommendation>> GetTieBrokenRecommendations(Guid userId, IEnumerable<Event> events);

    #endregion
}

/// <summary>
/// Supporting interfaces and dependencies for EventRecommendationEngine
/// These define the contracts that will be tested through mocks
/// </summary>

/// <summary>
/// Cultural Calendar service interface for Buddhist/Hindu calendar integration
/// </summary>
public interface ICulturalCalendar
{
    bool IsPoyaday(DateTime date);
    CulturalAppropriateness GetEventAppropriateness(Event @event, DateTime date);
    string ClassifyEventType(Event @event);
    DiasporaFriendliness GetDiasporaFriendliness(Event @event);
    CulturalAppropriateness CalculateAppropriateness(Event @event, string culturalBackground);
    FestivalPeriod GetFestivalPeriod(string festivalName, int year);
    bool IsOptimalFestivalTiming(Event @event, FestivalPeriod period);
    EventNature ClassifyEventNature(Event @event);
    SignificantDate[] GetSignificantDates(int year);
    CalendarValidationResult ValidateEventAgainstCalendar(Event @event);
}

/// <summary>
/// User Preferences service interface for learning and adaptation
/// </summary>
public interface IUserPreferences
{
    CulturalSensitivityLevel GetCulturalSensitivity(Guid userId);
    bool PrefersCulturallyAppropriateEvents(Guid userId);
    CulturalPreferences GetCulturalPreferences(Guid userId);
    DiasporaAdaptationLevel GetDiasporaAdaptationPreference(Guid userId);
    string GetCulturalBackground(Guid userId);
    bool PrefersFestivalAlignment(Guid userId);
    EventNaturePreferences GetEventNaturePreferences(Guid userId);
    AttendanceHistory GetAttendanceHistory(Guid userId);
    PreferencePatterns AnalyzePreferencePatterns(AttendanceHistory history);
    CulturalCategoryPreferences GetLearnedCulturalPreferences(Guid userId);
    void UpdatePreferenceLearning(Guid userId, Event @event, UserInteraction interaction);
    TimeSlotPreferences ExtractTimeSlotPreferences(Guid userId);
    TimeCompatibilityScore CalculateTimeCompatibility(Event @event, TimeSlotPreferences preferences);
    FamilyProfile GetFamilyProfile(Guid userId);
    FamilyCompatibilityScore CalculateFamilyCompatibility(Event @event, FamilyProfile profile);
    AgeGroupPreferences GetAgeGroupPreferences(int age);
    AgeCompatibilityScore CalculateAgeCompatibility(Event @event, AgeGroupPreferences preferences);
    LanguagePreferences GetLanguagePreferences(Guid userId);
    LanguageCompatibilityScore CalculateLanguageCompatibility(Event @event, LanguagePreferences preferences);
    CommunityInvolvementProfile GetCommunityInvolvementProfile(Guid userId);
    InvolvementCompatibilityScore CalculateInvolvementCompatibility(Event @event, CommunityInvolvementProfile profile);
    TransportationPreferences GetTransportationPreferences(Guid userId);
    Distance GetMaxTravelDistance(Guid userId);
    ScoringWeights GetScoringWeights(Guid userId);
    PersonalizedWeights CalculatePersonalizedWeights(Guid userId);
    PersonalizedEventScore ApplyPersonalizedWeighting(BaseEventScore baseScore, PersonalizedWeights weights);
    ConflictResolutionRules GetConflictResolutionRules(Guid userId);
    List<ConflictResolvedEvent> ResolveEventConflicts(List<Event> events, ConflictResolutionRules rules);
    EdgeCaseHandlingResult HandleScoringEdgeCases(Event @event);
    List<NormalizedEventScores> NormalizeScoresAcrossCriteria(List<RawEventScores> rawScores);
    TieBreakingRules GetTieBreakingRules(Guid userId);
    List<Event> ApplyTieBreakingLogic(List<Event> events, TieBreakingRules rules);
}

/// <summary>
/// Geographic Proximity service interface for location-based recommendations
/// </summary>
public interface IGeographicProximityService
{
    bool IsDiasporaLocation(string location);
    double GetCommunityDensity(string location);
    CommunityCluster[] AnalyzeCommunityCluster(string userLocation, IEnumerable<Event> events);
    Distance CalculateDistance(Coordinates from, Coordinates to);
    RegionalPreferences GetRegionalPreferences(string location);
    RegionalMatchScore CalculateRegionalMatch(Event @event, RegionalPreferences preferences);
    AccessibilityScore CalculateTransportationAccessibility(Event @event, TransportationPreferences preferences);
    MultiLocationProximity CalculateMultiLocationProximity(string userLocation, string[] eventLocations);
    ProximityScore CalculateProximityScore(MultiLocationProximity proximity);
    ProximityScore CalculateProximityScore(Event @event, string userLocation);
    LocationHandlingResult HandleLocationEdgeCase(string userLocation, Event @event);
    bool IsBorderLocation(string location);
}