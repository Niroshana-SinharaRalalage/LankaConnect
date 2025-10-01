using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Services;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Events.ValueObjects.Recommendations;
using LankaConnect.Domain.Tests.Events.TestHelpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace LankaConnect.Domain.Tests.Events.Services;

/// <summary>
/// TDD RED Phase: Event Recommendation Engine Tests (27 Tests)
/// Testing AI-powered cultural intelligence recommendation algorithms
/// Following London School TDD with sophisticated mock-driven development for complex recommendation scenarios
/// 
/// TARGET: Transform RED phase failing tests into GREEN phase passing implementation
/// FOCUS: Cultural intelligence, geographic clustering, user preference learning, multi-criteria scoring
/// </summary>
public class EventRecommendationEngineTests
{
    private readonly Mock<ICulturalCalendar> _mockCulturalCalendar;
    private readonly Mock<IUserPreferences> _mockUserPreferences;
    private readonly Mock<IGeographicProximityService> _mockGeographicService;
    private readonly IEventRecommendationEngine _recommendationEngine;

    // Test data
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly TestUser _testUser = new() { Id = Guid.NewGuid(), CulturalBackground = "Sri Lankan", Location = "Fremont, CA", Age = 35 };

    public EventRecommendationEngineTests()
    {
        _mockCulturalCalendar = new Mock<ICulturalCalendar>();
        _mockUserPreferences = new Mock<IUserPreferences>();
        _mockGeographicService = new Mock<IGeographicProximityService>();
        
        // This will fail until EventRecommendationEngine is implemented
        _recommendationEngine = new EventRecommendationEngine(
            _mockCulturalCalendar.Object,
            _mockUserPreferences.Object,
            _mockGeographicService.Object
        );
    }

    #region Core Recommendation Tests (2 tests)

    [Fact]
    public async Task GetRecommendations_ValidUserAndEvents_ShouldReturnPersonalizedRecommendations()
    {
        // Arrange
        var events = new[]
        {
            EventTestHelpers.CreateEventWithCategory("Buddhist Meditation", "Religious"),
            EventTestHelpers.CreateEventWithCategory("Tech Meetup", "Professional"),
            EventTestHelpers.CreateEventWithCategory("Cultural Dance", "Cultural")
        };

        _mockUserPreferences
            .Setup(x => x.GetCulturalPreferences(_testUserId))
            .Returns(new CulturalPreferences(new[] { "Religious", "Cultural" }, new[] { "Professional" }));

        // Act & Assert - Will fail until implementation exists
        var result = await _recommendationEngine.GetRecommendations(_testUserId, events);
        
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.First().Score.CulturalScore.Should().BeGreaterThan(0.7);
    }

    [Fact]
    public async Task GetRecommendationsForDate_WithCulturalCalendar_ShouldOptimizeForSpecificDate()
    {
        // Arrange
        var vesak = DateTime.UtcNow.AddDays(30);
        var events = new[]
        {
            EventTestHelpers.CreateEventWithTime("Vesak Celebration", 18, 0),
            EventTestHelpers.CreateEventWithTime("Business Conference", 9, 0)
        };

        _mockCulturalCalendar
            .Setup(x => x.GetSignificantDates(vesak.Year))
            .Returns(new[] { new SignificantDate("Vesak", vesak, SignificanceLevel.Critical) });

        // Act & Assert - Will fail until implementation exists
        var result = await _recommendationEngine.GetRecommendationsForDate(_testUserId, events, vesak);
        
        result.Should().NotBeNull();
        result.First().Event.Title.Value.Should().Contain("Vesak");
        result.First().Score.CompositeScore.Should().BeGreaterThan(0.8);
    }

    #endregion

    #region Cultural Intelligence Tests (6 tests)

    [Fact]
    public async Task GetCulturallyFilteredRecommendations_HighCulturalSensitivity_ShouldFilterInappropriateEvents()
    {
        // Arrange
        var events = new[]
        {
            EventTestHelpers.CreateEventWithTime("Poyaday Ceremony", 6, 0, DayOfWeek.Sunday), // High cultural appropriateness
            EventTestHelpers.CreateEventWithTime("Alcohol Tasting", 19, 0, DayOfWeek.Sunday) // Low cultural appropriateness
        };

        _mockUserPreferences
            .Setup(x => x.GetCulturalSensitivity(_testUserId))
            .Returns(CulturalSensitivityLevel.VeryHigh);

        _mockCulturalCalendar
            .Setup(x => x.IsPoyaday(It.IsAny<DateTime>()))
            .Returns(true);

        _mockCulturalCalendar
            .Setup(x => x.GetEventAppropriateness(It.IsAny<Event>(), It.IsAny<DateTime>()))
            .Returns((Event e, DateTime d) => e.Title.Value.Contains("Poyaday") ? 
                new CulturalAppropriateness(0.95) : new CulturalAppropriateness(0.15));

        // Act & Assert - Will fail until implementation exists
        var result = await _recommendationEngine.GetCulturallyFilteredRecommendations(_testUserId, events);
        
        result.Should().HaveCount(1);
        result.First().Event.Title.Value.Should().Contain("Poyaday");
        result.First().Score.CulturalScore.Should().BeGreaterThan(0.9);
    }

