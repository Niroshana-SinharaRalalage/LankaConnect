using LankaConnect.Domain.Common;
using System.Data;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Enterprise;
using LankaConnect.Domain.Common.Models;
using LankaConnect.Domain.Common.Monitoring;
using LankaConnect.Domain.Common.ValueObjects;
using LankaConnect.Domain.Common.Security;
using LankaConnect.Domain.Common.Recovery;
using LankaConnect.Domain.Common.Database;
using LankaConnect.Domain.Common.Enums;
using MultiLanguageModels = LankaConnect.Domain.Common.Database.MultiLanguageRoutingModels;

namespace LankaConnect.Application.Common.Interfaces;

public interface IEnterpriseConnectionPoolService : IDisposable
{
    Task<Result<IDbConnection>> GetOptimizedConnectionAsync(
        CulturalContext culturalContext,
        DatabaseOperationType operationType,
        CancellationToken cancellationToken = default);

    Task<Result<ConnectionPoolMetrics>> GetPoolHealthMetricsAsync(
        CancellationToken cancellationToken = default);

    Task<Result> OptimizePoolConfigurationAsync(
        PerformanceTarget performanceTarget,
        CancellationToken cancellationToken = default);

    Task<Result<CulturalConnectionRoutingResult>> RouteConnectionByCulturalContextAsync(
        CulturalContext culturalContext,
        DatabaseOperationType operationType,
        CancellationToken cancellationToken = default);

    Task<Result<EnterpriseConnectionPoolMetrics>> GetSystemWidePoolMetricsAsync(
        CancellationToken cancellationToken = default);

    Task<Result<Dictionary<string, ConnectionPoolHealth>>> GetAllPoolHealthStatusAsync(
        CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<CulturalPoolDistribution>>> CalculateOptimalPoolDistributionAsync(
        Dictionary<string, int> communityUserCounts,
        string region,
        CancellationToken cancellationToken = default);

    Task<Result<ConnectionPoolOptimizationResult>> ExecutePoolOptimizationAsync(
        ConnectionPoolOptimizationStrategy strategy,
        CancellationToken cancellationToken = default);

    Task<Result> ScalePoolAsync(
        string poolId,
        int targetConnections,
        CancellationToken cancellationToken = default);

    Task<Result<ConnectionPoolConfiguration>> CreateCulturallyOptimizedPoolAsync(
        string communityGroup,
        string region,
        DatabaseOperationType primaryOperationType,
        CancellationToken cancellationToken = default);

    Task<Result> MonitorPoolPerformanceAsync(
        string poolId,
        TimeSpan monitoringDuration,
        CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<string>>> GetPoolOptimizationRecommendationsAsync(
        string poolId,
        ConnectionPoolMetrics currentMetrics,
        CancellationToken cancellationToken = default);

    Task<Result> ExecutePoolFailoverAsync(
        string primaryPoolId,
        string backupPoolId,
        CancellationToken cancellationToken = default);

    Task<Result<double>> CalculatePoolEfficiencyScoreAsync(
        string poolId,
        TimeSpan evaluationPeriod,
        CancellationToken cancellationToken = default);

    Task<Result<Dictionary<string, TimeSpan>>> GetConnectionAcquisitionTimesAsync(
        string region,
        CancellationToken cancellationToken = default);
}