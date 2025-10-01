using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.DisasterRecovery;

/// <summary>
/// Alternative channel configuration for cultural event and diaspora community revenue streams
/// Defines backup revenue channels and alternative service delivery methods during disasters
/// </summary>
public class AlternativeChannelConfiguration
{
    public required string ConfigurationId { get; set; }
    public required string ConfigurationName { get; set; }
    public required AlternativeChannelType ChannelType { get; set; }
    public required AlternativeChannelScope Scope { get; set; }
    public required List<AlternativeRevenueChannel> AvailableChannels { get; set; }
    public required Dictionary<string, ChannelFailoverRule> FailoverRules { get; set; }
    public required AlternativeChannelPriority Priority { get; set; }
    public required TimeSpan ActivationThreshold { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? Description { get; set; }
    public List<CulturalChannelConfiguration> CulturalChannels { get; set; } = new();
    public Dictionary<string, DiasporaChannelMapping> DiasporaChannelMappings { get; set; } = new();
    public Dictionary<string, object> ConfigurationParameters { get; set; } = new();

    public AlternativeChannelConfiguration()
    {
        AvailableChannels = new List<AlternativeRevenueChannel>();
        FailoverRules = new Dictionary<string, ChannelFailoverRule>();
        CulturalChannels = new List<CulturalChannelConfiguration>();
        DiasporaChannelMappings = new Dictionary<string, DiasporaChannelMapping>();
        ConfigurationParameters = new Dictionary<string, object>();
    }

    public bool HasAvailableChannel(AlternativeChannelType channelType)
    {
        return IsEnabled && AvailableChannels.Any(c => c.ChannelType == channelType && c.IsAvailable);
    }

    public AlternativeRevenueChannel? GetPrimaryChannel(string streamId)
    {
        return AvailableChannels.FirstOrDefault(c => c.SupportedStreams.Contains(streamId) && c.Priority == AlternativeChannelPriority.Primary);
    }
}

/// <summary>
/// Alternative channel types
/// </summary>
public enum AlternativeChannelType
{
    OnlinePayment = 1,
    MobilePayment = 2,
    BankTransfer = 3,
    CashCollection = 4,
    PartnerChannels = 5,
    DigitalWallets = 6,
    CommunityAgents = 7,
    PostalServices = 8
}

/// <summary>
/// Alternative channel scope
/// </summary>
public enum AlternativeChannelScope
{
    CulturalEvents = 1,
    DiasporaServices = 2,
    PaymentProcessing = 3,
    ServiceDelivery = 4,
    CustomerSupport = 5,
    AllServices = 6
}

/// <summary>
/// Alternative channel priority
/// </summary>
public enum AlternativeChannelPriority
{
    Primary = 1,
    Secondary = 2,
    Tertiary = 3,
    Emergency = 4,
    LastResort = 5
}

/// <summary>
/// Alternative revenue channel
/// </summary>
public class AlternativeRevenueChannel
{
    public required string ChannelId { get; set; }
    public required string ChannelName { get; set; }
    public required AlternativeChannelType ChannelType { get; set; }
    public required AlternativeChannelPriority Priority { get; set; }
    public required List<string> SupportedStreams { get; set; }
    public required AlternativeChannelCapacity Capacity { get; set; }
    public required decimal ProcessingFee { get; set; }
    public required TimeSpan ActivationTime { get; set; }
    public bool IsAvailable { get; set; } = true;
    public string? ServiceProvider { get; set; }
    public List<string> SupportedRegions { get; set; } = new();
    public Dictionary<string, object> ChannelConfiguration { get; set; } = new();
    public List<AlternativeChannelConstraint> Constraints { get; set; } = new();
}

/// <summary>
/// Alternative channel capacity
/// </summary>
public class AlternativeChannelCapacity
{
    public required decimal MaxTransactionAmount { get; set; }
    public required int MaxTransactionsPerHour { get; set; }
    public required decimal MaxDailyVolume { get; set; }
    public required int ConcurrentUsers { get; set; }
    public AlternativeChannelCapacityStatus Status { get; set; } = AlternativeChannelCapacityStatus.Available;
    public double CurrentUtilization { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Alternative channel capacity status
/// </summary>
public enum AlternativeChannelCapacityStatus
{
    Available = 1,
    Limited = 2,
    AtCapacity = 3,
    Overloaded = 4,
    Unavailable = 5
}

/// <summary>
/// Channel failover rule
/// </summary>
public class ChannelFailoverRule
{
    public required string RuleId { get; set; }
    public required string PrimaryChannelId { get; set; }
    public required List<string> FailoverChannelIds { get; set; }
    public required ChannelFailoverTrigger TriggerCondition { get; set; }
    public required TimeSpan EvaluationWindow { get; set; }
    public required int FailureThreshold { get; set; }
    public bool IsEnabled { get; set; } = true;
    public ChannelFailoverStrategy Strategy { get; set; } = ChannelFailoverStrategy.Sequential;
    public Dictionary<string, object> RuleParameters { get; set; } = new();
}

/// <summary>
/// Channel failover triggers
/// </summary>
public enum ChannelFailoverTrigger
{
    ServiceUnavailable = 1,
    CapacityExceeded = 2,
    ErrorRateHigh = 3,
    ResponseTimeSlow = 4,
    ManualTrigger = 5,
    ScheduledMaintenance = 6
}

/// <summary>
/// Channel failover strategy
/// </summary>
public enum ChannelFailoverStrategy
{
    Sequential = 1,
    LoadBalance = 2,
    PriorityBased = 3,
    RegionBased = 4,
    Capacity = 5
}

/// <summary>
/// Alternative channel constraint
/// </summary>
public class AlternativeChannelConstraint
{
    public required string ConstraintId { get; set; }
    public required AlternativeChannelConstraintType Type { get; set; }
    public required string Description { get; set; }
    public required object ConstraintValue { get; set; }
    public bool IsActive { get; set; } = true;
    public AlternativeChannelConstraintSeverity Severity { get; set; } = AlternativeChannelConstraintSeverity.Medium;
    public Dictionary<string, object> ConstraintMetadata { get; set; } = new();
}

/// <summary>
/// Alternative channel constraint types
/// </summary>
public enum AlternativeChannelConstraintType
{
    GeographicRestriction = 1,
    CurrencyLimitation = 2,
    TransactionLimit = 3,
    TimeRestriction = 4,
    RegulatoryCompliance = 5,
    TechnicalRequirement = 6
}

/// <summary>
/// Alternative channel constraint severity
/// </summary>
public enum AlternativeChannelConstraintSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Blocking = 4
}

/// <summary>
/// Cultural channel configuration
/// </summary>
public class CulturalChannelConfiguration
{
    public required CulturalEventType EventType { get; set; }
    public required List<string> PreferredChannels { get; set; }
    public required Dictionary<string, object> CulturalRequirements { get; set; }
    public string? ReligiousConsiderations { get; set; }
    public List<string> LanguageSupport { get; set; } = new();
    public Dictionary<string, decimal> CommunityDiscounts { get; set; } = new();
    public bool RequiresSpecialHandling { get; set; }
}

/// <summary>
/// Diaspora channel mapping
/// </summary>
public class DiasporaChannelMapping
{
    public required string RegionId { get; set; }
    public required string RegionName { get; set; }
    public required List<string> AvailableChannels { get; set; }
    public required Dictionary<string, AlternativeChannelPriority> ChannelPriorities { get; set; }
    public required List<string> SupportedCurrencies { get; set; }
    public string? TimeZone { get; set; }
    public List<string> LocalPartners { get; set; } = new();
    public Dictionary<string, object> RegionalRequirements { get; set; } = new();
}