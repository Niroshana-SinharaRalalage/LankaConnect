using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Communications.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace LankaConnect.Domain.Tests.Communications.Services;

/// <summary>
/// TDD tests for Cultural WhatsApp Service - Testing Buddhist/Hindu calendar integration,
/// cultural appropriateness validation, and diaspora community targeting
/// </summary>
public class CulturalWhatsAppServiceTests
{
    private readonly Mock<ICulturalWhatsAppService> _mockCulturalWhatsAppService;
    private readonly Mock<IDiasporaNotificationService> _mockDiasporaService;
    private readonly Mock<ICulturalTimingOptimizer> _mockTimingOptimizer;
    private readonly Mock<ICulturalMessageValidator> _mockMessageValidator;

    public CulturalWhatsAppServiceTests()
    {
        _mockCulturalWhatsAppService = new Mock<ICulturalWhatsAppService>();
        _mockDiasporaService = new Mock<IDiasporaNotificationService>();
        _mockTimingOptimizer = new Mock<ICulturalTimingOptimizer>();
        _mockMessageValidator = new Mock<ICulturalMessageValidator>();
    }

    #region Cultural Appropriateness Validation Tests

    [Fact]
    public async Task ValidateCulturalAppropriatenessAsync_WithVesakGreeting_ShouldReturnHighScore()
    {
        // Arrange
        var vesakMessage = CreateVesakGreetingMessage();
        var expectedScore = 0.92;

        _mockCulturalWhatsAppService
            .Setup(s => s.ValidateCulturalAppropriatenessAsync(vesakMessage))
            .ReturnsAsync(Result<double>.Success(expectedScore));

        // Act
        var result = await _mockCulturalWhatsAppService.Object.ValidateCulturalAppropriatenessAsync(vesakMessage);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedScore);
        result.Value.Should().BeGreaterThan(0.7); // Above cultural validation threshold
        
