using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Monitoring;
using System.Collections.Concurrent;
using System.Text.Json;

namespace LankaConnect.Infrastructure.Monitoring;

public interface IEnterpriseAlertingService
{
    Task<Result> ProcessAlertAsync(CulturalIntelligenceAlert alert, CancellationToken cancellationToken = default);
    Task<Result> CheckSlaComplianceAsync(EnterpriseClientSlaMetrics slaMetrics, CancellationToken cancellationToken = default);
    Task<Result> MonitorCulturalDataQualityAsync(CulturalDataQualityMetrics qualityMetrics, CancellationToken cancellationToken = default);
    Task<Result> TriggerCriticalAlertAsync(CulturalIntelligenceAlertType alertType, string description, List<string> impactedCommunities, CancellationToken cancellationToken = default);
    Task<Result<List<ActiveAlert>>> GetActiveAlertsAsync(CancellationToken cancellationToken = default);
    Task<Result> AcknowledgeAlertAsync(string alertId, string acknowledgedBy, CancellationToken cancellationToken = default);
    Task<Result> ResolveAlertAsync(string alertId, string resolvedBy, string resolution, CancellationToken cancellationToken = default);
}

public class EnterpriseAlertingService : IEnterpriseAlertingService
{
    private readonly TelemetryClient _telemetryClient;
    private readonly ICulturalIntelligenceMetricsService _metricsService;
    private readonly ILogger<EnterpriseAlertingService> _logger;
    private readonly IOptions<AlertingConfiguration> _alertingConfig;
    private readonly IOptions<SlaComplianceOptions> _slaOptions;
    private readonly ConcurrentDictionary<string, ActiveAlert> _activeAlerts;
    private readonly SemaphoreSlim _alertProcessingSemaphore;

    public EnterpriseAlertingService(
        TelemetryClient telemetryClient,
        ICulturalIntelligenceMetricsService metricsService,
        ILogger<EnterpriseAlertingService> logger,
        IOptions<AlertingConfiguration> alertingConfig,
        IOptions<SlaComplianceOptions> slaOptions)
    {
        _telemetryClient = telemetryClient;
        _metricsService = metricsService;
        _logger = logger;
        _alertingConfig = alertingConfig;
        _slaOptions = slaOptions;
        _activeAlerts = new ConcurrentDictionary<string, ActiveAlert>();
        _alertProcessingSemaphore = new SemaphoreSlim(10, 10); // Allow 10 concurrent alert processes
    }

    public async Task<Result> ProcessAlertAsync(CulturalIntelligenceAlert alert, CancellationToken cancellationToken = default)
    {
        try
        {
            await _alertProcessingSemaphore.WaitAsync(cancellationToken);
            
            _logger.LogInformation(
                "Processing cultural intelligence alert: {AlertType} - {Severity} - {Description}",
                alert.AlertType, alert.Severity, alert.Description);

            // Create active alert record
            var activeAlert = new ActiveAlert
            {
                AlertId = alert.AlertId,
                AlertType = alert.AlertType,
                Severity = alert.Severity,
                Description = alert.Description,
                AffectedEndpoints = alert.AffectedEndpoints,
                ImpactedCommunities = alert.ImpactedCommunities,
                CreatedAt = alert.Timestamp,
                IsAcknowledged = false,
                IsResolved = false
            };

            _activeAlerts.TryAdd(alert.AlertId, activeAlert);

            // Determine alert handling strategy based on severity
            var handlingResult = alert.Severity switch
            {
                AlertSeverity.Critical or AlertSeverity.Emergency => await HandleCriticalAlertAsync(activeAlert, cancellationToken),
                AlertSeverity.High => await HandleHighPriorityAlertAsync(activeAlert, cancellationToken),
                AlertSeverity.Medium => await HandleMediumPriorityAlertAsync(activeAlert, cancellationToken),
                AlertSeverity.Low => await HandleLowPriorityAlertAsync(activeAlert, cancellationToken),
                _ => Result.Success()
            };

            if (!handlingResult.IsSuccess)
            {
                _logger.LogError("Failed to handle alert {AlertId}: {Error}", alert.AlertId, handlingResult.Error);
            }

            // Track alert processing metrics
            _telemetryClient.TrackEvent("AlertProcessed", new Dictionary<string, string>
            {
                ["AlertId"] = alert.AlertId,
                ["AlertType"] = alert.AlertType.ToString(),
                ["Severity"] = alert.Severity.ToString(),
                ["ProcessingSuccess"] = handlingResult.IsSuccess.ToString(),
                ["ImpactedCommunities"] = string.Join(",", alert.ImpactedCommunities)
            });

            return handlingResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process alert {AlertId}", alert.AlertId);
            return Result.Failure($"Alert processing failed: {ex.Message}");
        }
        finally
        {
            _alertProcessingSemaphore.Release();
        }
    }