    [Fact]
    public async Task GetDiasporaOptimizedRecommendations_SriLankanDiaspora_ShouldPrioritizeCommunityEvents()
    {
        // Arrange
        var events = new[]
        {
            EventTestHelpers.CreateEventWithLocation("Sri Lankan New Year", "Fremont Hindu Temple"),
            EventTestHelpers.CreateEventWithLocation("General Meetup", "Random Venue")
        };

        _mockGeographicService
            .Setup(x => x.IsDiasporaLocation("Fremont Hindu Temple"))
            .Returns(true);

        _mockGeographicService
            .Setup(x => x.GetCommunityDensity("Fremont Hindu Temple"))
            .Returns(0.85); // High Sri Lankan community density

        // Act & Assert - Will fail until implementation exists
        var result = await _recommendationEngine.GetDiasporaOptimizedRecommendations(_testUserId, events);
        
        result.Should().NotBeNull();
        result.First().Event.Title.Value.Should().Contain("New Year");
        result.First().Score.GeographicScore.Should().BeGreaterThan(0.8);
    }

    [Fact]
    public async Task GetFestivalOptimizedRecommendations_VesakSeason_ShouldAlignWithFestivalTiming()
    {
        // Arrange
        var vesak = DateTime.UtcNow.AddDays(14);
        var events = new[]
        {
            EventTestHelpers.CreateEventWithTime("Vesak Lantern Making", 10, 0),
            EventTestHelpers.CreateEventWithTime("Vesak Day Celebration", 18, 0)
        };

        var vesakPeriod = new FestivalPeriod(vesak.AddDays(-7), vesak.AddDays(1), "Vesak");
        
        _mockCulturalCalendar
            .Setup(x => x.GetFestivalPeriod("Vesak", vesak.Year))
            .Returns(vesakPeriod);

        _mockCulturalCalendar
            .Setup(x => x.IsOptimalFestivalTiming(It.IsAny<Event>(), vesakPeriod))
            .Returns(true);

        // Act & Assert - Will fail until implementation exists
        var result = await _recommendationEngine.GetFestivalOptimizedRecommendations(_testUserId, events, "Vesak");
        
        result.Should().HaveCount(2);
        result.All(r => r.Score.TimingScore > 0.8).Should().BeTrue();
    }

    [Fact]
    public async Task GetCategorizedRecommendations_MixedEventNatures_ShouldClassifyCorrectly()
    {
        // Arrange
        var events = new[]
        {
            EventTestHelpers.CreateEventWithCategory("Temple Visit", "Religious"),
            EventTestHelpers.CreateEventWithCategory("Cultural Dance", "Cultural"),
            EventTestHelpers.CreateEventWithCategory("Tech Conference", "Secular")
        };

        _mockCulturalCalendar
            .Setup(x => x.ClassifyEventNature(It.IsAny<Event>()))
            .Returns((Event e) => e.Title.Value.Contains("Temple") ? EventNature.Religious :
                                  e.Title.Value.Contains("Cultural") ? EventNature.Cultural :
                                  EventNature.Secular);

        // Act & Assert - Will fail until implementation exists
        var result = await _recommendationEngine.GetCategorizedRecommendations(_testUserId, events);
        
        result.Should().HaveCount(3);
        result.Should().Contain(r => r.Score.CategoryScore > 0.9); // Religious event should score highest
    }

    [Fact]
    public async Task GetCalendarValidatedRecommendations_ConflictingDates_ShouldValidateAppropriately()
    {
        // Arrange
        var poyadayDate = DateTime.UtcNow.AddDays(7);
        var events = new[]
        {
            EventTestHelpers.CreateEventWithTime("Meditation Retreat", 6, 0), // Appropriate for Poyaday
            EventTestHelpers.CreateEventWithTime("Party Event", 20, 0) // Inappropriate for Poyaday
        };

        _mockCulturalCalendar
            .Setup(x => x.ValidateEventAgainstCalendar(It.IsAny<Event>()))
            .Returns((Event e) => new CalendarValidationResult(
                !e.Title.Value.Contains("Party"),
                e.Title.Value.Contains("Party") ? "Inappropriate for religious observance" : "Appropriate timing",
                e.Title.Value.Contains("Party") ? new[] { "Consider rescheduling" } : Array.Empty<string>()
            ));

        // Act & Assert - Will fail until implementation exists
        var result = await _recommendationEngine.GetCalendarValidatedRecommendations(_testUserId, events);
        
        result.Should().HaveCount(1);
        result.First().Event.Title.Value.Should().Contain("Meditation");
    }

