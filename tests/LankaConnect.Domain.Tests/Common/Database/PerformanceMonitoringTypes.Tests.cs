using Xunit;
using FluentAssertions;
using LankaConnect.Domain.Common.Database;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LankaConnect.Domain.Tests.Common.Database;

/// <summary>
/// TDD RED Phase: Tests for Performance Monitoring Types
/// Testing cultural intelligence multi-region performance coordination
/// </summary>
public class PerformanceMonitoringTypesTests
{
    #region MultiRegionPerformanceCoordination Tests

    [Fact]
    public void MultiRegionPerformanceCoordination_Create_ValidInput_ShouldSucceed()
    {
        // Arrange
        var coordinationId = Guid.NewGuid();
        var regions = new List<string> { "US-East", "EU-West", "Asia-Pacific" };
        var strategy = CoordinationStrategy.LoadBalanced;

        // Act
        var result = MultiRegionPerformanceCoordination.Create(coordinationId, regions, strategy);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(coordinationId);
        result.Value.Regions.Should().BeEquivalentTo(regions);
        result.Value.Strategy.Should().Be(strategy);
        result.Value.IsActive.Should().BeTrue();
        result.Value.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MultiRegionPerformanceCoordination_Create_EmptyRegions_ShouldFail()
    {
        // Arrange
        var coordinationId = Guid.NewGuid();
        var regions = new List<string>();
        var strategy = CoordinationStrategy.LoadBalanced;

        // Act
        var result = MultiRegionPerformanceCoordination.Create(coordinationId, regions, strategy);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("regions");
    }

    [Fact]
    public void MultiRegionPerformanceCoordination_AddRegion_ValidRegion_ShouldSucceed()
    {
        // Arrange
        var coordination = CreateValidCoordination();
        var newRegion = "South-America";

        // Act
        var result = coordination.AddRegion(newRegion);

        // Assert
        result.IsSuccess.Should().BeTrue();
        coordination.Regions.Should().Contain(newRegion);
    }

    [Fact]
    public void MultiRegionPerformanceCoordination_GetCulturalLoadDistribution_ShouldReturnDistribution()
    {
        // Arrange
        var coordination = CreateValidCoordination();
        var eventType = "Festival";

        // Act
        var distribution = coordination.GetCulturalLoadDistribution(eventType);

        // Assert
        distribution.Should().NotBeNull();
        distribution.EventType.Should().Be(eventType);
        distribution.RegionLoads.Should().NotBeEmpty();
    }

    #endregion

    #region SynchronizationPolicy Tests

    [Fact]
    public void SynchronizationPolicy_Create_ValidInput_ShouldSucceed()
    {
        // Arrange
        var policyId = Guid.NewGuid();
        var policyName = "CulturalEventSync";
        var syncType = SynchronizationType.RealTime;
        var priority = SyncPriority.High;

        // Act
        var result = SynchronizationPolicy.Create(policyId, policyName, syncType, priority);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(policyId);
        result.Value.Name.Should().Be(policyName);
        result.Value.SyncType.Should().Be(syncType);
        result.Value.Priority.Should().Be(priority);
        result.Value.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void SynchronizationPolicy_Create_EmptyName_ShouldFail()
    {
        // Arrange
        var policyId = Guid.NewGuid();
        var policyName = "";
        var syncType = SynchronizationType.RealTime;
        var priority = SyncPriority.High;

        // Act
        var result = SynchronizationPolicy.Create(policyId, policyName, syncType, priority);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("name");
    }

    [Fact]
    public void SynchronizationPolicy_SetCulturalEventPriority_ShouldUpdatePriority()
    {
        // Arrange
        var policy = CreateValidSyncPolicy();
        var eventType = "ReligiousCeremony";
        var priority = SyncPriority.Critical;

        // Act
        policy.SetCulturalEventPriority(eventType, priority);

        // Assert
        policy.CulturalEventPriorities[eventType].Should().Be(priority);
    }

    #endregion

    #region RegionSyncResult Tests

    [Fact]
    public void RegionSyncResult_Create_ValidInput_ShouldSucceed()
    {
        // Arrange
        var syncId = Guid.NewGuid();
        var sourceRegion = "US-East";
        var targetRegion = "EU-West";
        var status = SyncStatus.Success;
        var syncDuration = TimeSpan.FromSeconds(30);

        // Act
        var result = RegionSyncResult.Create(syncId, sourceRegion, targetRegion, status, syncDuration);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SyncId.Should().Be(syncId);
        result.Value.SourceRegion.Should().Be(sourceRegion);
        result.Value.TargetRegion.Should().Be(targetRegion);
        result.Value.Status.Should().Be(status);
        result.Value.SyncDuration.Should().Be(syncDuration);
    }

    [Fact]
    public void RegionSyncResult_Create_InvalidSyncDuration_ShouldFail()
    {
        // Arrange
        var syncId = Guid.NewGuid();
        var sourceRegion = "US-East";
        var targetRegion = "EU-West";
        var status = SyncStatus.Success;
        var syncDuration = TimeSpan.FromSeconds(-1);

        // Act
        var result = RegionSyncResult.Create(syncId, sourceRegion, targetRegion, status, syncDuration);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("duration");
    }

    [Fact]
    public void RegionSyncResult_AddCulturalDataMetrics_ShouldUpdateMetrics()
    {
        // Arrange
        var syncResult = CreateValidSyncResult();
        var eventType = "Festival";
        var recordCount = 1500;

        // Act
        syncResult.AddCulturalDataMetrics(eventType, recordCount);

        // Assert
        syncResult.CulturalDataMetrics[eventType].Should().Be(recordCount);
    }

    #endregion

    #region PerformanceComparisonMetrics Tests

    [Fact]
    public void PerformanceComparisonMetrics_Create_ValidInput_ShouldSucceed()
    {
        // Arrange
        var metricsId = Guid.NewGuid();
        var baselineRegion = "US-East";
        var comparisonRegions = new List<string> { "EU-West", "Asia-Pacific" };

        // Act
        var result = PerformanceComparisonMetrics.Create(metricsId, baselineRegion, comparisonRegions);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(metricsId);
        result.Value.BaselineRegion.Should().Be(baselineRegion);
        result.Value.ComparisonRegions.Should().BeEquivalentTo(comparisonRegions);
    }

    [Fact]
    public void PerformanceComparisonMetrics_AddCulturalMetric_ShouldStoreMetric()
    {
        // Arrange
        var metrics = CreateValidComparisonMetrics();
        var culturalEventType = "Wedding";
        var metricName = "ResponseTime";
        var metricValue = 250.5;

        // Act
        metrics.AddCulturalMetric(culturalEventType, metricName, metricValue);

        // Assert
        metrics.CulturalMetrics[culturalEventType][metricName].Should().Be(metricValue);
    }

    #endregion

    #region Helper Methods

    private MultiRegionPerformanceCoordination CreateValidCoordination()
    {
        var regions = new List<string> { "US-East", "EU-West", "Asia-Pacific" };
        return MultiRegionPerformanceCoordination.Create(Guid.NewGuid(), regions, CoordinationStrategy.LoadBalanced).Value;
    }

    private SynchronizationPolicy CreateValidSyncPolicy()
    {
        return SynchronizationPolicy.Create(Guid.NewGuid(), "TestPolicy", SynchronizationType.RealTime, SyncPriority.High).Value;
    }

    private RegionSyncResult CreateValidSyncResult()
    {
        return RegionSyncResult.Create(Guid.NewGuid(), "US-East", "EU-West", SyncStatus.Success, TimeSpan.FromSeconds(30)).Value;
    }

    private PerformanceComparisonMetrics CreateValidComparisonMetrics()
    {
        var comparisonRegions = new List<string> { "EU-West", "Asia-Pacific" };
        return PerformanceComparisonMetrics.Create(Guid.NewGuid(), "US-East", comparisonRegions).Value;
    }

    #endregion
}