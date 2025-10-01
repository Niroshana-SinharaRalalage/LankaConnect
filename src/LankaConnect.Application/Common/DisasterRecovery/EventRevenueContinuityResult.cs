using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.DisasterRecovery;

/// <summary>
/// Event revenue continuity result for cultural events and diaspora community revenue preservation
/// Tracks the outcome and effectiveness of revenue continuity measures during disaster scenarios
/// </summary>
public class EventRevenueContinuityResult
{
    public required string ResultId { get; set; }
    public required string EventId { get; set; }
    public required string ContinuityStrategyId { get; set; }
    public required EventRevenueContinuityStatus Status { get; set; }
    public required DateTime ExecutionTimestamp { get; set; }
    public required TimeSpan ExecutionDuration { get; set; }
    public required List<RevenueContinuityMeasureResult> MeasureResults { get; set; }
    public required Dictionary<string, EventRevenueMetric> RevenueMetrics { get; set; }
    public required string ExecutedBy { get; set; }
    public required decimal PreEventRevenueProjection { get; set; }
    public required decimal ActualEventRevenue { get; set; }
    public required decimal RevenueRecoveryAmount { get; set; }
    public List<EventRevenueImpactAssessment> ImpactAssessments { get; set; } = new();
    public Dictionary<string, object> ExecutionContext { get; set; } = new();
    public string? ContinuitySummary { get; set; }
    public List<string> AffectedRevenueStreams { get; set; } = new();
    public Dictionary<string, decimal> StreamRecoveryRates { get; set; } = new();
    public CulturalEventRevenueContinuityDetails CulturalEventDetails { get; set; } = new();

    public EventRevenueContinuityResult()
    {
        MeasureResults = new List<RevenueContinuityMeasureResult>();
        RevenueMetrics = new Dictionary<string, EventRevenueMetric>();
        ImpactAssessments = new List<EventRevenueImpactAssessment>();
        ExecutionContext = new Dictionary<string, object>();
        AffectedRevenueStreams = new List<string>();
        StreamRecoveryRates = new Dictionary<string, decimal>();
    }

    public bool WasSuccessful()
    {
        return Status == EventRevenueContinuityStatus.Successful ||
               Status == EventRevenueContinuityStatus.PartiallySuccessful;
    }

    public decimal GetRevenueRecoveryPercentage()
    {
        if (PreEventRevenueProjection <= 0) return 0;
        return (ActualEventRevenue / PreEventRevenueProjection) * 100;
    }

    public bool HasCriticalImpacts()
    {
        return ImpactAssessments.Any(i => i.ImpactSeverity >= EventRevenueImpactSeverity.Critical);
    }
}

/// <summary>
/// Event revenue continuity status
/// </summary>
public enum EventRevenueContinuityStatus
{
    Successful = 1,
    PartiallySuccessful = 2,
    Failed = 3,
    InProgress = 4,
    Aborted = 5,
    NotExecuted = 6
}

/// <summary>
/// Revenue continuity measure result
/// </summary>
public class RevenueContinuityMeasureResult
{
    public required string MeasureId { get; set; }
    public required string MeasureName { get; set; }
    public required RevenueContinuityMeasureType Type { get; set; }
    public required RevenueContinuityMeasureOutcome Outcome { get; set; }
    public required DateTime ExecutedAt { get; set; }
    public required TimeSpan ExecutionDuration { get; set; }
    public required decimal RevenueImpact { get; set; }
    public required double EffectivenessScore { get; set; }
    public string? ExecutionDetails { get; set; }
    public List<string> SuccessFactors { get; set; } = new();
    public List<string> FailureReasons { get; set; } = new();
    public Dictionary<string, object> MeasureMetadata { get; set; } = new();
}

/// <summary>
/// Revenue continuity measure types
/// </summary>
public enum RevenueContinuityMeasureType
{
    SystemFailover = 1,
    AlternativePayment = 2,
    BackupVenue = 3,
    ServiceRedirection = 4,
    EmergencyProtocol = 5,
    CustomerCommunication = 6
}

/// <summary>
/// Revenue continuity measure outcome
/// </summary>
public enum RevenueContinuityMeasureOutcome
{
    Successful = 1,
    PartiallySuccessful = 2,
    Failed = 3,
    NotExecuted = 4,
    Cancelled = 5,
    Delayed = 6
}

