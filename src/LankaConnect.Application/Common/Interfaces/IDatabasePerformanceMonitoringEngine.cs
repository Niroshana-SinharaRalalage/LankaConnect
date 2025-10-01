using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LankaConnect.Application.Common.Models.Performance;
using ConfigurationModels = LankaConnect.Application.Common.Models.Configuration;
using CriticalModels = LankaConnect.Application.Common.Models.Critical;
using PerformancePerformanceCulturalEvent = LankaConnect.Application.Common.Models.Performance.CulturalEvent;
using LankaConnect.Application.Common.Models.Monitoring;
using LankaConnect.Application.Common.Models.Security;
using LankaConnect.Domain.Common.Database;
using AppPerformance = LankaConnect.Application.Common.Performance;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.Monitoring;
using DomainDatabase = LankaConnect.Domain.Common.Database;
using MultiLanguageModels = LankaConnect.Domain.Common.Database.MultiLanguageRoutingModels;
using LankaConnect.Domain.Common.ValueObjects;
using LankaConnect.Domain.Common.Models;
using LankaConnect.Domain.Common.Performance;
using DomainPerformance = LankaConnect.Domain.Common.Performance;
using LankaConnect.Domain.Common.Configuration;

namespace LankaConnect.Application.Common.Interfaces
{
    /// <summary>
    /// Comprehensive database performance monitoring and alerting engine interface
    /// for LankaConnect's cultural intelligence platform with Fortune 500 enterprise capabilities
    /// </summary>
    public interface IDatabasePerformanceMonitoringEngine
    {
        #region Cultural Intelligence Performance Monitoring