    public async Task<Result> CheckSlaComplianceAsync(
        EnterpriseClientSlaMetrics slaMetrics, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug(
                "Checking SLA compliance for client {ClientId}: Target={TargetMs}ms, Actual={ActualMs}ms",
                slaMetrics.ClientId, slaMetrics.SlaTarget.TotalMilliseconds, slaMetrics.ActualResponseTime.TotalMilliseconds);

            // Check if SLA breach occurred
            if (!slaMetrics.SlaCompliance)
            {
                var breachSeverity = DetermineSlaBreachSeverity(slaMetrics);
                var alert = new CulturalIntelligenceAlert
                {
                    AlertType = CulturalIntelligenceAlertType.SlaComplianceBreach,
                    Severity = breachSeverity,
                    Description = $"SLA breach detected for client {slaMetrics.ClientId}. Target: {slaMetrics.SlaTarget.TotalMilliseconds}ms, Actual: {slaMetrics.ActualResponseTime.TotalMilliseconds}ms",
                    ImpactedCommunities = new List<string> { $"enterprise_client_{slaMetrics.ClientId}" }
                };

                await ProcessAlertAsync(alert, cancellationToken);
                
                // Track SLA breach
                _telemetryClient.TrackEvent("SlaComplianceBreach", new Dictionary<string, string>
                {
                    ["ClientId"] = slaMetrics.ClientId,
                    ["ContractTier"] = slaMetrics.ContractTier.ToString(),
                    ["TargetMs"] = slaMetrics.SlaTarget.TotalMilliseconds.ToString("F2"),
                    ["ActualMs"] = slaMetrics.ActualResponseTime.TotalMilliseconds.ToString("F2"),
                    ["VarianceMs"] = slaMetrics.ResponseTimeVariance.TotalMilliseconds.ToString("F2"),
                    ["BreachSeverity"] = breachSeverity.ToString()
                });
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check SLA compliance for client {ClientId}", slaMetrics.ClientId);
            return Result.Failure($"SLA compliance check failed: {ex.Message}");
        }
    }

