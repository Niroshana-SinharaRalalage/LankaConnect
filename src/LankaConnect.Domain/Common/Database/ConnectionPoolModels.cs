namespace LankaConnect.Domain.Common;

public enum DatabaseOperationType
{
    Read,
    Write,
    Analytics,
    Migration,
    Backup,
    Monitoring
}

public enum PoolHealthStatus
{
    Healthy,
    Warning,
    Critical,
    Failed,
    Maintenance
}

public enum ConnectionPoolOptimizationStrategy
{
    Conservative,
    Balanced,
    Aggressive,
    CulturallyIntelligent
}

public class CulturalConnectionPoolKey
{
    public string PoolId { get; set; } = string.Empty;
    public string CommunityGroup { get; set; } = string.Empty;
    public string GeographicRegion { get; set; } = string.Empty;
    public DatabaseOperationType OperationType { get; set; } = DatabaseOperationType.Read;
    public double LoadBalancingWeight { get; set; } = 1.0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();
}


public class PerformanceTarget
{
    public TimeSpan MaxConnectionAcquisitionTime { get; set; } = TimeSpan.FromMilliseconds(5);
    public double MinPoolEfficiency { get; set; } = 0.95;
    public TimeSpan MaxFailoverTime { get; set; } = TimeSpan.FromMilliseconds(100);
    public int MaxMemoryPerThousandConnections { get; set; } = 50;
    public int TargetThroughputPerSecond { get; set; } = 10000;
    public int MaxConcurrentUsers { get; set; } = 1000000;
}

public class ConnectionPoolConfiguration
{
    public string PoolName { get; set; } = string.Empty;
    public string CommunityGroup { get; set; } = string.Empty;
    public string GeographicRegion { get; set; } = string.Empty;
    public int MaxConnections { get; set; } = 100;
    public int MinConnections { get; set; } = 10;
    public TimeSpan ConnectionLifetime { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public bool EnableLoadBalancing { get; set; } = true;
    public bool EnableHealthChecking { get; set; } = true;
    public ConnectionPoolOptimizationStrategy OptimizationStrategy { get; set; } = ConnectionPoolOptimizationStrategy.CulturallyIntelligent;
}

public class ConnectionPoolHealth
{
    public string PoolId { get; set; } = string.Empty;
    public PoolHealthStatus Status { get; set; } = PoolHealthStatus.Healthy;
    public double HealthScore { get; set; } = 1.0;
    public List<string> PerformanceIssues { get; set; } = new();
    public List<string> RecommendedActions { get; set; } = new();
    public DateTime LastHealthCheck { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> DiagnosticData { get; set; } = new();
}

public class CulturalConnectionRoutingResult
{
    public string SelectedPoolId { get; set; } = string.Empty;
    public string RoutingReason { get; set; } = string.Empty;
    public TimeSpan EstimatedConnectionAcquisitionTime { get; set; }
    public double LoadBalancingScore { get; set; }
    public List<string> AlternativePoolIds { get; set; } = new();
    public Dictionary<string, double> RoutingMetrics { get; set; } = new();
}

public class ConnectionPoolOptimizationResult
{
    public List<string> OptimizationActions { get; set; } = new();
    public Dictionary<string, ConnectionPoolConfiguration> OptimizedConfigurations { get; set; } = new();
    public TimeSpan EstimatedOptimizationTime { get; set; }
    public double ExpectedPerformanceImprovement { get; set; }
    public List<string> RiskMitigations { get; set; } = new();
}

public class ConnectionPoolOptions
{
    public int MaxConnectionsPerPool { get; set; } = 100;
    public int MinConnectionsPerPool { get; set; } = 10;
    public TimeSpan ConnectionLifetime { get; set; } = TimeSpan.FromMinutes(30);
    public TimeSpan HealthCheckInterval { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxPoolsPerRegion { get; set; } = 5;
    public bool EnableCulturalIntelligenceRouting { get; set; } = true;
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public double PoolEfficiencyThreshold { get; set; } = 0.95;
    public ConnectionPoolOptimizationStrategy DefaultOptimizationStrategy { get; set; } = ConnectionPoolOptimizationStrategy.CulturallyIntelligent;
    public Dictionary<string, string> CulturalConnectionStrings { get; set; } = new();
}

public class CulturalPoolDistribution
{
    public string CommunityId { get; set; } = string.Empty;
    public string GeographicRegion { get; set; } = string.Empty;
    public int AllocatedPools { get; set; }
    public int TotalConnections { get; set; }
    public double LoadDistributionWeight { get; set; }
    public List<string> OptimizationRecommendations { get; set; } = new();
}

