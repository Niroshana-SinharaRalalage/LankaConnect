using FluentAssertions;
using Xunit;
using LankaConnect.Infrastructure.Database.Scaling;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace LankaConnect.Infrastructure.Tests.Database;

public class CulturalIntelligencePredictiveScalingServiceTests
{
    [Fact]
    public void CulturalIntelligencePredictiveScalingService_CanBeConstructed_WithValidParameters()
    {
        // Arrange
        var logger = Substitute.For<ILogger<CulturalIntelligencePredictiveScalingService>>();
        var options = Options.Create(new PredictiveScalingOptions());
        var connectionPoolService = Substitute.For<IEnterpriseConnectionPoolService>();
        var shardingService = Substitute.For<ICulturalIntelligenceShardingService>();

        // Act
        var service = new CulturalIntelligencePredictiveScalingService(
            logger, options, connectionPoolService, shardingService);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void PredictiveScalingOptions_HasDefaultValues_WhenCreated()
    {
        // Arrange & Act
        var options = new PredictiveScalingOptions();

        // Assert
        options.EnableCulturalEventPrediction.Should().BeTrue();
        options.ScalingPredictionWindowHours.Should().Be(72);
        options.MinScalingThresholdPercentage.Should().Be(0.70);
        options.MaxScalingThresholdPercentage.Should().Be(0.85);
        options.CulturalEventMultiplier.Should().Be(3.5);
        options.PredictionAccuracyTarget.Should().Be(0.95);
        options.EnableGeographicLoadBalancing.Should().BeTrue();
        options.AutoScalingCooldownMinutes.Should().Be(15);
    }

    [Theory]
    [InlineData(CulturalEventType.BuddhistPoyaDay, "BuddhistPoyaDay")]
    [InlineData(CulturalEventType.Vesak, "Vesak")]
    [InlineData(CulturalEventType.Diwali, "Diwali")]
    [InlineData(CulturalEventType.Eid, "Eid")]
    [InlineData(CulturalEventType.ChineseNewYear, "ChineseNewYear")]
    [InlineData(CulturalEventType.Vaisakhi, "Vaisakhi")]
    public void CulturalEventType_EnumValues_ShouldHaveCorrectNames(CulturalEventType eventType, string expectedName)
    {
        // Act & Assert
        eventType.ToString().Should().Be(expectedName);
    }

    [Fact]
    public void CulturalEventPrediction_DefaultProperties_ShouldBeInitialized()
    {
        // Act
        var prediction = new CulturalEventPrediction();

        // Assert
        prediction.EventType.Should().Be(CulturalEventType.BuddhistPoyaDay);
        prediction.CommunityId.Should().NotBeNull();
        prediction.GeographicRegion.Should().NotBeNull();
        prediction.PredictedStartTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromDays(1));
        prediction.PredictedEndTime.Should().BeAfter(prediction.PredictedStartTime);
        prediction.ExpectedTrafficMultiplier.Should().Be(1.0);
        prediction.ConfidenceScore.Should().Be(0.0);
        prediction.CulturalSignificanceLevel.Should().Be(CulturalSignificance.Medium);
        prediction.AffectedCommunities.Should().NotBeNull();
    }

    [Theory]
    [InlineData(ScalingDirection.Up, "Up")]
    [InlineData(ScalingDirection.Down, "Down")]
    [InlineData(ScalingDirection.Maintain, "Maintain")]
    [InlineData(ScalingDirection.Emergency, "Emergency")]
    public void ScalingDirection_EnumValues_ShouldHaveCorrectNames(ScalingDirection direction, string expectedName)
    {
        // Act & Assert
        direction.ToString().Should().Be(expectedName);
    }

    [Fact]
    public void AutoScalingDecision_DefaultProperties_ShouldBeInitialized()
    {
        // Act
        var decision = new AutoScalingDecision();

        // Assert
        decision.DecisionId.Should().NotBeNull();
        decision.ScalingDirection.Should().Be(ScalingDirection.Maintain);
        decision.TargetCapacityPercentage.Should().Be(100);
        decision.ReasonCode.Should().NotBeNull();
        decision.CulturalContext.Should().BeNull();
        decision.GeographicRegion.Should().NotBeNull();
        decision.DecisionTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        decision.EstimatedExecutionTime.Should().Be(TimeSpan.Zero);
        decision.RiskAssessment.Should().NotBeNull();
    }

