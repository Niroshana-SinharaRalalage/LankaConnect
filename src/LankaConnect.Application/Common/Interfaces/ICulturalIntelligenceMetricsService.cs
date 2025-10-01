using LankaConnect.Application.Common.Enterprise;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.Monitoring;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Enterprise;
using LankaConnect.Domain.Common.Models;
using LankaConnect.Domain.Common.ValueObjects;
using MultiLanguageModels = LankaConnect.Domain.Common.Database.MultiLanguageRoutingModels;

// Explicit namespace aliases to resolve conflicts
using MonitoringMetrics = LankaConnect.Domain.Common.Monitoring;
using DatabaseModels = LankaConnect.Domain.Common.Database;

namespace LankaConnect.Application.Common.Interfaces;

public interface ICulturalIntelligenceMetricsService : IDisposable
{
    Task<Result<MonitoringMetrics.CulturalApiPerformanceMetrics>> TrackCulturalApiPerformanceAsync(
        MonitoringMetrics.CulturalIntelligenceEndpoint endpoint,
        TimeSpan responseTime,
        int requestSize,
        int responseSize,
        CulturalContext culturalContext,
        CancellationToken cancellationToken = default);

    Task<Result<MonitoringMetrics.CulturalApiAccuracyMetrics>> TrackCulturalApiAccuracyAsync(
        MonitoringMetrics.CulturalIntelligenceEndpoint endpoint,
        double accuracyScore,
        double confidenceScore,
        double culturalRelevanceScore,
        string communityId,
        CancellationToken cancellationToken = default);

    Task<Result<MonitoringMetrics.ApiResponseTimeMetrics>> TrackApiResponseTimeAsync(
        MonitoringMetrics.CulturalIntelligenceEndpoint endpoint,
        TimeSpan responseTime,
        CulturalContext culturalContext,
        CancellationToken cancellationToken = default);

    Task<Result<MonitoringMetrics.DiasporaEngagementMetrics>> TrackDiasporaEngagementAsync(
        string communityId,
        DiasporaEngagementType engagementType,
        string geographicRegion,
        int userCount,
        double engagementScore,
        CancellationToken cancellationToken = default);

    Task<Result<MonitoringMetrics.EnterpriseClientSlaMetrics>> TrackEnterpriseClientSlaAsync(
        string clientId,
        EnterpriseContractTier contractTier,
        TimeSpan slaTarget,
        TimeSpan actualResponseTime,
        MonitoringMetrics.CulturalFeatureUsageMetrics culturalFeatureUsage,
        CancellationToken cancellationToken = default);

    Task<Result<RevenueImpactMetrics>> TrackRevenueImpactAsync(
        CulturalIntelligenceEndpoint apiEndpoint,
        decimal revenuePerRequest,
        int requestCount,
        SubscriptionTier subscriptionTier,
        ClientSegment clientSegment,
        CancellationToken cancellationToken = default);

    Task<Result<MonitoringMetrics.CulturalIntelligenceHealthStatus>> GetCulturalIntelligenceHealthStatusAsync(
        CancellationToken cancellationToken = default);

    Task<Result<MonitoringMetrics.CulturalDataQualityMetrics>> TrackCulturalDataQualityAsync(
        CulturalDataType dataType,
        double accuracyScore,
        double completenessScore,
        double freshnessScore,
        double culturalAuthenticityScore,
        string communityId,
        CancellationToken cancellationToken = default);

    Task<Result<MonitoringMetrics.CulturalContextPerformanceRecord>> TrackCulturalContextPerformanceAsync(
        CulturalContext culturalContext,
        MonitoringMetrics.CulturalContextPerformanceMetrics performanceMetrics,
        CancellationToken cancellationToken = default);

    Task<Result<MonitoringMetrics.CulturalIntelligenceAlert>> TrackAlertingEventAsync(
        MonitoringMetrics.CulturalIntelligenceAlertType alertType,
        AlertSeverity severity,
        string description,
        IList<MonitoringMetrics.CulturalIntelligenceEndpoint> affectedEndpoints,
        IList<string> impactedCommunities,
        CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<MonitoringMetrics.CulturalApiPerformanceMetrics>>> GetApiPerformanceHistoryAsync(
        MonitoringMetrics.CulturalIntelligenceEndpoint endpoint,
        TimeSpan timeRange,
        CancellationToken cancellationToken = default);

    Task<Result<IEnumerable<MonitoringMetrics.DiasporaEngagementTrends>>> GetDiasporaEngagementTrendsAsync(
        string communityId,
        TimeSpan timeRange,
        CancellationToken cancellationToken = default);

    Task<Result<MonitoringMetrics.EnterpriseClientDashboard>> GetEnterpriseClientDashboardAsync(
        string clientId,
        TimeSpan timeRange,
        CancellationToken cancellationToken = default);
}