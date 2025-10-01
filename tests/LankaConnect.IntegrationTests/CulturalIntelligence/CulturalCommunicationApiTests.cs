using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LankaConnect.Application.CulturalIntelligence.Communications.Queries.OptimizeEmailTiming;
using LankaConnect.Application.CulturalIntelligence.Communications.Queries.GetMultiLanguageOptimization;
using LankaConnect.Application.CulturalIntelligence.Communications.Queries.GetCulturalEmailContext;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LankaConnect.IntegrationTests.CulturalIntelligence;

/// <summary>
/// Integration tests for Cultural Communication APIs
/// Tests sophisticated cultural email optimization and multi-language capabilities
/// </summary>
public class CulturalCommunicationApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public CulturalCommunicationApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        
        // Add authentication headers for API access
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");
        _client.DefaultRequestHeaders.Add("X-API-Version", "1.0");
    }

    #region Cultural Communication Timing API Tests

    [Fact]
    public async Task OptimizeEmailTiming_WithBuddhistRecipient_ShouldAvoidPoyadayConflicts()
    {
        // Arrange
        var request = new
        {
            RecipientId = Guid.NewGuid(),
            EmailType = "EventNotification",
            CulturalBackground = "Sri Lankan Buddhist",
            PreferredTiming = new
            {
                TimeZone = "America/Los_Angeles",
                PreferredDays = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" },
                AvoidReligiousDays = true
            },
            SchedulingWindow = new
            {
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(30)
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/cultural-communications/optimize-timing", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var optimization = JsonSerializer.Deserialize<EmailTimingOptimizationResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        optimization.Should().NotBeNull();
        optimization.OptimalSendTimes.Should().NotBeEmpty();
        optimization.CulturalConsiderations.Should().NotBeEmpty();
        optimization.PoyadayConflicts.Should().NotBeNull();
        
        // Verify Poyaday avoidance
        foreach (var sendTime in optimization.OptimalSendTimes)
        {
            sendTime.ConflictsWithReligiousDay.Should().BeFalse();
        }
    }

    [Fact]
    public async Task GetMultiLanguageOptimization_WithSinhalaPreference_ShouldReturnOptimizedTemplates()
    {
        // Arrange
        var request = new
        {
            RecipientId = Guid.NewGuid(),
            EmailType = "WelcomeEmail",
            LanguagePreferences = new[] { "Sinhala", "English" },
            CulturalContext = new
            {
                Region = "Colombo",
                CommunityType = "Urban",
                EducationLevel = "University"
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/cultural-communications/multi-language", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var optimization = JsonSerializer.Deserialize<MultiLanguageOptimizationResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        optimization.Should().NotBeNull();
        optimization.RecommendedTemplate.Should().NotBeNull();
        optimization.PrimaryLanguage.Should().Be("Sinhala");
        optimization.FallbackLanguage.Should().Be("English");
        optimization.CulturalAdaptations.Should().NotBeEmpty();
        
        // Verify Sinhala localization quality
        optimization.LanguageQualityScore.Should().BeGreaterThan(0.8);
        optimization.CulturalRelevanceScore.Should().BeGreaterThan(0.7);
    }

    [Fact]
    public async Task GetCulturalEmailContext_WithTamilCommunity_ShouldProvideContextualGuidance()
    {
        // Arrange
        var request = new
        {
            RecipientProfile = new
            {
                UserId = Guid.NewGuid(),
                CulturalBackground = "Sri Lankan Tamil",
                Location = "Toronto, Canada",
                CommunityInvolvement = "High",
                ReligiousAffiliation = "Hindu"
            },
            CommunicationType = "BusinessInvitation",
            EventContext = new
            {
                EventType = "Cultural",
                DateTime = DateTime.UtcNow.AddDays(14),
                IsFamilyFriendly = true
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/cultural-communications/cultural-context", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var context = JsonSerializer.Deserialize<CulturalEmailContextResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        context.Should().NotBeNull();
        context.CulturalAdaptations.Should().NotBeEmpty();
        context.LanguageSuggestions.Should().Contain("Tamil");
        context.ReligiousConsiderations.Should().NotBeEmpty();
        context.FamilyContextAdaptations.Should().NotBeEmpty();
        
        // Verify Tamil Hindu cultural understanding
        context.HinduFestivalAwareness.Should().BeTrue();
        context.FamilyOrientedMessaging.Should().BeTrue();
    }

    #endregion

    #region Cultural Template Selection API Tests

    [Fact]
    public async Task SelectCulturalTemplate_WithDiasporaContext_ShouldReturnAdaptedTemplate()
    {
        // Arrange
        var request = new
        {
            TemplateType = "EventReminder",
            RecipientProfile = new
            {
                CulturalBackground = "Sri Lankan",
                DiasporaGeneration = "SecondGeneration",
                Location = "Melbourne, Australia",
                LanguageProficiency = new
                {
                    English = "Native",
                    Sinhala = "Conversational",
                    Tamil = "Basic"
                }
            },
            EventDetails = new
            {
                EventName = "Avurudu Celebration",
                CulturalSignificance = "High",
                TargetAudience = "Families"
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/cultural-communications/template-selection", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var selection = JsonSerializer.Deserialize<CulturalTemplateSelectionResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        selection.Should().NotBeNull();
        selection.SelectedTemplate.Should().NotBeNull();
        selection.CulturalAdaptationScore.Should().BeGreaterThan(0.7);
        selection.DiasporaAppropriatenessScore.Should().BeGreaterThan(0.8);
        
        // Verify diaspora-specific adaptations
        selection.DiasporaAdaptations.Should().NotBeEmpty();
        selection.SelectedTemplate.IncludesTraditionalElements.Should().BeTrue();
        selection.SelectedTemplate.BalancesModernAndTraditional.Should().BeTrue();
    }

    #endregion

    #region Error Handling and Validation Tests

    [Fact]
    public async Task OptimizeEmailTiming_WithInvalidTimeZone_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new
        {
            RecipientId = Guid.NewGuid(),
            EmailType = "EventNotification",
            PreferredTiming = new
            {
                TimeZone = "Invalid/TimeZone"
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/cultural-communications/optimize-timing", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Invalid time zone");
    }

    [Fact]
    public async Task GetMultiLanguageOptimization_WithUnsupportedLanguage_ShouldReturnPartialSuccess()
    {
        // Arrange
        var request = new
        {
            RecipientId = Guid.NewGuid(),
            EmailType = "WelcomeEmail",
            LanguagePreferences = new[] { "Mandarin", "English" }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/cultural-communications/multi-language", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var optimization = JsonSerializer.Deserialize<MultiLanguageOptimizationResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        optimization.Should().NotBeNull();
        optimization.FallbackLanguage.Should().Be("English");
        optimization.Warnings.Should().Contain("Unsupported primary language");
    }

    #endregion

    #region Performance and Analytics Tests

    [Fact]
    public async Task CulturalCommunicationAnalytics_WithUsageMetrics_ShouldReturnInsights()
    {
        // Arrange
        var request = new
        {
            TimeRange = new
            {
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow
            },
            AnalyticsType = "CulturalOptimization",
            SegmentBy = new[] { "Language", "CulturalBackground", "EmailType" }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/cultural-communications/analytics", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var analytics = JsonSerializer.Deserialize<CulturalCommunicationAnalyticsResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        analytics.Should().NotBeNull();
        analytics.OptimizationMetrics.Should().NotBeNull();
        analytics.LanguageDistribution.Should().NotBeEmpty();
        analytics.CulturalEffectivenessScores.Should().NotBeEmpty();
        analytics.EngagementRatesBySegment.Should().NotBeEmpty();
    }

    #endregion
}

#region Response DTOs

public class EmailTimingOptimizationResponse
{
    public List<OptimalSendTimeDto> OptimalSendTimes { get; set; } = new();
    public List<string> CulturalConsiderations { get; set; } = new();
    public PoyadayConflictAnalysisDto PoyadayConflicts { get; set; } = new();
}

public class OptimalSendTimeDto
{
    public DateTime RecommendedTime { get; set; }
    public double OptimalityScore { get; set; }
    public bool ConflictsWithReligiousDay { get; set; }
    public string Reasoning { get; set; } = string.Empty;
}

public class MultiLanguageOptimizationResponse
{
    public EmailTemplateDto RecommendedTemplate { get; set; } = new();
    public string PrimaryLanguage { get; set; } = string.Empty;
    public string FallbackLanguage { get; set; } = string.Empty;
    public List<string> CulturalAdaptations { get; set; } = new();
    public double LanguageQualityScore { get; set; }
    public double CulturalRelevanceScore { get; set; }
    public List<string> Warnings { get; set; } = new();
}

public class CulturalEmailContextResponse
{
    public List<string> CulturalAdaptations { get; set; } = new();
    public List<string> LanguageSuggestions { get; set; } = new();
    public List<string> ReligiousConsiderations { get; set; } = new();
    public List<string> FamilyContextAdaptations { get; set; } = new();
    public bool HinduFestivalAwareness { get; set; }
    public bool FamilyOrientedMessaging { get; set; }
}

public class CulturalTemplateSelectionResponse
{
    public CulturalTemplateDto SelectedTemplate { get; set; } = new();
    public double CulturalAdaptationScore { get; set; }
    public double DiasporaAppropriatenessScore { get; set; }
    public List<string> DiasporaAdaptations { get; set; } = new();
}

public class CulturalTemplateDto
{
    public Guid TemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IncludesTraditionalElements { get; set; }
    public bool BalancesModernAndTraditional { get; set; }
}

public class CulturalCommunicationAnalyticsResponse
{
    public OptimizationMetricsDto OptimizationMetrics { get; set; } = new();
    public Dictionary<string, int> LanguageDistribution { get; set; } = new();
    public Dictionary<string, double> CulturalEffectivenessScores { get; set; } = new();
    public Dictionary<string, double> EngagementRatesBySegment { get; set; } = new();
}

#endregion