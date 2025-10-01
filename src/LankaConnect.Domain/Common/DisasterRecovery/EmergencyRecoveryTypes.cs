using System;
using System.Collections.Generic;

namespace LankaConnect.Domain.Common.DisasterRecovery;

/// <summary>
/// Emergency disaster recovery types to resolve CS0246 compilation errors
/// Phase 1 of 3-hour emergency architecture recovery - minimal implementation for compilation
/// </summary>

public class RevenueRecoveryPlan
{
    public string PlanId { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public decimal EstimatedRevenueLoss { get; set; }
    public Dictionary<string, object> RecoveryActions { get; set; } = new();
    public TimeSpan EstimatedRecoveryTime { get; set; }
    public List<string> CriticalServices { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}

public class MonitoringIntegrationConfiguration
{
    public string ConfigurationId { get; set; } = string.Empty;
    public string ConfigurationName { get; set; } = string.Empty;
    public Dictionary<string, object> MonitoringSettings { get; set; } = new();
    public List<string> IntegratedSystems { get; set; } = new();
    public bool IsEnabled { get; set; } = true;
    public DateTime ConfiguredAt { get; set; } = DateTime.UtcNow;
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromMinutes(5);
}

public class MonitoringIntegrationResult
{
    public string ResultId { get; set; } = string.Empty;
    public bool IntegrationSuccessful { get; set; }
    public Dictionary<string, object> IntegrationMetrics { get; set; } = new();
    public List<string> SuccessfulIntegrations { get; set; } = new();
    public List<string> FailedIntegrations { get; set; } = new();
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    public string? ErrorMessage { get; set; }
}

public class AutoScalingManagementResult
{
    public string ResultId { get; set; } = string.Empty;
    public bool ScalingSuccessful { get; set; }
    public Dictionary<string, object> ScalingMetrics { get; set; } = new();
    public List<string> ScaledServices { get; set; } = new();
    public string ScalingDirection { get; set; } = string.Empty; // "scale-up", "scale-down", "maintain"
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    public int ResourcesAdjusted { get; set; }
}

public class PredictiveScalingStrategy
{
    public string StrategyId { get; set; } = string.Empty;
    public string StrategyName { get; set; } = string.Empty;
    public Dictionary<string, object> PredictionParameters { get; set; } = new();
    public List<string> MonitoredMetrics { get; set; } = new();
    public TimeSpan PredictionHorizon { get; set; } = TimeSpan.FromHours(2);
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class PredictiveScalingResult
{
    public string ResultId { get; set; } = string.Empty;
    public Dictionary<string, object> PredictionResults { get; set; } = new();
    public List<string> RecommendedActions { get; set; } = new();
    public double ConfidenceScore { get; set; }
    public DateTime PredictedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan ValidFor { get; set; } = TimeSpan.FromHours(1);
}

public class AlertEscalationConfiguration
{
    public string ConfigurationId { get; set; } = string.Empty;
    public Dictionary<string, List<string>> EscalationRules { get; set; } = new();
    public TimeSpan EscalationDelay { get; set; } = TimeSpan.FromMinutes(15);
    public List<string> NotificationChannels { get; set; } = new();
    public bool IsEnabled { get; set; } = true;
    public DateTime ConfiguredAt { get; set; } = DateTime.UtcNow;
}

public class CapacityPlanningConfiguration
{
    public string ConfigurationId { get; set; } = string.Empty;
    public Dictionary<string, object> PlanningParameters { get; set; } = new();
    public TimeSpan PlanningHorizon { get; set; } = TimeSpan.FromDays(30);
    public List<string> MonitoredResources { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTime ConfiguredAt { get; set; } = DateTime.UtcNow;
}

public class CapacityPlanningResult
{
    public string ResultId { get; set; } = string.Empty;
    public Dictionary<string, object> CapacityProjections { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public Dictionary<string, double> ResourceUtilizationForecasts { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan ForecastPeriod { get; set; } = TimeSpan.FromDays(30);
}

public class ResourceUtilizationMonitoringConfiguration
{
    public string ConfigurationId { get; set; } = string.Empty;
    public Dictionary<string, object> MonitoringSettings { get; set; } = new();
    public List<string> MonitoredResourceTypes { get; set; } = new();
    public TimeSpan MonitoringInterval { get; set; } = TimeSpan.FromMinutes(5);
    public bool IsEnabled { get; set; } = true;
    public DateTime ConfiguredAt { get; set; } = DateTime.UtcNow;
}

public class ResourceUtilizationMonitoringResult
{
    public string ResultId { get; set; } = string.Empty;
    public Dictionary<string, double> ResourceUtilization { get; set; } = new();
    public List<string> UtilizationAlerts { get; set; } = new();
    public Dictionary<string, object> PerformanceMetrics { get; set; } = new();
    public DateTime MonitoredAt { get; set; } = DateTime.UtcNow;
    public bool HasAnomalies { get; set; }
}

public class PerformanceDegradationScenario
{
    public string ScenarioId { get; set; } = string.Empty;
    public string ScenarioName { get; set; } = string.Empty;
    public Dictionary<string, object> DegradationParameters { get; set; } = new();
    public decimal EstimatedRevenueImpact { get; set; }
    public TimeSpan MaxAcceptableDuration { get; set; } = TimeSpan.FromMinutes(30);
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class RevenueCalculationModel
{
    public string ModelId { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public Dictionary<string, double> RevenueFactors { get; set; } = new();
    public decimal BaseRevenueRate { get; set; }
    public DateTime ModelVersion { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}

public class RevenueRiskCalculation
{
    public string CalculationId { get; set; } = string.Empty;
    public decimal EstimatedRisk { get; set; }
    public Dictionary<string, decimal> RiskFactors { get; set; } = new();
    public double ConfidenceLevel { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan RiskPeriod { get; set; } = TimeSpan.FromHours(24);
}

public class CulturalEventLoadPattern
{
    public string PatternId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public Dictionary<int, double> HourlyLoadMultipliers { get; set; } = new(); // 0-23 hours
    public List<int> PeakHours { get; set; } = new();
    public double BaseLoadMultiplier { get; set; } = 1.0;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class SecurityPerformanceMonitoring
{
    public string MonitoringId { get; set; } = string.Empty;
    public Dictionary<string, object> SecurityMetrics { get; set; } = new();
    public List<string> SecurityAlerts { get; set; } = new();
    public DateTime MonitoredAt { get; set; } = DateTime.UtcNow;
    public bool IsHealthy { get; set; } = true;
}

// Additional Emergency Types for Compilation Recovery
public class ResourceUtilizationResult
{
    public string ResultId { get; set; } = string.Empty;
    public Dictionary<string, double> ResourceUtilization { get; set; } = new();
    public List<string> ResourceAlerts { get; set; } = new();
    public DateTime MeasuredAt { get; set; } = DateTime.UtcNow;
    public bool IsOptimal { get; set; } = true;
}

public class AutomatedRecoveryTriggerConfiguration
{
    public string ConfigurationId { get; set; } = string.Empty;
    public Dictionary<string, object> TriggerRules { get; set; } = new();
    public List<string> MonitoredEvents { get; set; } = new();
    public bool IsEnabled { get; set; } = true;
    public DateTime ConfiguredAt { get; set; } = DateTime.UtcNow;
}

public class AutomatedRecoveryTriggerResult
{
    public string ResultId { get; set; } = string.Empty;
    public bool TriggerActivated { get; set; }
    public List<string> TriggeredActions { get; set; } = new();
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;
    public string? TriggerReason { get; set; }
}

public class PerformanceMonitoringResult
{
    public string ResultId { get; set; } = string.Empty;
    public Dictionary<string, double> PerformanceMetrics { get; set; } = new();
    public List<string> PerformanceAlerts { get; set; } = new();
    public DateTime MonitoredAt { get; set; } = DateTime.UtcNow;
    public bool IsWithinThresholds { get; set; } = true;
}

public class LoadBalancingCoordinationResult
{
    public string ResultId { get; set; } = string.Empty;
    public bool CoordinationSuccessful { get; set; }
    public Dictionary<string, object> LoadDistribution { get; set; } = new();
    public List<string> ActiveNodes { get; set; } = new();
    public DateTime CoordinatedAt { get; set; } = DateTime.UtcNow;
}