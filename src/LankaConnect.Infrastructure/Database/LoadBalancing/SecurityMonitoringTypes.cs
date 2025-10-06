using System;
using System.Collections.Generic;

namespace LankaConnect.Infrastructure.Database.LoadBalancing;

#region Security Types

public class AppSecurity
{
    public string SecurityLevel { get; set; } = string.Empty;
    public SecurityAwareRouting RoutingConfig { get; set; } = new();
    public bool IsSecure { get; set; }
}

public class SecurityAwareRouting
{
    public bool EnableSecureRouting { get; set; }
    public List<string> SecureRoutes { get; set; } = new();
}

public class CulturalSecurityContext
{
    public string CultureCode { get; set; } = string.Empty;
    public List<string> SecurityPolicies { get; set; } = new();
    public DateTime ContextCreatedAt { get; set; }
}

public class HeritageDataSecurityResult
{
    public bool IsOptimized { get; set; }
    public double PreservationLevel { get; set; }
    public double SecurityScore { get; set; }
    public DateTime OptimizedAt { get; set; }

    public HeritageDataSecurityResult(bool isOptimized, double preservationLevel, double securityScore, DateTime optimizedAt)
    {
        IsOptimized = isOptimized;
        PreservationLevel = preservationLevel;
        SecurityScore = securityScore;
        OptimizedAt = optimizedAt;
    }
}

public class ModelSecurityCriteria
{
    public double MinimumSecurityScore { get; set; }
    public bool RequireModelIntegrity { get; set; }
    public List<string> RequiredValidations { get; set; } = new();
}

public class ModelSecurityResult
{
    public bool IsValid { get; set; }
    public double SecurityScore { get; set; }
    public bool ModelIntegrity { get; set; }
    public DateTime ValidatedAt { get; set; }

    public ModelSecurityResult(bool isValid, double securityScore, bool modelIntegrity, DateTime validatedAt)
    {
        IsValid = isValid;
        SecurityScore = securityScore;
        ModelIntegrity = modelIntegrity;
        ValidatedAt = validatedAt;
    }
}

public class PreservationSecurityConfig
{
    public int PreservationLevel { get; set; }
    public bool EnableEncryption { get; set; }
    public bool EnableAuditTrail { get; set; }
    public TimeSpan RetentionPeriod { get; set; }
}

public class RegionalSecurityImplementation
{
    public string RegionId { get; set; } = string.Empty;
    public List<string> SecurityPolicies { get; set; } = new();
    public DateTime ImplementedAt { get; set; }
}

public class RegionalSecurityMaintenance
{
    public string RegionId { get; set; } = string.Empty;
    public DateTime LastMaintenanceDate { get; set; }
    public DateTime NextMaintenanceDate { get; set; }
    public List<string> MaintenanceTasks { get; set; } = new();
}

public class SecurityIntegrationPolicy
{
    public bool EnableAutomatedIntegration { get; set; }
    public List<string> IntegrationPoints { get; set; } = new();
    public TimeSpan SyncInterval { get; set; }
}

public class SecurityMonitoringIntegration
{
    public bool IsEnabled { get; set; }
    public List<string> MonitoredSystems { get; set; } = new();
    public TimeSpan ReportingInterval { get; set; }
}

public class SecurityOptimizationStrategy
{
    public string StrategyName { get; set; } = string.Empty;
    public List<string> OptimizationTargets { get; set; } = new();
    public int Priority { get; set; }
}

public class SecurityPerformanceMonitoring
{
    public Dictionary<string, double> PerformanceMetrics { get; set; } = new();
    public DateTime MonitoringStartTime { get; set; }
    public TimeSpan MonitoringDuration { get; set; }
}

