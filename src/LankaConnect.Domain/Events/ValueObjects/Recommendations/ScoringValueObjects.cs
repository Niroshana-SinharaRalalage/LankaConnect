using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Events.ValueObjects.Recommendations;

/// <summary>
/// Base event score containing raw scoring components
/// </summary>
public class BaseEventScore : ValueObject
{
    public double CulturalScore { get; }
    public double GeographicScore { get; }
    public double TimeScore { get; }
    public double CommunityScore { get; }
    public double NoveltyScore { get; }
    public double PopularityScore { get; }

    public BaseEventScore(
        double culturalScore, 
        double geographicScore, 
        double timeScore, 
        double communityScore, 
        double noveltyScore,
        double popularityScore = 0.5)
    {
        CulturalScore = ValidateScore(culturalScore);
        GeographicScore = ValidateScore(geographicScore);
        TimeScore = ValidateScore(timeScore);
        CommunityScore = ValidateScore(communityScore);
        NoveltyScore = ValidateScore(noveltyScore);
        PopularityScore = ValidateScore(popularityScore);
    }

    private static double ValidateScore(double score)
    {
        if (double.IsNaN(score) || double.IsInfinity(score))
            return 0.0;
        return Math.Max(0.0, Math.Min(1.0, score));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return CulturalScore;
        yield return GeographicScore;
        yield return TimeScore;
        yield return CommunityScore;
        yield return NoveltyScore;
        yield return PopularityScore;
    }
}

/// <summary>
/// Scoring weights for multi-criteria decision analysis
/// </summary>
public class ScoringWeights : ValueObject
{
    public double CulturalWeight { get; }
    public double GeographicWeight { get; }
    public double HistoryWeight { get; }
    public double TimeWeight { get; }
    public double LanguageWeight { get; }
    public double FamilyWeight { get; }
    
    public ScoringWeights(
        double culturalWeight = 0.35,
        double geographicWeight = 0.25,
        double historyWeight = 0.20,
        double timeWeight = 0.10,
        double languageWeight = 0.05,
        double familyWeight = 0.05)
    {
        // Normalize weights to sum to 1.0
        var total = culturalWeight + geographicWeight + historyWeight + timeWeight + languageWeight + familyWeight;
        
        if (total <= 0)
            throw new ArgumentException("Sum of weights must be greater than 0");

        CulturalWeight = culturalWeight / total;
        GeographicWeight = geographicWeight / total;
        HistoryWeight = historyWeight / total;
        TimeWeight = timeWeight / total;
        LanguageWeight = languageWeight / total;
        FamilyWeight = familyWeight / total;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return CulturalWeight;
        yield return GeographicWeight;
        yield return HistoryWeight;
        yield return TimeWeight;
        yield return LanguageWeight;
        yield return FamilyWeight;
    }
}

/// <summary>
/// Personalized weights based on user behavior analysis
/// </summary>
public class PersonalizedWeights : ValueObject
{
    public double CulturalImportance { get; }
    public double ConvenienceImportance { get; }
    public double SocialImportance { get; }
    public double NoveltyImportance { get; }
    public double WeightConfidence { get; }

    public PersonalizedWeights(
        double culturalImportance = 0.4,
        double convenienceImportance = 0.3,
        double socialImportance = 0.2,
        double noveltyImportance = 0.1,
        double weightConfidence = 1.0)
    {
        // Normalize weights
        var total = culturalImportance + convenienceImportance + socialImportance + noveltyImportance;
        
        if (total <= 0)
            throw new ArgumentException("Sum of weights must be greater than 0");

        CulturalImportance = culturalImportance / total;
        ConvenienceImportance = convenienceImportance / total;
        SocialImportance = socialImportance / total;
        NoveltyImportance = noveltyImportance / total;
        WeightConfidence = Math.Max(0.0, Math.Min(1.0, weightConfidence));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return CulturalImportance;
        yield return ConvenienceImportance;
        yield return SocialImportance;
        yield return NoveltyImportance;
        yield return WeightConfidence;
    }
}

/// <summary>
/// Personalized event score with component breakdown
/// </summary>
public class PersonalizedEventScore : ValueObject
{
    public double WeightedScore { get; }
    public ComponentScores ComponentScores { get; }
    public double WeightingConfidence { get; }

