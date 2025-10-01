using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using FluentAssertions;
using Xunit;
using NSubstitute;
using LankaConnect.Infrastructure.Monitoring;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;

namespace LankaConnect.Infrastructure.Tests.Monitoring;

public class CulturalIntelligenceMetricsServiceTests
{
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger<CulturalIntelligenceMetricsService> _logger;
    private readonly CulturalIntelligenceMetricsService _sut;

    public CulturalIntelligenceMetricsServiceTests()
    {
        var telemetryConfiguration = new TelemetryConfiguration();
        _telemetryClient = new TelemetryClient(telemetryConfiguration);
        _logger = Substitute.For<ILogger<CulturalIntelligenceMetricsService>>();
        _sut = new CulturalIntelligenceMetricsService(_telemetryClient, _logger);
    }

    [Fact]
    public async Task TrackCulturalApiPerformance_WithValidMetrics_ShouldTrackPerformanceSuccessfully()
    {
        // Arrange
        var apiEndpoint = CulturalIntelligenceEndpoint.BuddhistCalendar;
        var responseTime = TimeSpan.FromMilliseconds(45);
        var requestSize = 1024;
        var responseSize = 2048;
        var culturalContext = new CulturalContext
        {
            CommunityId = "buddhist_sri_lanka",
            GeographicRegion = "sri_lanka",
            Language = "si"
        };

        // Act
        var result = await _sut.TrackCulturalApiPerformanceAsync(
            apiEndpoint, 
            responseTime, 
            requestSize, 
            responseSize, 
            culturalContext);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Endpoint.Should().Be(apiEndpoint);
        result.Value.ResponseTime.Should().Be(responseTime);
        result.Value.CulturalContext.Should().Be(culturalContext);
        result.Value.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task TrackCulturalApiAccuracy_WithValidAccuracyMetrics_ShouldTrackAccuracySuccessfully()
    {
        // Arrange
        var apiEndpoint = CulturalIntelligenceEndpoint.CulturalAppropriateness;
        var accuracyScore = 0.95;
        var confidenceScore = 0.89;
        var culturalRelevanceScore = 0.92;
        var communityId = "hindu_indian";

        // Act
        var result = await _sut.TrackCulturalApiAccuracyAsync(
            apiEndpoint,
            accuracyScore,
            confidenceScore,
            culturalRelevanceScore,
            communityId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.AccuracyScore.Should().Be(accuracyScore);
        result.Value.ConfidenceScore.Should().Be(confidenceScore);
        result.Value.CulturalRelevanceScore.Should().Be(culturalRelevanceScore);
        result.Value.CommunityId.Should().Be(communityId);
    }

    [Theory]
    [InlineData(CulturalIntelligenceEndpoint.BuddhistCalendar, 25.5)]
    [InlineData(CulturalIntelligenceEndpoint.HinduCalendar, 35.2)]
    [InlineData(CulturalIntelligenceEndpoint.DiasporaAnalytics, 150.8)]
    [InlineData(CulturalIntelligenceEndpoint.EventRecommendations, 80.3)]
    public async Task TrackApiResponseTime_WithDifferentEndpoints_ShouldTrackCorrectly(CulturalIntelligenceEndpoint endpoint, double responseTimeMs)
    {
        // Arrange
        var responseTime = TimeSpan.FromMilliseconds(responseTimeMs);
        var culturalContext = new CulturalContext
        {
            CommunityId = "test_community",
            GeographicRegion = "test_region",
            Language = "en"
        };

        // Act
        var result = await _sut.TrackApiResponseTimeAsync(endpoint, responseTime, culturalContext);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ResponseTime.TotalMilliseconds.Should().Be(responseTimeMs);
        result.Value.Endpoint.Should().Be(endpoint);
    }

    [Fact]
    public async Task TrackDiasporaEngagement_WithValidEngagementMetrics_ShouldTrackSuccessfully()
    {
        // Arrange
        var communityId = "tamil_sri_lanka";
        var engagementType = DiasporaEngagementType.CulturalEventParticipation;
        var geographicRegion = "sri_lanka";
        var userCount = 245;
        var engagementScore = 0.87;

        // Act
        var result = await _sut.TrackDiasporaEngagementAsync(
            communityId,
            engagementType,
            geographicRegion,
            userCount,
            engagementScore);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CommunityId.Should().Be(communityId);
        result.Value.EngagementType.Should().Be(engagementType);
        result.Value.UserCount.Should().Be(userCount);
        result.Value.EngagementScore.Should().Be(engagementScore);
    }

    [Fact]
    public async Task TrackEnterpriseClientSla_WithValidSlaMetrics_ShouldTrackSuccessfully()
    {
        // Arrange
        var clientId = "fortune500_client_001";
        var contractTier = EnterpriseContractTier.Fortune500Premium;
        var slaTarget = TimeSpan.FromMilliseconds(200);
        var actualResponseTime = TimeSpan.FromMilliseconds(145);
        var culturalFeatureUsage = new CulturalFeatureUsageMetrics
        {
            CulturalCalendarRequests = 1250,
            DiasporaAnalyticsRequests = 780,
            CulturalAppropriatenessRequests = 430,
            EventRecommendationRequests = 920
        };

        // Act
        var result = await _sut.TrackEnterpriseClientSlaAsync(
            clientId,
            contractTier,
            slaTarget,
            actualResponseTime,
            culturalFeatureUsage);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ClientId.Should().Be(clientId);
        result.Value.ContractTier.Should().Be(contractTier);
        result.Value.SlaCompliance.Should().BeTrue();
        result.Value.ResponseTimeVariance.Should().Be(TimeSpan.FromMilliseconds(-55));
    }

    [Theory]
    [InlineData(150, 200, true)]   // Within SLA
    [InlineData(250, 200, false)]  // SLA breach
    [InlineData(199, 200, true)]   // Edge case - just within
    [InlineData(201, 200, false)]  // Edge case - just outside
    public async Task TrackEnterpriseClientSla_WithDifferentResponseTimes_ShouldCalculateComplianceCorrectly(
        double actualMs, double targetMs, bool expectedCompliance)
    {
        // Arrange
        var clientId = "test_client";
        var contractTier = EnterpriseContractTier.Fortune500Standard;
        var slaTarget = TimeSpan.FromMilliseconds(targetMs);
        var actualResponseTime = TimeSpan.FromMilliseconds(actualMs);
        var culturalFeatureUsage = new CulturalFeatureUsageMetrics();

        // Act
        var result = await _sut.TrackEnterpriseClientSlaAsync(
            clientId,
            contractTier,
            slaTarget,
            actualResponseTime,
            culturalFeatureUsage);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SlaCompliance.Should().Be(expectedCompliance);
    }

    [Fact]
    public async Task TrackRevenueImpact_WithValidRevenueMetrics_ShouldTrackSuccessfully()
    {
        // Arrange
        var apiEndpoint = CulturalIntelligenceEndpoint.CulturalAppropriateness;
        var revenuePerRequest = 0.05m; // $0.05 per API call
        var requestCount = 10000;
        var subscriptionTier = SubscriptionTier.EnterprisePremium;
        var clientSegment = ClientSegment.Fortune500;

        // Act
        var result = await _sut.TrackRevenueImpactAsync(
            apiEndpoint,
            revenuePerRequest,
            requestCount,
            subscriptionTier,
            clientSegment);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalRevenue.Should().Be(500.0m); // $0.05 * 10,000
        result.Value.ApiEndpoint.Should().Be(apiEndpoint);
        result.Value.RequestCount.Should().Be(requestCount);
    }

    [Fact]
    public async Task GetCulturalIntelligenceHealthStatus_ShouldReturnComprehensiveHealthMetrics()
    {
        // Act
        var result = await _sut.GetCulturalIntelligenceHealthStatusAsync();

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.OverallHealthScore.Should().BeGreaterThan(0);
        result.Value.ComponentHealthStatuses.Should().NotBeEmpty();
        result.Value.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task TrackCulturalDataQuality_WithValidQualityMetrics_ShouldTrackSuccessfully()
    {
        // Arrange
        var dataType = CulturalDataType.CalendarEvents;
        var accuracyScore = 0.96;
        var completenessScore = 0.92;
        var freshnessScore = 0.88;
        var culturalAuthenticityScore = 0.94;
        var communityId = "sikh_punjabi";

        // Act
        var result = await _sut.TrackCulturalDataQualityAsync(
            dataType,
            accuracyScore,
            completenessScore,
            freshnessScore,
            culturalAuthenticityScore,
            communityId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.DataType.Should().Be(dataType);
        result.Value.AccuracyScore.Should().Be(accuracyScore);
        result.Value.CompletenessScore.Should().Be(completenessScore);
        result.Value.FreshnessScore.Should().Be(freshnessScore);
        result.Value.CulturalAuthenticityScore.Should().Be(culturalAuthenticityScore);
    }

    [Theory]
    [InlineData("buddhist_sri_lanka", "sri_lanka", "si")]
    [InlineData("hindu_indian", "india", "hi")]
    [InlineData("islamic_pakistani", "pakistan", "ur")]
    [InlineData("sikh_punjabi", "india", "pa")]
    public async Task TrackCulturalContextPerformance_WithDifferentCulturalContexts_ShouldTrackCorrectly(
        string communityId, string region, string language)
    {
        // Arrange
        var culturalContext = new CulturalContext
        {
            CommunityId = communityId,
            GeographicRegion = region,
            Language = language
        };
        var performanceMetrics = new CulturalContextPerformanceMetrics
        {
            ContextResolutionTime = TimeSpan.FromMilliseconds(25),
            CulturalRelevanceScore = 0.91,
            LanguageLocalizationScore = 0.87,
            RegionalAdaptationScore = 0.93
        };

        // Act
        var result = await _sut.TrackCulturalContextPerformanceAsync(culturalContext, performanceMetrics);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CulturalContext.Should().Be(culturalContext);
        result.Value.PerformanceMetrics.Should().Be(performanceMetrics);
    }

    [Fact]
    public async Task TrackAlertingEvent_WithCriticalCulturalIntelligenceAlert_ShouldTrackSuccessfully()
    {
        // Arrange
        var alertType = CulturalIntelligenceAlertType.CulturalDataCorruption;
        var severity = AlertSeverity.Critical;
        var description = "Cultural calendar data accuracy dropped below 95% threshold";
        var affectedEndpoints = new List<CulturalIntelligenceEndpoint> 
        { 
            CulturalIntelligenceEndpoint.BuddhistCalendar, 
            CulturalIntelligenceEndpoint.HinduCalendar 
        };
        var impactedCommunities = new List<string> { "buddhist_sri_lanka", "hindu_indian" };

        // Act
        var result = await _sut.TrackAlertingEventAsync(
            alertType,
            severity,
            description,
            affectedEndpoints,
            impactedCommunities);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AlertType.Should().Be(alertType);
        result.Value.Severity.Should().Be(severity);
        result.Value.AffectedEndpoints.Should().BeEquivalentTo(affectedEndpoints);
        result.Value.ImpactedCommunities.Should().BeEquivalentTo(impactedCommunities);
    }

    [Fact]
    public void Dispose_ShouldDisposeResourcesProperly()
    {
        // Act
        var disposingAction = () => _sut.Dispose();

        // Assert
        disposingAction.Should().NotThrow();
    }
}