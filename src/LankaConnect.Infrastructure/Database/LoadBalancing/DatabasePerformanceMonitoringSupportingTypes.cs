using System;
using System.Collections.Generic;
using LankaConnect.Domain.Common.Monitoring;
using System.ComponentModel.DataAnnotations;
using LankaConnect.Domain.Common.ValueObjects;
using LankaConnect.Domain.Shared;
using LankaConnect.Domain.Common.Enums;
using DateRange = LankaConnect.Domain.Common.ValueObjects.DateRange;

namespace LankaConnect.Infrastructure.Database.LoadBalancing
{
    #region Configuration Models

    /// <summary>
    /// Configuration for the database performance monitoring engine
    /// </summary>
    public class PerformanceMonitoringConfiguration
    {
        public int MonitoringIntervalSeconds { get; set; } = 30;
        public List<string> MonitoredDatabaseInstances { get; set; } = new();
        public List<ServiceLevelAgreement> ServiceLevelAgreements { get; set; } = new();
        public object AnalyticsConfiguration { get; set; }
        public object RegionConfiguration { get; set; }
        public object CulturalConfiguration { get; set; }
        public object RevenueConfiguration { get; set; }
        public TimeSpan AlertProcessingInterval { get; set; } = TimeSpan.FromSeconds(10);
        public TimeSpan SLAValidationInterval { get; set; } = TimeSpan.FromMinutes(5);
        public int MaxConcurrentAlerts { get; set; } = 100;
        public bool EnableCulturalIntelligence { get; set; } = true;
        public bool EnableRevenueProtection { get; set; } = true;
    }

    /// <summary>
    /// Performance threshold configuration for cultural events
    /// </summary>
    public class PerformanceThresholdConfig
    {
        public double ResponseTimeThresholdMs { get; set; }
        public double ThroughputThreshold { get; set; }
        public double ErrorRateThreshold { get; set; }
        public double ResourceUtilizationThreshold { get; set; }
        public CulturalEventType EventType { get; set; }
        public Dictionary<string, double> CustomThresholds { get; set; } = new();
    }

