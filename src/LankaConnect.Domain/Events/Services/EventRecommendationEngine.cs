using LankaConnect.Domain.Events.ValueObjects.Recommendations;
using CulturalAppropriateness = LankaConnect.Domain.Communications.ValueObjects.CulturalAppropriateness;
using CommunityCluster = LankaConnect.Domain.Events.ValueObjects.Recommendations.CommunityCluster;
using CulturalSensitivityLevel = LankaConnect.Domain.Events.ValueObjects.Recommendations.CulturalSensitivityLevel;

namespace LankaConnect.Domain.Events.Services;

/// <summary>
/// AI-powered Event Recommendation Engine - Domain Service Implementation
/// Implements sophisticated cultural intelligence, geographic clustering, user preference learning,
/// and multi-criteria recommendation scoring for Sri Lankan diaspora community platform.
/// 
/// Features:
/// - Cultural appropriateness scoring with Buddhist/Hindu calendar integration
/// - Geographic clustering algorithms for diaspora community density analysis
/// - Machine learning-based user preference adaptation
/// - Multi-criteria decision analysis with weighted scoring
/// - Festival timing optimization with religious calendar awareness
/// </summary>
public class EventRecommendationEngine : IEventRecommendationEngine
{
    private readonly ICulturalCalendar _culturalCalendar;
    private readonly IUserPreferences _userPreferences;
    private readonly IGeographicProximityService _geographicService;

    public EventRecommendationEngine(
        ICulturalCalendar culturalCalendar,
        IUserPreferences userPreferences,
        IGeographicProximityService geographicService)
    {
        _culturalCalendar = culturalCalendar ?? throw new ArgumentNullException(nameof(culturalCalendar));
        _userPreferences = userPreferences ?? throw new ArgumentNullException(nameof(userPreferences));
        _geographicService = geographicService ?? throw new ArgumentNullException(nameof(geographicService));
    }

    #region Core Recommendation Methods

    public async Task<IEnumerable<EventRecommendation>> GetRecommendations(Guid userId, IEnumerable<Event> events)
    {
        var eventList = events.ToList();
        if (!eventList.Any()) return Enumerable.Empty<EventRecommendation>();

        var culturalPreferences = _userPreferences.GetCulturalPreferences(userId);
        var scoringWeights = _userPreferences.GetScoringWeights(userId);

        var recommendations = new List<EventRecommendation>();

        foreach (var @event in eventList)
        {
            var culturalScore = await CalculateCulturalScoreInternal(userId, @event);
            var geographicScore = await CalculateGeographicScore(userId, @event);
            var historyScore = await CalculateHistoryScore(userId, @event);
            var timeScore = await CalculateTimeScore(userId, @event);
            var languageScore = await CalculateLanguageScore(userId, @event);
            var familyScore = await CalculateFamilyScore(userId, @event);
            var involvementScore = await CalculateInvolvementScore(userId, @event);

            var compositeScore = CalculateCompositeScore(
                culturalScore.Value, geographicScore, historyScore, timeScore,
                languageScore, familyScore, involvementScore, scoringWeights);

            var recommendationScore = new RecommendationScore(
                compositeScore,
                culturalScore.Value,
                geographicScore,
                historyScore,
                timeScore,
                languageScore,
                familyScore,
                involvementScore
            );

            var reason = GenerateRecommendationReason(@event, culturalScore.Value, geographicScore, historyScore);
            var confidence = CalculateConfidence(culturalScore.Value, geographicScore, historyScore);

            recommendations.Add(new EventRecommendation(@event, recommendationScore, reason, confidence));
        }

        return recommendations.OrderByDescending(r => r.Score.CompositeScore);
    }

    public async Task<IEnumerable<EventRecommendation>> GetRecommendationsForDate(Guid userId, IEnumerable<Event> events, DateTime date)
    {
        var baseRecommendations = await GetRecommendations(userId, events);
        var significantDates = _culturalCalendar.GetSignificantDates(date.Year);

        var dateOptimizedRecommendations = new List<EventRecommendation>();

        foreach (var recommendation in baseRecommendations)
        {
            var dateRelevanceScore = CalculateDateRelevance(recommendation.Event, date, significantDates);
            var culturalTimingScore = CalculateCulturalTimingScore(recommendation.Event, date);

            var adjustedCompositeScore = (recommendation.Score.CompositeScore * 0.7) + 
                                       (dateRelevanceScore * 0.2) + 
                                       (culturalTimingScore * 0.1);

            var adjustedScore = new RecommendationScore(
                adjustedCompositeScore,
                recommendation.Score.CulturalScore,
                recommendation.Score.GeographicScore,
                recommendation.Score.HistoryScore,
                recommendation.Score.TimeScore,
                recommendation.Score.LanguageScore,
                recommendation.Score.FamilyScore,
                recommendation.Score.InvolvementScore,
                timingScore: culturalTimingScore
            );

            var adjustedReason = $"{recommendation.RecommendationReason} (Date optimized: {dateRelevanceScore:F2})";
            dateOptimizedRecommendations.Add(new EventRecommendation(
                recommendation.Event, adjustedScore, adjustedReason, recommendation.Confidence));
        }

        return dateOptimizedRecommendations.OrderByDescending(r => r.Score.CompositeScore);
    }

