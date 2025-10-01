using FluentAssertions;
using LankaConnect.Domain.Common.Monitoring;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.ValueObjects;

namespace LankaConnect.Domain.Tests.Common.Monitoring;

/// <summary>
/// TDD RED PHASE: Tests for CulturalIntelligenceEndpoint (18 references - Tier 1 Priority)
/// These tests establish the contract for cultural intelligence monitoring endpoints
/// </summary>
public class CulturalIntelligenceEndpointTests
{
    [Test]
    public void CulturalIntelligenceEndpoint_ShouldHaveRequiredProperties()
    {
        // Arrange
        var endpoint = new CulturalIntelligenceEndpoint(
            name: "SriLankanBuddhistEvents",
            region: GeographicRegion.WesternProvince,
            culturalContext: CulturalContext.Buddhist,
            monitoringLevel: MonitoringLevel.Enhanced,
            isActive: true
        );

        // Act & Assert
        endpoint.Name.Should().Be("SriLankanBuddhistEvents");
        endpoint.Region.Should().Be(GeographicRegion.WesternProvince);
        endpoint.CulturalContext.Should().Be(CulturalContext.Buddhist);
        endpoint.MonitoringLevel.Should().Be(MonitoringLevel.Enhanced);
        endpoint.IsActive.Should().BeTrue();
        endpoint.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Test]
    public void CulturalIntelligenceEndpoint_ShouldValidateEndpointName()
    {
        // Arrange & Act
        var createWithInvalidName = () => new CulturalIntelligenceEndpoint(
            name: "", // Invalid empty name
            region: GeographicRegion.CentralProvince,
            culturalContext: CulturalContext.Tamil,
            monitoringLevel: MonitoringLevel.Standard,
            isActive: true
        );

        // Assert
        createWithInvalidName.Should().Throw<ArgumentException>()
            .WithMessage("Endpoint name cannot be empty");
    }

    [Test]
    public void CulturalIntelligenceEndpoint_ShouldSupportAllSriLankanCulturalContexts()
    {
        // Arrange
        var buddhist = new CulturalIntelligenceEndpoint(
            "VesakCelebrations", GeographicRegion.WesternProvince, CulturalContext.Buddhist, MonitoringLevel.Enhanced, true);

        var tamil = new CulturalIntelligenceEndpoint(
            "TamilNewYear", GeographicRegion.NorthernProvince, CulturalContext.Tamil, MonitoringLevel.Enhanced, true);

        var muslim = new CulturalIntelligenceEndpoint(
            "EidCelebrations", GeographicRegion.EasternProvince, CulturalContext.Muslim, MonitoringLevel.Standard, true);

        var christian = new CulturalIntelligenceEndpoint(
            "ChristmasServices", GeographicRegion.WesternProvince, CulturalContext.Christian, MonitoringLevel.Standard, true);

        // Act & Assert
        buddhist.GetCulturalSensitivityLevel().Should().Be(CulturalSensitivityLevel.High);
        tamil.GetCulturalSensitivityLevel().Should().Be(CulturalSensitivityLevel.High);
        muslim.GetCulturalSensitivityLevel().Should().Be(CulturalSensitivityLevel.Medium);
        christian.GetCulturalSensitivityLevel().Should().Be(CulturalSensitivityLevel.Medium);
    }

    [Test]
    public void CulturalIntelligenceEndpoint_ShouldConfigureMonitoringMetrics()
    {
        // Arrange
        var endpoint = new CulturalIntelligenceEndpoint(
            "DiasporaEngagement",
            GeographicRegion.International,
            CulturalContext.Multicultural,
            MonitoringLevel.Premium,
            true
        );

        // Act
        var metrics = endpoint.GetMonitoringMetrics();

        // Assert
        metrics.Should().NotBeNull();
        metrics.CulturalEventDetection.Should().BeTrue();
        metrics.DiasporaEngagementTracking.Should().BeTrue();
        metrics.LanguagePreferenceMonitoring.Should().BeTrue();
        metrics.CulturalSentimentAnalysis.Should().BeTrue();
        metrics.SacredEventRespectValidation.Should().BeTrue();
    }

    [Test]
    public void CulturalIntelligenceEndpoint_ShouldHandleSacredEventPeriods()
    {
        // Arrange
        var endpoint = new CulturalIntelligenceEndpoint(
            "SacredEventMonitoring",
            GeographicRegion.CentralProvince,
            CulturalContext.Buddhist,
            MonitoringLevel.Enhanced,
            true
        );

        var sacredPeriod = new SacredEventPeriod(
            eventName: "Vesak",
            startDate: DateTime.UtcNow.Date,
            endDate: DateTime.UtcNow.Date.AddDays(2),
            culturalSignificance: CulturalSignificance.Supreme
        );

        // Act
        endpoint.ConfigureForSacredPeriod(sacredPeriod);

        // Assert
        endpoint.IsInSacredMode.Should().BeTrue();
        endpoint.CurrentSacredEvent.Should().Be("Vesak");
        endpoint.MonitoringLevel.Should().Be(MonitoringLevel.Premium);
        endpoint.RequiresEnhancedRespect.Should().BeTrue();
    }

