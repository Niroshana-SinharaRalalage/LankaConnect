using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using LankaConnect.Application.Common.Models.Routing;
using LankaConnect.Domain.Common.Monitoring;
using Xunit;

namespace LankaConnect.Application.Tests.Common.Routing;

/// <summary>
/// TDD RED Phase: Comprehensive tests for Routing Foundation Types
/// Testing RoutingFailureContext, RoutingFallbackStrategy, DisasterRecoveryFailoverContext
/// </summary>
public class RoutingTypesTests
{
    #region RoutingFailureContext Tests (RED Phase)

    [Fact]
    public void RoutingFailureContext_Create_ShouldValidateFailureContext()
    {
        // Arrange
        var routingId = "route-cultural-vesak-2024";
        var failureReason = "Cultural event traffic overflow";
        var affectedRegions = new[] { "Colombo", "Kandy", "Galle" };
        var failureType = RoutingFailureType.CulturalEventOverload;
        var culturalContext = "Vesak Day peak traffic";

        // Act
        var context = RoutingFailureContext.Create(routingId, failureReason, affectedRegions, failureType, culturalContext);

        // Assert
        context.Should().NotBeNull();
        context.RoutingId.Should().Be(routingId);
        context.FailureReason.Should().Be(failureReason);
        context.AffectedRegions.Should().HaveCount(3);
        context.FailureType.Should().Be(failureType);
        context.CulturalContext.Should().Be(culturalContext);
        context.FailureDetectedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        context.IsCulturalEventRelated.Should().BeTrue();
        context.RequiresImmediateFailover.Should().BeTrue();
    }

    [Fact]
    public void RoutingFailureContext_Create_ShouldHandleStandardFailure()
    {
        // Arrange
        var routingId = "route-database-001";
        var failureReason = "Database connection timeout";
        var affectedRegions = new[] { "US-East" };
        var failureType = RoutingFailureType.DatabaseUnavailable;

        // Act
        var context = RoutingFailureContext.Create(routingId, failureReason, affectedRegions, failureType);

        // Assert
        context.Should().NotBeNull();
        context.RoutingId.Should().Be(routingId);
        context.FailureType.Should().Be(failureType);
        context.CulturalContext.Should().BeNull();
        context.IsCulturalEventRelated.Should().BeFalse();
        context.RequiresImmediateFailover.Should().BeTrue(); // Database failures are critical
    }

    [Theory]
    [InlineData(RoutingFailureType.CulturalEventOverload, true)]
    [InlineData(RoutingFailureType.DatabaseUnavailable, true)]
    [InlineData(RoutingFailureType.NetworkLatency, false)]
    [InlineData(RoutingFailureType.ServiceDegradation, false)]
    public void RoutingFailureContext_RequiresImmediateFailover_ShouldReturnCorrectValue(RoutingFailureType failureType, bool expected)
    {
        // Arrange & Act
        var context = RoutingFailureContext.Create("test-route", "Test failure", new[] { "region1" }, failureType);

        // Assert
        context.RequiresImmediateFailover.Should().Be(expected);
    }

    #endregion

    #region RoutingFallbackStrategy Tests (RED Phase)

    [Fact]
    public void RoutingFallbackStrategy_CreateCulturalAware_ShouldValidateStrategy()
    {
        // Arrange
        var strategyName = "Cultural Event Fallback Strategy";
        var primaryRoutes = new[] { "cultural-primary-colombo", "cultural-primary-kandy" };
        var fallbackRoutes = new[] { "cultural-backup-galle", "cultural-backup-jaffna" };
        var culturalEventTypes = new[] { CulturalEventType.ReligiousFestival, CulturalEventType.NationalHoliday };
        var failoverThreshold = TimeSpan.FromSeconds(500); // 500ms for cultural events

        // Act
        var strategy = RoutingFallbackStrategy.CreateCulturalAware(strategyName, primaryRoutes, fallbackRoutes, 
            culturalEventTypes, failoverThreshold);

        // Assert
        strategy.Should().NotBeNull();
        strategy.StrategyName.Should().Be(strategyName);
        strategy.PrimaryRoutes.Should().HaveCount(2);
        strategy.FallbackRoutes.Should().HaveCount(2);
        strategy.SupportedCulturalEvents.Should().HaveCount(2);
        strategy.FailoverThreshold.Should().Be(failoverThreshold);
        strategy.IsCulturallyAware.Should().BeTrue();
        strategy.IsActive.Should().BeTrue();
        strategy.MaxFailoverTime.Should().BeLessOrEqualTo(TimeSpan.FromSeconds(600)); // Fast cultural failover
    }

