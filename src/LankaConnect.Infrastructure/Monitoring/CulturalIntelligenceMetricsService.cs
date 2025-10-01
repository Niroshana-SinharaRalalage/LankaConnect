using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.Monitoring;
using MonitoringMetrics = LankaConnect.Domain.Common.Monitoring;
// Note: Interface expects simple CulturalContext with basic properties
using System.Diagnostics;

namespace LankaConnect.Infrastructure.Monitoring;

public class CulturalIntelligenceMetricsService : ICulturalIntelligenceMetricsService
{
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger<CulturalIntelligenceMetricsService> _logger;
    private readonly ActivitySource _activitySource;
    private bool _disposed;

    public CulturalIntelligenceMetricsService(
        TelemetryClient telemetryClient,
        ILogger<CulturalIntelligenceMetricsService> logger)
    {
        _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _activitySource = new ActivitySource("LankaConnect.CulturalIntelligence.Metrics");
    }

    public async Task<Result<MonitoringMetrics.CulturalApiPerformanceMetrics>> TrackCulturalApiPerformanceAsync(
        MonitoringMetrics.CulturalIntelligenceEndpoint endpoint,
        TimeSpan responseTime,
        int requestSize,
        int responseSize,
        CulturalContext culturalContext,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var activity = _activitySource.StartActivity("TrackCulturalApiPerformance");
            
            var metrics = new CulturalApiPerformanceMetrics
            {
                Endpoint = endpoint,
                ResponseTime = responseTime,
                RequestSize = requestSize,
                ResponseSize = responseSize,
                CulturalContext = culturalContext,
                Timestamp = DateTime.UtcNow
            };

            // Track custom telemetry
            var telemetry = new EventTelemetry("CulturalApiPerformance");
            telemetry.Properties["Endpoint"] = endpoint.ToString();
            telemetry.Properties["CommunityId"] = culturalContext.CommunityId;
            telemetry.Properties["GeographicRegion"] = culturalContext.GeographicRegion;
            telemetry.Properties["Language"] = culturalContext.Language;
            telemetry.Metrics["ResponseTimeMs"] = responseTime.TotalMilliseconds;
            telemetry.Metrics["RequestSizeBytes"] = requestSize;
            telemetry.Metrics["ResponseSizeBytes"] = responseSize;

            _telemetryClient.TrackEvent(telemetry);

            // Track response time as dependency
            var dependencyTelemetry = new DependencyTelemetry(
                "CulturalIntelligenceApi",
                endpoint.ToString(),
                $"Cultural API: {endpoint}",
                DateTime.UtcNow.Subtract(responseTime),
                responseTime,
                true);
            
            dependencyTelemetry.Properties["CommunityId"] = culturalContext.CommunityId;
            _telemetryClient.TrackDependency(dependencyTelemetry);

            _logger.LogInformation(
                "Tracked cultural API performance for {Endpoint}: {ResponseTimeMs}ms, Community: {CommunityId}",
                endpoint, responseTime.TotalMilliseconds, culturalContext.CommunityId);

            await Task.CompletedTask; // For async compliance
            return Result.Success(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track cultural API performance for {Endpoint}", endpoint);
            return Result.Failure<CulturalApiPerformanceMetrics>($"Failed to track API performance: {ex.Message}");
        }
    }