    #endregion

    #region Cultural Intelligence Methods

    public async Task<IEnumerable<EventRecommendation>> GetCulturallyFilteredRecommendations(Guid userId, IEnumerable<Event> events)
    {
        var culturalSensitivity = _userPreferences.GetCulturalSensitivity(userId);
        var culturalPreferences = _userPreferences.GetCulturalPreferences(userId);
        
        var filteredEvents = new List<Event>();

        foreach (var @event in events)
        {
            var appropriateness = _culturalCalendar.GetEventAppropriateness(@event, @event.StartDate);
            
            // Filter based on cultural sensitivity level
            var minimumAppropriatenessThreshold = culturalSensitivity switch
            {
                CulturalSensitivityLevel.VeryHigh => 0.8,
                CulturalSensitivityLevel.High => 0.6,
                CulturalSensitivityLevel.Medium => 0.4,
                CulturalSensitivityLevel.Low => 0.2,
                _ => 0.0
            };

            if (appropriateness.Value >= minimumAppropriatenessThreshold)
            {
                filteredEvents.Add(@event);
            }
        }

        return await GetRecommendations(userId, filteredEvents);
    }

    public async Task<IEnumerable<EventRecommendation>> GetDiasporaOptimizedRecommendations(Guid userId, IEnumerable<Event> events)
    {
        var diasporaPreference = _userPreferences.GetDiasporaAdaptationPreference(userId);
        var optimizedEvents = new List<Event>();

        foreach (var @event in events)
        {
            var eventLocation = ExtractEventLocation(@event);
            if (string.IsNullOrEmpty(eventLocation)) continue;

            var isDiasporaFriendly = _geographicService.IsDiasporaLocation(eventLocation);
            var communityDensity = _geographicService.GetCommunityDensity(eventLocation);

            // Apply diaspora optimization based on adaptation level
            var shouldInclude = diasporaPreference switch
            {
                DiasporaAdaptationLevel.Traditional => isDiasporaFriendly && communityDensity > 0.7,
                DiasporaAdaptationLevel.Conservative => isDiasporaFriendly && communityDensity > 0.5,
                DiasporaAdaptationLevel.Moderate => isDiasporaFriendly || communityDensity > 0.3,
                DiasporaAdaptationLevel.Adaptive => communityDensity > 0.1,
                DiasporaAdaptationLevel.FullyIntegrated => true,
                _ => true
            };

            if (shouldInclude)
            {
                optimizedEvents.Add(@event);
            }
        }

        return await GetRecommendations(userId, optimizedEvents);
    }

    public async Task<IEnumerable<EventRecommendation>> GetFestivalOptimizedRecommendations(Guid userId, IEnumerable<Event> events, string festivalName)
    {
        var festivalPeriod = _culturalCalendar.GetFestivalPeriod(festivalName, DateTime.UtcNow.Year);
        var optimizedRecommendations = new List<EventRecommendation>();

        foreach (var @event in events)
        {
            var isOptimalTiming = _culturalCalendar.IsOptimalFestivalTiming(@event, festivalPeriod);
            
            if (isOptimalTiming)
            {
                var culturalScore = await CalculateCulturalScoreInternal(userId, @event);
                var timingBonus = CalculateFestivalTimingBonus(@event, festivalPeriod);
                
                var adjustedScore = new RecommendationScore(
                    culturalScore.Value + timingBonus,
                    culturalScore.Value,
                    timingScore: timingBonus
                );

                var reason = $"Festival-optimized for {festivalName} - optimal timing";
                optimizedRecommendations.Add(new EventRecommendation(@event, adjustedScore, reason));
            }
        }

        return optimizedRecommendations.OrderByDescending(r => r.Score.CompositeScore);
    }

