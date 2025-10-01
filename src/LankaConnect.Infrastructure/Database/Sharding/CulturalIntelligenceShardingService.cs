using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using DomainCulturalContext = LankaConnect.Domain.Communications.ValueObjects.CulturalContext;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LankaConnect.Infrastructure.Database.Sharding;

public class CulturalIntelligenceShardingService : ICulturalIntelligenceShardingService
{
    private readonly ILogger<CulturalIntelligenceShardingService> _logger;
    private readonly IOptions<DatabaseShardingOptions> _shardingOptions;
    private readonly ConcurrentDictionary<string, CulturalIntelligenceShardKey> _shardCache;
    private readonly ConcurrentDictionary<string, ShardPerformanceMetrics> _performanceCache;
    private readonly SemaphoreSlim _rebalancingSemaphore;
    private bool _disposed;

    public CulturalIntelligenceShardingService(
        ILogger<CulturalIntelligenceShardingService> logger,
        IOptions<DatabaseShardingOptions> shardingOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _shardingOptions = shardingOptions ?? throw new ArgumentNullException(nameof(shardingOptions));
        _shardCache = new ConcurrentDictionary<string, CulturalIntelligenceShardKey>();
        _performanceCache = new ConcurrentDictionary<string, ShardPerformanceMetrics>();
        _rebalancingSemaphore = new SemaphoreSlim(1, 1);
    }

    public async Task<Result<CulturalIntelligenceShardKey>> DetermineShardKeyAsync(
        LankaConnect.Domain.Communications.ValueObjects.CulturalContext culturalContext,
        CulturalDataType dataType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var cacheKey = GenerateCacheKey(culturalContext, dataType);
            
            if (_shardCache.TryGetValue(cacheKey, out var cachedShard))
            {
                _logger.LogDebug("Retrieved shard key from cache for {CommunityId} in {Region}",
                    culturalContext.CommunityId, culturalContext.GeographicRegion);
                return Result.Success(cachedShard);
            }

            var shardKey = await GenerateShardKeyAsync(culturalContext, dataType, cancellationToken);
            _shardCache.TryAdd(cacheKey, shardKey);

            _logger.LogInformation(
                "Generated shard key {ShardId} for community {CommunityId} in region {Region} (DataType: {DataType})",
                shardKey.ShardId, culturalContext.CommunityId, culturalContext.GeographicRegion, dataType);

            return Result.Success(shardKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to determine shard key for {CommunityId}", culturalContext.CommunityId);
            return Result.Failure<CulturalIntelligenceShardKey>($"Shard key generation failed: {ex.Message}");
        }
    }

