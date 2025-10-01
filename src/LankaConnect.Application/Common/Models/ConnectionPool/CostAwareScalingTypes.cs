namespace LankaConnect.Application.Common.Models.ConnectionPool;

public class CostAwareScalingConfiguration
{
    public required string ConfigurationId { get; set; }
    public required Dictionary<string, double> CostThresholds { get; set; }
    public required string CostOptimizationStrategy { get; set; }
    public required Dictionary<string, double> ResourceCosts { get; set; }
    public required string BudgetConstraints { get; set; }
    public bool AutoCostOptimization { get; set; }
    public Dictionary<string, object> CostPredictionModels { get; set; } = new();
    public TimeSpan CostMonitoringInterval { get; set; }
}

public class CostAwareScalingResult
{
    public required bool ScalingSuccessful { get; set; }
    public required double EstimatedCostImpact { get; set; }
    public required Dictionary<string, double> ResourceAllocation { get; set; }
    public required DateTime ScalingTimestamp { get; set; }
    public required string CostJustification { get; set; }
    public Dictionary<string, double> CostSavings { get; set; } = new();
    public string? BudgetWarnings { get; set; }
}

public class RegionAwareScalingConfiguration
{
    public required string ConfigurationId { get; set; }
    public required List<string> TargetRegions { get; set; }
    public required Dictionary<string, object> RegionalPolicies { get; set; }
    public required Dictionary<string, double> RegionalCostFactors { get; set; }
    public required string ScalingPrecedence { get; set; }
    public bool ComplianceAware { get; set; }
    public Dictionary<string, object> RegionalConstraints { get; set; } = new();
}

public class RegionAwareScalingResult
{
    public required bool ScalingSuccessful { get; set; }
    public required Dictionary<string, int> RegionalAllocation { get; set; }
    public required List<string> ActiveRegions { get; set; }
    public required DateTime ScalingTimestamp { get; set; }
    public required Dictionary<string, object> ComplianceStatus { get; set; }
    public Dictionary<string, double> RegionalCosts { get; set; } = new();
    public List<string> RegionalWarnings { get; set; } = new();
}

public class InterRegionCommunicationConfiguration
{
    public required string ConfigurationId { get; set; }
    public required List<string> ParticipatingRegions { get; set; }
    public required Dictionary<string, object> CommunicationProtocols { get; set; }
    public required string EncryptionLevel { get; set; }
    public required TimeSpan SynchronizationInterval { get; set; }
    public bool DataResiliencyEnabled { get; set; }
    public Dictionary<string, object> NetworkConfiguration { get; set; } = new();
}

public class InterRegionCommunicationResult
{
    public required bool CommunicationEstablished { get; set; }
    public required Dictionary<string, string> RegionConnectivity { get; set; }
    public required List<string> ActiveConnections { get; set; }
    public required DateTime CommunicationTimestamp { get; set; }
    public required Dictionary<string, double> LatencyMetrics { get; set; }
    public Dictionary<string, object> DataSynchronizationStatus { get; set; } = new();
    public string? CommunicationIssues { get; set; }
}

public class MultiRegionConsistencyValidationRequest
{
    public required string ValidationId { get; set; }
    public required List<string> RegionsToValidate { get; set; }
    public required Dictionary<string, object> ValidationCriteria { get; set; }
    public required string ConsistencyLevel { get; set; }
    public required DateTime ValidationTimestamp { get; set; }
    public Dictionary<string, object> ValidationParameters { get; set; } = new();
    public bool ForceValidation { get; set; }
}

public class MultiRegionConsistencyValidationResult
{
    public required bool ConsistencyValidated { get; set; }
    public required Dictionary<string, bool> RegionConsistencyStatus { get; set; }
    public required List<string> InconsistentRegions { get; set; }
    public required DateTime ValidationTimestamp { get; set; }
    public required Dictionary<string, object> ConsistencyMetrics { get; set; }
    public Dictionary<string, string> RecommendedActions { get; set; } = new();
    public List<string> DataIntegrityIssues { get; set; } = new();
}