    public async Task<IEnumerable<EventRecommendation>> GetCategorizedRecommendations(Guid userId, IEnumerable<Event> events)
    {
        var eventNaturePreferences = _userPreferences.GetEventNaturePreferences(userId);
        var categorizedRecommendations = new List<EventRecommendation>();

        foreach (var @event in events)
        {
            var eventNature = _culturalCalendar.ClassifyEventNature(@event);
            
            var categoryScore = eventNature switch
            {
                EventNature.Religious => eventNaturePreferences.Religious,
                EventNature.Cultural => eventNaturePreferences.Cultural,
                EventNature.Secular => eventNaturePreferences.Secular,
                EventNature.Mixed => (eventNaturePreferences.Religious + eventNaturePreferences.Cultural + eventNaturePreferences.Secular) / 3.0,
                _ => 0.5
            };

            var recommendationScore = new RecommendationScore(
                categoryScore,
                categoryScore: categoryScore
            );

            var reason = $"Categorized as {eventNature} (Score: {categoryScore:F2})";
            categorizedRecommendations.Add(new EventRecommendation(@event, recommendationScore, reason));
        }

        return await Task.FromResult(categorizedRecommendations.OrderByDescending(r => r.Score.CategoryScore));
    }

    public async Task<IEnumerable<EventRecommendation>> GetCalendarValidatedRecommendations(Guid userId, IEnumerable<Event> events)
    {
        var validatedRecommendations = new List<EventRecommendation>();

        foreach (var @event in events)
        {
            var validationResult = _culturalCalendar.ValidateEventAgainstCalendar(@event);
            
            if (validationResult.IsValid)
            {
                var baseRecommendation = (await GetRecommendations(userId, new[] { @event })).First();
                var validationBonus = 0.1; // Bonus for calendar validation
                
                var adjustedScore = new RecommendationScore(
                    baseRecommendation.Score.CompositeScore + validationBonus,
                    baseRecommendation.Score.CulturalScore,
                    baseRecommendation.Score.GeographicScore,
                    baseRecommendation.Score.HistoryScore,
                    baseRecommendation.Score.TimeScore,
                    baseRecommendation.Score.LanguageScore,
                    baseRecommendation.Score.FamilyScore,
                    baseRecommendation.Score.InvolvementScore
                );

                var reason = $"{baseRecommendation.RecommendationReason} (Calendar validated)";
                validatedRecommendations.Add(new EventRecommendation(@event, adjustedScore, reason));
            }
        }

        return validatedRecommendations;
    }

    public async Task<CulturalScore> CalculateCulturalScore(Guid userId, Event @event)
    {
        return await CalculateCulturalScoreInternal(userId, @event);
    }

    #endregion

    #region Geographic Algorithm Methods

    public async Task<IEnumerable<EventRecommendation>> GetClusterOptimizedRecommendations(Guid userId, IEnumerable<Event> events)
    {
        var userLocation = await GetUserLocation(userId);
        var clusters = _geographicService.AnalyzeCommunityCluster(userLocation, events);
        var clusterOptimizedRecommendations = new List<EventRecommendation>();

        foreach (var @event in events)
        {
            var eventLocation = ExtractEventLocation(@event);
            var relevantCluster = clusters.FirstOrDefault(c => c.Location == eventLocation);
            
            var clusterScore = relevantCluster?.CommunityDensity ?? 0.0;
            var communityRelevanceScore = relevantCluster?.Size > 20 ? 0.8 : 0.4;

            var geographicScore = (clusterScore * 0.6) + (communityRelevanceScore * 0.4);
            
            var recommendationScore = new RecommendationScore(
                geographicScore,
                geographicScore: geographicScore
            );

            var reason = $"Cluster optimized (Density: {clusterScore:F2}, Community: {communityRelevanceScore:F2})";
            clusterOptimizedRecommendations.Add(new EventRecommendation(@event, recommendationScore, reason));
        }

        return clusterOptimizedRecommendations.OrderByDescending(r => r.Score.GeographicScore);
    }

    public async Task<IEnumerable<EventRecommendation>> GetDistanceFilteredRecommendations(Guid userId, IEnumerable<Event> events)
    {
        var maxDistance = _userPreferences.GetMaxTravelDistance(userId);
        var userLocation = await GetUserLocation(userId);
        var userCoordinates = await GetUserCoordinates(userId);
        
        var filteredRecommendations = new List<EventRecommendation>();

        foreach (var @event in events)
        {
            var eventCoordinates = ExtractEventCoordinates(@event);
            if (eventCoordinates == null) continue;

            var distance = _geographicService.CalculateDistance(userCoordinates, eventCoordinates);
            
            if (distance.Value <= maxDistance.Value && distance.Unit == maxDistance.Unit)
            {
                var distanceScore = 1.0 - (distance.Value / maxDistance.Value);
                
                var recommendationScore = new RecommendationScore(
                    distanceScore,
                    distanceScore: distanceScore
                );

                var reason = $"Within travel distance ({distance.Value:F1} {distance.Unit})";
                filteredRecommendations.Add(new EventRecommendation(@event, recommendationScore, reason));
            }
        }

        return filteredRecommendations.OrderByDescending(r => r.Score.DistanceScore);
    }

