using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.ValueObjects;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.Models.Configuration;

/// <summary>
/// Tier 1 Foundation Configuration Types - Core Infrastructure
/// These types are referenced extensively throughout the Application layer
/// Priority implementation for maximum error reduction impact
/// </summary>

/// <summary>
/// Synchronization policy for multi-region cultural intelligence coordination
/// Referenced in performance monitoring, disaster recovery, and security optimization
/// </summary>
public class SynchronizationPolicy
{
    public required string PolicyId { get; set; }
    public required string PolicyName { get; set; }
    public required SynchronizationMode SynchronizationMode { get; set; }
    public required TimeSpan SynchronizationInterval { get; set; }
    public required int MaxRetryAttempts { get; set; }
    public required TimeSpan RetryDelay { get; set; }
    public required bool EnableConflictResolution { get; set; }
    public required ConflictResolutionStrategy ConflictResolutionStrategy { get; set; }
    public required List<string> SynchronizedRegions { get; set; }
    public required Dictionary<string, object> PolicyConfiguration { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public Dictionary<string, string> Metadata { get; set; } = new();
}

/// <summary>
/// Network topology configuration for distributed cultural intelligence platform
/// Defines regional interconnections and failover patterns
/// </summary>
public class NetworkTopology
{
    public required string TopologyId { get; set; }
    public required string TopologyName { get; set; }
    public required NetworkTopologyType TopologyType { get; set; }
    public required List<NetworkNode> Nodes { get; set; }
    public required List<NetworkConnection> Connections { get; set; }
    public required List<string> PrimaryRegions { get; set; }
    public required List<string> SecondaryRegions { get; set; }
    public required Dictionary<string, FailoverRoute> FailoverRoutes { get; set; }
    public required NetworkSecurityConfiguration SecurityConfiguration { get; set; }
    public required NetworkPerformanceConfiguration PerformanceConfiguration { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public Dictionary<string, object> TopologyMetrics { get; set; } = new();
}

/// <summary>
/// Failover trigger criteria for automatic disaster recovery coordination
/// Critical for maintaining cultural intelligence availability during outages
/// </summary>
public class FailoverTriggerCriteria
{
    public required string CriteriaId { get; set; }
    public required string CriteriaName { get; set; }
    public required List<TriggerCondition> TriggerConditions { get; set; }
    public required FailoverSeverityLevel MinimumSeverityLevel { get; set; }
    public required TimeSpan ResponseTimeThreshold { get; set; }
    public required double ErrorRateThreshold { get; set; }
    public required int ConsecutiveFailureThreshold { get; set; }
    public required TimeSpan EvaluationWindow { get; set; }
    public required bool EnableAutomaticFailover { get; set; }
    public required List<string> ExcludedRegions { get; set; }
    public required Dictionary<string, object> CriteriaConfiguration { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public FailoverPriority Priority { get; set; } = FailoverPriority.Medium;
}

/// <summary>
/// Global metrics configuration for enterprise-wide performance monitoring
/// Coordinates metrics collection across all cultural intelligence services
/// </summary>
public class GlobalMetricsConfiguration
{
    public required string ConfigurationId { get; set; }
    public required string ConfigurationName { get; set; }
    public required List<MetricDefinition> MetricDefinitions { get; set; }
    public required Dictionary<string, MetricCollectionStrategy> CollectionStrategies { get; set; }
    public required TimeSpan DefaultCollectionInterval { get; set; }
    public required int MaxRetentionDays { get; set; }
    public required List<string> EnabledRegions { get; set; }
    public required MetricAggregationConfiguration AggregationConfiguration { get; set; }
    public required AlertingConfiguration AlertingConfiguration { get; set; }
    public required Dictionary<string, object> ExportConfiguration { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public Dictionary<string, MetricThreshold> Thresholds { get; set; } = new();
}

/// <summary>
/// Auto-scaling configuration for dynamic resource management
/// Most frequently referenced type - critical for performance optimization
/// </summary>
public class AutoScalingConfiguration
{
    public required string ConfigurationId { get; set; }
    public required string ConfigurationName { get; set; }
    public required AutoScalingMode ScalingMode { get; set; }
    public required ScalingPolicySet ScalingPolicies { get; set; }
    public required ResourceThresholds MinimumThresholds { get; set; }
    public required ResourceThresholds MaximumThresholds { get; set; }
    public required TimeSpan ScaleUpCooldownPeriod { get; set; }
    public required TimeSpan ScaleDownCooldownPeriod { get; set; }
    public required int MinInstanceCount { get; set; }
    public required int MaxInstanceCount { get; set; }
    public required double CpuUtilizationTarget { get; set; }
    public required double MemoryUtilizationTarget { get; set; }
    public required List<string> EnabledRegions { get; set; }
    public required Dictionary<string, CulturalEventScalingRule> CulturalEventRules { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public Dictionary<string, object> ScalingMetrics { get; set; } = new();
}

// Supporting Enums and Value Objects

public enum SynchronizationMode
{
    Immediate,
    Scheduled,
    EventDriven,
    Hybrid
}

public enum ConflictResolutionStrategy
{
    LastWriteWins,
    FirstWriteWins,
    Merge,
    Manual,
    CulturalPriority
}

public enum NetworkTopologyType
{
    Mesh,
    Star,
    Ring,
    Hybrid,
    Hierarchical
}

public enum FailoverSeverityLevel
{
    Low,
    Medium,
    High,
    Critical,
    Emergency
}

public enum FailoverPriority
{
    Low,
    Medium,
    High,
    Critical
}

public enum AutoScalingMode
{
    Reactive,
    Predictive,
    Hybrid,
    Manual,
    CulturalEventDriven
}

// Supporting Complex Types

public class NetworkNode
{
    public required string NodeId { get; set; }
    public required string NodeName { get; set; }
    public required string Region { get; set; }
    public required NodeType NodeType { get; set; }
    public required NodeCapacity Capacity { get; set; }
    public required NodeStatus Status { get; set; }
    public Dictionary<string, object> NodeMetrics { get; set; } = new();
}

public class NetworkConnection
{
    public required string ConnectionId { get; set; }
    public required string SourceNodeId { get; set; }
    public required string DestinationNodeId { get; set; }
    public required ConnectionType ConnectionType { get; set; }
    public required double Bandwidth { get; set; }
    public required double Latency { get; set; }
    public required ConnectionStatus Status { get; set; }
}

public class FailoverRoute
{
    public required string RouteId { get; set; }
    public required string PrimaryRegion { get; set; }
    public required string FailoverRegion { get; set; }
    public required FailoverStrategy Strategy { get; set; }
    public required TimeSpan EstimatedFailoverTime { get; set; }
    public required int Priority { get; set; }
}

public class TriggerCondition
{
    public required string ConditionId { get; set; }
    public required string MetricName { get; set; }
    public required ComparisonOperator Operator { get; set; }
    public required double Threshold { get; set; }
    public required TimeSpan EvaluationPeriod { get; set; }
}

public class MetricDefinition
{
    public required string MetricId { get; set; }
    public required string MetricName { get; set; }
    public required MetricType MetricType { get; set; }
    public required string Unit { get; set; }
    public required MetricAggregationType AggregationType { get; set; }
    public Dictionary<string, object> Dimensions { get; set; } = new();
}

public class ScalingPolicySet
{
    public required List<ScalingPolicy> ScaleUpPolicies { get; set; }
    public required List<ScalingPolicy> ScaleDownPolicies { get; set; }
    public required Dictionary<string, CulturalEventScalingPolicy> EventBasedPolicies { get; set; }
}

public class ResourceThresholds
{
    public required double CpuThreshold { get; set; }
    public required double MemoryThreshold { get; set; }
    public required double DiskThreshold { get; set; }
    public required double NetworkThreshold { get; set; }
    public Dictionary<string, double> CustomThresholds { get; set; } = new();
}

public class CulturalEventScalingRule
{
    public required string RuleId { get; set; }
    public required CulturalEventType EventType { get; set; }
    public required ScalingMultiplier ScalingMultiplier { get; set; }
    public required TimeSpan PreScalingDuration { get; set; }
    public required TimeSpan PostScalingDuration { get; set; }
}

// Additional supporting enums and classes as needed
public enum NodeType { Primary, Secondary, Cache, LoadBalancer }
public enum NodeStatus { Active, Inactive, Maintenance, Failed }
public enum ConnectionType { Primary, Backup, Cache, Management }
public enum ConnectionStatus { Active, Inactive, Degraded, Failed }
public enum FailoverStrategy { Immediate, Gradual, LoadBalanced, Priority }
public enum ComparisonOperator { Greater, GreaterOrEqual, Less, LessOrEqual, Equal, NotEqual }
public enum MetricType { Counter, Gauge, Histogram, Summary }
public enum MetricAggregationType { Sum, Average, Maximum, Minimum, Count }

// Placeholder classes that will be implemented in subsequent tiers
public class NetworkSecurityConfiguration { }
public class NetworkPerformanceConfiguration { }
public class NodeCapacity { }
public class MetricCollectionStrategy { }
public class MetricAggregationConfiguration { }
public class AlertingConfiguration { }
public class MetricThreshold { }
public class ScalingPolicy { }
public class CulturalEventScalingPolicy { }
public class ScalingMultiplier { }