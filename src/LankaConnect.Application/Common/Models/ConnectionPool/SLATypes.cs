namespace LankaConnect.Application.Common.Models.ConnectionPool;

public class SLABreachPreventionConfiguration
{
    public required string ConfigurationId { get; set; }
    public required Dictionary<string, double> ThresholdLimits { get; set; }
    public required TimeSpan MonitoringInterval { get; set; }
    public required List<string> AlertChannels { get; set; }
    public required string PreventionStrategy { get; set; }
    public bool AutoScalingEnabled { get; set; }
    public Dictionary<string, object> MetricWeights { get; set; } = new();
    public int MaxPreventionAttempts { get; set; } = 3;
}

public class SLABreachPreventionResult
{
    public required bool PreventionSuccessful { get; set; }
    public required string BreachType { get; set; }
    public required DateTime PreventionTimestamp { get; set; }
    public required List<string> ActionsPerformed { get; set; }
    public double RiskReductionPercentage { get; set; }
    public string? FailureReason { get; set; }
    public Dictionary<string, object> Metrics { get; set; } = new();
    public TimeSpan ResponseTime { get; set; }
}

public class CulturalDisruptionContext
{
    public required string EventId { get; set; }
    public required string CulturalRegion { get; set; }
    public required string DisruptionType { get; set; }
    public required DateTime EventTimestamp { get; set; }
    public required double ImpactSeverity { get; set; }
    public required List<string> AffectedCommunities { get; set; }
    public Dictionary<string, object> CulturalMetadata { get; set; } = new();
    public TimeSpan ExpectedDuration { get; set; }
}

public class SLACreditCalculationParameters
{
    public required string ServiceType { get; set; }
    public required TimeSpan DowntimeDuration { get; set; }
    public required double ServiceLevelAgreement { get; set; }
    public required string CustomerTier { get; set; }
    public required Dictionary<string, double> ImpactFactors { get; set; }
    public DateTime IncidentStartTime { get; set; }
    public DateTime IncidentEndTime { get; set; }
    public string? BusinessContext { get; set; }
}

public class SLACreditCalculationResult
{
    public required double CreditAmount { get; set; }
    public required string Currency { get; set; }
    public required string CalculationBasis { get; set; }
    public required DateTime CalculationTimestamp { get; set; }
    public required List<string> ApplicableTerms { get; set; }
    public bool AutoApproved { get; set; }
    public Dictionary<string, object> CalculationDetails { get; set; } = new();
    public string? ApprovalRequired { get; set; }
}

public class EnterpriseSLAConfiguration
{
    public required string EnterpriseId { get; set; }
    public required string SLALevel { get; set; }
    public required Dictionary<string, double> PerformanceTargets { get; set; }
    public required List<string> CoveredServices { get; set; }
    public required string EscalationProcedure { get; set; }
    public DateTime EffectiveDate { get; set; }
    public DateTime ExpirationDate { get; set; }
    public Dictionary<string, object> CustomTerms { get; set; } = new();
}

public class EnterpriseSLAValidationResult
{
    public required bool IsValid { get; set; }
    public required string ValidationStatus { get; set; }
    public required List<string> ValidationErrors { get; set; }
    public required DateTime ValidationTimestamp { get; set; }
    public required string ValidatedBy { get; set; }
    public Dictionary<string, object> ComplianceMetrics { get; set; } = new();
    public string? RecommendedActions { get; set; }
}

public class ProactiveSLAConfiguration
{
    public required string ConfigurationName { get; set; }
    public required Dictionary<string, double> PredictiveThresholds { get; set; }
    public required List<string> MonitoredMetrics { get; set; }
    public required string ResponseStrategy { get; set; }
    public required TimeSpan PredictionWindow { get; set; }
    public bool MachineLearningEnabled { get; set; }
    public Dictionary<string, object> ModelParameters { get; set; } = new();
    public int ConfidenceThreshold { get; set; } = 85;
}

public class ProactiveSLAManagementResult
{
    public required bool PredictionTriggered { get; set; }
    public required string PredictedIssue { get; set; }
    public required double ConfidenceScore { get; set; }
    public required List<string> PreventiveActions { get; set; }
    public required DateTime PredictionTimestamp { get; set; }
    public TimeSpan TimeToImpact { get; set; }
    public Dictionary<string, object> RiskFactors { get; set; } = new();
    public string? MitigationStatus { get; set; }
}

public class CulturalScalingDecision
{
    public required string DecisionId { get; set; }
    public required string CulturalContext { get; set; }
    public required string ScalingDirection { get; set; }
    public required int ScalingMagnitude { get; set; }
    public required DateTime DecisionTimestamp { get; set; }
    public required Dictionary<string, double> CulturalFactors { get; set; }
    public string Justification { get; set; } = string.Empty;
    public double ConfidenceLevel { get; set; }
}

public class SLAImpactAnalysisParameters
{
    public required string AnalysisScope { get; set; }
    public required List<string> ImpactedServices { get; set; }
    public required Dictionary<string, double> BaselineMetrics { get; set; }
    public required DateTime AnalysisStartTime { get; set; }
    public required DateTime AnalysisEndTime { get; set; }
    public string? BusinessUnit { get; set; }
    public Dictionary<string, object> ContextualData { get; set; } = new();
}

public class SLAImpactAnalysis
{
    public required string AnalysisId { get; set; }
    public required Dictionary<string, double> ImpactMetrics { get; set; }
    public required List<string> AffectedSLAs { get; set; }
    public required double OverallImpactScore { get; set; }
    public required DateTime AnalysisTimestamp { get; set; }
    public Dictionary<string, object> DetailedFindings { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
}

public class SLAOptimizationRequest
{
    public required string RequestId { get; set; }
    public required string OptimizationTarget { get; set; }
    public required Dictionary<string, double> CurrentPerformance { get; set; }
    public required Dictionary<string, double> TargetPerformance { get; set; }
    public required List<string> OptimizationScope { get; set; }
    public DateTime RequestTimestamp { get; set; }
    public Dictionary<string, object> Constraints { get; set; } = new();
}

public class SLAOptimizationResult
{
    public required bool OptimizationSuccessful { get; set; }
    public required List<string> OptimizationActions { get; set; }
    public required Dictionary<string, double> AchievedImprovements { get; set; }
    public required DateTime OptimizationTimestamp { get; set; }
    public required string OptimizationStrategy { get; set; }
    public Dictionary<string, object> PerformanceGains { get; set; } = new();
    public string? LimitingFactors { get; set; }
}
