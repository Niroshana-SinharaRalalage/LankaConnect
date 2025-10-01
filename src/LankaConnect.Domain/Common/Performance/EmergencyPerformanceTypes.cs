using System;
using System.Collections.Generic;

namespace LankaConnect.Domain.Common.Performance;

/// <summary>
/// Emergency performance types to resolve remaining CS0246 compilation errors
/// Final batch for 3-hour emergency architecture recovery
/// </summary>

public class ChurnRiskAnalysis
{
    public string AnalysisId { get; set; } = string.Empty;
    public Dictionary<string, double> ChurnProbabilities { get; set; } = new();
    public List<string> HighRiskCustomers { get; set; } = new();
    public Dictionary<string, object> RiskFactors { get; set; } = new();
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
}

public class FinancialConstraints
{
    public string ConstraintId { get; set; } = string.Empty;
    public decimal BudgetLimit { get; set; }
    public Dictionary<string, decimal> ResourceLimits { get; set; } = new();
    public List<string> ConstraintTypes { get; set; } = new();
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
}

public class PerformanceImpactThreshold
{
    public string ThresholdId { get; set; } = string.Empty;
    public Dictionary<string, double> PerformanceThresholds { get; set; } = new();
    public Dictionary<string, object> ImpactLevels { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTime ConfiguredAt { get; set; } = DateTime.UtcNow;
}

public class PerformanceIncident
{
    public string IncidentId { get; set; } = string.Empty;
    public string IncidentType { get; set; } = string.Empty;
    public Dictionary<string, object> PerformanceMetrics { get; set; } = new();
    public string Severity { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}

public class RevenueOptimizationObjective
{
    public string ObjectiveId { get; set; } = string.Empty;
    public decimal TargetRevenue { get; set; }
    public Dictionary<string, object> OptimizationParameters { get; set; } = new();
    public TimeSpan AchievementTimeframe { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class RevenueOptimizationRecommendations
{
    public string RecommendationId { get; set; } = string.Empty;
    public List<string> Recommendations { get; set; } = new();
    public Dictionary<string, decimal> EstimatedImpacts { get; set; } = new();
    public List<string> RequiredActions { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class RevenueProtectionPolicy
{
    public string PolicyId { get; set; } = string.Empty;
    public Dictionary<string, object> ProtectionRules { get; set; } = new();
    public List<string> CoveredServices { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
}

public class RevenueRecoveryMetrics
{
    public string MetricsId { get; set; } = string.Empty;
    public Dictionary<string, decimal> RecoveryMetrics { get; set; } = new();
    public TimeSpan RecoveryTime { get; set; }
    public decimal RecoveredAmount { get; set; }
    public DateTime MeasuredAt { get; set; } = DateTime.UtcNow;
}

public class MaintenanceRevenueProtection
{
    public string ProtectionId { get; set; } = string.Empty;
    public Dictionary<string, object> MaintenanceSchedule { get; set; } = new();
    public decimal ProtectedRevenue { get; set; }
    public List<string> CriticalServices { get; set; } = new();
    public DateTime ScheduledMaintenance { get; set; }
}

// Cross-Region and Multi-Region Types
public class CrossRegionIncidentResponseResult
{
    public string ResponseId { get; set; } = string.Empty;
    public bool ResponseSuccessful { get; set; }
    public Dictionary<string, object> ResponseActions { get; set; } = new();
    public List<string> ParticipatingRegions { get; set; } = new();
    public DateTime ResponseTimestamp { get; set; } = DateTime.UtcNow;
}

public class CrossRegionResponseProtocol
{
    public string ProtocolId { get; set; } = string.Empty;
    public Dictionary<string, List<string>> ResponseSteps { get; set; } = new();
    public List<string> InvolvedRegions { get; set; } = new();
    public TimeSpan MaxResponseTime { get; set; }
    public bool IsActive { get; set; } = true;
}

public class MultiRegionIncident
{
    public string IncidentId { get; set; } = string.Empty;
    public List<string> AffectedRegions { get; set; } = new();
    public Dictionary<string, object> IncidentDetails { get; set; } = new();
    public string Severity { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}

public class MultiJurisdictionCompliance
{
    public string ComplianceId { get; set; } = string.Empty;
    public Dictionary<string, List<string>> JurisdictionRequirements { get; set; } = new();
    public bool IsCompliant { get; set; }
    public List<string> ComplianceGaps { get; set; } = new();
    public DateTime AssessedAt { get; set; } = DateTime.UtcNow;
}

public class RegionalComplianceAlignmentResult
{
    public string ResultId { get; set; } = string.Empty;
    public Dictionary<string, bool> RegionalAlignment { get; set; } = new();
    public List<string> AlignmentIssues { get; set; } = new();
    public bool OverallAlignment { get; set; }
    public DateTime AlignedAt { get; set; } = DateTime.UtcNow;
}

// Key Management Types
public class KeyDistributionPolicy
{
    public string PolicyId { get; set; } = string.Empty;
    public Dictionary<string, object> DistributionRules { get; set; } = new();
    public List<string> AuthorizedRegions { get; set; } = new();
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
}

public class RegionalKeyManagementResult
{
    public string ResultId { get; set; } = string.Empty;
    public Dictionary<string, bool> KeyOperationResults { get; set; } = new();
    public List<string> SuccessfulOperations { get; set; } = new();
    public List<string> FailedOperations { get; set; } = new();
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}

public class RegionalKeyRotationSchedule
{
    public string ScheduleId { get; set; } = string.Empty;
    public Dictionary<string, DateTime> RegionalRotationTimes { get; set; } = new();
    public TimeSpan RotationFrequency { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime NextRotation { get; set; }
}