    [Fact]
    public async Task CalculateCulturalScore_BuddhistEventForBuddhistUser_ShouldReturnHighScore()
    {
        // Arrange
        var buddhistEvent = EventTestHelpers.CreateEventWithCategory("Dhamma Discussion", "Religious");
        
        _mockUserPreferences
            .Setup(x => x.GetCulturalBackground(_testUserId))
            .Returns("Sinhala Buddhist");

        _mockCulturalCalendar
            .Setup(x => x.CalculateAppropriateness(buddhistEvent, "Sinhala Buddhist"))
            .Returns(new CulturalAppropriateness(0.95));

        // Act & Assert - Will fail until implementation exists
        var result = await _recommendationEngine.CalculateCulturalScore(_testUserId, buddhistEvent);
        
        result.Value.Should().BeGreaterThan(0.9);
        result.Level.Should().Be(CulturalAppropriateness.HighlyAppropriate);
    }

    #endregion

    #region Geographic Algorithm Tests (6 tests)

    [Fact]
    public async Task GetClusterOptimizedRecommendations_HighDensityArea_ShouldPrioritizeClusterEvents()
    {
        // Arrange
        var events = new[]
        {
            EventTestHelpers.CreateEventWithLocation("Community Gathering", "Fremont"),
            EventTestHelpers.CreateEventWithLocation("Isolated Event", "Remote Location")
        };

        var clusters = new[]
        {
            new CommunityCluster("Fremont", 50, 0.85, new[] { "Sri Lankan", "Tamil" }),
            new CommunityCluster("Remote Location", 5, 0.15, new[] { "General" })
        };

        _mockGeographicService
            .Setup(x => x.AnalyzeCommunityCluster(_testUser.Location, events))
            .Returns(clusters);

        // Act & Assert - Will fail until implementation exists
        var result = await _recommendationEngine.GetClusterOptimizedRecommendations(_testUserId, events);
        
        result.Should().NotBeNull();
        result.First().Event.Title.Value.Should().Contain("Community");
        result.First().Score.GeographicScore.Should().BeGreaterThan(0.8);
    }

    [Fact]
    public async Task GetDistanceFilteredRecommendations_WithTravelPreferences_ShouldFilterByDistance()
    {
        // Arrange
        var events = new[]
        {
            EventTestHelpers.CreateEventWithCoordinates("Nearby Event", new Coordinates(37.5483, -121.9886)),
            EventTestHelpers.CreateEventWithCoordinates("Far Event", new Coordinates(34.0522, -118.2437)) // LA
        };

        _mockUserPreferences
            .Setup(x => x.GetMaxTravelDistance(_testUserId))
            .Returns(new Distance(50, DistanceUnit.Miles));

        _mockGeographicService
            .Setup(x => x.CalculateDistance(It.IsAny<Coordinates>(), It.IsAny<Coordinates>()))
            .Returns((Coordinates from, Coordinates to) =>
                Math.Abs(from.Latitude - to.Latitude) > 1 ? new Distance(400, DistanceUnit.Miles) : new Distance(25, DistanceUnit.Miles));

        // Act & Assert - Will fail until implementation exists
        var result = await _recommendationEngine.GetDistanceFilteredRecommendations(_testUserId, events);
        
        result.Should().HaveCount(1);
        result.First().Event.Title.Value.Should().Contain("Nearby");
    }

    [Fact]
    public async Task GetRegionalOptimizedRecommendations_BayAreaPreferences_ShouldMatchRegion()
    {
        // Arrange
        var events = new[]
        {
            EventTestHelpers.CreateEventWithLocation("Bay Area Cultural Night", "San Jose"),
            EventTestHelpers.CreateEventWithLocation("LA Cultural Event", "Los Angeles")
        };

        var bayAreaPreferences = new RegionalPreferences(
            PreferredRegions: new[] { "Bay Area", "Silicon Valley" },
            RegionalCulturalNuances: new Dictionary<string, double> { { "Tech-savvy", 0.9 }, { "Diverse", 0.8 } }
        );

        _mockGeographicService
            .Setup(x => x.GetRegionalPreferences("San Jose"))
            .Returns(bayAreaPreferences);

        _mockGeographicService
            .Setup(x => x.CalculateRegionalMatch(It.IsAny<Event>(), bayAreaPreferences))
            .Returns((Event e, RegionalPreferences p) => 
                new RegionalMatchScore(e.Title.Value.Contains("Bay Area") ? 0.9 : 0.3));

        // Act & Assert - Will fail until implementation exists
        var result = await _recommendationEngine.GetRegionalOptimizedRecommendations(_testUserId, events);
        
        result.Should().NotBeNull();
        result.First().Score.RegionalScore.Should().BeGreaterThan(0.8);
    }