    [Theory]
    [InlineData(ScalingRiskLevel.Low, "Low")]
    [InlineData(ScalingRiskLevel.Medium, "Medium")]
    [InlineData(ScalingRiskLevel.High, "High")]
    [InlineData(ScalingRiskLevel.Critical, "Critical")]
    public void ScalingRiskLevel_EnumValues_ShouldHaveCorrectNames(ScalingRiskLevel riskLevel, string expectedName)
    {
        // Act & Assert
        riskLevel.ToString().Should().Be(expectedName);
    }

    [Fact]
    public void CulturalLoadPattern_DefaultProperties_ShouldBeValid()
    {
        // Act
        var pattern = new CulturalLoadPattern
        {
            CommunityId = "sri_lankan_buddhist",
            GeographicRegion = "north_america",
            PatternType = CulturalEventType.Vesak,
            BaselineLoad = 1000,
            PeakLoad = 5000,
            LoadMultiplier = 5.0
        };

        // Assert
        pattern.CommunityId.Should().Be("sri_lankan_buddhist");
        pattern.GeographicRegion.Should().Be("north_america");
        pattern.PatternType.Should().Be(CulturalEventType.Vesak);
        pattern.BaselineLoad.Should().Be(1000);
        pattern.PeakLoad.Should().Be(5000);
        pattern.LoadMultiplier.Should().Be(5.0);
    }

    [Theory]
    [InlineData("sri_lankan_buddhist", "north_america", CulturalEventType.BuddhistPoyaDay, 2.5)]
    [InlineData("indian_hindu", "europe", CulturalEventType.Diwali, 4.0)]
    [InlineData("pakistani_muslim", "australia", CulturalEventType.Eid, 3.5)]
    [InlineData("sikh_punjabi", "canada", CulturalEventType.Vaisakhi, 2.8)]
    public void CulturalEventPrediction_WithDifferentCombinations_ShouldBeValid(
        string communityId, string region, CulturalEventType eventType, double trafficMultiplier)
    {
        // Act
        var prediction = new CulturalEventPrediction
        {
            CommunityId = communityId,
            GeographicRegion = region,
            EventType = eventType,
            ExpectedTrafficMultiplier = trafficMultiplier,
            ConfidenceScore = 0.92,
            CulturalSignificanceLevel = CulturalSignificance.High
        };

        // Assert
        prediction.CommunityId.Should().Be(communityId);
        prediction.GeographicRegion.Should().Be(region);
        prediction.EventType.Should().Be(eventType);
        prediction.ExpectedTrafficMultiplier.Should().Be(trafficMultiplier);
        prediction.ConfidenceScore.Should().Be(0.92);
        prediction.CulturalSignificanceLevel.Should().Be(CulturalSignificance.High);
    }