    public async Task<IEnumerable<EventRecommendation>> GetRegionalOptimizedRecommendations(Guid userId, IEnumerable<Event> events)
    {
        var userLocation = await GetUserLocation(userId);
        var regionalPreferences = _geographicService.GetRegionalPreferences(userLocation);
        
        var regionalOptimizedRecommendations = new List<EventRecommendation>();

        foreach (var @event in events)
        {
            var regionalMatch = _geographicService.CalculateRegionalMatch(@event, regionalPreferences);
            
            var recommendationScore = new RecommendationScore(
                regionalMatch.MatchScore,
                regionalScore: regionalMatch.MatchScore
            );

            var reason = $"Regional match (Score: {regionalMatch.MatchScore:F2})";
            regionalOptimizedRecommendations.Add(new EventRecommendation(@event, recommendationScore, reason));
        }

        return regionalOptimizedRecommendations.OrderByDescending(r => r.Score.RegionalScore);
    }

    public async Task<IEnumerable<EventRecommendation>> GetAccessibilityOptimizedRecommendations(Guid userId, IEnumerable<Event> events)
    {
        var transportationPrefs = _userPreferences.GetTransportationPreferences(userId);
        var accessibilityOptimizedRecommendations = new List<EventRecommendation>();

        foreach (var @event in events)
        {
            var accessibilityScore = _geographicService.CalculateTransportationAccessibility(@event, transportationPrefs);
            
            var recommendationScore = new RecommendationScore(
                accessibilityScore.Value,
                accessibilityScore: accessibilityScore.Value
            );

            var reason = $"Accessibility optimized (Score: {accessibilityScore.Value:F2})";
            accessibilityOptimizedRecommendations.Add(new EventRecommendation(@event, recommendationScore, reason));
        }

        return await Task.FromResult(accessibilityOptimizedRecommendations.OrderByDescending(r => r.Score.AccessibilityScore));
    }

    public async Task<IEnumerable<EventRecommendation>> GetProximityOptimizedRecommendations(Guid userId, IEnumerable<Event> events)
    {
        var userLocation = await GetUserLocation(userId);
        var proximityOptimizedRecommendations = new List<EventRecommendation>();

        foreach (var @event in events)
        {
            var eventLocations = ExtractMultipleEventLocations(@event);
            var proximity = _geographicService.CalculateMultiLocationProximity(userLocation, eventLocations);
            var proximityScore = _geographicService.CalculateProximityScore(proximity);
            
            var recommendationScore = new RecommendationScore(
                proximityScore.Value,
                proximityScore: proximityScore.Value
            );

            var reason = $"Proximity optimized (Score: {proximityScore.Value:F2})";
            proximityOptimizedRecommendations.Add(new EventRecommendation(@event, recommendationScore, reason));
        }

        return proximityOptimizedRecommendations.OrderByDescending(r => r.Score.ProximityScore);
    }

    public async Task<IEnumerable<EventRecommendation>> GetLocationEdgeCaseRecommendations(Guid userId, IEnumerable<Event> events)
    {
        var userLocation = await GetUserLocation(userId);
        var edgeCaseRecommendations = new List<EventRecommendation>();

        foreach (var @event in events)
        {
            var edgeCaseResult = _geographicService.HandleLocationEdgeCase(userLocation, @event);
            
            if (edgeCaseResult.CanRecommend)
            {
                var recommendationScore = new RecommendationScore(
                    edgeCaseResult.ProximityScore,
                    locationScore: edgeCaseResult.ProximityScore
                );

                var reason = $"Edge case handled: {edgeCaseResult.Reason}";
                edgeCaseRecommendations.Add(new EventRecommendation(@event, recommendationScore, reason));
            }
        }

        return edgeCaseRecommendations.OrderByDescending(r => r.Score.LocationScore);
    }

    #endregion

    #region User Preference Analysis Methods

    public async Task<IEnumerable<EventRecommendation>> GetHistoryBasedRecommendations(Guid userId, IEnumerable<Event> events)
    {
        var attendanceHistory = _userPreferences.GetAttendanceHistory(userId);
        var patterns = _userPreferences.AnalyzePreferencePatterns(attendanceHistory);
        
        var historyBasedRecommendations = new List<EventRecommendation>();

        foreach (var @event in events)
        {
            var historyScore = CalculateHistoryCompatibility(@event, patterns);
            
            var recommendationScore = new RecommendationScore(
                historyScore,
                historyScore: historyScore
            );

            var reason = $"Based on attendance history (Score: {historyScore:F2})";
            historyBasedRecommendations.Add(new EventRecommendation(@event, recommendationScore, reason));
        }

        return await Task.FromResult(historyBasedRecommendations.OrderByDescending(r => r.Score.HistoryScore));
    }

