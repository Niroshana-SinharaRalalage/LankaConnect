using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.DisasterRecovery;

/// <summary>
/// Billing continuity configuration for cultural events and diaspora community billing systems
/// Ensures uninterrupted billing operations during disaster recovery scenarios
/// </summary>
public class BillingContinuityConfiguration
{
    public required string ConfigurationId { get; set; }
    public required string ConfigurationName { get; set; }
    public required BillingContinuityScope Scope { get; set; }
    public required List<BillingSystemConfiguration> BillingSystems { get; set; }
    public required Dictionary<string, BillingFailoverRule> FailoverRules { get; set; }
    public required TimeSpan BillingContinuityThreshold { get; set; }
    public required BillingContinuityStrategy Strategy { get; set; }
    public required List<string> CriticalBillingStreams { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? Description { get; set; }
    public List<CulturalBillingConfiguration> CulturalBillingConfigs { get; set; } = new();
    public Dictionary<string, DiasporaBillingPolicy> DiasporaBillingPolicies { get; set; } = new();
    public Dictionary<string, object> ConfigurationParameters { get; set; } = new();

    public BillingContinuityConfiguration()
    {
        BillingSystems = new List<BillingSystemConfiguration>();
        FailoverRules = new Dictionary<string, BillingFailoverRule>();
        CriticalBillingStreams = new List<string>();
        CulturalBillingConfigs = new List<CulturalBillingConfiguration>();
        DiasporaBillingPolicies = new Dictionary<string, DiasporaBillingPolicy>();
        ConfigurationParameters = new Dictionary<string, object>();
    }

    public bool IsCriticalBillingStream(string streamId)
    {
        return CriticalBillingStreams.Contains(streamId);
    }

    public BillingSystemConfiguration? GetPrimaryBillingSystem()
    {
        return BillingSystems.FirstOrDefault(s => s.Priority == BillingSystemPriority.Primary && s.IsEnabled);
    }
}

/// <summary>
/// Billing continuity scope
/// </summary>
public enum BillingContinuityScope
{
    CulturalEvents = 1,
    DiasporaServices = 2,
    SubscriptionBilling = 3,
    TransactionFees = 4,
    AdvertisementBilling = 5,
    AllBillingSystems = 6
}

/// <summary>
/// Billing continuity strategy
/// </summary>
public enum BillingContinuityStrategy
{
    ActivePassive = 1,
    ActiveActive = 2,
    LoadBalanced = 3,
    GeographicallyDistributed = 4,
    CloudFailover = 5,
    HybridApproach = 6
}

/// <summary>
/// Billing system configuration
/// </summary>
public class BillingSystemConfiguration
{
    public required string SystemId { get; set; }
    public required string SystemName { get; set; }
    public required BillingSystemType Type { get; set; }
    public required BillingSystemPriority Priority { get; set; }
    public required List<string> SupportedPaymentMethods { get; set; }
    public required BillingSystemCapacity Capacity { get; set; }
    public required string EndpointUrl { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? ApiKey { get; set; }
    public List<string> SupportedCurrencies { get; set; } = new();
    public Dictionary<string, object> SystemConfiguration { get; set; } = new();
    public List<BillingSystemConstraint> Constraints { get; set; } = new();
}

/// <summary>
/// Billing system types
/// </summary>
public enum BillingSystemType
{
    Primary = 1,
    Backup = 2,
    Emergency = 3,
    Partner = 4,
    Cloud = 5,
    Legacy = 6
}

/// <summary>
/// Billing system priority
/// </summary>
public enum BillingSystemPriority
{
    Primary = 1,
    Secondary = 2,
    Tertiary = 3,
    Emergency = 4,
    Deprecated = 5
}

/// <summary>
/// Billing system capacity
/// </summary>
public class BillingSystemCapacity
{
    public required decimal MaxTransactionAmount { get; set; }
    public required int MaxTransactionsPerMinute { get; set; }
    public required decimal MaxDailyVolume { get; set; }
    public required int MaxConcurrentUsers { get; set; }
    public BillingSystemCapacityStatus Status { get; set; } = BillingSystemCapacityStatus.Available;
    public double CurrentUtilization { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Billing system capacity status
/// </summary>
public enum BillingSystemCapacityStatus
{
    Available = 1,
    Limited = 2,
    AtCapacity = 3,
    Overloaded = 4,
    Unavailable = 5
}

/// <summary>
/// Billing failover rule
/// </summary>
public class BillingFailoverRule
{
    public required string RuleId { get; set; }
    public required string PrimarySystemId { get; set; }
    public required List<string> FailoverSystemIds { get; set; }
    public required BillingFailoverTrigger TriggerCondition { get; set; }
    public required TimeSpan EvaluationWindow { get; set; }
    public required int FailureThreshold { get; set; }
    public bool IsEnabled { get; set; } = true;
    public BillingFailoverStrategy Strategy { get; set; } = BillingFailoverStrategy.Sequential;
    public Dictionary<string, object> RuleParameters { get; set; } = new();
}

/// <summary>
/// Billing failover triggers
/// </summary>
public enum BillingFailoverTrigger
{
    SystemUnavailable = 1,
    HighErrorRate = 2,
    ResponseTimeout = 3,
    CapacityExceeded = 4,
    SecurityBreach = 5,
    ManualTrigger = 6
}

/// <summary>
/// Billing failover strategy
/// </summary>
public enum BillingFailoverStrategy
{
    Sequential = 1,
    LoadBalance = 2,
    GeographicRouting = 3,
    CapacityBased = 4,
    CostOptimized = 5
}

/// <summary>
/// Billing system constraint
/// </summary>
public class BillingSystemConstraint
{
    public required string ConstraintId { get; set; }
    public required BillingSystemConstraintType Type { get; set; }
    public required string Description { get; set; }
    public required object ConstraintValue { get; set; }
    public bool IsActive { get; set; } = true;
    public BillingSystemConstraintSeverity Severity { get; set; } = BillingSystemConstraintSeverity.Medium;
    public Dictionary<string, object> ConstraintMetadata { get; set; } = new();
}

/// <summary>
/// Billing system constraint types
/// </summary>
public enum BillingSystemConstraintType
{
    GeographicRestriction = 1,
    CurrencyLimitation = 2,
    TransactionLimit = 3,
    TimeRestriction = 4,
    RegulatoryCompliance = 5,
    TechnicalRequirement = 6
}

/// <summary>
/// Billing system constraint severity
/// </summary>
public enum BillingSystemConstraintSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Blocking = 4
}

/// <summary>
/// Cultural billing configuration
/// </summary>
public class CulturalBillingConfiguration
{
    public required CulturalEventType EventType { get; set; }
    public required List<string> PreferredPaymentMethods { get; set; }
    public required Dictionary<string, decimal> CommunityDiscounts { get; set; }
    public required List<string> SupportedLanguages { get; set; }
    public string? ReligiousConsiderations { get; set; }
    public bool RequiresSpecialHandling { get; set; }
    public Dictionary<string, object> CulturalMetadata { get; set; } = new();
}

/// <summary>
/// Diaspora billing policy
/// </summary>
public class DiasporaBillingPolicy
{
    public required string RegionId { get; set; }
    public required string RegionName { get; set; }
    public required List<string> SupportedCurrencies { get; set; }
    public required Dictionary<string, decimal> ExchangeRates { get; set; }
    public required List<string> LocalPaymentMethods { get; set; }
    public required Dictionary<string, decimal> RegionalFees { get; set; }
    public string? TimeZone { get; set; }
    public List<string> RegulatoryRequirements { get; set; } = new();
    public Dictionary<string, object> RegionMetadata { get; set; } = new();
}