    public async Task<Result> MonitorCulturalDataQualityAsync(
        CulturalDataQualityMetrics qualityMetrics, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var qualityIssues = new List<string>();
            
            // Check accuracy threshold (95% minimum)
            if (qualityMetrics.AccuracyScore < 0.95)
            {
                qualityIssues.Add($"Data accuracy below threshold: {qualityMetrics.AccuracyScore:P2} < 95%");
            }

            // Check completeness threshold (90% minimum)
            if (qualityMetrics.CompletenessScore < 0.90)
            {
                qualityIssues.Add($"Data completeness below threshold: {qualityMetrics.CompletenessScore:P2} < 90%");
            }

            // Check freshness threshold (85% minimum)
            if (qualityMetrics.FreshnessScore < 0.85)
            {
                qualityIssues.Add($"Data freshness below threshold: {qualityMetrics.FreshnessScore:P2} < 85%");
            }

            // Check cultural authenticity threshold (90% minimum)
            if (qualityMetrics.CulturalAuthenticityScore < 0.90)
            {
                qualityIssues.Add($"Cultural authenticity below threshold: {qualityMetrics.CulturalAuthenticityScore:P2} < 90%");
            }

            if (qualityIssues.Any())
            {
                var alert = new CulturalIntelligenceAlert
                {
                    AlertType = CulturalIntelligenceAlertType.DataQualityDegradation,
                    Severity = DetermineDataQualitySeverity(qualityMetrics),
                    Description = $"Cultural data quality issues detected for {qualityMetrics.DataType} in community {qualityMetrics.CommunityId}: {string.Join(", ", qualityIssues)}",
                    ImpactedCommunities = new List<string> { qualityMetrics.CommunityId }
                };

                await ProcessAlertAsync(alert, cancellationToken);
            }

            // Track data quality metrics
            _telemetryClient.TrackEvent("CulturalDataQualityMonitored", new Dictionary<string, string>
            {
                ["DataType"] = qualityMetrics.DataType.ToString(),
                ["CommunityId"] = qualityMetrics.CommunityId,
                ["AccuracyScore"] = qualityMetrics.AccuracyScore.ToString("F3"),
                ["CompletenessScore"] = qualityMetrics.CompletenessScore.ToString("F3"),
                ["FreshnessScore"] = qualityMetrics.FreshnessScore.ToString("F3"),
                ["CulturalAuthenticityScore"] = qualityMetrics.CulturalAuthenticityScore.ToString("F3"),
                ["QualityIssuesCount"] = qualityIssues.Count.ToString()
            });

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to monitor cultural data quality for {DataType}", qualityMetrics.DataType);
            return Result.Failure($"Data quality monitoring failed: {ex.Message}");
        }
    }

    public async Task<Result> TriggerCriticalAlertAsync(
        CulturalIntelligenceAlertType alertType, 
        string description, 
        List<string> impactedCommunities, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var alert = new CulturalIntelligenceAlert
            {
                AlertType = alertType,
                Severity = AlertSeverity.Critical,
                Description = description,
                ImpactedCommunities = impactedCommunities,
                Timestamp = DateTime.UtcNow
            };

            var result = await ProcessAlertAsync(alert, cancellationToken);
            
            if (result.IsSuccess)
            {
                _logger.LogCritical(
                    "Critical alert triggered: {AlertType} - {Description} - Communities: {Communities}",
                    alertType, description, string.Join(",", impactedCommunities));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger critical alert for {AlertType}", alertType);
            return Result.Failure($"Critical alert trigger failed: {ex.Message}");
        }
    }

    public async Task<Result<List<ActiveAlert>>> GetActiveAlertsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var activeAlerts = _activeAlerts.Values
                .Where(alert => !alert.IsResolved)
                .OrderByDescending(alert => alert.Severity)
                .ThenByDescending(alert => alert.CreatedAt)
                .ToList();

            await Task.CompletedTask; // For async compliance
            return Result.Success(activeAlerts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active alerts");
            return Result.Failure<List<ActiveAlert>>($"Failed to get active alerts: {ex.Message}");
        }
    }

    public async Task<Result> AcknowledgeAlertAsync(string alertId, string acknowledgedBy, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_activeAlerts.TryGetValue(alertId, out var alert))
            {
                alert.IsAcknowledged = true;
                alert.AcknowledgedAt = DateTime.UtcNow;
                alert.AcknowledgedBy = acknowledgedBy;

                _logger.LogInformation("Alert {AlertId} acknowledged by {AcknowledgedBy}", alertId, acknowledgedBy);
                
                _telemetryClient.TrackEvent("AlertAcknowledged", new Dictionary<string, string>
                {
                    ["AlertId"] = alertId,
                    ["AcknowledgedBy"] = acknowledgedBy,
                    ["AlertType"] = alert.AlertType.ToString(),
                    ["Severity"] = alert.Severity.ToString()
                });

                await Task.CompletedTask;
                return Result.Success();
            }
            else
            {
                return Result.Failure($"Alert {alertId} not found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to acknowledge alert {AlertId}", alertId);
            return Result.Failure($"Alert acknowledgment failed: {ex.Message}");
        }
    }

    public async Task<Result> ResolveAlertAsync(string alertId, string resolvedBy, string resolution, CancellationToken cancellationToken = default)
    {
        try
        {
            if (_activeAlerts.TryGetValue(alertId, out var alert))
            {
                alert.IsResolved = true;
                alert.ResolvedAt = DateTime.UtcNow;
                alert.ResolvedBy = resolvedBy;
                alert.Resolution = resolution;

                _logger.LogInformation(
                    "Alert {AlertId} resolved by {ResolvedBy}: {Resolution}", 
                    alertId, resolvedBy, resolution);
                
                _telemetryClient.TrackEvent("AlertResolved", new Dictionary<string, string>
                {
                    ["AlertId"] = alertId,
                    ["ResolvedBy"] = resolvedBy,
                    ["Resolution"] = resolution,
                    ["AlertType"] = alert.AlertType.ToString(),
                    ["Severity"] = alert.Severity.ToString(),
                    ["ResolutionTimeMinutes"] = ((DateTime.UtcNow - alert.CreatedAt).TotalMinutes).ToString("F2")
                });

                await Task.CompletedTask;
                return Result.Success();
            }
            else
            {
                return Result.Failure($"Alert {alertId} not found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve alert {AlertId}", alertId);
            return Result.Failure($"Alert resolution failed: {ex.Message}");
        }
    }

    // Private helper methods
    private async Task<Result> HandleCriticalAlertAsync(ActiveAlert alert, CancellationToken cancellationToken)
    {
        try
        {
            // Immediate notification to all critical channels
            await SendSlackNotificationAsync(alert, "#critical-alerts", cancellationToken);
            await SendTeamsNotificationAsync(alert, cancellationToken);
            await SendEmailNotificationAsync(alert, _alertingConfig.Value.AlertRecipients, cancellationToken);
            
            // Trigger pager duty for emergency alerts
            if (alert.Severity == AlertSeverity.Emergency)
            {
                await TriggerPagerDutyAsync(alert, cancellationToken);
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to handle critical alert {AlertId}", alert.AlertId);
            return Result.Failure($"Critical alert handling failed: {ex.Message}");
        }
    }

    private async Task<Result> HandleHighPriorityAlertAsync(ActiveAlert alert, CancellationToken cancellationToken)
    {
        await SendSlackNotificationAsync(alert, "#high-priority-alerts", cancellationToken);
        await SendTeamsNotificationAsync(alert, cancellationToken);
        return Result.Success();
    }

    private async Task<Result> HandleMediumPriorityAlertAsync(ActiveAlert alert, CancellationToken cancellationToken)
    {
        await SendSlackNotificationAsync(alert, "#alerts", cancellationToken);
        return Result.Success();
    }

    private async Task<Result> HandleLowPriorityAlertAsync(ActiveAlert alert, CancellationToken cancellationToken)
    {
        // Log only for low priority alerts
        _logger.LogInformation("Low priority alert: {AlertType} - {Description}", alert.AlertType, alert.Description);
        await Task.CompletedTask;
        return Result.Success();
    }

    private AlertSeverity DetermineSlaBreachSeverity(EnterpriseClientSlaMetrics slaMetrics)
    {
        var varianceRatio = slaMetrics.ResponseTimeVariance.TotalMilliseconds / slaMetrics.SlaTarget.TotalMilliseconds;
        
        return varianceRatio switch
        {
            > 2.0 => AlertSeverity.Emergency,  // More than 300% of target
            > 1.0 => AlertSeverity.Critical,   // More than 200% of target
            > 0.5 => AlertSeverity.High,       // More than 150% of target
            > 0.2 => AlertSeverity.Medium,     // More than 120% of target
            _ => AlertSeverity.Low
        };
    }

    private AlertSeverity DetermineDataQualitySeverity(CulturalDataQualityMetrics qualityMetrics)
    {
        var minScore = Math.Min(
            Math.Min(qualityMetrics.AccuracyScore, qualityMetrics.CompletenessScore),
            Math.Min(qualityMetrics.FreshnessScore, qualityMetrics.CulturalAuthenticityScore));
        
        return minScore switch
        {
            < 0.70 => AlertSeverity.Critical,   // Below 70%
            < 0.80 => AlertSeverity.High,       // Below 80%
            < 0.90 => AlertSeverity.Medium,     // Below 90%
            _ => AlertSeverity.Low
        };
    }

    // Notification methods (simplified implementations)
    private async Task SendSlackNotificationAsync(ActiveAlert alert, string channel, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_alertingConfig.Value.SlackWebhookUrl)) return;
        
        // Implementation would send to Slack
        _logger.LogInformation("Sending Slack notification for alert {AlertId} to {Channel}", alert.AlertId, channel);
        await Task.Delay(100, cancellationToken); // Simulate API call
    }

    private async Task SendTeamsNotificationAsync(ActiveAlert alert, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_alertingConfig.Value.TeamsWebhookUrl)) return;
        
        _logger.LogInformation("Sending Teams notification for alert {AlertId}", alert.AlertId);
        await Task.Delay(100, cancellationToken);
    }

    private async Task SendEmailNotificationAsync(ActiveAlert alert, List<string> recipients, CancellationToken cancellationToken)
    {
        if (!recipients.Any()) return;
        
        _logger.LogInformation("Sending email notification for alert {AlertId} to {Recipients}", 
            alert.AlertId, string.Join(",", recipients));
        await Task.Delay(100, cancellationToken);
    }

    private async Task TriggerPagerDutyAsync(ActiveAlert alert, CancellationToken cancellationToken)
    {
        _logger.LogCritical("Triggering PagerDuty for emergency alert {AlertId}", alert.AlertId);
        await Task.Delay(100, cancellationToken);
    }
}

// Supporting model for active alerts
public class ActiveAlert
{
    public string AlertId { get; set; } = string.Empty;
    public CulturalIntelligenceAlertType AlertType { get; set; }
    public AlertSeverity Severity { get; set; }
    public string Description { get; set; } = string.Empty;
    public List<CulturalIntelligenceEndpoint> AffectedEndpoints { get; set; } = new();
    public List<string> ImpactedCommunities { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public bool IsAcknowledged { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public string AcknowledgedBy { get; set; } = string.Empty;
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string ResolvedBy { get; set; } = string.Empty;
    public string Resolution { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}