    [Fact]
    public async Task GetAccessibilityOptimizedRecommendations_TransportationOptions_ShouldConsiderAccessibility()
    {
        // Arrange
        var events = new[]
        {
            EventTestHelpers.CreateEventWithTransportation("BART Accessible Event", new[] { "BART", "Public Transit" }),
            EventTestHelpers.CreateEventWithTransportation("Car Only Event", new[] { "Parking" })
        };

        var transportPrefs = new TransportationPreferences(
            PreferredModes: new[] { "BART", "Public Transit" },
            AvoidedModes: new[] { "Driving" },
            AccessibilityRequirements: new[] { "Wheelchair accessible" }
        );

        _mockUserPreferences
            .Setup(x => x.GetTransportationPreferences(_testUserId))
            .Returns(transportPrefs);

        _mockGeographicService
            .Setup(x => x.CalculateTransportationAccessibility(It.IsAny<Event>(), transportPrefs))
            .Returns((Event e, TransportationPreferences p) =>
                new AccessibilityScore(e.Title.Value.Contains("BART") ? 0.9 : 0.3));

        // Act & Assert - Will fail until implementation exists
        var result = await _recommendationEngine.GetAccessibilityOptimizedRecommendations(_testUserId, events);
        
        result.Should().NotBeNull();
        result.First().Event.Title.Value.Should().Contain("BART");
        result.First().Score.AccessibilityScore.Should().BeGreaterThan(0.8);
    }

    [Fact]
    public async Task GetProximityOptimizedRecommendations_MultiLocationEvent_ShouldHandleComplexProximity()
    {
        // Arrange
        var events = new[]
        {
            EventTestHelpers.CreateMultiLocationEvent("Multi-Venue Festival", new[] { "Fremont", "Newark", "Union City" }),
            EventTestHelpers.CreateEventWithLocation("Single Venue", "San Francisco")
        };

        var multiLocationProximity = new MultiLocationProximity(5.0, 12.5, 20.0, 3);

        _mockGeographicService
            .Setup(x => x.CalculateMultiLocationProximity(_testUser.Location, It.IsAny<string[]>()))
            .Returns(multiLocationProximity);

        _mockGeographicService
            .Setup(x => x.CalculateProximityScore(multiLocationProximity))
            .Returns(new ProximityScore(0.85));

        // Act & Assert - Will fail until implementation exists
        var result = await _recommendationEngine.GetProximityOptimizedRecommendations(_testUserId, events);
        
        result.Should().NotBeNull();
        result.First().Score.ProximityScore.Should().BeGreaterThan(0.8);
    }

    [Fact]
    public async Task GetLocationEdgeCaseRecommendations_BorderLocation_ShouldHandleEdgeCases()
    {
        // Arrange
        var events = new[]
        {
            EventTestHelpers.CreateEventWithMissingData("Event Missing Location"),
            EventTestHelpers.CreateEventWithInvalidData("Event Invalid Data")
        };

        _mockGeographicService
            .Setup(x => x.HandleLocationEdgeCase(_testUser.Location, It.IsAny<Event>()))
            .Returns(new LocationHandlingResult(true, "Handled successfully", 0.7));

        _mockGeographicService
            .Setup(x => x.IsBorderLocation(_testUser.Location))
            .Returns(true);

        // Act & Assert - Will fail until implementation exists
        var result = await _recommendationEngine.GetLocationEdgeCaseRecommendations(_testUserId, events);
        
        result.Should().NotBeNull();
        result.Should().HaveCount(2); // Should handle both edge cases
    }

    #endregion

    #region User Preference Analysis Tests (7 tests)

