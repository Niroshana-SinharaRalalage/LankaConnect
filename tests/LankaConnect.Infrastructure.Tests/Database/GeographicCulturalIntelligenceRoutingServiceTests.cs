using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common.Database;
using LankaConnect.Infrastructure.Database.LoadBalancing;
using Microsoft.Extensions.Logging;
using Moq;
using System.ComponentModel.DataAnnotations;
using Xunit;

namespace LankaConnect.Infrastructure.Tests.Database;

/// <summary>
/// TDD RED phase comprehensive test suite for Geographic Cultural Intelligence Routing Service
/// Implements architect-recommended testing strategy for global diaspora routing:
/// - Multi-dimensional routing engine with spatial cultural intelligence
/// - Buddhist/Hindu/Islamic calendar intelligence integration  
/// - Multi-language cultural appropriateness scoring across regions
/// - <200ms global response time validation under cultural event loads
/// - Cultural routing conflict resolution with sacred event prioritization
/// </summary>
public class GeographicCulturalIntelligenceRoutingServiceTests : IDisposable
{
    private readonly Mock<ILogger<GeographicCulturalIntelligenceRoutingService>> _loggerMock;
    private readonly Mock<ISpatialCulturalIntelligenceEngine> _spatialEngineMock;
    private readonly Mock<IBuddhistHinduIslamicCalendarEngine> _calendarEngineMock;
    private readonly Mock<IMultiLanguageRoutingOptimizer> _languageOptimizerMock;
    private readonly Mock<ICulturalAppropriatenessScorer> _appropriatenessScorerMock;
    private readonly Mock<IGlobalDiasporaClusteringService> _diasporaServiceMock;
    private readonly Mock<ICulturalRoutingConflictResolver> _conflictResolverMock;
    private readonly GeographicCulturalIntelligenceRoutingService _service;

    public GeographicCulturalIntelligenceRoutingServiceTests()
    {
        _loggerMock = new Mock<ILogger<GeographicCulturalIntelligenceRoutingService>>();
        _spatialEngineMock = new Mock<ISpatialCulturalIntelligenceEngine>();
        _calendarEngineMock = new Mock<IBuddhistHinduIslamicCalendarEngine>();
        _languageOptimizerMock = new Mock<IMultiLanguageRoutingOptimizer>();
        _appropriatenessScorerMock = new Mock<ICulturalAppropriatenessScorer>();
        _diasporaServiceMock = new Mock<IGlobalDiasporaClusteringService>();
        _conflictResolverMock = new Mock<ICulturalRoutingConflictResolver>();

        _service = new GeographicCulturalIntelligenceRoutingService(
            _loggerMock.Object,
            _spatialEngineMock.Object,
            _calendarEngineMock.Object,
            _languageOptimizerMock.Object,
            _appropriatenessScorerMock.Object,
            _diasporaServiceMock.Object,
            _conflictResolverMock.Object);
    }

    #region Constructor and Service Initialization Tests

    [Fact]
    public void Constructor_WithAllValidDependencies_ShouldCreateInstance()
    {
        // Act & Assert
        _service.Should().NotBeNull();
        _service.Should().BeAssignableTo<IGeographicCulturalIntelligenceRoutingService>();
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Action act = () => new GeographicCulturalIntelligenceRoutingService(
            null!,
            _spatialEngineMock.Object,
            _calendarEngineMock.Object,
            _languageOptimizerMock.Object,
            _appropriatenessScorerMock.Object,
            _diasporaServiceMock.Object,
            _conflictResolverMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithNullSpatialEngine_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        Action act = () => new GeographicCulturalIntelligenceRoutingService(
            _loggerMock.Object,
            null!,
            _calendarEngineMock.Object,
            _languageOptimizerMock.Object,
            _appropriatenessScorerMock.Object,
            _diasporaServiceMock.Object,
            _conflictResolverMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("spatialEngine");
    }

    #endregion

    #region Core Geographic Cultural Intelligence Routing Tests

    [Fact]
    public async Task RouteWithCulturalIntelligence_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _service.RouteWithCulturalIntelligence(null!, CancellationToken.None));
    }

