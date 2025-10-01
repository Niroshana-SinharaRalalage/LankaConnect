using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using LankaConnect.Domain.Common.ValueObjects;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Communications.ValueObjects;

namespace LankaConnect.Domain.Common.Database
{
    #region Enumerations

    /// <summary>
    /// Types of monitoring in the system
    /// </summary>
    public enum MonitoringType
    {
        DatabasePerformance = 1,
        ConnectionPool = 2,
        QueryExecutionTime = 3,
        ResourceUtilization = 4,
        ConcurrentUsers = 5,
        TransactionThroughput = 6,
        CulturalEventLoad = 7,
        RegionalPerformance = 8,
        SacredEventPriority = 9,
        CommunityEngagement = 10,
        BusinessDirectoryLoad = 11,
        UserAuthenticationLoad = 12,
        NotificationDelivery = 13,
        FileStorageAccess = 14,
        SearchPerformance = 15,
        CacheHitRatio = 16,
        ApiResponseTime = 17,
        BackupOperations = 18,
        SecurityAuditLog = 19,
        DataConsistency = 20
    }


    /// <summary>
    /// Cultural performance thresholds based on event importance
    /// </summary>
    public enum CulturalPerformanceThreshold
    {
        General = 5,              // Level 5 - General events
        Regional = 6,             // Level 6 - Regional celebrations
        National = 7,             // Level 7 - National holidays
        Religious = 8,            // Level 8 - Religious observances
        Sacred = 10               // Level 10 - Sacred events (highest priority)
    }

    /// <summary>
    /// Types of performance metrics
    /// </summary>
    public enum PerformanceMetricType
    {
        ResponseTime = 1,
        Throughput = 2,
        ErrorRate = 3,
        ConcurrentUsers = 4,
        ResourceUsage = 5,
        CulturalEngagement = 6,
        BusinessDirectoryViews = 7,
        UserRetention = 8,
        SearchAccuracy = 9,
        NotificationDelivery = 10
    }

    /// <summary>
    /// Escalation procedure types
    /// </summary>
    public enum EscalationProcedureType
    {
        Automatic = 1,
        Manual = 2,
        CulturalIntelligent = 3,  // Uses cultural context for escalation
        HybridAutoManual = 4,
        SacredEventPriority = 5
    }

    /// <summary>
    /// SLA compliance status - Consolidated from AutoScalingModels and DatabaseMonitoringModels
    /// </summary>
    public enum SlaComplianceStatus
    {
        Compliant = 1,
        Warning = 2,
        Breach = 3,
        CriticalBreach = 4,
        Violation = 3,           // Alias for Breach (from AutoScalingModels)
        CriticalViolation = 4,   // Alias for CriticalBreach (from AutoScalingModels)
        SacredEventBreach = 10,  // Special breach for sacred events
        CulturalEventException = 11,  // From AutoScalingModels
        RevenueImpacting = 12         // From AutoScalingModels
    }

    /// <summary>
    /// Monitoring coordination types for multi-region
    /// </summary>
    public enum MonitoringCoordinationType
    {
        Centralized = 1,
        Distributed = 2,
        Hybrid = 3,
        CulturallyIntelligent = 4,  // Adapts based on cultural events
        RegionalAutonomous = 5
    }

    #endregion

    #region Value Objects

    // PerformanceThreshold moved to LankaConnect.Domain.Common.ValueObjects

    /// <summary>
    /// Value object for metric measurement
    /// </summary>
    public class MetricMeasurement
    {
        public double Value { get; }
        public string Unit { get; }
        public DateTime Timestamp { get; }
        public string Source { get; }

        public MetricMeasurement(double value, string unit, DateTime timestamp, string source)
        {
            Value = value;
            Unit = unit ?? throw new ArgumentNullException(nameof(unit));
            Timestamp = timestamp;
            Source = source ?? throw new ArgumentNullException(nameof(source));
        }
    }

    /// <summary>
    /// CulturalContext duplicate removed - using canonical from Domain.Communications.ValueObjects.CulturalContext
    /// </summary>

    #endregion

    #region Domain Models