    [Fact]
    public async Task GetHistoryBasedRecommendations_AttendancePatterns_ShouldLearnFromHistory()
    {
        // Arrange
        var events = new[]
        {
            EventTestHelpers.CreateEventWithCategory("Religious Gathering", "Religious"),
            EventTestHelpers.CreateEventWithCategory("Tech Talk", "Professional"),
            EventTestHelpers.CreateEventWithCategory("Cultural Show", "Cultural")
        };

        var attendanceHistory = new AttendanceHistory(new[]
        {
            new AttendedEvent(Guid.NewGuid(), "Previous Religious Event", EventNature.Religious, 0.9, DateTime.UtcNow.AddDays(-30)),
            new AttendedEvent(Guid.NewGuid(), "Previous Cultural Event", EventNature.Cultural, 0.8, DateTime.UtcNow.AddDays(-60))
        });

        var patterns = new PreferencePatterns(
            new[] { "Religious", "Cultural" },
            new[] { "Professional" },
            0.85,
            0.9
        );

        _mockUserPreferences
            .Setup(x => x.GetAttendanceHistory(_testUserId))
            .Returns(attendanceHistory);

        _mockUserPreferences
            .Setup(x => x.AnalyzePreferencePatterns(attendanceHistory))
            .Returns(patterns);

        // Act & Assert - Will fail until implementation exists
        var result = await _recommendationEngine.GetHistoryBasedRecommendations(_testUserId, events);
        
        result.Should().NotBeNull();
        result.First().Score.HistoryScore.Should().BeGreaterThan(0.8);
        result.First().Event.Title.Value.Should().Contain("Religious");
    }

    [Fact]
    public async Task GetAdaptiveRecommendations_MachineLearning_ShouldAdaptToChangingPreferences()
    {
        // Arrange
        var events = new[]
        {
            EventTestHelpers.CreateEventWithCategory("Evolved Preference Event", "New Category"),
            EventTestHelpers.CreateEventWithCategory("Traditional Event", "Traditional Category")
        };

        var learnedPreferences = new CulturalCategoryPreferences(
            PrimaryCategories: new[] { "New Category" },
            SecondaryCategories: new[] { "Traditional Category" },
            LearningConfidence: 0.85
        );

        _mockUserPreferences
            .Setup(x => x.GetLearnedCulturalPreferences(_testUserId))
            .Returns(learnedPreferences);

        // Act & Assert - Will fail until implementation exists
        var result = await _recommendationEngine.GetAdaptiveRecommendations(_testUserId, events);
        
        result.Should().NotBeNull();
        result.First().Event.Title.Value.Should().Contain("Evolved");
    }

    [Fact]
    public async Task GetTimeOptimizedRecommendations_SchedulePreferences_ShouldMatchOptimalTimes()
    {
        // Arrange
        var events = new[]
        {
            EventTestHelpers.CreateEventWithTime("Morning Event", 9, 0, DayOfWeek.Saturday),
            EventTestHelpers.CreateEventWithTime("Evening Event", 19, 0, DayOfWeek.Sunday)
        };

        var timeSlotPrefs = new TimeSlotPreferences(new[]
        {
            new TimeSlot(DayOfWeek.Saturday, TimeSpan.FromHours(9), TimeSpan.FromHours(12), 0.9),
            new TimeSlot(DayOfWeek.Sunday, TimeSpan.FromHours(18), TimeSpan.FromHours(22), 0.7)
        });

        _mockUserPreferences
            .Setup(x => x.ExtractTimeSlotPreferences(_testUserId))
            .Returns(timeSlotPrefs);

        _mockUserPreferences
            .Setup(x => x.CalculateTimeCompatibility(It.IsAny<Event>(), timeSlotPrefs))
            .Returns((Event e, TimeSlotPreferences p) =>
                new TimeCompatibilityScore(e.Title.Value.Contains("Morning") ? 0.9 : 0.7));

        // Act & Assert - Will fail until implementation exists
        var result = await _recommendationEngine.GetTimeOptimizedRecommendations(_testUserId, events);
        
        result.Should().NotBeNull();
        result.First().Score.TimeScore.Should().BeGreaterThan(0.8);
    }

    [Fact]
    public async Task GetFamilyOptimizedRecommendations_FamilyProfile_ShouldConsiderFamilyNeeds()
    {
        // Arrange
        var events = new[]
        {
            EventTestHelpers.CreateFamilyEvent("Family Festival"),
            EventTestHelpers.CreateAdultEvent("Adult Only Event")
        };

        var familyProfile = new FamilyProfile(
            hasChildren: true,
            childrenAges: new[] { 8, 12 },
            familyEventPreference: 0.9,
            adultOnlyEventPreference: 0.2
        );

        _mockUserPreferences
            .Setup(x => x.GetFamilyProfile(_testUserId))
            .Returns(familyProfile);

        _mockUserPreferences
            .Setup(x => x.CalculateFamilyCompatibility(It.IsAny<Event>(), familyProfile))
            .Returns((Event e, FamilyProfile f) =>
                new FamilyCompatibilityScore(e.Title.Value.Contains("Family") ? 0.9 : 0.2));

        // Act & Assert - Will fail until implementation exists
        var result = await _recommendationEngine.GetFamilyOptimizedRecommendations(_testUserId, events);
        
        result.Should().NotBeNull();
        result.First().Event.Title.Value.Should().Contain("Family");
        result.First().Score.FamilyScore.Should().BeGreaterThan(0.8);
    }

