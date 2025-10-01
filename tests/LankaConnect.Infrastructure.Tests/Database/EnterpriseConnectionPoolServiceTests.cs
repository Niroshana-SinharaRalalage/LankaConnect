using FluentAssertions;
using Xunit;
using LankaConnect.Infrastructure.Database.ConnectionPooling;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using System.Data;

namespace LankaConnect.Infrastructure.Tests.Database;

public class EnterpriseConnectionPoolServiceTests
{
    [Fact]
    public void EnterpriseConnectionPoolService_CanBeConstructed_WithValidParameters()
    {
        // Arrange
        var logger = Substitute.For<ILogger<EnterpriseConnectionPoolService>>();
        var options = Options.Create(new ConnectionPoolOptions());
        var shardingService = Substitute.For<ICulturalIntelligenceShardingService>();

        // Act
        var service = new EnterpriseConnectionPoolService(logger, options, shardingService);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public void ConnectionPoolOptions_HasDefaultValues_WhenCreated()
    {
        // Arrange & Act
        var options = new ConnectionPoolOptions();

        // Assert
        options.MaxConnectionsPerPool.Should().Be(100);
        options.MinConnectionsPerPool.Should().Be(10);
        options.ConnectionLifetime.Should().Be(TimeSpan.FromMinutes(30));
        options.HealthCheckInterval.Should().Be(TimeSpan.FromMinutes(5));
        options.MaxPoolsPerRegion.Should().Be(5);
        options.EnableCulturalIntelligenceRouting.Should().BeTrue();
        options.ConnectionTimeout.Should().Be(TimeSpan.FromSeconds(30));
        options.PoolEfficiencyThreshold.Should().Be(0.95);
    }

    [Theory]
    [InlineData(DatabaseOperationType.Write, "Write")]
    [InlineData(DatabaseOperationType.Read, "Read")]
    [InlineData(DatabaseOperationType.Analytics, "Analytics")]
    [InlineData(DatabaseOperationType.Migration, "Migration")]
    public void DatabaseOperationType_EnumValues_ShouldHaveCorrectNames(DatabaseOperationType operationType, string expectedName)
    {
        // Act & Assert
        operationType.ToString().Should().Be(expectedName);
    }

    [Fact]
    public void CulturalConnectionPoolKey_DefaultProperties_ShouldBeInitialized()
    {
        // Act
        var poolKey = new CulturalConnectionPoolKey();

        // Assert
        poolKey.PoolId.Should().NotBeNull();
        poolKey.CommunityGroup.Should().NotBeNull();
        poolKey.GeographicRegion.Should().NotBeNull();
        poolKey.OperationType.Should().Be(DatabaseOperationType.Read);
        poolKey.LoadBalancingWeight.Should().Be(1.0);
        poolKey.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ConnectionPoolMetrics_DefaultProperties_ShouldBeInitialized()
    {
        // Act
        var metrics = new ConnectionPoolMetrics();

        // Assert
        metrics.PoolId.Should().NotBeNull();
        metrics.ActiveConnections.Should().Be(0);
        metrics.IdleConnections.Should().Be(0);
        metrics.PendingRequests.Should().Be(0);
        metrics.TotalConnectionsCreated.Should().Be(0);
        metrics.TotalConnectionsClosed.Should().Be(0);
        metrics.AverageConnectionAcquisitionTime.Should().Be(TimeSpan.Zero);
        metrics.PoolEfficiency.Should().Be(0.0);
        metrics.LastHealthCheck.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(PoolHealthStatus.Healthy, "Healthy")]
    [InlineData(PoolHealthStatus.Warning, "Warning")]
    [InlineData(PoolHealthStatus.Critical, "Critical")]
    [InlineData(PoolHealthStatus.Failed, "Failed")]
    [InlineData(PoolHealthStatus.Maintenance, "Maintenance")]
    public void PoolHealthStatus_EnumValues_ShouldHaveCorrectNames(PoolHealthStatus status, string expectedName)
    {
        // Act & Assert
        status.ToString().Should().Be(expectedName);
    }

    [Fact]
    public void PerformanceTarget_DefaultProperties_ShouldBeInitialized()
    {
        // Act
        var target = new PerformanceTarget();

        // Assert
        target.MaxConnectionAcquisitionTime.Should().Be(TimeSpan.FromMilliseconds(5));
        target.MinPoolEfficiency.Should().Be(0.95);
        target.MaxFailoverTime.Should().Be(TimeSpan.FromMilliseconds(100));
        target.MaxMemoryPerThousandConnections.Should().Be(50);
        target.TargetThroughputPerSecond.Should().Be(10000);
        target.MaxConcurrentUsers.Should().Be(1000000);
    }

    [Fact]
    public void ConnectionPoolConfiguration_DefaultProperties_ShouldBeValid()
    {
        // Act
        var configuration = new ConnectionPoolConfiguration
        {
            PoolName = "sri_lankan_buddhist_north_america",
            CommunityGroup = "sri_lankan_buddhist",
            GeographicRegion = "north_america",
            MaxConnections = 150,
            MinConnections = 25,
            ConnectionLifetime = TimeSpan.FromMinutes(20),
            EnableLoadBalancing = true
        };

        // Assert
        configuration.PoolName.Should().Be("sri_lankan_buddhist_north_america");
        configuration.CommunityGroup.Should().Be("sri_lankan_buddhist");
        configuration.GeographicRegion.Should().Be("north_america");
        configuration.MaxConnections.Should().Be(150);
        configuration.MinConnections.Should().Be(25);
        configuration.ConnectionLifetime.Should().Be(TimeSpan.FromMinutes(20));
        configuration.EnableLoadBalancing.Should().BeTrue();
    }

    [Theory]
    [InlineData("sri_lankan_buddhist", "north_america", DatabaseOperationType.Write)]
    [InlineData("indian_hindu", "europe", DatabaseOperationType.Read)]
    [InlineData("pakistani_islamic", "australia", DatabaseOperationType.Analytics)]
    [InlineData("sikh_punjabi", "canada", DatabaseOperationType.Migration)]
    public void CulturalConnectionPoolKey_WithDifferentCombinations_ShouldBeValid(
        string communityGroup, string region, DatabaseOperationType operationType)
    {
        // Act
        var poolKey = new CulturalConnectionPoolKey
        {
            PoolId = Guid.NewGuid().ToString(),
            CommunityGroup = communityGroup,
            GeographicRegion = region,
            OperationType = operationType,
            LoadBalancingWeight = 0.85
        };

        // Assert
        poolKey.CommunityGroup.Should().Be(communityGroup);
        poolKey.GeographicRegion.Should().Be(region);
        poolKey.OperationType.Should().Be(operationType);
        poolKey.LoadBalancingWeight.Should().Be(0.85);
    }

    [Fact]
    public async Task GetOptimizedConnectionAsync_ShouldReturnResult_WhenCalled()
    {
        // Arrange
        var logger = Substitute.For<ILogger<EnterpriseConnectionPoolService>>();
        var options = Options.Create(new ConnectionPoolOptions());
        var shardingService = Substitute.For<ICulturalIntelligenceShardingService>();
        var service = new EnterpriseConnectionPoolService(logger, options, shardingService);

        var culturalContext = new CulturalContext
        {
            CommunityId = "sri_lankan_buddhist",
            GeographicRegion = "north_america",
            CulturalPreferences = new Dictionary<string, string>
            {
                ["calendar_type"] = "buddhist",
                ["language"] = "sinhala"
            }
        };

        // Act
        var result = await service.GetOptimizedConnectionAsync(
            culturalContext, 
            DatabaseOperationType.Read, 
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task GetPoolHealthMetricsAsync_ShouldReturnMetrics_WhenCalled()
    {
        // Arrange
        var logger = Substitute.For<ILogger<EnterpriseConnectionPoolService>>();
        var options = Options.Create(new ConnectionPoolOptions());
        var shardingService = Substitute.For<ICulturalIntelligenceShardingService>();
        var service = new EnterpriseConnectionPoolService(logger, options, shardingService);

        // Act
        var result = await service.GetPoolHealthMetricsAsync(CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task OptimizePoolConfigurationAsync_ShouldReturnResult_WhenCalled()
    {
        // Arrange
        var logger = Substitute.For<ILogger<EnterpriseConnectionPoolService>>();
        var options = Options.Create(new ConnectionPoolOptions());
        var shardingService = Substitute.For<ICulturalIntelligenceShardingService>();
        var service = new EnterpriseConnectionPoolService(logger, options, shardingService);

        var performanceTarget = new PerformanceTarget
        {
            MaxConnectionAcquisitionTime = TimeSpan.FromMilliseconds(3),
            MinPoolEfficiency = 0.98,
            MaxConcurrentUsers = 1500000
        };

        // Act
        var result = await service.OptimizePoolConfigurationAsync(performanceTarget, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void ConnectionPoolHealth_DefaultProperties_ShouldBeInitialized()
    {
        // Act
        var health = new ConnectionPoolHealth();

        // Assert
        health.PoolId.Should().NotBeNull();
        health.Status.Should().Be(PoolHealthStatus.Healthy);
        health.HealthScore.Should().Be(1.0);
        health.PerformanceIssues.Should().NotBeNull();
        health.RecommendedActions.Should().NotBeNull();
        health.LastHealthCheck.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void CulturalConnectionRoutingResult_DefaultProperties_ShouldBeValid()
    {
        // Act
        var result = new CulturalConnectionRoutingResult
        {
            SelectedPoolId = "buddhist_sri_lanka_write_pool",
            RoutingReason = "Optimal cultural context matching for Buddhist calendar operations",
            EstimatedConnectionAcquisitionTime = TimeSpan.FromMilliseconds(2),
            LoadBalancingScore = 0.92
        };

        // Assert
        result.SelectedPoolId.Should().Be("buddhist_sri_lanka_write_pool");
        result.RoutingReason.Should().Be("Optimal cultural context matching for Buddhist calendar operations");
        result.EstimatedConnectionAcquisitionTime.Should().Be(TimeSpan.FromMilliseconds(2));
        result.LoadBalancingScore.Should().Be(0.92);
    }

    [Theory]
    [InlineData(ConnectionPoolOptimizationStrategy.Conservative, "Conservative")]
    [InlineData(ConnectionPoolOptimizationStrategy.Balanced, "Balanced")]
    [InlineData(ConnectionPoolOptimizationStrategy.Aggressive, "Aggressive")]
    [InlineData(ConnectionPoolOptimizationStrategy.CulturallyIntelligent, "CulturallyIntelligent")]
    public void ConnectionPoolOptimizationStrategy_EnumValues_ShouldHaveCorrectNames(
        ConnectionPoolOptimizationStrategy strategy, string expectedName)
    {
        // Act & Assert
        strategy.ToString().Should().Be(expectedName);
    }
}