    [Test]
    public void CulturalIntelligenceEndpoint_ShouldGenerateHealthStatus()
    {
        // Arrange
        var endpoint = new CulturalIntelligenceEndpoint(
            "CulturalDataProcessing",
            GeographicRegion.SouthernProvince,
            CulturalContext.Sinhala,
            MonitoringLevel.Standard,
            true
        );

        // Simulate some monitoring data
        endpoint.RecordCulturalEvent("TraditionalDance", CulturalEventType.Cultural);
        endpoint.RecordCulturalEvent("HeritageFestival", CulturalEventType.Heritage);

        // Act
        var healthStatus = endpoint.GetHealthStatus();

        // Assert
        healthStatus.Should().NotBeNull();
        healthStatus.IsHealthy.Should().BeTrue();
        healthStatus.CulturalEventsProcessed.Should().Be(2);
        healthStatus.CulturalSensitivityScore.Should().BeGreaterThan(0.0);
        healthStatus.LastActivityTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Test]
    public void CulturalIntelligenceEndpoint_ShouldDetectCulturalInsensitivity()
    {
        // Arrange
        var endpoint = new CulturalIntelligenceEndpoint(
            "ContentModerationEndpoint",
            GeographicRegion.WesternProvince,
            CulturalContext.Buddhist,
            MonitoringLevel.Enhanced,
            true
        );

        // Act
        var insensitiveResult = endpoint.ValidateCulturalSensitivity("Buddha statue in inappropriate context");
        var respectfulResult = endpoint.ValidateCulturalSensitivity("Traditional Buddhist ceremony");

        // Assert
        insensitiveResult.IsRespectful.Should().BeFalse();
        insensitiveResult.SensitivityScore.Should().BeLessThan(0.5);
        insensitiveResult.RequiresReview.Should().BeTrue();

        respectfulResult.IsRespectful.Should().BeTrue();
        respectfulResult.SensitivityScore.Should().BeGreaterThan(0.8);
        respectfulResult.RequiresReview.Should().BeFalse();
    }

    [Test]
    public void CulturalIntelligenceEndpoint_ShouldSupportDiasporaCommunities()
    {
        // Arrange
        var diasporaEndpoint = new CulturalIntelligenceEndpoint(
            "GlobalSriLankanDiaspora",
            GeographicRegion.International,
            CulturalContext.Diaspora,
            MonitoringLevel.Premium,
            true
        );

        // Act
        var diasporaConfig = diasporaEndpoint.GetDiasporaConfiguration();

        // Assert
        diasporaConfig.Should().NotBeNull();
        diasporaConfig.SupportsMultipleTimeZones.Should().BeTrue();
        diasporaConfig.TracksHomelandConnections.Should().BeTrue();
        diasporaConfig.MonitorsCulturalPreservation.Should().BeTrue();
        diasporaConfig.SupportsMultipleLanguages.Should().BeTrue();
        diasporaConfig.ConnectsWithHomelandEvents.Should().BeTrue();
    }
}

/// <summary>
/// Supporting types for cultural intelligence endpoint testing
/// </summary>
public record SacredEventPeriod(
    string EventName,
    DateTime StartDate,
    DateTime EndDate,
    CulturalSignificance CulturalSignificance
);

public record CulturalIntelligenceMetrics(
    bool CulturalEventDetection,
    bool DiasporaEngagementTracking,
    bool LanguagePreferenceMonitoring,
    bool CulturalSentimentAnalysis,
    bool SacredEventRespectValidation
);

public record EndpointHealthStatus(
    bool IsHealthy,
    int CulturalEventsProcessed,
    double CulturalSensitivityScore,
    DateTime LastActivityTimestamp
);

public record CulturalSensitivityResult(
    bool IsRespectful,
    double SensitivityScore,
    bool RequiresReview,
    string RecommendedAction
);

public record DiasporaConfiguration(
    bool SupportsMultipleTimeZones,
    bool TracksHomelandConnections,
    bool MonitorsCulturalPreservation,
    bool SupportsMultipleLanguages,
    bool ConnectsWithHomelandEvents
);

public enum CulturalContext
{
    Buddhist,
    Tamil,
    Muslim,
    Christian,
    Sinhala,
    Multicultural,
    Diaspora
}

public enum MonitoringLevel
{
    Basic,
    Standard,
    Enhanced,
    Premium
}

public enum CulturalSensitivityLevel
{
    Low,
    Medium,
    High,
    Supreme
}

public enum CulturalSignificance
{
    Minor,
    Moderate,
    High,
    Supreme
}