    public async Task<Result<string>> GetOptimalConnectionStringAsync(
        CulturalIntelligenceShardKey shardKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var baseConnectionString = GetRegionConnectionString(shardKey.Region);
            var optimizedConnectionString = BuildOptimizedConnectionString(baseConnectionString, shardKey);

            _logger.LogDebug("Generated optimized connection string for shard {ShardId} in region {Region}",
                shardKey.ShardId, shardKey.Region);

            await Task.CompletedTask; // For async compliance
            return Result.Success(optimizedConnectionString);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get optimal connection string for shard {ShardId}", shardKey.ShardId);
            return Result.Failure<string>($"Connection string optimization failed: {ex.Message}");
        }
    }

    public async Task<Result<Dictionary<string, CulturalIntelligenceShardKey>>> CalculateShardDistributionAsync(
        IEnumerable<string> communities,
        string region,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var distribution = new Dictionary<string, CulturalIntelligenceShardKey>();
            var communityList = communities.ToList();
            var totalCommunities = communityList.Count;
            
            for (int i = 0; i < totalCommunities; i++)
            {
                var community = communityList[i];
                var loadWeight = CalculateLoadBalancingWeight(community, i, totalCommunities);
                
                var shardKey = new CulturalIntelligenceShardKey
                {
                    ShardId = GenerateShardId(community, region, i),
                    Region = region,
                    CommunityGroup = community,
                    DataType = CulturalDataType.CommunityInsights,
                    LoadBalancingWeight = loadWeight,
                    ShardingReason = "CulturalIntelligence Community Distribution"
                };
                
                distribution[community] = shardKey;
            }

            _logger.LogInformation(
                "Calculated shard distribution for {CommunityCount} communities in region {Region}",
                totalCommunities, region);

            await Task.CompletedTask;
            return Result.Success(distribution);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate shard distribution for region {Region}", region);
            return Result.Failure<Dictionary<string, CulturalIntelligenceShardKey>>($"Shard distribution calculation failed: {ex.Message}");
        }
    }

    public async Task<Result<QueryRoutingResult>> OptimizeQueryRoutingAsync(
        CulturalIntelligenceQueryContext queryContext,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var culturalContext = LankaConnect.Domain.Communications.ValueObjects.CulturalContext.CreateForBuddhistCommunity(
                Enum.Parse<GeographicRegion>(queryContext.GeographicRegion));

            var shardKeyResult = await DetermineShardKeyAsync(culturalContext, CulturalDataType.CommunityInsights, cancellationToken);
            if (!shardKeyResult.IsSuccess)
            {
                return Result.Failure<QueryRoutingResult>(shardKeyResult.Error);
            }

            var optimizations = GenerateQueryOptimizations(queryContext);
            var estimatedResponseTime = CalculateEstimatedResponseTime(queryContext);
            
            var routingResult = new QueryRoutingResult
            {
                SelectedShard = shardKeyResult.Value,
                QueryOptimizations = optimizations,
                EstimatedResponseTime = estimatedResponseTime,
                RoutingReason = $"Optimized for {queryContext.QueryType} with {queryContext.PerformanceRequirement} requirements",
                ConfidenceScore = 0.92
            };

            _logger.LogDebug(
                "Optimized query routing for {QueryType} to shard {ShardId} (Estimated: {ResponseTimeMs}ms)",
                queryContext.QueryType, shardKeyResult.Value.ShardId, estimatedResponseTime.TotalMilliseconds);

            return Result.Success(routingResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to optimize query routing for {QueryType}", queryContext.QueryType);
            return Result.Failure<QueryRoutingResult>($"Query routing optimization failed: {ex.Message}");
        }
    }

    public async Task<Result<ShardRebalancingResult>> HandleShardRebalancingAsync(
        Dictionary<string, ShardLoadMetrics> currentDistribution,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _rebalancingSemaphore.WaitAsync(cancellationToken);
            
            var rebalancingRequired = IsRebalancingRequired(currentDistribution);
            
            if (!rebalancingRequired)
            {
                return Result.Success(new ShardRebalancingResult
                {
                    RebalancingRequired = false,
                    RebalancingStrategy = "No rebalancing needed"
                });
            }

            var rebalancingActions = GenerateRebalancingActions(currentDistribution);
            var expectedDistribution = CalculateExpectedDistribution(currentDistribution, rebalancingActions);
            
            var result = new ShardRebalancingResult
            {
                RebalancingRequired = true,
                RebalancingActions = rebalancingActions,
                ExpectedLoadDistribution = expectedDistribution,
                EstimatedRebalancingTime = TimeSpan.FromMinutes(15),
                RebalancingStrategy = "Load-based rebalancing with cultural context awareness",
                RiskMitigations = new List<string>
                {
                    "Gradual migration to minimize cultural intelligence disruption",
                    "Maintain cultural data consistency during rebalancing",
                    "Monitor diaspora community performance during migration"
                }
            };

            _logger.LogInformation(
                "Shard rebalancing analysis completed. Required: {Required}, Actions: {ActionCount}",
                rebalancingRequired, rebalancingActions.Count);

            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle shard rebalancing");
            return Result.Failure<ShardRebalancingResult>($"Shard rebalancing failed: {ex.Message}");
        }
        finally
        {
            _rebalancingSemaphore.Release();
        }
    }

    public async Task<Result<IEnumerable<CulturalDataDistribution>>> GetCulturalDataDistributionAsync(
        Dictionary<string, int> diasporaCommunitiesDistribution,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var distributions = new List<CulturalDataDistribution>();
            
            foreach (var (communityId, userCount) in diasporaCommunitiesDistribution)
            {
                var shardsNeeded = CalculateShardsNeeded(userCount);
                var storageRequired = EstimateStorageRequirement(userCount);
                var queriesPerSecond = EstimateQueriesPerSecond(userCount);
                
                var distribution = new CulturalDataDistribution
                {
                    CommunityId = communityId,
                    Region = ExtractRegionFromCommunityId(communityId),
                    EstimatedUserCount = userCount,
                    AllocatedShards = shardsNeeded,
                    StorageRequirementGB = storageRequired,
                    ExpectedQueriesPerSecond = queriesPerSecond,
                    OptimizationRecommendations = GenerateOptimizationRecommendations(userCount, shardsNeeded)
                };
                
                distributions.Add(distribution);
            }

            _logger.LogInformation(
                "Calculated cultural data distribution for {CommunityCount} diaspora communities",
                diasporaCommunitiesDistribution.Count);

            await Task.CompletedTask;
            return Result.Success<IEnumerable<CulturalDataDistribution>>(distributions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cultural data distribution");
            return Result.Failure<IEnumerable<CulturalDataDistribution>>($"Cultural data distribution calculation failed: {ex.Message}");
        }
    }

    public async Task<Result<CrossRegionSynchronizationResult>> HandleCrossRegionSynchronizationAsync(
        CulturalEventSyncData culturalEvent,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var syncPlan = GenerateSynchronizationPlan(culturalEvent);
            var consistencyLevel = DetermineConsistencyLevel(culturalEvent.CulturalSignificance);
            var estimatedSyncTime = CalculateSyncTime(culturalEvent);
            
            var result = new CrossRegionSynchronizationResult
            {
                SynchronizationPlan = syncPlan,
                EstimatedSyncTime = estimatedSyncTime,
                ConsistencyLevel = consistencyLevel,
                SynchronizedRegions = culturalEvent.TargetRegions.ToList()
            };

            _logger.LogInformation(
                "Cross-region synchronization plan generated for cultural event {EventId} ({EventType}) to {RegionCount} regions",
                culturalEvent.EventId, culturalEvent.EventType, culturalEvent.TargetRegions.Length);

            await Task.CompletedTask;
            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle cross-region synchronization for event {EventId}", culturalEvent.EventId);
            return Result.Failure<CrossRegionSynchronizationResult>($"Cross-region synchronization failed: {ex.Message}");
        }
    }

    public async Task<Result<ShardPerformanceMetrics>> MonitorShardPerformanceAsync(
        string shardId,
        TimeSpan monitoringDuration,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Simulate performance monitoring
            await Task.Delay(100, cancellationToken);
            
            var metrics = new ShardPerformanceMetrics
            {
                ShardId = shardId,
                MonitoringPeriod = monitoringDuration,
                AverageResponseTime = TimeSpan.FromMilliseconds(Random.Shared.Next(50, 150)),
                ThroughputQueriesPerSecond = Random.Shared.Next(100, 500),
                HealthScore = 0.85 + (Random.Shared.NextDouble() * 0.15), // 85-100%
            };
            
            // Generate performance metrics
            metrics.PerformanceMetrics["cpu_utilization"] = Random.Shared.NextDouble() * 0.3 + 0.4; // 40-70%
            metrics.PerformanceMetrics["memory_utilization"] = Random.Shared.NextDouble() * 0.25 + 0.5; // 50-75%
            metrics.PerformanceMetrics["disk_io_rate"] = Random.Shared.NextDouble() * 100 + 50; // 50-150 MB/s
            metrics.PerformanceMetrics["network_throughput"] = Random.Shared.NextDouble() * 200 + 100; // 100-300 Mbps
            
            _performanceCache.TryAdd(shardId, metrics);
            
            _logger.LogDebug(
                "Performance monitoring completed for shard {ShardId}: {HealthScore:P2} health, {ResponseTimeMs}ms avg response",
                shardId, metrics.HealthScore, metrics.AverageResponseTime.TotalMilliseconds);

            return Result.Success(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to monitor performance for shard {ShardId}", shardId);
            return Result.Failure<ShardPerformanceMetrics>($"Shard performance monitoring failed: {ex.Message}");
        }
    }

    public async Task<Result<OptimalShardingResult>> CalculateOptimalShardCountAsync(
        CommunityShardingMetrics communityMetrics,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var baseShardCount = CalculateBaseShardCount(communityMetrics.ActiveUsers, communityMetrics.DailyQueries);
            var performanceAdjustment = CalculatePerformanceAdjustment(communityMetrics.PerformanceRequirement);
            var growthAdjustment = CalculateGrowthAdjustment(communityMetrics.DataGrowthRate);
            
            var optimalCount = Math.Max(2, (int)(baseShardCount * performanceAdjustment * growthAdjustment));
            
            var result = new OptimalShardingResult
            {
                OptimalShardCount = optimalCount,
                ShardingRationale = $"Calculated for {communityMetrics.ActiveUsers:N0} users, {communityMetrics.DailyQueries:N0} daily queries, {communityMetrics.DataGrowthRate:P1} growth rate",
                EstimatedMigrationTime = TimeSpan.FromHours(optimalCount * 2)
            };
            
            // Generate performance projections
            result.PerformanceProjections["average_response_time"] = $"<{200 / optimalCount}ms";
            result.PerformanceProjections["queries_per_second"] = $"{communityMetrics.DailyQueries / (24 * 3600) * optimalCount:N0}";
            result.PerformanceProjections["concurrent_users"] = $"{communityMetrics.ActiveUsers / optimalCount:N0} per shard";
            
            // Generate scaling recommendations
            result.ScalingRecommendations.Add($"Distribute load across {optimalCount} shards for optimal performance");
            result.ScalingRecommendations.Add("Monitor cultural community growth patterns for proactive scaling");
            result.ScalingRecommendations.Add("Implement automated shard splitting when utilization exceeds 75%");
            
            _logger.LogInformation(
                "Calculated optimal shard count for community {CommunityId}: {ShardCount} shards",
                communityMetrics.CommunityId, optimalCount);

            await Task.CompletedTask;
            return Result.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate optimal shard count for community {CommunityId}", communityMetrics.CommunityId);
            return Result.Failure<OptimalShardingResult>($"Optimal shard count calculation failed: {ex.Message}");
        }
    }

    // Additional interface methods with simplified implementations
    public async Task<Result<IEnumerable<CulturalIntelligenceShardKey>>> GetAvailableShardsAsync(
        string region,
        CulturalDataType dataType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var shards = _shardCache.Values
                .Where(s => s.Region == region && s.DataType == dataType)
                .ToList();
                
            await Task.CompletedTask;
            return Result.Success<IEnumerable<CulturalIntelligenceShardKey>>(shards);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available shards for region {Region}", region);
            return Result.Failure<IEnumerable<CulturalIntelligenceShardKey>>($"Failed to get available shards: {ex.Message}");
        }
    }

    public async Task<Result<Dictionary<string, double>>> GetShardHealthScoresAsync(
        string region,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var healthScores = _performanceCache
                .Where(p => p.Value.ShardId.Contains(region))
                .ToDictionary(p => p.Key, p => p.Value.HealthScore);
                
            await Task.CompletedTask;
            return Result.Success(healthScores);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get shard health scores for region {Region}", region);
            return Result.Failure<Dictionary<string, double>>($"Failed to get health scores: {ex.Message}");
        }
    }

    public async Task<Result> ExecuteShardMigrationAsync(
        string sourceShard,
        string targetShard,
        CulturalDataType dataType,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Executing shard migration from {SourceShard} to {TargetShard} for {DataType}",
                sourceShard, targetShard, dataType);
                
            // Simulate migration process
            await Task.Delay(1000, cancellationToken);
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute shard migration from {SourceShard} to {TargetShard}", sourceShard, targetShard);
            return Result.Failure($"Shard migration failed: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<string>>> GetRecommendedOptimizationsAsync(
        CulturalIntelligenceShardKey shardKey,
        ShardPerformanceMetrics performanceMetrics,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var optimizations = new List<string>();
            
            if (performanceMetrics.HealthScore < 0.8)
            {
                optimizations.Add("Consider shard rebalancing to distribute load more evenly");
            }
            
            if (performanceMetrics.AverageResponseTime > TimeSpan.FromMilliseconds(200))
            {
                optimizations.Add("Optimize cultural intelligence queries with better indexing");
            }
            
            optimizations.Add("Enable cultural data caching for improved diaspora community performance");
            
            await Task.CompletedTask;
            return Result.Success<IEnumerable<string>>(optimizations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recommended optimizations for shard {ShardId}", shardKey.ShardId);
            return Result.Failure<IEnumerable<string>>($"Failed to get optimizations: {ex.Message}");
        }
    }

    // Private helper methods
    private async Task<CulturalIntelligenceShardKey> GenerateShardKeyAsync(
        LankaConnect.Domain.Communications.ValueObjects.CulturalContext culturalContext,
        CulturalDataType dataType,
        CancellationToken cancellationToken)
    {
        var communityId = culturalContext.CulturalBackground.ToString();
        var region = culturalContext.GeographicRegion.ToString();
        var shardId = GenerateShardId(communityId, region, 0);

        return new CulturalIntelligenceShardKey
        {
            ShardId = shardId,
            Region = region,
            CommunityGroup = communityId,
            DataType = dataType,
            LoadBalancingWeight = Random.Shared.NextDouble() * 0.5 + 0.5, // 0.5-1.0
            ShardingReason = "CulturalIntelligence pattern-based sharding"
        };
    }

    private string GenerateCacheKey(LankaConnect.Domain.Communications.ValueObjects.CulturalContext culturalContext, CulturalDataType dataType)
    {
        return $"{culturalContext.CulturalBackground}:{culturalContext.GeographicRegion}:{dataType}";
    }

    private string GenerateShardId(string communityId, string region, int index)
    {
        using var sha256 = SHA256.Create();
        var input = $"{communityId}:{region}:{index}";
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return new Guid(hash.Take(16).ToArray()).ToString();
    }

    private double CalculateLoadBalancingWeight(string community, int index, int total)
    {
        // Distribute weight more evenly for better load balancing
        var baseWeight = 1.0 / total;
        var adjustment = (Random.Shared.NextDouble() - 0.5) * 0.2; // ±10% variation
        return Math.Max(0.1, baseWeight + adjustment);
    }

    private string GetRegionConnectionString(string region)
    {
        return _shardingOptions.Value.RegionConnectionStrings.TryGetValue(region, out var connectionString)
            ? connectionString
            : $"Server=db-{region}.lankaconnect.com;Database=cultural_intelligence_{region};";
    }

    private string BuildOptimizedConnectionString(string baseConnectionString, CulturalIntelligenceShardKey shardKey)
    {
        return $"{baseConnectionString}Shard={shardKey.ShardId.Substring(0, 8)};Community={shardKey.CommunityGroup};";
    }

    private List<string> GenerateQueryOptimizations(CulturalIntelligenceQueryContext queryContext)
    {
        var optimizations = new List<string> { "Performance indexing enabled" };
        
        if (queryContext.PerformanceRequirement == PerformanceRequirement.RealTime)
        {
            optimizations.Add("Real-time query optimization");
        }
        
        if (queryContext.CachingPreference != CachingStrategy.None)
        {
            optimizations.Add("Cultural intelligence caching enabled");
        }
        
        return optimizations;
    }

    private TimeSpan CalculateEstimatedResponseTime(CulturalIntelligenceQueryContext queryContext)
    {
        var baseTime = queryContext.QueryType switch
        {
            CulturalQueryType.BuddhistCalendar => TimeSpan.FromMilliseconds(50),
            CulturalQueryType.DiasporaAnalytics => TimeSpan.FromMilliseconds(150),
            CulturalQueryType.CrossCulturalAnalysis => TimeSpan.FromMilliseconds(300),
            CulturalQueryType.EventRecommendations => TimeSpan.FromMilliseconds(100),
            _ => TimeSpan.FromMilliseconds(100)
        };
        
        var performanceMultiplier = queryContext.PerformanceRequirement switch
        {
            PerformanceRequirement.RealTime => 0.7,
            PerformanceRequirement.FastResponse => 0.85,
            _ => 1.0
        };
        
        return TimeSpan.FromMilliseconds(baseTime.TotalMilliseconds * performanceMultiplier);
    }

    private bool IsRebalancingRequired(Dictionary<string, ShardLoadMetrics> currentDistribution)
    {
        if (!currentDistribution.Any()) return false;
        
        var maxLoad = currentDistribution.Values.Max(l => l.LoadPercentage);
        var minLoad = currentDistribution.Values.Min(l => l.LoadPercentage);
        
        return (maxLoad - minLoad) > 30; // >30% difference requires rebalancing
    }

    private List<string> GenerateRebalancingActions(Dictionary<string, ShardLoadMetrics> currentDistribution)
    {
        var actions = new List<string>();
        var sortedShards = currentDistribution.OrderByDescending(s => s.Value.LoadPercentage).ToList();
        
        var heaviestShard = sortedShards.First();
        var lightestShard = sortedShards.Last();
        
        actions.Add($"Migrate 25% of load from {heaviestShard.Key} to {lightestShard.Key}");
        actions.Add("Update cultural intelligence routing to balance diaspora community load");
        actions.Add("Monitor cultural data access patterns during rebalancing");
        
        return actions;
    }

    private Dictionary<string, ShardLoadMetrics> CalculateExpectedDistribution(
        Dictionary<string, ShardLoadMetrics> currentDistribution,
        List<string> rebalancingActions)
    {
        var expectedDistribution = new Dictionary<string, ShardLoadMetrics>();
        
        foreach (var (shardId, currentMetrics) in currentDistribution)
        {
            var adjustedLoad = Math.Max(40, Math.Min(70, currentMetrics.LoadPercentage + Random.Shared.Next(-15, 15)));
            
            expectedDistribution[shardId] = new ShardLoadMetrics
            {
                LoadPercentage = adjustedLoad,
                ConnectionCount = (int)(currentMetrics.ConnectionCount * (adjustedLoad / currentMetrics.LoadPercentage)),
                QueriesPerSecond = currentMetrics.QueriesPerSecond * (adjustedLoad / currentMetrics.LoadPercentage)
            };
        }
        
        return expectedDistribution;
    }

    private int CalculateShardsNeeded(int userCount)
    {
        return userCount switch
        {
            < 50000 => 2,
            < 200000 => 3,
            < 500000 => 5,
            _ => Math.Min(8, (userCount / 100000) + 2)
        };
    }

    private double EstimateStorageRequirement(int userCount)
    {
        // Estimate GB of storage needed per user for cultural intelligence data
        return userCount * 0.025; // 25MB per user
    }

    private double EstimateQueriesPerSecond(int userCount)
    {
        // Estimate average queries per second based on user count
        return userCount * 0.003; // 3 queries per 1000 users per second
    }

    private string ExtractRegionFromCommunityId(string communityId)
    {
        var parts = communityId.Split('_');
        return parts.Length > 2 ? parts[^1] : "global"; // Last part or "global"
    }

    private List<string> GenerateOptimizationRecommendations(int userCount, int shardCount)
    {
        var recommendations = new List<string>();
        
        if (userCount > 100000)
        {
            recommendations.Add("Enable read replicas for improved cultural intelligence query performance");
        }
        
        if (shardCount > 3)
        {
            recommendations.Add("Implement connection pooling optimization for multi-shard queries");
        }
        
        recommendations.Add("Configure cultural data caching for diaspora community patterns");
        
        return recommendations;
    }

    private List<string> GenerateSynchronizationPlan(CulturalEventSyncData culturalEvent)
    {
        var plan = new List<string>
        {
            $"Phase 1: Prepare cultural event data for {culturalEvent.EventType} synchronization",
            "Phase 2: Validate cultural significance and community context",
            "Phase 3: Execute cross-region data replication with consistency checks",
            "Phase 4: Verify cultural intelligence data integrity across all target regions"
        };
        
        return plan;
    }

    private ConsistencyLevel DetermineConsistencyLevel(LankaConnect.Domain.Common.CulturalSignificance significance)
    {
        return significance switch
        {
            LankaConnect.Domain.Common.CulturalSignificance.Critical or LankaConnect.Domain.Common.CulturalSignificance.Sacred => ConsistencyLevel.Strong,
            LankaConnect.Domain.Common.CulturalSignificance.High => ConsistencyLevel.BoundedStaleness,
            LankaConnect.Domain.Common.CulturalSignificance.Medium => ConsistencyLevel.Session,
            _ => ConsistencyLevel.Eventual
        };
    }

    private TimeSpan CalculateSyncTime(CulturalEventSyncData culturalEvent)
    {
        var baseTime = TimeSpan.FromSeconds(10);
        var regionMultiplier = culturalEvent.TargetRegions.Length * 0.3;
        var significanceMultiplier = culturalEvent.CulturalSignificance switch
        {
            CulturalSignificance.Critical or CulturalSignificance.Sacred => 0.5, // Faster for important events
            _ => 1.0
        };
        
        return TimeSpan.FromSeconds(baseTime.TotalSeconds * regionMultiplier * significanceMultiplier);
    }

    private int CalculateBaseShardCount(int activeUsers, long dailyQueries)
    {
        var userBasedShards = Math.Ceiling(activeUsers / 25000.0); // 25K users per shard
        var queryBasedShards = Math.Ceiling(dailyQueries / 100000.0); // 100K queries per shard per day
        
        return (int)Math.Max(userBasedShards, queryBasedShards);
    }

    private double CalculatePerformanceAdjustment(PerformanceRequirement requirement)
    {
        return requirement switch
        {
            PerformanceRequirement.RealTime => 1.5,
            PerformanceRequirement.FastResponse => 1.2,
            PerformanceRequirement.StandardResponse => 1.0,
            _ => 0.8
        };
    }

    private double CalculateGrowthAdjustment(double growthRate)
    {
        return 1.0 + (growthRate * 2); // Add capacity based on growth rate
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _rebalancingSemaphore?.Dispose();
            _disposed = true;
        }
    }
}