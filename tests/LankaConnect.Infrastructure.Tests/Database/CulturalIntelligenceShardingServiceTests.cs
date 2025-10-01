using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FluentAssertions;
using Xunit;
using NSubstitute;
using LankaConnect.Infrastructure.Database.Sharding;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;

namespace LankaConnect.Infrastructure.Tests.Database;

public class CulturalIntelligenceShardingServiceTests
{
    private readonly ILogger<CulturalIntelligenceShardingService> _logger;
    private readonly IOptions<DatabaseShardingOptions> _shardingOptions;
    private readonly CulturalIntelligenceShardingService _sut;

    public CulturalIntelligenceShardingServiceTests()
    {
        _logger = Substitute.For<ILogger<CulturalIntelligenceShardingService>>();
        var options = new DatabaseShardingOptions
        {
            EnableSharding = true,
            MaxShardsPerRegion = 8,
            CulturalCommunitySharding = true,
            GeographicSharding = true,
            ShardingStrategy = ShardingStrategy.CulturalIntelligence
        };
        _shardingOptions = Options.Create(options);
        _sut = new CulturalIntelligenceShardingService(_logger, _shardingOptions);
    }

    [Fact]
    public async Task DetermineShardKey_WithCulturalContext_ShouldReturnCorrectShard()
    {
        // Arrange
        var culturalContext = new CulturalContext
        {
            CommunityId = "buddhist_sri_lanka",
            GeographicRegion = "north_america",
            Language = "si"
        };
        var dataType = CulturalDataType.CalendarEvents;

        // Act
        var result = await _sut.DetermineShardKeyAsync(culturalContext, dataType);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.ShardId.Should().NotBeEmpty();
        result.Value.Region.Should().Be("north_america");
        result.Value.CommunityGroup.Should().Be("buddhist_sri_lanka");
        result.Value.ShardingReason.Should().Contain("CulturalIntelligence");
    }

    [Theory]
    [InlineData("buddhist_sri_lanka", "north_america", "si")]
    [InlineData("hindu_indian", "europe", "hi")]
    [InlineData("islamic_pakistani", "australia", "ur")]
    [InlineData("sikh_punjabi", "asia_pacific", "pa")]
    public async Task DetermineShardKey_WithDifferentCulturalContexts_ShouldDistributeCorrectly(
        string communityId, string region, string language)
    {
        // Arrange
        var culturalContext = new CulturalContext
        {
            CommunityId = communityId,
            GeographicRegion = region,
            Language = language
        };
        var dataType = CulturalDataType.CommunityInsights;

        // Act
        var result = await _sut.DetermineShardKeyAsync(culturalContext, dataType);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Region.Should().Be(region);
        result.Value.CommunityGroup.Should().Be(communityId);
        result.Value.ShardId.Should().MatchRegex(@"^[a-f0-9-]{36}$"); // GUID format
        result.Value.LoadBalancingWeight.Should().BeInRange(0.1, 1.0);
    }

    [Fact]
    public async Task GetOptimalConnectionString_WithShardKey_ShouldReturnCorrectConnection()
    {
        // Arrange
        var shardKey = new CulturalIntelligenceShardKey
        {
            ShardId = Guid.NewGuid().ToString(),
            Region = "north_america",
            CommunityGroup = "buddhist_sri_lanka",
            DataType = CulturalDataType.CalendarEvents,
            LoadBalancingWeight = 0.75
        };

        // Act
        var result = await _sut.GetOptimalConnectionStringAsync(shardKey);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNullOrEmpty();
        result.Value.Should().Contain("north_america");
        result.Value.Should().Contain("cultural_intelligence");
    }

    [Fact]
    public async Task CalculateShardDistribution_WithCulturalCommunities_ShouldBalanceLoad()
    {
        // Arrange
        var communities = new List<string>
        {
            "buddhist_sri_lanka", "hindu_indian", "islamic_pakistani", 
            "sikh_punjabi", "bengali_bangladeshi", "tamil_sri_lanka"
        };
        var region = "north_america";

        // Act
        var result = await _sut.CalculateShardDistributionAsync(communities, region);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        result.Value.Should().HaveCount(communities.Count);
        
        // Verify load balancing
        var totalWeight = result.Value.Values.Sum(s => s.LoadBalancingWeight);
        totalWeight.Should().BeApproximately(communities.Count, 0.5); // Roughly balanced
        
        // Verify each community has appropriate distribution
        foreach (var community in communities)
        {
            result.Value.Should().ContainKey(community);
            result.Value[community].CommunityGroup.Should().Be(community);
            result.Value[community].Region.Should().Be(region);
        }
    }

