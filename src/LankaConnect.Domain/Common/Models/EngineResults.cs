namespace LankaConnect.Domain.Common.Models;

/// <summary>
/// Result types for database performance monitoring operations
/// </summary>
public record AlertSuppressionResult(
    bool IsSuccessful,
    string AlertId,
    TimeSpan SuppressionDuration,
    string Reason
);

public record AlertEffectivenessMetrics(
    double AlertAccuracy,
    double FalsePositiveRate,
    double ResponseTime,
    int TotalAlerts
);

public record AlertCorrelationResult(
    string CorrelationId,
    List<string> CorrelatedAlerts,
    string RootCause,
    double ConfidenceLevel
);

public record SLAPerformanceReport(
    string ServiceName,
    double UptimePercentage,
    TimeSpan AverageResponseTime,
    int ViolationsCount,
    DateTime ReportPeriod
);

public record SLABreachAnalysis(
    string ServiceName,
    DateTime BreachTime,
    TimeSpan BreachDuration,
    string Impact,
    List<string> AffectedComponents
);

public record SLACreditCalculation(
    decimal CreditAmount,
    string Reason,
    DateTime CalculationDate,
    string CustomerId
);

public record SLARiskAssessment(
    string ServiceName,
    double RiskScore,
    List<string> RiskFactors,
    DateTime AssessmentDate
);

public record CustomerSLAReport(
    string CustomerId,
    List<SLAPerformanceReport> ServiceReports,
    decimal TotalCredits,
    DateTime ReportDate
);

public record SLAImprovementTracker(
    string ServiceName,
    List<string> ImprovementInitiatives,
    double TargetImprovement,
    DateTime StartDate
);

/// <summary>
/// Result types for backup disaster recovery operations
/// </summary>
public record BusinessContinuityActivationResult(
    bool IsActivated,
    string PlanId,
    DateTime ActivationTime,
    List<string> ActivatedSystems
);

public record ProcessContinuityResult(
    string ProcessId,
    bool IsContinuous,
    TimeSpan RecoveryTime,
    List<string> Dependencies
);

public record StakeholderCommunicationResult(
    List<string> NotifiedStakeholders,
    string CommunicationMethod,
    DateTime NotificationTime,
    bool AllNotified
);

public record ServiceLevelMaintenanceResult(
    string ServiceName,
    bool MaintenanceSuccessful,
    TimeSpan MaintenanceDuration,
    List<string> AffectedComponents
);

public record ServiceDegradationResult(
    string ServiceName,
    string DegradationLevel,
    List<string> AffectedFeatures,
    TimeSpan EstimatedRecoveryTime
);

public record ContinuityTestResult(
    string TestId,
    bool TestPassed,
    List<string> TestedSystems,
    DateTime TestDate
);

public record ComplianceMaintenanceResult(
    List<string> ComplianceRequirements,
    bool AllRequirementsMet,
    List<string> NonCompliantAreas,
    DateTime ComplianceDate
);

/// <summary>
/// Result types for database security optimization operations
/// </summary>
public record IncidentPatternAnalysisResult(
    string PatternId,
    List<string> DetectedPatterns,
    double ConfidenceLevel,
    DateTime AnalysisDate
);

public record CulturalABACResult(
    bool AccessGranted,
    string UserId,
    string ResourceId,
    List<string> AppliedPolicies
);

/// <summary>
/// Threshold and configuration types
/// </summary>
public record AlertEffectivenessThreshold(
    double MinAccuracy,
    double MaxFalsePositiveRate,
    TimeSpan MaxResponseTime
);

public record CorrelationConfiguration(
    TimeSpan CorrelationWindow,
    double MinConfidenceLevel,
    int MaxCorrelatedAlerts
);

public record SLAThresholdAdjustment(
    string ServiceName,
    string MetricName,
    double OldThreshold,
    double NewThreshold,
    string Reason
);

public record PatternAnalysisConfiguration(
    TimeSpan AnalysisWindow,
    int MinOccurrences,
    double ConfidenceThreshold
);

