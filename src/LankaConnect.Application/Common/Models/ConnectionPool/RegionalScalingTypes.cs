namespace LankaConnect.Application.Common.Models.ConnectionPool;

public class SLARecoveryConfiguration
{
    public required string RecoveryId { get; set; }
    public required Dictionary<string, double> SLATargets { get; set; }
    public required string RecoveryStrategy { get; set; }
    public required TimeSpan RecoveryTimeObjective { get; set; }
    public required List<string> RecoveryActions { get; set; }
    public bool AutoRecoveryEnabled { get; set; }
    public Dictionary<string, object> RecoveryParameters { get; set; } = new();
}

public class SLARecoveryResult
{
    public required bool RecoverySuccessful { get; set; }
    public required string RecoveryStrategy { get; set; }
    public required Dictionary<string, double> AchievedSLAs { get; set; }
    public required DateTime RecoveryTimestamp { get; set; }
    public required TimeSpan RecoveryDuration { get; set; }
    public Dictionary<string, object> RecoveryMetrics { get; set; } = new();
    public string? RecoveryLimitations { get; set; }
}

public class MultiRegionScalingConfiguration
{
    public required string ConfigurationId { get; set; }
    public required List<string> TargetRegions { get; set; }
    public required Dictionary<string, double> RegionWeights { get; set; }
    public required string ScalingStrategy { get; set; }
    public required Dictionary<string, int> RegionCapacities { get; set; }
    public bool CrossRegionFailoverEnabled { get; set; }
    public Dictionary<string, object> NetworkConfiguration { get; set; } = new();
}

public class MultiRegionScalingResult
{
    public required bool ScalingSuccessful { get; set; }
    public required Dictionary<string, int> RegionInstances { get; set; }
    public required string ScalingStrategy { get; set; }
    public required DateTime ScalingTimestamp { get; set; }
    public required Dictionary<string, double> LoadDistribution { get; set; }
    public Dictionary<string, object> PerformanceMetrics { get; set; } = new();
    public List<string> ScalingWarnings { get; set; } = new();
}

public class CrossRegionLoadBalancingConfiguration
{
    public required string ConfigurationId { get; set; }
    public required List<string> ParticipatingRegions { get; set; }
    public required Dictionary<string, double> LoadBalancingWeights { get; set; }
    public required string BalancingAlgorithm { get; set; }
    public required Dictionary<string, object> HealthCheckConfiguration { get; set; }
    public bool GeographicAfinityEnabled { get; set; }
    public TimeSpan HealthCheckInterval { get; set; }
}

public class CrossRegionLoadBalancingResult
{
    public required bool BalancingSuccessful { get; set; }
    public required Dictionary<string, double> RegionLoadDistribution { get; set; }
    public required List<string> ActiveRegions { get; set; }
    public required DateTime BalancingTimestamp { get; set; }
    public required string BalancingStrategy { get; set; }
    public Dictionary<string, object> LatencyMetrics { get; set; } = new();
    public List<string> FailedRegions { get; set; } = new();
}

public class ScalingSynchronizationRequest
{
    public required string RequestId { get; set; }
    public required List<string> ParticipatingRegions { get; set; }
    public required Dictionary<string, object> SynchronizationParameters { get; set; }
    public required string SynchronizationType { get; set; }
    public required DateTime RequestTimestamp { get; set; }
    public Dictionary<string, string> RegionStates { get; set; } = new();
    public bool ForceSync { get; set; }
}

public class ScalingSynchronizationResult
{
    public required bool SynchronizationSuccessful { get; set; }
    public required Dictionary<string, string> RegionSyncStates { get; set; }
    public required DateTime SynchronizationTimestamp { get; set; }
    public required TimeSpan SynchronizationDuration { get; set; }
    public required List<string> SynchronizedRegions { get; set; }
    public Dictionary<string, object> ConflictResolutions { get; set; } = new();
    public List<string> FailedSyncs { get; set; } = new();
}

