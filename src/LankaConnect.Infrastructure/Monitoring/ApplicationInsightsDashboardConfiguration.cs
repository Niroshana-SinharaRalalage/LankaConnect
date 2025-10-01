using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace LankaConnect.Infrastructure.Monitoring;

public static class ApplicationInsightsDashboardConfiguration
{
    public static IServiceCollection AddCulturalIntelligenceDashboard(
        this IServiceCollection services, 
        IConfiguration configuration)
    {
        // Enhanced Application Insights configuration for Cultural Intelligence
        services.AddApplicationInsightsTelemetry(options =>
        {
            options.ConnectionString = configuration.GetConnectionString("ApplicationInsights");
            options.EnableDependencyTrackingTelemetryModule = true;
            options.EnableRequestTrackingTelemetryModule = true;
            options.EnableEventCounterCollectionModule = true;
            options.EnablePerformanceCounterCollectionModule = true;
            options.EnableHeartbeat = true;
            options.EnableActiveTelemetryConfigurationSetup = true;
        });

        // Custom telemetry initializers for cultural intelligence context
        services.AddSingleton<ITelemetryInitializer, CulturalIntelligenceTelemetryInitializer>();
        services.AddSingleton<ITelemetryInitializer, DiasporaContextTelemetryInitializer>();
        services.AddSingleton<ITelemetryInitializer, EnterpriseClientTelemetryInitializer>();

        // Cultural Intelligence Dashboard services
        services.AddScoped<ICulturalIntelligenceDashboardService, CulturalIntelligenceDashboardService>();
        services.AddScoped<IRealtimeMetricsCollector, RealtimeMetricsCollector>();
        services.AddScoped<IEnterpriseAlertingService, EnterpriseAlertingService>();

        // Background services for continuous monitoring
        services.AddHostedService<CulturalIntelligenceMonitoringBackgroundService>();
        services.AddHostedService<DiasporaEngagementMetricsCollectionService>();
        services.AddHostedService<EnterpriseClientHealthMonitoringService>();

        return services;
    }

    public static IServiceCollection AddCulturalIntelligenceCustomMetrics(
        this IServiceCollection services)
    {
        // Register custom metric collectors
        services.AddScoped<ICulturalCalendarMetricsCollector, CulturalCalendarMetricsCollector>();
        services.AddScoped<IDiasporaAnalyticsMetricsCollector, DiasporaAnalyticsMetricsCollector>();
        services.AddScoped<ICulturalAppropriatenessMetricsCollector, CulturalAppropriatenessMetricsCollector>();
        services.AddScoped<IBusinessDirectoryMetricsCollector, BusinessDirectoryMetricsCollector>();
        
        // Performance tracking services
        services.AddScoped<ICulturalIntelligencePerformanceTracker, CulturalIntelligencePerformanceTracker>();
        services.AddScoped<IApiResponseTimeTracker, ApiResponseTimeTracker>();
        services.AddScoped<ICulturalAccuracyTracker, CulturalAccuracyTracker>();

        return services;
    }

    public static IServiceCollection AddEnterpriseMonitoringServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Enterprise-grade monitoring configuration
        var enterpriseConfig = configuration.GetSection("EnterpriseMonitoring");
        
        services.Configure<EnterpriseMonitoringOptions>(enterpriseConfig);
        services.Configure<SlaComplianceOptions>(configuration.GetSection("SlaCompliance"));
        services.Configure<AlertingConfiguration>(configuration.GetSection("Alerting"));

        // Enterprise monitoring services
        services.AddScoped<IEnterpriseClientMonitoringService, EnterpriseClientMonitoringService>();
        services.AddScoped<ISlaComplianceTrackingService, SlaComplianceTrackingService>();
        services.AddScoped<IRevenueImpactAnalysisService, RevenueImpactAnalysisService>();
        services.AddScoped<IFortuneClientReportingService, FortuneClientReportingService>();

        // Real-time alerting and notification services
        services.AddScoped<ICriticalAlertService, CriticalAlertService>();
        services.AddScoped<ISlaBreachDetectionService, SlaBreachDetectionService>();
        services.AddScoped<ICulturalDataQualityMonitor, CulturalDataQualityMonitor>();

        return services;
    }
}

