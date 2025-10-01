using System;
using System.Collections.Generic;

namespace LankaConnect.Domain.Common.Database
{
    #region Regulatory and Compliance Types

    public enum RegulatoryFramework
    {
        GDPR,
        CCPA,
        HIPAA,
        SOX,
        PCI_DSS,
        ISO27001,
        NIST,
        Custom
    }

    public enum IndustryType
    {
        Healthcare,
        Financial,
        Education,
        Government,
        Technology,
        Retail,
        Manufacturing,
        Cultural
    }

    public class RegulatoryComplianceResult
    {
        public Guid Id { get; set; }
        public RegulatoryFramework Framework { get; set; }
        public bool IsCompliant { get; set; }
        public List<string> ComplianceGaps { get; set; } = new();
        public List<string> RecommendedActions { get; set; } = new();
        public DateTime AssessmentDate { get; set; }
    }

    public class ComplianceRequirements
    {
        public Guid Id { get; set; }
        public IndustryType Industry { get; set; }
        public List<RegulatoryFramework> ApplicableFrameworks { get; set; } = new();
        public Dictionary<string, object> SpecificRequirements { get; set; } = new();
        public DateTime EffectiveDate { get; set; }
    }

    public class IndustryComplianceResult
    {
        public Guid Id { get; set; }
        public IndustryType Industry { get; set; }
        public Dictionary<string, bool> ComplianceStatus { get; set; } = new();
        public List<string> CriticalFindings { get; set; } = new();
        public string ComplianceScore { get; set; } = string.Empty;
        public DateTime AssessmentDate { get; set; }
    }

    public class ComplianceMetrics
    {
        public Guid Id { get; set; }
        public Dictionary<string, double> MetricValues { get; set; } = new();
        public List<string> TrendIndicators { get; set; } = new();
        public DateTime MetricsTimestamp { get; set; }
    }

    public class ComplianceMonitoringResult
    {
        public Guid Id { get; set; }
        public bool IsMonitoringActive { get; set; }
        public List<string> MonitoredAreas { get; set; } = new();
        public Dictionary<string, string> MonitoringResults { get; set; } = new();
        public DateTime MonitoringTimestamp { get; set; }
    }

    #endregion

    #region Security Incident Types

