using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using LankaConnect.Domain.Common.Database;
using LankaConnect.Infrastructure.Database.LoadBalancing;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Infrastructure.Tests.Database;

/// <summary>
/// TDD RED Phase: Comprehensive failing tests for Multi-Language Affinity Routing Engine
/// Testing multi-language support for 6M+ South Asian diaspora across generational cohorts
/// Covers Sinhala, Tamil, Hindi, Urdu, Punjabi, Bengali, Gujarati language preferences
/// </summary>
public class MultiLanguageAffinityRoutingEngineTests
{
    private readonly Mock<ILogger<MultiLanguageAffinityRoutingEngine>> _mockLogger;
    private readonly MultiLanguageAffinityRoutingEngine _routingEngine;
    
    public MultiLanguageAffinityRoutingEngineTests()
    {
        _mockLogger = new Mock<ILogger<MultiLanguageAffinityRoutingEngine>>();
        _routingEngine = new MultiLanguageAffinityRoutingEngine(_mockLogger.Object);
    }

    #region Multi-Language Detection Tests

    [Fact]
    public async Task DetectLanguagePreferences_WithSinhaleseContent_ShouldDetectSinhala()
    {
        // Arrange - Sinhala content for Buddhist community
        var userContent = "මෙම පොඩක්ක මම අභිමන් කරන සිංහල ඉතිහාසය";
        var userId = Guid.NewGuid();
        
        // Act
        var result = await _routingEngine.DetectLanguagePreferencesAsync(userId, userContent);
        
        // Assert - Should fail until implemented
        result.Should().NotBeNull();
        result.PrimaryLanguage.Should().Be(SouthAsianLanguage.Sinhala);
        result.LanguageConfidence.Should().BeGreaterThan(0.85m);
        result.CulturalContext.Should().Be(CulturalContext.Buddhist);
    }

    [Fact]
    public async Task DetectLanguagePreferences_WithTamilContent_ShouldDetectTamil()
    {
        // Arrange - Tamil content for Hindu community
        var userContent = "இந்த தமிழ் சமுதாயம் மிகவும் முக்கியமானது";
        var userId = Guid.NewGuid();
        
        // Act
        var result = await _routingEngine.DetectLanguagePreferencesAsync(userId, userContent);
        
        // Assert - Should fail until implemented
        result.Should().NotBeNull();
        result.PrimaryLanguage.Should().Be(SouthAsianLanguage.Tamil);
        result.LanguageConfidence.Should().BeGreaterThan(0.85m);
        result.CulturalContext.Should().Be(CulturalContext.Hindu);
    }

    [Theory]
    [InlineData(SouthAsianLanguage.Hindi, CulturalContext.Hindu)]
    [InlineData(SouthAsianLanguage.Urdu, CulturalContext.Islamic)]
    [InlineData(SouthAsianLanguage.Punjabi, CulturalContext.Sikh)]
    [InlineData(SouthAsianLanguage.Bengali, CulturalContext.Hindu)]
    [InlineData(SouthAsianLanguage.Gujarati, CulturalContext.Hindu)]
    public async Task DetectLanguagePreferences_WithVariousSouthAsianLanguages_ShouldDetectCorrectly(
        SouthAsianLanguage expectedLanguage, CulturalContext expectedContext)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userContent = GetSampleContentForLanguage(expectedLanguage);
        
        // Act
        var result = await _routingEngine.DetectLanguagePreferencesAsync(userId, userContent);
        