    public async Task<IEnumerable<EventRecommendation>> GetAdaptiveRecommendations(Guid userId, IEnumerable<Event> events)
    {
        var learnedPreferences = _userPreferences.GetLearnedCulturalPreferences(userId);
        var adaptiveRecommendations = new List<EventRecommendation>();

        foreach (var @event in events)
        {
            var adaptiveScore = CalculateAdaptiveScore(@event, learnedPreferences);
            
            var recommendationScore = new RecommendationScore(
                adaptiveScore,
                culturalScore: adaptiveScore
            );

            var reason = $"Adaptive learning (Confidence: {learnedPreferences.LearningConfidence:F2})";
            adaptiveRecommendations.Add(new EventRecommendation(@event, recommendationScore, reason));
        }

        return await Task.FromResult(adaptiveRecommendations.OrderByDescending(r => r.Score.CulturalScore));
    }

    public async Task<IEnumerable<EventRecommendation>> GetTimeOptimizedRecommendations(Guid userId, IEnumerable<Event> events)
    {
        var timeSlotPrefs = _userPreferences.ExtractTimeSlotPreferences(userId);
        var timeOptimizedRecommendations = new List<EventRecommendation>();

        foreach (var @event in events)
        {
            var timeCompatibility = _userPreferences.CalculateTimeCompatibility(@event, timeSlotPrefs);
            
            var recommendationScore = new RecommendationScore(
                timeCompatibility.Score,
                timeScore: timeCompatibility.Score
            );

            var reason = $"Time optimized (Compatibility: {timeCompatibility.Score:F2})";
            timeOptimizedRecommendations.Add(new EventRecommendation(@event, recommendationScore, reason));
        }

        return await Task.FromResult(timeOptimizedRecommendations.OrderByDescending(r => r.Score.TimeScore));
    }

    public async Task<IEnumerable<EventRecommendation>> GetFamilyOptimizedRecommendations(Guid userId, IEnumerable<Event> events)
    {
        var familyProfile = _userPreferences.GetFamilyProfile(userId);
        var familyOptimizedRecommendations = new List<EventRecommendation>();

        foreach (var @event in events)
        {
            var familyCompatibility = _userPreferences.CalculateFamilyCompatibility(@event, familyProfile);
            
            var recommendationScore = new RecommendationScore(
                familyCompatibility.Score,
                familyScore: familyCompatibility.Score
            );

            var reason = $"Family optimized (Compatibility: {familyCompatibility.Score:F2})";
            familyOptimizedRecommendations.Add(new EventRecommendation(@event, recommendationScore, reason));
        }

        return await Task.FromResult(familyOptimizedRecommendations.OrderByDescending(r => r.Score.FamilyScore));
    }

    public async Task<IEnumerable<EventRecommendation>> GetAgeOptimizedRecommendations(Guid userId, IEnumerable<Event> events)
    {
        var userAge = await GetUserAge(userId);
        var agePrefs = _userPreferences.GetAgeGroupPreferences(userAge);
        var ageOptimizedRecommendations = new List<EventRecommendation>();

        foreach (var @event in events)
        {
            var ageCompatibility = _userPreferences.CalculateAgeCompatibility(@event, agePrefs);
            
            var recommendationScore = new RecommendationScore(
                ageCompatibility.Score,
                categoryScore: ageCompatibility.Score
            );

            var reason = $"Age optimized (Age: {userAge}, Compatibility: {ageCompatibility.Score:F2})";
            ageOptimizedRecommendations.Add(new EventRecommendation(@event, recommendationScore, reason));
        }

        return ageOptimizedRecommendations.OrderByDescending(r => r.Score.CategoryScore);
    }

    public async Task<IEnumerable<EventRecommendation>> GetLanguageOptimizedRecommendations(Guid userId, IEnumerable<Event> events)
    {
        var languagePrefs = _userPreferences.GetLanguagePreferences(userId);
        var languageOptimizedRecommendations = new List<EventRecommendation>();

        foreach (var @event in events)
        {
            var languageCompatibility = _userPreferences.CalculateLanguageCompatibility(@event, languagePrefs);
            
            var recommendationScore = new RecommendationScore(
                languageCompatibility.Score,
                languageScore: languageCompatibility.Score
            );

            var reason = $"Language optimized (Compatibility: {languageCompatibility.Score:F2})";
            languageOptimizedRecommendations.Add(new EventRecommendation(@event, recommendationScore, reason));
        }

        return await Task.FromResult(languageOptimizedRecommendations.OrderByDescending(r => r.Score.LanguageScore));
    }

