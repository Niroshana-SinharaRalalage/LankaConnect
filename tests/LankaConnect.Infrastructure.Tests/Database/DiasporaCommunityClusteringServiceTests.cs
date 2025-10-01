using FluentAssertions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Infrastructure.Database.LoadBalancing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace LankaConnect.Infrastructure.Tests.Database;

public class DiasporaCommunityClusteringServiceTests
{
    private readonly ITestOutputHelper _output;
    private readonly Mock<ILogger<DiasporaCommunityClusteringService>> _mockLogger;
    private readonly DiasporaClusteringOptions _options;

    public DiasporaCommunityClusteringServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _mockLogger = new Mock<ILogger<DiasporaCommunityClusteringService>>();
        _options = new DiasporaClusteringOptions
        {
            MaxClustersPerRegion = 50,
            MinCommunitySize = 100,
            CacheExpirationMinutes = 15,
            PerformanceTargetMs = 200,
            AccuracyTarget = 0.94,
            EnableSpatialIndexing = true,
            EnableMultiDimensionalClustering = true
        };
    }

    [Theory]
    [InlineData("north-america-east")]
    [InlineData("europe-west")]
    [InlineData("asia-pacific")]
    [InlineData("south-america")]
    public async Task AnalyzeCommunityClusteringAsync_ValidRegion_ShouldReturnAnalysisResults(string region)
    {
        // Arrange
        var service = CreateService();
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await service.AnalyzeCommunityClusteringAsync(
            region, 
            CulturalCommunityType.SriLankanBuddhist,
            cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.GeographicRegion.Should().Be(region);
        result.Value.PrimaryCommunityType.Should().Be(CulturalCommunityType.SriLankanBuddhist);
        result.Value.CommunityPopulation.Should().BeGreaterThan(0);
        result.Value.CommunityDensity.Should().BeInRange(0.0, 1.0);
        result.Value.CulturalInstitutions.Should().NotBeEmpty();
        result.Value.LanguageSpeakers.Should().NotBeEmpty();
        result.Value.PopularCulturalEvents.Should().NotBeEmpty();
        result.Value.BusinessDirectoryDensity.Should().BeInRange(0.0, 1.0);
        result.Value.AnalysisTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        _output.WriteLine($"✓ Community clustering analysis completed for {region} with {result.Value.CommunityPopulation} population members");
    }

    [Theory]
    [InlineData(CulturalCommunityType.SriLankanBuddhist)]
    [InlineData(CulturalCommunityType.IndianHindu)]
    [InlineData(CulturalCommunityType.PakistaniMuslim)]
    [InlineData(CulturalCommunityType.BangladeshiMuslim)]
    [InlineData(CulturalCommunityType.TamilHindu)]
    [InlineData(CulturalCommunityType.SikhPunjabi)]
    [InlineData(CulturalCommunityType.GujaratiJain)]
    [InlineData(CulturalCommunityType.NepaleseBuddhist)]
    [InlineData(CulturalCommunityType.MaldivianMuslim)]
    [InlineData(CulturalCommunityType.BhutaneseBuddhist)]
    public async Task GetCommunityDensityAnalysisAsync_AllCommunityTypes_ShouldReturnDensityMetrics(CulturalCommunityType communityType)
    {
        // Arrange
        var service = CreateService();
        var region = "north-america-east";
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await service.GetCommunityDensityAnalysisAsync(region, communityType, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.GeographicRegion.Should().Be(region);
        result.Value.PrimaryCommunityType.Should().Be(communityType);
        result.Value.CommunityDensity.Should().BeInRange(0.0, 1.0);
        result.Value.CommunityPopulation.Should().BeGreaterThan(0);
        result.Value.CulturalInstitutions.Should().NotBeNull();
        result.Value.LanguageSpeakers.Should().NotBeNull();
        result.Value.BusinessDirectoryDensity.Should().BeInRange(0.0, 1.0);

        _output.WriteLine($"✓ Density analysis completed for {communityType}: {result.Value.CommunityDensity:P2} density with {result.Value.CommunityPopulation} members");
    }

    [Theory]
    [InlineData(new[] { "north-america-east", "europe-west" })]
    [InlineData(new[] { "asia-pacific", "south-america" })]
    [InlineData(new[] { "north-america-east", "europe-west", "asia-pacific" })]
    public async Task MapCommunitiesToRegionsAsync_MultipleRegions_ShouldReturnCommunityMapping(string[] regions)
    {
        // Arrange
        var service = CreateService();
        var regionList = regions.ToList();
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await service.MapCommunitiesToRegionsAsync(regionList, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().NotBeEmpty();
        result.Value.Keys.Should().NotBeEmpty("Should have at least one community type mapped");
        
        foreach (var kvp in result.Value)
        {
            kvp.Key.Should().BeOfType<CulturalCommunityType>();
            kvp.Value.Should().NotBeNull();
            kvp.Value.Should().AllBeOfType<string>();
            kvp.Value.Should().OnlyContain(region => regionList.Contains(region) || region == "default-region");
        }

        _output.WriteLine($"✓ Community mapping completed for {regions.Length} regions with {result.Value.Count} community types mapped");
    }

    [Fact]
    public async Task GenerateCommunityCrossConnectionsAsync_ValidCommunityAndRegion_ShouldReturnRecommendations()
    {
        // Arrange
        var service = CreateService();
        var sourceCommunity = CulturalCommunityType.SriLankanBuddhist;
        var region = "north-america-east";
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await service.GenerateCommunityCrossConnectionsAsync(sourceCommunity, region, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().NotBeEmpty();

        foreach (var recommendation in result.Value)
        {
            recommendation.SourceCommunity.Should().Be(sourceCommunity);
            recommendation.RecommendedCommunity.Should().NotBe(sourceCommunity);
            recommendation.SimilarityScore.Should().BeInRange(0.0, 1.0);
            recommendation.SharedCulturalElements.Should().NotBeNull();
            recommendation.CrossCulturalEvents.Should().NotBeNull();
            recommendation.RecommendationReason.Should().NotBeNullOrWhiteSpace();
            recommendation.ExpectedEngagementIncrease.Should().BeInRange(0.0, 1.0);
            recommendation.RecommendationTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        }

        _output.WriteLine($"✓ Cross-cultural connections generated: {result.Value.Count()} recommendations for {sourceCommunity}");
    }

    [Fact]
    public async Task CreateCulturalRegionProfileAsync_ValidRegionData_ShouldCreateProfile()
    {
        // Arrange
        var service = CreateService();
        var regionName = "test-cultural-region";
        var coordinates = new GeographicCoordinates 
        { 
            Latitude = 40.7128, 
            Longitude = -74.0060,
            Accuracy = 100.0
        };
        var dominantCommunities = new List<CulturalCommunityType>
        {
            CulturalCommunityType.SriLankanBuddhist,
            CulturalCommunityType.IndianHindu
        };
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await service.CreateCulturalRegionProfileAsync(regionName, coordinates, dominantCommunities, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.RegionName.Should().Be(regionName);
        result.Value.GeographicCoordinates.Should().BeEquivalentTo(coordinates);
        result.Value.DominantCommunities.Should().BeEquivalentTo(dominantCommunities);
        result.Value.RegionId.Should().NotBeNullOrWhiteSpace();
        result.Value.CommunityDensity.Should().BeGreaterThanOrEqualTo(0);
        result.Value.BusinessDirectoryCount.Should().BeGreaterThanOrEqualTo(0);
        result.Value.CulturalDiversityIndex.Should().BeInRange(0.0, 1.0);

        _output.WriteLine($"✓ Cultural region profile created for '{regionName}' with {dominantCommunities.Count} dominant communities");
    }

    [Theory]
    [InlineData(CulturalCommunityType.SriLankanBuddhist, new[] { "north-america-east", "europe-west" })]
    [InlineData(CulturalCommunityType.IndianHindu, new[] { "north-america-east", "europe-west", "asia-pacific" })]
    [InlineData(CulturalCommunityType.PakistaniMuslim, new[] { "europe-west", "asia-pacific" })]
    public async Task CalculateCommunityPopulationDistributionAsync_ValidCommunityAndRegions_ShouldReturnDistribution(
        CulturalCommunityType communityType, string[] regions)
    {
        // Arrange
        var service = CreateService();
        var regionList = regions.ToList();
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await service.CalculateCommunityPopulationDistributionAsync(communityType, regionList, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().NotBeEmpty();
        result.Value.Keys.Should().AllBeOfType<string>();
        result.Value.Values.Should().AllSatisfy(population => population.Should().BeGreaterThanOrEqualTo(0));

        var totalPopulation = result.Value.Values.Sum();
        totalPopulation.Should().BeGreaterThan(0, "Total population should be greater than 0");

        _output.WriteLine($"✓ Population distribution calculated for {communityType}: {totalPopulation:N0} total across {regions.Length} regions");
    }

    [Theory]
    [InlineData("north-america-east")]
    [InlineData("europe-west")]
    [InlineData("asia-pacific")]
    public async Task CalculateCulturalDiversityIndexAsync_ValidRegion_ShouldReturnDiversityIndex(string region)
    {
        // Arrange
        var service = CreateService();
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await service.CalculateCulturalDiversityIndexAsync(region, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeInRange(0.0, 1.0, "Cultural diversity index should be between 0 and 1");

        _output.WriteLine($"✓ Cultural diversity index for {region}: {result.Value:P2}");
    }

    [Theory]
    [InlineData(CulturalCommunityType.SriLankanBuddhist, 3)]
    [InlineData(CulturalCommunityType.IndianHindu, 5)]
    [InlineData(CulturalCommunityType.PakistaniMuslim, 2)]
    public async Task GetOptimalRegionsForCommunityExpansionAsync_ValidCommunityType_ShouldReturnRecommendedRegions(
        CulturalCommunityType communityType, int maxRegions)
    {
        // Arrange
        var service = CreateService();
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await service.GetOptimalRegionsForCommunityExpansionAsync(communityType, maxRegions, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().NotBeEmpty();
        result.Value.Count().Should().BeLessOrEqualTo(maxRegions);
        result.Value.Should().AllBeOfType<string>();
        result.Value.Should().OnlyContain(region => !string.IsNullOrWhiteSpace(region));

        _output.WriteLine($"✓ Optimal expansion regions for {communityType}: {string.Join(", ", result.Value)} (max {maxRegions})");
    }

    [Theory]
    [InlineData("north-america-east")]
    [InlineData("europe-west")]
    [InlineData("asia-pacific")]
    public async Task AnalyzeLanguageDistributionAsync_ValidRegion_ShouldReturnLanguageAnalysis(string region)
    {
        // Arrange
        var service = CreateService();
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await service.AnalyzeLanguageDistributionAsync(region, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().NotBeEmpty();
        result.Value.Keys.Should().AllBeOfType<CulturalLanguage>();
        result.Value.Values.Should().AllSatisfy(percentage => percentage.Should().BeInRange(0.0, 1.0));

        var totalPercentage = result.Value.Values.Sum();
        totalPercentage.Should().BeInRange(0.8, 1.2, "Total language distribution should be approximately 100%");

        _output.WriteLine($"✓ Language distribution for {region}: {result.Value.Count} languages analyzed");
    }

    [Theory]
    [InlineData("north-america-east", CulturalCommunityType.SriLankanBuddhist)]
    [InlineData("europe-west", CulturalCommunityType.TamilHindu)]
    [InlineData("asia-pacific", CulturalCommunityType.BangladeshiMuslim)]
    public async Task GetCulturalInstitutionsByRegionAsync_ValidRegionAndCommunity_ShouldReturnInstitutions(
        string region, CulturalCommunityType communityType)
    {
        // Arrange
        var service = CreateService();
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await service.GetCulturalInstitutionsByRegionAsync(region, communityType, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().AllBeOfType<string>();
        result.Value.Should().OnlyContain(institution => !string.IsNullOrWhiteSpace(institution));

        _output.WriteLine($"✓ Cultural institutions for {communityType} in {region}: {result.Value.Count()} institutions found");
    }

    [Theory]
    [InlineData("north-america-east", CulturalCommunityType.SriLankanBuddhist)]
    [InlineData("europe-west", CulturalCommunityType.IndianHindu)]
    [InlineData("asia-pacific", CulturalCommunityType.PakistaniMuslim)]
    public async Task GetBusinessDirectoryCountAsync_ValidRegionAndCommunity_ShouldReturnCount(
        string region, CulturalCommunityType communityType)
    {
        // Arrange
        var service = CreateService();
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await service.GetBusinessDirectoryCountAsync(region, communityType, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThanOrEqualTo(0);

        _output.WriteLine($"✓ Business directory count for {communityType} in {region}: {result.Value:N0} businesses");
    }

    [Fact]
    public async Task UpdateRegionalCulturalProfileAsync_ValidProfileUpdate_ShouldUpdateSuccessfully()
    {
        // Arrange
        var service = CreateService();
        var regionId = "test-region-update";
        var updatedProfile = new GeographicCulturalRegion
        {
            RegionId = regionId,
            RegionName = "Updated Test Region",
            GeographicCoordinates = new GeographicCoordinates { Latitude = 51.5074, Longitude = -0.1278 },
            DominantCommunities = new List<CulturalCommunityType> { CulturalCommunityType.TamilHindu },
            CommunityDensity = 1500,
            BusinessDirectoryCount = 250,
            CulturalDiversityIndex = 0.75
        };
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await service.UpdateRegionalCulturalProfileAsync(regionId, updatedProfile, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        _output.WriteLine($"✓ Regional cultural profile updated for {regionId}");
    }

    [Theory]
    [InlineData(CulturalCommunityType.SriLankanBuddhist, CulturalCommunityType.NepaleseBuddhist)]
    [InlineData(CulturalCommunityType.IndianHindu, CulturalCommunityType.TamilHindu)]
    [InlineData(CulturalCommunityType.PakistaniMuslim, CulturalCommunityType.BangladeshiMuslim)]
    public async Task CalculateCommunitySimilarityScoreAsync_RelatedCommunities_ShouldReturnHighSimilarity(
        CulturalCommunityType community1, CulturalCommunityType community2)
    {
        // Arrange
        var service = CreateService();
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await service.CalculateCommunitySimilarityScoreAsync(community1, community2, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeInRange(0.0, 1.0);

        // Related communities should have higher similarity scores
        if ((community1 == CulturalCommunityType.SriLankanBuddhist && community2 == CulturalCommunityType.NepaleseBuddhist) ||
            (community1 == CulturalCommunityType.IndianHindu && community2 == CulturalCommunityType.TamilHindu) ||
            (community1 == CulturalCommunityType.PakistaniMuslim && community2 == CulturalCommunityType.BangladeshiMuslim))
        {
            result.Value.Should().BeGreaterThan(0.6, "Related communities should have higher similarity");
        }

        _output.WriteLine($"✓ Community similarity between {community1} and {community2}: {result.Value:P2}");
    }

    [Theory]
    [InlineData("north-america-east", CulturalCommunityType.SriLankanBuddhist)]
    [InlineData("europe-west", CulturalCommunityType.IndianHindu)]
    [InlineData("asia-pacific", CulturalCommunityType.PakistaniMuslim)]
    public async Task GetPopularCulturalEventsInRegionAsync_ValidRegionAndCommunity_ShouldReturnEvents(
        string region, CulturalCommunityType communityType)
    {
        // Arrange
        var service = CreateService();
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await service.GetPopularCulturalEventsInRegionAsync(region, communityType, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().AllBeOfType<CulturalEventType>();

        _output.WriteLine($"✓ Popular cultural events for {communityType} in {region}: {result.Value.Count()} events found");
    }

    [Theory]
    [InlineData("north-america-east")]
    [InlineData("europe-west")]
    [InlineData("asia-pacific")]
    public async Task GetRegionCenterCoordinatesAsync_ValidRegion_ShouldReturnCoordinates(string region)
    {
        // Arrange
        var service = CreateService();
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await service.GetRegionCenterCoordinatesAsync(region, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Latitude.Should().BeInRange(-90, 90);
        result.Value.Longitude.Should().BeInRange(-180, 180);

        _output.WriteLine($"✓ Region center coordinates for {region}: ({result.Value.Latitude:F4}, {result.Value.Longitude:F4})");
    }

    [Fact]
    public async Task CalculateGeographicDistanceAsync_ValidCoordinates_ShouldCalculateDistance()
    {
        // Arrange
        var service = CreateService();
        var location1 = new GeographicCoordinates { Latitude = 40.7128, Longitude = -74.0060 }; // NYC
        var location2 = new GeographicCoordinates { Latitude = 51.5074, Longitude = -0.1278 };  // London
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await service.CalculateGeographicDistanceAsync(location1, location2, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeGreaterThan(0);
        result.Value.Should().BeLessThan(20000, "Distance should be reasonable for Earth coordinates");

        _output.WriteLine($"✓ Geographic distance calculated: {result.Value:F2} km");
    }

    [Fact]
    public async Task GetNearbyCommunitiesAsync_ValidCoordinatesAndRadius_ShouldReturnNearbyCommunities()
    {
        // Arrange
        var service = CreateService();
        var coordinates = new GeographicCoordinates { Latitude = 40.7128, Longitude = -74.0060 };
        var radiusKm = 100.0;
        var communityType = CulturalCommunityType.SriLankanBuddhist;
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await service.GetNearbyCommunitiesAsync(coordinates, radiusKm, communityType, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().AllBeOfType<GeographicCulturalRegion>();

        _output.WriteLine($"✓ Nearby communities found within {radiusKm} km: {result.Value.Count()} communities");
    }

    [Theory]
    [InlineData(CulturalCommunityType.SriLankanBuddhist)]
    [InlineData(CulturalCommunityType.IndianHindu)]
    [InlineData(CulturalCommunityType.PakistaniMuslim)]
    public async Task CalculateRegionalCommunityGrowthTrendsAsync_ValidCommunityType_ShouldReturnGrowthTrends(
        CulturalCommunityType communityType)
    {
        // Arrange
        var service = CreateService();
        var analysisWindow = TimeSpan.FromDays(365); // 1 year
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await service.CalculateRegionalCommunityGrowthTrendsAsync(communityType, analysisWindow, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().NotBeEmpty();
        result.Value.Keys.Should().AllBeOfType<string>();
        result.Value.Values.Should().AllSatisfy(growthRate => growthRate.Should().BeGreaterThan(-1.0));

        _output.WriteLine($"✓ Growth trends calculated for {communityType}: {result.Value.Count} regions analyzed");
    }

    [Theory]
    [InlineData("north-america-east")]
    [InlineData("europe-west")]
    [InlineData("asia-pacific")]
    public async Task ValidateCommunityClusteringDataAsync_ValidRegion_ShouldValidateData(string region)
    {
        // Arrange
        var service = CreateService();
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await service.ValidateCommunityClusteringDataAsync(region, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeOfType<bool>();

        _output.WriteLine($"✓ Data validation for {region}: {(result.Value ? "Valid" : "Invalid")}");
    }

    [Theory]
    [InlineData(CulturalCommunityType.SriLankanBuddhist, 1000)]
    [InlineData(CulturalCommunityType.IndianHindu, 2000)]
    [InlineData(CulturalCommunityType.PakistaniMuslim, 500)]
    public async Task GetRegionsWithCommunityPresenceAsync_ValidCommunityAndPopulation_ShouldReturnRegions(
        CulturalCommunityType communityType, int minimumPopulation)
    {
        // Arrange
        var service = CreateService();
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await service.GetRegionsWithCommunityPresenceAsync(communityType, minimumPopulation, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().AllBeOfType<string>();
        result.Value.Should().OnlyContain(region => !string.IsNullOrWhiteSpace(region));

        _output.WriteLine($"✓ Regions with {communityType} presence (>{minimumPopulation:N0}): {result.Value.Count()} regions");
    }

    [Fact]
    public async Task PerformanceTest_AnalyzeCommunityClusteringAsync_ShouldMeetPerformanceTargets()
    {
        // Arrange
        var service = CreateService();
        var region = "north-america-east";
        var communityType = CulturalCommunityType.SriLankanBuddhist;
        var cancellationToken = CancellationToken.None;

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var result = await service.AnalyzeCommunityClusteringAsync(region, communityType, cancellationToken);
        stopwatch.Stop();

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        stopwatch.ElapsedMilliseconds.Should().BeLessOrEqualTo(_options.PerformanceTargetMs, 
            $"Performance target of {_options.PerformanceTargetMs}ms should be met");

        _output.WriteLine($"✓ Performance test passed: {stopwatch.ElapsedMilliseconds}ms (target: {_options.PerformanceTargetMs}ms)");
    }

    [Fact]
    public async Task AccuracyTest_MultipleCommunityAnalyses_ShouldMeetAccuracyTargets()
    {
        // Arrange
        var service = CreateService();
        var testCases = new[]
        {
            (Region: "north-america-east", Community: CulturalCommunityType.SriLankanBuddhist),
            (Region: "europe-west", Community: CulturalCommunityType.TamilHindu),
            (Region: "asia-pacific", Community: CulturalCommunityType.BangladeshiMuslim)
        };
        var successfulAnalyses = 0;
        var cancellationToken = CancellationToken.None;

        // Act
        foreach (var testCase in testCases)
        {
            var result = await service.AnalyzeCommunityClusteringAsync(testCase.Region, testCase.Community, cancellationToken);
            if (result.IsSuccess && result.Value.CommunityDensity > 0)
            {
                successfulAnalyses++;
            }
        }

        var accuracyRate = (double)successfulAnalyses / testCases.Length;

        // Assert
        accuracyRate.Should().BeGreaterOrEqualTo(_options.AccuracyTarget, 
            $"Accuracy rate should meet target of {_options.AccuracyTarget:P2}");

        _output.WriteLine($"✓ Accuracy test passed: {accuracyRate:P2} (target: {_options.AccuracyTarget:P2})");
    }

    [Fact]
    public async Task ErrorHandling_InvalidRegion_ShouldReturnFailureResult()
    {
        // Arrange
        var service = CreateService();
        var invalidRegion = "";
        var communityType = CulturalCommunityType.SriLankanBuddhist;
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await service.AnalyzeCommunityClusteringAsync(invalidRegion, communityType, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNullOrWhiteSpace();

        _output.WriteLine($"✓ Error handling test passed: {result.Error}");
    }

    [Fact]
    public async Task CancellationToken_CancelledOperation_ShouldThrowOperationCancelledException()
    {
        // Arrange
        var service = CreateService();
        var region = "north-america-east";
        var communityType = CulturalCommunityType.SriLankanBuddhist;
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act & Assert
        await FluentActions.Invoking(async () =>
            await service.AnalyzeCommunityClusteringAsync(region, communityType, cancellationTokenSource.Token))
            .Should().ThrowAsync<OperationCanceledException>();

        _output.WriteLine("✓ Cancellation token test passed");
    }

    private IDiasporaCommunityClusteringService CreateService()
    {
        return new DiasporaCommunityClusteringService(
            _mockLogger.Object,
            Options.Create(_options)
        );
    }
}

public class DiasporaClusteringOptions
{
    public int MaxClustersPerRegion { get; set; } = 50;
    public int MinCommunitySize { get; set; } = 100;
    public int CacheExpirationMinutes { get; set; } = 15;
    public int PerformanceTargetMs { get; set; } = 200;
    public double AccuracyTarget { get; set; } = 0.94;
    public bool EnableSpatialIndexing { get; set; } = true;
    public bool EnableMultiDimensionalClustering { get; set; } = true;
}