    public async Task<Result<CulturalApiAccuracyMetrics>> TrackCulturalApiAccuracyAsync(
        MonitoringMetrics.CulturalIntelligenceEndpoint endpoint,
        double accuracyScore,
        double confidenceScore,
        double culturalRelevanceScore,
        string communityId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metrics = new CulturalApiAccuracyMetrics
            {
                Endpoint = endpoint,
                AccuracyScore = accuracyScore,
                ConfidenceScore = confidenceScore,
                CulturalRelevanceScore = culturalRelevanceScore,
                CommunityId = communityId,
                Timestamp = DateTime.UtcNow
            };

            var telemetry = new EventTelemetry("CulturalApiAccuracy");
            telemetry.Properties["Endpoint"] = endpoint.ToString();
            telemetry.Properties["CommunityId"] = communityId;
            telemetry.Metrics["AccuracyScore"] = accuracyScore;
            telemetry.Metrics["ConfidenceScore"] = confidenceScore;
            telemetry.Metrics["CulturalRelevanceScore"] = culturalRelevanceScore;

            _telemetryClient.TrackEvent(telemetry);

            _logger.LogInformation(
                "Tracked cultural API accuracy for {Endpoint}: Accuracy={AccuracyScore:P2}, Community={CommunityId}",
                endpoint, accuracyScore, communityId);

            await Task.CompletedTask;
            return Result.Success(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track cultural API accuracy for {Endpoint}", endpoint);
            return Result.Failure<CulturalApiAccuracyMetrics>($"Failed to track API accuracy: {ex.Message}");
        }
    }

    public async Task<Result<ApiResponseTimeMetrics>> TrackApiResponseTimeAsync(
        MonitoringMetrics.CulturalIntelligenceEndpoint endpoint,
        TimeSpan responseTime,
        CulturalContext culturalContext,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var slaTarget = GetSlaTargetForEndpoint(endpoint);
            var metrics = new ApiResponseTimeMetrics
            {
                Endpoint = endpoint,
                ResponseTime = responseTime,
                CulturalContext = culturalContext,
                Timestamp = DateTime.UtcNow,
                IsSlaBreach = responseTime > slaTarget,
                SlaTarget = slaTarget
            };

            var telemetry = new MetricTelemetry(
                "CulturalApiResponseTime",
                responseTime.TotalMilliseconds);
            telemetry.Properties["Endpoint"] = endpoint.ToString();
            telemetry.Properties["CommunityId"] = culturalContext.CommunityId;
            telemetry.Properties["IsSlaBreach"] = metrics.IsSlaBreach.ToString();

            _telemetryClient.TrackMetric(telemetry);

            await Task.CompletedTask;
            return Result.Success(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track API response time for {Endpoint}", endpoint);
            return Result.Failure<ApiResponseTimeMetrics>($"Failed to track response time: {ex.Message}");
        }
    }

    public async Task<Result<DiasporaEngagementMetrics>> TrackDiasporaEngagementAsync(
        string communityId,
        DiasporaEngagementType engagementType,
        string geographicRegion,
        int userCount,
        double engagementScore,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metrics = new DiasporaEngagementMetrics
            {
                CommunityId = communityId,
                EngagementType = engagementType,
                GeographicRegion = geographicRegion,
                UserCount = userCount,
                EngagementScore = engagementScore,
                Timestamp = DateTime.UtcNow
            };

            var telemetry = new EventTelemetry("DiasporaEngagement");
            telemetry.Properties["CommunityId"] = communityId;
            telemetry.Properties["EngagementType"] = engagementType.ToString();
            telemetry.Properties["GeographicRegion"] = geographicRegion;
            telemetry.Metrics["UserCount"] = userCount;
            telemetry.Metrics["EngagementScore"] = engagementScore;

            _telemetryClient.TrackEvent(telemetry);

            await Task.CompletedTask;
            return Result.Success(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track diaspora engagement for {CommunityId}", communityId);
            return Result.Failure<DiasporaEngagementMetrics>($"Failed to track engagement: {ex.Message}");
        }
    }