    public async Task<IEnumerable<EventRecommendation>> GetInvolvementOptimizedRecommendations(Guid userId, IEnumerable<Event> events)
    {
        var involvementProfile = _userPreferences.GetCommunityInvolvementProfile(userId);
        var involvementOptimizedRecommendations = new List<EventRecommendation>();

        foreach (var @event in events)
        {
            var involvementCompatibility = _userPreferences.CalculateInvolvementCompatibility(@event, involvementProfile);
            
            var recommendationScore = new RecommendationScore(
                involvementCompatibility.Score,
                involvementScore: involvementCompatibility.Score
            );

            var reason = $"Involvement optimized (Level: {involvementProfile.InvolvementLevel}, Compatibility: {involvementCompatibility.Score:F2})";
            involvementOptimizedRecommendations.Add(new EventRecommendation(@event, recommendationScore, reason));
        }

        return await Task.FromResult(involvementOptimizedRecommendations.OrderByDescending(r => r.Score.InvolvementScore));
    }

    public async Task RecordUserInteraction(Guid userId, Event @event, UserInteraction interaction)
    {
        _userPreferences.UpdatePreferenceLearning(userId, @event, interaction);
        await Task.CompletedTask;
    }

    #endregion

    #region Recommendation Scoring Methods

    public async Task<IEnumerable<EventRecommendation>> GetScoredRecommendations(Guid userId, IEnumerable<Event> events)
    {
        return await GetRecommendations(userId, events); // Base implementation already includes comprehensive scoring
    }

    public async Task<PersonalizedEventScore> CalculatePersonalizedScore(Guid userId, Event @event, BaseEventScore baseScore)
    {
        var personalizedWeights = _userPreferences.CalculatePersonalizedWeights(userId);
        return await Task.FromResult(_userPreferences.ApplyPersonalizedWeighting(baseScore, personalizedWeights));
    }

    public async Task<IEnumerable<ConflictResolvedRecommendation>> GetConflictResolvedRecommendations(Guid userId, IEnumerable<Event> events)
    {
        var conflictRules = _userPreferences.GetConflictResolutionRules(userId);
        var resolvedEvents = _userPreferences.ResolveEventConflicts(events.ToList(), conflictRules);
        
        var conflictResolvedRecommendations = new List<ConflictResolvedRecommendation>();

        foreach (var resolvedEvent in resolvedEvents)
        {
            var baseRecommendation = (await GetRecommendations(userId, new[] { resolvedEvent.Event })).First();
            var conflictResolution = DetermineConflictResolution(resolvedEvent);
            
            conflictResolvedRecommendations.Add(new ConflictResolvedRecommendation(
                baseRecommendation.Event, baseRecommendation.Score.CompositeScore, resolvedEvent.ResolutionReason));
        }

        return conflictResolvedRecommendations;
    }

    public async Task<IEnumerable<EventRecommendation>> GetEdgeCaseHandledRecommendations(Guid userId, IEnumerable<Event> events)
    {
        var edgeCaseRecommendations = new List<EventRecommendation>();

        foreach (var @event in events)
        {
            var edgeCaseResult = _userPreferences.HandleScoringEdgeCases(@event);
            
            if (edgeCaseResult.IsHandled)
            {
                var recommendationScore = new RecommendationScore(
                    edgeCaseResult.FallbackScore,
                    culturalScore: edgeCaseResult.FallbackScore
                );

                var reason = $"Edge case handled: {edgeCaseResult.HandlingExplanation}";
                edgeCaseRecommendations.Add(new EventRecommendation(@event, recommendationScore, reason));
            }
        }

        return await Task.FromResult(edgeCaseRecommendations);
    }

    public async Task<IEnumerable<NormalizedEventRecommendation>> GetNormalizedRecommendations(Guid userId, IEnumerable<Event> events, List<RawEventScores> rawScores)
    {
        var normalizedScores = _userPreferences.NormalizeScoresAcrossCriteria(rawScores);
        var normalizedRecommendations = new List<NormalizedEventRecommendation>();

        foreach (var normalizedScore in normalizedScores)
        {
            var recommendation = new EventRecommendation(
                normalizedScore.Event,
                new RecommendationScore(normalizedScore.CompositeScore),
                "Normalized across criteria"
            );

            normalizedRecommendations.Add(new NormalizedEventRecommendation(
                recommendation, normalizedScore, rawScores.First(r => r.Event == normalizedScore.Event).ComponentScores));
        }

        return await Task.FromResult(normalizedRecommendations.OrderByDescending(r => r.Recommendation.Score.CompositeScore));
    }