    [Fact]
    public async Task PredictCulturalEventScalingAsync_ShouldReturnPrediction_WhenCalled()
    {
        // Arrange
        var logger = Substitute.For<ILogger<CulturalIntelligencePredictiveScalingService>>();
        var options = Options.Create(new PredictiveScalingOptions());
        var connectionPoolService = Substitute.For<IEnterpriseConnectionPoolService>();
        var shardingService = Substitute.For<ICulturalIntelligenceShardingService>();
        var service = new CulturalIntelligencePredictiveScalingService(
            logger, options, connectionPoolService, shardingService);

        var culturalContext = new CulturalContext
        {
            CommunityId = "sri_lankan_buddhist",
            GeographicRegion = "north_america"
        };

        // Act
        var result = await service.PredictCulturalEventScalingAsync(
            culturalContext, 
            TimeSpan.FromHours(72), 
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task EvaluateAutoScalingTriggersAsync_ShouldReturnDecision_WhenCalled()
    {
        // Arrange
        var logger = Substitute.For<ILogger<CulturalIntelligencePredictiveScalingService>>();
        var options = Options.Create(new PredictiveScalingOptions());
        var connectionPoolService = Substitute.For<IEnterpriseConnectionPoolService>();
        var shardingService = Substitute.For<ICulturalIntelligenceShardingService>();
        var service = new CulturalIntelligencePredictiveScalingService(
            logger, options, connectionPoolService, shardingService);

        var currentMetrics = new DatabaseScalingMetrics
        {
            AverageConnectionUtilization = 0.78,
            ResponseTimePercentile95 = TimeSpan.FromMilliseconds(150),
            QueriesPerSecond = 8500,
            ErrorRate = 0.001
        };

        // Act
        var result = await service.EvaluateAutoScalingTriggersAsync(
            currentMetrics, 
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void DatabaseScalingMetrics_DefaultProperties_ShouldBeInitialized()
    {
        // Act
        var metrics = new DatabaseScalingMetrics();

        // Assert
        metrics.MetricsId.Should().NotBeNull();
        metrics.AverageConnectionUtilization.Should().Be(0.0);
        metrics.ResponseTimePercentile95.Should().Be(TimeSpan.Zero);
        metrics.ResponseTimePercentile99.Should().Be(TimeSpan.Zero);
        metrics.QueriesPerSecond.Should().Be(0);
        metrics.ErrorRate.Should().Be(0.0);
        metrics.CollectionTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        metrics.AdditionalMetrics.Should().NotBeNull();
    }

    [Fact]
    public void ScalingExecutionResult_DefaultProperties_ShouldBeInitialized()
    {
        // Act
        var result = new ScalingExecutionResult();

        // Assert
        result.ExecutionId.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ExecutionDuration.Should().Be(TimeSpan.Zero);
        result.AchievedCapacityPercentage.Should().Be(100);
        result.ExecutionLogs.Should().NotBeNull();
        result.PerformanceImpact.Should().NotBeNull();
        result.ExecutionTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(CulturalTrafficPattern.RegularWorkday, "RegularWorkday")]
    [InlineData(CulturalTrafficPattern.Weekend, "Weekend")]
    [InlineData(CulturalTrafficPattern.CulturalHoliday, "CulturalHoliday")]
    [InlineData(CulturalTrafficPattern.ReligiousObservance, "ReligiousObservance")]
    [InlineData(CulturalTrafficPattern.CommunityEvent, "CommunityEvent")]
    public void CulturalTrafficPattern_EnumValues_ShouldHaveCorrectNames(
        CulturalTrafficPattern pattern, string expectedName)
    {
        // Act & Assert
        pattern.ToString().Should().Be(expectedName);
    }

    [Fact]
    public void GeographicScalingConfiguration_DefaultProperties_ShouldBeValid()
    {
        // Act
        var config = new GeographicScalingConfiguration
        {
            Region = "north_america",
            MaxConcurrentUsers = 500000,
            ScalingThreshold = 0.80,
            PreferredScalingDirection = ScalingDirection.Up,
            CulturalCommunityPriority = new List<string> { "sri_lankan_buddhist", "indian_hindu" }
        };

        // Assert
        config.Region.Should().Be("north_america");
        config.MaxConcurrentUsers.Should().Be(500000);
        config.ScalingThreshold.Should().Be(0.80);
        config.PreferredScalingDirection.Should().Be(ScalingDirection.Up);
        config.CulturalCommunityPriority.Should().Contain("sri_lankan_buddhist");
        config.CulturalCommunityPriority.Should().Contain("indian_hindu");
    }

    [Fact]
    public async Task ExecuteScalingActionAsync_ShouldReturnResult_WhenCalled()
    {
        // Arrange
        var logger = Substitute.For<ILogger<CulturalIntelligencePredictiveScalingService>>();
        var options = Options.Create(new PredictiveScalingOptions());
        var connectionPoolService = Substitute.For<IEnterpriseConnectionPoolService>();
        var shardingService = Substitute.For<ICulturalIntelligenceShardingService>();
        var service = new CulturalIntelligencePredictiveScalingService(
            logger, options, connectionPoolService, shardingService);

        var decision = new AutoScalingDecision
        {
            ScalingDirection = ScalingDirection.Up,
            TargetCapacityPercentage = 150,
            GeographicRegion = "north_america"
        };

        // Act
        var result = await service.ExecuteScalingActionAsync(decision, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void CulturalEventCalendar_DefaultProperties_ShouldBeInitialized()
    {
        // Act
        var calendar = new CulturalEventCalendar();

        // Assert
        calendar.CalendarId.Should().NotBeNull();
        calendar.CommunityId.Should().NotBeNull();
        calendar.SupportedEventTypes.Should().NotBeNull();
        calendar.GeographicRegion.Should().NotBeNull();
        calendar.CalendarAccuracy.Should().Be(0.95);
        calendar.LastUpdated.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        calendar.UpcomingEvents.Should().NotBeNull();
    }

    [Fact]
    public void PredictiveScalingInsights_DefaultProperties_ShouldBeInitialized()
    {
        // Act
        var insights = new PredictiveScalingInsights();

        // Assert
        insights.InsightId.Should().NotBeNull();
        insights.PredictionAccuracy.Should().Be(0.0);
        insights.CulturalEventPredictions.Should().NotBeNull();
        insights.GeographicLoadDistribution.Should().NotBeNull();
        insights.ScalingRecommendations.Should().NotBeNull();
        insights.OptimizationOpportunities.Should().NotBeNull();
        insights.GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(AutoScalingStrategy.Conservative, "Conservative")]
    [InlineData(AutoScalingStrategy.Balanced, "Balanced")]
    [InlineData(AutoScalingStrategy.Aggressive, "Aggressive")]
    [InlineData(AutoScalingStrategy.CulturallyIntelligent, "CulturallyIntelligent")]
    public void AutoScalingStrategy_EnumValues_ShouldHaveCorrectNames(
        AutoScalingStrategy strategy, string expectedName)
    {
        // Act & Assert
        strategy.ToString().Should().Be(expectedName);
    }

    [Fact]
    public async Task GetCulturalLoadPatternsAsync_ShouldReturnPatterns_WhenCalled()
    {
        // Arrange
        var logger = Substitute.For<ILogger<CulturalIntelligencePredictiveScalingService>>();
        var options = Options.Create(new PredictiveScalingOptions());
        var connectionPoolService = Substitute.For<IEnterpriseConnectionPoolService>();
        var shardingService = Substitute.For<ICulturalIntelligenceShardingService>();
        var service = new CulturalIntelligencePredictiveScalingService(
            logger, options, connectionPoolService, shardingService);

        // Act
        var result = await service.GetCulturalLoadPatternsAsync(
            "sri_lankan_buddhist", 
            "north_america", 
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task MonitorScalingPerformanceAsync_ShouldReturnMetrics_WhenCalled()
    {
        // Arrange
        var logger = Substitute.For<ILogger<CulturalIntelligencePredictiveScalingService>>();
        var options = Options.Create(new PredictiveScalingOptions());
        var connectionPoolService = Substitute.For<IEnterpriseConnectionPoolService>();
        var shardingService = Substitute.For<ICulturalIntelligenceShardingService>();
        var service = new CulturalIntelligencePredictiveScalingService(
            logger, options, connectionPoolService, shardingService);

        // Act
        var result = await service.MonitorScalingPerformanceAsync(
            TimeSpan.FromHours(24), 
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void CulturalScalingAlert_DefaultProperties_ShouldBeValid()
    {
        // Act
        var alert = new CulturalScalingAlert
        {
            AlertId = "cultural_vesak_2025_alert",
            AlertType = CulturalAlertType.UpcomingCulturalEvent,
            CommunityId = "sri_lankan_buddhist",
            EventType = CulturalEventType.Vesak,
            RecommendedAction = "Scale up by 400% starting 48 hours before event",
            Urgency = CulturalAlertUrgency.High
        };

        // Assert
        alert.AlertId.Should().Be("cultural_vesak_2025_alert");
        alert.AlertType.Should().Be(CulturalAlertType.UpcomingCulturalEvent);
        alert.CommunityId.Should().Be("sri_lankan_buddhist");
        alert.EventType.Should().Be(CulturalEventType.Vesak);
        alert.RecommendedAction.Should().Be("Scale up by 400% starting 48 hours before event");
        alert.Urgency.Should().Be(CulturalAlertUrgency.High);
    }

    [Theory]
    [InlineData(CulturalAlertType.UpcomingCulturalEvent, "UpcomingCulturalEvent")]
    [InlineData(CulturalAlertType.UnexpectedTrafficSpike, "UnexpectedTrafficSpike")]
    [InlineData(CulturalAlertType.ScalingFailure, "ScalingFailure")]
    [InlineData(CulturalAlertType.CulturalConflict, "CulturalConflict")]
    public void CulturalAlertType_EnumValues_ShouldHaveCorrectNames(
        CulturalAlertType alertType, string expectedName)
    {
        // Act & Assert
        alertType.ToString().Should().Be(expectedName);
    }
}