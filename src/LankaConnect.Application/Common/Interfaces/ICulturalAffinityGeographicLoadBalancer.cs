using LankaConnect.Domain.Common;
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

public interface ICulturalAffinityGeographicLoadBalancer : IDisposable
{
    Task<Result<DiasporaLoadBalancingResult>> RouteToOptimalCulturalRegionAsync(
        DiasporaLoadBalancingRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CulturalAffinityScore>> CalculateCulturalAffinityScoreAsync(
        CulturalCommunityType sourceCommunity,
        CulturalCommunityType targetCommunity,
        CancellationToken cancellationToken = default);

    Task<Result<DiasporaLoadBalancingHealth>> GetDiasporaLoadBalancingHealthAsync(
        List<string> regions,
        CancellationToken cancellationToken = default);

    Task<Result<CulturalEventLoadOptimizationResult>> OptimizeForCulturalEventAsync(
        CulturalEventLoadContext eventContext,
        CancellationToken cancellationToken = default);

    Task<Result<DiasporaLoadBalancingMetrics>> GetDiasporaLoadBalancingMetricsAsync(
        TimeSpan evaluationPeriod,
        CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<CrossCulturalDiscoveryRecommendation>>> GenerateCrossCulturalRecommendationsAsync(
        CulturalCommunityType sourceCommunity,
        string geographicRegion,
        CancellationToken cancellationToken = default);

    Task<Result<GeographicCulturalRegion>> GetOptimalCulturalRegionAsync(
        CulturalContext culturalContext,
        DiasporaRoutingStrategy strategy,
        CancellationToken cancellationToken = default);

    Task<Result<CulturalAffinityMatrix>> GetCulturalAffinityMatrixAsync(
        List<CulturalCommunityType> communities,
        CancellationToken cancellationToken = default);

    Task<Result<DiasporaRoutingDecision>> EvaluateRoutingOptionsAsync(
        DiasporaLoadBalancingRequest request,
        List<string> candidateRegions,
        CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<DiasporaLoadBalancingAlert>>> GenerateLoadBalancingAlertsAsync(
        TimeSpan monitoringWindow,
        CancellationToken cancellationToken = default);

    Task<Result<Dictionary<string, double>>> CalculateRegionalLoadDistributionAsync(
        CulturalEventType culturalEvent,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateCulturalAffinityMatrixAsync(
        CulturalAffinityMatrix affinityMatrix,
        CancellationToken cancellationToken = default);

    Task<Result<CulturalCommunityClusterAnalysis>> AnalyzeCommunityClusteringAsync(
        string geographicRegion,
        CulturalCommunityType communityType,
        CancellationToken cancellationToken = default);

    Task<Result<bool>> ValidateLoadBalancingConfigurationAsync(
        DiasporaLoadBalancingConfiguration configuration,
        CancellationToken cancellationToken = default);

    Task<Result<TimeSpan>> PredictRoutingResponseTimeAsync(
        DiasporaLoadBalancingRequest request,
        string targetRegion,
        CancellationToken cancellationToken = default);

    Task<Result<Dictionary<CulturalLanguage, List<string>>>> GetLanguageOptimizedRegionsAsync(
        List<CulturalLanguage> preferredLanguages,
        CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<string>>> GetRecommendedRegionsForCommunityAsync(
        CulturalCommunityType communityType,
        int maxRecommendations,
        CancellationToken cancellationToken = default);

    Task<Result> EnableCulturalEventOptimizationAsync(
        CulturalEventType eventType,
        List<string> regions,
        CancellationToken cancellationToken = default);

    Task<Result> DisableCulturalEventOptimizationAsync(
        CulturalEventType eventType,
        List<string> regions,
        CancellationToken cancellationToken = default);

    Task<Result<double>> CalculateLoadBalancingEfficiencyAsync(
        string region,
        TimeSpan evaluationPeriod,
        CancellationToken cancellationToken = default);
}