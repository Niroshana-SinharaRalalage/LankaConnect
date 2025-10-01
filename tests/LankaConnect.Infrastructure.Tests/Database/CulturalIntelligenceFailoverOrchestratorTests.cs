using FluentAssertions;
using Xunit;
using LankaConnect.Infrastructure.Database.Failover;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace LankaConnect.Infrastructure.Tests.Database;

public class CulturalIntelligenceFailoverOrchestratorTests
{
    [Fact]
    public void CulturalIntelligenceFailoverOrchestrator_CanBeConstructed_WithValidParameters()
    {
        // Arrange
        var logger = Substitute.For<ILogger<CulturalIntelligenceFailoverOrchestrator>>();
        var options = Options.Create(new CulturalFailoverOptions());
        var consistencyService = Substitute.For<ICulturalIntelligenceConsistencyService>();
        var replicationService = Substitute.For<ICulturalStateReplicationService>();

        // Act
        var orchestrator = new CulturalIntelligenceFailoverOrchestrator(
            logger, options, consistencyService, replicationService);

        // Assert
        orchestrator.Should().NotBeNull();
    }

    [Fact]
    public void CulturalFailoverOptions_HasDefaultValues_WhenCreated()
    {
        // Arrange & Act
        var options = new CulturalFailoverOptions();

        // Assert
        options.MaxFailoverTimeSeconds.Should().Be(60);
        options.SacredEventFailoverTimeSeconds.Should().Be(30);
        options.CommunityContentFailoverTimeSeconds.Should().Be(45);
        options.EnableCulturalIntelligenceFailover.Should().BeTrue();
        options.PreserveCulturalConsistency.Should().BeTrue();
        options.RevenueProtectionMode.Should().BeTrue();
        options.DiasporaCommunityContinuity.Should().BeTrue();
        options.FailoverHealthCheckIntervalSeconds.Should().Be(30);
    }

    [Theory]
    [InlineData(CulturalFailoverPriority.Sacred, "Sacred")]
    [InlineData(CulturalFailoverPriority.Critical, "Critical")]
    [InlineData(CulturalFailoverPriority.High, "High")]
    [InlineData(CulturalFailoverPriority.Medium, "Medium")]
    [InlineData(CulturalFailoverPriority.Low, "Low")]
    public void CulturalFailoverPriority_EnumValues_ShouldHaveCorrectNames(
        CulturalFailoverPriority priority, string expectedName)
    {
        // Act & Assert
        priority.ToString().Should().Be(expectedName);
    }

    [Fact]
    public void CulturalFailoverRequest_DefaultProperties_ShouldBeInitialized()
    {
        // Act
        var request = new CulturalFailoverRequest();

        // Assert
        request.FailoverRequestId.Should().NotBeNull();
        request.SourceRegion.Should().NotBeNull();
        request.TargetRegion.Should().NotBeNull();
        request.FailoverReason.Should().NotBeNull();
        request.CulturalDataTypes.Should().NotBeNull();
        request.FailoverPriority.Should().Be(CulturalFailoverPriority.Medium);
        request.PreserveCulturalConsistency.Should().BeTrue();
        request.RequestTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        request.MaxFailoverDuration.Should().Be(TimeSpan.FromSeconds(60));
    }

    [Theory]
    [InlineData(FailoverExecutionStatus.Pending, "Pending")]
    [InlineData(FailoverExecutionStatus.InProgress, "InProgress")]
    [InlineData(FailoverExecutionStatus.Completed, "Completed")]
    [InlineData(FailoverExecutionStatus.Failed, "Failed")]
    [InlineData(FailoverExecutionStatus.PartiallyCompleted, "PartiallyCompleted")]
    [InlineData(FailoverExecutionStatus.RolledBack, "RolledBack")]
    public void FailoverExecutionStatus_EnumValues_ShouldHaveCorrectNames(
        FailoverExecutionStatus status, string expectedName)
    {
        // Act & Assert
        status.ToString().Should().Be(expectedName);
    }

