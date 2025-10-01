using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.DisasterRecovery;

/// <summary>
/// Revenue protection implementation result for cultural event and diaspora community revenue safeguarding
/// Critical for ensuring business continuity during disasters for Sri Lankan cultural events
/// </summary>
public class RevenueProtectionImplementationResult
{
    public required string ImplementationId { get; set; }
    public required string ProtectionPlanId { get; set; }
    public required RevenueProtectionImplementationStatus Status { get; set; }
    public required DateTime ImplementationTimestamp { get; set; }
    public required TimeSpan ImplementationDuration { get; set; }
    public required List<RevenueProtectionMeasure> ImplementedMeasures { get; set; }
    public required Dictionary<string, RevenueProtectionMetric> ProtectionMetrics { get; set; }
    public required string ImplementedBy { get; set; }
    public required RevenueProtectionScope Scope { get; set; }
    public required decimal EstimatedRevenueProtected { get; set; }
    public List<RevenueRiskAssessment> RiskAssessments { get; set; } = new();
    public Dictionary<string, object> ImplementationContext { get; set; } = new();
    public string? ImplementationSummary { get; set; }
    public List<string> ProtectedRevenueStreams { get; set; } = new();
    public Dictionary<string, decimal> RevenueImpactMitigation { get; set; } = new();
    public List<CulturalEventRevenueProtection> CulturalEventProtections { get; set; } = new();

    public RevenueProtectionImplementationResult()
    {
        ImplementedMeasures = new List<RevenueProtectionMeasure>();
        ProtectionMetrics = new Dictionary<string, RevenueProtectionMetric>();
        RiskAssessments = new List<RevenueRiskAssessment>();
        ImplementationContext = new Dictionary<string, object>();
        ProtectedRevenueStreams = new List<string>();
        RevenueImpactMitigation = new Dictionary<string, decimal>();
        CulturalEventProtections = new List<CulturalEventRevenueProtection>();
    }

    public bool HasSuccessfulImplementation()
    {
        return Status == RevenueProtectionImplementationStatus.Completed &&
               ImplementedMeasures.Any(m => m.Status == RevenueProtectionMeasureStatus.Active);
    }

    public decimal GetTotalProtectedRevenue()
    {
        return EstimatedRevenueProtected + RevenueImpactMitigation.Values.Sum();
    }
}

/// <summary>
/// Revenue protection implementation status
/// </summary>
public enum RevenueProtectionImplementationStatus
{
    Planned = 1,
    InProgress = 2,
    Completed = 3,
    Failed = 4,
    PartiallyImplemented = 5,
    Rollback = 6
}

/// <summary>
/// Revenue protection scope
/// </summary>
public enum RevenueProtectionScope
{
    CulturalEvents = 1,
    DiasporaServices = 2,
    AdvertisementRevenue = 3,
    SubscriptionServices = 4,
    TransactionFees = 5,
    AllRevenueStreams = 6
}

/// <summary>
/// Revenue protection measure
/// </summary>
public class RevenueProtectionMeasure
{
    public required string MeasureId { get; set; }
    public required RevenueProtectionMeasureType Type { get; set; }
    public required RevenueProtectionMeasureStatus Status { get; set; }
    public required string Description { get; set; }
    public required DateTime ImplementedAt { get; set; }
    public required List<string> ProtectedComponents { get; set; }
    public decimal ExpectedImpact { get; set; }
    public string? ImplementationDetails { get; set; }
    public Dictionary<string, object> MeasureConfiguration { get; set; } = new();
    public List<string> Dependencies { get; set; } = new();
}

/// <summary>
/// Revenue protection measure types
/// </summary>
public enum RevenueProtectionMeasureType
{
    AlternativePaymentGateway = 1,
    BackupBillingSystem = 2,
    RevenueDataBackup = 3,
    TransactionFailover = 4,
    CulturalEventContinuity = 5,
    DiasporaServiceRedundancy = 6,
    AdvertisementFailover = 7,
    SubscriptionProtection = 8
}