// Telemetry Initializers for Cultural Context
public class CulturalIntelligenceTelemetryInitializer : ITelemetryInitializer
{
    public void Initialize(ITelemetry telemetry)
    {
        if (telemetry is ISupportProperties telemetryWithProperties)
        {
            // Add cultural intelligence context to all telemetry
            telemetryWithProperties.Properties["ApplicationLayer"] = "CulturalIntelligence";
            telemetryWithProperties.Properties["ServiceType"] = "DiasporaCommunityPlatform";
            telemetryWithProperties.Properties["BusinessVertical"] = "CulturalTechnology";
            telemetryWithProperties.Properties["MarketSegment"] = "DiasporaConnectivity";
        }
    }
}

public class DiasporaContextTelemetryInitializer : ITelemetryInitializer
{
    private readonly IHttpContextAccessor? _httpContextAccessor;
    
    public DiasporaContextTelemetryInitializer(IServiceProvider serviceProvider)
    {
        // Safely try to get HTTP context accessor (might not be available in background services)
        _httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();
    }

    public void Initialize(ITelemetry telemetry)
    {
        if (telemetry is ISupportProperties telemetryWithProperties && _httpContextAccessor?.HttpContext != null)
        {
            var context = _httpContextAccessor.HttpContext;
            
            // Extract diaspora community context from headers or claims
            if (context.Request.Headers.ContainsKey("X-Community-Id"))
            {
                telemetryWithProperties.Properties["DiasporaCommunity"] = 
                    context.Request.Headers["X-Community-Id"].ToString();
            }
            
            if (context.Request.Headers.ContainsKey("X-Geographic-Region"))
            {
                telemetryWithProperties.Properties["GeographicRegion"] = 
                    context.Request.Headers["X-Geographic-Region"].ToString();
            }
            
            if (context.Request.Headers.ContainsKey("X-Cultural-Language"))
            {
                telemetryWithProperties.Properties["CulturalLanguage"] = 
                    context.Request.Headers["X-Cultural-Language"].ToString();
            }
        }
    }
}

public class EnterpriseClientTelemetryInitializer : ITelemetryInitializer
{
    private readonly IHttpContextAccessor? _httpContextAccessor;
    
    public EnterpriseClientTelemetryInitializer(IServiceProvider serviceProvider)
    {
        _httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();
    }

    public void Initialize(ITelemetry telemetry)
    {
        if (telemetry is ISupportProperties telemetryWithProperties && _httpContextAccessor?.HttpContext != null)
        {
            var context = _httpContextAccessor.HttpContext;
            
            // Extract enterprise client context
            if (context.Request.Headers.ContainsKey("X-Enterprise-Client-Id"))
            {
                telemetryWithProperties.Properties["EnterpriseClientId"] = 
                    context.Request.Headers["X-Enterprise-Client-Id"].ToString();
            }
            
            if (context.Request.Headers.ContainsKey("X-Contract-Tier"))
            {
                telemetryWithProperties.Properties["ContractTier"] = 
                    context.Request.Headers["X-Contract-Tier"].ToString();
            }
            
            if (context.Request.Headers.ContainsKey("X-Sla-Target"))
            {
                telemetryWithProperties.Properties["SlaTarget"] = 
                    context.Request.Headers["X-Sla-Target"].ToString();
            }
        }
    }
}

// Configuration Options
public class EnterpriseMonitoringOptions
{
    public bool EnableRealtimeMonitoring { get; set; } = true;
    public bool EnablePredictiveAlerting { get; set; } = true;
    public bool EnableRevenueTracking { get; set; } = true;
    public bool EnableCulturalAccuracyMonitoring { get; set; } = true;
    public TimeSpan MetricsCollectionInterval { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan DashboardRefreshInterval { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxConcurrentEnterpriseClients { get; set; } = 100;
}

public class SlaComplianceOptions
{
    public Dictionary<string, TimeSpan> EndpointSlaTargets { get; set; } = new();
    public double SlaBreachThreshold { get; set; } = 0.95; // 95% compliance required
    public TimeSpan SlaViolationGracePeriod { get; set; } = TimeSpan.FromMinutes(5);
    public bool EnableAutomaticEscalation { get; set; } = true;
    public List<string> CriticalEnterpriseClients { get; set; } = new();
}

public class AlertingConfiguration
{
    public bool EnableSlackIntegration { get; set; } = true;
    public bool EnableTeamsIntegration { get; set; } = true;
    public bool EnableEmailAlerts { get; set; } = true;
    public bool EnableSmsAlerts { get; set; } = true;
    public string SlackWebhookUrl { get; set; } = string.Empty;
    public string TeamsWebhookUrl { get; set; } = string.Empty;
    public List<string> AlertRecipients { get; set; } = new();
    public Dictionary<string, List<string>> EscalationMatrix { get; set; } = new();
}