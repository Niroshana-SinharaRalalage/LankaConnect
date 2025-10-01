using System;
using System.Collections.Generic;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Application.Common.DTOs;
using LankaConnect.Application.Common.Performance;

namespace LankaConnect.Application.Common.Models.Performance;

/// <summary>
/// Performance monitoring types for global and regional metrics
/// </summary>

#region Global Performance Types

public class GlobalPerformanceMetrics
{
    public string MetricId { get; set; } = string.Empty;
    public DateTime MetricTimestamp { get; set; }
    public GlobalMetricsConfiguration Configuration { get; set; } = null!;

    // Global Database Metrics
    public decimal GlobalCpuUtilization { get; set; }
    public decimal GlobalMemoryUtilization { get; set; }
    public long GlobalConnectionCount { get; set; }
    public decimal GlobalQueryResponseTime { get; set; }
    public long GlobalThroughputPerSecond { get; set; }

    // Regional Breakdown
    public Dictionary<string, RegionalMetrics> RegionalBreakdown { get; set; } = new();

    // Cultural Intelligence Metrics
    public CulturalIntelligencePerformanceMetrics CulturalMetrics { get; set; } = null!;

    // Performance Thresholds
    public bool PerformanceThresholdsExceeded { get; set; }
    public List<string> ExceededThresholds { get; set; } = new();
}

public class GlobalMetricsConfiguration
{
    public string ConfigurationId { get; set; } = string.Empty;
    public TimeSpan CollectionInterval { get; set; }
    public List<string> MonitoredRegions { get; set; } = new();
    public List<string> MonitoredServices { get; set; } = new();
    public Dictionary<string, decimal> PerformanceThresholds { get; set; } = new();
    public bool CulturalEventAwareMonitoring { get; set; }
    public List<MetricType> EnabledMetricTypes { get; set; } = new();
}

public class RegionalMetrics
{
    public string RegionCode { get; set; } = string.Empty;
    public string RegionName { get; set; } = string.Empty;
    public decimal CpuUtilization { get; set; }
    public decimal MemoryUtilization { get; set; }
    public int ConnectionCount { get; set; }
    public decimal ResponseTime { get; set; }
    public long ThroughputPerSecond { get; set; }
    public int ActiveCulturalEvents { get; set; }
    public List<string> PerformanceIssues { get; set; } = new();
}

#endregion

#region Timezone-Aware Reporting Types

public class TimezoneAwarePerformanceReport
{
    public string ReportId { get; set; } = string.Empty;
    public DateTime ReportGenerationTime { get; set; }
    public string PrimaryTimezone { get; set; } = string.Empty;
    public TimeSpan ReportingPeriod { get; set; }

    // Timezone-specific Metrics
    public Dictionary<string, TimezonePerformanceMetrics> TimezoneMetrics { get; set; } = new();

    // Peak Performance Analysis
    public PeakPerformanceAnalysis PeakAnalysis { get; set; } = null!;

    // Cultural Event Correlation
    public List<CulturalEventCorrelation> CulturalCorrelations { get; set; } = new();

    // Recommendations
    public List<PerformanceRecommendation> Recommendations { get; set; } = new();
}

public class TimezonePerformanceMetrics
{
    public string TimezoneId { get; set; } = string.Empty;
    public DateTime LocalTimestamp { get; set; }
    public DateTime UtcTimestamp { get; set; }
    public decimal AverageResponseTime { get; set; }
    public decimal PeakResponseTime { get; set; }
    public long TotalRequests { get; set; }
    public long ErrorCount { get; set; }
    public decimal ErrorRate { get; set; }
    public List<string> ActiveCulturalEvents { get; set; } = new();
    public CulturalLoadFactor CulturalLoad { get; set; } = null!;
}

public class PeakPerformanceAnalysis
{
    public DateTime PeakStartTime { get; set; }
    public DateTime PeakEndTime { get; set; }
    public string PeakTimezone { get; set; } = string.Empty;
    public decimal PeakCpuUtilization { get; set; }
    public decimal PeakMemoryUtilization { get; set; }
    public long PeakThroughput { get; set; }
    public string PeakCause { get; set; } = string.Empty;
    public List<string> ContributingFactors { get; set; } = new();
}

#endregion

#region Regional Compliance Types

// RegionalComplianceStatus moved to Application/Common/Performance/PerformanceMonitoringResultTypes.cs
// Use: using LankaConnect.Application.Common.Performance;

public class DataProtectionRegulation
{
    public string RegulationId { get; set; } = string.Empty;
    public string RegulationName { get; set; } = string.Empty;
    public string RegulationType { get; set; } = string.Empty; // GDPR, CCPA, PIPEDA, etc.
    public List<string> ApplicableRegions { get; set; } = new();
    public List<string> RequiredSafeguards { get; set; } = new();
    public Dictionary<string, object> RegulationParameters { get; set; } = new();
    public DateTime EffectiveDate { get; set; }
    public bool CulturalDataSpecificRequirements { get; set; }
}

// ComplianceViolation class removed - use LegacyComplianceViolationDto from DTOs namespace
// This enforces Clean Architecture boundaries

public class ComplianceRequirement
{
    public string RequirementId { get; set; } = string.Empty;
    public string RequirementName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ComplianceRequirementStatus Status { get; set; }
    public DateTime DueDate { get; set; }
    public List<string> ImplementationSteps { get; set; } = new();
}

#endregion

#region Inter-Region Optimization Types

public class InterRegionOptimizationResult
{
    public string OptimizationId { get; set; } = string.Empty;
    public DateTime OptimizationTimestamp { get; set; }
    public NetworkTopology CurrentTopology { get; set; } = null!;
    public NetworkTopology RecommendedTopology { get; set; } = null!;