/// <summary>
/// Revenue protection measure status
/// </summary>
public enum RevenueProtectionMeasureStatus
{
    Active = 1,
    Inactive = 2,
    Testing = 3,
    Failed = 4,
    Maintenance = 5,
    Standby = 6
}

/// <summary>
/// Revenue protection metric
/// </summary>
public class RevenueProtectionMetric
{
    public required string MetricName { get; set; }
    public required double Value { get; set; }
    public required string Unit { get; set; }
    public required RevenueProtectionMetricStatus Status { get; set; }
    public DateTime MeasuredAt { get; set; }
    public string? Description { get; set; }
    public double? TargetValue { get; set; }
    public double? ThresholdValue { get; set; }
}

/// <summary>
/// Revenue protection metric status
/// </summary>
public enum RevenueProtectionMetricStatus
{
    Normal = 1,
    Warning = 2,
    Critical = 3,
    Protected = 4,
    AtRisk = 5
}

/// <summary>
/// Revenue risk assessment
/// </summary>
public class RevenueRiskAssessment
{
    public required string AssessmentId { get; set; }
    public required RevenueRiskType RiskType { get; set; }
    public required RevenueRiskSeverity Severity { get; set; }
    public required string Description { get; set; }
    public required decimal PotentialImpact { get; set; }
    public required double Probability { get; set; }
    public DateTime AssessedAt { get; set; }
    public string? MitigationStrategy { get; set; }
    public List<string> AffectedRevenueStreams { get; set; } = new();
    public Dictionary<string, object> RiskContext { get; set; } = new();
}

/// <summary>
/// Revenue risk types
/// </summary>
public enum RevenueRiskType
{
    SystemFailure = 1,
    PaymentGatewayFailure = 2,
    DatabaseCorruption = 3,
    NetworkOutage = 4,
    CulturalEventDisruption = 5,
    DiasporaServiceInterruption = 6,
    AdvertisementSystemFailure = 7,
    SubscriptionBillingFailure = 8
}

/// <summary>
/// Revenue risk severity
/// </summary>
public enum RevenueRiskSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4,
    Catastrophic = 5
}

/// <summary>
/// Cultural event revenue protection
/// </summary>
public class CulturalEventRevenueProtection
{
    public required string EventId { get; set; }
    public required string EventName { get; set; }
    public required CulturalEventType EventType { get; set; }
    public required List<RevenueStream> ProtectedRevenueStreams { get; set; }
    public required decimal EstimatedRevenue { get; set; }
    public required CulturalEventProtectionStatus ProtectionStatus { get; set; }
    public DateTime EventDate { get; set; }
    public string? DiasporaRegion { get; set; }
    public Dictionary<string, decimal> RevenueBreakdown { get; set; } = new();
    public List<string> BackupVenues { get; set; } = new();
}

/// <summary>
/// Cultural event types
/// </summary>
// CulturalEventType is now imported from Domain.Common.Enums

/// <summary>
/// Cultural event protection status
/// </summary>
public enum CulturalEventProtectionStatus
{
    Protected = 1,
    PartiallyProtected = 2,
    AtRisk = 3,
    Failed = 4,
    NotProtected = 5
}

/// <summary>
/// Revenue stream
/// </summary>
public class RevenueStream
{
    public required string StreamId { get; set; }
    public required string StreamName { get; set; }
    public required RevenueStreamType Type { get; set; }
    public required decimal Amount { get; set; }
    public required RevenueStreamStatus Status { get; set; }
    public string? Currency { get; set; }
    public Dictionary<string, object> StreamMetadata { get; set; } = new();
}

/// <summary>
/// Revenue stream types
/// </summary>
public enum RevenueStreamType
{
    TicketSales = 1,
    Advertisements = 2,
    Sponsorships = 3,
    Subscriptions = 4,
    TransactionFees = 5,
    PremiumServices = 6
}

/// <summary>
/// Revenue stream status
/// </summary>
public enum RevenueStreamStatus
{
    Active = 1,
    Protected = 2,
    AtRisk = 3,
    Failed = 4,
    Suspended = 5
}