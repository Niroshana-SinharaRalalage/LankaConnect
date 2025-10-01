using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.Monitoring;

namespace LankaConnect.Application.Common.Interfaces;

public interface ICulturalStateReplicationService : IDisposable
{
    Task<Result<CrossRegionSynchronizationResult>> ReplicateCulturalStateAsync(
        string sourceRegion,
        List<string> targetRegions,
        List<CulturalDataType> culturalDataTypes,
        CancellationToken cancellationToken = default);

    Task<Result<CulturalStateSnapshot>> CaptureRealTimeCulturalStateAsync(
        string region,
        CancellationToken cancellationToken = default);

    Task<Result> SynchronizeBuddhistCalendarStateAsync(
        List<string> regions,
        CancellationToken cancellationToken = default);

    Task<Result> SynchronizeHinduCalendarStateAsync(
        List<string> regions,
        CancellationToken cancellationToken = default);

    Task<Result> SynchronizeIslamicCalendarStateAsync(
        List<string> regions,
        CancellationToken cancellationToken = default);

    Task<Result> SynchronizeDiasporaCommunitiesStateAsync(
        List<string> regions,
        CancellationToken cancellationToken = default);

    Task<Result<double>> ValidateCulturalDataIntegrityAsync(
        string sourceRegion,
        string targetRegion,
        CulturalDataType dataType,
        CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<string>>> DetectCulturalDataDivergenceAsync(
        List<string> regions,
        CulturalDataType dataType,
        CancellationToken cancellationToken = default);

    Task<Result> EnableRealTimeReplicationAsync(
        string sourceRegion,
        List<string> targetRegions,
        CulturalDataType dataType,
        CancellationToken cancellationToken = default);

    Task<Result> DisableRealTimeReplicationAsync(
        string sourceRegion,
        List<string> targetRegions,
        CulturalDataType dataType,
        CancellationToken cancellationToken = default);

    Task<Result<TimeSpan>> EstimateReplicationTimeAsync(
        string sourceRegion,
        List<string> targetRegions,
        List<CulturalDataType> dataTypes,
        CancellationToken cancellationToken = default);

    Task<Result<Dictionary<string, double>>> GetReplicationLagAsync(
        List<string> regions,
        CulturalDataType dataType,
        CancellationToken cancellationToken = default);

    Task<Result> ResolveCulturalDataConflictAsync(
        CulturalDataConflict conflict,
        ConflictResolutionStrategy strategy,
        CancellationToken cancellationToken = default);

    Task<Result<CulturalConsistencyMetrics>> GetReplicationMetricsAsync(
        TimeSpan evaluationPeriod,
        CancellationToken cancellationToken = default);

    Task<Result> TriggerManualReplicationAsync(
        string sourceRegion,
        List<string> targetRegions,
        CulturalDataType dataType,
        bool forceReplication,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> IsReplicationHealthyAsync(
        string sourceRegion,
        string targetRegion,
        CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<CulturalConsistencyAlert>>> GenerateReplicationAlertsAsync(
        TimeSpan monitoringWindow,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateReplicationConfigurationAsync(
        string region,
        Dictionary<CulturalDataType, TimeSpan> replicationIntervals,
        CancellationToken cancellationToken = default);

    Task<Result<Dictionary<string, CulturalHealthStatus>>> GetRegionalReplicationStatusAsync(
        List<string> regions,
        CancellationToken cancellationToken = default);

    Task<Result<CulturalStateSnapshot>> CreateIncrementalSnapshotAsync(
        string region,
        CulturalStateSnapshot baseSnapshot,
        CancellationToken cancellationToken = default);
}