    [Fact]
    public void RoutingFallbackStrategy_CreateStandard_ShouldValidateStrategy()
    {
        // Arrange
        var strategyName = "Standard Database Fallback";
        var primaryRoutes = new[] { "db-primary-cluster1" };
        var fallbackRoutes = new[] { "db-backup-cluster2", "db-backup-cluster3" };
        var failoverThreshold = TimeSpan.FromSeconds(2000); // 2s for standard operations

        // Act
        var strategy = RoutingFallbackStrategy.CreateStandard(strategyName, primaryRoutes, fallbackRoutes, failoverThreshold);

        // Assert
        strategy.Should().NotBeNull();
        strategy.StrategyName.Should().Be(strategyName);
        strategy.PrimaryRoutes.Should().HaveCount(1);
        strategy.FallbackRoutes.Should().HaveCount(2);
        strategy.SupportedCulturalEvents.Should().BeEmpty();
        strategy.IsCulturallyAware.Should().BeFalse();
        strategy.FailoverThreshold.Should().Be(failoverThreshold);
        strategy.MaxFailoverTime.Should().BeGreaterThan(TimeSpan.FromSeconds(1500));
    }

    [Fact]
    public void RoutingFallbackStrategy_ShouldTriggerFailover_ShouldReturnCorrectDecision()
    {
        // Arrange
        var strategy = RoutingFallbackStrategy.CreateStandard("Test Strategy", 
            new[] { "primary" }, new[] { "backup" }, TimeSpan.FromSeconds(1));
        var context = RoutingFailureContext.Create("test-route", "High latency", 
            new[] { "region1" }, RoutingFailureType.NetworkLatency);

        // Act
        var shouldFailover = strategy.ShouldTriggerFailover(context);

        // Assert
        shouldFailover.Should().BeFalse(); // NetworkLatency shouldn't trigger immediate failover
    }

    #endregion

    #region DisasterRecoveryFailoverContext Tests (RED Phase)

    [Fact]
    public void DisasterRecoveryFailoverContext_Create_ShouldValidateFailoverContext()
    {
        // Arrange
        var disasterId = "disaster-cultural-ddos-001";
        var disasterType = DisasterType.CulturalEventDDoS;
        var affectedServices = new[] { "cultural-intelligence", "user-matching", "community-clustering" };
        var recoveryStrategy = RecoveryStrategy.CulturalEventLoadDistribution;
        var targetRegion = "Asia-Pacific-Cultural-Backup";
        var culturalContext = "Diwali celebration traffic spike";

        // Act
        var context = DisasterRecoveryFailoverContext.Create(disasterId, disasterType, affectedServices, 
            recoveryStrategy, targetRegion, culturalContext);

        // Assert
        context.Should().NotBeNull();
        context.DisasterId.Should().Be(disasterId);
        context.DisasterType.Should().Be(disasterType);
        context.AffectedServices.Should().HaveCount(3);
        context.RecoveryStrategy.Should().Be(recoveryStrategy);
        context.TargetRegion.Should().Be(targetRegion);
        context.CulturalContext.Should().Be(culturalContext);
        context.TriggeredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        context.IsCulturalDisaster.Should().BeTrue();
        context.RequiresSpecializedRecovery.Should().BeTrue();
    }

