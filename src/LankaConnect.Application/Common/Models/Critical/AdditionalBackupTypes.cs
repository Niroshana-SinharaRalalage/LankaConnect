using System;
using System.Collections.Generic;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.Models.Critical;

/// <summary>
/// Additional critical types for backup and disaster recovery operations
/// </summary>

#region Integrity Validation Types

public enum IntegrityValidationDepth
{
    Basic = 1,
    Standard = 2,
    Comprehensive = 3,
    Deep = 4,
    ExhaustiveWithCultural = 5
}

public enum CorruptionDetectionScope
{
    CulturalData = 1,
    UserProfiles = 2,
    CommunityContent = 3,
    SystemConfiguration = 4,
    AllData = 5
}

public enum DetectionSensitivity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Ultra = 4,
    CulturalSensitive = 5
}

public enum IntegrityValidationMode
{
    Quick = 1,
    Standard = 2,
    Thorough = 3,
    CulturalAware = 4
}

public enum DataLineageScope
{
    SingleTable = 1,
    RelatedTables = 2,
    DatabaseWide = 3,
    CrossDatabase = 4,
    GlobalCultural = 5
}

#endregion

#region Performance Monitoring Types

public class UpcomingCulturalEvent
{
    public Guid EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public CulturalEventType EventType { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public List<string> AffectedRegions { get; set; } = new();
    public decimal ExpectedLoadMultiplier { get; set; } = 1.0m;
    public long EstimatedParticipants { get; set; }
    public CulturalSignificanceLevel SignificanceLevel { get; set; }
}

public enum PredictionTimeframe
{
    NextHour = 1,
    Next6Hours = 2,
    Next24Hours = 3,
    NextWeek = 4,
    NextMonth = 5
}

public class PerformanceThresholdConfig
{
    public string ConfigId { get; set; } = string.Empty;
    public CulturalEventType EventType { get; set; }
    public Dictionary<string, decimal> CpuThresholds { get; set; } = new();
    public Dictionary<string, decimal> MemoryThresholds { get; set; } = new();
    public Dictionary<string, long> ConnectionThresholds { get; set; } = new();
    public Dictionary<string, decimal> ResponseTimeThresholds { get; set; } = new();
    public bool CulturalEventAdjustmentEnabled { get; set; }
    public decimal CulturalEventMultiplier { get; set; } = 1.5m;
}

#endregion

#region Cultural Event Types

public enum CulturalSignificanceLevel
{
    Local = 1,
    Regional = 2,
    National = 3,
    International = 4,
    GlobalDiaspora = 5
}

#endregion

#region Lineage and Validation Types


#endregion

#region Revenue Impact Types

public class RevenueMetricsConfiguration
{
    public string ConfigurationId { get; set; } = string.Empty;
    public Dictionary<string, decimal> PerformanceThresholds { get; set; } = new();
    public Dictionary<string, decimal> RevenuePerMetric { get; set; } = new();
    public List<RevenueCalculationRule> CalculationRules { get; set; } = new();
    public bool CulturalEventImpactTracking { get; set; }
    public decimal CulturalEventRevenueMultiplier { get; set; } = 1.2m;
}

public class RevenueCalculationRule
{
    public string RuleId { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public string MetricType { get; set; } = string.Empty;
    public string CalculationFormula { get; set; } = string.Empty;
    public Dictionary<string, decimal> Parameters { get; set; } = new();
    public bool IsCulturalEventSpecific { get; set; }
}

public class PerformanceDegradationScenario
{
    public string ScenarioId { get; set; } = string.Empty;
    public string ScenarioName { get; set; } = string.Empty;
    public Dictionary<string, decimal> PerformanceDegradation { get; set; } = new();
    public TimeSpan Duration { get; set; }
    public List<string> AffectedServices { get; set; } = new();
    public List<string> AffectedRegions { get; set; } = new();
    public decimal ProbabilityScore { get; set; }
}

public class RevenueCalculationModel
{
    public string ModelId { get; set; } = string.Empty;
    public string ModelVersion { get; set; } = string.Empty;
    public Dictionary<string, decimal> BaseRevenueRates { get; set; } = new();
    public List<RevenueAdjustmentFactor> AdjustmentFactors { get; set; } = new();
    public bool CulturalEventAware { get; set; }
    public Dictionary<CulturalEventType, decimal> CulturalEventMultipliers { get; set; } = new();
}

public class RevenueAdjustmentFactor
{
    public string FactorId { get; set; } = string.Empty;
    public string FactorName { get; set; } = string.Empty;
    public string FactorType { get; set; } = string.Empty;
    public decimal ImpactMultiplier { get; set; } = 1.0m;
    public Dictionary<string, object> FactorParameters { get; set; } = new();
}

public class RevenueRiskCalculation
{
    public string CalculationId { get; set; } = string.Empty;
    public DateTime CalculationTimestamp { get; set; }
    public decimal TotalRevenueAtRisk { get; set; }
    public decimal ProbabilityOfLoss { get; set; }
    public TimeSpan RiskWindow { get; set; }
    public List<RevenueRiskFactor> RiskFactors { get; set; } = new();
    public decimal CulturalEventRisk { get; set; }
}

public class RevenueRiskFactor
{
    public string FactorId { get; set; } = string.Empty;
    public string FactorName { get; set; } = string.Empty;
    public decimal RevenueImpact { get; set; }
    public decimal Probability { get; set; }
    public string MitigationStrategy { get; set; } = string.Empty;
}

public class PerformanceIncident
{
    public string IncidentId { get; set; } = string.Empty;
    public DateTime IncidentStartTime { get; set; }
    public DateTime? IncidentEndTime { get; set; }
    public string IncidentType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public List<string> AffectedServices { get; set; } = new();
    public List<string> AffectedRegions { get; set; } = new();
    public decimal EstimatedRevenueImpact { get; set; }
    public bool IsCulturalEventRelated { get; set; }
    public string CulturalEventId { get; set; } = string.Empty;
}

public class RevenueProtectionPolicy
{
    public string PolicyId { get; set; } = string.Empty;
    public string PolicyName { get; set; } = string.Empty;
    public List<RevenueProtectionRule> ProtectionRules { get; set; } = new();
    public Dictionary<string, object> PolicyParameters { get; set; } = new();
    public bool CulturalEventSpecific { get; set; }
    public List<CulturalEventType> ApplicableEvents { get; set; } = new();
}

public class RevenueProtectionRule
{
    public string RuleId { get; set; } = string.Empty;
    public string RuleName { get; set; } = string.Empty;
    public string TriggerCondition { get; set; } = string.Empty;
    public List<string> ProtectionActions { get; set; } = new();
    public decimal ThresholdValue { get; set; }
    public string ThresholdMetric { get; set; } = string.Empty;
}

#endregion