    [Fact]
    public void CulturalFailoverResult_DefaultProperties_ShouldBeInitialized()
    {
        // Act
        var result = new CulturalFailoverResult();

        // Assert
        result.FailoverResultId.Should().NotBeNull();
        result.SourceRegion.Should().NotBeNull();
        result.TargetRegion.Should().NotBeNull();
        result.ExecutionStatus.Should().Be(FailoverExecutionStatus.Pending);
        result.FailoverStartTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.FailoverDuration.Should().Be(TimeSpan.Zero);
        result.CulturalConsistencyMaintained.Should().BeTrue();
        result.RevenueImpact.Should().Be(0.0);
        result.AffectedCommunities.Should().NotBeNull();
        result.FailoverLogs.Should().NotBeNull();
    }

    [Theory]
    [InlineData(CulturalDisasterType.RegionOutage, "RegionOutage")]
    [InlineData(CulturalDisasterType.NetworkPartition, "NetworkPartition")]
    [InlineData(CulturalDisasterType.DatabaseFailure, "DatabaseFailure")]
    [InlineData(CulturalDisasterType.ServiceDegradation, "ServiceDegradation")]
    [InlineData(CulturalDisasterType.SecurityBreach, "SecurityBreach")]
    [InlineData(CulturalDisasterType.NaturalDisaster, "NaturalDisaster")]
    public void CulturalDisasterType_EnumValues_ShouldHaveCorrectNames(
        CulturalDisasterType disasterType, string expectedName)
    {
        // Act & Assert
        disasterType.ToString().Should().Be(expectedName);
    }

    [Fact]
    public void CulturalDisasterRecoveryPlan_DefaultProperties_ShouldBeValid()
    {
        // Act
        var plan = new CulturalDisasterRecoveryPlan
        {
            PlanId = "vesak_2025_disaster_recovery",
            DisasterType = CulturalDisasterType.RegionOutage,
            AffectedRegions = new List<string> { "asia_pacific", "north_america" },
            CulturalEventType = CulturalEventType.Vesak,
            RecoveryTimeObjective = TimeSpan.FromSeconds(30),
            RecoveryPointObjective = TimeSpan.FromSeconds(5),
            CulturalSignificance = CulturalSignificance.Sacred
        };

        // Assert
        plan.PlanId.Should().Be("vesak_2025_disaster_recovery");
        plan.DisasterType.Should().Be(CulturalDisasterType.RegionOutage);
        plan.AffectedRegions.Should().Contain("asia_pacific");
        plan.AffectedRegions.Should().Contain("north_america");
        plan.CulturalEventType.Should().Be(CulturalEventType.Vesak);
        plan.RecoveryTimeObjective.Should().Be(TimeSpan.FromSeconds(30));
        plan.RecoveryPointObjective.Should().Be(TimeSpan.FromSeconds(5));
        plan.CulturalSignificance.Should().Be(CulturalSignificance.Sacred);
    }

    [Theory]
    [InlineData("sri_lankan_buddhist", "north_america", CulturalEventType.BuddhistPoyaDay)]
    [InlineData("indian_hindu", "europe", CulturalEventType.Diwali)]
    [InlineData("pakistani_muslim", "australia", CulturalEventType.Eid)]
    [InlineData("sikh_punjabi", "canada", CulturalEventType.Vaisakhi)]
    public void CulturalFailoverRequest_WithDifferentCombinations_ShouldBeValid(
        string communityId, string region, CulturalEventType eventType)
    {
        // Act
        var request = new CulturalFailoverRequest
        {
            SourceRegion = region,
            TargetRegion = "north_america_backup",
            FailoverReason = $"Primary region failure during {eventType}",
            CulturalDataTypes = new List<CulturalDataType> { CulturalDataType.CalendarEvents },
            FailoverPriority = CulturalFailoverPriority.Sacred,
            CulturalContext = new CulturalContext
            {
                CommunityId = communityId,
                GeographicRegion = region
            }
        };

        // Assert
        request.SourceRegion.Should().Be(region);
        request.TargetRegion.Should().Be("north_america_backup");
        request.FailoverReason.Should().Contain(eventType.ToString());
        request.CulturalDataTypes.Should().Contain(CulturalDataType.CalendarEvents);
        request.FailoverPriority.Should().Be(CulturalFailoverPriority.Sacred);
        request.CulturalContext.Should().NotBeNull();
        request.CulturalContext.CommunityId.Should().Be(communityId);
        request.CulturalContext.GeographicRegion.Should().Be(region);
    }

