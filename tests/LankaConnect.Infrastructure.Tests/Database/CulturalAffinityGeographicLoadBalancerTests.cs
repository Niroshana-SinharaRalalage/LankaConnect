using FluentAssertions;
using Xunit;
using LankaConnect.Infrastructure.Database.LoadBalancing;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace LankaConnect.Infrastructure.Tests.Database;

public class CulturalAffinityGeographicLoadBalancerTests
{
    [Fact]
    public void CulturalAffinityGeographicLoadBalancer_CanBeConstructed_WithValidParameters()
    {
        // Arrange
        var logger = Substitute.For<ILogger<CulturalAffinityGeographicLoadBalancer>>();
        var options = Options.Create(new DiasporaLoadBalancingOptions());
        var shardingService = Substitute.For<ICulturalIntelligenceShardingService>();
        var clusteringService = Substitute.For<IDiasporaCommunityClusteringService>();

        // Act
        var loadBalancer = new CulturalAffinityGeographicLoadBalancer(
            logger, options, shardingService, clusteringService);

        // Assert
        loadBalancer.Should().NotBeNull();
    }

    [Fact]
    public void DiasporaLoadBalancingOptions_HasDefaultValues_WhenCreated()
    {
        // Arrange & Act
        var options = new DiasporaLoadBalancingOptions();

        // Assert
        options.EnableCulturalAffinityRouting.Should().BeTrue();
        options.GeographicProximityWeight.Should().Be(0.30);
        options.CulturalAffinityWeight.Should().Be(0.70);
        options.LanguagePreferenceWeight.Should().Be(0.15);
        options.ReligiousObservanceWeight.Should().Be(0.25);
        options.CulturalEventParticipationWeight.Should().Be(0.20);
        options.CrossCulturalDiscoveryWeight.Should().Be(0.10);
        options.MaxResponseTimeMs.Should().Be(200);
        options.CacheExpirationMinutes.Should().Be(15);
        options.EnableLoadBalancingHealthChecks.Should().BeTrue();
    }

    [Theory]
    [InlineData(DiasporaRoutingStrategy.CulturalAffinity, "CulturalAffinity")]
    [InlineData(DiasporaRoutingStrategy.GeographicProximity, "GeographicProximity")]
    [InlineData(DiasporaRoutingStrategy.Balanced, "Balanced")]
    [InlineData(DiasporaRoutingStrategy.CrossCulturalDiscovery, "CrossCulturalDiscovery")]
    [InlineData(DiasporaRoutingStrategy.PerformanceOptimized, "PerformanceOptimized")]
    public void DiasporaRoutingStrategy_EnumValues_ShouldHaveCorrectNames(
        DiasporaRoutingStrategy strategy, string expectedName)
    {
        // Act & Assert
        strategy.ToString().Should().Be(expectedName);
    }

    [Fact]
    public void DiasporaLoadBalancingRequest_DefaultProperties_ShouldBeInitialized()
    {
        // Act
        var request = new DiasporaLoadBalancingRequest();

        // Assert
        request.RequestId.Should().NotBeNull();
        request.SourceRegion.Should().NotBeNull();
        request.CulturalContext.Should().BeNull();
        request.RoutingStrategy.Should().Be(DiasporaRoutingStrategy.CulturalAffinity);
        request.RequestTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        request.MaxResponseTime.Should().Be(TimeSpan.FromMilliseconds(200));
        request.RequiredServices.Should().NotBeNull();
        request.PreferredLanguages.Should().NotBeNull();
        request.CulturalEventContext.Should().BeNull();
    }

