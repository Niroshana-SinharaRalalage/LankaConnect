using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.Database;
using CrossRegionFailoverResult = LankaConnect.Domain.Common.Database.CrossRegionFailoverResult;

namespace LankaConnect.Application.Common.Interfaces;

public interface ICulturalIntelligenceConsistencyService : IDisposable
{
    Task<Result<CrossRegionSynchronizationResult>> SynchronizeCulturalDataAcrossRegionsAsync(
        CulturalDataSynchronizationRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CulturalDataConsistencyCheck>> ValidateCrossCulturalConsistencyAsync(
        CulturalDataType dataType,
        List<string> regions,
        CancellationToken cancellationToken = default);

    Task<Result<CulturalConflictResolution>> ResolveCulturalConflictAsync(
        CulturalDataConflict conflict,
        CancellationToken cancellationToken = default);

    Task<Result<CrossRegionFailoverResult>> ExecuteCrossRegionFailoverAsync(
        CrossRegionFailoverRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CulturalConsistencyMetrics>> GetCulturalConsistencyMetricsAsync(
        TimeSpan evaluationPeriod,
        CancellationToken cancellationToken = default);

    Task<Result<CulturalEventSynchronizationStatus>> MonitorCrossRegionConsistencyAsync(
        List<string> regions,
        CancellationToken cancellationToken = default);

    ConsistencyLevel GetRequiredConsistencyLevel(CulturalDataType dataType);

    Task<Result<IEnumerable<CulturalConsistencyAlert>>> GenerateConsistencyAlertsAsync(
        TimeSpan monitoringWindow,
        CancellationToken cancellationToken = default);

    Task<Result<RegionalCulturalProfile>> GetRegionalCulturalProfileAsync(
        string region,
        CancellationToken cancellationToken = default);

    Task<Result<Dictionary<string, double>>> CalculateRegionalConsistencyScoresAsync(
        List<string> regions,
        CulturalDataType dataType,
        CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<CulturalAuthoritySource>>> GetCulturalAuthoritiesForDataTypeAsync(
        CulturalDataType dataType,
        string region,
        CancellationToken cancellationToken = default);

    Task<Result> ValidateCulturalAuthorityAsync(
        CulturalAuthoritySource authority,
        CancellationToken cancellationToken = default);

    Task<Result<ConflictResolutionStrategy>> DetermineOptimalResolutionStrategyAsync(
        CulturalDataConflict conflict,
        CancellationToken cancellationToken = default);

    Task<Result<TimeSpan>> EstimateSynchronizationTimeAsync(
        CulturalDataSynchronizationRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> EnableEmergencyConsistencyModeAsync(
        string region,
        CulturalDataType dataType,
        CancellationToken cancellationToken = default);

    Task<Result> DisableEmergencyConsistencyModeAsync(
        string region,
        CulturalDataType dataType,
        CancellationToken cancellationToken = default);

    Task<Result<double>> CalculateCulturalDataStalenessAsync(
        string sourceRegion,
        string targetRegion,
        CulturalDataType dataType,
        CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<string>>> GetRegionsWithInconsistentDataAsync(
        CulturalDataType dataType,
        double consistencyThreshold,
        CancellationToken cancellationToken = default);

    Task<Result> TriggerManualSynchronizationAsync(
        string sourceRegion,
        List<string> targetRegions,
        CulturalDataType dataType,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> IsRegionHealthyForConsistencyOperationsAsync(
        string region,
        CancellationToken cancellationToken = default);
}