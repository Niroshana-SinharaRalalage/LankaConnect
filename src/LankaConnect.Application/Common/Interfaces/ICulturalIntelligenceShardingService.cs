using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.Monitoring;

namespace LankaConnect.Application.Common.Interfaces;

public interface ICulturalIntelligenceShardingService : IDisposable
{
    Task<Result<CulturalIntelligenceShardKey>> DetermineShardKeyAsync(
        LankaConnect.Domain.Communications.ValueObjects.CulturalContext culturalContext,
        CulturalDataType dataType,
        CancellationToken cancellationToken = default);

    Task<Result<string>> GetOptimalConnectionStringAsync(
        CulturalIntelligenceShardKey shardKey,
        CancellationToken cancellationToken = default);

    Task<Result<Dictionary<string, CulturalIntelligenceShardKey>>> CalculateShardDistributionAsync(
        IEnumerable<string> communities,
        string region,
        CancellationToken cancellationToken = default);

    Task<Result<QueryRoutingResult>> OptimizeQueryRoutingAsync(
        CulturalIntelligenceQueryContext queryContext,
        CancellationToken cancellationToken = default);

    Task<Result<ShardRebalancingResult>> HandleShardRebalancingAsync(
        Dictionary<string, ShardLoadMetrics> currentDistribution,
        CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<CulturalDataDistribution>>> GetCulturalDataDistributionAsync(
        Dictionary<string, int> diasporaCommunitiesDistribution,
        CancellationToken cancellationToken = default);

    Task<Result<CrossRegionSynchronizationResult>> HandleCrossRegionSynchronizationAsync(
        CulturalEventSyncData culturalEvent,
        CancellationToken cancellationToken = default);

    Task<Result<ShardPerformanceMetrics>> MonitorShardPerformanceAsync(
        string shardId,
        TimeSpan monitoringDuration,
        CancellationToken cancellationToken = default);

    Task<Result<OptimalShardingResult>> CalculateOptimalShardCountAsync(
        CommunityShardingMetrics communityMetrics,
        CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<CulturalIntelligenceShardKey>>> GetAvailableShardsAsync(
        string region,
        CulturalDataType dataType,
        CancellationToken cancellationToken = default);

    Task<Result<Dictionary<string, double>>> GetShardHealthScoresAsync(
        string region,
        CancellationToken cancellationToken = default);

    Task<Result> ExecuteShardMigrationAsync(
        string sourceShard,
        string targetShard,
        CulturalDataType dataType,
        CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<string>>> GetRecommendedOptimizationsAsync(
        CulturalIntelligenceShardKey shardKey,
        ShardPerformanceMetrics performanceMetrics,
        CancellationToken cancellationToken = default);
}