    [Theory]
    [InlineData(CulturalCommunityType.SriLankanBuddhist, "SriLankanBuddhist")]
    [InlineData(CulturalCommunityType.IndianHindu, "IndianHindu")]
    [InlineData(CulturalCommunityType.PakistaniMuslim, "PakistaniMuslim")]
    [InlineData(CulturalCommunityType.BangladeshiMuslim, "BangladeshiMuslim")]
    [InlineData(CulturalCommunityType.SikhPunjabi, "SikhPunjabi")]
    [InlineData(CulturalCommunityType.TamilHindu, "TamilHindu")]
    [InlineData(CulturalCommunityType.GujaratiJain, "GujaratiJain")]
    public void CulturalCommunityType_EnumValues_ShouldHaveCorrectNames(
        CulturalCommunityType communityType, string expectedName)
    {
        // Act & Assert
        communityType.ToString().Should().Be(expectedName);
    }

    [Fact]
    public void CulturalAffinityScore_DefaultProperties_ShouldBeInitialized()
    {
        // Act
        var score = new CulturalAffinityScore();

        // Assert
        score.ScoreId.Should().NotBeNull();
        score.SourceCommunity.Should().Be(CulturalCommunityType.SriLankanBuddhist);
        score.TargetCommunity.Should().Be(CulturalCommunityType.SriLankanBuddhist);
        score.AffinityScore.Should().Be(0.0);
        score.ReligiousAffinityScore.Should().Be(0.0);
        score.LanguageAffinityScore.Should().Be(0.0);
        score.CulturalEventAffinityScore.Should().Be(0.0);
        score.GeographicProximityScore.Should().Be(0.0);
        score.CalculationTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("sri_lankan_buddhist", "bay_area", CulturalCommunityType.SriLankanBuddhist)]
    [InlineData("indian_hindu", "new_york", CulturalCommunityType.IndianHindu)]
    [InlineData("pakistani_muslim", "toronto", CulturalCommunityType.PakistaniMuslim)]
    [InlineData("sikh_punjabi", "vancouver", CulturalCommunityType.SikhPunjabi)]
    public void DiasporaLoadBalancingRequest_WithDifferentCombinations_ShouldBeValid(
        string communityId, string region, CulturalCommunityType expectedType)
    {
        // Act
        var request = new DiasporaLoadBalancingRequest
        {
            SourceRegion = region,
            CulturalContext = new CulturalContext
            {
                CommunityId = communityId,
                GeographicRegion = region
            },
            RoutingStrategy = DiasporaRoutingStrategy.CulturalAffinity,
            PreferredLanguages = GetLanguagesForCommunity(expectedType)
        };

        // Assert
        request.SourceRegion.Should().Be(region);
        request.CulturalContext.Should().NotBeNull();
        request.CulturalContext.CommunityId.Should().Be(communityId);
        request.CulturalContext.GeographicRegion.Should().Be(region);
        request.RoutingStrategy.Should().Be(DiasporaRoutingStrategy.CulturalAffinity);
        request.PreferredLanguages.Should().NotBeEmpty();
    }