    [Fact]
    public async Task GetAgeOptimizedRecommendations_AgeGroup_ShouldMatchAgePreferences()
    {
        // Arrange
        var events = new[]
        {
            EventTestHelpers.CreateChildrenEvent("Kids Workshop"),
            EventTestHelpers.CreateAllAgesEvent("Community Event")
        };

        var agePrefs = new AgeGroupPreferences(
            OptimalAgeRange: new[] { 30, 40 },
            PreferredEventTypes: new[] { "Professional", "Cultural" },
            AvoidedEventTypes: new[] { "Children-only" }
        );

        _mockUserPreferences
            .Setup(x => x.GetAgeGroupPreferences(_testUser.Age))
            .Returns(agePrefs);

        _mockUserPreferences
            .Setup(x => x.CalculateAgeCompatibility(It.IsAny<Event>(), agePrefs))
            .Returns((Event e, AgeGroupPreferences a) =>
                new AgeCompatibilityScore(e.Title.Value.Contains("Community") ? 0.8 : 0.3));

        // Act & Assert - Will fail until implementation exists
        var result = await _recommendationEngine.GetAgeOptimizedRecommendations(_testUserId, events);
        
        result.Should().NotBeNull();
        result.First().Event.Title.Value.Should().Contain("Community");
        result.First().Score.CategoryScore.Should().BeGreaterThan(0.7);
    }

    [Fact]
    public async Task GetLanguageOptimizedRecommendations_MultiLanguage_ShouldMatchLanguagePrefs()
    {
        // Arrange
        var events = new[]
        {
            EventTestHelpers.CreateEventWithLanguage("Sinhala Event", new[] { "Sinhala" }),
            EventTestHelpers.CreateEventWithLanguage("Tamil Event", new[] { "Tamil" }),
            EventTestHelpers.CreateEventWithLanguage("English Event", new[] { "English" })
        };

        var langPrefs = new LanguagePreferences(
            PrimaryLanguages: new[] { "Sinhala", "English" },
            SecondaryLanguages: new[] { "Tamil" },
            MultilingualPreference: 0.8
        );

        _mockUserPreferences
            .Setup(x => x.GetLanguagePreferences(_testUserId))
            .Returns(langPrefs);

        _mockUserPreferences
            .Setup(x => x.CalculateLanguageCompatibility(It.IsAny<Event>(), langPrefs))
            .Returns((Event e, LanguagePreferences l) =>
                new LanguageCompatibilityScore(e.Title.Value.Contains("Sinhala") ? 0.9 : 
                                              e.Title.Value.Contains("English") ? 0.8 : 0.6));

        // Act & Assert - Will fail until implementation exists
        var result = await _recommendationEngine.GetLanguageOptimizedRecommendations(_testUserId, events);
        
        result.Should().NotBeNull();
        result.First().Score.LanguageScore.Should().BeGreaterThan(0.8);
    }

    [Fact]
    public async Task GetInvolvementOptimizedRecommendations_CommunityEngagement_ShouldMatchInvolvementLevel()
    {
        // Arrange
        var events = new[]
        {
            EventTestHelpers.CreateVolunteerEvent("Community Service"),
            EventTestHelpers.CreateLeadershipEvent("Board Meeting"),
            EventTestHelpers.CreateCasualEvent("Social Gathering"),
            EventTestHelpers.CreateMembershipEvent("Members Meeting")
        };

        var involvementProfile = new CommunityInvolvementProfile(
            InvolvementLevel.High,
            volunteerHours: 20,
            leadershipRoles: 2,
            membershipCount: 3,
            CommitmentLevel.Medium
        );

        _mockUserPreferences
            .Setup(x => x.GetCommunityInvolvementProfile(_testUserId))
            .Returns(involvementProfile);

        _mockUserPreferences
            .Setup(x => x.CalculateInvolvementCompatibility(It.IsAny<Event>(), involvementProfile))
            .Returns((Event e, CommunityInvolvementProfile i) =>
                new InvolvementCompatibilityScore(e.Title.Value.Contains("Leadership") ? 0.9 : 0.7));

        // Act & Assert - Will fail until implementation exists
        var result = await _recommendationEngine.GetInvolvementOptimizedRecommendations(_testUserId, events);
        
        result.Should().NotBeNull();
        result.First().Score.InvolvementScore.Should().BeGreaterThan(0.8);
    }