    public PersonalizedEventScore(double weightedScore, ComponentScores componentScores, double weightingConfidence)
    {
        WeightedScore = Math.Max(0.0, Math.Min(1.0, weightedScore));
        ComponentScores = componentScores ?? throw new ArgumentNullException(nameof(componentScores));
        WeightingConfidence = Math.Max(0.0, Math.Min(1.0, weightingConfidence));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return WeightedScore;
        yield return ComponentScores;
        yield return WeightingConfidence;
    }
}

/// <summary>
/// Component scores breakdown for transparency
/// </summary>
public class ComponentScores : ValueObject
{
    public double CulturalComponent { get; }
    public double ConvenienceComponent { get; }
    public double SocialComponent { get; }
    public double NoveltyComponent { get; }

    public ComponentScores(double culturalComponent, double convenienceComponent, double socialComponent, double noveltyComponent)
    {
        CulturalComponent = Math.Max(0.0, culturalComponent);
        ConvenienceComponent = Math.Max(0.0, convenienceComponent);
        SocialComponent = Math.Max(0.0, socialComponent);
        NoveltyComponent = Math.Max(0.0, noveltyComponent);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return CulturalComponent;
        yield return ConvenienceComponent;
        yield return SocialComponent;
        yield return NoveltyComponent;
    }
}

/// <summary>
/// Conflict resolution rules for handling scheduling conflicts
/// </summary>
public class ConflictResolutionRules : ValueObject
{
    public double ReligiousEventPriority { get; }
    public double CulturalEventPriority { get; }
    public double SocialEventPriority { get; }
    public double TimeConflictPenalty { get; }
    public double CulturalConflictPenalty { get; }

    public ConflictResolutionRules(
        double religiousEventPriority = 0.9,
        double culturalEventPriority = 0.7,
        double socialEventPriority = 0.4,
        double timeConflictPenalty = -0.3,
        double culturalConflictPenalty = -0.6)
    {
        ReligiousEventPriority = Math.Max(0.0, Math.Min(1.0, religiousEventPriority));
        CulturalEventPriority = Math.Max(0.0, Math.Min(1.0, culturalEventPriority));
        SocialEventPriority = Math.Max(0.0, Math.Min(1.0, socialEventPriority));
        TimeConflictPenalty = Math.Max(-1.0, Math.Min(0.0, timeConflictPenalty));
        CulturalConflictPenalty = Math.Max(-1.0, Math.Min(0.0, culturalConflictPenalty));
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return ReligiousEventPriority;
        yield return CulturalEventPriority;
        yield return SocialEventPriority;
        yield return TimeConflictPenalty;
        yield return CulturalConflictPenalty;
    }
}

/// <summary>
/// Conflict types for resolution prioritization
/// </summary>
public enum ConflictType
{
    None = 0,
    TimeOverlap = 1,
    CulturalConflict = 2,
    ResourceConflict = 3,
    LocationConflict = 4
}

/// <summary>
/// Event priority levels
/// </summary>
public enum Priority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

/// <summary>
/// Conflict resolved event with resolution details
/// </summary>
public class ConflictResolvedEvent : ValueObject
{
    public Event Event { get; }
    public double ResolvedScore { get; }
    public string ConflictResolution { get; }
    public string ResolutionReason => ConflictResolution; // Alias for backward compatibility
    public ConflictType OriginalConflict { get; }

    public ConflictResolvedEvent(Event @event, double resolvedScore, string conflictResolution, ConflictType originalConflict = ConflictType.None)
    {
        Event = @event ?? throw new ArgumentNullException(nameof(@event));
        ResolvedScore = Math.Max(0.0, Math.Min(1.0, resolvedScore));
        ConflictResolution = conflictResolution ?? string.Empty;
        OriginalConflict = originalConflict;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Event;
        yield return ResolvedScore;
        yield return ConflictResolution;
        yield return OriginalConflict;
    }
}

/// <summary>
/// Conflict resolved recommendation
/// </summary>
public class ConflictResolvedRecommendation : ValueObject
{
    public Event Event { get; }
    public double Score { get; }
    public string Resolution { get; }

    public ConflictResolvedRecommendation(Event @event, double score, string resolution)
    {
        Event = @event ?? throw new ArgumentNullException(nameof(@event));
        Score = Math.Max(0.0, Math.Min(1.0, score));
        Resolution = resolution ?? string.Empty;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Event;
        yield return Score;
        yield return Resolution;
    }
}