    [Fact]
    public async Task OptimizeQueryRouting_WithCulturalIntelligenceQuery_ShouldSelectBestShard()
    {
        // Arrange
        var queryContext = new CulturalIntelligenceQueryContext
        {
            QueryType = CulturalQueryType.BuddhistCalendar,
            CommunityId = "buddhist_sri_lanka",
            GeographicRegion = "north_america",
            ExpectedDataSize = DataSize.Medium,
            PerformanceRequirement = PerformanceRequirement.RealTime,
            CachingPreference = CachingStrategy.Aggressive
        };

        // Act
        var result = await _sut.OptimizeQueryRoutingAsync(queryContext);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.SelectedShard.CommunityGroup.Should().Be("buddhist_sri_lanka");
        result.Value.SelectedShard.Region.Should().Be("north_america");
        result.Value.QueryOptimizations.Should().NotBeEmpty();
        result.Value.EstimatedResponseTime.Should().BeLessThan(TimeSpan.FromMilliseconds(200));
    }

    [Theory]
    [InlineData(CulturalQueryType.BuddhistCalendar, 50)] // Fast calendar lookups
    [InlineData(CulturalQueryType.DiasporaAnalytics, 150)] // Medium analytics queries
    [InlineData(CulturalQueryType.CrossCulturalAnalysis, 300)] // Complex cross-cultural queries
    [InlineData(CulturalQueryType.EventRecommendations, 100)] // AI-powered recommendations
    public async Task OptimizeQueryRouting_WithDifferentQueryTypes_ShouldMeetPerformanceTargets(
        CulturalQueryType queryType, int maxResponseTimeMs)
    {
        // Arrange
        var queryContext = new CulturalIntelligenceQueryContext
        {
            QueryType = queryType,
            CommunityId = "test_community",
            GeographicRegion = "test_region",
            PerformanceRequirement = PerformanceRequirement.RealTime
        };

        // Act
        var result = await _sut.OptimizeQueryRoutingAsync(queryContext);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.EstimatedResponseTime.TotalMilliseconds.Should().BeLessOrEqualTo(maxResponseTimeMs);
        result.Value.QueryOptimizations.Should().Contain(opt => 
            opt.OptimizationType.Contains("Performance"));
    }

    [Fact]
    public async Task HandleShardRebalancing_WhenLoadImbalanceDetected_ShouldRebalanceShards()
    {
        // Arrange
        var currentDistribution = new Dictionary<string, ShardLoadMetrics>
        {
            ["shard1"] = new() { LoadPercentage = 85, ConnectionCount = 450, QueriesPerSecond = 1200 },
            ["shard2"] = new() { LoadPercentage = 45, ConnectionCount = 200, QueriesPerSecond = 600 },
            ["shard3"] = new() { LoadPercentage = 35, ConnectionCount = 150, QueriesPerSecond = 400 }
        };

        // Act
        var result = await _sut.HandleShardRebalancingAsync(currentDistribution);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.RebalancingRequired.Should().BeTrue();
        result.Value.RebalancingActions.Should().NotBeEmpty();
        result.Value.ExpectedLoadDistribution.Should().HaveCount(3);
        
        // Verify rebalancing reduces load variance
        var maxLoad = result.Value.ExpectedLoadDistribution.Values.Max(l => l.LoadPercentage);
        var minLoad = result.Value.ExpectedLoadDistribution.Values.Min(l => l.LoadPercentage);
        (maxLoad - minLoad).Should().BeLessThan(30); // Better load distribution
    }