    public class SecurityIncidentTrigger
    {
        public Guid Id { get; set; }
        public string TriggerType { get; set; } = string.Empty;
        public Dictionary<string, object> TriggerConditions { get; set; } = new();
        public string Severity { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class CulturalIncidentContext
    {
        public Guid Id { get; set; }
        public string CulturalRegion { get; set; } = string.Empty;
        public List<string> AffectedCommunities { get; set; } = new();
        public Dictionary<string, object> CulturalFactors { get; set; } = new();
        public string SensitivityLevel { get; set; } = string.Empty;
    }

    public class ResponsePlaybook
    {
        public Guid Id { get; set; }
        public string PlaybookName { get; set; } = string.Empty;
        public List<string> ResponseSteps { get; set; } = new();
        public Dictionary<string, object> PlaybookParameters { get; set; } = new();
        public bool IsActive { get; set; }
    }

    public class CriticalIncident
    {
        public Guid Id { get; set; }
        public string IncidentType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public DateTime IncidentTimestamp { get; set; }
        public Dictionary<string, object> IncidentData { get; set; } = new();
    }

    public class EscalationPath
    {
        public Guid Id { get; set; }
        public List<string> EscalationLevels { get; set; } = new();
        public Dictionary<string, string> ContactInformation { get; set; } = new();
        public List<string> EscalationCriteria { get; set; } = new();
        public bool IsActive { get; set; }
    }

    public class IncidentEscalationResult
    {
        public Guid Id { get; set; }
        public bool IsEscalated { get; set; }
        public string EscalationLevel { get; set; } = string.Empty;
        public List<string> NotifiedParties { get; set; } = new();
        public DateTime EscalationTimestamp { get; set; }
    }

    #endregion

    #region Advanced Security Types

    public class ThreatIntelligence
    {
        public Guid Id { get; set; }
        public Dictionary<string, object> ThreatData { get; set; } = new();
        public List<string> ThreatIndicators { get; set; } = new();
        public string ThreatLevel { get; set; } = string.Empty;
        public DateTime IntelligenceTimestamp { get; set; }
    }

    public class APTDetectionResult
    {
        public Guid Id { get; set; }
        public bool APTDetected { get; set; }
        public List<string> DetectedPatterns { get; set; } = new();
        public string ThreatActorProfile { get; set; } = string.Empty;
        public Dictionary<string, object> DetectionMetrics { get; set; } = new();
        public DateTime DetectionTimestamp { get; set; }
    }

    public class BehavioralAnalyticsConfiguration
    {
        public Guid Id { get; set; }
        public Dictionary<string, object> AnalyticsRules { get; set; } = new();
        public List<string> MonitoredBehaviors { get; set; } = new();
        public TimeSpan AnalysisWindow { get; set; }
        public bool IsEnabled { get; set; }
    }

    public class CulturalUserBehaviorPatterns
    {
        public Guid Id { get; set; }
        public string CulturalContext { get; set; } = string.Empty;
        public Dictionary<string, object> BehaviorPatterns { get; set; } = new();
        public List<string> NormalizedBehaviors { get; set; } = new();
        public DateTime PatternAnalysisDate { get; set; }
    }

    public class BehavioralAnalyticsResult
    {
        public Guid Id { get; set; }
        public Dictionary<string, double> AnomalyScores { get; set; } = new();
        public List<string> DetectedAnomalies { get; set; } = new();
        public string RiskAssessment { get; set; } = string.Empty;
        public DateTime AnalysisTimestamp { get; set; }
    }

    public class SOARConfiguration
    {
        public Guid Id { get; set; }
        public Dictionary<string, object> AutomationRules { get; set; } = new();
        public List<string> IntegratedTools { get; set; } = new();
        public bool IsEnabled { get; set; }
    }

    public class AutomatedResponsePlaybooks
    {
        public Guid Id { get; set; }
        public Dictionary<string, List<string>> Playbooks { get; set; } = new();
        public List<string> AutomatedActions { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }

    public class SOARResult
    {
        public Guid Id { get; set; }
        public bool AutomationSuccessful { get; set; }
        public List<string> ExecutedActions { get; set; } = new();
        public string AutomationSummary { get; set; } = string.Empty;
        public DateTime ExecutionTimestamp { get; set; }
    }

    #endregion

    #region Cryptography Types

    public class QuantumCryptographyConfiguration
    {
        public Guid Id { get; set; }
        public Dictionary<string, string> CryptographicAlgorithms { get; set; } = new();
        public List<string> SupportedProtocols { get; set; } = new();
        public bool QuantumReadinessEnabled { get; set; }
        public DateTime ConfigurationDate { get; set; }
    }

    public class CryptographicTransitionPlan
    {
        public Guid Id { get; set; }
        public Dictionary<string, string> TransitionSteps { get; set; } = new();
        public List<string> MigrationMilestones { get; set; } = new();
        public DateTime PlannedCompletionDate { get; set; }
        public string TransitionStatus { get; set; } = string.Empty;
    }

    public class QuantumResistantCryptographyResult
    {
        public Guid Id { get; set; }
        public bool IsQuantumResistant { get; set; }
        public Dictionary<string, string> CryptographicStatus { get; set; } = new();
        public List<string> RecommendedUpgrades { get; set; } = new();
        public DateTime AssessmentDate { get; set; }
    }

    #endregion

    #region Performance Optimization Types

    public class HighVolumeEventConfiguration
    {
        public Guid Id { get; set; }
        public Dictionary<string, object> EventParameters { get; set; } = new();
        public List<string> OptimizationTargets { get; set; } = new();
        public TimeSpan EventDuration { get; set; }
        public bool IsEnabled { get; set; }
    }

    public class PerformanceOptimizationStrategy
    {
        public Guid Id { get; set; }
        public string StrategyName { get; set; } = string.Empty;
        public Dictionary<string, object> OptimizationParameters { get; set; } = new();
        public List<string> TargetMetrics { get; set; } = new();
        public DateTime StrategyDate { get; set; }
    }

    public class SecurityPerformanceOptimizationResult
    {
        public Guid Id { get; set; }
        public bool OptimizationSuccessful { get; set; }
        public Dictionary<string, double> PerformanceImprovements { get; set; } = new();
        public List<string> OptimizationActions { get; set; } = new();
        public DateTime OptimizationTimestamp { get; set; }
    }

    #endregion
}