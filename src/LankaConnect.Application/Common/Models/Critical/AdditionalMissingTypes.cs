using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.ValueObjects;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.Models.Critical;

/// <summary>
/// Additional missing types identified from recent compilation errors
/// </summary>

/// <summary>
/// Data protection regulation for regulatory compliance
/// Referenced in performance monitoring interfaces
/// </summary>
public class DataProtectionRegulation
{
    public required string RegulationId { get; set; }
    public required string RegulationName { get; set; }
    public required DataProtectionRegulationType RegulationType { get; set; }
    public required List<string> ApplicableRegions { get; set; }
    public required Dictionary<string, RegulationRequirement> Requirements { get; set; }
    public required RegulationComplianceLevel ComplianceLevel { get; set; }
    public required DateTime EffectiveDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

/// <summary>
/// Performance cultural event for domain-specific cultural performance tracking
/// This is the Domain version referenced in interfaces
/// </summary>
public class PerformanceCulturalEvent
{
    public required string EventId { get; set; }
    public required string EventName { get; set; }
    public required CulturalEventType EventType { get; set; }
    public required PerformanceImpactLevel ImpactLevel { get; set; }
    public required DateTime EventStart { get; set; }
    public required DateTime EventEnd { get; set; }
    public required Dictionary<string, PerformanceEventMetric> PerformanceMetrics { get; set; }
    public required List<string> AffectedRegions { get; set; }
    public required CulturalEventPriority Priority { get; set; }
    public Dictionary<string, object> EventContext { get; set; } = new();
    public string? EventDescription { get; set; }
    public bool IsActive { get; set; } = true;
}

// Supporting Enums and Types

public enum DataProtectionRegulationType
{
    GDPR,
    CCPA,
    PIPEDA,
    LGPD,
    CulturalDataProtection,
    Regional,
    Industry,
    Custom
}

public enum RegulationComplianceLevel
{
    Basic,
    Standard,
    Enhanced,
    Strict,
    Custom
}

public enum PerformanceImpactLevel
{
    Minimal,
    Low,
    Medium,
    High,
    Critical,
    Severe
}

public enum CulturalEventPriority
{
    Low,
    Medium,
    High,
    Critical,
    Sacred
}

// Supporting Complex Types

public class RegulationRequirement
{
    public required string RequirementId { get; set; }
    public required string RequirementName { get; set; }
    public required string Description { get; set; }
    public required RegulationRequirementType RequirementType { get; set; }
    public required bool IsMandatory { get; set; }
    public Dictionary<string, object> RequirementParameters { get; set; } = new();
}

public class PerformanceEventMetric
{
    public required string MetricName { get; set; }
    public required double Value { get; set; }
    public required string Unit { get; set; }
    public required PerformanceMetricType MetricType { get; set; }
    public required DateTime MeasurementTimestamp { get; set; }
    public PerformanceMetricStatus Status { get; set; } = PerformanceMetricStatus.Normal;
}

public enum RegulationRequirementType
{
    DataProcessing,
    ConsentManagement,
    DataRetention,
    DataTransfer,
    UserRights,
    SecurityMeasures,
    CulturalSensitivity
}

public enum PerformanceMetricType
{
    Counter,
    Gauge,
    Rate,
    Histogram,
    Summary,
    Cultural
}

public enum PerformanceMetricStatus
{
    Normal,
    Warning,
    Critical,
    Unknown,
    Degraded
}