public class RegionalFailoverConfiguration
{
    public required string ConfigurationId { get; set; }
    public required string PrimaryRegion { get; set; }
    public required List<string> FailoverRegions { get; set; }
    public required Dictionary<string, int> FailoverPriorities { get; set; }
    public required string FailoverTrigger { get; set; }
    public required TimeSpan FailoverThreshold { get; set; }
    public bool AutoFailoverEnabled { get; set; }
    public Dictionary<string, object> FailoverCriteria { get; set; } = new();
}

public class RegionalFailoverResult
{
    public required bool FailoverTriggered { get; set; }
    public required string OriginalRegion { get; set; }
    public required string FailoverRegion { get; set; }
    public required DateTime FailoverTimestamp { get; set; }
    public required TimeSpan FailoverDuration { get; set; }
    public required string FailoverReason { get; set; }
    public Dictionary<string, object> DataConsistencyStatus { get; set; } = new();
    public bool RollbackRequired { get; set; }
}

public class ResourceAllocationOptimizationRequest
{
    public required string RequestId { get; set; }
    public required Dictionary<string, double> CurrentAllocations { get; set; }
    public required Dictionary<string, double> TargetUtilization { get; set; }
    public required string OptimizationObjective { get; set; }
    public required List<string> OptimizationConstraints { get; set; }
    public DateTime RequestTimestamp { get; set; }
    public Dictionary<string, object> CostParameters { get; set; } = new();
}

public class ResourceAllocationOptimizationResult
{
    public required bool OptimizationSuccessful { get; set; }
    public required Dictionary<string, double> OptimizedAllocations { get; set; }
    public required double OptimizationGain { get; set; }
    public required DateTime OptimizationTimestamp { get; set; }
    public required string OptimizationStrategy { get; set; }
    public Dictionary<string, object> PerformanceImpact { get; set; } = new();
    public Dictionary<string, double> CostSavings { get; set; } = new();
}

public class GeoDistributedScalingConfiguration
{
    public required string ConfigurationId { get; set; }
    public required Dictionary<string, object> GeographicParameters { get; set; }
    public required List<string> DistributionRegions { get; set; }
    public required Dictionary<string, double> LatencyTargets { get; set; }
    public required string DistributionStrategy { get; set; }
    public bool ProximityBasedRouting { get; set; }
    public Dictionary<string, object> ComplianceRequirements { get; set; } = new();
}

public class GeoDistributedScalingResult
{
    public required bool DistributionSuccessful { get; set; }
    public required Dictionary<string, double> RegionalDistribution { get; set; }
    public required List<string> ActiveDistributionPoints { get; set; }
    public required DateTime DistributionTimestamp { get; set; }
    public required Dictionary<string, double> LatencyMetrics { get; set; }
    public Dictionary<string, object> ComplianceStatus { get; set; } = new();
    public string? DistributionLimitations { get; set; }
}

public class MultiRegionCapacityPlanningRequest
{
    public required string RequestId { get; set; }
    public required Dictionary<string, double> ForecastedDemand { get; set; }
    public required List<string> PlanningRegions { get; set; }
    public required TimeSpan PlanningHorizon { get; set; }
    public required Dictionary<string, object> GrowthProjections { get; set; }
    public Dictionary<string, double> RegionalConstraints { get; set; } = new();
    public DateTime PlanningTimestamp { get; set; }
}

public class MultiRegionCapacityPlanningResult
{
    public required bool PlanningSuccessful { get; set; }
    public required Dictionary<string, double> RecommendedCapacities { get; set; }
    public required List<string> CapacityRecommendations { get; set; }
    public required DateTime PlanningTimestamp { get; set; }
    public required Dictionary<string, object> CostProjections { get; set; }
    public Dictionary<string, double> UtilizationForecasts { get; set; } = new();
    public List<string> RiskFactors { get; set; } = new();
}