public class SecuritySynchronizationResult
{
    public bool IsSuccessful { get; set; }
    public int SynchronizedItems { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime SynchronizedAt { get; set; }
}

public class SecurityIntegrityChecks
{
    public bool VerifyEncryption { get; set; }
    public bool VerifyAccessControl { get; set; }
    public bool VerifyAuditLog { get; set; }
}

#endregion

#region Monitoring Types

public class MonitoringConfiguration
{
    public bool EnableRealTimeMonitoring { get; set; }
    public TimeSpan SamplingInterval { get; set; }
    public List<string> MetricNames { get; set; } = new();
    public Dictionary<string, object> ThresholdValues { get; set; } = new();
}

public class MonitoringMetrics
{
    public Dictionary<string, double> Metrics { get; set; } = new();
    public DateTime CollectedAt { get; set; }
    public TimeSpan CollectionDuration { get; set; }
}

public class RegionalMonitoringConfiguration
{
    public string RegionId { get; set; } = string.Empty;
    public MonitoringConfiguration Configuration { get; set; } = new();
    public List<string> MonitoredServices { get; set; } = new();
}

public class CrossRegionAlertingSystem
{
    public List<string> Regions { get; set; } = new();
    public Dictionary<string, AlertConfiguration> AlertConfigurations { get; set; } = new();
    public bool EnableCrossRegionAlerts { get; set; }
}

#endregion

#region Alert Types

public class AlertAcknowledgment
{
    public string AlertId { get; set; } = string.Empty;
    public string AcknowledgedBy { get; set; } = string.Empty;
    public DateTime AcknowledgedAt { get; set; }
    public string Comments { get; set; } = string.Empty;
}

public class AlertConfiguration
{
    public string AlertName { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public List<string> NotificationChannels { get; set; } = new();
    public Dictionary<string, object> Thresholds { get; set; } = new();
}

public class AlertCorrelationResult
{
    public List<string> CorrelatedAlertIds { get; set; } = new();
    public string RootCauseAlertId { get; set; } = string.Empty;
    public int DuplicatesRemoved { get; set; }
    public DateTime ProcessedAt { get; set; }
}

public class AlertEffectivenessMetrics
{
    public double TruePositiveRate { get; set; }
    public double FalsePositiveRate { get; set; }
    public double MeanTimeToAcknowledge { get; set; }
    public double MeanTimeToResolve { get; set; }
    public int TotalAlerts { get; set; }
}

public class AlertEffectivenessThreshold
{
    public double MinimumTruePositiveRate { get; set; }
    public double MaximumFalsePositiveRate { get; set; }
    public TimeSpan MaxTimeToAcknowledge { get; set; }
}

public class AlertResolutionResult
{
    public string AlertId { get; set; } = string.Empty;
    public bool IsResolved { get; set; }
    public string ResolutionDetails { get; set; } = string.Empty;
    public DateTime ResolvedAt { get; set; }
}

public class AlertSuppressionResult
{
    public int SuppressedAlertCount { get; set; }
    public TimeSpan SuppressionDuration { get; set; }
    public List<string> SuppressedAlertIds { get; set; } = new();
}

#endregion

#region Performance Types

public class AutoScalingPerformanceImpact
{
    public double PerformanceImprovement { get; set; }
    public int ResourcesAdded { get; set; }
    public TimeSpan ScalingDuration { get; set; }
    public double CostImpact { get; set; }
}

public class CompetitivePerformanceAnalysis
{
    public Dictionary<string, double> CompetitorMetrics { get; set; } = new();
    public double RelativePerformance { get; set; }
    public List<string> PerformanceGaps { get; set; } = new();
}

public class GlobalPerformanceMetrics
{
    public Dictionary<string, double> RegionalMetrics { get; set; } = new();
    public double GlobalAverageLatency { get; set; }
    public double GlobalThroughput { get; set; }
    public DateTime CollectedAt { get; set; }
}

public class PerformanceDegradationScenario
{
    public string ScenarioName { get; set; } = string.Empty;
    public double DegradationPercentage { get; set; }
    public TimeSpan Duration { get; set; }
    public List<string> AffectedServices { get; set; } = new();
}

public class PerformanceRequirement
{
    public string RequirementName { get; set; } = string.Empty;
    public double TargetValue { get; set; }
    public string MetricType { get; set; } = string.Empty;
    public int Priority { get; set; }
}

public class PerformanceThreshold
{
    public string MetricName { get; set; } = string.Empty;
    public double WarningThreshold { get; set; }
    public double CriticalThreshold { get; set; }
    public string Unit { get; set; } = string.Empty;
}

public class TimezoneAwarePerformanceReport
{
    public Dictionary<string, Dictionary<string, double>> TimezoneMetrics { get; set; } = new();
    public DateTime ReportStartTime { get; set; }
    public DateTime ReportEndTime { get; set; }
}

#endregion

#region SLA Types

public class CustomerSLAReport
{
    public string CustomerId { get; set; } = string.Empty;
    public double SLACompliancePercentage { get; set; }
    public List<SLABreach> Breaches { get; set; } = new();
    public Dictionary<string, double> MetricValues { get; set; } = new();
    public DateTime ReportGeneratedAt { get; set; }
}

public class SLABreach
{
    public string BreachId { get; set; } = string.Empty;
    public string SLAMetric { get; set; } = string.Empty;
    public double ThresholdValue { get; set; }
    public double ActualValue { get; set; }
    public SLABreachSeverity Severity { get; set; }
    public DateTime BreachTime { get; set; }
    public TimeSpan Duration { get; set; }
}

public class SLABreachAnalysis
{
    public int TotalBreaches { get; set; }
    public Dictionary<SLABreachSeverity, int> BreachesBySeverity { get; set; } = new();
    public List<string> RootCauses { get; set; } = new();
    public List<string> RecommendedActions { get; set; } = new();
}

public enum SLABreachSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public class SLACreditCalculation
{
    public decimal TotalCredits { get; set; }
    public Dictionary<string, decimal> BreachCredits { get; set; } = new();
    public DateTime CalculatedAt { get; set; }
}

public class SLAImprovementInitiative
{
    public string InitiativeId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string TargetMetric { get; set; } = string.Empty;
    public double TargetImprovement { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime TargetCompletionDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class SLAImprovementTracker
{
    public List<SLAImprovementInitiative> Initiatives { get; set; } = new();
    public Dictionary<string, double> ImprovementProgress { get; set; } = new();
    public DateTime LastUpdated { get; set; }
}

public class SLAPerformanceReport
{
    public Dictionary<string, double> SLAMetrics { get; set; } = new();
    public double OverallCompliancePercentage { get; set; }
    public List<SLABreach> RecentBreaches { get; set; } = new();
    public DateTime ReportPeriodStart { get; set; }
    public DateTime ReportPeriodEnd { get; set; }
}

public enum SLAReportFormat
{
    JSON,
    PDF,
    HTML,
    CSV,
    Excel
}

public class SLAReportingConfiguration
{
    public SLAReportFormat Format { get; set; }
    public List<string> IncludedMetrics { get; set; } = new();
    public bool IncludeBreachDetails { get; set; }
    public bool IncludeImprovementSuggestions { get; set; }
    public TimeSpan ReportingFrequency { get; set; }
}

public class SLARiskAssessment
{
    public Dictionary<string, double> RiskScores { get; set; } = new();
    public List<string> HighRiskAreas { get; set; } = new();
    public List<string> MitigationRecommendations { get; set; } = new();
    public DateTime AssessedAt { get; set; }
}

public class SLAThresholdAdjustment
{
    public string SLAMetric { get; set; } = string.Empty;
    public double OldThreshold { get; set; }
    public double NewThreshold { get; set; }
    public string Justification { get; set; } = string.Empty;
    public DateTime EffectiveDate { get; set; }
}

#endregion

#region Access & Compliance Types

public class APIAccessRequest
{
    public string RequestId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string APIEndpoint { get; set; } = string.Empty;
    public List<string> RequestedPermissions { get; set; } = new();
    public DateTime RequestedAt { get; set; }
}

public class CulturalResourceAccess
{
    public string ResourceId { get; set; } = string.Empty;
    public string CultureCode { get; set; } = string.Empty;
    public List<string> AccessPermissions { get; set; } = new();
    public bool RequiresCulturalSensitivity { get; set; }
}

public class CrossBorderComplianceRequirements
{
    public List<string> SourceCountries { get; set; } = new();
    public List<string> DestinationCountries { get; set; } = new();
    public Dictionary<string, List<string>> ComplianceRules { get; set; } = new();
    public bool RequiresDataLocalization { get; set; }
}

public class JITAccessRequest
{
    public string RequestId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string ResourceId { get; set; } = string.Empty;
    public TimeSpan RequestedDuration { get; set; }
    public string Justification { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
}

public class MultiJurisdictionCompliance
{
    public List<string> Jurisdictions { get; set; } = new();
    public Dictionary<string, bool> ComplianceStatus { get; set; } = new();
    public List<string> ConflictingRequirements { get; set; } = new();
    public DateTime LastAuditedAt { get; set; }
}

public class ComplianceValidationDuringScaling
{
    public bool IsCompliant { get; set; }
    public List<string> ValidationResults { get; set; } = new();
    public List<string> ComplianceIssues { get; set; } = new();
}

#endregion