    public async Task<Result<EnterpriseClientSlaMetrics>> TrackEnterpriseClientSlaAsync(
        string clientId,
        EnterpriseContractTier contractTier,
        TimeSpan slaTarget,
        TimeSpan actualResponseTime,
        CulturalFeatureUsageMetrics culturalFeatureUsage,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var isCompliant = actualResponseTime <= slaTarget;
            var variance = actualResponseTime - slaTarget;
            
            var metrics = new EnterpriseClientSlaMetrics
            {
                ClientId = clientId,
                ContractTier = contractTier,
                SlaTarget = slaTarget,
                ActualResponseTime = actualResponseTime,
                SlaCompliance = isCompliant,
                ResponseTimeVariance = variance,
                CulturalFeatureUsage = culturalFeatureUsage,
                Timestamp = DateTime.UtcNow
            };

            var telemetry = new EventTelemetry("EnterpriseClientSla");
            telemetry.Properties["ClientId"] = clientId;
            telemetry.Properties["ContractTier"] = contractTier.ToString();
            telemetry.Properties["SlaCompliance"] = isCompliant.ToString();
            telemetry.Metrics["SlaTargetMs"] = slaTarget.TotalMilliseconds;
            telemetry.Metrics["ActualResponseTimeMs"] = actualResponseTime.TotalMilliseconds;
            telemetry.Metrics["VarianceMs"] = variance.TotalMilliseconds;

            _telemetryClient.TrackEvent(telemetry);

            if (!isCompliant)
            {
                _logger.LogWarning(
                    "SLA breach detected for client {ClientId}: Target={SlaTargetMs}ms, Actual={ActualMs}ms",
                    clientId, slaTarget.TotalMilliseconds, actualResponseTime.TotalMilliseconds);
            }

            await Task.CompletedTask;
            return Result.Success(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track enterprise client SLA for {ClientId}", clientId);
            return Result.Failure<EnterpriseClientSlaMetrics>($"Failed to track SLA: {ex.Message}");
        }
    }

    public async Task<Result<LankaConnect.Domain.Common.Monitoring.RevenueImpactMetrics>> TrackRevenueImpactAsync(
        CulturalIntelligenceEndpoint apiEndpoint,
        decimal revenuePerRequest,
        int requestCount,
        SubscriptionTier subscriptionTier,
        ClientSegment clientSegment,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var totalRevenue = revenuePerRequest * requestCount;
            var metrics = new LankaConnect.Domain.Common.Monitoring.RevenueImpactMetrics
            {
                ApiEndpoint = apiEndpoint,
                RevenuePerRequest = revenuePerRequest,
                RequestCount = requestCount,
                TotalRevenue = totalRevenue,
                SubscriptionTier = subscriptionTier,
                ClientSegment = clientSegment,
                Timestamp = DateTime.UtcNow
            };

            var telemetry = new EventTelemetry("RevenueImpact");
            telemetry.Properties["ApiEndpoint"] = apiEndpoint.ToString();
            telemetry.Properties["SubscriptionTier"] = subscriptionTier.ToString();
            telemetry.Properties["ClientSegment"] = clientSegment.ToString();
            telemetry.Metrics["RevenuePerRequest"] = (double)revenuePerRequest;
            telemetry.Metrics["RequestCount"] = requestCount;
            telemetry.Metrics["TotalRevenue"] = (double)totalRevenue;

            _telemetryClient.TrackEvent(telemetry);

            await Task.CompletedTask;
            return Result.Success(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track revenue impact for {ApiEndpoint}", apiEndpoint);
            return Result.Failure<RevenueImpactMetrics>($"Failed to track revenue: {ex.Message}");
        }
    }

    public async Task<Result<CulturalIntelligenceHealthStatus>> GetCulturalIntelligenceHealthStatusAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            var healthStatus = new CulturalIntelligenceHealthStatus
            {
                OverallHealthScore = 0.95, // 95% healthy
                LastUpdated = DateTime.UtcNow,
                Status = "Healthy"
            };

            // Simulate component health checks
            healthStatus.ComponentHealthStatuses["BuddhistCalendar"] = true;
            healthStatus.ComponentHealthStatuses["HinduCalendar"] = true;
            healthStatus.ComponentHealthStatuses["CulturalAppropriateness"] = true;
            healthStatus.ComponentHealthStatuses["DiasporaAnalytics"] = true;