    /// <summary>
    /// Database performance monitoring request
    /// </summary>
    public class DatabasePerformanceRequest
    {
        public Guid RequestId { get; }
        public MonitoringType MonitoringType { get; set; }
        public DateTime RequestTimestamp { get; }
        public string DatabaseInstance { get; set; }
        public string Region { get; set; }
        public CulturalContext? CulturalContext { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public TimeSpan RequestTimeout { get; set; }
        public int Priority { get; set; }

        public DatabasePerformanceRequest(
            MonitoringType monitoringType,
            string databaseInstance,
            string region)
        {
            RequestId = Guid.NewGuid();
            MonitoringType = monitoringType;
            RequestTimestamp = DateTime.UtcNow;
            DatabaseInstance = databaseInstance ?? throw new ArgumentNullException(nameof(databaseInstance));
            Region = region ?? throw new ArgumentNullException(nameof(region));
            Parameters = new Dictionary<string, object>();
            RequestTimeout = TimeSpan.FromMinutes(5);
            Priority = 1;
        }
    }

    /// <summary>
    /// Database performance monitoring response
    /// </summary>
    public class DatabasePerformanceResponse
    {
        public Guid RequestId { get; }
        public Guid ResponseId { get; }
        public DateTime ResponseTimestamp { get; }
        public bool IsSuccess { get; set; }
        public required string ErrorMessage { get; set; }
        public List<PerformanceMetrics> Metrics { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public required string DatabaseInstance { get; set; }
        public required string Region { get; set; }

        public DatabasePerformanceResponse(Guid requestId)
        {
            RequestId = requestId;
            ResponseId = Guid.NewGuid();
            ResponseTimestamp = DateTime.UtcNow;
            Metrics = new List<PerformanceMetrics>();
        }
    }

    /// <summary>
    /// Performance metrics model
    /// </summary>
    public class PerformanceMetrics
    {
        public Guid MetricId { get; }
        public PerformanceMetricType MetricType { get; set; }
        public string MetricName { get; set; }
        public MetricMeasurement Measurement { get; set; }
        public required PerformanceThreshold Threshold { get; set; }
        public AlertSeverity CurrentSeverity { get; set; } = AlertSeverity.Low;
        public required string DatabaseInstance { get; set; }
        public required string Region { get; set; }
        public required CulturalContext CulturalContext { get; set; }
        public Dictionary<string, object> AdditionalProperties { get; set; }

        public PerformanceMetrics(
            PerformanceMetricType metricType,
            string metricName,
            MetricMeasurement measurement)
        {
            MetricId = Guid.NewGuid();
            MetricType = metricType;
            MetricName = metricName ?? throw new ArgumentNullException(nameof(metricName));
            Measurement = measurement ?? throw new ArgumentNullException(nameof(measurement));
            AdditionalProperties = new Dictionary<string, object>();
        }

        public void EvaluateThreshold()
        {
            if (Threshold != null)
            {
                CurrentSeverity = (AlertSeverity)Threshold.GetSeverityForValue(Measurement.Value);
            }
        }
    }

    /// <summary>
    /// Alerting configuration model
    /// </summary>
    public class AlertingConfiguration
    {
        public Guid ConfigurationId { get; }
        public string ConfigurationName { get; set; }
        public MonitoringType MonitoringType { get; set; }
        public List<AlertRule> AlertRules { get; set; }
        public List<NotificationChannel> NotificationChannels { get; set; }
        public required EscalationProcedure EscalationProcedure { get; set; }
        public bool IsEnabled { get; set; }
        public required string Region { get; set; }
        public required CulturalContext CulturalContext { get; set; }
        public DateTime CreatedAt { get; }
        public DateTime? UpdatedAt { get; set; }
        public required string CreatedBy { get; set; }

