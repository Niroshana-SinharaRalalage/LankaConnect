using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Domain.Common.Database;

/// <summary>
/// Additional missing model types for Cultural Intelligence services
/// </summary>

// Calendar and Event Models
public class CulturalEventCalendar
{
    public string CalendarId { get; set; } = string.Empty;
    public string CalendarName { get; set; } = string.Empty;
    public CulturalEventType CalendarType { get; set; }
    public string CulturalContext { get; set; } = string.Empty;
    public CulturalEventSchedule[] Events { get; set; } = Array.Empty<CulturalEventSchedule>();
    public DateTime LastSynchronized { get; set; }
}

public class CulturalEventPrediction
{
    public CulturalEventType EventType { get; set; }
    public string CommunityId { get; set; } = string.Empty;
    public string GeographicRegion { get; set; } = string.Empty;
    public DateTime PredictedStartTime { get; set; }
    public DateTime PredictedEndTime { get; set; }
    public double ExpectedTrafficMultiplier { get; set; }
    public double ConfidenceScore { get; set; }
    public CulturalSignificance CulturalSignificanceLevel { get; set; }
    public List<string> AffectedCommunities { get; set; } = new();
    public string PredictionModel { get; set; } = string.Empty;
}

// Database and Scaling Models
public class DatabaseScalingMetrics
{
    public double AverageConnectionUtilization { get; set; }
    public TimeSpan ResponseTimePercentile95 { get; set; }
    public int QueriesPerSecond { get; set; }
    public double ErrorRate { get; set; }
    public double CpuUtilization { get; set; }
    public double MemoryUtilization { get; set; }
    public int ConnectionCount { get; set; }
    public DateTime MetricTimestamp { get; set; } = DateTime.UtcNow;
}

