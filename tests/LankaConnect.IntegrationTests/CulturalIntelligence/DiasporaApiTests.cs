using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LankaConnect.Application.CulturalIntelligence.Diaspora.Queries.AnalyzeCommunityCluster;
using LankaConnect.Application.CulturalIntelligence.Diaspora.Queries.GetCulturalPreferences;
using LankaConnect.Application.CulturalIntelligence.Diaspora.Queries.OptimizeGeographic;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LankaConnect.IntegrationTests.CulturalIntelligence;

/// <summary>
/// Integration tests for Diaspora Community Intelligence APIs
/// Tests sophisticated geographic community analysis and cultural targeting optimization
/// </summary>
public class DiasporaApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public DiasporaApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        
        // Add authentication headers for API access
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");
        _client.DefaultRequestHeaders.Add("X-API-Version", "1.0");
    }

    #region Community Clustering Analysis Tests

    [Theory]
    [InlineData(37.7749, -122.4194, "San Francisco Bay Area")] // Bay Area
    [InlineData(43.7184, -79.3776, "Toronto GTA")] // Toronto
    [InlineData(-37.8136, 144.9631, "Melbourne")] // Melbourne
    [InlineData(51.5074, -0.1278, "London")] // London
    public async Task AnalyzeCommunityCluster_WithMajorDiasporaLocations_ShouldReturnAccurateCommunityData(
        double latitude, double longitude, string expectedRegion)
    {
        // Arrange
        var request = new
        {
            CenterPoint = new
            {
                Latitude = latitude,
                Longitude = longitude
            },
            AnalysisRadius = 50, // km
            ClusteringParameters = new
            {
                MinCommunitySize = 100,
                IncludeDemographics = true,
                IncludeCulturalMetrics = true,
                AnalyzeGenerationalDifferences = true
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/diaspora/community-clustering", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var analysis = JsonSerializer.Deserialize<CommunityClusterAnalysisResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        analysis.Should().NotBeNull();
        analysis.CommunityCluster.Should().NotBeNull();
        analysis.CommunityCluster.RegionName.Should().Contain(expectedRegion.Split(' ')[0]); // Match first word
        analysis.CommunityCluster.SriLankanPopulation.Should().BeGreaterThan(0);
        analysis.Demographics.Should().NotBeNull();
        
        // Verify cultural metrics
        analysis.CulturalMetrics.Should().NotBeNull();
        analysis.CulturalMetrics.LanguageRetention.Should().NotBeNull();
        analysis.CulturalMetrics.TraditionalObservance.Should().BeInRange(0, 1);
        analysis.CulturalMetrics.CommunityEngagement.Should().BeInRange(0, 1);
        
        // Verify generational analysis
        analysis.GenerationalBreakdown.Should().NotBeEmpty();
        analysis.GenerationalBreakdown.Should().ContainKey("FirstGeneration");
        analysis.GenerationalBreakdown.Should().ContainKey("SecondGeneration");
    }

    [Fact]
    public async Task GetCulturalPreferences_WithDiasporaProfile_ShouldReturnDetailedPreferenceAnalysis()
    {
        // Arrange
        var request = new
        {
            DiasporaProfile = new
            {
                Location = new
                {
                    City = "Toronto",
                    Country = "Canada",
                    Coordinates = new { Latitude = 43.7184, Longitude = -79.3776 }
                },
                Demographics = new
                {
                    Generation = "SecondGeneration",
                    AgeRange = "25-35",
                    EducationLevel = "University",
                    IncomeLevel = "Middle"
                },
                CulturalBackground = new
                {
                    Ethnicity = "Sri Lankan Tamil",
                    Religion = "Hindu",
                    PrimaryLanguage = "English",
                    SecondaryLanguages = new[] { "Tamil", "Sinhala" }
                }
            },
            AnalysisScope = new
            {
                IncludeEventPreferences = true,
                IncludeCommunicationPreferences = true,
                IncludeCulturalRetention = true,
                IncludeIntegrationPatterns = true
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/diaspora/cultural-preferences", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var preferences = JsonSerializer.Deserialize<CulturalPreferencesResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        preferences.Should().NotBeNull();
        preferences.EventPreferences.Should().NotBeNull();
        preferences.CommunicationPreferences.Should().NotBeNull();
        preferences.CulturalRetention.Should().NotBeNull();
        
        // Verify Tamil Hindu specific preferences
        preferences.EventPreferences.ReligiousEventImportance.Should().BeGreaterThan(0.5);
        preferences.EventPreferences.PreferredFestivals.Should().Contain(f => f.Contains("Thaipusam") || f.Contains("Diwali"));
        
        // Verify second-generation patterns
        preferences.IntegrationPatterns.Should().NotBeNull();
        preferences.IntegrationPatterns.BilingualPreference.Should().BeTrue();
        preferences.IntegrationPatterns.ModernTraditionalBalance.Should().BeInRange(0.4, 0.8); // Balanced for 2nd gen
    }

    [Fact]
    public async Task OptimizeGeographicTargeting_WithMultipleCommunities_ShouldReturnOptimalStrategy()
    {
        // Arrange
        var request = new
        {
            TargetCommunities = new[]
            {
                new
                {
                    Location = "San Francisco Bay Area",
                    CommunityProfile = new
                    {
                        Size = 15000,
                        PredominantGeneration = "FirstGeneration",
                        PrimaryEthnicity = "Sri Lankan Sinhala",
                        PrimaryReligion = "Buddhist"
                    }
                },
                new
                {
                    Location = "Toronto GTA",
                    CommunityProfile = new
                    {
                        Size = 25000,
                        PredominantGeneration = "SecondGeneration",
                        PrimaryEthnicity = "Sri Lankan Tamil",
                        PrimaryReligion = "Hindu"
                    }
                }
            },
            OptimizationCriteria = new
            {
                PrioritizeEngagement = true,
                ConsiderCulturalSensitivity = true,
                OptimizeForReach = true,
                BalanceGenerations = true
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/diaspora/geographic-optimization", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var optimization = JsonSerializer.Deserialize<GeographicOptimizationResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        optimization.Should().NotBeNull();
        optimization.OptimalStrategy.Should().NotBeNull();
        optimization.CommunitySpecificTactics.Should().HaveCount(2);
        
        // Verify differentiated strategies
        var bayAreaTactic = optimization.CommunitySpecificTactics
            .FirstOrDefault(t => t.Location.Contains("San Francisco"));
        bayAreaTactic.Should().NotBeNull();
        bayAreaTactic.CulturalApproach.Should().Contain("Buddhist");
        bayAreaTactic.LanguageStrategy.Should().Contain("Sinhala");
        
        var torontoTactic = optimization.CommunitySpecificTactics
            .FirstOrDefault(t => t.Location.Contains("Toronto"));
        torontoTactic.Should().NotBeNull();
        torontoTactic.CulturalApproach.Should().Contain("Hindu");
        torontoTactic.LanguageStrategy.Should().Contain("Tamil");
        
        // Verify optimization metrics
        optimization.ExpectedReach.Should().BeGreaterThan(0);
        optimization.CulturalRelevanceScore.Should().BeInRange(0.7, 1.0);
    }

    #endregion

    #region Diaspora Intelligence Analytics Tests

    [Fact]
    public async Task GetDiasporaAnalytics_WithGlobalScope_ShouldReturnComprehensiveInsights()
    {
        // Arrange
        var request = new
        {
            AnalysisScope = "Global",
            MetricsToInclude = new[]
            {
                "PopulationDistribution",
                "CulturalRetention",
                "CommunityEngagement",
                "LanguagePatterns",
                "GenerationalTrends"
            },
            TimeRange = new
            {
                StartDate = DateTime.UtcNow.AddYears(-5),
                EndDate = DateTime.UtcNow
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/diaspora/analytics", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var analytics = JsonSerializer.Deserialize<DiasporaAnalyticsResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        analytics.Should().NotBeNull();
        analytics.PopulationDistribution.Should().NotBeEmpty();
        analytics.CulturalRetentionTrends.Should().NotBeNull();
        analytics.LanguagePatterns.Should().NotBeNull();
        
        // Verify major diaspora regions
        analytics.PopulationDistribution.Should().ContainKey("United States");
        analytics.PopulationDistribution.Should().ContainKey("Canada");
        analytics.PopulationDistribution.Should().ContainKey("Australia");
        analytics.PopulationDistribution.Should().ContainKey("United Kingdom");
        
        // Verify cultural insights
        analytics.CulturalRetentionTrends.BuddhistObservance.Should().BeGreaterThan(0);
        analytics.CulturalRetentionTrends.HinduObservance.Should().BeGreaterThan(0);
        analytics.CulturalRetentionTrends.TraditionalFoodPreferences.Should().BeInRange(0, 1);
    }

    [Fact]
    public async Task PredictCommunityGrowth_WithDemographicTrends_ShouldReturnGrowthProjections()
    {
        // Arrange
        var request = new
        {
            Location = "Melbourne, Australia",
            CurrentDemographics = new
            {
                TotalSriLankanPopulation = 12000,
                AverageAge = 32,
                BirthRate = 1.8,
                ImmigrationRate = 200 // per year
            },
            ProjectionParameters = new
            {
                ProjectionYears = 10,
                IncludeEconomicFactors = true,
                IncludeImmigrationPolicy = true,
                IncludeGenerationalShift = true
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/diaspora/growth-prediction", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var prediction = JsonSerializer.Deserialize<GrowthPredictionResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        prediction.Should().NotBeNull();
        prediction.PopulationProjections.Should().NotBeEmpty();
        prediction.CulturalEvolutionPredictions.Should().NotBeNull();
        prediction.ConfidenceLevel.Should().BeInRange(0.5, 1.0);
        
        // Verify 10-year projection
        var finalProjection = prediction.PopulationProjections.LastOrDefault();
        finalProjection.Should().NotBeNull();
        finalProjection.Year.Should().Be(DateTime.UtcNow.Year + 10);
        finalProjection.ProjectedPopulation.Should().BeGreaterThan(12000); // Growth expected
    }

    #endregion

    #region Cultural Integration Analysis Tests

    [Fact]
    public async Task AnalyzeCulturalIntegration_WithMultiGenerationalData_ShouldReturnIntegrationPatterns()
    {
        // Arrange
        var request = new
        {
            CommunityData = new
            {
                Location = "London, UK",
                Generations = new[]
                {
                    new
                    {
                        GenerationType = "FirstGeneration",
                        Population = 5000,
                        AverageArrivalYear = 1985,
                        LanguageProficiency = new
                        {
                            English = 0.7,
                            NativeLanguage = 0.95
                        },
                        CulturalRetention = 0.9
                    },
                    new
                    {
                        GenerationType = "SecondGeneration",
                        Population = 8000,
                        BornInHost = true,
                        LanguageProficiency = new
                        {
                            English = 1.0,
                            NativeLanguage = 0.6
                        },
                        CulturalRetention = 0.65
                    },
                    new
                    {
                        GenerationType = "ThirdGeneration",
                        Population = 3000,
                        BornInHost = true,
                        LanguageProficiency = new
                        {
                            English = 1.0,
                            NativeLanguage = 0.3
                        },
                        CulturalRetention = 0.4
                    }
                }
            },
            AnalysisDepth = "Comprehensive"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/diaspora/integration-analysis", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var analysis = JsonSerializer.Deserialize<IntegrationAnalysisResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        analysis.Should().NotBeNull();
        analysis.IntegrationPatterns.Should().NotBeNull();
        analysis.LanguageShiftAnalysis.Should().NotBeNull();
        analysis.CulturalRetentionAnalysis.Should().NotBeNull();
        
        // Verify generational trends
        analysis.IntegrationPatterns.GenerationalProgression.Should().NotBeEmpty();
        analysis.IntegrationPatterns.GenerationalProgression.Should().HaveCount(3);
        
        // Verify language shift pattern
        var firstGen = analysis.LanguageShiftAnalysis.GenerationalData
            .FirstOrDefault(g => g.Generation == "FirstGeneration");
        firstGen.Should().NotBeNull();
        firstGen.NativeLanguageRetention.Should().BeGreaterThan(0.8);
        
        var thirdGen = analysis.LanguageShiftAnalysis.GenerationalData
            .FirstOrDefault(g => g.Generation == "ThirdGeneration");
        thirdGen.Should().NotBeNull();
        thirdGen.NativeLanguageRetention.Should().BeLessThan(0.5);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task AnalyzeCommunityCluster_WithInvalidCoordinates_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new
        {
            CenterPoint = new
            {
                Latitude = 200.0, // Invalid latitude
                Longitude = -122.4194
            },
            AnalysisRadius = 50
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/diaspora/community-clustering", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid coordinates");
    }

    [Fact]
    public async Task GetCulturalPreferences_WithInsufficientData_ShouldReturnPartialResults()
    {
        // Arrange
        var request = new
        {
            DiasporaProfile = new
            {
                Location = new
                {
                    City = "Small Town",
                    Country = "Unknown"
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/diaspora/cultural-preferences", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var preferences = JsonSerializer.Deserialize<CulturalPreferencesResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        preferences.Should().NotBeNull();
        preferences.DataQuality.Should().Be("Limited");
        preferences.Warnings.Should().Contain("Insufficient community data for location");
    }

    #endregion
}

#region Response DTOs

public class CommunityClusterAnalysisResponse
{
    public CommunityClusterDto CommunityCluster { get; set; } = new();
    public DemographicsDto Demographics { get; set; } = new();
    public CulturalMetricsDto CulturalMetrics { get; set; } = new();
    public Dictionary<string, int> GenerationalBreakdown { get; set; } = new();
}

public class CommunityClusterDto
{
    public string RegionName { get; set; } = string.Empty;
    public int SriLankanPopulation { get; set; }
    public double CommunityDensity { get; set; }
    public List<string> MajorNeighborhoods { get; set; } = new();
}

public class CulturalMetricsDto
{
    public LanguageRetentionDto LanguageRetention { get; set; } = new();
    public double TraditionalObservance { get; set; }
    public double CommunityEngagement { get; set; }
}

public class CulturalPreferencesResponse
{
    public EventPreferencesDto EventPreferences { get; set; } = new();
    public CommunicationPreferencesDto CommunicationPreferences { get; set; } = new();
    public CulturalRetentionDto CulturalRetention { get; set; } = new();
    public IntegrationPatternsDto IntegrationPatterns { get; set; } = new();
    public string DataQuality { get; set; } = string.Empty;
    public List<string> Warnings { get; set; } = new();
}

public class GeographicOptimizationResponse
{
    public OptimalStrategyDto OptimalStrategy { get; set; } = new();
    public List<CommunityTacticDto> CommunitySpecificTactics { get; set; } = new();
    public int ExpectedReach { get; set; }
    public double CulturalRelevanceScore { get; set; }
}

public class DiasporaAnalyticsResponse
{
    public Dictionary<string, int> PopulationDistribution { get; set; } = new();
    public CulturalRetentionTrendsDto CulturalRetentionTrends { get; set; } = new();
    public LanguagePatternsDto LanguagePatterns { get; set; } = new();
}

public class GrowthPredictionResponse
{
    public List<PopulationProjectionDto> PopulationProjections { get; set; } = new();
    public CulturalEvolutionDto CulturalEvolutionPredictions { get; set; } = new();
    public double ConfidenceLevel { get; set; }
}

public class IntegrationAnalysisResponse
{
    public IntegrationPatternsDetailDto IntegrationPatterns { get; set; } = new();
    public LanguageShiftAnalysisDto LanguageShiftAnalysis { get; set; } = new();
    public CulturalRetentionAnalysisDto CulturalRetentionAnalysis { get; set; } = new();
}

#endregion