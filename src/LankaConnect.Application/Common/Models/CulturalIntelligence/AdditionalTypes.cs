namespace LankaConnect.Application.Common.Models.CulturalIntelligence;

public class SOC2Gap
{
    public required string GapId { get; set; }
    public required string GapCategory { get; set; }
    public required string Description { get; set; }
    public required string Severity { get; set; }
    public required DateTime IdentifiedDate { get; set; }
    public required string ResponsibleTeam { get; set; }
    public DateTime? TargetResolutionDate { get; set; }
    public string? RemediationPlan { get; set; }
    public Dictionary<string, object> ComplianceDetails { get; set; } = new();

    /// <summary>
    /// Constructor for creating SOC2 compliance gaps with category and description
    /// Used by DatabaseSecurityOptimizationEngine for automated gap detection
    /// </summary>
    public SOC2Gap(string gapCategory, string description)
    {
        GapId = Guid.NewGuid().ToString();
        GapCategory = gapCategory;
        Description = description;
        Severity = DetermineSeverityFromCategory(gapCategory);
        IdentifiedDate = DateTime.UtcNow;
        ResponsibleTeam = DetermineResponsibleTeam(gapCategory);
    }

    /// <summary>
    /// Parameterless constructor for object initialization syntax
    /// </summary>
    public SOC2Gap()
    {
        GapId = string.Empty;
        GapCategory = string.Empty;
        Description = string.Empty;
        Severity = string.Empty;
        IdentifiedDate = DateTime.UtcNow;
        ResponsibleTeam = string.Empty;
    }

    private static string DetermineSeverityFromCategory(string category)
    {
        return category.ToUpperInvariant() switch
        {
            "SECURITY" => "Critical",
            "AVAILABILITY" => "High",
            "PROCESSING_INTEGRITY" => "High",
            "CONFIDENTIALITY" => "Critical",
            "PRIVACY" => "Critical",
            _ => "Medium"
        };
    }

    private static string DetermineResponsibleTeam(string category)
    {
        return category.ToUpperInvariant() switch
        {
            "SECURITY" => "Security Team",
            "AVAILABILITY" => "Infrastructure Team",
            "PROCESSING_INTEGRITY" => "Development Team",
            "CONFIDENTIALITY" => "Security Team",
            "PRIVACY" => "Compliance Team",
            _ => "General Team"
        };
    }
}

public enum IncidentType
{
    SecurityBreach,
    DataCorruption,
    SystemFailure,
    PerformanceDegradation,
    ComplianceViolation,
    CulturalConflict,
    AccessViolation,
    BackupFailure,
    NetworkIssue,
    IntegrationFailure
}

public enum ResponseStatus
{
    Open,
    InProgress,
    Escalated,
    Resolved,
    Closed,
    Cancelled,
    UnderReview,
    AwaitingApproval,
    PendingVerification
}

public class CulturalIdentity
{
    public required string IdentityId { get; set; }
    public required string CulturalGroup { get; set; }
    public required List<string> CulturalAttributes { get; set; }
    public required string Region { get; set; }
    public required Dictionary<string, object> CulturalPreferences { get; set; }
    public required List<string> LanguagePreferences { get; set; }
    public DateTime IdentityCreated { get; set; }
    public Dictionary<string, string> CustomAttributes { get; set; } = new();
}

public class CulturalPermission
{
    public required string PermissionId { get; set; }
    public required string PermissionName { get; set; }
    public required List<string> AllowedActions { get; set; }
    public required Dictionary<string, object> CulturalConstraints { get; set; }
    public required string PermissionLevel { get; set; }
    public required List<string> ApplicableGroups { get; set; }
    public DateTime PermissionGranted { get; set; }
    public DateTime? PermissionExpiry { get; set; }
}

public class AccessAuditTrail
{
    public required string AuditId { get; set; }
    public required string UserId { get; set; }
    public required string ResourceAccessed { get; set; }
    public required string AccessType { get; set; }
    public required DateTime AccessTimestamp { get; set; }
    public required string AccessResult { get; set; }
    public required string IPAddress { get; set; }
    public string? UserAgent { get; set; }
    public Dictionary<string, object> AdditionalContext { get; set; } = new();
}

public class CulturalEventLoad
{
    public required string EventId { get; set; }
    public required string EventName { get; set; }
    public required DateTime EventDate { get; set; }
    public required string CulturalRegion { get; set; }
    public required int ExpectedParticipants { get; set; }
    public required double LoadMultiplier { get; set; }
    public required TimeSpan EventDuration { get; set; }
    public Dictionary<string, object> LoadCharacteristics { get; set; } = new();
}

public class LoadPredictionModel
{
    public required string ModelId { get; set; }
    public required string ModelName { get; set; }
    public required Dictionary<string, double> ModelParameters { get; set; }
    public required string AlgorithmType { get; set; }
    public required double AccuracyScore { get; set; }
    public required DateTime LastTrained { get; set; }
    public Dictionary<string, object> TrainingData { get; set; } = new();
    public List<string> FeatureImportance { get; set; } = new();
}

public class LoadDistributionStrategy
{
    public required string StrategyId { get; set; }
    public required string StrategyName { get; set; }
    public required List<string> DistributionRules { get; set; }
    public required Dictionary<string, double> ResourceWeights { get; set; }
    public required string LoadBalancingMethod { get; set; }
    public bool AdaptiveScalingEnabled { get; set; }
    public Dictionary<string, object> PerformanceTargets { get; set; } = new();
}

public class LoadDistributionResult
{
    public required bool DistributionSuccessful { get; set; }
    public required Dictionary<string, double> ResourceUtilization { get; set; }
    public required List<string> ActiveInstances { get; set; }
    public required DateTime DistributionTimestamp { get; set; }
    public required double LoadBalance { get; set; }
    public Dictionary<string, object> PerformanceMetrics { get; set; } = new();
    public string? DistributionWarnings { get; set; }
}

public class CulturalScalingParameters
{
    public required string ScalingId { get; set; }
    public required string CulturalContext { get; set; }
    public required Dictionary<string, double> ScalingTriggers { get; set; }
    public required string ScalingPolicy { get; set; }
    public required int MinInstances { get; set; }
    public required int MaxInstances { get; set; }
    public Dictionary<string, object> CulturalFactors { get; set; } = new();
    public TimeSpan ScalingCooldown { get; set; }
}

public class CulturalScalingResult
{
    public required bool ScalingTriggered { get; set; }
    public required string ScalingAction { get; set; }
    public required int PreviousInstanceCount { get; set; }
    public required int NewInstanceCount { get; set; }
    public required DateTime ScalingTimestamp { get; set; }
    public required string ScalingReason { get; set; }
    public Dictionary<string, object> ScalingMetrics { get; set; } = new();
}

public class RegionalLoadProfile
{
    public required string ProfileId { get; set; }
    public required string Region { get; set; }
    public required Dictionary<string, double> LoadPatterns { get; set; }
    public required List<string> PeakHours { get; set; }
    public required Dictionary<string, object> CulturalFactors { get; set; }
    public required TimeSpan TimeZoneOffset { get; set; }
    public Dictionary<string, double> SeasonalVariations { get; set; } = new();
}

public class GlobalLoadCoordination
{
    public required string CoordinationId { get; set; }
    public required List<string> ParticipatingRegions { get; set; }
    public required Dictionary<string, double> RegionCapacities { get; set; }
    public required string CoordinationStrategy { get; set; }
    public required DateTime CoordinationTimestamp { get; set; }
    public Dictionary<string, object> LoadRedistribution { get; set; } = new();
    public bool EmergencyModeActive { get; set; }
}