    #endregion

    #region Recommendation Scoring Tests (6 tests)

    [Fact]
    public async Task GetScoredRecommendations_MultiCriteria_ShouldUseComprehensiveScoring()
    {
        // Arrange
        var events = new[]
        {
            EventTestHelpers.CreateEventWithScore("High Score Event", allScoresMax: true),
            EventTestHelpers.CreateEventWithScore("Low Score Event", allScoresMin: true)
        };

        var scoringWeights = new ScoringWeights(
            CulturalWeight: 0.3,
            GeographicWeight: 0.2,
            HistoryWeight: 0.2,
            TimeWeight: 0.1,
            LanguageWeight: 0.1,
            FamilyWeight: 0.1
        );

        _mockUserPreferences
            .Setup(x => x.GetScoringWeights(_testUserId))
            .Returns(scoringWeights);

        // Act & Assert - Will fail until implementation exists
        var result = await _recommendationEngine.GetScoredRecommendations(_testUserId, events);
        
        result.Should().HaveCount(2);
        result.First().Score.CompositeScore.Should().BeGreaterThan(result.Last().Score.CompositeScore);
    }

    [Fact]
    public async Task CalculatePersonalizedScore_UserWeights_ShouldApplyPersonalization()
    {
        // Arrange
        var testEvent = EventTestHelpers.CreateTestEvent("Test Event");
        var baseScore = new BaseEventScore(0.8, 0.7, 0.9, 0.6, 0.8);

        var personalizedWeights = new PersonalizedWeights(
            CulturalImportance: 0.9,  // User highly values cultural fit
            GeographicImportance: 0.5,
            HistoryImportance: 0.7,
            TimeImportance: 0.6,
            PersonalizationConfidence: 0.85
        );

        _mockUserPreferences
            .Setup(x => x.CalculatePersonalizedWeights(_testUserId))
            .Returns(personalizedWeights);

        var expectedPersonalizedScore = new PersonalizedEventScore(0.82, baseScore, personalizedWeights);

        _mockUserPreferences
            .Setup(x => x.ApplyPersonalizedWeighting(baseScore, personalizedWeights))
            .Returns(expectedPersonalizedScore);

        // Act & Assert - Will fail until implementation exists
        var result = await _recommendationEngine.CalculatePersonalizedScore(_testUserId, testEvent, baseScore);
        
        result.Should().NotBeNull();
        result.FinalScore.Should().BeGreaterThan(baseScore.CompositeScore);
        result.PersonalizationApplied.Should().BeTrue();
    }

    [Fact]
    public async Task GetConflictResolvedRecommendations_SchedulingConflicts_ShouldResolveIntelligently()
    {
        // Arrange
        var conflictingEvents = new[]
        {
            EventTestHelpers.CreateEventWithConflict("High Priority Event", ConflictType.TimeOverlap, Priority.High),
            EventTestHelpers.CreateEventWithConflict("Medium Priority Event", ConflictType.TimeOverlap, Priority.Medium),
            EventTestHelpers.CreateEventWithConflict("Cultural Conflict Event", ConflictType.CulturalInappropriateness, Priority.Low)
        };

        var conflictRules = new ConflictResolutionRules(
            PriorityWeightings: new Dictionary<Priority, double> { { Priority.High, 0.9 }, { Priority.Medium, 0.6 }, { Priority.Low, 0.3 } },
            ConflictHandlingStrategies: new Dictionary<ConflictType, string> 
            { 
                { ConflictType.TimeOverlap, "Prioritize by importance" },
                { ConflictType.CulturalInappropriateness, "Exclude" }
            }
        );

        _mockUserPreferences
            .Setup(x => x.GetConflictResolutionRules(_testUserId))
            .Returns(conflictRules);

        _mockUserPreferences
            .Setup(x => x.ResolveEventConflicts(It.IsAny<List<Event>>(), conflictRules))
            .Returns(new List<ConflictResolvedEvent>
            {
                new ConflictResolvedEvent(conflictingEvents[0], ConflictResolution.Accepted, "High priority maintained"),
                new ConflictResolvedEvent(conflictingEvents[2], ConflictResolution.Rejected, "Cultural inappropriateness")
            });

        // Act & Assert - Will fail until implementation exists
        var result = await _recommendationEngine.GetConflictResolvedRecommendations(_testUserId, conflictingEvents);
        
        result.Should().HaveCount(2);
        result.Should().Contain(r => r.Resolution == ConflictResolution.Accepted);
        result.Should().Contain(r => r.Resolution == ConflictResolution.Rejected);
    }