    /// <summary>
    /// Alerting rule configuration with cultural context
    /// </summary>
    public class AlertingRuleConfiguration
    {
        public string RuleId { get; set; } = Guid.NewGuid().ToString();
        public string RuleName { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public AlertSeverity Severity { get; set; }
        public bool IsEnabled { get; set; } = true;
        public TimeSpan EvaluationInterval { get; set; } = TimeSpan.FromMinutes(1);
        public List<NotificationChannel> NotificationChannels { get; set; } = new();
        public CulturalEventContext CulturalContext { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new();
    }


    /// <summary>
    /// Revenue metrics configuration
    /// </summary>
    public class RevenueMetricsConfiguration
    {
        public string ConfigurationName { get; set; } = string.Empty;
        public List<string> RevenueStreams { get; set; } = new();
        public Dictionary<string, double> RevenueThresholds { get; set; } = new();
        public TimeSpan MonitoringWindow { get; set; } = TimeSpan.FromMinutes(15);
        public bool EnableRealTimeMonitoring { get; set; } = true;
        public List<string> CriticalBusinessProcesses { get; set; } = new();
    }

    #endregion

    #region Performance Monitoring Result Models

    /// <summary>
    /// Database performance snapshot with real-time metrics
    /// </summary>
    public class DatabasePerformanceSnapshot
    {
        public string SnapshotId { get; set; } = Guid.NewGuid().ToString();
        public string DatabaseInstance { get; set; } = string.Empty;
        public DateTimeOffset SnapshotTime { get; set; }
        public MetricsGranularity Granularity { get; set; }
        public Dictionary<string, PerformanceMetric> PerformanceMetrics { get; set; } = new();
        public int ActiveConnections { get; set; }
        public Dictionary<string, double> ResourceUtilization { get; set; } = new();
        public List<ActiveAlert> CurrentAlerts { get; set; } = new();
        public PerformanceHealthStatus HealthStatus { get; set; }
    }

    /// <summary>
    /// Connection pool metrics for health monitoring
    /// </summary>
    public class ConnectionPoolMetrics
    {
        public string PoolIdentifier { get; set; } = string.Empty;
        public DateTimeOffset MeasurementTime { get; set; }
        public int TotalConnections { get; set; }
        public int ActiveConnections { get; set; }
        public int IdleConnections { get; set; }
        public double PoolUtilization { get; set; }
        public TimeSpan AverageConnectionAge { get; set; }
        public int ConnectionLeaks { get; set; }
        public double PoolPerformanceScore { get; set; }
        public List<ConnectionPoolIssue> DetectedIssues { get; set; } = new();
    }

    /// <summary>
    /// Query performance analysis results
    /// </summary>
    public class QueryPerformanceAnalysis
    {
        public string AnalysisId { get; set; } = Guid.NewGuid().ToString();
        public TimeSpan AnalysisWindow { get; set; }
        public QueryComplexityThreshold Threshold { get; set; }
        public DateTimeOffset AnalysisTime { get; set; }
        public List<SlowQueryAnalysis> SlowQueries { get; set; } = new();
        public List<QueryOptimization> QueryOptimizations { get; set; } = new();
        public List<QueryPerformanceTrend> PerformanceTrends { get; set; } = new();
        public List<CulturalQueryPattern> CulturalQueryPatterns { get; set; } = new();
    }

    /// <summary>
    /// Storage utilization metrics across databases
    /// </summary>
    public class StorageUtilizationMetrics
    {
        public string MetricsId { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset MeasurementTime { get; set; }
        public Dictionary<string, DatabaseStorageMetric> DatabaseStorageMetrics { get; set; } = new();
        public double TotalStorageUtilization { get; set; }
        public double StorageGrowthRate { get; set; }
        public List<StorageOptimization> StorageOptimizationOpportunities { get; set; } = new();
        public List<CulturalContentStorageMetric> CulturalContentStorageMetrics { get; set; } = new();
    }

    /// <summary>
    /// Transaction performance metrics
    /// </summary>
    public class TransactionPerformanceMetrics
    {
        public string MetricsId { get; set; } = Guid.NewGuid().ToString();
        public TransactionIsolationLevel IsolationLevel { get; set; }
        public DateTimeOffset MeasurementTime { get; set; }
        public int TransactionThroughput { get; set; }
        public TimeSpan AverageTransactionDuration { get; set; }
        public int DeadlockCount { get; set; }
        public int BlockingTransactions { get; set; }
        public double TransactionRollbackRate { get; set; }
        public List<CulturalTransactionPattern> CulturalTransactionPatterns { get; set; } = new();
        public List<TransactionBottleneck> PerformanceBottlenecks { get; set; } = new();
    }

    /// <summary>
    /// Index performance analysis results
    /// </summary>
    public class IndexPerformanceAnalysis
    {
        public string AnalysisId { get; set; } = Guid.NewGuid().ToString();
        public string TableName { get; set; } = string.Empty;
        public DateTimeOffset AnalysisTime { get; set; }
        public List<IndexMetric> IndexMetrics { get; set; } = new();
        public List<UnusedIndex> UnusedIndexes { get; set; } = new();
        public List<MissingIndex> MissingIndexes { get; set; } = new();
        public List<IndexOptimization> IndexOptimizationRecommendations { get; set; } = new();
        public List<CulturalIndexPattern> CulturalIndexPatterns { get; set; } = new();
    }

    /// <summary>
    /// Backup and recovery performance metrics
    /// </summary>
    public class BackupRecoveryMetrics
    {
        public string MetricsId { get; set; } = Guid.NewGuid().ToString();
        public BackupStrategy BackupStrategy { get; set; }
        public DateTimeOffset MeasurementTime { get; set; }
        public List<BackupPerformanceMetric> BackupPerformanceMetrics { get; set; } = new();
        public List<RecoveryPerformanceMetric> RecoveryPerformanceMetrics { get; set; } = new();
        public BackupComplianceStatus BackupComplianceStatus { get; set; }
        public List<CulturalBackupPriority> CulturalBackupPriorities { get; set; } = new();
    }

    #endregion

    #region Cultural Intelligence Models

    /// <summary>
    /// Diaspora activity metrics for community monitoring
    /// </summary>
    public class DiasporaActivityMetrics
    {
        public string MetricsId { get; set; } = Guid.NewGuid().ToString();
        public TimeSpan MonitoringWindow { get; set; }
        public List<string> CommunityRegions { get; set; } = new();
        public Dictionary<string, RegionalActivityMetric> RegionalActivities { get; set; } = new();
        public List<CrossCulturalInteraction> CrossCulturalInteractions { get; set; } = new();
        public List<EngagementTrend> CommunityEngagementTrends { get; set; } = new();
    }

    /// <summary>
    /// Cultural content engagement performance metrics
    /// </summary>
    public class ContentEngagementPerformanceMetrics
    {
        public string ContentId { get; set; } = string.Empty;
        public CulturalContentType ContentType { get; set; }
        public DateTimeOffset MeasurementTime { get; set; }
        public Dictionary<string, double> EngagementMetrics { get; set; } = new();
        public Dictionary<string, PerformanceMetric> PerformanceImpact { get; set; } = new();
        public List<CulturalReachMetric> CulturalReach { get; set; } = new();
    }

    /// <summary>
    /// Multilingual search performance metrics
    /// </summary>
    public class MultilingualSearchMetrics
    {
        public string MetricsId { get; set; } = Guid.NewGuid().ToString();
        public List<string> SupportedLanguages { get; set; } = new();
        public SearchComplexityLevel ComplexityLevel { get; set; }
        public DateTimeOffset MeasurementTime { get; set; }
        public Dictionary<string, LanguagePerformanceMetric> LanguagePerformanceMetrics { get; set; } = new();
        public List<CrossLanguageInteraction> CrossLanguageInteractions { get; set; } = new();
        public List<SearchOptimizationRecommendation> SearchOptimizationRecommendations { get; set; } = new();
    }

    /// <summary>
    /// Cultural event correlation analysis
    /// </summary>
    public class CulturalCorrelationAnalysis
    {
        public string AnalysisId { get; set; } = Guid.NewGuid().ToString();
        public DateRange AnalysisPeriod { get; set; }
        public List<EventCorrelation> EventCorrelations { get; set; } = new();
        public List<PerformanceCorrelation> PerformanceCorrelations { get; set; } = new();
        public List<SeasonalPattern> SeasonalPatterns { get; set; } = new();
        public List<PredictiveInsight> PredictiveInsights { get; set; } = new();
    }

    #endregion

    #region Alerting and Escalation Models

    /// <summary>
    /// Performance alert for monitoring system
    /// </summary>
    public class PerformanceAlert
    {
        public string AlertId { get; set; } = Guid.NewGuid().ToString();
        public AlertSeverity Severity { get; set; }
        public string AlertType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTimeOffset TriggeredAt { get; set; }
        public string DatabaseInstance { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public Dictionary<string, object> AlertData { get; set; } = new();
        public CulturalEventContext CulturalContext { get; set; }
        public AlertStatus Status { get; set; } = AlertStatus.Active;
    }

    /// <summary>
    /// Alert processing context
    /// </summary>
    public class AlertProcessingContext
    {
        public string ContextId { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset ProcessingStartTime { get; set; }
        public string ProcessingAgent { get; set; } = string.Empty;
        public Dictionary<string, object> ContextData { get; set; } = new();
        public CulturalEventContext CulturalContext { get; set; }
        public List<string> RequiredActions { get; set; } = new();
        public AlertPriority Priority { get; set; }
    }

    /// <summary>
    /// Alert processing result
    /// </summary>
    public class AlertProcessingResult
    {
        public string AlertId { get; set; } = string.Empty;
        public DateTimeOffset ProcessingTime { get; set; }
        public AlertProcessingStatus ProcessingStatus { get; set; }
        public List<NotificationResult> NotificationResults { get; set; } = new();
        public List<EscalationAction> EscalationActions { get; set; } = new();
        public TimeSpan ProcessingDuration { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Alert escalation request
    /// </summary>
    public class AlertEscalationRequest
    {
        public string AlertId { get; set; } = string.Empty;
        public AlertEscalationReason Reason { get; set; }
        public DateTimeOffset RequestedAt { get; set; }
        public string RequestedBy { get; set; } = string.Empty;
        public Dictionary<string, object> EscalationData { get; set; } = new();
        public CulturalEventContext CulturalContext { get; set; }
    }

    /// <summary>
    /// Escalation result
    /// </summary>
    public class EscalationResult
    {
        public string EscalationId { get; set; } = string.Empty;
        public string AlertId { get; set; } = string.Empty;
        public DateTimeOffset EscalationTime { get; set; }
        public List<EscalationAction> ExecutedActions { get; set; } = new();
        public List<NotificationResult> NotificationsSent { get; set; } = new();
        public EscalationStatus EscalationStatus { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    /// <summary>
    /// Escalation policy configuration
    /// </summary>
    public class EscalationPolicy
    {
        public string PolicyId { get; set; } = Guid.NewGuid().ToString();
        public string PolicyName { get; set; } = string.Empty;
        public List<EscalationLevel> EscalationLevels { get; set; } = new();
        public TimeSpan DefaultEscalationDelay { get; set; } = TimeSpan.FromMinutes(15);
        public bool IsEnabled { get; set; } = true;
        public CulturalEventContext CulturalContext { get; set; }
        public Dictionary<AlertSeverity, TimeSpan> SeverityBasedDelays { get; set; } = new();
    }

    #endregion

    #region SLA and Compliance Models

    /// <summary>
    /// Service level agreement definition
    /// </summary>
    public class ServiceLevelAgreement
    {
        public string SLAId { get; set; } = Guid.NewGuid().ToString();
        public string ServiceName { get; set; } = string.Empty;
        public string SLAName { get; set; } = string.Empty;
        public Dictionary<string, SLAMetric> Metrics { get; set; } = new();
        public TimeSpan MeasurementPeriod { get; set; }
        public bool IsActive { get; set; } = true;
        public CulturalPerformanceThreshold CulturalPriority { get; set; }
        public List<string> ApplicableRegions { get; set; } = new();
    }

    /// <summary>
    /// SLA compliance status with details
    /// </summary>
    public class SLAComplianceStatus
    {
        public string SLAId { get; set; } = string.Empty;
        public ComplianceLevel ComplianceLevel { get; set; }
        public double CompliancePercentage { get; set; }
        public List<ComplianceViolation> Violations { get; set; } = new();
        public bool HasViolations => Violations.Count > 0;
        public DateTimeOffset LastValidated { get; set; }
        public Dictionary<string, double> MetricCompliance { get; set; } = new();
    }

    /// <summary>
    /// Overall compliance score calculation
    /// </summary>
    public class OverallComplianceScore
    {
        public double OverallPercentage { get; set; }
        public Dictionary<string, double> ServiceCompliance { get; set; } = new();
        public ComplianceLevel OverallLevel { get; set; }
        public List<string> CriticalViolations { get; set; } = new();
        public DateTimeOffset CalculatedAt { get; set; }
    }

    /// <summary>
    /// Compliance trend analysis
    /// </summary>
    public class ComplianceTrend
    {
        public TrendDirection Direction { get; set; }
        public double TrendPercentage { get; set; }
        public List<ComplianceDataPoint> TrendData { get; set; } = new();
        public TimeSpan AnalysisPeriod { get; set; }
        public string TrendDescription { get; set; } = string.Empty;
    }

    // ComplianceViolation class removed - use canonical domain type:
    // LankaConnect.Domain.Common.Monitoring.ComplianceViolation

    #endregion

    #region Revenue Protection Models

    /// <summary>
    /// Revenue impact metrics for performance monitoring
    /// </summary>
    public class RevenueImpactMetrics
    {
        public string MetricsId { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset MeasurementTime { get; set; }
        public decimal EstimatedRevenueImpact { get; set; }
        public Dictionary<string, RevenueMetric> RevenueMetrics { get; set; } = new();
        public List<RevenueRiskFactor> RiskFactors { get; set; } = new();
        public RevenueProtectionStatus RevenueProtectionStatus { get; set; }
        public List<RevenueProtectionMeasure> ActiveProtectionMeasures { get; set; } = new();
    }

    /// <summary>
    /// Revenue metric definition
    /// </summary>
    public class RevenueMetric
    {
        public string MetricName { get; set; } = string.Empty;
        public decimal CurrentValue { get; set; }
        public decimal BaselineValue { get; set; }
        public string Currency { get; set; } = "USD";
        public TimeSpan MeasurementPeriod { get; set; }
        public RevenueMetricType MetricType { get; set; }
        public double ChangePercentage => BaselineValue != 0 ? (double)((CurrentValue - BaselineValue) / BaselineValue * 100) : 0;
    }

    /// <summary>
    /// Revenue risk factor
    /// </summary>
    public class RevenueRiskFactor
    {
        public string FactorName { get; set; } = string.Empty;
        public RiskLevel RiskLevel { get; set; }
        public double ImpactProbability { get; set; }
        public decimal PotentialImpact { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<string> MitigationActions { get; set; } = new();
    }

    #endregion

    #region Auto-Scaling Models

    /// <summary>
    /// Auto-scaling recommendation
    /// </summary>
    public class AutoScalingRecommendation
    {
        public string RecommendationId { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset GeneratedAt { get; set; }
        public ScalingRecommendationType RecommendationType { get; set; }
        public int RecommendedInstances { get; set; }
        public double ConfidenceScore { get; set; }
        public List<ScalingJustification> Justifications { get; set; } = new();
        public TimeSpan EstimatedImplementationTime { get; set; }
        public decimal EstimatedCostImpact { get; set; }
        public CulturalEventContext CulturalContext { get; set; }
    }

    /// <summary>
    /// Scaling policy configuration
    /// </summary>
    public class ScalingPolicy
    {
        public string PolicyId { get; set; } = Guid.NewGuid().ToString();
        public string PolicyName { get; set; } = string.Empty;
        public Dictionary<string, double> ScalingThresholds { get; set; } = new();
        public int MinInstances { get; set; } = 1;
        public int MaxInstances { get; set; } = 10;
        public TimeSpan ScalingCooldown { get; set; } = TimeSpan.FromMinutes(5);
        public bool EnableCulturalIntelligence { get; set; } = true;
        public List<string> ApplicableRegions { get; set; } = new();
    }

    #endregion

    #region Health and System Models

    /// <summary>
    /// Component health status
    /// </summary>
    public class ComponentHealth
    {
        public string ComponentName { get; set; } = string.Empty;
        public HealthStatus Status { get; set; }
        public double HealthScore { get; set; }
        public List<HealthIssue> Issues { get; set; } = new();
        public DateTimeOffset LastChecked { get; set; }
        public Dictionary<string, double> Metrics { get; set; } = new();
        public List<HealthRecommendation> Recommendations { get; set; } = new();
    }

    /// <summary>
    /// Overall health status enumeration
    /// </summary>
    public enum OverallHealthStatus
    {
        Healthy = 1,
        Warning = 2,
        Degraded = 3,
        Critical = 4,
        Unknown = 0
    }

    /// <summary>
    /// Health trend analysis
    /// </summary>
    public class HealthTrend
    {
        public TrendDirection Direction { get; set; }
        public double TrendScore { get; set; }
        public List<HealthDataPoint> TrendData { get; set; } = new();
        public TimeSpan AnalysisPeriod { get; set; }
        public string Analysis { get; set; } = string.Empty;
    }

    /// <summary>
    /// Health issue identification
    /// </summary>
    public class HealthIssue
    {
        public string IssueId { get; set; } = Guid.NewGuid().ToString();
        public string Component { get; set; } = string.Empty;
        public IssueSeverity Severity { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTimeOffset DetectedAt { get; set; }
        public List<string> PossibleCauses { get; set; } = new();
        public List<string> RecommendedActions { get; set; } = new();
    }

    /// <summary>
    /// Health recommendation
    /// </summary>
    public class HealthRecommendation
    {
        public string RecommendationId { get; set; } = Guid.NewGuid().ToString();
        public string Component { get; set; } = string.Empty;
        public RecommendationPriority Priority { get; set; }
        public string Description { get; set; } = string.Empty;
        public List<string> Actions { get; set; } = new();
        public TimeSpan EstimatedImplementationTime { get; set; }
        public decimal EstimatedCost { get; set; }
    }

    /// <summary>
    /// System health validation
    /// </summary>
    public class SystemHealthValidation
    {
        public string ValidationId { get; set; } = string.Empty;
        public DateTimeOffset ValidationTime { get; set; }
        public Dictionary<string, ComponentHealthCheck> ComponentHealthChecks { get; set; } = new();
        public SystemHealthStatus OverallHealthStatus { get; set; }
        public List<ValidationResult> ValidationResults { get; set; } = new();
    }

    /// <summary>
    /// Component health check result
    /// </summary>
    public class ComponentHealthCheck
    {
        public string ComponentName { get; set; } = string.Empty;
        public bool IsHealthy { get; set; }
        public string HealthMessage { get; set; } = string.Empty;
        public double HealthScore { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public Dictionary<string, object> CheckData { get; set; } = new();
    }

    // ValidationResult moved to Infrastructure.Common.Models.InfrastructureValidationResult
    // This prevents CS0101 duplicate type errors

    /// <summary>
    /// Health validation configuration
    /// </summary>
    public class HealthValidationConfiguration
    {
        public List<string> ComponentsToValidate { get; set; } = new();
        public TimeSpan ValidationTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public bool IncludePerformanceChecks { get; set; } = true;
        public bool IncludeCulturalIntelligenceChecks { get; set; } = true;
        public Dictionary<string, object> CustomSettings { get; set; } = new();
    }

    /// <summary>
    /// Shutdown configuration
    /// </summary>
    public class ShutdownConfiguration
    {
        public TimeSpan GracefulShutdownTimeout { get; set; } = TimeSpan.FromMinutes(2);
        public bool ForceShutdown { get; set; } = false;
        public bool SaveStateBeforeShutdown { get; set; } = true;
        public List<string> CriticalProcessesToComplete { get; set; } = new();
    }

    #endregion

    #region Enumerations


    /// <summary>
    /// Cultural performance thresholds
    /// </summary>
    public enum CulturalPerformanceThreshold
    {
        General = 5,
        Regional = 6,
        National = 7,
        Religious = 8,
        Sacred = 10
    }


    /// <summary>
    /// Metrics granularity levels
    /// </summary>
    public enum MetricsGranularity
    {
        Second = 1,
        Minute = 2,
        Hour = 3,
        Day = 4
    }

    /// <summary>
    /// Performance health status
    /// </summary>
    public enum PerformanceHealthStatus
    {
        Healthy = 1,
        Warning = 2,
        Degraded = 3,
        Critical = 4,
        Unknown = 0
    }

    /// <summary>
    /// Health assessment depth
    /// </summary>
    public enum HealthAssessmentDepth
    {
        Basic = 1,
        Standard = 2,
        Comprehensive = 3,
        Deep = 4
    }

    /// <summary>
    /// Query complexity threshold
    /// </summary>
    public enum QueryComplexityThreshold
    {
        Low = 1,
        Medium = 2,
        High = 3,
        VeryHigh = 4
    }

    /// <summary>
    /// Transaction isolation levels
    /// </summary>
    public enum TransactionIsolationLevel
    {
        ReadUncommitted = 1,
        ReadCommitted = 2,
        RepeatableRead = 3,
        Serializable = 4
    }

    /// <summary>
    /// Backup strategies
    /// </summary>
    public enum BackupStrategy
    {
        Full = 1,
        Incremental = 2,
        Differential = 3,
        Transaction = 4
    }

    /// <summary>
    /// Analytics granularity
    /// </summary>
    public enum AnalyticsGranularity
    {
        Minute = 1,
        Hour = 2,
        Day = 3,
        Week = 4,
        Month = 5
    }

    /// <summary>
    /// Insight analysis depth
    /// </summary>
    public enum InsightAnalysisDepth
    {
        Basic = 1,
        Standard = 2,
        Advanced = 3,
        Expert = 4
    }

    /// <summary>
    /// Cultural content types
    /// </summary>
    public enum CulturalContentType
    {
        Article = 1,
        Video = 2,
        Audio = 3,
        Image = 4,
        Event = 5,
        Recipe = 6,
        Story = 7,
        Prayer = 8,
        Ritual = 9,
        Celebration = 10
    }

    /// <summary>
    /// Search complexity levels
    /// </summary>
    public enum SearchComplexityLevel
    {
        Simple = 1,
        Moderate = 2,
        Complex = 3,
        Advanced = 4
    }

    /// <summary>
    /// Alert status
    /// </summary>
    public enum AlertStatus
    {
        Active = 1,
        Acknowledged = 2,
        Resolved = 3,
        Suppressed = 4
    }

    /// <summary>
    /// Alert priority levels
    /// </summary>
    public enum AlertPriority
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4,
        Sacred = 10
    }

    /// <summary>
    /// Alert processing status
    /// </summary>
    public enum AlertProcessingStatus
    {
        Pending = 1,
        InProgress = 2,
        Completed = 3,
        Failed = 4,
        Cancelled = 5
    }

    /// <summary>
    /// Alert escalation reasons
    /// </summary>
    public enum AlertEscalationReason
    {
        NoResponse = 1,
        SeverityIncrease = 2,
        CulturalEventImpact = 3,
        RevenueImpact = 4,
        ManualEscalation = 5
    }

    /// <summary>
    /// Escalation status
    /// </summary>
    public enum EscalationStatus
    {
        Pending = 1,
        InProgress = 2,
        Completed = 3,
        Failed = 4,
        Cancelled = 5
    }

    /// <summary>
    /// Escalation action status
    /// </summary>
    public enum EscalationActionStatus
    {
        Pending = 1,
        Executed = 2,
        Acknowledged = 3,
        Failed = 4
    }

    /// <summary>
    /// Compliance levels
    /// </summary>
    public enum ComplianceLevel
    {
        NonCompliant = 1,
        PartiallyCompliant = 2,
        Compliant = 3,
        ExceedsRequirements = 4
    }

    /// <summary>
    /// Compliance validation periods
    /// </summary>
    public enum ComplianceValidationPeriod
    {
        Current = 1,
        LastHour = 2,
        LastDay = 3,
        LastWeek = 4,
        LastMonth = 5,
        Custom = 99
    }

    /// <summary>
    /// Trend directions
    /// </summary>
    public enum TrendDirection
    {
        Declining = -1,
        Stable = 0,
        Improving = 1,
        Unknown = 99
    }

    /// <summary>
    /// Violation severity
    /// </summary>
    public enum ViolationSeverity
    {
        Minor = 1,
        Moderate = 2,
        Major = 3,
        Critical = 4,
        SacredEventImpact = 10
    }

    /// <summary>
    /// Revenue protection status
    /// </summary>
    public enum RevenueProtectionStatus
    {
        Protected = 1,
        Monitoring = 2,
        AtRisk = 3,
        Critical = 4,
        Compromised = 5
    }

    /// <summary>
    /// Revenue metric types
    /// </summary>
    public enum RevenueMetricType
    {
        Gross = 1,
        Net = 2,
        Recurring = 3,
        OneTime = 4,
        Projected = 5
    }

    /// <summary>
    /// Risk levels
    /// </summary>
    public enum RiskLevel
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4,
        Extreme = 5
    }

    /// <summary>
    /// Scaling recommendation types
    /// </summary>
    public enum ScalingRecommendationType
    {
        ScaleUp = 1,
        ScaleDown = 2,
        ScaleOut = 3,
        ScaleIn = 4,
        NoChange = 5
    }

    /// <summary>
    /// Health status enumeration
    /// </summary>
    public enum HealthStatus
    {
        Healthy = 1,
        Warning = 2,
        Degraded = 3,
        Critical = 4,
        Unknown = 0
    }


    /// <summary>
    /// Issue severity levels
    /// </summary>
    public enum IssueSeverity
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4
    }

    /// <summary>
    /// Recommendation priorities
    /// </summary>
    public enum RecommendationPriority
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Critical = 4,
        Immediate = 5
    }

    /// <summary>
    /// Validation severity levels
    /// </summary>
    public enum ValidationSeverity
    {
        Info = 1,
        Warning = 2,
        Error = 3,
        Critical = 4
    }

    /// <summary>
    /// Backup compliance status
    /// </summary>
    public enum BackupComplianceStatus
    {
        Compliant = 1,
        Warning = 2,
        NonCompliant = 3,
        Unknown = 0
    }

    /// <summary>
    /// Performance impact severity
    /// </summary>
    public enum PerformanceImpactSeverity
    {
        Minimal = 1,
        Low = 2,
        Moderate = 3,
        High = 4,
        Critical = 5,
        Severe = 6
    }

    /// <summary>
    /// Prediction confidence levels
    /// </summary>
    public enum PredictionConfidenceLevel
    {
        VeryLow = 1,
        Low = 2,
        Medium = 3,
        High = 4,
        VeryHigh = 5
    }

    /// <summary>
    /// Prediction timeframes
    /// </summary>
    public enum PredictionTimeframe
    {
        NextHour = 1,
        Next6Hours = 2,
        NextDay = 3,
        NextWeek = 4,
        NextMonth = 5
    }

    #endregion

    #region Additional Supporting Types

    /// <summary>
    /// Performance metric with metadata
    /// </summary>
    public class PerformanceMetric
    {
        public string MetricName { get; set; } = string.Empty;
        public double Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
        public MetricType Type { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new();
    }

    /// <summary>
    /// Metric types
    /// </summary>
    public enum MetricType
    {
        Counter = 1,
        Gauge = 2,
        Histogram = 3,
        Summary = 4
    }

    // DateRange moved to Infrastructure.Common.Models.InfrastructureDateRange
    // This prevents CS0101 duplicate type errors

    /// <summary>
    /// Active alert information
    /// </summary>
    public class ActiveAlert
    {
        public string AlertId { get; set; } = string.Empty;
        public AlertSeverity Severity { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTimeOffset TriggeredAt { get; set; }
        public TimeSpan Duration => DateTimeOffset.UtcNow - TriggeredAt;
    }

    /// <summary>
    /// Connection pool issue identification
    /// </summary>
    public class ConnectionPoolIssue
    {
        public string IssueType { get; set; } = string.Empty;
        public IssueSeverity Severity { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTimeOffset DetectedAt { get; set; }
        public List<string> RecommendedActions { get; set; } = new();
    }

    /// <summary>
    /// Notification result
    /// </summary>
    public class NotificationResult
    {
        public string NotificationId { get; set; } = Guid.NewGuid().ToString();
        public string Channel { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTimeOffset SentAt { get; set; }
        public TimeSpan DeliveryTime { get; set; }
    }

    /// <summary>
    /// Escalation action
    /// </summary>
    public class EscalationAction
    {
        public string ActionId { get; set; } = Guid.NewGuid().ToString();
        public string ActionType { get; set; } = string.Empty;
        public EscalationActionStatus Status { get; set; }
        public DateTimeOffset ExecutedAt { get; set; }
        public string ExecutedBy { get; set; } = string.Empty;
        public Dictionary<string, object> ActionData { get; set; } = new();
    }

    /// <summary>
    /// Escalation level configuration
    /// </summary>
    public class EscalationLevel
    {
        public int Level { get; set; }
        public string LevelName { get; set; } = string.Empty;
        public List<NotificationChannel> NotificationChannels { get; set; } = new();
        public TimeSpan EscalationDelay { get; set; }
        public List<string> ResponsiblePersons { get; set; } = new();
        public List<string> Actions { get; set; } = new();
        public bool RequireAcknowledgment { get; set; }
    }

    /// <summary>
    /// Notification channel configuration
    /// </summary>
    public class NotificationChannel
    {
        public string ChannelId { get; set; } = Guid.NewGuid().ToString();
        public string ChannelName { get; set; } = string.Empty;
        public string ChannelType { get; set; } = string.Empty;
        public string Endpoint { get; set; } = string.Empty;
        public Dictionary<string, string> Configuration { get; set; } = new();
        public bool IsEnabled { get; set; } = true;
        public List<AlertSeverity> SeverityFilters { get; set; } = new();
    }

    #endregion

    #region Placeholder Supporting Types

    // These would be fully implemented in a complete solution
    public class CulturalEvent { }
    public class UpcomingCulturalEvent { }
    public class LoadPredictionPoint { }
    public class CulturalEventFactor { }
    public class RecommendedPreparation { }
    public class RegionalImpact { }
    public class ImpactMitigationStrategy { }
    public class RecommendedAction { }
    public class CulturalLoadImpact { }
    public class PerformanceAnomaly { }
    public class RegionalActivityMetric { }
    public class CrossCulturalInteraction { }
    public class EngagementTrend { }
    public class CulturalReachMetric { }
    public class LanguagePerformanceMetric { }
    public class CrossLanguageInteraction { }
    public class SearchOptimizationRecommendation { }
    public class EventCorrelation { }
    public class PerformanceCorrelation { }
    public class SeasonalPattern { }
    public class PredictiveInsight { }
    public class SlowQueryAnalysis { }
    public class QueryOptimization { }
    public class QueryPerformanceTrend { }
    public class CulturalQueryPattern { }
    public class DatabaseStorageMetric { }
    public class StorageOptimization { }
    public class CulturalContentStorageMetric { }
    public class CulturalTransactionPattern { }
    public class TransactionBottleneck { }
    public class IndexMetric { }
    public class UnusedIndex { }
    public class MissingIndex { }
    public class IndexOptimization { }
    public class CulturalIndexPattern { }
    public class BackupPerformanceMetric { }
    public class RecoveryPerformanceMetric { }
    public class CulturalBackupPriority { }
    public class AnalyticsWidget { }
    public class PerformanceInsight { }
    public class PerformanceSummary { }
    public class ActionableRecommendation { }
    public class PerformanceInsightsReport { }
    public class PerformanceDataset { }
    public class ComplianceDataPoint { }
    public class HealthDataPoint { }
    public class RevenueProtectionMeasure { }
    public class ScalingJustification { }
    public class SLAMetric { }

    #endregion
}