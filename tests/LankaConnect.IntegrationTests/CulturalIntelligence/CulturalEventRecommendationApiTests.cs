using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LankaConnect.API.Controllers;
using LankaConnect.Application.CulturalIntelligence.Events.Queries.GetEventRecommendations;
using LankaConnect.Application.CulturalIntelligence.Events.Queries.GetCulturallyAppropriateEvents;
using LankaConnect.Application.CulturalIntelligence.Events.Queries.GetFestivalOptimizedEvents;
using LankaConnect.Application.CulturalIntelligence.Events.Queries.GetDiasporaOptimizedEvents;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace LankaConnect.IntegrationTests.CulturalIntelligence;

/// <summary>
/// Integration tests for Cultural Event Recommendation APIs
/// Tests the exposed cultural intelligence capabilities through REST endpoints
/// </summary>
public class CulturalEventRecommendationApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public CulturalEventRecommendationApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        
        // Add authentication headers for API access
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");
        _client.DefaultRequestHeaders.Add("X-API-Version", "1.0");
    }

    #region Cultural Event Recommendation API Tests

    [Fact]
    public async Task GetEventRecommendations_WithValidUserId_ShouldReturnPersonalizedRecommendations()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new
        {
            UserId = userId,
            MaxResults = 10,
            IncludeCulturalScoring = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/cultural-events/recommendations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var recommendations = JsonSerializer.Deserialize<EventRecommendationResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        recommendations.Should().NotBeNull();
        recommendations.Recommendations.Should().NotBeEmpty();
        recommendations.Recommendations.Should().HaveCountLessOrEqualTo(10);
        
        // Verify cultural intelligence scoring
        foreach (var recommendation in recommendations.Recommendations)
        {
            recommendation.CulturalScore.Should().NotBeNull();
            recommendation.CulturalScore.OverallScore.Should().BeInRange(0, 1);
            recommendation.CulturalScore.AppropriatenessScore.Should().BeInRange(0, 1);
            recommendation.CulturalScore.DiasporaFriendlinessScore.Should().BeInRange(0, 1);
        }
    }

    [Fact]
    public async Task AnalyzeCulturalAppropriateness_WithEventAndUserContext_ShouldReturnDetailedScoring()
    {
        // Arrange
        var request = new
        {
            EventId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            CulturalBackground = "Sri Lankan Buddhist",
            AnalysisDate = DateTime.UtcNow
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/cultural-events/analyze-appropriateness", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var analysis = JsonSerializer.Deserialize<CulturalAppropriatenessResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        analysis.Should().NotBeNull();
        analysis.AppropriatenessScore.Should().BeInRange(0, 1);
        analysis.ConflictLevel.Should().NotBeNull();
        analysis.Recommendations.Should().NotBeEmpty();
        analysis.CalendarValidation.Should().NotBeNull();
    }

    [Fact]
    public async Task GetFestivalOptimizedEvents_WithVesakFullMoon_ShouldReturnBuddhistEvents()
    {
        // Arrange
        var request = new
        {
            FestivalName = "Vesak",
            Year = DateTime.UtcNow.Year,
            UserId = Guid.NewGuid(),
            GeographicRegion = "NorthAmerica"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/cultural-events/festival-optimization", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var optimization = JsonSerializer.Deserialize<FestivalOptimizationResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        optimization.Should().NotBeNull();
        optimization.OptimalEvents.Should().NotBeEmpty();
        optimization.FestivalPeriod.Should().NotBeNull();
        optimization.CulturalGuidelines.Should().NotBeEmpty();
        
        // Verify Buddhist calendar integration
        optimization.FestivalPeriod.PoyadayDates.Should().NotBeEmpty();
        optimization.OptimalEvents.All(e => e.CulturalAlignment.IsAppropriateForBuddhist).Should().BeTrue();
    }

    [Fact]
    public async Task GetDiasporaTargetedEvents_WithBayAreaLocation_ShouldReturnCommunityOptimizedEvents()
    {
        // Arrange
        var request = new
        {
            UserId = Guid.NewGuid(),
            Location = new
            {
                Latitude = 37.7749,
                Longitude = -122.4194,
                City = "San Francisco",
                State = "CA"
            },
            DiasporaProfile = new
            {
                CommunityInvolvement = "High",
                PreferredLanguages = new[] { "English", "Sinhala" },
                CulturalAdaptationLevel = "Moderate"
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/cultural-events/diaspora-targeting", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var targeting = JsonSerializer.Deserialize<DiasporaTargetingResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        targeting.Should().NotBeNull();
        targeting.CommunityOptimizedEvents.Should().NotBeEmpty();
        targeting.CommunityClusterAnalysis.Should().NotBeNull();
        targeting.ProximityScore.Should().BeGreaterThan(0);
        
        // Verify diaspora community intelligence
        targeting.CommunityClusterAnalysis.SriLankanDensity.Should().BeGreaterThan(0);
        targeting.CommunityOptimizedEvents.All(e => e.DiasporaFriendlinessScore > 0.5).Should().BeTrue();
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task GetEventRecommendations_WithInvalidUserId_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new
        {
            UserId = Guid.Empty,
            MaxResults = 10
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/cultural-events/recommendations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("UserId is required");
    }

    [Fact]
    public async Task AnalyzeCulturalAppropriateness_WithMissingApiKey_ShouldReturnUnauthorized()
    {
        // Arrange
        var clientWithoutAuth = _factory.CreateClient();
        var request = new
        {
            EventId = Guid.NewGuid(),
            UserId = Guid.NewGuid()
        };

        // Act
        var response = await clientWithoutAuth.PostAsJsonAsync("/api/v1/cultural-events/analyze-appropriateness", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetFestivalOptimizedEvents_WithUnsupportedFestival_ShouldReturnNotFound()
    {
        // Arrange
        var request = new
        {
            FestivalName = "UnsupportedFestival",
            Year = 2024,
            UserId = Guid.NewGuid()
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/cultural-events/festival-optimization", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Festival not supported");
    }

    #endregion

    #region Rate Limiting Tests

    [Fact]
    public async Task MultipleApiCalls_ExceedingRateLimit_ShouldReturnTooManyRequests()
    {
        // Arrange
        var request = new
        {
            UserId = Guid.NewGuid(),
            MaxResults = 5
        };

        // Act - Make multiple rapid requests to trigger rate limiting
        var tasks = Enumerable.Range(0, 20)
            .Select(i => _client.PostAsJsonAsync("/api/v1/cultural-events/recommendations", request))
            .ToArray();

        var responses = await Task.WhenAll(tasks);

        // Assert
        var rateLimitedResponses = responses.Count(r => r.StatusCode == HttpStatusCode.TooManyRequests);
        rateLimitedResponses.Should().BeGreaterThan(0);
        
        var rateLimitedResponse = responses.First(r => r.StatusCode == HttpStatusCode.TooManyRequests);
        rateLimitedResponse.Headers.Should().ContainKey("X-RateLimit-Remaining");
        rateLimitedResponse.Headers.Should().ContainKey("X-RateLimit-Reset");
    }

    #endregion

    #region Cultural Calendar Integration Tests

    [Fact]
    public async Task GetPoyadayValidation_WithBuddhistEvent_ShouldValidateAgainstLunarCalendar()
    {
        // Arrange
        var request = new
        {
            EventDate = new DateTime(2024, 5, 23), // Vesak Poyaday 2024
            EventType = "Religious",
            Religion = "Buddhist"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/cultural-calendar/poya-days", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var validation = JsonSerializer.Deserialize<PoyadayValidationResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        validation.Should().NotBeNull();
        validation.IsPoyaday.Should().BeTrue();
        validation.PoyadayType.Should().Be("Vesak");
        validation.CulturalSignificance.Should().NotBeNull();
        validation.RecommendedEventTiming.Should().NotBeNull();
    }

    #endregion
}

#region Response DTOs

public class EventRecommendationResponse
{
    public List<EventRecommendationDto> Recommendations { get; set; } = new();
    public int TotalCount { get; set; }
    public CulturalIntelligenceMetadata Metadata { get; set; } = new();
}

public class EventRecommendationDto
{
    public Guid EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public double RecommendationScore { get; set; }
    public CulturalScoreDto CulturalScore { get; set; } = new();
    public GeographicScoreDto GeographicScore { get; set; } = new();
}

public class CulturalScoreDto
{
    public double OverallScore { get; set; }
    public double AppropriatenessScore { get; set; }
    public double DiasporaFriendlinessScore { get; set; }
    public string ConflictLevel { get; set; } = string.Empty;
}

public class CulturalAppropriatenessResponse
{
    public double AppropriatenessScore { get; set; }
    public string ConflictLevel { get; set; } = string.Empty;
    public List<string> Recommendations { get; set; } = new();
    public CalendarValidationDto CalendarValidation { get; set; } = new();
}

public class FestivalOptimizationResponse
{
    public List<OptimalEventDto> OptimalEvents { get; set; } = new();
    public FestivalPeriodDto FestivalPeriod { get; set; } = new();
    public List<string> CulturalGuidelines { get; set; } = new();
}

public class OptimalEventDto
{
    public Guid EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public CulturalAlignmentDto CulturalAlignment { get; set; } = new();
}

public class DiasporaTargetingResponse
{
    public List<CommunityOptimizedEventDto> CommunityOptimizedEvents { get; set; } = new();
    public CommunityClusterAnalysisDto CommunityClusterAnalysis { get; set; } = new();
    public double ProximityScore { get; set; }
}

public class PoyadayValidationResponse
{
    public bool IsPoyaday { get; set; }
    public string PoyadayType { get; set; } = string.Empty;
    public CulturalSignificanceDto CulturalSignificance { get; set; } = new();
    public RecommendedTimingDto RecommendedEventTiming { get; set; } = new();
}

#endregion