    // Optimization Metrics
    public InterRegionOptimizationMetrics CurrentMetrics { get; set; } = null!;
    public InterRegionOptimizationMetrics ProjectedMetrics { get; set; } = null!;

    // Cultural Intelligence Optimization
    public CulturalIntelligenceOptimization CulturalOptimization { get; set; } = null!;

    // Implementation Plan
    public OptimizationImplementationPlan ImplementationPlan { get; set; } = null!;
}

public class NetworkTopology
{
    public string TopologyId { get; set; } = string.Empty;
    public string TopologyType { get; set; } = string.Empty; // Star, Mesh, Ring, Hybrid
    public List<NetworkNode> Nodes { get; set; } = new();
    public List<NetworkConnection> Connections { get; set; } = new();
    public Dictionary<string, decimal> PerformanceCharacteristics { get; set; } = new();
    public List<string> CulturalDataFlows { get; set; } = new();
}

public class NetworkNode
{
    public string NodeId { get; set; } = string.Empty;
    public string RegionCode { get; set; } = string.Empty;
    public NodeType NodeType { get; set; }
    public decimal Capacity { get; set; }
    public decimal CurrentUtilization { get; set; }
    public List<string> SupportedServices { get; set; } = new();
    public bool CulturalIntelligenceEnabled { get; set; }
}

public class NetworkConnection
{
    public string ConnectionId { get; set; } = string.Empty;
    public string SourceNodeId { get; set; } = string.Empty;
    public string TargetNodeId { get; set; } = string.Empty;
    public decimal Bandwidth { get; set; }
    public decimal Latency { get; set; }
    public decimal Reliability { get; set; }
    public ConnectionType ConnectionType { get; set; }
}

public class InterRegionOptimizationMetrics
{
    public decimal AverageLatency { get; set; }
    public decimal PeakLatency { get; set; }
    public decimal ThroughputCapacity { get; set; }
    public decimal CurrentThroughput { get; set; }
    public decimal ReliabilityScore { get; set; }
    public decimal CostPerGbTransferred { get; set; }
    public List<RegionPairMetrics> RegionPairMetrics { get; set; } = new();
}

#endregion

#region Supporting Types

public enum MetricType
{
    CPU = 1,
    Memory = 2,
    Network = 3,
    Storage = 4,
    Application = 5,
    CulturalIntelligence = 6
}

public enum ComplianceViolationSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum ComplianceRequirementStatus
{
    NotStarted = 1,
    InProgress = 2,
    Completed = 3,
    Overdue = 4
}

public enum NodeType
{
    Primary = 1,
    Secondary = 2,
    Cache = 3,
    LoadBalancer = 4,
    CulturalIntelligence = 5
}

public enum ConnectionType
{
    Fiber = 1,
    Satellite = 2,
    Wireless = 3,
    Hybrid = 4
}

public class CulturalIntelligencePerformanceMetrics
{
    public decimal CulturalProcessingLatency { get; set; }
    public decimal CulturalAccuracyScore { get; set; }
    public long CulturalRequestsPerSecond { get; set; }
    public int ActiveCulturalModels { get; set; }
    public decimal ModelUtilization { get; set; }
}

public class CulturalEventCorrelation
{
    public string EventId { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public DateTime EventTime { get; set; }
    public decimal PerformanceImpact { get; set; }
    public string CorrelationType { get; set; } = string.Empty;
}

public class PerformanceRecommendation
{
    public string RecommendationId { get; set; } = string.Empty;
    public string RecommendationType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal PotentialImpact { get; set; }
    public decimal ImplementationCost { get; set; }
    public TimeSpan EstimatedImplementationTime { get; set; }
}

public class CulturalLoadFactor
{
    public decimal LoadMultiplier { get; set; }
    public List<string> ContributingEvents { get; set; } = new();
    public string PrimaryLanguage { get; set; } = string.Empty;
    public int ConcurrentUsers { get; set; }
}

public class CulturalDataProtectionStatus
{
    public bool SacredContentProtected { get; set; }
    public bool PersonalDataEncrypted { get; set; }
    public bool CulturalContextMaintained { get; set; }
    public List<string> ProtectionMethods { get; set; } = new();
}

public class CompliancePerformanceImpact
{
    public decimal LatencyIncrease { get; set; }
    public decimal ThroughputReduction { get; set; }
    public decimal AdditionalResourceUsage { get; set; }
    public List<string> ImpactFactors { get; set; } = new();
}

public class CulturalIntelligenceOptimization
{
    public List<string> OptimalCulturalRoutes { get; set; } = new();
    public Dictionary<string, decimal> CulturalAffinityScores { get; set; } = new();
    public List<string> RecommendedCulturalCacheLocations { get; set; } = new();
}

public class OptimizationImplementationPlan
{
    public List<ImplementationPhase> Phases { get; set; } = new();
    public TimeSpan EstimatedDuration { get; set; }
    public decimal EstimatedCost { get; set; }
    public List<string> RequiredResources { get; set; } = new();
    public List<string> RiskFactors { get; set; } = new();
}

public class ImplementationPhase
{
    public string PhaseId { get; set; } = string.Empty;
    public string PhaseName { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public List<string> Tasks { get; set; } = new();
    public List<string> Dependencies { get; set; } = new();
}

public class RegionPairMetrics
{
    public string SourceRegion { get; set; } = string.Empty;
    public string TargetRegion { get; set; } = string.Empty;
    public decimal Latency { get; set; }
    public decimal Bandwidth { get; set; }
    public decimal ErrorRate { get; set; }
    public long DataTransferVolume { get; set; }
}

#endregion