        _mockCulturalWhatsAppService.Verify(s => s.ValidateCulturalAppropriatenessAsync(vesakMessage), Times.Once);
    }

    [Fact]
    public async Task ValidateCulturalAppropriatenessAsync_WithInappropriateContent_ShouldReturnLowScore()
    {
        // Arrange
        var inappropriateMessage = CreateInappropriateReligiousMessage();
        var expectedScore = 0.15;

        _mockCulturalWhatsAppService
            .Setup(s => s.ValidateCulturalAppropriatenessAsync(inappropriateMessage))
            .ReturnsAsync(Result<double>.Success(expectedScore));

        // Act
        var result = await _mockCulturalWhatsAppService.Object.ValidateCulturalAppropriatenessAsync(inappropriateMessage);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedScore);
        result.Value.Should().BeLessThan(0.7); // Below cultural validation threshold
    }

    [Fact]
    public async Task ValidateCulturalAppropriatenessAsync_WithDeepavaliGreeting_ShouldReturnHighScore()
    {
        // Arrange
        var deepavaliMessage = CreateDeepavaliGreetingMessage();
        var expectedScore = 0.88;

        _mockCulturalWhatsAppService
            .Setup(s => s.ValidateCulturalAppropriatenessAsync(deepavaliMessage))
            .ReturnsAsync(Result<double>.Success(expectedScore));

        // Act
        var result = await _mockCulturalWhatsAppService.Object.ValidateCulturalAppropriatenessAsync(deepavaliMessage);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(expectedScore);
        result.Value.Should().BeGreaterThan(0.7);
    }

    #endregion

    #region Buddhist/Hindu Calendar Integration Tests

    [Fact]
    public async Task OptimizeMessageTimingAsync_WithVesakDay_ShouldAvoidMeditationHours()
    {
        // Arrange
        var vesakMessage = CreateVesakGreetingMessage();
        var requestedTime = new DateTime(2024, 5, 16, 19, 0, 0, DateTimeKind.Utc); // 7 PM meditation time
        var optimizedTime = new DateTime(2024, 5, 16, 10, 0, 0, DateTimeKind.Utc); // 10 AM optimal

        _mockCulturalWhatsAppService
            .Setup(s => s.OptimizeMessageTimingAsync(vesakMessage, requestedTime))
            .ReturnsAsync(Result<DateTime>.Success(optimizedTime));

        // Act
        var result = await _mockCulturalWhatsAppService.Object.OptimizeMessageTimingAsync(vesakMessage, requestedTime);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(optimizedTime);
        result.Value.Hour.Should().Be(10); // Morning time, avoiding evening meditation
    }

    [Fact]
    public async Task OptimizeMessageTimingAsync_WithPoyaDayMessage_ShouldRespectObservanceHours()
    {
        // Arrange
        var poyaMessage = CreatePoyaDayReminderMessage();
        var requestedTime = new DateTime(2024, 6, 1, 20, 30, 0, DateTimeKind.Utc); // 8:30 PM quiet hours
        var optimizedTime = new DateTime(2024, 6, 1, 8, 0, 0, DateTimeKind.Utc); // 8 AM optimal

        _mockCulturalWhatsAppService
            .Setup(s => s.OptimizeMessageTimingAsync(poyaMessage, requestedTime))
            .ReturnsAsync(Result<DateTime>.Success(optimizedTime));

        // Act
        var result = await _mockCulturalWhatsAppService.Object.OptimizeMessageTimingAsync(poyaMessage, requestedTime);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(optimizedTime);
        result.Value.Hour.Should().Be(8); // Early morning, respecting evening quiet hours
    }

    [Fact]
    public async Task IsTimingReligiouslyAppropriateAsync_WithDeepavaliEvening_ShouldAllowLakshmiPujaTime()
    {
        // Arrange
        var deepavaliContext = CulturalContext.ForHinduFestival("Deepavali", new DateTime(2024, 11, 1));
        var eveningTime = new DateTime(2024, 11, 1, 18, 0, 0, DateTimeKind.Utc); // 6 PM - Lakshmi puja time

        _mockCulturalWhatsAppService
            .Setup(s => s.IsTimingReligiouslyAppropriateAsync(eveningTime, deepavaliContext))
            .ReturnsAsync(Result<bool>.Success(true));

        // Act
        var result = await _mockCulturalWhatsAppService.Object.IsTimingReligiouslyAppropriateAsync(eveningTime, deepavaliContext);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue(); // Evening is appropriate for Deepavali celebration
    }

    #endregion

    #region Diaspora Community Targeting Tests

    [Fact]
    public async Task GetOptimalDiasporaRegionsAsync_WithCulturalEvent_ShouldReturnTargetedRegions()
    {
        // Arrange
        var culturalEventMessage = CreateCulturalEventMessage();
        var expectedRegions = new[] { "Bay Area", "Toronto", "London", "Sydney" };

        _mockCulturalWhatsAppService
            .Setup(s => s.GetOptimalDiasporaRegionsAsync(culturalEventMessage))
            .ReturnsAsync(Result<IEnumerable<string>>.Success(expectedRegions));

        // Act
        var result = await _mockCulturalWhatsAppService.Object.GetOptimalDiasporaRegionsAsync(culturalEventMessage);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(expectedRegions);
        result.Value.Should().Contain("Bay Area"); // Major Sri Lankan diaspora hub
        result.Value.Should().Contain("Toronto"); // Canadian diaspora community
        result.Value.Should().Contain("London"); // UK diaspora community
    }

    [Fact]
    public async Task GetTargetDiasporaCommunitiesAsync_WithBroadcastMessage_ShouldReturnClusterAnalysis()
    {
        // Arrange
        var broadcastMessage = CreateDiasporaBroadcastMessage();
        var expectedClusters = new[]
        {
            new DiasporaCluster
            {
                Region = "Bay Area",
                City = "San Jose",
                Country = "USA",
                TimeZone = "America/Los_Angeles",
                EstimatedPopulation = 45000,
                EngagementScore = 0.82,
                PreferredLanguages = new[] { "si", "en", "ta" },
                DominantReligions = new[] { "Buddhism", "Hinduism", "Christianity" }
            },
            new DiasporaCluster
            {
                Region = "Toronto",
                City = "Toronto",
                Country = "Canada", 
                TimeZone = "America/Toronto",
                EstimatedPopulation = 38000,
                EngagementScore = 0.78,
                PreferredLanguages = new[] { "en", "ta", "si" },
                DominantReligions = new[] { "Hinduism", "Buddhism", "Christianity" }
            }
        };

        _mockDiasporaService
            .Setup(s => s.GetTargetDiasporaCommunitiesAsync(broadcastMessage))
            .ReturnsAsync(Result<IEnumerable<DiasporaCluster>>.Success(expectedClusters));

        // Act
        var result = await _mockDiasporaService.Object.GetTargetDiasporaCommunitiesAsync(broadcastMessage);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        
        var bayAreaCluster = result.Value.First(c => c.Region == "Bay Area");
        bayAreaCluster.EstimatedPopulation.Should().Be(45000);
        bayAreaCluster.EngagementScore.Should().Be(0.82);
        bayAreaCluster.PreferredLanguages.Should().Contain("si");
    }

    #endregion

    #region Multi-Language Support Tests

    [Fact]
    public async Task SelectOptimalLanguageAsync_WithSinhaleseRecipients_ShouldReturnSinhala()
    {
        // Arrange
        var message = CreateVesakGreetingMessage();
        var sinhaleseRecipients = new[] { "+94711234567", "+94771234567" }; // Sri Lankan numbers

        _mockCulturalWhatsAppService
            .Setup(s => s.SelectOptimalLanguageAsync(message, sinhaleseRecipients))
            .ReturnsAsync(Result<string>.Success("si"));

        // Act
        var result = await _mockCulturalWhatsAppService.Object.SelectOptimalLanguageAsync(message, sinhaleseRecipients);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("si"); // Sinhala for Buddhist festival
    }

    [Fact]
    public async Task SelectOptimalLanguageAsync_WithTamilRecipients_ShouldReturnTamil()
    {
        // Arrange
        var deepavaliMessage = CreateDeepavaliGreetingMessage();
        var tamilRecipients = new[] { "+15551234567" }; // US diaspora with Tamil preference

        _mockCulturalWhatsAppService
            .Setup(s => s.SelectOptimalLanguageAsync(deepavaliMessage, tamilRecipients))
            .ReturnsAsync(Result<string>.Success("ta"));

        // Act
        var result = await _mockCulturalWhatsAppService.Object.SelectOptimalLanguageAsync(deepavaliMessage, tamilRecipients);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("ta"); // Tamil for Hindu festival
    }

    [Fact]
    public async Task GenerateCulturallyAwareContentAsync_WithVesakContext_ShouldIncludeBuddhistWisdom()
    {
        // Arrange
        var baseContent = "Happy Vesak Day!";
        var vesakContext = CulturalContext.ForBuddhistFestival("Vesak", new DateTime(2024, 5, 16));
        var expectedEnhancedContent = "May this sacred Vesak Day bring you inner peace and wisdom. May the Buddha's teachings guide your path to enlightenment. Happy Vesak Day!";

        _mockCulturalWhatsAppService
            .Setup(s => s.GenerateCulturallyAwareContentAsync(baseContent, vesakContext, "en"))
            .ReturnsAsync(Result<string>.Success(expectedEnhancedContent));

        // Act
        var result = await _mockCulturalWhatsAppService.Object.GenerateCulturallyAwareContentAsync(baseContent, vesakContext, "en");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Contain("inner peace");
        result.Value.Should().Contain("Buddha's teachings");
        result.Value.Should().Contain("enlightenment");
    }

    #endregion

    #region Cultural Metadata and Intelligence Tests

    [Fact]
    public async Task GetRecommendedCulturalMetadataAsync_WithBuddhistEvent_ShouldReturnReligiousMetadata()
    {
        // Arrange
        var buddhistEventMessage = CreateVesakGreetingMessage();
        var expectedMetadata = new Dictionary<string, string>
        {
            { "festival_significance", "buddha_birth_enlightenment_death" },
            { "religious_observance", "meditation_dana_precepts" },
            { "timing_preference", "morning_optimal" },
            { "cultural_sensitivity", "high" },
            { "target_demographic", "buddhist_diaspora" }
        };

        _mockCulturalWhatsAppService
            .Setup(s => s.GetRecommendedCulturalMetadataAsync(buddhistEventMessage))
            .ReturnsAsync(Result<Dictionary<string, string>>.Success(expectedMetadata));

        // Act
        var result = await _mockCulturalWhatsAppService.Object.GetRecommendedCulturalMetadataAsync(buddhistEventMessage);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainKey("festival_significance");
        result.Value.Should().ContainKey("religious_observance");
        result.Value["cultural_sensitivity"].Should().Be("high");
    }

    #endregion

    #region Timing Optimizer Tests

    [Fact]
    public async Task IsBuddhistObservanceTimeAsync_WithPoyaDayEvening_ShouldReturnTrue()
    {
        // Arrange
        var poyaDayEvening = new DateTime(2024, 6, 1, 19, 0, 0, DateTimeKind.Utc); // 7 PM meditation time
        var sriLankaTimeZone = "Asia/Colombo";

        _mockTimingOptimizer
            .Setup(s => s.IsBuddhistObservanceTimeAsync(poyaDayEvening, sriLankaTimeZone))
            .ReturnsAsync(Result<bool>.Success(true));

        // Act
        var result = await _mockTimingOptimizer.Object.IsBuddhistObservanceTimeAsync(poyaDayEvening, sriLankaTimeZone);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue(); // Evening of Poyaday is observance time
    }

    [Fact]
    public async Task FindNextOptimalDeliveryTimeAsync_WithConflictingTime_ShouldSuggestAlternative()
    {
        // Arrange
        var conflictingTime = new DateTime(2024, 5, 16, 20, 0, 0, DateTimeKind.Utc); // Vesak evening meditation
        var vesakContext = CulturalContext.ForBuddhistFestival("Vesak", new DateTime(2024, 5, 16));
        var optimalTime = new DateTime(2024, 5, 16, 9, 0, 0, DateTimeKind.Utc); // Morning alternative

        _mockTimingOptimizer
            .Setup(s => s.FindNextOptimalDeliveryTimeAsync(conflictingTime, vesakContext, "Asia/Colombo"))
            .ReturnsAsync(Result<DateTime>.Success(optimalTime));

        // Act
        var result = await _mockTimingOptimizer.Object.FindNextOptimalDeliveryTimeAsync(conflictingTime, vesakContext, "Asia/Colombo");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(optimalTime);
        result.Value.Hour.Should().Be(9); // Morning alternative
    }

    #endregion

    #region Message Validator Tests

    [Fact]
    public async Task ValidateMessageContentAsync_WithAppropriateBuddhistContent_ShouldReturnHighScore()
    {
        // Arrange
        var appropriateContent = "May the blessings of the Triple Gem bring you peace and wisdom on this sacred Vesak Day.";
        var buddhistContext = CulturalContext.ForBuddhistFestival("Vesak", DateTime.Now);
        var expectedResult = new CulturalValidationResult
        {
            AppropriatnessScore = 0.94,
            IsAcceptable = true,
            Issues = new List<string>(),
            Suggestions = new[] { "Consider adding reference to Buddha's teachings" },
            DetailedScores = new Dictionary<string, double>
            {
                { "religious_accuracy", 0.96 },
                { "cultural_sensitivity", 0.92 },
                { "language_appropriateness", 0.94 }
            }
        };

        _mockMessageValidator
            .Setup(s => s.ValidateMessageContentAsync(appropriateContent, buddhistContext, "en"))
            .ReturnsAsync(Result<CulturalValidationResult>.Success(expectedResult));

        // Act
        var result = await _mockMessageValidator.Object.ValidateMessageContentAsync(appropriateContent, buddhistContext, "en");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AppropriatnessScore.Should().Be(0.94);
        result.Value.IsAcceptable.Should().BeTrue();
        result.Value.Issues.Should().BeEmpty();
    }

    [Fact]
    public async Task DetectCulturalSensitivityIssuesAsync_WithInappropriateContent_ShouldReturnIssues()
    {
        // Arrange
        var inappropriateContent = "Let's party hard this Vesak Day with drinks and loud music!";
        var expectedIssues = new[]
        {
            "Inappropriate suggestion of alcohol consumption during religious observance",
            "Loud activities conflict with meditation and reflection requirements",
            "Party atmosphere inappropriate for sacred Buddhist festival"
        };

        _mockMessageValidator
            .Setup(s => s.DetectCulturalSensitivityIssuesAsync(inappropriateContent, "en"))
            .ReturnsAsync(Result<IEnumerable<string>>.Success(expectedIssues));

        // Act
        var result = await _mockMessageValidator.Object.DetectCulturalSensitivityIssuesAsync(inappropriateContent, "en");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value.Should().Contain(issue => issue.Contains("alcohol consumption"));
        result.Value.Should().Contain(issue => issue.Contains("meditation and reflection"));
    }

    #endregion

    #region Integration Scenario Tests

    [Fact]
    public async Task DiasporaBroadcast_VesakCelebration_ShouldOptimizeForGlobalCommunities()
    {
        // Arrange
        var globalVesakBroadcast = CreateGlobalVesakBroadcastMessage();
        var targetClusters = new[]
        {
            CreateDiasporaCluster("Bay Area", "America/Los_Angeles", 45000),
            CreateDiasporaCluster("Toronto", "America/Toronto", 38000),
            CreateDiasporaCluster("London", "Europe/London", 42000),
            CreateDiasporaCluster("Sydney", "Australia/Sydney", 28000)
        };

        var optimizedTimings = new Dictionary<string, DateTime>
        {
            { "Bay Area", new DateTime(2024, 5, 16, 10, 0, 0, DateTimeKind.Utc) },
            { "Toronto", new DateTime(2024, 5, 16, 11, 0, 0, DateTimeKind.Utc) },
            { "London", new DateTime(2024, 5, 16, 9, 0, 0, DateTimeKind.Utc) },
            { "Sydney", new DateTime(2024, 5, 16, 8, 0, 0, DateTimeKind.Utc) }
        };

        _mockDiasporaService
            .Setup(s => s.GetTargetDiasporaCommunitiesAsync(globalVesakBroadcast))
            .ReturnsAsync(Result<IEnumerable<DiasporaCluster>>.Success(targetClusters));

        _mockDiasporaService
            .Setup(s => s.OptimizeBroadcastTimingByRegionAsync(globalVesakBroadcast, targetClusters))
            .ReturnsAsync(Result<Dictionary<string, DateTime>>.Success(optimizedTimings));

        // Act
        var clustersResult = await _mockDiasporaService.Object.GetTargetDiasporaCommunitiesAsync(globalVesakBroadcast);
        var timingResult = await _mockDiasporaService.Object.OptimizeBroadcastTimingByRegionAsync(globalVesakBroadcast, clustersResult.Value);

        // Assert
        clustersResult.IsSuccess.Should().BeTrue();
        clustersResult.Value.Should().HaveCount(4);
        
        timingResult.IsSuccess.Should().BeTrue();
        timingResult.Value.Should().ContainKey("Bay Area");
        timingResult.Value["Bay Area"].Hour.Should().Be(10); // Morning time for Buddhist observance
        timingResult.Value["Sydney"].Hour.Should().Be(8); // Early morning for Australia
    }

    #endregion

    #region Helper Methods

    private static WhatsAppMessage CreateVesakGreetingMessage()
    {
        var culturalContext = WhatsAppCulturalContext.ForBuddhistFestival("Vesak", new DateTime(2024, 5, 16));
        return WhatsAppMessage.Create(
            "+94771234567",
            new[] { "+15551234567", "+16501234567" },
            "May this sacred Vesak Day bring you inner peace, wisdom, and compassion. May the Buddha's teachings illuminate your path to enlightenment.",
            WhatsAppMessageType.FestivalGreeting,
            culturalContext,
            "si").Value;
    }

    private static WhatsAppMessage CreateDeepavaliGreetingMessage()
    {
        var culturalContext = WhatsAppCulturalContext.ForHinduFestival("Deepavali", new DateTime(2024, 11, 1));
        return WhatsAppMessage.Create(
            "+94771234567",
            new[] { "+15551234567", "+16471234567" },
            "Wishing you and your family a very Happy Deepavali! May the festival of lights bring prosperity, happiness, and success to your home.",
            WhatsAppMessageType.FestivalGreeting,
            culturalContext,
            "ta").Value;
    }

    private static WhatsAppMessage CreateInappropriateReligiousMessage()
    {
        var culturalContext = WhatsAppCulturalContext.ForBuddhistFestival("Vesak", new DateTime(2024, 5, 16));
        return WhatsAppMessage.Create(
            "+94771234567",
            new[] { "+15551234567" },
            "Let's party hard this Vesak Day with drinks and loud music!",
            WhatsAppMessageType.FestivalGreeting,
            culturalContext).Value;
    }

    private static WhatsAppMessage CreatePoyaDayReminderMessage()
    {
        var culturalContext = new WhatsAppCulturalContext(
            hasReligiousContent: true,
            primaryReligion: "Buddhism",
            requiresBuddhistCalendarAwareness: true);
        return WhatsAppMessage.Create(
            "+94771234567",
            new[] { "+94711234567", "+94721234567" },
            "Today is Poyaday. A time for meditation, dana, and observing the eight precepts. May you find peace in your practice.",
            WhatsAppMessageType.Reminder,
            culturalContext,
            "si").Value;
    }

    private static WhatsAppMessage CreateCulturalEventMessage()
    {
        var culturalContext = new WhatsAppCulturalContext(hasReligiousContent: true, isFestivalRelated: true);
        return WhatsAppMessage.Create(
            "+94771234567",
            new[] { "+15551234567", "+16471234567", "+447911123456" },
            "Join us for the annual Sri Lankan Cultural Festival featuring traditional music, dance, and cuisine.",
            WhatsAppMessageType.EventNotification,
            culturalContext).Value;
    }

    private static WhatsAppMessage CreateDiasporaBroadcastMessage()
    {
        return WhatsAppMessage.Create(
            "+94771234567",
            new[] { "+15551234567", "+16471234567", "+447911123456", "+61412345678" },
            "Important community announcement for all Sri Lankan diaspora members.",
            WhatsAppMessageType.Broadcast,
            CulturalContext.None).Value;
    }

    private static WhatsAppMessage CreateGlobalVesakBroadcastMessage()
    {
        var culturalContext = WhatsAppCulturalContext.ForBuddhistFestival("Vesak", new DateTime(2024, 5, 16));
        return WhatsAppMessage.Create(
            "+94771234567",
            new[] { "+15551234567", "+16471234567", "+447911123456", "+61412345678" },
            "May the blessings of Vesak Day bring peace and wisdom to Buddhist communities worldwide.",
            WhatsAppMessageType.Broadcast,
            culturalContext).Value;
    }

    private static DiasporaCluster CreateDiasporaCluster(string region, string timeZone, int population)
    {
        return new DiasporaCluster
        {
            Region = region,
            TimeZone = timeZone,
            EstimatedPopulation = population,
            EngagementScore = 0.8,
            PreferredLanguages = new[] { "si", "en", "ta" },
            DominantReligions = new[] { "Buddhism", "Hinduism" }
        };
    }

    #endregion
}