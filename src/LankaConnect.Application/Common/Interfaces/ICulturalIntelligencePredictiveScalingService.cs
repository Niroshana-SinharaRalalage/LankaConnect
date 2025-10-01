using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Database;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.Monitoring;

namespace LankaConnect.Application.Common.Interfaces;

public interface ICulturalIntelligencePredictiveScalingService : IDisposable
{
    Task<Result<CulturalEventPrediction>> PredictCulturalEventScalingAsync(
        LankaConnect.Domain.Communications.ValueObjects.CulturalContext culturalContext,
        TimeSpan predictionWindow,
        CancellationToken cancellationToken = default);

    Task<Result<AutoScalingDecision>> EvaluateAutoScalingTriggersAsync(
        DatabaseScalingMetrics currentMetrics,
        CancellationToken cancellationToken = default);

    Task<Result<ScalingExecutionResult>> ExecuteScalingActionAsync(
        AutoScalingDecision scalingDecision,
        CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<CulturalLoadPattern>>> GetCulturalLoadPatternsAsync(
        string communityId,
        string geographicRegion,
        CancellationToken cancellationToken = default);

    Task<Result<PredictiveScalingInsights>> MonitorScalingPerformanceAsync(
        TimeSpan monitoringPeriod,
        CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<CulturalEventPrediction>>> GetUpcomingCulturalEventsAsync(
        string geographicRegion,
        TimeSpan predictionWindow,
        CancellationToken cancellationToken = default);

    Task<Result<GeographicScalingConfiguration>> OptimizeRegionalScalingConfigurationAsync(
        string region,
        Dictionary<string, int> communityUserCounts,
        CancellationToken cancellationToken = default);

    Task<Result<CrossRegionScalingCoordination>> CoordinateCrossRegionScalingAsync(
        CulturalEventType culturalEvent,
        List<string> affectedRegions,
        CancellationToken cancellationToken = default);

    Task<Result<CulturalScalingMetrics>> GetScalingPerformanceMetricsAsync(
        TimeSpan evaluationPeriod,
        CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<CulturalScalingAlert>>> GenerateCulturalScalingAlertsAsync(
        TimeSpan alertWindow,
        CancellationToken cancellationToken = default);

    Task<Result<DiasporaCommunityScalingProfile>> CreateCommunityScalingProfileAsync(
        string communityId,
        List<string> geographicRegions,
        CancellationToken cancellationToken = default);

    Task<Result<Dictionary<string, double>>> CalculateCulturalAffinityScoresAsync(
        string primaryCommunityId,
        List<string> candidateCommunities,
        CancellationToken cancellationToken = default);

    Task<Result<DatabaseScalingMetrics>> CollectRealTimeScalingMetricsAsync(
        string region,
        CancellationToken cancellationToken = default);

    Task<Result<CulturalEventCalendar>> GetCulturalEventCalendarAsync(
        string communityId,
        string geographicRegion,
        CancellationToken cancellationToken = default);

    Task<Result> ValidateScalingConfigurationAsync(
        GeographicScalingConfiguration configuration,
        CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<string>>> GetScalingOptimizationRecommendationsAsync(
        string region,
        DatabaseScalingMetrics currentMetrics,
        CancellationToken cancellationToken = default);

    Task<Result> EnableEmergencyScalingModeAsync(
        string region,
        string reason,
        CancellationToken cancellationToken = default);

    Task<Result> DisableEmergencyScalingModeAsync(
        string region,
        CancellationToken cancellationToken = default);

    Task<Result<TimeSpan>> CalculateOptimalScalingLeadTimeAsync(
        CulturalEventType eventType,
        string communityId,
        CancellationToken cancellationToken = default);

    Task<Result<double>> PredictCulturalEventAccuracyAsync(
        CulturalEventPrediction prediction,
        CancellationToken cancellationToken = default);
}