public class AutoScalingDecision
{
    public string GeographicRegion { get; set; } = string.Empty;
    public DateTime DecisionTimestamp { get; set; } = DateTime.UtcNow;
    public ScalingDirection ScalingDirection { get; set; }
    public int TargetCapacityPercentage { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public CulturalContext? CulturalContext { get; set; }
    public TimeSpan EstimatedScalingTime { get; set; }
    public double ConfidenceLevel { get; set; }
}

// Security Enhancement Models
public class EnhancedSecurityConfig
{
    public string ConfigurationId { get; set; } = string.Empty;
    public string[] SecurityFeatures { get; set; } = Array.Empty<string>();
    public string CulturalSensitivityLevel { get; set; } = string.Empty;
    public bool EnableAdvancedThreatDetection { get; set; }
    public string[] ComplianceStandards { get; set; } = Array.Empty<string>();
}

public class SacredEventSecurityResult
{
    public bool IsSecurityEnhanced { get; set; }
    public string[] AppliedSecurityMeasures { get; set; } = Array.Empty<string>();
    public decimal SecurityLevel { get; set; }
    public string[] SacredContentProtections { get; set; } = Array.Empty<string>();
    public bool RequiresSpecialHandling { get; set; }
}

public class SensitiveData
{
    public string DataId { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public decimal SensitivityScore { get; set; }
    public string CulturalContext { get; set; } = string.Empty;
    public string[] ProtectionRequirements { get; set; } = Array.Empty<string>();
}

public class CulturalEncryptionPolicy
{
    public string PolicyId { get; set; } = string.Empty;
    public string EncryptionAlgorithm { get; set; } = string.Empty;
    public string KeyManagementStrategy { get; set; } = string.Empty;
    public string[] CulturalConsiderations { get; set; } = Array.Empty<string>();
    public bool RequiresCulturalApproval { get; set; }
}

public class EncryptionResult
{
    public bool IsSuccessful { get; set; }
    public string EncryptedDataId { get; set; } = string.Empty;
    public string EncryptionMethod { get; set; } = string.Empty;
    public DateTime EncryptionTimestamp { get; set; }
    public string[] SecurityWarnings { get; set; } = Array.Empty<string>();
}

// Privacy and Data Protection Models
public class CulturalDataElement
{
    public string ElementId { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public string CulturalContext { get; set; } = string.Empty;
    public decimal CulturalSensitivity { get; set; }
    public string[] PrivacyClassifications { get; set; } = Array.Empty<string>();
}

public enum PrivacyClassification
{
    Public,
    Internal,
    Confidential,
    Restricted,
    SacredConfidential
}

public enum DataSensitivityLevel
{
    Low,
    Medium,
    High,
    Critical,
    Sacred
}

public class PrivacyProtectionMetrics
{
    public decimal ProtectionScore { get; set; }
    public int DataElementsProtected { get; set; }
    public string[] ProtectionMethods { get; set; } = Array.Empty<string>();
    public DateTime LastAssessment { get; set; }
    public bool ComplianceStatus { get; set; }
}

public class PrivacyViolation
{
    public string ViolationId { get; set; } = string.Empty;
    public string ViolationType { get; set; } = string.Empty;
    public decimal SeverityScore { get; set; }
    public string CulturalImpact { get; set; } = string.Empty;
    public DateTime DetectedTimestamp { get; set; }
    public bool IsResolved { get; set; }
}

// Cross-Cultural Security Models
public class CulturalBoundary
{
    public string BoundaryId { get; set; } = string.Empty;
    public string[] CulturalContexts { get; set; } = Array.Empty<string>();
    public string[] SecurityRequirements { get; set; } = Array.Empty<string>();
    public bool RequiresSpecialHandling { get; set; }
    public string[] CrossCulturalRules { get; set; } = Array.Empty<string>();
}

public class SecurityEnforcementPolicy
{
    public string PolicyId { get; set; } = string.Empty;
    public string[] EnforcementRules { get; set; } = Array.Empty<string>();
    public string CulturalScope { get; set; } = string.Empty;
    public decimal EnforcementStrength { get; set; }
    public bool EnableAutomaticEnforcement { get; set; }
}

public class CrossCulturalSecurityResult
{
    public bool IsCompliant { get; set; }
    public string[] AppliedPolicies { get; set; } = Array.Empty<string>();
    public string[] CulturalConsiderations { get; set; } = Array.Empty<string>();
    public decimal SecurityScore { get; set; }
    public string[] CrossCulturalWarnings { get; set; } = Array.Empty<string>();
}

// Additional types required by CulturalIntelligencePredictiveScalingService
public class ScalingExecutionResult
{
    public DateTime ExecutionTimestamp { get; set; } = DateTime.UtcNow;
    public TimeSpan ExecutionDuration { get; set; }
    public bool Success { get; set; }
    public int AchievedCapacityPercentage { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string PerformanceImpact { get; set; } = string.Empty;
    public List<string> ExecutionLogs { get; set; } = new();
}

public class CulturalLoadPattern
{
    public string CommunityId { get; set; } = string.Empty;
    public string GeographicRegion { get; set; } = string.Empty;
    public CulturalEventType PatternType { get; set; }
    public int BaselineLoad { get; set; }
    public int PeakLoad { get; set; }
    public double LoadMultiplier { get; set; }
    public TimeSpan TypicalDuration { get; set; }
    public double HistoricalAccuracy { get; set; }
}

public class PredictiveScalingInsights
{
    public double PredictionAccuracy { get; set; }
    public List<CulturalEventPrediction> CulturalEventPredictions { get; set; } = new();
    public Dictionary<string, int> GeographicLoadDistribution { get; set; } = new();
    public List<string> ScalingRecommendations { get; set; } = new();
    public List<string> OptimizationOpportunities { get; set; } = new();
}

public class GeographicScalingConfiguration
{
    public string Region { get; set; } = string.Empty;
    public int MaxConcurrentUsers { get; set; }
    public double ScalingThreshold { get; set; }
    public string Strategy { get; set; } = string.Empty;
}

public class CrossRegionScalingCoordination
{
    public CulturalEventType CulturalEvent { get; set; }
    public List<string> AffectedRegions { get; set; } = new();
    public DateTime CoordinationTimestamp { get; set; } = DateTime.UtcNow;
    public bool IsCoordinated { get; set; }
    public Dictionary<string, int> RegionalCapacities { get; set; } = new();
}

public class CulturalScalingMetrics
{
    public DateTime EvaluationPeriodStart { get; set; }
    public DateTime EvaluationPeriodEnd { get; set; }
    public double AverageResponseTime { get; set; }
    public int TotalScalingActions { get; set; }
    public double ScalingEfficiency { get; set; }
    public Dictionary<CulturalEventType, int> EventsByType { get; set; } = new();
}

public class CulturalScalingAlert
{
    public string AlertId { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty;
    public DateTime AlertTimestamp { get; set; } = DateTime.UtcNow;
    public string Message { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public CulturalEventType? RelatedEvent { get; set; }
    public string Severity { get; set; } = "Medium";
}

public class DiasporaCommunityScalingProfile
{
    public string CommunityId { get; set; } = string.Empty;
    public List<string> GeographicRegions { get; set; } = new();
    public Dictionary<string, double> RegionalTrafficPatterns { get; set; } = new();
    public List<CulturalEventType> PreferredEvents { get; set; } = new();
    public DateTime ProfileCreated { get; set; } = DateTime.UtcNow;
}

public class CulturalContext
{
    public string CommunityId { get; set; } = string.Empty;
    public string GeographicRegion { get; set; } = string.Empty;
}

public enum ScalingDirection
{
    Maintain,
    Up,
    Down,
    Emergency,
    CulturalBoost,
    RevenueBoost,
    RegionalRebalance
}

public enum CulturalSignificance
{
    Low,
    Medium,
    High,
    Critical,
    Sacred
}

// Additional types for service implementation
public class CulturalEventSchedule
{
    public CulturalEventType EventType { get; set; }
    public DateTime ScheduledDate { get; set; }
    public TimeSpan Duration { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class PredictiveScalingOptions
{
    public int ScalingPredictionWindowHours { get; set; } = 48;
    public double MinScalingThresholdPercentage { get; set; } = 0.3;
    public double MaxScalingThresholdPercentage { get; set; } = 0.8;
    public double CulturalEventMultiplier { get; set; } = 5.0;
    public string DefaultStrategy { get; set; } = "adaptive";
    public double PredictionAccuracyTarget { get; set; } = 0.9;
}