    [Fact]
    public async Task GetCulturalDataDistribution_WithDiasporaCommunities_ShouldOptimizeStorage()
    {
        // Arrange
        var diasporaCommunitiesDistribution = new Dictionary<string, int>
        {
            ["buddhist_sri_lanka_north_america"] = 125000,
            ["hindu_indian_north_america"] = 850000,
            ["islamic_pakistani_north_america"] = 420000,
            ["sikh_punjabi_north_america"] = 340000,
            ["tamil_sri_lanka_north_america"] = 95000
        };

        // Act
        var result = await _sut.GetCulturalDataDistributionAsync(diasporaCommunitiesDistribution);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        
        // Verify larger communities get appropriate shard allocation
        var hinduDistribution = result.Value.First(d => d.CommunityId.Contains("hindu_indian"));
        hinduDistribution.AllocatedShards.Should().BeGreaterThan(2); // Large community needs more shards
        
        var tamilDistribution = result.Value.First(d => d.CommunityId.Contains("tamil_sri_lanka"));
        tamilDistribution.AllocatedShards.Should().BeLessOrEqualTo(2); // Smaller community needs fewer shards
    }

    [Fact]
    public async Task HandleCrossRegionSynchronization_WithCulturalEvents_ShouldMaintainConsistency()
    {
        // Arrange
        var culturalEvent = new CulturalEventSyncData
        {
            EventId = Guid.NewGuid(),
            CommunityId = "buddhist_sri_lanka",
            EventType = "vesak_celebration",
            SourceRegion = "north_america",
            TargetRegions = new[] { "europe", "australia", "asia_pacific" },
            CulturalSignificance = CulturalSignificance.High,
            RequiredSyncLatency = TimeSpan.FromSeconds(30)
        };

        // Act
        var result = await _sut.HandleCrossRegionSynchronizationAsync(culturalEvent);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.SynchronizationPlan.Should().NotBeEmpty();
        result.Value.EstimatedSyncTime.Should().BeLessOrEqualTo(TimeSpan.FromSeconds(30));
        result.Value.ConsistencyLevel.Should().Be(ConsistencyLevel.Strong);
        result.Value.SynchronizedRegions.Should().HaveCount(3);
    }

    [Fact]
    public async Task MonitorShardPerformance_WithRealTimeMetrics_ShouldTrackPerformance()
    {
        // Arrange
        var shardId = "shard_buddhist_north_america_001";
        var monitoringDuration = TimeSpan.FromMinutes(5);

        // Act
        var result = await _sut.MonitorShardPerformanceAsync(shardId, monitoringDuration);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.ShardId.Should().Be(shardId);
        result.Value.MonitoringPeriod.Should().Be(monitoringDuration);
        result.Value.PerformanceMetrics.Should().NotBeEmpty();
        result.Value.AverageResponseTime.Should().BeLessThan(TimeSpan.FromMilliseconds(200));
        result.Value.ThroughputQueriesPerSecond.Should().BeGreaterThan(100);
        result.Value.HealthScore.Should().BeInRange(0.0, 1.0);
    }

    [Theory]
    [InlineData("buddhist_sri_lanka", "north_america", 3)] // Medium-size community
    [InlineData("hindu_indian", "north_america", 5)] // Large community
    [InlineData("tamil_sri_lanka", "north_america", 2)] // Smaller community
    [InlineData("sikh_punjabi", "north_america", 3)] // Medium-size community
    public async Task CalculateOptimalShardCount_ForCommunitySize_ShouldAllocateAppropriately(
        string communityId, string region, int expectedMinShards)
    {
        // Arrange
        var communityMetrics = new CommunityShardingMetrics
        {
            CommunityId = communityId,
            Region = region,
            ActiveUsers = 50000,
            DailyQueries = 100000,
            DataGrowthRate = 0.15,
            PerformanceRequirement = PerformanceRequirement.RealTime
        };

        // Act
        var result = await _sut.CalculateOptimalShardCountAsync(communityMetrics);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OptimalShardCount.Should().BeGreaterOrEqualTo(expectedMinShards);
        result.Value.ShardingRationale.Should().NotBeEmpty();
        result.Value.PerformanceProjections.Should().NotBeEmpty();
        result.Value.ScalingRecommendations.Should().NotBeEmpty();
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