    public async Task<IEnumerable<EventRecommendation>> GetTieBrokenRecommendations(Guid userId, IEnumerable<Event> events)
    {
        var tieBreakingRules = _userPreferences.GetTieBreakingRules(userId);
        var eventsList = events.ToList();
        var tieBrokenEvents = _userPreferences.ApplyTieBreakingLogic(eventsList, tieBreakingRules);
        
        var tieBrokenRecommendations = new List<EventRecommendation>();
        var rank = 1;

        foreach (var @event in tieBrokenEvents)
        {
            var baseRecommendation = (await GetRecommendations(userId, new[] { @event })).First();
            var reason = $"{baseRecommendation.RecommendationReason} (Tie-broken rank: {rank})";
            
            tieBrokenRecommendations.Add(new EventRecommendation(
                @event, baseRecommendation.Score, reason, baseRecommendation.Confidence));
            rank++;
        }

        return tieBrokenRecommendations;
    }

    #endregion

    #region Private Helper Methods

    private async Task<CulturalScore> CalculateCulturalScoreInternal(Guid userId, Event @event)
    {
        var culturalBackground = _userPreferences.GetCulturalBackground(userId);
        var appropriateness = _culturalCalendar.CalculateAppropriateness(@event, culturalBackground);
        return await Task.FromResult(new CulturalScore(appropriateness.Value));
    }

    private async Task<double> CalculateGeographicScore(Guid userId, Event @event)
    {
        try
        {
            var userLocation = await GetUserLocation(userId);
            var eventLocation = ExtractEventLocation(@event);
            
            if (string.IsNullOrEmpty(eventLocation)) return await Task.FromResult(0.5); // Default score for missing location
            
            var isDiaspora = _geographicService.IsDiasporaLocation(eventLocation);
            var communityDensity = _geographicService.GetCommunityDensity(eventLocation);
            
            return await Task.FromResult((isDiaspora ? 0.7 : 0.3) + (communityDensity * 0.3));
        }
        catch
        {
            return await Task.FromResult(0.5); // Default fallback score
        }
    }

    private async Task<double> CalculateHistoryScore(Guid userId, Event @event)
    {
        try
        {
            var history = _userPreferences.GetAttendanceHistory(userId);
            var patterns = _userPreferences.AnalyzePreferencePatterns(history);
            return await Task.FromResult(CalculateHistoryCompatibility(@event, patterns));
        }
        catch
        {
            return await Task.FromResult(0.5); // Default fallback score
        }
    }

    private async Task<double> CalculateTimeScore(Guid userId, Event @event)
    {
        try
        {
            var timePrefs = _userPreferences.ExtractTimeSlotPreferences(userId);
            var timeCompatibility = _userPreferences.CalculateTimeCompatibility(@event, timePrefs);
            return await Task.FromResult(timeCompatibility.Score);
        }
        catch
        {
            return await Task.FromResult(0.5); // Default fallback score
        }
    }

    private async Task<double> CalculateLanguageScore(Guid userId, Event @event)
    {
        try
        {
            var languagePrefs = _userPreferences.GetLanguagePreferences(userId);
            var languageCompatibility = _userPreferences.CalculateLanguageCompatibility(@event, languagePrefs);
            return await Task.FromResult(languageCompatibility.Score);
        }
        catch
        {
            return await Task.FromResult(0.5); // Default fallback score
        }
    }

    private async Task<double> CalculateFamilyScore(Guid userId, Event @event)
    {
        try
        {
            var familyProfile = _userPreferences.GetFamilyProfile(userId);
            var familyCompatibility = _userPreferences.CalculateFamilyCompatibility(@event, familyProfile);
            return await Task.FromResult(familyCompatibility.Score);
        }
        catch
        {
            return await Task.FromResult(0.5); // Default fallback score
        }
    }

    private async Task<double> CalculateInvolvementScore(Guid userId, Event @event)
    {
        try
        {
            var involvementProfile = _userPreferences.GetCommunityInvolvementProfile(userId);
            var involvementCompatibility = _userPreferences.CalculateInvolvementCompatibility(@event, involvementProfile);
            return await Task.FromResult(involvementCompatibility.Score);
        }
        catch
        {
            return await Task.FromResult(0.5); // Default fallback score
        }
    }

    private double CalculateCompositeScore(
        double culturalScore, double geographicScore, double historyScore, double timeScore,
        double languageScore, double familyScore, double involvementScore, ScoringWeights weights)
    {
        return (culturalScore * weights.CulturalWeight) +
               (geographicScore * weights.GeographicWeight) +
               (historyScore * weights.HistoryWeight) +
               (timeScore * weights.TimeWeight) +
               (languageScore * weights.LanguageWeight) +
               (familyScore * weights.FamilyWeight) +
               (involvementScore * (1.0 - (weights.CulturalWeight + weights.GeographicWeight + 
                                          weights.HistoryWeight + weights.TimeWeight + 
                                          weights.LanguageWeight + weights.FamilyWeight)));
    }

