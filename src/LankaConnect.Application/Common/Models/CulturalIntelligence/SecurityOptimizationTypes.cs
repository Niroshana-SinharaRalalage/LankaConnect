namespace LankaConnect.Application.Common.Models.CulturalIntelligence;

public class DatabaseSecurityConfiguration
{
    public required string ConfigurationId { get; set; }
    public required Dictionary<string, object> SecurityPolicies { get; set; }
    public required List<string> ComplianceFrameworks { get; set; }
    public required string EncryptionLevel { get; set; }
    public required Dictionary<string, string> AccessControls { get; set; }
    public required List<string> AuditRequirements { get; set; }
    public bool RealTimeMonitoring { get; set; }
    public Dictionary<string, object> ThreatDetection { get; set; } = new();
}

public class SecurityComplianceValidation
{
    public required string ValidationId { get; set; }
    public required List<string> ComplianceStandards { get; set; }
    public required Dictionary<string, bool> ComplianceStatus { get; set; }
    public required DateTime ValidationTimestamp { get; set; }
    public required string ValidatingAuthority { get; set; }
    public Dictionary<string, object> ValidationDetails { get; set; } = new();
    public List<string> NonComplianceIssues { get; set; } = new();
}


public class ThreatAssessmentParameters
{
    public required string AssessmentId { get; set; }
    public required List<string> ThreatVectors { get; set; }
    public required Dictionary<string, double> RiskFactors { get; set; }
    public required string AssessmentScope { get; set; }
    public required DateTime AssessmentPeriod { get; set; }
    public Dictionary<string, object> EnvironmentalFactors { get; set; } = new();
    public List<string> AssetInventory { get; set; } = new();
}

public class ThreatAssessmentResult
{
    public required string AssessmentId { get; set; }
    public required Dictionary<string, double> ThreatLevels { get; set; }
    public required List<string> IdentifiedThreats { get; set; }
    public required double OverallRiskScore { get; set; }
    public required DateTime AssessmentTimestamp { get; set; }
    public Dictionary<string, object> MitigationRecommendations { get; set; } = new();
    public List<string> CriticalVulnerabilities { get; set; } = new();
}

public class VulnerabilityPatchingStrategy
{
    public required string StrategyId { get; set; }
    public required List<string> VulnerabilityCategories { get; set; }
    public required Dictionary<string, int> PatchingPriorities { get; set; }
    public required string PatchingSchedule { get; set; }
    public required List<string> TestingRequirements { get; set; }
    public Dictionary<string, object> RollbackProcedures { get; set; } = new();
    public bool AutomatedPatchingEnabled { get; set; }
}

public class VulnerabilityPatchingResult
{
    public required bool PatchingSuccessful { get; set; }
    public required List<string> PatchedVulnerabilities { get; set; }
    public required Dictionary<string, string> PatchingStatus { get; set; }
    public required DateTime PatchingTimestamp { get; set; }
    public required string PatchingStrategy { get; set; }
    public Dictionary<string, object> PostPatchValidation { get; set; } = new();
    public List<string> FailedPatches { get; set; } = new();
}

public class SecurityAuditConfiguration
{
    public required string AuditId { get; set; }
    public required List<string> AuditScopes { get; set; }
    public required Dictionary<string, object> AuditParameters { get; set; }
    public required string AuditType { get; set; }
    public required TimeSpan AuditFrequency { get; set; }
    public required List<string> ComplianceRequirements { get; set; }
    public bool AutomatedAuditEnabled { get; set; }
}

public class SecurityAuditResult
{
    public required string AuditId { get; set; }
    public required Dictionary<string, object> AuditFindings { get; set; }
    public required List<string> SecurityGaps { get; set; }
    public required string ComplianceStatus { get; set; }
    public required DateTime AuditTimestamp { get; set; }
    public Dictionary<string, int> RiskRatings { get; set; } = new();
    public List<string> RecommendedActions { get; set; } = new();
}

public class PerformanceSecurityBalance
{
    public required string BalanceId { get; set; }
    public required Dictionary<string, double> SecurityMetrics { get; set; }
    public required Dictionary<string, double> PerformanceMetrics { get; set; }
    public required string OptimizationTarget { get; set; }
    public required double SecurityThreshold { get; set; }
    public required double PerformanceThreshold { get; set; }
    public Dictionary<string, object> TradeOffAnalysis { get; set; } = new();
}

public class AccessControlOptimization
{
    public required string OptimizationId { get; set; }
    public required Dictionary<string, object> AccessPolicies { get; set; }
    public required List<string> UserRoles { get; set; }
    public required Dictionary<string, List<string>> PermissionMatrix { get; set; }
    public required string AuthenticationMethod { get; set; }
    public bool MultiFactorEnabled { get; set; }
    public Dictionary<string, object> SessionManagement { get; set; } = new();
}

public class EncryptionOptimizationStrategy
{
    public required string StrategyId { get; set; }
    public required Dictionary<string, string> EncryptionAlgorithms { get; set; }
    public required List<string> DataClassifications { get; set; }
    public required Dictionary<string, object> KeyManagement { get; set; }
    public required string PerformanceTarget { get; set; }
    public bool HardwareAccelerationEnabled { get; set; }
    public Dictionary<string, int> EncryptionStrength { get; set; } = new();
}

public class ComplianceFrameworkMapping
{
    public required string MappingId { get; set; }
    public required List<string> FrameworkStandards { get; set; }
    public required Dictionary<string, List<string>> ControlMappings { get; set; }
    public required Dictionary<string, object> ComplianceRequirements { get; set; }
    public required string MappingVersion { get; set; }
    public DateTime LastUpdated { get; set; }
    public Dictionary<string, bool> FrameworkCompliance { get; set; } = new();
}

public class SecurityMetricsConfiguration
{
    public required string ConfigurationId { get; set; }
    public required List<string> TrackedMetrics { get; set; }
    public required Dictionary<string, double> MetricThresholds { get; set; }
    public required TimeSpan MonitoringInterval { get; set; }
    public required List<string> AlertingChannels { get; set; }
    public bool RealTimeAlerting { get; set; }
    public Dictionary<string, object> MetricWeights { get; set; } = new();
}