    [Fact]
    public async Task ExecuteCulturalFailoverAsync_ShouldReturnResult_WhenCalled()
    {
        // Arrange
        var logger = Substitute.For<ILogger<CulturalIntelligenceFailoverOrchestrator>>();
        var options = Options.Create(new CulturalFailoverOptions());
        var consistencyService = Substitute.For<ICulturalIntelligenceConsistencyService>();
        var replicationService = Substitute.For<ICulturalStateReplicationService>();
        var orchestrator = new CulturalIntelligenceFailoverOrchestrator(
            logger, options, consistencyService, replicationService);

        var failoverRequest = new CulturalFailoverRequest
        {
            SourceRegion = "asia_pacific",
            TargetRegion = "north_america",
            FailoverReason = "Region outage during Vesak celebrations",
            FailoverPriority = CulturalFailoverPriority.Sacred,
            CulturalDataTypes = new List<CulturalDataType> { CulturalDataType.BuddhistCalendar }
        };

        // Act
        var result = await orchestrator.ExecuteCulturalFailoverAsync(failoverRequest, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task AssessCulturalImpactAsync_ShouldReturnAssessment_WhenCalled()
    {
        // Arrange
        var logger = Substitute.For<ILogger<CulturalIntelligenceFailoverOrchestrator>>();
        var options = Options.Create(new CulturalFailoverOptions());
        var consistencyService = Substitute.For<ICulturalIntelligenceConsistencyService>();
        var replicationService = Substitute.For<ICulturalStateReplicationService>();
        var orchestrator = new CulturalIntelligenceFailoverOrchestrator(
            logger, options, consistencyService, replicationService);

        var disasterScenario = new CulturalDisasterScenario
        {
            DisasterType = CulturalDisasterType.RegionOutage,
            AffectedRegions = new List<string> { "asia_pacific" },
            EstimatedImpactDuration = TimeSpan.FromHours(2),
            ConcurrentCulturalEvents = new List<CulturalEventType> { CulturalEventType.Vesak }
        };

        // Act
        var result = await orchestrator.AssessCulturalImpactAsync(disasterScenario, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void CulturalDisasterScenario_DefaultProperties_ShouldBeInitialized()
    {
        // Act
        var scenario = new CulturalDisasterScenario();

        // Assert
        scenario.ScenarioId.Should().NotBeNull();
        scenario.DisasterType.Should().Be(CulturalDisasterType.RegionOutage);
        scenario.AffectedRegions.Should().NotBeNull();
        scenario.EstimatedImpactDuration.Should().Be(TimeSpan.Zero);
        scenario.ConcurrentCulturalEvents.Should().NotBeNull();
        scenario.AffectedCommunities.Should().NotBeNull();
        scenario.EstimatedRevenueImpact.Should().Be(0.0);
        scenario.CulturalSignificanceLevel.Should().Be(CulturalSignificance.Medium);
        scenario.ScenarioTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void CulturalImpactAssessment_DefaultProperties_ShouldBeInitialized()
    {
        // Act
        var assessment = new CulturalImpactAssessment();

        // Assert
        assessment.AssessmentId.Should().NotBeNull();
        assessment.ImpactScore.Should().Be(0.0);
        assessment.AffectedCommunityCount.Should().Be(0);
        assessment.EstimatedRevenueImpact.Should().Be(0.0);
        assessment.CriticalCulturalEvents.Should().NotBeNull();
        assessment.RecommendedActions.Should().NotBeNull();
        assessment.FailoverPriorityRecommendation.Should().Be(CulturalFailoverPriority.Medium);
        assessment.AssessmentTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        assessment.CulturalMitigationStrategies.Should().NotBeNull();
    }

    [Theory]
    [InlineData(CulturalHealthStatus.Healthy, "Healthy")]
    [InlineData(CulturalHealthStatus.Degraded, "Degraded")]
    [InlineData(CulturalHealthStatus.Critical, "Critical")]
    [InlineData(CulturalHealthStatus.Failed, "Failed")]
    [InlineData(CulturalHealthStatus.Recovering, "Recovering")]
    public void CulturalHealthStatus_EnumValues_ShouldHaveCorrectNames(
        CulturalHealthStatus status, string expectedName)
    {
        // Act & Assert
        status.ToString().Should().Be(expectedName);
    }

    [Fact]
    public async Task MonitorCulturalFailoverHealthAsync_ShouldReturnStatus_WhenCalled()
    {
        // Arrange
        var logger = Substitute.For<ILogger<CulturalIntelligenceFailoverOrchestrator>>();
        var options = Options.Create(new CulturalFailoverOptions());
        var consistencyService = Substitute.For<ICulturalIntelligenceConsistencyService>();
        var replicationService = Substitute.For<ICulturalStateReplicationService>();
        var orchestrator = new CulturalIntelligenceFailoverOrchestrator(
            logger, options, consistencyService, replicationService);

        var regions = new List<string> { "north_america", "europe", "asia_pacific" };

        // Act
        var result = await orchestrator.MonitorCulturalFailoverHealthAsync(regions, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void CulturalFailoverHealthReport_DefaultProperties_ShouldBeInitialized()
    {
        // Act
        var report = new CulturalFailoverHealthReport();

        // Assert
        report.ReportId.Should().NotBeNull();
        report.OverallHealthStatus.Should().Be(CulturalHealthStatus.Healthy);
        report.RegionalHealthStatuses.Should().NotBeNull();
        report.CriticalIssues.Should().NotBeNull();
        report.FailoverReadinessScore.Should().Be(0.0);
        report.ReportTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        report.CulturalEventReadiness.Should().NotBeNull();
        report.DiasporaCommunityCoverage.Should().Be(0.0);
    }

    [Fact]
    public void CulturalFailoverConfiguration_DefaultProperties_ShouldBeValid()
    {
        // Act
        var config = new CulturalFailoverConfiguration
        {
            PrimaryRegion = "north_america",
            BackupRegions = new List<string> { "europe", "asia_pacific" },
            CulturalEventPriority = CulturalFailoverPriority.Sacred,
            MaxFailoverTime = TimeSpan.FromSeconds(30),
            CulturalDataTypes = new List<CulturalDataType> 
            { 
                CulturalDataType.BuddhistCalendar, 
                CulturalDataType.HinduCalendar,
                CulturalDataType.IslamicCalendar 
            }
        };

        // Assert
        config.PrimaryRegion.Should().Be("north_america");
        config.BackupRegions.Should().Contain("europe");
        config.BackupRegions.Should().Contain("asia_pacific");
        config.CulturalEventPriority.Should().Be(CulturalFailoverPriority.Sacred);
        config.MaxFailoverTime.Should().Be(TimeSpan.FromSeconds(30));
        config.CulturalDataTypes.Should().Contain(CulturalDataType.BuddhistCalendar);
        config.CulturalDataTypes.Should().Contain(CulturalDataType.HinduCalendar);
        config.CulturalDataTypes.Should().Contain(CulturalDataType.IslamicCalendar);
    }

    [Fact]
    public async Task CreateCulturalDisasterRecoveryPlanAsync_ShouldReturnPlan_WhenCalled()
    {
        // Arrange
        var logger = Substitute.For<ILogger<CulturalIntelligenceFailoverOrchestrator>>();
        var options = Options.Create(new CulturalFailoverOptions());
        var consistencyService = Substitute.For<ICulturalIntelligenceConsistencyService>();
        var replicationService = Substitute.For<ICulturalStateReplicationService>();
        var orchestrator = new CulturalIntelligenceFailoverOrchestrator(
            logger, options, consistencyService, replicationService);

        var scenario = new CulturalDisasterScenario
        {
            DisasterType = CulturalDisasterType.RegionOutage,
            AffectedRegions = new List<string> { "asia_pacific" },
            ConcurrentCulturalEvents = new List<CulturalEventType> { CulturalEventType.Vesak },
            CulturalSignificanceLevel = CulturalSignificance.Sacred
        };

        // Act
        var result = await orchestrator.CreateCulturalDisasterRecoveryPlanAsync(scenario, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Theory]
    [InlineData(CulturalFailoverTrigger.ManualActivation, "ManualActivation")]
    [InlineData(CulturalFailoverTrigger.AutomaticDetection, "AutomaticDetection")]
    [InlineData(CulturalFailoverTrigger.SacredEventProtection, "SacredEventProtection")]
    [InlineData(CulturalFailoverTrigger.PerformanceThreshold, "PerformanceThreshold")]
    [InlineData(CulturalFailoverTrigger.RegionHealthCheck, "RegionHealthCheck")]
    public void CulturalFailoverTrigger_EnumValues_ShouldHaveCorrectNames(
        CulturalFailoverTrigger trigger, string expectedName)
    {
        // Act & Assert
        trigger.ToString().Should().Be(expectedName);
    }

    [Fact]
    public void CulturalFailoverMetrics_DefaultProperties_ShouldBeInitialized()
    {
        // Act
        var metrics = new CulturalFailoverMetrics();

        // Assert
        metrics.MetricsId.Should().NotBeNull();
        metrics.TotalFailoverExecutions.Should().Be(0);
        metrics.SuccessfulFailovers.Should().Be(0);
        metrics.FailedFailovers.Should().Be(0);
        metrics.AverageFailoverTime.Should().Be(TimeSpan.Zero);
        metrics.SacredEventFailoverSuccessRate.Should().Be(0.0);
        metrics.DiasporaCommunityImpactScore.Should().Be(0.0);
        metrics.RevenueProtectionEffectiveness.Should().Be(0.0);
        metrics.MetricsCollectionTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetCulturalFailoverMetricsAsync_ShouldReturnMetrics_WhenCalled()
    {
        // Arrange
        var logger = Substitute.For<ILogger<CulturalIntelligenceFailoverOrchestrator>>();
        var options = Options.Create(new CulturalFailoverOptions());
        var consistencyService = Substitute.For<ICulturalIntelligenceConsistencyService>();
        var replicationService = Substitute.For<ICulturalStateReplicationService>();
        var orchestrator = new CulturalIntelligenceFailoverOrchestrator(
            logger, options, consistencyService, replicationService);

        // Act
        var result = await orchestrator.GetCulturalFailoverMetricsAsync(
            TimeSpan.FromDays(30), CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void RegionalCulturalBackupStatus_DefaultProperties_ShouldBeValid()
    {
        // Act
        var status = new RegionalCulturalBackupStatus
        {
            Region = "north_america",
            BackupHealthStatus = CulturalHealthStatus.Healthy,
            LastBackupTimestamp = DateTime.UtcNow.AddMinutes(-30),
            CulturalDataIntegrity = 0.99,
            BuddhistCalendarSyncStatus = true,
            HinduCalendarSyncStatus = true,
            IslamicCalendarSyncStatus = true,
            DiasporaCommunityCoverage = 0.95
        };

        // Assert
        status.Region.Should().Be("north_america");
        status.BackupHealthStatus.Should().Be(CulturalHealthStatus.Healthy);
        status.LastBackupTimestamp.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(-30), TimeSpan.FromMinutes(1));
        status.CulturalDataIntegrity.Should().Be(0.99);
        status.BuddhistCalendarSyncStatus.Should().BeTrue();
        status.HinduCalendarSyncStatus.Should().BeTrue();
        status.IslamicCalendarSyncStatus.Should().BeTrue();
        status.DiasporaCommunityCoverage.Should().Be(0.95);
    }

    [Fact]
    public async Task ValidateFailoverReadinessAsync_ShouldReturnValidation_WhenCalled()
    {
        // Arrange
        var logger = Substitute.For<ILogger<CulturalIntelligenceFailoverOrchestrator>>();
        var options = Options.Create(new CulturalFailoverOptions());
        var consistencyService = Substitute.For<ICulturalIntelligenceConsistencyService>();
        var replicationService = Substitute.For<ICulturalStateReplicationService>();
        var orchestrator = new CulturalIntelligenceFailoverOrchestrator(
            logger, options, consistencyService, replicationService);

        var configuration = new CulturalFailoverConfiguration
        {
            PrimaryRegion = "north_america",
            BackupRegions = new List<string> { "europe", "asia_pacific" },
            CulturalEventPriority = CulturalFailoverPriority.Sacred
        };

        // Act
        var result = await orchestrator.ValidateFailoverReadinessAsync(configuration, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }
}