/// <summary>
/// Event revenue metric
/// </summary>
public class EventRevenueMetric
{
    public required string MetricName { get; set; }
    public required double Value { get; set; }
    public required string Unit { get; set; }
    public required EventRevenueMetricType Type { get; set; }
    public required DateTime MeasuredAt { get; set; }
    public double? BaselineValue { get; set; }
    public double? TargetValue { get; set; }
    public string? Description { get; set; }
    public EventRevenueMetricStatus Status { get; set; } = EventRevenueMetricStatus.Normal;
}

/// <summary>
/// Event revenue metric types
/// </summary>
public enum EventRevenueMetricType
{
    TotalRevenue = 1,
    TicketSales = 2,
    Sponsorships = 3,
    Advertisements = 4,
    TransactionFees = 5,
    RecoveryRate = 6,
    AttendanceRate = 7,
    ConversionRate = 8
}

/// <summary>
/// Event revenue metric status
/// </summary>
public enum EventRevenueMetricStatus
{
    Normal = 1,
    Warning = 2,
    Critical = 3,
    Improving = 4,
    Declining = 5
}

/// <summary>
/// Event revenue impact assessment
/// </summary>
public class EventRevenueImpactAssessment
{
    public required string AssessmentId { get; set; }
    public required EventRevenueImpactType ImpactType { get; set; }
    public required EventRevenueImpactSeverity ImpactSeverity { get; set; }
    public required string Description { get; set; }
    public required decimal FinancialImpact { get; set; }
    public required List<string> AffectedComponents { get; set; }
    public DateTime AssessedAt { get; set; }
    public string? MitigationStrategy { get; set; }
    public EventRevenueImpactStatus Status { get; set; } = EventRevenueImpactStatus.Identified;
    public Dictionary<string, object> ImpactContext { get; set; } = new();
}

/// <summary>
/// Event revenue impact types
/// </summary>
public enum EventRevenueImpactType
{
    RevenueReduction = 1,
    AttendanceDecline = 2,
    PaymentFailure = 3,
    ServiceDisruption = 4,
    CustomerExperience = 5,
    OperationalCost = 6
}

/// <summary>
/// Event revenue impact severity
/// </summary>
public enum EventRevenueImpactSeverity
{
    Minimal = 1,
    Low = 2,
    Medium = 3,
    High = 4,
    Critical = 5
}

/// <summary>
/// Event revenue impact status
/// </summary>
public enum EventRevenueImpactStatus
{
    Identified = 1,
    Assessing = 2,
    Mitigating = 3,
    Resolved = 4,
    Accepted = 5
}

/// <summary>
/// Cultural event revenue continuity details
/// </summary>
public class CulturalEventRevenueContinuityDetails
{
    public CulturalEventType EventType { get; set; } = CulturalEventType.Community;
    public string? DiasporaRegion { get; set; }
    public List<string> CulturalConsiderations { get; set; } = new();
    public Dictionary<string, decimal> CommunityImpactMetrics { get; set; } = new();
    public List<CulturalRevenueStreamResult> CulturalStreamResults { get; set; } = new();
    public string? ReligiousSignificance { get; set; }
    public bool RequiredSpecialAccommodations { get; set; }
    public Dictionary<string, object> CulturalMetadata { get; set; } = new();

    public CulturalEventRevenueContinuityDetails()
    {
        CulturalConsiderations = new List<string>();
        CommunityImpactMetrics = new Dictionary<string, decimal>();
        CulturalStreamResults = new List<CulturalRevenueStreamResult>();
        CulturalMetadata = new Dictionary<string, object>();
    }
}

/// <summary>
/// Cultural revenue stream result
/// </summary>
public class CulturalRevenueStreamResult
{
    public required string StreamId { get; set; }
    public required CulturalRevenueStreamType StreamType { get; set; }
    public required decimal BaselineRevenue { get; set; }
    public required decimal ActualRevenue { get; set; }
    public required CulturalStreamContinuityStatus ContinuityStatus { get; set; }
    public string? CulturalImpactFactor { get; set; }
    public List<string> AdaptationsMade { get; set; } = new();
    public Dictionary<string, object> StreamContext { get; set; } = new();
}

/// <summary>
/// Cultural revenue stream types
/// </summary>
public enum CulturalRevenueStreamType
{
    ReligiousDonations = 1,
    FestivalTickets = 2,
    CulturalSponsorship = 3,
    CommunityAdvertising = 4,
    EducationalServices = 5,
    TraditionalCrafts = 6
}

/// <summary>
/// Cultural stream continuity status
/// </summary>
public enum CulturalStreamContinuityStatus
{
    Maintained = 1,
    Adapted = 2,
    Reduced = 3,
    Suspended = 4,
    Alternative = 5,
    Enhanced = 6
}