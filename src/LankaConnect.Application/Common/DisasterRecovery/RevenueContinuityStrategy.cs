using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.DisasterRecovery;

/// <summary>
/// Revenue continuity strategy for maintaining cultural event and diaspora community revenue during disasters
/// Defines comprehensive strategy for ensuring business continuity and revenue protection
/// </summary>
public class RevenueContinuityStrategy
{
    public required string StrategyId { get; set; }
    public required string StrategyName { get; set; }
    public required RevenueContinuityStrategyType StrategyType { get; set; }
    public required RevenueContinuityScope Scope { get; set; }
    public required List<RevenueContinuityObjective> Objectives { get; set; }
    public required Dictionary<string, RevenueContinuityPlan> ContinuityPlans { get; set; }
    public required TimeSpan RecoveryTimeObjective { get; set; }
    public required decimal RecoveryPointObjective { get; set; }
    public required List<string> CriticalRevenueStreams { get; set; }
    public required Dictionary<string, RevenueContinuityAction> Actions { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public List<CulturalEventContinuityPlan> CulturalEventPlans { get; set; } = new();
    public Dictionary<string, DiasporaRegionContinuityConfig> RegionConfigurations { get; set; } = new();

    public RevenueContinuityStrategy()
    {
        Objectives = new List<RevenueContinuityObjective>();
        ContinuityPlans = new Dictionary<string, RevenueContinuityPlan>();
        CriticalRevenueStreams = new List<string>();
        Actions = new Dictionary<string, RevenueContinuityAction>();
        CulturalEventPlans = new List<CulturalEventContinuityPlan>();
        RegionConfigurations = new Dictionary<string, DiasporaRegionContinuityConfig>();
    }

    public bool IsCriticalStream(string streamId)
    {
        return CriticalRevenueStreams.Contains(streamId);
    }

    public RevenueContinuityPlan? GetPlanForComponent(string componentId)
    {
        return ContinuityPlans.TryGetValue(componentId, out var plan) ? plan : null;
    }
}

/// <summary>
/// Revenue continuity strategy types
/// </summary>
public enum RevenueContinuityStrategyType
{
    Preventive = 1,
    Reactive = 2,
    Proactive = 3,
    Adaptive = 4,
    Comprehensive = 5
}

/// <summary>
/// Revenue continuity scope
/// </summary>
public enum RevenueContinuityScope
{
    CulturalEvents = 1,
    DiasporaServices = 2,
    PaymentSystems = 3,
    AdvertisementPlatform = 4,
    SubscriptionServices = 5,
    AllSystems = 6
}

/// <summary>
/// Revenue continuity objective
/// </summary>
public class RevenueContinuityObjective
{
    public required string ObjectiveId { get; set; }
    public required RevenueContinuityObjectiveType Type { get; set; }
    public required string Description { get; set; }
    public required RevenueContinuityObjectivePriority Priority { get; set; }
    public required decimal TargetValue { get; set; }
    public required TimeSpan TimeFrame { get; set; }
    public required List<string> SuccessMetrics { get; set; }
    public RevenueContinuityObjectiveStatus Status { get; set; } = RevenueContinuityObjectiveStatus.Active;
    public string? OwnerRole { get; set; }
    public Dictionary<string, object> ObjectiveMetadata { get; set; } = new();
}

/// <summary>
/// Revenue continuity objective types
/// </summary>
public enum RevenueContinuityObjectiveType
{
    RevenueProtection = 1,
    ServiceAvailability = 2,
    RecoveryTime = 3,
    DataIntegrity = 4,
    CustomerRetention = 5,
    BusinessProcess = 6
}

/// <summary>
/// Revenue continuity objective priority
/// </summary>
public enum RevenueContinuityObjectivePriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4,
    Essential = 5
}

/// <summary>
/// Revenue continuity objective status
/// </summary>
public enum RevenueContinuityObjectiveStatus
{
    Active = 1,
    Achieved = 2,
    InProgress = 3,
    AtRisk = 4,
    Failed = 5,
    Suspended = 6
}

/// <summary>
/// Revenue continuity plan
/// </summary>
public class RevenueContinuityPlan
{
    public required string PlanId { get; set; }
    public required string PlanName { get; set; }
    public required RevenueContinuityPlanType Type { get; set; }
    public required List<string> TriggerConditions { get; set; }
    public required List<RevenueContinuityStep> Steps { get; set; }
    public required Dictionary<string, RevenueContinuityResource> RequiredResources { get; set; }
    public required TimeSpan EstimatedExecutionTime { get; set; }
    public RevenueContinuityPlanStatus Status { get; set; } = RevenueContinuityPlanStatus.Ready;
    public string? ResponsibleTeam { get; set; }
    public List<string> Dependencies { get; set; } = new();
    public Dictionary<string, object> PlanParameters { get; set; } = new();
}