/// <summary>
/// Edge case handling result for robust error handling
/// </summary>
public class EdgeCaseHandlingResult : ValueObject
{
    public bool CanScore { get; }
    public bool IsHandled { get; }
    public double DefaultScore { get; }
    public double FallbackScore { get; }
    public string HandlingStrategy { get; }
    public string HandlingExplanation { get; }

    public EdgeCaseHandlingResult(bool canScore, double defaultScore, string handlingStrategy)
        : this(canScore, canScore, defaultScore, defaultScore, handlingStrategy, handlingStrategy)
    {
    }

    public EdgeCaseHandlingResult(
        bool canScore, 
        bool isHandled, 
        double defaultScore, 
        double fallbackScore, 
        string handlingStrategy, 
        string handlingExplanation)
    {
        CanScore = canScore;
        IsHandled = isHandled;
        DefaultScore = Math.Max(0.0, Math.Min(1.0, defaultScore));
        FallbackScore = Math.Max(0.0, Math.Min(1.0, fallbackScore));
        HandlingStrategy = handlingStrategy ?? string.Empty;
        HandlingExplanation = handlingExplanation ?? string.Empty;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return CanScore;
        yield return IsHandled;
        yield return DefaultScore;
        yield return FallbackScore;
        yield return HandlingStrategy;
        yield return HandlingExplanation;
    }
}

/// <summary>
/// Raw event scores before normalization
/// </summary>
public class RawEventScores : ValueObject
{
    public Event Event { get; }
    public ComponentScores ComponentScores { get; }
    public double CulturalRaw { get; }
    public double GeographicRaw { get; }
    public double TimeRaw { get; }
    public double HistoryRaw { get; }

    public RawEventScores(Event @event, ComponentScores componentScores)
        : this(@event, componentScores, componentScores.CulturalComponent, componentScores.ConvenienceComponent, 
               componentScores.SocialComponent, componentScores.NoveltyComponent)
    {
    }

    public RawEventScores(Event @event, ComponentScores componentScores, double culturalRaw, double geographicRaw, double timeRaw, double historyRaw)
    {
        Event = @event ?? throw new ArgumentNullException(nameof(@event));
        ComponentScores = componentScores ?? throw new ArgumentNullException(nameof(componentScores));
        CulturalRaw = culturalRaw;
        GeographicRaw = geographicRaw;
        TimeRaw = timeRaw;
        HistoryRaw = historyRaw;
    }

    public RawEventScores(double culturalRaw, double geographicRaw, double timeRaw, double historyRaw)
        : this(Event.CreateDefault(), new ComponentScores(culturalRaw, geographicRaw, timeRaw, historyRaw), culturalRaw, geographicRaw, timeRaw, historyRaw)
    {
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Event;
        yield return ComponentScores;
        yield return CulturalRaw;
        yield return GeographicRaw;
        yield return TimeRaw;
        yield return HistoryRaw;
    }
}

/// <summary>
/// Normalized event scores (0-1 range)
/// </summary>
public class NormalizedEventScores : ValueObject
{
    public Event Event { get; }
    public ComponentScores NormalizedScores { get; }
    public double CulturalNormalized { get; }
    public double GeographicNormalized { get; }
    public double TimeNormalized { get; }
    public double HistoryNormalized { get; }
    public double CompositeScore { get; }

    public NormalizedEventScores(Event @event, ComponentScores normalizedScores)
        : this(@event, normalizedScores, 
               normalizedScores.CulturalComponent, normalizedScores.ConvenienceComponent, 
               normalizedScores.SocialComponent, normalizedScores.NoveltyComponent)
    {
    }

    public NormalizedEventScores(Event @event, ComponentScores normalizedScores, double culturalNormalized, double geographicNormalized, double timeNormalized, double historyNormalized)
    {
        Event = @event ?? throw new ArgumentNullException(nameof(@event));
        NormalizedScores = normalizedScores ?? throw new ArgumentNullException(nameof(normalizedScores));
        CulturalNormalized = Math.Max(0.0, Math.Min(1.0, culturalNormalized));
        GeographicNormalized = Math.Max(0.0, Math.Min(1.0, geographicNormalized));
        TimeNormalized = Math.Max(0.0, Math.Min(1.0, timeNormalized));
        HistoryNormalized = Math.Max(0.0, Math.Min(1.0, historyNormalized));
        CompositeScore = (CulturalNormalized + GeographicNormalized + TimeNormalized + HistoryNormalized) / 4.0;
    }