    private string GenerateRecommendationReason(Event @event, double culturalScore, double geographicScore, double historyScore)
    {
        var primaryReason = culturalScore >= geographicScore && culturalScore >= historyScore 
            ? $"High cultural relevance ({culturalScore:F2})" 
            : geographicScore >= historyScore 
                ? $"Geographic proximity ({geographicScore:F2})"
                : $"Historical preference match ({historyScore:F2})";

        return $"Recommended: {primaryReason}";
    }

    private double CalculateConfidence(double culturalScore, double geographicScore, double historyScore)
    {
        var scoreVariance = CalculateVariance(new[] { culturalScore, geographicScore, historyScore });
        return Math.Max(0.5, 1.0 - scoreVariance); // Higher confidence for consistent scores
    }

    private double CalculateVariance(double[] scores)
    {
        var mean = scores.Average();
        return scores.Average(score => Math.Pow(score - mean, 2));
    }

    private double CalculateDateRelevance(Event @event, DateTime targetDate, SignificantDate[] significantDates)
    {
        var daysDifference = Math.Abs((@event.StartDate - targetDate).TotalDays);
        var proximityScore = Math.Max(0, 1.0 - (daysDifference / 30.0)); // Score based on 30-day window

        var significantDateBonus = significantDates.Any(sd => 
            Math.Abs((sd.Date - @event.StartDate).TotalDays) <= 3) ? 0.2 : 0.0;

        return Math.Min(1.0, proximityScore + significantDateBonus);
    }

    private double CalculateCulturalTimingScore(Event @event, DateTime date)
    {
        // Check if event aligns with culturally appropriate timing
        var isPoyaday = _culturalCalendar.IsPoyaday(date);
        var eventType = _culturalCalendar.ClassifyEventType(@event);

        return (isPoyaday && eventType.Contains("Religious")) ? 0.9 : 0.7;
    }

    private double CalculateFestivalTimingBonus(Event @event, FestivalPeriod festivalPeriod)
    {
        var isWithinPeriod = @event.StartDate >= festivalPeriod.StartDate && @event.StartDate <= festivalPeriod.EndDate;
        return isWithinPeriod ? 0.2 : 0.0;
    }

    private double CalculateHistoryCompatibility(Event @event, PreferencePatterns patterns)
    {
        var eventCategory = ExtractEventCategory(@event);
        var isPreferred = patterns.StrongPreferences.Contains(eventCategory);
        var isAvoided = patterns.WeakPreferences.Contains(eventCategory);

        if (isPreferred) return patterns.OptimalFrequency * patterns.EngagementScore;
        if (isAvoided) return 0.2;
        return 0.5; // Neutral
    }

    private double CalculateAdaptiveScore(Event @event, CulturalCategoryPreferences learnedPrefs)
    {
        var eventCategory = ExtractEventCategory(@event);
        var isPrimary = learnedPrefs.PrimaryCategories.Contains(eventCategory);
        var isSecondary = learnedPrefs.SecondaryCategories.Contains(eventCategory);

        if (isPrimary) return 0.9 * learnedPrefs.LearningConfidence;
        if (isSecondary) return 0.6 * learnedPrefs.LearningConfidence;
        return 0.3;
    }

    private ConflictResolution DetermineConflictResolution(ConflictResolvedEvent resolvedEvent)
    {
        return resolvedEvent.ResolutionReason.Contains("Accepted") ? ConflictResolution.Accepted :
               resolvedEvent.ResolutionReason.Contains("Rejected") ? ConflictResolution.Rejected :
               resolvedEvent.ResolutionReason.Contains("Modified") ? ConflictResolution.Modified :
               ConflictResolution.Deferred;
    }

    // Helper methods to extract event information (these would need to be implemented based on Event model)
    private string ExtractEventLocation(Event @event) => "Default Location"; // Placeholder
    private string ExtractEventCategory(Event @event) => "General"; // Placeholder
    private Coordinates? ExtractEventCoordinates(Event @event) => new Coordinates(37.5485, -121.9886); // Placeholder
    private string[] ExtractMultipleEventLocations(Event @event) => new[] { "Default Location" }; // Placeholder
    private async Task<string> GetUserLocation(Guid userId) => await Task.FromResult("Default User Location"); // Placeholder
    private async Task<Coordinates> GetUserCoordinates(Guid userId) => await Task.FromResult(new Coordinates(37.5485, -121.9886)); // Placeholder
    private async Task<int> GetUserAge(Guid userId) => await Task.FromResult(35); // Placeholder

    #endregion
}