        public AlertingConfiguration(string configurationName, MonitoringType monitoringType)
        {
            ConfigurationId = Guid.NewGuid();
            ConfigurationName = configurationName ?? throw new ArgumentNullException(nameof(configurationName));
            MonitoringType = monitoringType;
            AlertRules = new List<AlertRule>();
            NotificationChannels = new List<NotificationChannel>();
            IsEnabled = true;
            CreatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Alert rule model
    /// </summary>
    public class AlertRule
    {
        public Guid RuleId { get; }
        public string RuleName { get; set; }
        public string Condition { get; set; }
        public required PerformanceThreshold Threshold { get; set; }
        public AlertSeverity Severity { get; set; } = AlertSeverity.Low;
        public required string Description { get; set; }
        public bool IsEnabled { get; set; }
        public TimeSpan EvaluationInterval { get; set; }
        public int ConsecutiveFailures { get; set; }
        public CulturalPerformanceThreshold CulturalPriority { get; set; }

        public AlertRule(string ruleName, string condition)
        {
            RuleId = Guid.NewGuid();
            RuleName = ruleName ?? throw new ArgumentNullException(nameof(ruleName));
            Condition = condition ?? throw new ArgumentNullException(nameof(condition));
            IsEnabled = true;
            EvaluationInterval = TimeSpan.FromMinutes(1);
            ConsecutiveFailures = 1;
        }
    }

    /// <summary>
    /// Notification channel model
    /// </summary>
    public class NotificationChannel
    {
        public Guid ChannelId { get; }
        public string ChannelName { get; set; }
        public string ChannelType { get; set; } // Email, SMS, Slack, WhatsApp, etc.
        public string Endpoint { get; set; }
        public Dictionary<string, string> Configuration { get; set; }
        public bool IsEnabled { get; set; }
        public List<AlertSeverity> SeverityFilters { get; set; }
        public CulturalContext? CulturalContext { get; set; }

        public NotificationChannel(string channelName, string channelType, string endpoint)
        {
            ChannelId = Guid.NewGuid();
            ChannelName = channelName ?? throw new ArgumentNullException(nameof(channelName));
            ChannelType = channelType ?? throw new ArgumentNullException(nameof(channelType));
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            Configuration = new Dictionary<string, string>();
            IsEnabled = true;
            SeverityFilters = new List<AlertSeverity>();
        }
    }

    /// <summary>
    /// Cultural event performance analysis model
    /// </summary>
    public class CulturalEventPerformanceAnalysis
    {
        public Guid AnalysisId { get; }
        public CulturalContext EventContext { get; set; }
        public List<PerformanceMetrics> BaselineMetrics { get; set; }
        public List<PerformanceMetrics> EventMetrics { get; set; }
        public required PerformanceImpactAnalysis ImpactAnalysis { get; set; }
        public List<string> Recommendations { get; set; }
        public DateTime AnalysisDate { get; }
        public required string AnalyzedBy { get; set; }
        public required string Region { get; set; }

        public CulturalEventPerformanceAnalysis(CulturalContext eventContext)
        {
            AnalysisId = Guid.NewGuid();
            EventContext = eventContext ?? throw new ArgumentNullException(nameof(eventContext));
            BaselineMetrics = new List<PerformanceMetrics>();
            EventMetrics = new List<PerformanceMetrics>();
            Recommendations = new List<string>();
            AnalysisDate = DateTime.UtcNow;
        }

        public double CalculatePerformanceImpact()
        {
            if (!BaselineMetrics.Any() || !EventMetrics.Any())
                return 0;

            var baselineAvg = BaselineMetrics.Average(m => m.Measurement.Value);
            var eventAvg = EventMetrics.Average(m => m.Measurement.Value);
            
            return ((eventAvg - baselineAvg) / baselineAvg) * 100;
        }
    }

    /// <summary>
    /// Performance impact analysis model
    /// </summary>
    public class PerformanceImpactAnalysis
    {
        public double PerformanceDegradation { get; set; }
        public double UserImpact { get; set; }
        public double BusinessImpact { get; set; }
        public List<string> AffectedServices { get; set; }
        public List<string> RootCauses { get; set; }
        public required string Severity { get; set; }
        public TimeSpan RecoveryTime { get; set; }

        public PerformanceImpactAnalysis()
        {
            AffectedServices = new List<string>();
            RootCauses = new List<string>();
        }
    }

    /// <summary>
    /// Monitoring coordination model for multi-region setup
    /// </summary>
    public class MonitoringCoordination
    {
        public Guid CoordinationId { get; }
        public string CoordinationName { get; set; }
        public MonitoringCoordinationType CoordinationType { get; set; }
        public List<RegionalMonitoringNode> RegionalNodes { get; set; }
        public required CoordinationConfiguration Configuration { get; set; }
        public bool IsEnabled { get; set; }
        public DateTime CreatedAt { get; }
        public required string CreatedBy { get; set; }

        public MonitoringCoordination(string coordinationName, MonitoringCoordinationType coordinationType)
        {
            CoordinationId = Guid.NewGuid();
            CoordinationName = coordinationName ?? throw new ArgumentNullException(nameof(coordinationName));
            CoordinationType = coordinationType;
            RegionalNodes = new List<RegionalMonitoringNode>();
            IsEnabled = true;
            CreatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Regional monitoring node model
    /// </summary>
    public class RegionalMonitoringNode
    {
        public Guid NodeId { get; }
        public string NodeName { get; set; }
        public string Region { get; set; }
        public string Endpoint { get; set; }
        public NodeStatus Status { get; set; }
        public List<MonitoringType> SupportedMonitoringTypes { get; set; }
        public required CulturalContext PrimaryCulturalContext { get; set; }
        public DateTime LastHealthCheck { get; set; }
        public Dictionary<string, object> NodeConfiguration { get; set; }

        public RegionalMonitoringNode(string nodeName, string region, string endpoint)
        {
            NodeId = Guid.NewGuid();
            NodeName = nodeName ?? throw new ArgumentNullException(nameof(nodeName));
            Region = region ?? throw new ArgumentNullException(nameof(region));
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            SupportedMonitoringTypes = new List<MonitoringType>();
            NodeConfiguration = new Dictionary<string, object>();
            Status = NodeStatus.Unknown;
        }
    }

    /// <summary>
    /// Node status enumeration
    /// </summary>
    public enum NodeStatus
    {
        Unknown = 0,
        Online = 1,
        Offline = 2,
        Degraded = 3,
        Maintenance = 4
    }

    /// <summary>
    /// Coordination configuration model
    /// </summary>
    public class CoordinationConfiguration
    {
        public TimeSpan SyncInterval { get; set; }
        public int MaxRetryAttempts { get; set; }
        public TimeSpan RetryDelay { get; set; }
        public bool EnableCulturalIntelligence { get; set; }
        public bool EnableAutoFailover { get; set; }
        public Dictionary<string, object> AdditionalSettings { get; set; }

        public CoordinationConfiguration()
        {
            SyncInterval = TimeSpan.FromMinutes(1);
            MaxRetryAttempts = 3;
            RetryDelay = TimeSpan.FromSeconds(30);
            EnableCulturalIntelligence = true;
            EnableAutoFailover = true;
            AdditionalSettings = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// SLA performance metrics model
    /// </summary>
    public class SlaPerformanceMetrics
    {
        public Guid MetricId { get; }
        public string ServiceName { get; set; }
        public string SlaCategory { get; set; }
        public double TargetValue { get; set; }
        public double ActualValue { get; set; }
        public SlaComplianceStatus ComplianceStatus { get; set; }
        public DateTime MeasurementDate { get; }
        public TimeSpan MeasurementPeriod { get; set; }
        public required string Region { get; set; }
        public required CulturalContext CulturalContext { get; set; }
        public List<ComplianceBreach> Breaches { get; set; }

        public SlaPerformanceMetrics(string serviceName, string slaCategory)
        {
            MetricId = Guid.NewGuid();
            ServiceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
            SlaCategory = slaCategory ?? throw new ArgumentNullException(nameof(slaCategory));
            MeasurementDate = DateTime.UtcNow;
            Breaches = new List<ComplianceBreach>();
        }

        public void EvaluateCompliance()
        {
            var difference = Math.Abs(ActualValue - TargetValue);
            var percentageDifference = (difference / TargetValue) * 100;

            if (percentageDifference > 5) // TODO: Restore cultural priority logic with canonical CulturalContext
            {
                ComplianceStatus = SlaComplianceStatus.SacredEventBreach;
            }
            else if (percentageDifference > 25)
            {
                ComplianceStatus = SlaComplianceStatus.CriticalBreach;
            }
            else if (percentageDifference > 15)
            {
                ComplianceStatus = SlaComplianceStatus.Breach;
            }
            else if (percentageDifference > 5)
            {
                ComplianceStatus = SlaComplianceStatus.Warning;
            }
            else
            {
                ComplianceStatus = SlaComplianceStatus.Compliant;
            }
        }
    }

    /// <summary>
    /// Compliance breach model
    /// </summary>
    public class ComplianceBreach
    {
        public Guid BreachId { get; }
        public DateTime BreachStartTime { get; set; }
        public DateTime? BreachEndTime { get; set; }
        public SlaComplianceStatus BreachSeverity { get; set; }
        public required string Description { get; set; }
        public List<string> RootCauses { get; set; }
        public List<string> ResolutionActions { get; set; }
        public bool IsResolved => BreachEndTime.HasValue;
        public TimeSpan? BreachDuration => BreachEndTime?.Subtract(BreachStartTime);

        public ComplianceBreach()
        {
            BreachId = Guid.NewGuid();
            RootCauses = new List<string>();
            ResolutionActions = new List<string>();
        }
    }

    /// <summary>
    /// Escalation procedure model
    /// </summary>
    public class EscalationProcedure
    {
        public Guid ProcedureId { get; }
        public string ProcedureName { get; set; }
        public EscalationProcedureType ProcedureType { get; set; }
        public List<EscalationLevel> EscalationLevels { get; set; }
        public TimeSpan DefaultEscalationInterval { get; set; }
        public bool IsEnabled { get; set; }
        public required CulturalContext CulturalContext { get; set; }
        public Dictionary<AlertSeverity, TimeSpan> SeverityBasedIntervals { get; set; }

        public EscalationProcedure(string procedureName, EscalationProcedureType procedureType)
        {
            ProcedureId = Guid.NewGuid();
            ProcedureName = procedureName ?? throw new ArgumentNullException(nameof(procedureName));
            ProcedureType = procedureType;
            EscalationLevels = new List<EscalationLevel>();
            DefaultEscalationInterval = TimeSpan.FromMinutes(15);
            IsEnabled = true;
            SeverityBasedIntervals = new Dictionary<AlertSeverity, TimeSpan>();
        }
    }

    /// <summary>
    /// Escalation level model
    /// </summary>
    public class EscalationLevel
    {
        public int Level { get; set; }
        public string LevelName { get; set; }
        public List<NotificationChannel> NotificationChannels { get; set; }
        public TimeSpan EscalationDelay { get; set; }
        public List<string> ResponsiblePersons { get; set; }
        public List<string> Actions { get; set; }
        public bool RequireAcknowledgment { get; set; }

        public EscalationLevel(int level, string levelName)
        {
            Level = level;
            LevelName = levelName ?? throw new ArgumentNullException(nameof(levelName));
            NotificationChannels = new List<NotificationChannel>();
            ResponsiblePersons = new List<string>();
            Actions = new List<string>();
        }
    }

    /// <summary>
    /// Revenue protection monitoring model
    /// </summary>
    public class RevenueProtectionMonitoring
    {
        public Guid MonitoringId { get; }
        public string ServiceName { get; set; }
        public List<RevenueMetric> RevenueMetrics { get; set; }
        public List<BusinessImpactThreshold> ImpactThresholds { get; set; }
        public required AlertingConfiguration AlertingConfiguration { get; set; }
        public bool IsEnabled { get; set; }
        public required CulturalContext CulturalContext { get; set; }
        public DateTime CreatedAt { get; }

        public RevenueProtectionMonitoring(string serviceName)
        {
            MonitoringId = Guid.NewGuid();
            ServiceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
            RevenueMetrics = new List<RevenueMetric>();
            ImpactThresholds = new List<BusinessImpactThreshold>();
            IsEnabled = true;
            CreatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Revenue metric model
    /// </summary>
    public class RevenueMetric
    {
        public Guid MetricId { get; }
        public string MetricName { get; set; }
        public double CurrentValue { get; set; }
        public double BaselineValue { get; set; }
        public required string Currency { get; set; }
        public DateTime MeasurementTime { get; set; }
        public TimeSpan MeasurementPeriod { get; set; }
        public required string Region { get; set; }

        public RevenueMetric(string metricName)
        {
            MetricId = Guid.NewGuid();
            MetricName = metricName ?? throw new ArgumentNullException(nameof(metricName));
        }

        public double CalculateRevenueImpact()
        {
            if (BaselineValue == 0) return 0;
            return ((CurrentValue - BaselineValue) / BaselineValue) * 100;
        }
    }

    /// <summary>
    /// Business impact threshold model
    /// </summary>
    public class BusinessImpactThreshold
    {
        public Guid ThresholdId { get; }
        public string ThresholdName { get; set; }
        public double MinorImpactThreshold { get; set; }
        public double MajorImpactThreshold { get; set; }
        public double CriticalImpactThreshold { get; set; }
        public required string ImpactDescription { get; init; }
        public AlertSeverity AssociatedSeverity { get; set; } = AlertSeverity.Low;
        public CulturalPerformanceThreshold CulturalPriority { get; set; }

        public BusinessImpactThreshold(string thresholdName)
        {
            ThresholdId = Guid.NewGuid();
            ThresholdName = thresholdName ?? throw new ArgumentNullException(nameof(thresholdName));
        }
    }

    #endregion
}