        /// <summary>
        /// Monitors database performance during cultural events with intelligent scaling
        /// </summary>
        Task<CulturalEventPerformanceMetrics> MonitorCulturalEventPerformanceAsync(
            string culturalEventId,
            CulturalEventType eventType,
            DateTimeOffset eventStartTime,
            DateTimeOffset eventEndTime,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes performance impact of cultural celebrations on database operations
        /// </summary>
        Task<CulturalImpactAnalysis> AnalyzeCulturalEventImpactAsync(
            LankaConnect.Domain.Common.Database.PerformanceCulturalEvent culturalEvent,
            TimeSpan analysisWindow,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Predicts database load during upcoming cultural events using AI models
        /// </summary>
        Task<CulturalLoadPrediction> PredictCulturalEventLoadAsync(
            List<UpcomingCulturalEvent> upcomingEvents,
            PredictionTimeframe timeframe,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Configures cultural event-aware performance thresholds
        /// </summary>
        Task<bool> ConfigureCulturalEventThresholdsAsync(
            CulturalEventType eventType,
            PerformanceThresholdConfig thresholds,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Monitors diaspora community activity patterns affecting database performance
        /// </summary>
        Task<DiasporaActivityMetrics> MonitorDiasporaActivityPatternsAsync(
            List<string> communityRegions,
            TimeSpan monitoringWindow,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Tracks cultural content engagement impact on database performance
        /// </summary>
        Task<ContentEngagementPerformanceMetrics> TrackCulturalContentEngagementAsync(
            string contentId,
            CulturalContentType contentType,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Monitors multilingual search performance across cultural contexts
        /// </summary>
        Task<MultilingualSearchMetrics> MonitorMultilingualSearchPerformanceAsync(
            List<string> supportedLanguages,
            SearchComplexityLevel complexityLevel,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes cultural event correlation with system performance bottlenecks
        /// </summary>
        Task<CulturalCorrelationAnalysis> AnalyzeCulturalEventCorrelationsAsync(
            DateTimeOffset startDate,
            DateTimeOffset endDate,
            CancellationToken cancellationToken = default);

        #endregion

        #region Database Health Monitoring

        /// <summary>
        /// Performs comprehensive database health assessment
        /// </summary>
        Task<DatabaseHealthReport> AssessDatabaseHealthAsync(
            string connectionString,
            HealthAssessmentDepth assessmentDepth,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Monitors real-time database performance metrics
        /// </summary>
        Task<DatabasePerformanceSnapshot> GetRealTimePerformanceSnapshotAsync(
            string databaseInstance,
            MetricsGranularity granularity,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Tracks database connection pool health and optimization
        /// </summary>
        Task<LankaConnect.Application.Common.Models.Performance.ConnectionPoolMetrics> MonitorConnectionPoolHealthAsync(
            string poolIdentifier,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Monitors database query performance and optimization opportunities
        /// </summary>
        Task<QueryPerformanceAnalysis> AnalyzeQueryPerformanceAsync(
            TimeSpan analysisWindow,
            QueryComplexityThreshold threshold,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Tracks database storage utilization and growth patterns
        /// </summary>
        Task<StorageUtilizationMetrics> MonitorStorageUtilizationAsync(
            List<string> databaseNames,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Monitors database transaction performance and deadlock detection
        /// </summary>
        Task<TransactionPerformanceMetrics> MonitorTransactionPerformanceAsync(
            TransactionIsolationLevel isolationLevel,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Tracks database index performance and maintenance requirements
        /// </summary>
        Task<IndexPerformanceAnalysis> AnalyzeIndexPerformanceAsync(
            string tableName,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Monitors database backup and recovery performance
        /// </summary>
        Task<BackupRecoveryMetrics> MonitorBackupRecoveryPerformanceAsync(
            BackupStrategy backupStrategy,
            CancellationToken cancellationToken = default);

        #endregion

        #region Performance Analytics and Insights

        /// <summary>
        /// Generates comprehensive performance analytics dashboard
        /// </summary>
        Task<PerformanceAnalyticsDashboard> GeneratePerformanceAnalyticsAsync(
            DateTimeOffset startDate,
            DateTimeOffset endDate,
            AnalyticsGranularity granularity,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Provides AI-powered performance insights and recommendations
        /// </summary>
        Task<PerformanceInsightsReport> GeneratePerformanceInsightsAsync(
            PerformanceDataset dataset,
            InsightAnalysisDepth analysisDepth,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes performance trends and seasonal patterns
        /// </summary>
        Task<DomainDatabase.PerformanceTrendAnalysis> AnalyzePerformanceTrendsAsync(
            TrendAnalysisParameters parameters,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Benchmarks current performance against historical baselines
        /// </summary>
        Task<PerformanceBenchmarkReport> BenchmarkPerformanceAsync(
            BenchmarkConfiguration benchmarkConfig,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Provides capacity planning insights based on performance analytics
        /// </summary>
        Task<CapacityPlanningReport> GenerateCapacityPlanningInsightsAsync(
            CapacityPlanningHorizon planningHorizon,
            GrowthProjectionModel projectionModel,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes performance impact of application deployments
        /// </summary>
        Task<DeploymentPerformanceImpact> AnalyzeDeploymentPerformanceImpactAsync(
            string deploymentId,
            TimeSpan preDeploymentWindow,
            TimeSpan postDeploymentWindow,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates performance optimization recommendations
        /// </summary>
        Task<PerformanceOptimizationRecommendations> GenerateOptimizationRecommendationsAsync(
            OptimizationScope scope,
            PerformanceObjective objective,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Provides cost-performance analysis and optimization insights
        /// </summary>
        Task<CostPerformanceAnalysis> AnalyzeCostPerformanceRatioAsync(
            CostAnalysisParameters parameters,
            CancellationToken cancellationToken = default);

        #endregion

        #region Real-Time Alerting and Escalation

        /// <summary>
        /// Configures intelligent alerting rules with cultural event awareness
        /// </summary>
        Task<bool> ConfigureIntelligentAlertingRulesAsync(
            AlertingRuleConfiguration ruleConfig,
            CulturalEventContext culturalContext,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Manages real-time alert processing and distribution
        /// </summary>
        Task<AlertProcessingResult> ProcessRealTimeAlertAsync(
            CriticalModels.PerformanceAlert alert,
            AlertProcessingContext context,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Handles alert escalation workflows with intelligent routing
        /// </summary>
        Task<EscalationResult> ExecuteAlertEscalationAsync(
            AlertEscalationRequest escalationRequest,
            EscalationPolicy escalationPolicy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Manages alert acknowledgment and resolution tracking
        /// </summary>
        Task<AlertResolutionResult> ProcessAlertAcknowledgmentAsync(
            string alertId,
            AlertAcknowledgment acknowledgment,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Configures alert notification channels and preferences
        /// </summary>
        Task<bool> ConfigureAlertNotificationChannelsAsync(
            List<LankaConnect.Domain.Common.Monitoring.NotificationChannel> channels,
            NotificationPreferences preferences,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Manages alert suppression during planned maintenance windows
        /// </summary>
        Task<AlertSuppressionResult> ManageAlertSuppressionAsync(
            MaintenanceWindow maintenanceWindow,
            AlertSuppressionPolicy suppressionPolicy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Tracks alert effectiveness and false positive rates
        /// </summary>
        Task<AlertEffectivenessMetrics> AnalyzeAlertEffectivenessAsync(
            TimeSpan analysisWindow,
            AlertEffectivenessThreshold threshold,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Provides intelligent alert correlation and deduplication
        /// </summary>
        Task<AlertCorrelationResult> CorrelateAndDeduplicateAlertsAsync(
            List<CriticalModels.PerformanceAlert> incomingAlerts,
            CorrelationConfiguration correlationConfig,
            CancellationToken cancellationToken = default);

        #endregion

        #region SLA Compliance and Reporting

        /// <summary>
        /// Validates SLA compliance across all performance metrics
        /// </summary>
        Task<SLAComplianceReport> ValidateSLAComplianceAsync(
            List<ServiceLevelAgreement> slas,
            ComplianceValidationPeriod validationPeriod,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates comprehensive SLA performance reports
        /// </summary>
        Task<SLAPerformanceReport> GenerateSLAPerformanceReportAsync(
            SLAReportingConfiguration reportConfig,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Tracks SLA breach incidents and root cause analysis
        /// </summary>
        Task<SLABreachAnalysis> AnalyzeSLABreachIncidentsAsync(
            TimeSpan analysisWindow,
            SLABreachSeverity minimumSeverity,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Manages SLA credit calculations and customer impact assessment
        /// </summary>
        Task<SLACreditCalculation> CalculateSLACreditsAsync(
            List<SLABreach> slaBreaaches,
            CreditCalculationPolicy creditPolicy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Provides predictive SLA risk assessment
        /// </summary>
        Task<SLARiskAssessment> AssessSLARiskAsync(
            List<ServiceLevelAgreement> slas,
            RiskAssessmentTimeframe timeframe,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates customer-facing SLA transparency reports
        /// </summary>
        Task<CustomerSLAReport> GenerateCustomerSLAReportAsync(
            string customerId,
            ReportingPeriod reportingPeriod,
            SLAReportFormat reportFormat,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Manages SLA threshold adjustments based on business requirements
        /// </summary>
        Task<bool> AdjustSLAThresholdsAsync(
            string slaId,
            SLAThresholdAdjustment adjustment,
            ThresholdAdjustmentReason reason,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Tracks SLA improvement initiatives and their effectiveness
        /// </summary>
        Task<SLAImprovementTracker> TrackSLAImprovementInitiativesAsync(
            List<SLAImprovementInitiative> initiatives,
            CancellationToken cancellationToken = default);

        #endregion

        #region Multi-Region Monitoring Coordination

        /// <summary>
        /// Coordinates performance monitoring across multiple geographic regions
        /// </summary>
        Task<MultiRegionPerformanceCoordination> CoordinateMultiRegionMonitoringAsync(
            List<GeographicRegion> regions,
            LankaConnect.Domain.Common.Database.CoordinationStrategy coordinationStrategy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Manages cross-region performance data synchronization
        /// </summary>
        Task<RegionSyncResult> SynchronizeRegionPerformanceDataAsync(
            string sourceRegion,
            List<string> targetRegions,
            SynchronizationPolicy syncPolicy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes regional performance disparities and optimization opportunities
        /// </summary>
        Task<RegionalPerformanceAnalysis> AnalyzeRegionalPerformanceDisparitiesAsync(
            List<string> regions,
            PerformanceComparisonMetrics metrics,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Manages failover coordination between regions during performance degradation
        /// </summary>
        Task<RegionFailoverResult> CoordinateRegionFailoverAsync(
            string primaryRegion,
            string failoverRegion,
            FailoverTriggerCriteria triggerCriteria,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Tracks global performance metrics with regional breakdown
        /// </summary>
        Task<GlobalPerformanceMetrics> TrackGlobalPerformanceMetricsAsync(
            GlobalMetricsConfiguration metricsConfig,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Manages timezone-aware performance monitoring and reporting
        /// </summary>
        Task<TimezoneAwarePerformanceReport> GenerateTimezoneAwareReportAsync(
            List<TimeZoneInfo> targetTimezones,
            ReportingConfiguration reportConfig,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Coordinates regional compliance with local data protection regulations
        /// </summary>
        Task<AppPerformance.RegionalComplianceStatus> ValidateRegionalComplianceAsync(
            string region,
            List<DataProtectionRegulation> regulations,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Optimizes inter-region communication for performance monitoring
        /// </summary>
        Task<InterRegionOptimizationResult> OptimizeInterRegionCommunicationAsync(
            NetworkTopology networkTopology,
            OptimizationObjective objective,
            CancellationToken cancellationToken = default);

        #endregion

        #region Revenue Protection Integration

        /// <summary>
        /// Monitors performance metrics that directly impact revenue generation
        /// </summary>
        Task<RevenueImpactMetrics> MonitorRevenueImpactPerformanceAsync(
            RevenueMetricsConfiguration revenueConfig,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculates revenue at risk due to performance degradation
        /// </summary>
        Task<RevenueRiskCalculation> CalculateRevenueAtRiskAsync(
            PerformanceDegradationScenario scenario,
            RevenueCalculationModel calculationModel,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Triggers revenue protection measures during performance incidents
        /// </summary>
        Task<RevenueProtectionResult> TriggerRevenueProtectionMeasuresAsync(
            LankaConnect.Application.Common.Models.Performance.PerformanceIncident incident,
            LankaConnect.Application.Common.Models.Performance.RevenueProtectionPolicy protectionPolicy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes customer churn risk due to performance issues
        /// </summary>
        Task<LankaConnect.Application.Common.Models.Performance.ChurnRiskAnalysis> AnalyzePerformanceChurnRiskAsync(
            List<string> customerIds,
            LankaConnect.Application.Common.Models.Performance.PerformanceImpactThreshold impactThreshold,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Tracks revenue recovery after performance incident resolution
        /// </summary>
        Task<LankaConnect.Application.Common.Models.Performance.RevenueRecoveryMetrics> TrackRevenueRecoveryAsync(
            string incidentId,
            TimeSpan recoveryWindow,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates revenue-focused performance improvement recommendations
        /// </summary>
        Task<LankaConnect.Application.Common.Models.Performance.RevenueOptimizationRecommendations> GenerateRevenueOptimizationRecommendationsAsync(
            LankaConnect.Application.Common.Models.Performance.RevenueOptimizationObjective objective,
            LankaConnect.Application.Common.Models.Performance.FinancialConstraints constraints,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Manages revenue protection during planned maintenance activities
        /// </summary>
        Task<LankaConnect.Application.Common.Models.Performance.MaintenanceRevenueProtection> ManageMaintenanceRevenueProtectionAsync(
            PlannedMaintenanceWindow maintenanceWindow,
            RevenueProtectionStrategy protectionStrategy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes competitive impact of performance differences
        /// </summary>
        Task<CompetitivePerformanceAnalysis> AnalyzeCompetitivePerformanceImpactAsync(
            CompetitiveBenchmarkData benchmarkData,
            MarketPositionAnalysis marketPosition,
            CancellationToken cancellationToken = default);

        #endregion

        #region Auto-Scaling System Integration

        /// <summary>
        /// Integrates with auto-scaling decisions based on performance metrics
        /// </summary>
        Task<AutoScalingRecommendation> GenerateAutoScalingRecommendationAsync(
            PerformanceMetrics currentMetrics,
            ConfigurationModels.ScalingPolicy scalingPolicy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Monitors auto-scaling events and their performance impact
        /// </summary>
        Task<AutoScalingPerformanceImpact> MonitorAutoScalingPerformanceImpactAsync(
            string scalingEventId,
            TimeSpan monitoringWindow,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Optimizes scaling thresholds based on performance patterns
        /// </summary>
        Task<ScalingThresholdOptimization> OptimizeScalingThresholdsAsync(
            HistoricalPerformanceData historicalData,
            OptimizationObjective objective,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Coordinates predictive scaling based on performance forecasting
        /// </summary>
        Task<PredictiveScalingCoordination> CoordinatePredictiveScalingAsync(
            PerformanceForecast performanceForecast,
            PredictiveScalingPolicy scalingPolicy,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Manages scaling-related performance anomaly detection
        /// </summary>
        Task<ScalingAnomalyDetectionResult> DetectScalingPerformanceAnomaliesAsync(
            ScalingMetrics scalingMetrics,
            AnomalyDetectionThreshold detectionThreshold,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates scaling effectiveness against performance objectives
        /// </summary>
        Task<ScalingEffectivenessValidation> ValidateScalingEffectivenessAsync(
            List<ScalingEvent> scalingEvents,
            PerformanceObjective performanceObjective,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Manages cost-aware scaling decisions based on performance requirements
        /// </summary>
        Task<CostAwareScalingDecision> MakeCostAwareScalingDecisionAsync(
            PerformanceRequirement performanceRequirement,
            CostConstraints costConstraints,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Coordinates scaling activities across multiple application tiers
        /// </summary>
        Task<MultiTierScalingCoordination> CoordinateMultiTierScalingAsync(
            List<ApplicationTier> applicationTiers,
            ScalingCoordinationStrategy coordinationStrategy,
            CancellationToken cancellationToken = default);

        #endregion

        #region Performance Threshold Management

        /// <summary>
        /// Manages dynamic performance threshold adjustments
        /// </summary>
        Task<bool> ManageDynamicPerformanceThresholdsAsync(
            string metricName,
            DynamicThresholdConfiguration thresholdConfig,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates threshold effectiveness and accuracy
        /// </summary>
        Task<ThresholdValidationResult> ValidateThresholdEffectivenessAsync(
            List<PerformanceThreshold> thresholds,
            ValidationCriteria validationCriteria,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Provides intelligent threshold recommendation based on historical data
        /// </summary>
        Task<ThresholdRecommendation> RecommendOptimalThresholdsAsync(
            string metricName,
            HistoricalPerformanceData historicalData,
            ThresholdOptimizationObjective objective,
            CancellationToken cancellationToken = default);

        #endregion

        #region Advanced Monitoring Features

        /// <summary>
        /// Enables distributed tracing performance monitoring
        /// </summary>
        Task<DistributedTracingMetrics> MonitorDistributedTracingPerformanceAsync(
            string traceId,
            LankaConnect.Application.Common.Models.Security.TracingConfiguration tracingConfig,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Manages synthetic transaction monitoring for proactive performance validation
        /// </summary>
        Task<SyntheticTransactionResults> ExecuteSyntheticTransactionMonitoringAsync(
            List<SyntheticTransaction> syntheticTransactions,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Provides application performance monitoring integration
        /// </summary>
        Task<APMIntegrationStatus> IntegrateApplicationPerformanceMonitoringAsync(
            APMConfiguration apmConfig,
            CancellationToken cancellationToken = default);

        #endregion

        #region System Integration and Lifecycle Management

        /// <summary>
        /// Initializes the performance monitoring engine with configuration
        /// </summary>
        Task<bool> InitializePerformanceMonitoringEngineAsync(
            PerformanceMonitoringConfiguration configuration,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs graceful shutdown of monitoring operations
        /// </summary>
        Task<bool> ShutdownPerformanceMonitoringEngineAsync(
            ShutdownConfiguration shutdownConfig,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validates system health and readiness for monitoring operations
        /// </summary>
        Task<SystemHealthValidation> ValidateSystemHealthAsync(
            HealthValidationConfiguration validationConfig,
            CancellationToken cancellationToken = default);

        #endregion
    }

    #region Supporting Types and Enums


    public class CulturalImpactAnalysis
    {
        public required string AnalysisId { get; set; }
        public required PerformanceCulturalEvent PerformanceCulturalEvent { get; set; }
        public PerformanceImpactSeverity ImpactSeverity { get; set; }
        public required Dictionary<string, CriticalModels.PerformanceMetric> AffectedMetrics { get; set; }
        public required List<RegionalImpact> RegionalImpacts { get; set; }
        public TimeSpan AnalysisWindow { get; set; }
        public required List<ImpactMitigationStrategy> MitigationStrategies { get; set; }
    }

    public class CulturalLoadPrediction
    {
        public required string PredictionId { get; set; }
        public required List<LoadPredictionPoint> PredictionPoints { get; set; }
        public PredictionConfidenceLevel ConfidenceLevel { get; set; }
        public required List<CulturalEventFactor> InfluencingFactors { get; set; }
        public required PredictionTimeframe Timeframe { get; set; }
        public required List<RecommendedPreparation> RecommendedPreparations { get; set; }
    }

    public class DatabaseHealthReport
    {
        public required string ReportId { get; set; }
        public DateTimeOffset GeneratedAt { get; set; }
        public OverallHealthStatus OverallHealth { get; set; }
        public required Dictionary<string, ComponentHealth> ComponentHealthStatuses { get; set; }
        public required List<HealthIssue> IdentifiedIssues { get; set; }
        public required List<HealthRecommendation> Recommendations { get; set; }
        public required HealthTrend HealthTrend { get; set; }
    }

    public class PerformanceAnalyticsDashboard
    {
        public required string DashboardId { get; set; }
        public DateTimeOffset StartDate { get; set; }
        public DateTimeOffset EndDate { get; set; }
        public required Dictionary<string, AnalyticsWidget> Widgets { get; set; }
        public required List<PerformanceInsight> KeyInsights { get; set; }
        public required PerformanceSummary OverallSummary { get; set; }
        public required List<ActionableRecommendation> ActionableRecommendations { get; set; }
    }

    public class SLAComplianceReport
    {
        public required string ReportId { get; set; }
        public ComplianceValidationPeriod ValidationPeriod { get; set; }
        public required Dictionary<string, SLAComplianceStatus> SLAStatuses { get; set; }
        public required OverallComplianceScore OverallComplianceScore { get; set; }
        public required List<ComplianceViolation> Violations { get; set; }
        public required ComplianceTrend ComplianceTrend { get; set; }
    }

    public class RevenueImpactMetrics
    {
        public required string MetricsId { get; set; }
        public DateTimeOffset MeasurementTime { get; set; }
        public decimal EstimatedRevenueImpact { get; set; }
        public required Dictionary<string, RevenueMetric> RevenueMetrics { get; set; }
        public required List<RevenueRiskFactor> RiskFactors { get; set; }
        public required RevenueProtectionStatus ProtectionStatus { get; set; }
    }

    public class AutoScalingRecommendation
    {
        public required string RecommendationId { get; set; }
        public required ScalingAction RecommendedAction { get; set; }
        public ScalingDirection Direction { get; set; }
        public int RecommendedCapacityChange { get; set; }
        public RecommendationConfidence Confidence { get; set; }
        public required List<string> Justifications { get; set; }
        public TimeSpan RecommendedImplementationWindow { get; set; }
    }

    // CulturalEventType enum removed - using LankaConnect.Domain.Common.Enums.CulturalEventType

    public enum PerformanceImpactSeverity
    {
        Negligible,
        Minor,
        Moderate,
        Significant,
        Critical
    }

    public enum PredictionConfidenceLevel
    {
        Low,
        Medium,
        High,
        VeryHigh
    }

    public enum ScalingDirection
    {
        ScaleUp,
        ScaleDown,
        ScaleOut,
        ScaleIn,
        Maintain
    }

    public enum RecommendationConfidence
    {
        Low,
        Medium,
        High,
        VeryHigh
    }

    public enum OverallHealthStatus
    {
        Excellent,
        Good,
        Fair,
        Poor,
        Critical
    }

    public enum ComplianceValidationPeriod
    {
        Daily,
        Weekly,
        Monthly,
        Quarterly,
        Annually
    }

    public enum HealthAssessmentDepth
    {
        Basic,
        Standard,
        Comprehensive,
        Enterprise
    }

    public enum MetricsGranularity
    {
        Second,
        Minute,
        Hour,
        Day
    }

    public enum AnalyticsGranularity
    {
        Hourly,
        Daily,
        Weekly,
        Monthly
    }

    public enum InsightAnalysisDepth
    {
        Surface,
        Detailed,
        Deep,
        Comprehensive
    }

    #endregion
}