    public NormalizedEventScores(double culturalNormalized, double geographicNormalized, double timeNormalized, double historyNormalized)
        : this(CreateDefaultEvent(), new ComponentScores(culturalNormalized, geographicNormalized, timeNormalized, historyNormalized), 
               culturalNormalized, geographicNormalized, timeNormalized, historyNormalized)
    {
    }

    private static Event CreateDefaultEvent()
    {
        return Event.Create(
            EventTitle.Create("Default Event for Scoring").Value,
            EventDescription.Create("Default event used for score normalization").Value,
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(1),
            Guid.NewGuid(),
            1
        ).Value;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Event;
        yield return NormalizedScores;
        yield return CulturalNormalized;
        yield return GeographicNormalized;
        yield return TimeNormalized;
        yield return HistoryNormalized;
        yield return CompositeScore;
    }
}

/// <summary>
/// Normalized event recommendation with score breakdown
/// </summary>
public class NormalizedEventRecommendation : ValueObject
{
    public Event Event { get; }
    public EventRecommendation Recommendation { get; }
    public NormalizedEventScores NormalizedScores { get; }
    public ComponentScores RawScores { get; }
    public double CompositeNormalizedScore { get; }

    public NormalizedEventRecommendation(EventRecommendation recommendation, NormalizedEventScores normalizedScores, ComponentScores rawScores)
    {
        Recommendation = recommendation ?? throw new ArgumentNullException(nameof(recommendation));
        Event = recommendation.Event;
        NormalizedScores = normalizedScores ?? throw new ArgumentNullException(nameof(normalizedScores));
        RawScores = rawScores ?? throw new ArgumentNullException(nameof(rawScores));
        CompositeNormalizedScore = normalizedScores.CompositeScore;
    }

    public NormalizedEventRecommendation(Event @event, NormalizedEventScores normalizedScores, double compositeNormalizedScore)
        : this(new EventRecommendation(@event, new RecommendationScore(compositeNormalizedScore), "Normalized recommendation"),
               normalizedScores, new ComponentScores(compositeNormalizedScore, compositeNormalizedScore, compositeNormalizedScore, compositeNormalizedScore))
    {
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Event;
        yield return Recommendation;
        yield return NormalizedScores;
        yield return RawScores;
        yield return CompositeNormalizedScore;
    }
}

/// <summary>
/// Tie-breaking rules for equal scores
/// </summary>
public class TieBreakingRules : ValueObject
{
    public TieBreakingCriteria PrimaryTiebreaker { get; }
    public TieBreakingCriteria SecondaryTiebreaker { get; }
    public TieBreakingCriteria TertiaryTiebreaker { get; }
    public TieBreakingCriteria QuaternaryTiebreaker { get; }

    public TieBreakingRules(
        TieBreakingCriteria primaryTiebreaker,
        TieBreakingCriteria secondaryTiebreaker,
        TieBreakingCriteria tertiaryTiebreaker,
        TieBreakingCriteria quaternaryTiebreaker)
    {
        PrimaryTiebreaker = primaryTiebreaker;
        SecondaryTiebreaker = secondaryTiebreaker;
        TertiaryTiebreaker = tertiaryTiebreaker;
        QuaternaryTiebreaker = quaternaryTiebreaker;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return PrimaryTiebreaker;
        yield return SecondaryTiebreaker;
        yield return TertiaryTiebreaker;
        yield return QuaternaryTiebreaker;
    }
}

// Additional enums for Event Recommendation Engine
public enum EdgeCaseType { ExtremeValues, MissingData, InvalidData }
public enum ConflictResolution { Accepted, Rejected, Modified, Deferred }
public enum TieBreakerCriterion { CulturalRelevance, TimeProximity, EventCapacity, UserHistory }

/// <summary>
/// Tie-breaking criteria options
/// </summary>
public enum TieBreakingCriteria
{
    EventPriority = 0,
    EventDate = 1,
    Proximity = 2,
    Capacity = 3,
    Popularity = 4,
    CreationDate = 5,
    Random = 6
}