        // Assert - Should fail until implemented
        result.Should().NotBeNull();
        result.PrimaryLanguage.Should().Be(expectedLanguage);
        result.CulturalContext.Should().Be(expectedContext);
        result.LanguageConfidence.Should().BeGreaterThan(0.80m);
    }

    #endregion

    #region Generational Pattern Recognition Tests

    [Fact]
    public async Task AnalyzeGenerationalPattern_WithFirstGenerationUser_ShouldReturnHeritageLanguageDominance()
    {
        // Arrange - First generation user with strong heritage language preference
        var userId = Guid.NewGuid();
        var userProfile = new MultiLanguageUserProfile
        {
            UserId = userId,
            GenerationalCohort = GenerationalCohort.FirstGeneration,
            NativeLanguages = new Dictionary<SouthAsianLanguage, decimal>
            {
                { SouthAsianLanguage.Sinhala, 0.95m },
                { SouthAsianLanguage.English, 0.60m }
            },
            CulturalBackground = CulturalBackground.SriLankanBuddhist
        };
        
        // Act
        var result = await _routingEngine.AnalyzeGenerationalPatternAsync(userProfile);
        
        // Assert - Should fail until implemented
        result.Should().NotBeNull();
        result.HeritageLanguagePreference.Should().BeGreaterThan(0.85m);
        result.EnglishPreference.Should().BeLessThan(0.20m);
        result.CulturalEventBoostFactor.Should().BeGreaterThan(0.95m);
        result.SacredContentLanguageRequirement.Should().Be(SouthAsianLanguage.Sinhala);
    }

    [Fact]
    public async Task AnalyzeGenerationalPattern_WithSecondGenerationUser_ShouldReturnBalancedPattern()
    {
        // Arrange - Second generation user with balanced language preferences
        var userId = Guid.NewGuid();
        var userProfile = new MultiLanguageUserProfile
        {
            UserId = userId,
            GenerationalCohort = GenerationalCohort.SecondGeneration,
            NativeLanguages = new Dictionary<SouthAsianLanguage, decimal>
            {
                { SouthAsianLanguage.Tamil, 0.75m },
                { SouthAsianLanguage.English, 0.85m }
            },
            CulturalBackground = CulturalBackground.IndianTamil
        };
        
        // Act
        var result = await _routingEngine.AnalyzeGenerationalPatternAsync(userProfile);
        
        // Assert - Should fail until implemented
        result.Should().NotBeNull();
        result.HeritageLanguagePreference.Should().BeApproximately(0.45m, 0.05m);
        result.EnglishPreference.Should().BeApproximately(0.55m, 0.05m);
        result.CulturalEventBoostFactor.Should().BeApproximately(0.75m, 0.05m);
        result.BilingualContentPreference.Should().BeTrue();
    }

    [Fact]
    public async Task AnalyzeGenerationalPattern_WithThirdGenerationUser_ShouldReturnEnglishDominance()
    {
        // Arrange - Third generation user with English preference
        var userId = Guid.NewGuid();
        var userProfile = new MultiLanguageUserProfile
        {
            UserId = userId,
            GenerationalCohort = GenerationalCohort.ThirdGenerationPlus,
            NativeLanguages = new Dictionary<SouthAsianLanguage, decimal>
            {
                { SouthAsianLanguage.Punjabi, 0.45m },
                { SouthAsianLanguage.English, 0.95m }
            },
            CulturalBackground = CulturalBackground.SikhPunjabi
        };
        
        // Act
        var result = await _routingEngine.AnalyzeGenerationalPatternAsync(userProfile);
        
        // Assert - Should fail until implemented
        result.Should().NotBeNull();
        result.HeritageLanguagePreference.Should().BeApproximately(0.25m, 0.05m);
        result.EnglishPreference.Should().BeApproximately(0.75m, 0.05m);
        result.HeritageLanguageLearningRecommendation.Should().BeTrue();
        result.IntergenerationalBridgingContent.Should().BeTrue();
    }

    #endregion

    #region Cultural Event Language Boost Tests

    [Theory]
    [InlineData(CulturalEvent.Vesak, SouthAsianLanguage.Sinhala, 0.95)]
    [InlineData(CulturalEvent.Diwali, SouthAsianLanguage.Hindi, 0.90)]
    [InlineData(CulturalEvent.Eid, SouthAsianLanguage.Urdu, 0.85)]
    [InlineData(CulturalEvent.Thaipusam, SouthAsianLanguage.Tamil, 0.90)]
    [InlineData(CulturalEvent.Vaisakhi, SouthAsianLanguage.Punjabi, 0.85)]
    public async Task CalculateCulturalEventLanguageBoost_DuringMajorEvents_ShouldBoostHeritageLanguage(
        CulturalEvent culturalEvent, SouthAsianLanguage expectedLanguage, double expectedBoost)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var eventContext = new CulturalEventContext
        {
            CurrentEvent = culturalEvent,
            EventIntensity = CulturalEventIntensity.Major,
            DaysUntilEvent = 0,
            CommunityParticipationLevel = CommunityParticipationLevel.High
        };
        
        // Act
        var result = await _routingEngine.CalculateCulturalEventLanguageBoostAsync(userId, eventContext);
        
        // Assert - Should fail until implemented
        result.Should().NotBeNull();
        result.PrimaryLanguage.Should().Be(expectedLanguage);
        result.BoostFactor.Should().BeGreaterThan((decimal)expectedBoost);
        result.SacredContentRequirement.Should().BeTrue();
    }

    [Fact]
    public async Task CalculateCulturalEventLanguageBoost_DuringMultipleCulturalEvents_ShouldHandleConflicts()
    {
        // Arrange - User with multiple cultural backgrounds during overlapping events
        var userId = Guid.NewGuid();
        var eventContext = new CulturalEventContext
        {
            OverlappingEvents = new List<CulturalEvent> { CulturalEvent.Diwali, CulturalEvent.Eid },
            EventIntensity = CulturalEventIntensity.Major,
            DaysUntilEvent = 1
        };
        
        // Act
        var result = await _routingEngine.CalculateCulturalEventLanguageBoostAsync(userId, eventContext);
        
        // Assert - Should fail until implemented
        result.Should().NotBeNull();
        result.ConflictResolutionStrategy.Should().NotBeNull();
        result.MultiCulturalContent.Should().BeTrue();
        result.LanguageAlternatives.Should().NotBeEmpty();
    }

    #endregion

    #region Sacred Content Language Requirements Tests

    [Fact]
    public async Task ValidateSacredContentLanguageRequirements_WithBuddhistContent_ShouldRequireSinhala()
    {
        // Arrange
        var contentRequest = new SacredContentRequest
        {
            ContentType = SacredContentType.Buddhist,
            RequestedLanguage = SouthAsianLanguage.English,
            UserCulturalBackground = CulturalBackground.SriLankanBuddhist
        };
        
        // Act
        var result = await _routingEngine.ValidateSacredContentLanguageRequirementsAsync(contentRequest);
        
        // Assert - Should fail until implemented
        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
        result.RequiredLanguage.Should().Be(SouthAsianLanguage.Sinhala);
        result.CulturalAppropriatenessScore.Should().BeLessThan(0.5m);
        result.RecommendedLanguage.Should().Be(SouthAsianLanguage.Sinhala);
    }

    [Fact]
    public async Task ValidateSacredContentLanguageRequirements_WithHinduContent_ShouldAllowMultipleLanguages()
    {
        // Arrange
        var contentRequest = new SacredContentRequest
        {
            ContentType = SacredContentType.Hindu,
            RequestedLanguage = SouthAsianLanguage.Tamil,
            UserCulturalBackground = CulturalBackground.IndianTamil
        };
        
        // Act
        var result = await _routingEngine.ValidateSacredContentLanguageRequirementsAsync(contentRequest);
        
        // Assert - Should fail until implemented
        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
        result.CulturalAppropriatenessScore.Should().BeGreaterThan(0.85m);
        result.AlternativeLanguages.Should().Contain(SouthAsianLanguage.Hindi);
        result.AlternativeLanguages.Should().Contain(SouthAsianLanguage.Sanskrit);
    }

    [Fact]
    public async Task ValidateSacredContentLanguageRequirements_WithIslamicContent_ShouldRequireArabicOrUrdu()
    {
        // Arrange
        var contentRequest = new SacredContentRequest
        {
            ContentType = SacredContentType.Islamic,
            RequestedLanguage = SouthAsianLanguage.English,
            UserCulturalBackground = CulturalBackground.PakistaniMuslim
        };
        
        // Act
        var result = await _routingEngine.ValidateSacredContentLanguageRequirementsAsync(contentRequest);
        
        // Assert - Should fail until implemented
        result.Should().NotBeNull();
        result.RequiredLanguage.Should().Be(SouthAsianLanguage.Arabic);
        result.AcceptableAlternatives.Should().Contain(SouthAsianLanguage.Urdu);
        result.CulturalAppropriatenessValidation.Should().BeTrue();
    }

    #endregion

    #region Multi-Language Routing Performance Tests

    [Fact]
    public async Task ExecuteMultiLanguageRouting_WithOptimalConditions_ShouldMeetPerformanceTargets()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var routingRequest = new MultiLanguageRoutingRequest
        {
            UserId = userId,
            ContentType = ContentType.CommunityDiscussion,
            RequestedLanguages = new List<SouthAsianLanguage> { SouthAsianLanguage.Sinhala, SouthAsianLanguage.English },
            PerformanceRequirement = PerformanceRequirement.FortuneToOSLA
        };
        
        var startTime = DateTime.UtcNow;
        
        // Act
        var result = await _routingEngine.ExecuteMultiLanguageRoutingAsync(routingRequest);
        
        var executionTime = DateTime.UtcNow - startTime;
        
        // Assert - Should fail until implemented
        result.Should().NotBeNull();
        executionTime.TotalMilliseconds.Should().BeLessThan(100); // <100ms requirement
        result.RoutingAccuracy.Should().BeGreaterThan(0.95m);
        result.CacheHitRate.Should().BeGreaterThan(0.80m);
        result.DatabaseQueriesCount.Should().BeLessThan(3);
    }

    [Fact]
    public async Task ExecuteMultiLanguageRouting_WithHighConcurrency_ShouldMaintainPerformance()
    {
        // Arrange - Simulate cultural event traffic spike (5x normal load)
        var concurrentRequests = Enumerable.Range(0, 100).Select(i => new MultiLanguageRoutingRequest
        {
            UserId = Guid.NewGuid(),
            ContentType = ContentType.CulturalEvent,
            RequestedLanguages = new List<SouthAsianLanguage> { SouthAsianLanguage.Tamil },
            PerformanceRequirement = PerformanceRequirement.CulturalEventSpike
        }).ToList();
        
        var startTime = DateTime.UtcNow;
        
        // Act
        var results = await Task.WhenAll(
            concurrentRequests.Select(request => _routingEngine.ExecuteMultiLanguageRoutingAsync(request))
        );
        
        var executionTime = DateTime.UtcNow - startTime;
        
        // Assert - Should fail until implemented
        results.Should().AllSatisfy(result => result.Should().NotBeNull());
        executionTime.TotalMilliseconds.Should().BeLessThan(500); // <500ms during spikes
        results.Should().AllSatisfy(result => result.RoutingAccuracy.Should().BeGreaterThan(0.90m));
    }

    #endregion

    #region Database Integration Tests

    [Fact]
    public async Task StoreMultiLanguageProfile_WithComplexLanguageData_ShouldPersistCorrectly()
    {
        // Arrange
        var userProfile = new MultiLanguageUserProfile
        {
            UserId = Guid.NewGuid(),
            NativeLanguages = new Dictionary<SouthAsianLanguage, decimal>
            {
                { SouthAsianLanguage.Sinhala, 0.95m },
                { SouthAsianLanguage.Tamil, 0.80m },
                { SouthAsianLanguage.English, 0.85m }
            },
            HeritageLanguages = new Dictionary<SouthAsianLanguage, decimal>
            {
                { SouthAsianLanguage.Sinhala, 0.90m },
                { SouthAsianLanguage.Pali, 0.60m }
            },
            GenerationalCohort = GenerationalCohort.FirstGeneration,
            CulturalLanguagePreferences = new Dictionary<CulturalContext, SouthAsianLanguage>
            {
                { CulturalContext.Buddhist, SouthAsianLanguage.Sinhala },
                { CulturalContext.Business, SouthAsianLanguage.English }
            }
        };
        
        // Act
        var result = await _routingEngine.StoreMultiLanguageProfileAsync(userProfile);
        
        // Assert - Should fail until implemented
        result.Should().BeTrue();
        
        var retrievedProfile = await _routingEngine.GetMultiLanguageProfileAsync(userProfile.UserId);
        retrievedProfile.Should().NotBeNull();
        retrievedProfile.NativeLanguages.Should().HaveCount(3);
        retrievedProfile.GenerationalCohort.Should().Be(GenerationalCohort.FirstGeneration);
    }

    [Fact]
    public async Task QueryLanguageRoutingData_WithPartitionedData_ShouldOptimizeQuery()
    {
        // Arrange
        var query = new LanguageRoutingQuery
        {
            Languages = new List<SouthAsianLanguage> { SouthAsianLanguage.Sinhala, SouthAsianLanguage.Tamil },
            CulturalRegions = new List<CulturalRegion> { CulturalRegion.SriLankanDiaspora },
            PerformanceMode = DatabasePerformanceMode.OptimalRouting
        };
        
        var startTime = DateTime.UtcNow;
        
        // Act
        var result = await _routingEngine.QueryLanguageRoutingDataAsync(query);
        
        var queryTime = DateTime.UtcNow - startTime;
        
        // Assert - Should fail until implemented
        result.Should().NotBeNull();
        queryTime.TotalMilliseconds.Should().BeLessThan(50); // Database partition optimization
        result.PartitionHit.Should().Be("ComplexScriptPartition");
        result.IndexUsage.Should().Contain("language_affinity_idx");
    }

    #endregion

    #region Heritage Language Preservation Tests

    [Fact]
    public async Task AnalyzeHeritageLanguagePreservation_WithCommunityData_ShouldProvideInsights()
    {
        // Arrange
        var communityId = Guid.NewGuid();
        var preservationRequest = new HeritageLanguagePreservationRequest
        {
            CommunityId = communityId,
            TargetLanguage = SouthAsianLanguage.Sinhala,
            GenerationalAnalysis = true,
            PreservationStrategies = true
        };
        
        // Act
        var result = await _routingEngine.AnalyzeHeritageLanguagePreservationAsync(preservationRequest);
        
        // Assert - Should fail until implemented
        result.Should().NotBeNull();
        result.LanguageVitality.Should().BeGreaterThan(0.0m);
        result.GenerationalDecline.Should().NotBeEmpty();
        result.PreservationRecommendations.Should().NotBeEmpty();
        result.CommunityEngagementOpportunities.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GenerateIntergenerationalContent_WithLanguageBridge_ShouldConnectGenerations()
    {
        // Arrange
        var contentRequest = new IntergenerationalContentRequest
        {
            FirstGenerationLanguage = SouthAsianLanguage.Tamil,
            YoungerGenerationLanguage = SouthAsianLanguage.English,
            ContentType = ContentType.CulturalStory,
            BridgingStrategy = LanguageBridgingStrategy.GradualTransition
        };
        
        // Act
        var result = await _routingEngine.GenerateIntergenerationalContentAsync(contentRequest);
        
        // Assert - Should fail until implemented
        result.Should().NotBeNull();
        result.BilingualContent.Should().BeTrue();
        result.LanguageLearningOpportunities.Should().NotBeEmpty();
        result.CulturalConnectionPoints.Should().NotBeEmpty();
        result.GenerationalEngagement.Should().BeGreaterThan(0.75m);
    }

    #endregion

    #region Revenue Optimization Tests

    [Fact]
    public async Task AnalyzeLanguageBasedRevenueOpportunities_WithCommunityData_ShouldIdentifyOpportunities()
    {
        // Arrange
        var revenueAnalysisRequest = new LanguageRevenueAnalysisRequest
        {
            TargetLanguages = new List<SouthAsianLanguage> { SouthAsianLanguage.Sinhala, SouthAsianLanguage.Tamil },
            RevenueStreams = new List<RevenueStream> { RevenueStream.PremiumContent, RevenueStream.CulturalEvents },
            AnalysisPeriod = TimeSpan.FromDays(90)
        };
        
        // Act
        var result = await _routingEngine.AnalyzeLanguageBasedRevenueOpportunitiesAsync(revenueAnalysisRequest);
        
        // Assert - Should fail until implemented
        result.Should().NotBeNull();
        result.EngagementIncrease.Should().BeGreaterThan(0.15m); // 15-25% target
        result.RevenueMultiplier.Should().BeGreaterThan(1.0m);
        result.NewRevenueStreams.Should().NotBeEmpty();
        result.BusinessDirectoryOptimization.Should().NotBeNull();
    }

    [Fact]
    public async Task OptimizeBusinessDirectoryLanguageMatching_WithCulturalContext_ShouldImproveConversions()
    {
        // Arrange
        var businessMatchingRequest = new BusinessLanguageMatchingRequest
        {
            UserLanguageProfile = new MultiLanguageUserProfile
            {
                UserId = Guid.NewGuid(),
                NativeLanguages = new Dictionary<SouthAsianLanguage, decimal>
                {
                    { SouthAsianLanguage.Tamil, 0.90m },
                    { SouthAsianLanguage.English, 0.75m }
                }
            },
            BusinessCategory = BusinessCategory.Restaurant,
            CulturalPreferences = new List<CulturalPreference> { CulturalPreference.AuthenticCuisine }
        };
        
        // Act
        var result = await _routingEngine.OptimizeBusinessDirectoryLanguageMatchingAsync(businessMatchingRequest);
        
        // Assert - Should fail until implemented
        result.Should().NotBeNull();
        result.LanguageMatchScore.Should().BeGreaterThan(0.85m);
        result.CulturalRelevanceScore.Should().BeGreaterThan(0.80m);
        result.ConversionProbability.Should().BeGreaterThan(0.70m);
        result.RecommendedBusinesses.Should().NotBeEmpty();
    }

    #endregion

    #region Error Handling and Fallback Tests

    [Fact]
    public async Task HandleLanguageDetectionFailure_WithFallbackStrategy_ShouldGracefullyDegrade()
    {
        // Arrange
        var problematicContent = "Mixed content with unknown scripts and corrupted data";
        var userId = Guid.NewGuid();
        
        // Act
        var result = await _routingEngine.DetectLanguagePreferencesAsync(userId, problematicContent);
        
        // Assert - Should fail until implemented
        result.Should().NotBeNull();
        result.PrimaryLanguage.Should().Be(SouthAsianLanguage.English); // Fallback
        result.LanguageConfidence.Should().BeLessThan(0.50m);
        result.FallbackStrategy.Should().Be(LanguageFallbackStrategy.DefaultToEnglish);
        result.RequiresManualReview.Should().BeTrue();
    }

    [Fact]
    public async Task HandleDatabasePartitionFailure_WithFailoverStrategy_ShouldMaintainService()
    {
        // Arrange - Simulate partition failure
        var routingRequest = new MultiLanguageRoutingRequest
        {
            UserId = Guid.NewGuid(),
            ContentType = ContentType.SacredContent,
            RequestedLanguages = new List<SouthAsianLanguage> { SouthAsianLanguage.Sinhala },
            FailoverMode = DatabaseFailoverMode.CrossRegion
        };
        
        // Act
        var result = await _routingEngine.ExecuteMultiLanguageRoutingAsync(routingRequest);
        
        // Assert - Should fail until implemented
        result.Should().NotBeNull();
        result.DatabaseFailoverUsed.Should().BeTrue();
        result.PerformanceDegradation.Should().BeLessThan(0.20m); // <20% degradation
        result.ServiceContinuity.Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    private string GetSampleContentForLanguage(SouthAsianLanguage language)
    {
        return language switch
        {
            SouthAsianLanguage.Hindi => "यह एक हिंदी भाषा का नमूना है",
            SouthAsianLanguage.Urdu => "یہ اردو زبان کا ایک نمونہ ہے",
            SouthAsianLanguage.Punjabi => "ਇਹ ਪੰਜਾਬੀ ਭਾਸ਼ਾ ਦਾ ਨਮੂਨਾ ਹੈ",
            SouthAsianLanguage.Bengali => "এটি বাংলা ভাষার একটি নমুনা",
            SouthAsianLanguage.Gujarati => "આ ગુજરાતી ભાષાનો નમૂનો છે",
            _ => "Sample content for language testing"
        };
    }

    #endregion
}

#region Test Data Models (To be moved to proper domain models)

public enum SouthAsianLanguage
{
    Sinhala,
    Tamil,
    Hindi,
    Urdu,
    Punjabi,
    Bengali,
    Gujarati,
    English,
    Arabic,
    Sanskrit,
    Pali
}

public enum GenerationalCohort
{
    FirstGeneration,
    SecondGeneration,
    ThirdGenerationPlus
}

public enum CulturalContext
{
    Buddhist,
    Hindu,
    Islamic,
    Sikh,
    Business,
    Social,
    Sacred,
    Educational
}

public enum CulturalBackground
{
    SriLankanBuddhist,
    IndianTamil,
    PakistaniMuslim,
    SikhPunjabi,
    BengaliHindu,
    GujaratiHindu
}

public class MultiLanguageUserProfile
{
    public Guid UserId { get; set; }
    public Dictionary<SouthAsianLanguage, decimal> NativeLanguages { get; set; } = new();
    public Dictionary<SouthAsianLanguage, decimal> HeritageLanguages { get; set; } = new();
    public GenerationalCohort GenerationalCohort { get; set; }
    public Dictionary<CulturalContext, SouthAsianLanguage> CulturalLanguagePreferences { get; set; } = new();
    public Dictionary<CulturalContext, SouthAsianLanguage> SacredLanguageRequirements { get; set; } = new();
    public CulturalBackground CulturalBackground { get; set; }
}

#endregion