            // Simulate endpoint health scores
            foreach (CulturalIntelligenceEndpoint endpoint in Enum.GetValues<CulturalIntelligenceEndpoint>())
            {
                healthStatus.EndpointHealthScores[endpoint] = Random.Shared.NextDouble() * 0.1 + 0.9; // 90-100%
            }

            await Task.CompletedTask;
            return Result.Success(healthStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cultural intelligence health status");
            return Result.Failure<CulturalIntelligenceHealthStatus>($"Failed to get health status: {ex.Message}");
        }
    }

    public async Task<Result<CulturalDataQualityMetrics>> TrackCulturalDataQualityAsync(
        CulturalDataType dataType,
        double accuracyScore,
        double completenessScore,
        double freshnessScore,
        double culturalAuthenticityScore,
        string communityId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metrics = new CulturalDataQualityMetrics
            {
                DataType = dataType,
                AccuracyScore = accuracyScore,
                CompletenessScore = completenessScore,
                FreshnessScore = freshnessScore,
                CulturalAuthenticityScore = culturalAuthenticityScore,
                CommunityId = communityId,
                Timestamp = DateTime.UtcNow,
                LastDataUpdate = DateTime.UtcNow.AddHours(-2) // Simulate last update
            };

            var telemetry = new EventTelemetry("CulturalDataQuality");
            telemetry.Properties["DataType"] = dataType.ToString();
            telemetry.Properties["CommunityId"] = communityId;
            telemetry.Metrics["AccuracyScore"] = accuracyScore;
            telemetry.Metrics["CompletenessScore"] = completenessScore;
            telemetry.Metrics["FreshnessScore"] = freshnessScore;
            telemetry.Metrics["CulturalAuthenticityScore"] = culturalAuthenticityScore;

            _telemetryClient.TrackEvent(telemetry);

            await Task.CompletedTask;
            return Result.Success(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track cultural data quality for {DataType}", dataType);
            return Result.Failure<CulturalDataQualityMetrics>($"Failed to track data quality: {ex.Message}");
        }
    }

    public async Task<Result<CulturalContextPerformanceRecord>> TrackCulturalContextPerformanceAsync(
        CulturalContext culturalContext,
        CulturalContextPerformanceMetrics performanceMetrics,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var record = new CulturalContextPerformanceRecord
            {
                CulturalContext = culturalContext,
                PerformanceMetrics = performanceMetrics,
                Timestamp = DateTime.UtcNow
            };

            var telemetry = new EventTelemetry("CulturalContextPerformance");
            telemetry.Properties["CommunityId"] = culturalContext.CulturalBackground.ToString();
            telemetry.Properties["GeographicRegion"] = culturalContext.GeographicRegion.ToString();
            telemetry.Properties["Language"] = culturalContext.Language.ToString();
            telemetry.Metrics["ContextResolutionTimeMs"] = performanceMetrics.ContextResolutionTime.TotalMilliseconds;
            telemetry.Metrics["CulturalRelevanceScore"] = performanceMetrics.CulturalRelevanceScore;

            _telemetryClient.TrackEvent(telemetry);

            await Task.CompletedTask;
            return Result.Success(record);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track cultural context performance");
            return Result.Failure<CulturalContextPerformanceRecord>($"Failed to track context performance: {ex.Message}");
        }
    }

    public async Task<Result<CulturalIntelligenceAlert>> TrackAlertingEventAsync(
        LankaConnect.Domain.Common.Monitoring.CulturalIntelligenceAlertType alertType,
        AlertSeverity severity,
        string description,
        IList<LankaConnect.Domain.Common.Monitoring.CulturalIntelligenceEndpoint> affectedEndpoints,
        IList<string> impactedCommunities,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var alert = new CulturalIntelligenceAlert
            {
                AlertType = alertType,
                Severity = severity,
                Description = description,
                AffectedEndpoints = affectedEndpoints.ToList(),
                ImpactedCommunities = impactedCommunities.ToList(),
                Timestamp = DateTime.UtcNow
            };

            var telemetry = new EventTelemetry("CulturalIntelligenceAlert");
            telemetry.Properties["AlertType"] = alertType.ToString();
            telemetry.Properties["Severity"] = severity.ToString();
            telemetry.Properties["Description"] = description;
            telemetry.Properties["AffectedEndpoints"] = string.Join(",", affectedEndpoints);
            telemetry.Properties["ImpactedCommunities"] = string.Join(",", impactedCommunities);

            _telemetryClient.TrackEvent(telemetry);

            // Log critical alerts
            if (severity >= AlertSeverity.Critical)
            {
                _logger.LogCritical(
                    "Critical cultural intelligence alert: {AlertType} - {Description}",
                    alertType, description);
            }

            await Task.CompletedTask;
            return Result.Success(alert);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track alerting event for {AlertType}", alertType);
            return Result.Failure<CulturalIntelligenceAlert>($"Failed to track alert: {ex.Message}");
        }
    }

    // Additional methods for comprehensive monitoring
    public async Task<Result<IEnumerable<CulturalApiPerformanceMetrics>>> GetApiPerformanceHistoryAsync(
        MonitoringMetrics.CulturalIntelligenceEndpoint endpoint,
        TimeSpan timeRange,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // In a real implementation, this would query a time-series database
            // For now, return mock data
            var mockData = new List<CulturalApiPerformanceMetrics>();
            await Task.CompletedTask;
            return Result.Success<IEnumerable<CulturalApiPerformanceMetrics>>(mockData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get API performance history for {Endpoint}", endpoint);
            return Result.Failure<IEnumerable<CulturalApiPerformanceMetrics>>($"Failed to get performance history: {ex.Message}");
        }
    }

    public async Task<Result<IEnumerable<DiasporaEngagementTrends>>> GetDiasporaEngagementTrendsAsync(
        string communityId,
        TimeSpan timeRange,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var mockData = new List<DiasporaEngagementTrends>();
            await Task.CompletedTask;
            return Result.Success<IEnumerable<DiasporaEngagementTrends>>(mockData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get diaspora engagement trends for {CommunityId}", communityId);
            return Result.Failure<IEnumerable<DiasporaEngagementTrends>>($"Failed to get engagement trends: {ex.Message}");
        }
    }

    public async Task<Result<EnterpriseClientDashboard>> GetEnterpriseClientDashboardAsync(
        string clientId,
        TimeSpan timeRange,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var dashboard = new EnterpriseClientDashboard
            {
                ClientId = clientId,
                ContractTier = EnterpriseContractTier.Fortune500Premium,
                OverallFeatureUsage = new CulturalFeatureUsageMetrics(),
                LastUpdated = DateTime.UtcNow
            };

            await Task.CompletedTask;
            return Result.Success(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get enterprise client dashboard for {ClientId}", clientId);
            return Result.Failure<EnterpriseClientDashboard>($"Failed to get client dashboard: {ex.Message}");
        }
    }

    public async Task<Result<CulturalIntelligenceAlertResult>> TrackAlertingEventAsync(
        CulturalIntelligenceAlertType alertType,
        AlertSeverity severity,
        string alertDescription,
        IList<CulturalIntelligenceEndpoint> affectedEndpoints,
        IList<string> affectedRegions,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var activity = _activitySource.StartActivity("TrackAlertingEvent");
            activity?.SetTag("alert.type", alertType.ToString());
            activity?.SetTag("alert.severity", severity.ToString());
            activity?.SetTag("alert.endpoints_count", affectedEndpoints.Count);

            var alertResult = new CulturalIntelligenceAlertResult
            {
                AlertId = Guid.NewGuid(),
                AlertType = alertType,
                Severity = severity,
                Description = alertDescription,
                AffectedEndpoints = affectedEndpoints.ToList(),
                AffectedRegions = affectedRegions.ToList(),
                Timestamp = DateTime.UtcNow,
                AlertProcessed = true
            };

            // Log the alert based on severity
            switch (severity)
            {
                case AlertSeverity.Critical:
                    _logger.LogCritical("CRITICAL Cultural Intelligence Alert: {AlertType} - {Description}. Affected endpoints: {EndpointCount}, regions: {RegionCount}",
                        alertType, alertDescription, affectedEndpoints.Count, affectedRegions.Count);
                    break;
                case AlertSeverity.High:
                    _logger.LogError("HIGH Cultural Intelligence Alert: {AlertType} - {Description}. Affected endpoints: {EndpointCount}, regions: {RegionCount}",
                        alertType, alertDescription, affectedEndpoints.Count, affectedRegions.Count);
                    break;
                case AlertSeverity.Medium:
                    _logger.LogWarning("MEDIUM Cultural Intelligence Alert: {AlertType} - {Description}. Affected endpoints: {EndpointCount}, regions: {RegionCount}",
                        alertType, alertDescription, affectedEndpoints.Count, affectedRegions.Count);
                    break;
                default:
                    _logger.LogInformation("Cultural Intelligence Alert: {AlertType} - {Description}. Affected endpoints: {EndpointCount}, regions: {RegionCount}",
                        alertType, alertDescription, affectedEndpoints.Count, affectedRegions.Count);
                    break;
            }

            // Track metrics for alert processing
            using var meter = _meterProvider.GetMeter("CulturalIntelligence.Alerts");
            var alertCounter = meter.CreateCounter<int>("cultural_intelligence_alerts_total");
            alertCounter.Add(1, new KeyValuePair<string, object?>("alert_type", alertType.ToString()),
                               new KeyValuePair<string, object?>("severity", severity.ToString()));

            await Task.CompletedTask;
            return Result.Success(alertResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track alerting event for {AlertType} with severity {Severity}", alertType, severity);
            return Result.Failure<CulturalIntelligenceAlertResult>($"Failed to track alerting event: {ex.Message}");
        }
    }

    private static TimeSpan GetSlaTargetForEndpoint(CulturalIntelligenceEndpoint endpoint)
    {
        return endpoint switch
        {
            CulturalIntelligenceEndpoint.BuddhistCalendar => TimeSpan.FromMilliseconds(50),
            CulturalIntelligenceEndpoint.HinduCalendar => TimeSpan.FromMilliseconds(50),
            CulturalIntelligenceEndpoint.CulturalAppropriateness => TimeSpan.FromMilliseconds(100),
            CulturalIntelligenceEndpoint.DiasporaAnalytics => TimeSpan.FromMilliseconds(200),
            CulturalIntelligenceEndpoint.EventRecommendations => TimeSpan.FromMilliseconds(150),
            _ => TimeSpan.FromMilliseconds(200)
        };
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
            _activitySource?.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Cultural Intelligence Alert Result
/// </summary>
public class CulturalIntelligenceAlertResult
{
    public Guid AlertId { get; set; }
    public CulturalIntelligenceAlertType AlertType { get; set; }
    public AlertSeverity Severity { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<CulturalIntelligenceEndpoint> AffectedEndpoints { get; set; } = new();
    public List<string> AffectedRegions { get; set; } = new();
    public DateTime Timestamp { get; set; }
    public bool AlertProcessed { get; set; }
}

/// <summary>
/// Alert severity levels
/// </summary>
public enum AlertSeverity
{
    Low,
    Medium,
    High,
    Critical
}

/// <summary>
/// Cultural Intelligence Alert Types
/// </summary>
public enum CulturalIntelligenceAlertType
{
    PerformanceDegradation,
    ServiceUnavailable,
    HighLatency,
    CulturalEventOverload,
    DataInconsistency,
    SecurityIncident,
    ComplianceViolation,
    SystemHealthAlert
}