    [Fact]
    public void DisasterRecoveryFailoverContext_Create_ShouldHandleStandardDisaster()
    {
        // Arrange
        var disasterId = "disaster-datacenter-fire-001";
        var disasterType = DisasterType.DataCenterFailure;
        var affectedServices = new[] { "database", "api-gateway" };
        var recoveryStrategy = RecoveryStrategy.StandardFailover;
        var targetRegion = "US-West-Backup";

        // Act
        var context = DisasterRecoveryFailoverContext.Create(disasterId, disasterType, affectedServices, 
            recoveryStrategy, targetRegion);

        // Assert
        context.Should().NotBeNull();
        context.DisasterType.Should().Be(disasterType);
        context.RecoveryStrategy.Should().Be(recoveryStrategy);
        context.CulturalContext.Should().BeNull();
        context.IsCulturalDisaster.Should().BeFalse();
        context.RequiresSpecializedRecovery.Should().BeTrue(); // DataCenter failure is specialized
    }

    [Theory]
    [InlineData(DisasterType.CulturalEventDDoS, true)]
    [InlineData(DisasterType.DataCenterFailure, true)]
    [InlineData(DisasterType.NetworkPartition, false)]
    [InlineData(DisasterType.ServiceDegradation, false)]
    public void DisasterRecoveryFailoverContext_RequiresSpecializedRecovery_ShouldReturnCorrectValue(DisasterType disasterType, bool expected)
    {
        // Arrange & Act
        var context = DisasterRecoveryFailoverContext.Create("test-disaster", disasterType, 
            new[] { "service1" }, RecoveryStrategy.StandardFailover, "backup-region");

        // Assert
        context.RequiresSpecializedRecovery.Should().Be(expected);
    }

    [Fact]
    public void DisasterRecoveryFailoverContext_EstimateRecoveryTime_ShouldReturnCulturalAwareEstimate()
    {
        // Arrange
        var culturalContext = DisasterRecoveryFailoverContext.Create("cultural-disaster", 
            DisasterType.CulturalEventDDoS, new[] { "cultural-service" }, 
            RecoveryStrategy.CulturalEventLoadDistribution, "cultural-backup", "Festival overload");

        var standardContext = DisasterRecoveryFailoverContext.Create("standard-disaster", 
            DisasterType.DataCenterFailure, new[] { "database" }, 
            RecoveryStrategy.StandardFailover, "standard-backup");

        // Act
        var culturalRecoveryTime = culturalContext.EstimateRecoveryTime();
        var standardRecoveryTime = standardContext.EstimateRecoveryTime();

        // Assert
        culturalRecoveryTime.Should().BeLessOrEqualTo(TimeSpan.FromMinutes(5)); // Cultural events need fast recovery
        standardRecoveryTime.Should().BeGreaterThan(TimeSpan.FromMinutes(10)); // Standard disasters take longer
    }

    #endregion
}

/// <summary>
/// Supporting enums and helpers for routing tests
/// </summary>
public static class RoutingTestHelpers
{
    public static RoutingFailureContext CreateCulturalFailure()
    {
        return RoutingFailureContext.Create(
            "cultural-routing-failure-001",
            "Vesak Day traffic surge exceeding cultural intelligence capacity",
            new[] { "Colombo", "Kandy", "Anuradhapura" },
            RoutingFailureType.CulturalEventOverload,
            "Vesak Day celebration peak hours");
    }

    public static RoutingFallbackStrategy CreateDefaultCulturalStrategy()
    {
        return RoutingFallbackStrategy.CreateCulturalAware(
            "Default Cultural Event Fallback",
            new[] { "cultural-primary-sri-lanka", "cultural-primary-india" },
            new[] { "cultural-backup-singapore", "cultural-backup-australia" },
            new[] { CulturalEventType.ReligiousFestival, CulturalEventType.NationalHoliday },
            TimeSpan.FromSeconds(300));
    }

    public static DisasterRecoveryFailoverContext CreateCulturalDisaster()
    {
        return DisasterRecoveryFailoverContext.Create(
            "cultural-disaster-diwali-001",
            DisasterType.CulturalEventDDoS,
            new[] { "cultural-intelligence", "user-matching", "event-clustering" },
            RecoveryStrategy.CulturalEventLoadDistribution,
            "Asia-Pacific-Cultural-Resilience-Zone",
            "Diwali celebration coordinated across multiple diaspora communities");
    }
}