/// <summary>
/// Enumeration types
/// </summary>
public enum SLABreachSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum SLAReportFormat
{
    Json,
    Xml,
    Csv,
    Pdf
}

public enum RiskAssessmentTimeframe
{
    Daily,
    Weekly,
    Monthly,
    Quarterly
}

public enum ThresholdAdjustmentReason
{
    PerformanceImprovement,
    BusinessRequirement,
    TechnicalConstraint,
    ComplianceRequirement
}

public enum ServiceDegradationLevel
{
    None,
    Minimal,
    Moderate,
    Significant,
    Severe
}

public enum CreditCalculationPolicy
{
    Automatic,
    Manual,
    Hybrid
}

public enum CoordinationStrategy
{
    Active,
    Passive,
    Hybrid
}

/// <summary>
/// Business domain types
/// </summary>
public record CulturalLoadImpact(
    string EventType,
    double LoadMultiplier,
    List<string> AffectedRegions,
    DateTime ImpactDate
);

public record ScalingAction(
    string ActionType,
    int TargetCapacity,
    string Reason,
    DateTime ExecutionTime
);

public record CriticalBusinessProcess(
    string ProcessId,
    string ProcessName,
    int Priority,
    List<string> Dependencies
);

public record ContinuityStrategy(
    string StrategyName,
    List<string> Steps,
    TimeSpan MaxRecoveryTime,
    List<string> RequiredResources
);

public record BusinessContinuityEvent(
    string EventId,
    string EventType,
    DateTime OccurredAt,
    string Impact
);

public record StakeholderGroup(
    string GroupId,
    string GroupName,
    List<string> ContactMethods,
    int Priority
);

public record GracefulDegradationStrategy(
    string StrategyName,
    List<string> DisabledFeatures,
    List<string> EssentialFeatures,
    TimeSpan MaxDegradationTime
);

public record BusinessContinuityTestPlan(
    string PlanId,
    string PlanName,
    List<string> TestScenarios,
    DateTime ScheduledDate
);

public record RegulatoryRequirement(
    string RequirementId,
    string RequirementName,
    string RegulatoryBody,
    DateTime EffectiveDate
);

public record ComplianceMaintenanceStrategy(
    string StrategyName,
    List<string> ComplianceActivities,
    TimeSpan ReviewCycle,
    List<string> ResponsibleTeams
);

public record ContinuityCoordinationStrategy(
    string StrategyName,
    List<string> CoordinationMechanisms,
    List<string> StakeholderGroups,
    TimeSpan CoordinationFrequency
);

/// <summary>
/// Security and cultural intelligence types
/// </summary>
public record RegionalComplianceRequirements(
    string Region,
    List<string> ComplianceStandards,
    Dictionary<string, object> Requirements
);

public record RegionalSecurityStatus(
    string Region,
    bool IsCompliant,
    List<string> SecurityMeasures,
    DateTime LastAudit
);

public record CrossRegionSecurityMetrics(
    List<RegionalSecurityStatus> RegionalStatuses,
    double OverallComplianceScore,
    DateTime MetricsDate
);

public record CulturalRoleDefinition(
    string RoleId,
    string RoleName,
    List<string> Permissions,
    List<string> CulturalContexts
);

public record CulturalResourceAccess(
    string ResourceId,
    string UserId,
    List<string> AllowedActions,
    Dictionary<string, object> CulturalAttributes
);

public record AttributeBasedPolicy(
    string PolicyId,
    string PolicyName,
    Dictionary<string, object> Attributes,
    List<string> Rules
);

public record CulturalSecurityContext(
    string UserId,
    string CulturalBackground,
    List<string> TrustedRegions,
    Dictionary<string, object> SecurityAttributes
);

public record SLAImprovementInitiative(
    string InitiativeId,
    string InitiativeName,
    double TargetImprovement,
    DateTime StartDate,
    List<string> RequiredActions
);

public record SLABreach(
    string ServiceName,
    DateTime BreachTime,
    string MetricName,
    double ActualValue,
    double ThresholdValue
);