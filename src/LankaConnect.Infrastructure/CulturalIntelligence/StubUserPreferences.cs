using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Services;
using LankaConnect.Domain.Events.ValueObjects.Recommendations;
using CulturalSensitivityLevel = LankaConnect.Domain.Events.ValueObjects.Recommendations.CulturalSensitivityLevel;

namespace LankaConnect.Infrastructure.CulturalIntelligence;

/// <summary>
/// Stub implementation of IUserPreferences for MVP
/// TODO: Replace with real user preference learning in Phase 2
/// </summary>
public class StubUserPreferences : IUserPreferences
{
    public CulturalSensitivityLevel GetCulturalSensitivity(Guid userId) => CulturalSensitivityLevel.Medium;

    public bool PrefersCulturallyAppropriateEvents(Guid userId) => true;

    public CulturalPreferences GetCulturalPreferences(Guid userId)
        => new CulturalPreferences(Array.Empty<string>(), Array.Empty<string>(), 0.7);

    public DiasporaAdaptationLevel GetDiasporaAdaptationPreference(Guid userId)
        => DiasporaAdaptationLevel.Moderate;

    public string GetCulturalBackground(Guid userId) => "Sri Lankan";

    public bool PrefersFestivalAlignment(Guid userId) => true;

    public EventNaturePreferences GetEventNaturePreferences(Guid userId)
        => new EventNaturePreferences(0.7, 0.7, 0.5);

    public AttendanceHistory GetAttendanceHistory(Guid userId)
        => new AttendanceHistory(userId, Array.Empty<AttendedEvent>(), 0.0);

    public PreferencePatterns AnalyzePreferencePatterns(AttendanceHistory history)
        => new PreferencePatterns(Array.Empty<string>(), Array.Empty<string>(), 0.0, 0.7);

    public CulturalCategoryPreferences GetLearnedCulturalPreferences(Guid userId)
        => new CulturalCategoryPreferences(new Dictionary<string, double>(), 0.5, DateTime.UtcNow, 0);

    public void UpdatePreferenceLearning(Guid userId, Event @event, UserInteraction interaction)
    {
        // Stub: No-op for MVP
    }

    public TimeSlotPreferences ExtractTimeSlotPreferences(Guid userId)
        => new TimeSlotPreferences(Array.Empty<DayOfWeek>(), Array.Empty<DayOfWeek>(), Array.Empty<TimeSlot>());

    public TimeCompatibilityScore CalculateTimeCompatibility(Event @event, TimeSlotPreferences preferences)
        => new TimeCompatibilityScore(0.7, "Stub compatibility");

    public FamilyProfile GetFamilyProfile(Guid userId)
        => new FamilyProfile(false);

    public FamilyCompatibilityScore CalculateFamilyCompatibility(Event @event, FamilyProfile profile)
        => new FamilyCompatibilityScore(0.7, "Stub family compatibility");

    public AgeGroupPreferences GetAgeGroupPreferences(int age)
        => new AgeGroupPreferences(Array.Empty<string>(), "Moderate", 0.7);

    public AgeCompatibilityScore CalculateAgeCompatibility(Event @event, AgeGroupPreferences preferences)
        => new AgeCompatibilityScore(0.7, "Stub age compatibility");

    public LanguagePreferences GetLanguagePreferences(Guid userId)
        => new LanguagePreferences(new[] { "English" }, Array.Empty<string>());

    public LanguageCompatibilityScore CalculateLanguageCompatibility(Event @event, LanguagePreferences preferences)
        => new LanguageCompatibilityScore(0.7, "Stub language compatibility");

    public CommunityInvolvementProfile GetCommunityInvolvementProfile(Guid userId)
        => new CommunityInvolvementProfile(InvolvementLevel.Casual, 0, 0, 0, CommitmentLevel.Medium);

    public InvolvementCompatibilityScore CalculateInvolvementCompatibility(Event @event, CommunityInvolvementProfile profile)
        => new InvolvementCompatibilityScore(0.7, "Stub involvement compatibility");

    public TransportationPreferences GetTransportationPreferences(Guid userId)
        => new TransportationPreferences(Array.Empty<string>(), Array.Empty<string>());

    public Distance GetMaxTravelDistance(Guid userId)
        => new Distance(50, DistanceUnit.Miles);

    public ScoringWeights GetScoringWeights(Guid userId)
        => new ScoringWeights();

    public PersonalizedWeights CalculatePersonalizedWeights(Guid userId)
        => new PersonalizedWeights();

    public PersonalizedEventScore ApplyPersonalizedWeighting(BaseEventScore baseScore, PersonalizedWeights weights)
        => new PersonalizedEventScore(0.7, new ComponentScores(0.7, 0.7, 0.7, 0.7), 0.9);

    public ConflictResolutionRules GetConflictResolutionRules(Guid userId)
        => new ConflictResolutionRules();

    public List<ConflictResolvedEvent> ResolveEventConflicts(List<Event> events, ConflictResolutionRules rules)
        => events.Select(e => new ConflictResolvedEvent(e, 0.7, "No conflicts")).ToList();

    public EdgeCaseHandlingResult HandleScoringEdgeCases(Event @event)
        => new EdgeCaseHandlingResult(true, 0.5, "Default handling");

    public List<NormalizedEventScores> NormalizeScoresAcrossCriteria(List<RawEventScores> rawScores)
        => rawScores.Select(r => new NormalizedEventScores(r.Event, r.ComponentScores)).ToList();

    public TieBreakingRules GetTieBreakingRules(Guid userId)
        => new TieBreakingRules(
            TieBreakingCriteria.EventPriority,
            TieBreakingCriteria.EventDate,
            TieBreakingCriteria.Proximity,
            TieBreakingCriteria.Popularity);

    public List<Event> ApplyTieBreakingLogic(List<Event> events, TieBreakingRules rules)
        => events;
}