/// <summary>
/// Revenue continuity plan types
/// </summary>
public enum RevenueContinuityPlanType
{
    Failover = 1,
    Backup = 2,
    AlternativeChannel = 3,
    EmergencyProcedure = 4,
    RecoveryProtocol = 5,
    ContingencyMeasure = 6
}

/// <summary>
/// Revenue continuity plan status
/// </summary>
public enum RevenueContinuityPlanStatus
{
    Ready = 1,
    Executing = 2,
    Completed = 3,
    Failed = 4,
    Suspended = 5,
    Testing = 6
}

/// <summary>
/// Revenue continuity step
/// </summary>
public class RevenueContinuityStep
{
    public required string StepId { get; set; }
    public required string Description { get; set; }
    public required RevenueContinuityStepType Type { get; set; }
    public required int ExecutionOrder { get; set; }
    public required TimeSpan EstimatedDuration { get; set; }
    public required List<string> Prerequisites { get; set; }
    public RevenueContinuityStepStatus Status { get; set; } = RevenueContinuityStepStatus.Pending;
    public string? ResponsibleRole { get; set; }
    public List<string> ValidationCriteria { get; set; } = new();
    public Dictionary<string, object> StepParameters { get; set; } = new();
}

/// <summary>
/// Revenue continuity step types
/// </summary>
public enum RevenueContinuityStepType
{
    Assessment = 1,
    Activation = 2,
    Execution = 3,
    Validation = 4,
    Notification = 5,
    Recovery = 6
}

/// <summary>
/// Revenue continuity step status
/// </summary>
public enum RevenueContinuityStepStatus
{
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    Failed = 4,
    Skipped = 5,
    Blocked = 6
}

/// <summary>
/// Revenue continuity action
/// </summary>
public class RevenueContinuityAction
{
    public required string ActionId { get; set; }
    public required RevenueContinuityActionType Type { get; set; }
    public required string Description { get; set; }
    public required RevenueContinuityActionPriority Priority { get; set; }
    public required List<string> TriggerEvents { get; set; }
    public required TimeSpan ExecutionTimeWindow { get; set; }
    public bool IsAutomated { get; set; }
    public string? AutomationScript { get; set; }
    public List<string> ManualSteps { get; set; } = new();
    public Dictionary<string, object> ActionConfiguration { get; set; } = new();
}

/// <summary>
/// Revenue continuity action types
/// </summary>
public enum RevenueContinuityActionType
{
    SystemFailover = 1,
    ServiceRedirect = 2,
    DataBackup = 3,
    AlertNotification = 4,
    ResourceReallocation = 5,
    CustomerCommunication = 6
}

/// <summary>
/// Revenue continuity action priority
/// </summary>
public enum RevenueContinuityActionPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4,
    Immediate = 5
}

/// <summary>
/// Revenue continuity resource
/// </summary>
public class RevenueContinuityResource
{
    public required string ResourceId { get; set; }
    public required RevenueContinuityResourceType Type { get; set; }
    public required string Description { get; set; }
    public required RevenueContinuityResourceAvailability Availability { get; set; }
    public required decimal Cost { get; set; }
    public string? Location { get; set; }
    public List<string> Capabilities { get; set; } = new();
    public Dictionary<string, object> ResourceMetadata { get; set; } = new();
}

/// <summary>
/// Revenue continuity resource types
/// </summary>
public enum RevenueContinuityResourceType
{
    Infrastructure = 1,
    Personnel = 2,
    Technology = 3,
    Financial = 4,
    Partner = 5,
    External = 6
}

/// <summary>
/// Revenue continuity resource availability
/// </summary>
public enum RevenueContinuityResourceAvailability
{
    Available = 1,
    Limited = 2,
    Reserved = 3,
    Unavailable = 4,
    OnDemand = 5
}

/// <summary>
/// Cultural event continuity plan
/// </summary>
public class CulturalEventContinuityPlan
{
    public required string EventId { get; set; }
    public required CulturalEventType EventType { get; set; }
    public required List<RevenueStream> CriticalStreams { get; set; }
    public required Dictionary<string, RevenueContinuityAction> EventSpecificActions { get; set; }
    public required TimeSpan EventDuration { get; set; }
    public string? DiasporaRegion { get; set; }
    public List<string> AlternativeVenues { get; set; } = new();
    public Dictionary<string, object> CulturalConsiderations { get; set; } = new();
}

/// <summary>
/// Diaspora region continuity configuration
/// </summary>
public class DiasporaRegionContinuityConfig
{
    public required string RegionId { get; set; }
    public required string RegionName { get; set; }
    public required List<string> PreferredLanguages { get; set; }
    public required TimeSpan RegionTimeZone { get; set; }
    public required Dictionary<string, RevenueContinuityPlan> RegionSpecificPlans { get; set; }
    public List<string> CulturalConsiderations { get; set; } = new();
    public Dictionary<string, object> RegionMetadata { get; set; } = new();
}