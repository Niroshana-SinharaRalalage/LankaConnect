using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.Database;

namespace LankaConnect.Infrastructure.Common.Models;

/// <summary>
/// Infrastructure-specific disaster recovery models
/// TDD GREEN Phase: Foundation types for Backup and Disaster Recovery
/// </summary>
public class SacredEventRecoveryResult
{
    public string RecoveryId { get; set; } = Guid.NewGuid().ToString();
    public bool IsSuccessful { get; set; }
    public DateTime RecoveryStartTime { get; set; }
    public DateTime RecoveryEndTime { get; set; }
    public TimeSpan RecoveryDuration => RecoveryEndTime - RecoveryStartTime;
    public List<RecoveryStep> ExecutedSteps { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public CulturalDataIntegrityStatus DataIntegrity { get; set; }
    public Dictionary<string, object> Metrics { get; set; } = new();
}

/// <summary>
/// Priority-based recovery plan for cultural events
/// </summary>
public class PriorityRecoveryPlan
{
    public string PlanId { get; set; } = Guid.NewGuid().ToString();
    public string PlanName { get; set; } = string.Empty;
    public CulturalEventPriority Priority { get; set; }
    public List<RecoveryStep> Steps { get; set; } = new();
    public TimeSpan EstimatedDuration { get; set; }
    public List<string> Dependencies { get; set; } = new();
    public Dictionary<string, object> Configuration { get; set; } = new();
}

/// <summary>
/// Multi-cultural recovery result aggregation
/// </summary>
public class MultiCulturalRecoveryResult
{
    public string AggregateRecoveryId { get; set; } = Guid.NewGuid().ToString();
    public Dictionary<string, SacredEventRecoveryResult> CulturalRecoveries { get; set; } = new();
    public bool AllRecoveriesSuccessful => CulturalRecoveries.Values.All(r => r.IsSuccessful);
    public DateTime OverallStartTime { get; set; }
    public DateTime OverallEndTime { get; set; }
    public List<string> FailedCultures { get; set; } = new();
    public CulturalCoordinationStatus CoordinationStatus { get; set; }
}

/// <summary>
/// Individual recovery step execution details
/// </summary>
public class RecoveryStep
{
    public string StepId { get; set; } = Guid.NewGuid().ToString();
    public string StepName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RecoveryStepStatus Status { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;
    public List<string> Errors { get; set; } = new();
    public Dictionary<string, object> Results { get; set; } = new();
}

/// <summary>
/// Backup frequency configuration
/// </summary>
public class BackupFrequency
{
    public string FrequencyId { get; set; } = Guid.NewGuid().ToString();
    public BackupFrequencyType Type { get; set; }
    public TimeSpan Interval { get; set; }
    public List<DayOfWeek> ScheduledDays { get; set; } = new();
    public TimeOnly ScheduledTime { get; set; }
    public CulturalEventAwareness CulturalAwareness { get; set; }
}

/// <summary>
/// Data retention policy for cultural intelligence
/// </summary>
public class DataRetentionPolicy
{
    public string PolicyId { get; set; } = Guid.NewGuid().ToString();
    public string PolicyName { get; set; } = string.Empty;
    public TimeSpan RetentionPeriod { get; set; }
    public DataClassification DataClassification { get; set; }
    public List<RetentionRule> Rules { get; set; } = new();
    public bool RequiresComplianceApproval { get; set; }
    public Dictionary<string, object> ComplianceMetadata { get; set; } = new();
}

/// <summary>
/// Data integrity validation result for disaster recovery
/// </summary>
public class DataIntegrityValidationResult
{
    public string ValidationId { get; set; } = Guid.NewGuid().ToString();
    public bool IsValid { get; set; }
    public DateTime ValidationTime { get; set; }
    public List<IntegrityIssue> Issues { get; set; } = new();
    public Dictionary<string, ValidationCheckResult> CheckResults { get; set; } = new();
    public DataIntegrityScore IntegrityScore { get; set; } = new();
}

/// <summary>
/// Individual retention rule
/// </summary>
public class RetentionRule
{
    public string RuleId { get; set; } = Guid.NewGuid().ToString();
    public string Condition { get; set; } = string.Empty;
    public TimeSpan RetentionPeriod { get; set; }
    public RetentionAction Action { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Data integrity issue details
/// </summary>
public class IntegrityIssue
{
    public string IssueId { get; set; } = Guid.NewGuid().ToString();
    public IntegrityIssueSeverity Severity { get; set; }
    public string Description { get; set; } = string.Empty;
    public string AffectedComponent { get; set; } = string.Empty;
    public Dictionary<string, object> Details { get; set; } = new();
}

/// <summary>
/// Validation check result
/// </summary>
public class ValidationCheckResult
{
    public bool Passed { get; set; }
    public string CheckName { get; set; } = string.Empty;
    public List<string> Messages { get; set; } = new();
    public Dictionary<string, object> Metrics { get; set; } = new();
}

/// <summary>
/// Data integrity scoring
/// </summary>
public class DataIntegrityScore
{
    public double OverallScore { get; set; }
    public Dictionary<string, double> ComponentScores { get; set; } = new();
    public IntegrityLevel Level { get; set; }
}

// Enumerations for Infrastructure models
public enum CulturalDataIntegrityStatus
{
    Intact = 0,
    PartialLoss = 1,
    SignificantLoss = 2,
    CriticalLoss = 3
}


public enum CulturalCoordinationStatus
{
    Coordinated = 0,
    PartialCoordination = 1,
    Uncoordinated = 2,
    Failed = 3
}

public enum RecoveryStepStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Failed = 3,
    Skipped = 4
}

public enum BackupFrequencyType
{
    Continuous = 0,
    Hourly = 1,
    Daily = 2,
    Weekly = 3,
    Monthly = 4,
    EventBased = 5
}

public enum CulturalEventAwareness
{
    None = 0,
    Basic = 1,
    Enhanced = 2,
    Sacred = 3
}

public enum DataClassification
{
    Public = 0,
    Internal = 1,
    Confidential = 2,
    Sacred = 3
}

public enum RetentionAction
{
    Archive = 0,
    Delete = 1,
    Anonymize = 2,
    Preserve = 3
}

public enum IntegrityIssueSeverity
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public enum IntegrityLevel
{
    Excellent = 0,
    Good = 1,
    Fair = 2,
    Poor = 3,
    Critical = 4
}