    [Fact]
    public async Task GetEdgeCaseHandledRecommendations_ExtremeValues_ShouldHandleRobustly()
    {
        // Arrange
        var edgeCaseEvents = new[]
        {
            EventTestHelpers.CreateEventWithExtremeValues("Extreme Event"),
            EventTestHelpers.CreateEventWithMissingData("Missing Data Event"),
            EventTestHelpers.CreateEventWithInvalidData("Invalid Data Event")
        };

        _mockUserPreferences
            .Setup(x => x.HandleScoringEdgeCases(It.IsAny<Event>()))
            .Returns((Event e) => new EdgeCaseHandlingResult(
                true,
                e.Title.Value.Contains("Extreme") ? EdgeCaseType.ExtremeValues :
                e.Title.Value.Contains("Missing") ? EdgeCaseType.MissingData :
                EdgeCaseType.InvalidData,
                0.5, // Default fallback score
                $"Handled {e.Title.Value} appropriately"
            ));

        // Act & Assert - Will fail until implementation exists
        var result = await _recommendationEngine.GetEdgeCaseHandledRecommendations(_testUserId, edgeCaseEvents);
        
        result.Should().HaveCount(3);
        result.All(r => r.Score.CompositeScore > 0).Should().BeTrue();
    }

    [Fact]
    public async Task GetNormalizedRecommendations_RawScores_ShouldNormalizeAcrossCriteria()
    {
        // Arrange
        var events = new[]
        {
            EventTestHelpers.CreateTestEvent("Event 1"),
            EventTestHelpers.CreateTestEvent("Event 2"),
            EventTestHelpers.CreateTestEvent("Event 3")
        };

        var rawScores = new List<RawEventScores>
        {
            new RawEventScores(events[0], new ComponentScores(0.9, 0.1, 0.8, 0.3, 0.7)),
            new RawEventScores(events[1], new ComponentScores(0.5, 0.9, 0.4, 0.8, 0.6)),
            new RawEventScores(events[2], new ComponentScores(0.3, 0.7, 0.9, 0.5, 0.8))
        };

        var normalizedScores = new List<NormalizedEventScores>
        {
            new NormalizedEventScores(events[0], new ComponentScores(0.85, 0.45, 0.75, 0.55, 0.70)),
            new NormalizedEventScores(events[1], new ComponentScores(0.60, 0.75, 0.50, 0.70, 0.65)),
            new NormalizedEventScores(events[2], new ComponentScores(0.45, 0.65, 0.85, 0.60, 0.75))
        };

        _mockUserPreferences
            .Setup(x => x.NormalizeScoresAcrossCriteria(rawScores))
            .Returns(normalizedScores);

        // Act & Assert - Will fail until implementation exists
        var result = await _recommendationEngine.GetNormalizedRecommendations(_testUserId, events, rawScores);
        
        result.Should().HaveCount(3);
        result.All(r => r.NormalizedScores.CompositeScore >= 0.4 && r.NormalizedScores.CompositeScore <= 0.9).Should().BeTrue();
    }

    [Fact]
    public async Task GetTieBrokenRecommendations_EqualScores_ShouldApplyTieBreaking()
    {
        // Arrange
        var tiedEvents = new[]
        {
            EventTestHelpers.CreateEventWithTiebreakingFactors("Event A", 0.8, DateTime.UtcNow.AddDays(7), "Cultural", 50),
            EventTestHelpers.CreateEventWithTiebreakingFactors("Event B", 0.8, DateTime.UtcNow.AddDays(14), "Professional", 100),
            EventTestHelpers.CreateEventWithTiebreakingFactors("Event C", 0.8, DateTime.UtcNow.AddDays(21), "Cultural", 75)
        };

        var tieBreakingRules = new TieBreakingRules(
            PrimaryTieBreaker: TieBreakerCriterion.CulturalRelevance,
            SecondaryTieBreaker: TieBreakerCriterion.EventCapacity,
            TertiaryTieBreaker: TieBreakerCriterion.TimeProximity
        );

        _mockUserPreferences
            .Setup(x => x.GetTieBreakingRules(_testUserId))
            .Returns(tieBreakingRules);

        _mockUserPreferences
            .Setup(x => x.ApplyTieBreakingLogic(It.IsAny<List<Event>>(), tieBreakingRules))
            .Returns(new List<Event> { tiedEvents[0], tiedEvents[2], tiedEvents[1] }); // Cultural events first

        // Act & Assert - Will fail until implementation exists
        var result = await _recommendationEngine.GetTieBrokenRecommendations(_testUserId, tiedEvents);
        
        result.Should().HaveCount(3);
        result.First().Event.Title.Value.Should().Contain("Event A");
        result.Last().Event.Title.Value.Should().Contain("Event B");
    }

    #endregion
}