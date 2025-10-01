using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.Monitoring;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Enterprise;
using LankaConnect.Domain.Common.Models;
using LankaConnect.Domain.Common.ValueObjects;
using LankaConnect.Domain.Common.Security;
using LankaConnect.Domain.Common.Recovery;
using MultiLanguageModels = LankaConnect.Domain.Common.Database.MultiLanguageRoutingModels;

// Explicit namespace aliases to resolve conflicts
using MonitoringMetrics = LankaConnect.Domain.Common.Monitoring;
using DatabaseModels = LankaConnect.Domain.Common.Database;

namespace LankaConnect.Application.Common.Interfaces;

public interface ICulturalIntelligenceFailoverOrchestrator : IDisposable
{
    Task<Result<CulturalFailoverResult>> ExecuteCulturalFailoverAsync(
        CulturalFailoverRequest failoverRequest,
        CancellationToken cancellationToken = default);

    Task<Result<LankaConnect.Domain.Common.CulturalImpactAssessment>> AssessCulturalImpactAsync(
        CulturalDisasterScenario disasterScenario,
        CancellationToken cancellationToken = default);

    Task<Result<CulturalFailoverHealthReport>> MonitorCulturalFailoverHealthAsync(
        List<string> regions,
        CancellationToken cancellationToken = default);

    Task<Result<CulturalDisasterRecoveryPlan>> CreateCulturalDisasterRecoveryPlanAsync(
        CulturalDisasterScenario scenario,
        CancellationToken cancellationToken = default);

    Task<Result<CulturalFailoverMetrics>> GetCulturalFailoverMetricsAsync(
        TimeSpan evaluationPeriod,
        CancellationToken cancellationToken = default);

    Task<Result<CulturalFailoverValidationResult>> ValidateFailoverReadinessAsync(
        CulturalFailoverConfiguration configuration,
        CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<CulturalFailoverAlert>>> GenerateFailoverAlertsAsync(
        TimeSpan monitoringWindow,
        CancellationToken cancellationToken = default);

    Task<Result<RegionalCulturalBackupStatus>> GetRegionalBackupStatusAsync(
        string region,
        CancellationToken cancellationToken = default);

    Task<Result<CulturalStateSnapshot>> CreateCulturalStateSnapshotAsync(
        string region,
        List<CulturalDataType> dataTypes,
        CancellationToken cancellationToken = default);

    Task<Result> RestoreCulturalStateFromSnapshotAsync(
        CulturalStateSnapshot snapshot,
        string targetRegion,
        CancellationToken cancellationToken = default);

    Task<Result<TimeSpan>> EstimateFailoverTimeAsync(
        CulturalFailoverRequest failoverRequest,
        CancellationToken cancellationToken = default);

    Task<Result<Dictionary<string, double>>> CalculateRegionalFailoverRisksAsync(
        List<string> regions,
        CancellationToken cancellationToken = default);

    Task<Result<CulturalFailoverConfiguration>> OptimizeFailoverConfigurationAsync(
        string primaryRegion,
        List<string> candidateBackupRegions,
        CancellationToken cancellationToken = default);

    Task<Result> TriggerEmergencyFailoverAsync(
        string sourceRegion,
        CulturalDisasterType disasterType,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> IsRegionReadyForFailoverAsync(
        string region,
        CulturalDataType dataType,
        CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<string>>> GetOptimalBackupRegionsAsync(
        string primaryRegion,
        CulturalEventType culturalEvent,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateFailoverConfigurationAsync(
        CulturalFailoverConfiguration configuration,
        CancellationToken cancellationToken = default);

    Task<Result<double>> CalculateFailoverSuccessRateAsync(
        string region,
        TimeSpan evaluationPeriod,
        CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<CulturalFailoverResult>>> GetFailoverHistoryAsync(
        string region,
        TimeSpan historyPeriod,
        CancellationToken cancellationToken = default);

    Task<Result> TestFailoverScenarioAsync(
        CulturalDisasterScenario scenario,
        bool simulationMode,
        CancellationToken cancellationToken = default);
}