    [Fact]
    public async Task RouteWithCulturalIntelligence_ForBayAreaSriLankanBuddhistCommunity_ShouldReturnOptimalRouting()
    {
        // Arrange
        var request = new CulturalRoutingRequest
        {
            RequestId = Guid.NewGuid(),
            Location = new GeographicLocation
            {
                Latitude = 37.4419, // Bay Area coordinates
                Longitude = -122.1430,
                Address = "Palo Alto, CA",
                Country = "United States",
                Region = "NorthAmerica"
            },
            UserLanguages = new List<LanguageCode> { LanguageCode.Sinhala, LanguageCode.English },
            CulturalCommunityType = CulturalCommunityType.SriLankanBuddhist,
            Timestamp = DateTime.UtcNow,
            RequiredResponseTime = TimeSpan.FromMilliseconds(200),
            CulturalContextHints = new List<string> { "පොය දිනය", "වෙසක්", "පින්කම්" }
        };

        var expectedSpatialContext = new SpatialCulturalContext
        {
            PrimaryCulturalRegion = "BayAreaSriLankanBuddhist",
            CommunityDensity = 0.87m,
            SacredSiteProximity = 2500, // meters to nearest Buddhist temple
            CulturalAuthenticity = 0.94m
        };

        var expectedCalendarContext = new CalendarInfluenceContext
        {
            ActiveCalendarInfluences = new List<CalendarInfluence>
            {
                new() { Type = CalendarType.Buddhist, Intensity = 0.8m, RoutingPriority = SacredEventPriority.Level8MonthlyObservance }
            },
            OverallInfluence = 0.8m
        };

        var expectedLanguageRouting = new LanguageRoutingOptimization
        {
            PrimaryLanguage = LanguageCode.Sinhala,
            SecondaryLanguages = new[] { LanguageCode.English },
            LocalizedContentAvailability = 0.95m,
            TranslationQuality = 0.92m
        };

        var expectedAppropriatenessScore = new CulturalAppropriatenessScore
        {
            OverallScore = 0.93m,
            GeographicScore = 0.94m,
            TemporalScore = 0.91m,
            CommunityScore = 0.94m,
            CulturalConfidenceLevel = 0.93m
        };

        var expectedRoutingDecision = new CulturalRoutingDecision
        {
            DecisionId = Guid.NewGuid(),
            SelectedRegion = new CulturalRegion
            {
                RegionId = "bayarea-srilankanbud-001",
                RegionName = "Bay Area Sri Lankan Buddhist Community",
                PrimaryCulturalCommunity = CulturalCommunityType.SriLankanBuddhist,
                AverageResponseTimeMs = 145,
                CulturalAuthorityRating = 0.96m
            },
            CulturalAppropriateness = 0.93m,
            ExpectedResponseTime = TimeSpan.FromMilliseconds(145),
            RoutingConfidence = 0.95m,
            PerformanceOptimizations = new List<string> { "BuddhistTempleProximityBoost", "SinhalaContentOptimized" }
        };

        // Mock service dependencies
        _spatialEngineMock.Setup(x => x.AnalyzeSpatialCulturalContext(
                It.IsAny<CulturalRoutingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedSpatialContext);

        _calendarEngineMock.Setup(x => x.EvaluateCalendarInfluence(
                It.IsAny<DateTime>(), It.IsAny<GeographicLocation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedCalendarContext);

        _languageOptimizerMock.Setup(x => x.OptimizeLanguageRouting(
                It.IsAny<List<LanguageCode>>(), It.IsAny<GeographicLocation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedLanguageRouting);

        _appropriatenessScorerMock.Setup(x => x.CalculateCulturalAppropriateness(
                It.IsAny<SpatialCulturalContext>(), It.IsAny<CalendarInfluenceContext>(),
                It.IsAny<LanguageRoutingOptimization>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedAppropriatenessScore);

        _diasporaServiceMock.Setup(x => x.OptimizeForGlobalPerformance(
                It.IsAny<SpatialCulturalContext>(), It.IsAny<CalendarInfluenceContext>(),
                It.IsAny<LanguageRoutingOptimization>(), It.IsAny<CulturalAppropriatenessScore>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRoutingDecision);

        // Act
        var result = await _service.RouteWithCulturalIntelligence(request, CancellationToken.None);

        // Assert - Geographic Cultural Intelligence
        result.Should().NotBeNull();
        result.CulturalAppropriateness.Should().BeGreaterThan(0.9m);
        result.ExpectedResponseTime.Should().BeLessThan(TimeSpan.FromMilliseconds(200));
        result.SelectedRegion.PrimaryCulturalCommunity.Should().Be(CulturalCommunityType.SriLankanBuddhist);
        result.RoutingConfidence.Should().BeGreaterThan(0.9m);
        
        // Verify all cultural intelligence layers were invoked
        _spatialEngineMock.Verify(x => x.AnalyzeSpatialCulturalContext(
            It.IsAny<CulturalRoutingRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _calendarEngineMock.Verify(x => x.EvaluateCalendarInfluence(
            It.IsAny<DateTime>(), It.IsAny<GeographicLocation>(), It.IsAny<CancellationToken>()), Times.Once);
        _languageOptimizerMock.Verify(x => x.OptimizeLanguageRouting(
            It.IsAny<List<LanguageCode>>(), It.IsAny<GeographicLocation>(), It.IsAny<CancellationToken>()), Times.Once);
        _appropriatenessScorerMock.Verify(x => x.CalculateCulturalAppropriateness(
            It.IsAny<SpatialCulturalContext>(), It.IsAny<CalendarInfluenceContext>(),
            It.IsAny<LanguageRoutingOptimization>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(CulturalCommunityType.SriLankanBuddhist, LanguageCode.Sinhala, "BayArea", 0.94)]
    [InlineData(CulturalCommunityType.IndianHindu, LanguageCode.Hindi, "NewYork", 0.92)]
    [InlineData(CulturalCommunityType.PakistaniMuslim, LanguageCode.Urdu, "Chicago", 0.91)]
    [InlineData(CulturalCommunityType.SikhPunjabi, LanguageCode.Punjabi, "CentralValley", 0.90)]
    [InlineData(CulturalCommunityType.TamilHindu, LanguageCode.Tamil, "Toronto", 0.93)]
    public async Task RouteWithCulturalIntelligence_ForDifferentDiasporaCommunities_ShouldReturnCommunityOptimizedRouting(
        CulturalCommunityType communityType, LanguageCode primaryLanguage, string region, decimal expectedAccuracy)
    {
        // Arrange
        var request = new CulturalRoutingRequest
        {
            RequestId = Guid.NewGuid(),
            UserLanguages = new List<LanguageCode> { primaryLanguage, LanguageCode.English },
            CulturalCommunityType = communityType,
            RequiredResponseTime = TimeSpan.FromMilliseconds(200)
        };

        var mockRoutingDecision = new CulturalRoutingDecision
        {
            DecisionId = Guid.NewGuid(),
            CulturalAppropriateness = expectedAccuracy,
            ExpectedResponseTime = TimeSpan.FromMilliseconds(150),
            SelectedRegion = new CulturalRegion
            {
                RegionId = $"{region.ToLower()}-{communityType.ToString().ToLower()}-001",
                PrimaryCulturalCommunity = communityType
            }
        };

        // Mock all intermediate steps
        _spatialEngineMock.Setup(x => x.AnalyzeSpatialCulturalContext(It.IsAny<CulturalRoutingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SpatialCulturalContext());
        _calendarEngineMock.Setup(x => x.EvaluateCalendarInfluence(It.IsAny<DateTime>(), It.IsAny<GeographicLocation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CalendarInfluenceContext());
        _languageOptimizerMock.Setup(x => x.OptimizeLanguageRouting(It.IsAny<List<LanguageCode>>(), It.IsAny<GeographicLocation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LanguageRoutingOptimization());
        _appropriatenessScorerMock.Setup(x => x.CalculateCulturalAppropriateness(It.IsAny<SpatialCulturalContext>(), It.IsAny<CalendarInfluenceContext>(), It.IsAny<LanguageRoutingOptimization>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CulturalAppropriatenessScore());
        _diasporaServiceMock.Setup(x => x.OptimizeForGlobalPerformance(It.IsAny<SpatialCulturalContext>(), It.IsAny<CalendarInfluenceContext>(), It.IsAny<LanguageRoutingOptimization>(), It.IsAny<CulturalAppropriatenessScore>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockRoutingDecision);

        // Act
        var result = await _service.RouteWithCulturalIntelligence(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.SelectedRegion.PrimaryCulturalCommunity.Should().Be(communityType);
        result.CulturalAppropriateness.Should().Be(expectedAccuracy);
        result.ExpectedResponseTime.Should().BeLessThan(TimeSpan.FromMilliseconds(200));
    }

    #endregion

    #region Buddhist/Hindu/Islamic Calendar Intelligence Tests

    [Fact]
    public async Task RouteWithCulturalIntelligence_DuringVesakDayBuddhist_ShouldApplySacredEventPriorityRouting()
    {
        // Arrange - Vesak Day (May full moon day)
        var vesakDay = new DateTime(2024, 5, 23); // Vesak Day 2024
        var request = new CulturalRoutingRequest
        {
            RequestId = Guid.NewGuid(),
            CulturalCommunityType = CulturalCommunityType.SriLankanBuddhist,
            Timestamp = vesakDay,
            UserLanguages = new List<LanguageCode> { LanguageCode.Sinhala }
        };

        var sacredEventCalendarContext = new CalendarInfluenceContext
        {
            ActiveCalendarInfluences = new List<CalendarInfluence>
            {
                new()
                {
                    Type = CalendarType.Buddhist,
                    EventName = "VesakDay",
                    Intensity = 1.0m, // Maximum intensity for most sacred Buddhist day
                    RoutingPriority = SacredEventPriority.Level10Sacred,
                    TrafficMultiplier = 5.0m,
                    CulturalSignificance = "MostSacredBuddhistHoliday"
                }
            },
            OverallInfluence = 1.0m,
            RecommendedOptimizations = new List<string> { "VesakSacredEventOptimization", "BuddhistTempleProximityBoost" }
        };

        var expectedRoutingDecision = new CulturalRoutingDecision
        {
            DecisionId = Guid.NewGuid(),
            CulturalAppropriateness = 0.98m, // Highest appropriateness for sacred event
            ExpectedResponseTime = TimeSpan.FromMilliseconds(120), // Optimized for sacred event
            SacredEventOptimization = true,
            CalendarInfluence = sacredEventCalendarContext
        };

        // Mock calendar intelligence for Vesak Day
        _calendarEngineMock.Setup(x => x.EvaluateCalendarInfluence(
                vesakDay, It.IsAny<GeographicLocation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sacredEventCalendarContext);

        // Mock other services
        _spatialEngineMock.Setup(x => x.AnalyzeSpatialCulturalContext(It.IsAny<CulturalRoutingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SpatialCulturalContext());
        _languageOptimizerMock.Setup(x => x.OptimizeLanguageRouting(It.IsAny<List<LanguageCode>>(), It.IsAny<GeographicLocation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LanguageRoutingOptimization());
        _appropriatenessScorerMock.Setup(x => x.CalculateCulturalAppropriateness(It.IsAny<SpatialCulturalContext>(), It.IsAny<CalendarInfluenceContext>(), It.IsAny<LanguageRoutingOptimization>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CulturalAppropriatenessScore());
        _diasporaServiceMock.Setup(x => x.OptimizeForGlobalPerformance(It.IsAny<SpatialCulturalContext>(), It.IsAny<CalendarInfluenceContext>(), It.IsAny<LanguageRoutingOptimization>(), It.IsAny<CulturalAppropriatenessScore>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRoutingDecision);

        // Act
        var result = await _service.RouteWithCulturalIntelligence(request, CancellationToken.None);

        // Assert - Sacred Event Priority Routing
        result.Should().NotBeNull();
        result.SacredEventOptimization.Should().BeTrue();
        result.CulturalAppropriateness.Should().BeGreaterThan(0.95m);
        result.ExpectedResponseTime.Should().BeLessThan(TimeSpan.FromMilliseconds(150));
        
        // Verify calendar intelligence was consulted with Vesak date
        _calendarEngineMock.Verify(x => x.EvaluateCalendarInfluence(
            vesakDay, It.IsAny<GeographicLocation>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("2024-11-01", CalendarType.Hindu, "Diwali", SacredEventPriority.Level9MajorFestival, 4.5)]
    [InlineData("2024-04-10", CalendarType.Islamic, "EidAlFitr", SacredEventPriority.Level10Sacred, 4.0)]
    [InlineData("2024-03-13", CalendarType.Hindu, "Holi", SacredEventPriority.Level8MonthlyObservance, 2.8)]
    [InlineData("2024-04-13", CalendarType.Sikh, "Vaisakhi", SacredEventPriority.Level7RegionalFestival, 3.2)]
    public async Task RouteWithCulturalIntelligence_DuringVariousCulturalFestivals_ShouldApplyAppropriateCalendarInfluence(
        string dateStr, CalendarType calendarType, string eventName, SacredEventPriority expectedPriority, double expectedMultiplier)
    {
        // Arrange
        var festivalDate = DateTime.Parse(dateStr);
        var request = new CulturalRoutingRequest
        {
            RequestId = Guid.NewGuid(),
            Timestamp = festivalDate,
            UserLanguages = new List<LanguageCode> { LanguageCode.English }
        };

        var festivalCalendarContext = new CalendarInfluenceContext
        {
            ActiveCalendarInfluences = new List<CalendarInfluence>
            {
                new()
                {
                    Type = calendarType,
                    EventName = eventName,
                    RoutingPriority = expectedPriority,
                    TrafficMultiplier = (decimal)expectedMultiplier,
                    Intensity = 0.9m
                }
            }
        };

        // Mock services
        _calendarEngineMock.Setup(x => x.EvaluateCalendarInfluence(
                festivalDate, It.IsAny<GeographicLocation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(festivalCalendarContext);

        _spatialEngineMock.Setup(x => x.AnalyzeSpatialCulturalContext(It.IsAny<CulturalRoutingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SpatialCulturalContext());
        _languageOptimizerMock.Setup(x => x.OptimizeLanguageRouting(It.IsAny<List<LanguageCode>>(), It.IsAny<GeographicLocation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LanguageRoutingOptimization());
        _appropriatenessScorerMock.Setup(x => x.CalculateCulturalAppropriateness(It.IsAny<SpatialCulturalContext>(), It.IsAny<CalendarInfluenceContext>(), It.IsAny<LanguageRoutingOptimization>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CulturalAppropriatenessScore());
        _diasporaServiceMock.Setup(x => x.OptimizeForGlobalPerformance(It.IsAny<SpatialCulturalContext>(), It.IsAny<CalendarInfluenceContext>(), It.IsAny<LanguageRoutingOptimization>(), It.IsAny<CulturalAppropriatenessScore>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CulturalRoutingDecision { CalendarInfluence = festivalCalendarContext });

        // Act
        var result = await _service.RouteWithCulturalIntelligence(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.CalendarInfluence.Should().NotBeNull();
        result.CalendarInfluence.ActiveCalendarInfluences.Should().HaveCount(1);
        result.CalendarInfluence.ActiveCalendarInfluences.First().Type.Should().Be(calendarType);
        result.CalendarInfluence.ActiveCalendarInfluences.First().EventName.Should().Be(eventName);
        result.CalendarInfluence.ActiveCalendarInfluences.First().TrafficMultiplier.Should().Be((decimal)expectedMultiplier);
    }

    #endregion

    #region Multi-Language Cultural Appropriateness Tests

    [Theory]
    [InlineData(LanguageCode.Sinhala, CulturalCommunityType.SriLankanBuddhist, 0.95)]
    [InlineData(LanguageCode.Tamil, CulturalCommunityType.TamilHindu, 0.94)]
    [InlineData(LanguageCode.Hindi, CulturalCommunityType.IndianHindu, 0.93)]
    [InlineData(LanguageCode.Urdu, CulturalCommunityType.PakistaniMuslim, 0.92)]
    [InlineData(LanguageCode.Punjabi, CulturalCommunityType.SikhPunjabi, 0.91)]
    [InlineData(LanguageCode.Bengali, CulturalCommunityType.BengaliHindu, 0.90)]
    [InlineData(LanguageCode.Gujarati, CulturalCommunityType.IndianHindu, 0.89)]
    public async Task RouteWithCulturalIntelligence_WithSpecificLanguageCommunityPairs_ShouldOptimizeForLanguageCulturalAlignment(
        LanguageCode language, CulturalCommunityType communityType, decimal expectedAccuracy)
    {
        // Arrange
        var request = new CulturalRoutingRequest
        {
            RequestId = Guid.NewGuid(),
            UserLanguages = new List<LanguageCode> { language, LanguageCode.English },
            CulturalCommunityType = communityType
        };

        var languageOptimization = new LanguageRoutingOptimization
        {
            PrimaryLanguage = language,
            SecondaryLanguages = new[] { LanguageCode.English },
            LocalizedContentAvailability = expectedAccuracy,
            TranslationQuality = expectedAccuracy * 0.98m,
            CulturalNuancePreservation = expectedAccuracy * 0.96m
        };

        var appropriatenessScore = new CulturalAppropriatenessScore
        {
            OverallScore = expectedAccuracy,
            LanguageScore = expectedAccuracy,
            CulturalConfidenceLevel = expectedAccuracy * 0.95m
        };

        // Mock services
        _spatialEngineMock.Setup(x => x.AnalyzeSpatialCulturalContext(It.IsAny<CulturalRoutingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SpatialCulturalContext());
        _calendarEngineMock.Setup(x => x.EvaluateCalendarInfluence(It.IsAny<DateTime>(), It.IsAny<GeographicLocation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CalendarInfluenceContext());
        _languageOptimizerMock.Setup(x => x.OptimizeLanguageRouting(
                It.Is<List<LanguageCode>>(l => l.Contains(language)), It.IsAny<GeographicLocation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(languageOptimization);
        _appropriatenessScorerMock.Setup(x => x.CalculateCulturalAppropriateness(It.IsAny<SpatialCulturalContext>(), It.IsAny<CalendarInfluenceContext>(), It.IsAny<LanguageRoutingOptimization>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(appropriatenessScore);
        _diasporaServiceMock.Setup(x => x.OptimizeForGlobalPerformance(It.IsAny<SpatialCulturalContext>(), It.IsAny<CalendarInfluenceContext>(), It.IsAny<LanguageRoutingOptimization>(), It.IsAny<CulturalAppropriatenessScore>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CulturalRoutingDecision { CulturalAppropriateness = expectedAccuracy, LanguageOptimization = languageOptimization });

        // Act
        var result = await _service.RouteWithCulturalIntelligence(request, CancellationToken.None);

        // Assert - Language-Cultural Alignment
        result.Should().NotBeNull();
        result.CulturalAppropriateness.Should().Be(expectedAccuracy);
        result.LanguageOptimization.Should().NotBeNull();
        result.LanguageOptimization.PrimaryLanguage.Should().Be(language);
        result.LanguageOptimization.LocalizedContentAvailability.Should().BeGreaterOrEqualTo(expectedAccuracy);
    }

    [Fact]
    public async Task RouteWithCulturalIntelligence_WithMultiLanguageRequest_ShouldOptimizeForLanguagePriority()
    {
        // Arrange - Multi-lingual user (common in diaspora communities)
        var request = new CulturalRoutingRequest
        {
            RequestId = Guid.NewGuid(),
            UserLanguages = new List<LanguageCode> 
            { 
                LanguageCode.Sinhala,    // Primary cultural language
                LanguageCode.Tamil,      // Secondary cultural language  
                LanguageCode.English     // Common diaspora language
            },
            CulturalCommunityType = CulturalCommunityType.SriLankanBuddhist
        };

        var multiLanguageOptimization = new LanguageRoutingOptimization
        {
            PrimaryLanguage = LanguageCode.Sinhala,
            SecondaryLanguages = new[] { LanguageCode.Tamil, LanguageCode.English },
            LanguagePreferencePriority = new Dictionary<LanguageCode, decimal>
            {
                { LanguageCode.Sinhala, 1.0m },
                { LanguageCode.Tamil, 0.8m },
                { LanguageCode.English, 0.6m }
            },
            LocalizedContentAvailability = 0.92m,
            MultiLanguageCompatibility = true
        };

        // Mock services
        _spatialEngineMock.Setup(x => x.AnalyzeSpatialCulturalContext(It.IsAny<CulturalRoutingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SpatialCulturalContext());
        _calendarEngineMock.Setup(x => x.EvaluateCalendarInfluence(It.IsAny<DateTime>(), It.IsAny<GeographicLocation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CalendarInfluenceContext());
        _languageOptimizerMock.Setup(x => x.OptimizeLanguageRouting(
                It.Is<List<LanguageCode>>(l => l.Count == 3), It.IsAny<GeographicLocation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(multiLanguageOptimization);
        _appropriatenessScorerMock.Setup(x => x.CalculateCulturalAppropriateness(It.IsAny<SpatialCulturalContext>(), It.IsAny<CalendarInfluenceContext>(), It.IsAny<LanguageRoutingOptimization>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CulturalAppropriatenessScore());
        _diasporaServiceMock.Setup(x => x.OptimizeForGlobalPerformance(It.IsAny<SpatialCulturalContext>(), It.IsAny<CalendarInfluenceContext>(), It.IsAny<LanguageRoutingOptimization>(), It.IsAny<CulturalAppropriatenessScore>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CulturalRoutingDecision { LanguageOptimization = multiLanguageOptimization });

        // Act
        var result = await _service.RouteWithCulturalIntelligence(request, CancellationToken.None);

        // Assert - Multi-Language Priority Optimization
        result.Should().NotBeNull();
        result.LanguageOptimization.Should().NotBeNull();
        result.LanguageOptimization.PrimaryLanguage.Should().Be(LanguageCode.Sinhala);
        result.LanguageOptimization.SecondaryLanguages.Should().Contain(LanguageCode.Tamil);
        result.LanguageOptimization.SecondaryLanguages.Should().Contain(LanguageCode.English);
        result.LanguageOptimization.MultiLanguageCompatibility.Should().BeTrue();
    }

    #endregion

    #region Global Performance and SLA Compliance Tests

    [Theory]
    [InlineData("NorthAmerica", "BayArea", 145)]
    [InlineData("Europe", "London", 155)]
    [InlineData("AsiaPacific", "Sydney", 165)]
    [InlineData("NorthAmerica", "Toronto", 150)]
    [InlineData("Europe", "Netherlands", 160)]
    public async Task RouteWithCulturalIntelligence_ForGlobalDiasporaRegions_ShouldMaintainSub200msResponseTime(
        string continent, string region, int expectedResponseTimeMs)
    {
        // Arrange
        var request = new CulturalRoutingRequest
        {
            RequestId = Guid.NewGuid(),
            Location = new GeographicLocation
            {
                Region = continent,
                Address = region
            },
            RequiredResponseTime = TimeSpan.FromMilliseconds(200)
        };

        var performanceOptimizedDecision = new CulturalRoutingDecision
        {
            DecisionId = Guid.NewGuid(),
            ExpectedResponseTime = TimeSpan.FromMilliseconds(expectedResponseTimeMs),
            PerformanceOptimizations = new List<string> 
            { 
                $"{region}RegionalCacheOptimization",
                "GlobalCDNRouting",
                "CulturalContentPrewarm"
            },
            SlaCompliance = new SlaComplianceValidation
            {
                ResponseTimeSlaCompliant = expectedResponseTimeMs < 200,
                GlobalPerformanceRating = 0.95m
            }
        };

        // Mock services for performance optimization
        _spatialEngineMock.Setup(x => x.AnalyzeSpatialCulturalContext(It.IsAny<CulturalRoutingRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SpatialCulturalContext());
        _calendarEngineMock.Setup(x => x.EvaluateCalendarInfluence(It.IsAny<DateTime>(), It.IsAny<GeographicLocation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CalendarInfluenceContext());
        _languageOptimizerMock.Setup(x => x.OptimizeLanguageRouting(It.IsAny<List<LanguageCode>>(), It.IsAny<GeographicLocation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LanguageRoutingOptimization());
        _appropriatenessScorerMock.Setup(x => x.CalculateCulturalAppropriateness(It.IsAny<SpatialCulturalContext>(), It.IsAny<CalendarInfluenceContext>(), It.IsAny<LanguageRoutingOptimization>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CulturalAppropriatenessScore());
        _diasporaServiceMock.Setup(x => x.OptimizeForGlobalPerformance(It.IsAny<SpatialCulturalContext>(), It.IsAny<CalendarInfluenceContext>(), It.IsAny<LanguageRoutingOptimization>(), It.IsAny<CulturalAppropriatenessScore>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(performanceOptimizedDecision);

        // Act
        var result = await _service.RouteWithCulturalIntelligence(request, CancellationToken.None);

        // Assert - Global Performance SLA Compliance
        result.Should().NotBeNull();
        result.ExpectedResponseTime.Should().BeLessThan(TimeSpan.FromMilliseconds(200));
        result.PerformanceOptimizations.Should().NotBeEmpty();
        result.SlaCompliance.Should().NotBeNull();
        result.SlaCompliance.ResponseTimeSlaCompliant.Should().BeTrue();
        result.SlaCompliance.GlobalPerformanceRating.Should().BeGreaterThan(0.9m);
    }

    #endregion

    #region Cultural Routing Conflict Resolution Tests

    [Fact]
    public async Task ResolveOverlappingCulturalRegions_WithMultipleRegionOptions_ShouldSelectOptimalRegion()
    {
        // Arrange
        var overlappingRegions = new List<CulturalRegion>
        {
            new()
            {
                RegionId = "bayarea-srilankanbud-001",
                PrimaryCulturalCommunity = CulturalCommunityType.SriLankanBuddhist,
                AverageResponseTimeMs = 145,
                CulturalAuthorityRating = 0.96m,
                CommunityDensity = 0.85m
            },
            new()
            {
                RegionId = "bayarea-indianhind-002",
                PrimaryCulturalCommunity = CulturalCommunityType.IndianHindu,
                AverageResponseTimeMs = 155,
                CulturalAuthorityRating = 0.92m,
                CommunityDensity = 0.78m
            },
            new()
            {
                RegionId = "bayarea-multicult-003",
                PrimaryCulturalCommunity = CulturalCommunityType.MultiCultural,
                AverageResponseTimeMs = 135,
                CulturalAuthorityRating = 0.88m,
                CommunityDensity = 0.95m
            }
        };

        var request = new CulturalRoutingRequest
        {
            RequestId = Guid.NewGuid(),
            CulturalCommunityType = CulturalCommunityType.SriLankanBuddhist,
            UserLanguages = new List<LanguageCode> { LanguageCode.Sinhala }
        };

        var expectedOptimalDecision = new CulturalRoutingDecision
        {
            DecisionId = Guid.NewGuid(),
            SelectedRegion = overlappingRegions[0], // Best cultural match for Sri Lankan Buddhist
            ConflictResolutionReason = "CulturalAuthenticityPriority: SriLankanBuddhist community match (96% authority) selected over performance optimization (135ms vs 145ms)",
            CulturalAppropriateness = 0.96m,
            ConflictResolutionStrategy = ConflictResolutionStrategy.CulturalAuthenticityFirst
        };

        _conflictResolverMock.Setup(x => x.ResolveOverlappingCulturalRegions(
                It.IsAny<CulturalRoutingRequest>(), It.Is<List<CulturalRegion>>(r => r.Count == 3), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedOptimalDecision);

        // Act
        var result = await _service.ResolveOverlappingCulturalRegions(request, overlappingRegions, CancellationToken.None);

        // Assert - Conflict Resolution with Cultural Priority
        result.Should().NotBeNull();
        result.SelectedRegion.PrimaryCulturalCommunity.Should().Be(CulturalCommunityType.SriLankanBuddhist);
        result.ConflictResolutionStrategy.Should().Be(ConflictResolutionStrategy.CulturalAuthenticityFirst);
        result.ConflictResolutionReason.Should().Contain("CulturalAuthenticityPriority");
        result.CulturalAppropriateness.Should().BeGreaterThan(0.95m);
    }

    #endregion

    #region Validation and Error Handling Tests

    [Fact]
    public async Task RouteWithCulturalIntelligence_WithInvalidLocation_ShouldThrowValidationException()
    {
        // Arrange
        var invalidRequest = new CulturalRoutingRequest
        {
            RequestId = Guid.NewGuid(),
            Location = null!, // Invalid location
            UserLanguages = new List<LanguageCode> { LanguageCode.English }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.RouteWithCulturalIntelligence(invalidRequest, CancellationToken.None));
    }

    [Fact]
    public async Task RouteWithCulturalIntelligence_WithEmptyLanguagesList_ShouldThrowValidationException()
    {
        // Arrange
        var invalidRequest = new CulturalRoutingRequest
        {
            RequestId = Guid.NewGuid(),
            UserLanguages = new List<LanguageCode>(), // Empty languages list
            Location = new GeographicLocation { Region = "NorthAmerica" }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.RouteWithCulturalIntelligence(invalidRequest, CancellationToken.None));
    }

    #endregion

    #region IDisposable Implementation

    public void Dispose()
    {
        _service?.Dispose();
        GC.SuppressFinalize(this);
    }

    #endregion
}

#region Test Supporting Models and Enums

public enum LanguageCode
{
    English = 1,
    Sinhala = 2,
    Tamil = 3,
    Hindi = 4,
    Urdu = 5,
    Punjabi = 6,
    Bengali = 7,
    Gujarati = 8,
    Arabic = 9
}

public enum CalendarType
{
    Buddhist = 1,
    Hindu = 2,
    Islamic = 3,
    Sikh = 4,
    Christian = 5
}

public enum ConflictResolutionStrategy
{
    CulturalAuthenticityFirst = 1,
    PerformanceFirst = 2,
    BalancedApproach = 3,
    SacredEventPriority = 4
}

public class CulturalRoutingRequest
{
    public Guid RequestId { get; set; }
    public GeographicLocation Location { get; set; } = null!;
    public List<LanguageCode> UserLanguages { get; set; } = new();
    public CulturalCommunityType CulturalCommunityType { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public TimeSpan RequiredResponseTime { get; set; } = TimeSpan.FromMilliseconds(200);
    public List<string> CulturalContextHints { get; set; } = new();
}

public class GeographicLocation
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
}

public class CulturalRoutingDecision
{
    public Guid DecisionId { get; set; }
    public CulturalRegion SelectedRegion { get; set; } = null!;
    public decimal CulturalAppropriateness { get; set; }
    public TimeSpan ExpectedResponseTime { get; set; }
    public decimal RoutingConfidence { get; set; }
    public List<string> PerformanceOptimizations { get; set; } = new();
    public bool SacredEventOptimization { get; set; }
    public CalendarInfluenceContext CalendarInfluence { get; set; } = null!;
    public LanguageRoutingOptimization LanguageOptimization { get; set; } = null!;
    public SlaComplianceValidation SlaCompliance { get; set; } = null!;
    public string ConflictResolutionReason { get; set; } = string.Empty;
    public ConflictResolutionStrategy ConflictResolutionStrategy { get; set; }
}

public class SpatialCulturalContext
{
    public string PrimaryCulturalRegion { get; set; } = string.Empty;
    public decimal CommunityDensity { get; set; }
    public int SacredSiteProximity { get; set; }
    public decimal CulturalAuthenticity { get; set; }
}

public class CalendarInfluenceContext
{
    public List<CalendarInfluence> ActiveCalendarInfluences { get; set; } = new();
    public decimal OverallInfluence { get; set; }
    public List<string> RecommendedOptimizations { get; set; } = new();
}

public class CalendarInfluence
{
    public CalendarType Type { get; set; }
    public string EventName { get; set; } = string.Empty;
    public decimal Intensity { get; set; }
    public SacredEventPriority RoutingPriority { get; set; }
    public decimal TrafficMultiplier { get; set; }
    public string CulturalSignificance { get; set; } = string.Empty;
}

public class LanguageRoutingOptimization
{
    public LanguageCode PrimaryLanguage { get; set; }
    public LanguageCode[] SecondaryLanguages { get; set; } = Array.Empty<LanguageCode>();
    public decimal LocalizedContentAvailability { get; set; }
    public decimal TranslationQuality { get; set; }
    public decimal CulturalNuancePreservation { get; set; }
    public Dictionary<LanguageCode, decimal> LanguagePreferencePriority { get; set; } = new();
    public bool MultiLanguageCompatibility { get; set; }
}

public class CulturalAppropriatenessScore
{
    public decimal OverallScore { get; set; }
    public decimal GeographicScore { get; set; }
    public decimal TemporalScore { get; set; }
    public decimal CommunityScore { get; set; }
    public decimal LanguageScore { get; set; }
    public decimal CulturalConfidenceLevel { get; set; }
}

public class CulturalRegion
{
    public string RegionId { get; set; } = string.Empty;
    public string RegionName { get; set; } = string.Empty;
    public CulturalCommunityType PrimaryCulturalCommunity { get; set; }
    public int AverageResponseTimeMs { get; set; }
    public decimal CulturalAuthorityRating { get; set; }
    public decimal CommunityDensity { get; set; }
}

public class SlaComplianceValidation
{
    public bool ResponseTimeSlaCompliant { get; set; }
    public decimal GlobalPerformanceRating { get; set; }
}

#endregion