    [Fact]
    public async Task RouteToOptimalCulturalRegionAsync_ShouldReturnResult_WhenCalled()
    {
        // Arrange
        var logger = Substitute.For<ILogger<CulturalAffinityGeographicLoadBalancer>>();
        var options = Options.Create(new DiasporaLoadBalancingOptions());
        var shardingService = Substitute.For<ICulturalIntelligenceShardingService>();
        var clusteringService = Substitute.For<IDiasporaCommunityClusteringService>();
        var loadBalancer = new CulturalAffinityGeographicLoadBalancer(
            logger, options, shardingService, clusteringService);

        var request = new DiasporaLoadBalancingRequest
        {
            SourceRegion = "north_america",
            CulturalContext = new CulturalContext
            {
                CommunityId = "sri_lankan_buddhist",
                GeographicRegion = "bay_area"
            },
            RoutingStrategy = DiasporaRoutingStrategy.CulturalAffinity
        };

        // Act
        var result = await loadBalancer.RouteToOptimalCulturalRegionAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task CalculateCulturalAffinityScoreAsync_ShouldReturnScore_WhenCalled()
    {
        // Arrange
        var logger = Substitute.For<ILogger<CulturalAffinityGeographicLoadBalancer>>();
        var options = Options.Create(new DiasporaLoadBalancingOptions());
        var shardingService = Substitute.For<ICulturalIntelligenceShardingService>();
        var clusteringService = Substitute.For<IDiasporaCommunityClusteringService>();
        var loadBalancer = new CulturalAffinityGeographicLoadBalancer(
            logger, options, shardingService, clusteringService);

        // Act
        var result = await loadBalancer.CalculateCulturalAffinityScoreAsync(
            CulturalCommunityType.SriLankanBuddhist,
            CulturalCommunityType.TamilHindu,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void DiasporaLoadBalancingResult_DefaultProperties_ShouldBeInitialized()
    {
        // Act
        var result = new DiasporaLoadBalancingResult();

        // Assert
        result.ResultId.Should().NotBeNull();
        result.OptimalRegion.Should().NotBeNull();
        result.RoutingReason.Should().NotBeNull();
        result.CulturalAffinityScore.Should().Be(0.0);
        result.GeographicProximityScore.Should().Be(0.0);
        result.ResponseTime.Should().Be(TimeSpan.Zero);
        result.AlternativeRegions.Should().NotBeNull();
        result.LoadBalancingMetrics.Should().NotBeNull();
        result.RoutingTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(LoadBalancingHealthStatus.Healthy, "Healthy")]
    [InlineData(LoadBalancingHealthStatus.Degraded, "Degraded")]
    [InlineData(LoadBalancingHealthStatus.Overloaded, "Overloaded")]
    [InlineData(LoadBalancingHealthStatus.Failed, "Failed")]
    [InlineData(LoadBalancingHealthStatus.Maintenance, "Maintenance")]
    public void LoadBalancingHealthStatus_EnumValues_ShouldHaveCorrectNames(
        LoadBalancingHealthStatus status, string expectedName)
    {
        // Act & Assert
        status.ToString().Should().Be(expectedName);
    }

    [Fact]
    public void GeographicCulturalRegion_DefaultProperties_ShouldBeValid()
    {
        // Act
        var region = new GeographicCulturalRegion
        {
            RegionId = "bay_area_buddhist",
            RegionName = "San Francisco Bay Area Buddhist Community",
            GeographicCoordinates = new GeographicCoordinates { Latitude = 37.7749, Longitude = -122.4194 },
            DominantCommunities = new List<CulturalCommunityType> 
            { 
                CulturalCommunityType.SriLankanBuddhist, 
                CulturalCommunityType.TamilHindu 
            },
            CommunityDensity = 1200,
            BusinessDirectoryCount = 45,
            CulturalInstitutions = new List<string> { "Buddhist Temple of Bay Area", "Sri Lankan Cultural Center" }
        };

        // Assert
        region.RegionId.Should().Be("bay_area_buddhist");
        region.RegionName.Should().Be("San Francisco Bay Area Buddhist Community");
        region.GeographicCoordinates.Should().NotBeNull();
        region.GeographicCoordinates.Latitude.Should().Be(37.7749);
        region.GeographicCoordinates.Longitude.Should().Be(-122.4194);
        region.DominantCommunities.Should().Contain(CulturalCommunityType.SriLankanBuddhist);
        region.DominantCommunities.Should().Contain(CulturalCommunityType.TamilHindu);
        region.CommunityDensity.Should().Be(1200);
        region.BusinessDirectoryCount.Should().Be(45);
        region.CulturalInstitutions.Should().Contain("Buddhist Temple of Bay Area");
    }

    [Fact]
    public void GeographicCoordinates_DefaultProperties_ShouldBeInitialized()
    {
        // Act
        var coordinates = new GeographicCoordinates();

        // Assert
        coordinates.Latitude.Should().Be(0.0);
        coordinates.Longitude.Should().Be(0.0);
        coordinates.Altitude.Should().Be(0.0);
        coordinates.Accuracy.Should().Be(0.0);
    }

    [Fact]
    public async Task GetDiasporaLoadBalancingHealthAsync_ShouldReturnHealth_WhenCalled()
    {
        // Arrange
        var logger = Substitute.For<ILogger<CulturalAffinityGeographicLoadBalancer>>();
        var options = Options.Create(new DiasporaLoadBalancingOptions());
        var shardingService = Substitute.For<ICulturalIntelligenceShardingService>();
        var clusteringService = Substitute.For<IDiasporaCommunityClusteringService>();
        var loadBalancer = new CulturalAffinityGeographicLoadBalancer(
            logger, options, shardingService, clusteringService);

        var regions = new List<string> { "north_america", "europe", "asia_pacific" };

        // Act
        var result = await loadBalancer.GetDiasporaLoadBalancingHealthAsync(regions, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void DiasporaLoadBalancingHealth_DefaultProperties_ShouldBeInitialized()
    {
        // Act
        var health = new DiasporaLoadBalancingHealth();

        // Assert
        health.HealthId.Should().NotBeNull();
        health.OverallHealthStatus.Should().Be(LoadBalancingHealthStatus.Healthy);
        health.RegionalHealthStatuses.Should().NotBeNull();
        health.AvgResponseTime.Should().Be(TimeSpan.Zero);
        health.CulturalAffinityAccuracy.Should().Be(0.0);
        health.HealthCheckTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        health.ActiveCulturalEvents.Should().NotBeNull();
        health.LoadBalancingEfficiency.Should().Be(0.0);
    }

    [Theory]
    [InlineData(CulturalLanguage.Sinhala, "Sinhala")]
    [InlineData(CulturalLanguage.Tamil, "Tamil")]
    [InlineData(CulturalLanguage.Hindi, "Hindi")]
    [InlineData(CulturalLanguage.Urdu, "Urdu")]
    [InlineData(CulturalLanguage.Punjabi, "Punjabi")]
    [InlineData(CulturalLanguage.Bengali, "Bengali")]
    [InlineData(CulturalLanguage.Gujarati, "Gujarati")]
    public void CulturalLanguage_EnumValues_ShouldHaveCorrectNames(
        CulturalLanguage language, string expectedName)
    {
        // Act & Assert
        language.ToString().Should().Be(expectedName);
    }

    [Fact]
    public void CulturalEventLoadContext_DefaultProperties_ShouldBeValid()
    {
        // Act
        var context = new CulturalEventLoadContext
        {
            EventType = CulturalEventType.Vesak,
            EventDate = DateTime.UtcNow.AddDays(7),
            ExpectedAttendance = 2500,
            CulturalSignificance = CulturalSignificance.Sacred,
            AffectedCommunities = new List<CulturalCommunityType> 
            { 
                CulturalCommunityType.SriLankanBuddhist, 
                CulturalCommunityType.TamilHindu 
            },
            TrafficMultiplier = 5.0,
            GeographicRegions = new List<string> { "bay_area", "los_angeles", "new_york" }
        };

        // Assert
        context.EventType.Should().Be(CulturalEventType.Vesak);
        context.EventDate.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromHours(1));
        context.ExpectedAttendance.Should().Be(2500);
        context.CulturalSignificance.Should().Be(CulturalSignificance.Sacred);
        context.AffectedCommunities.Should().Contain(CulturalCommunityType.SriLankanBuddhist);
        context.TrafficMultiplier.Should().Be(5.0);
        context.GeographicRegions.Should().Contain("bay_area");
    }

    [Fact]
    public async Task OptimizeForCulturalEventAsync_ShouldReturnOptimization_WhenCalled()
    {
        // Arrange
        var logger = Substitute.For<ILogger<CulturalAffinityGeographicLoadBalancer>>();
        var options = Options.Create(new DiasporaLoadBalancingOptions());
        var shardingService = Substitute.For<ICulturalIntelligenceShardingService>();
        var clusteringService = Substitute.For<IDiasporaCommunityClusteringService>();
        var loadBalancer = new CulturalAffinityGeographicLoadBalancer(
            logger, options, shardingService, clusteringService);

        var eventContext = new CulturalEventLoadContext
        {
            EventType = CulturalEventType.Diwali,
            EventDate = DateTime.UtcNow.AddDays(14),
            ExpectedAttendance = 5000,
            CulturalSignificance = CulturalSignificance.Critical,
            TrafficMultiplier = 4.5
        };

        // Act
        var result = await loadBalancer.OptimizeForCulturalEventAsync(eventContext, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void DiasporaLoadBalancingMetrics_DefaultProperties_ShouldBeInitialized()
    {
        // Act
        var metrics = new DiasporaLoadBalancingMetrics();

        // Assert
        metrics.MetricsId.Should().NotBeNull();
        metrics.TotalRoutingDecisions.Should().Be(0);
        metrics.SuccessfulRoutings.Should().Be(0);
        metrics.AverageResponseTime.Should().Be(TimeSpan.Zero);
        metrics.CulturalAffinityAccuracy.Should().Be(0.0);
        metrics.CrossCulturalDiscoveryRate.Should().Be(0.0);
        metrics.CulturalEventOptimizationSuccess.Should().Be(0.0);
        metrics.MetricsCollectionTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        metrics.RegionalPerformanceMetrics.Should().NotBeNull();
    }

    [Fact]
    public async Task GetDiasporaLoadBalancingMetricsAsync_ShouldReturnMetrics_WhenCalled()
    {
        // Arrange
        var logger = Substitute.For<ILogger<CulturalAffinityGeographicLoadBalancer>>();
        var options = Options.Create(new DiasporaLoadBalancingOptions());
        var shardingService = Substitute.For<ICulturalIntelligenceShardingService>();
        var clusteringService = Substitute.For<IDiasporaCommunityClusteringService>();
        var loadBalancer = new CulturalAffinityGeographicLoadBalancer(
            logger, options, shardingService, clusteringService);

        // Act
        var result = await loadBalancer.GetDiasporaLoadBalancingMetricsAsync(
            TimeSpan.FromDays(7), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Theory]
    [InlineData(CulturalEventType.Vesak, 5.0, CulturalCommunityType.SriLankanBuddhist)]
    [InlineData(CulturalEventType.Diwali, 4.5, CulturalCommunityType.IndianHindu)]
    [InlineData(CulturalEventType.Eid, 4.0, CulturalCommunityType.PakistaniMuslim)]
    [InlineData(CulturalEventType.Vaisakhi, 3.0, CulturalCommunityType.SikhPunjabi)]
    public void CulturalEventLoadContext_WithDifferentEvents_ShouldHaveCorrectMultipliers(
        CulturalEventType eventType, double expectedMultiplier, CulturalCommunityType primaryCommunity)
    {
        // Act
        var context = new CulturalEventLoadContext
        {
            EventType = eventType,
            TrafficMultiplier = expectedMultiplier,
            AffectedCommunities = new List<CulturalCommunityType> { primaryCommunity }
        };

        // Assert
        context.EventType.Should().Be(eventType);
        context.TrafficMultiplier.Should().Be(expectedMultiplier);
        context.AffectedCommunities.Should().Contain(primaryCommunity);
    }

    private static List<CulturalLanguage> GetLanguagesForCommunity(CulturalCommunityType communityType)
    {
        return communityType switch
        {
            CulturalCommunityType.SriLankanBuddhist => new List<CulturalLanguage> { CulturalLanguage.Sinhala },
            CulturalCommunityType.TamilHindu => new List<CulturalLanguage> { CulturalLanguage.Tamil },
            CulturalCommunityType.IndianHindu => new List<CulturalLanguage> { CulturalLanguage.Hindi, CulturalLanguage.Gujarati },
            CulturalCommunityType.PakistaniMuslim => new List<CulturalLanguage> { CulturalLanguage.Urdu },
            CulturalCommunityType.SikhPunjabi => new List<CulturalLanguage> { CulturalLanguage.Punjabi },
            CulturalCommunityType.BangladeshiMuslim => new List<CulturalLanguage> { CulturalLanguage.Bengali },
            _ => new List<CulturalLanguage> { CulturalLanguage.Hindi }
        };
    }
}