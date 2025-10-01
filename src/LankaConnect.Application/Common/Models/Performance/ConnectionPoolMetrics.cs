namespace LankaConnect.Application.Common.Models.Performance;

/// <summary>
/// Metrics for database connection pool performance monitoring
/// </summary>
public class ConnectionPoolMetrics
{
    public required string PoolId { get; set; }
    public required string PoolName { get; set; }
    public required int TotalConnections { get; set; }
    public required int ActiveConnections { get; set; }
    public required int IdleConnections { get; set; }
    public required int PendingRequests { get; set; }
    public required double ConnectionUtilizationPercentage { get; set; }
    public required TimeSpan AverageConnectionAcquisitionTime { get; set; }
    public required TimeSpan MaxConnectionAcquisitionTime { get; set; }
    public required int SuccessfulConnections { get; set; }
    public required int FailedConnections { get; set; }
    public required int TimeoutConnections { get; set; }
    public required DateTime MetricsTimestamp { get; set; }
    public Dictionary<string, object> AdditionalMetrics { get; set; } = new();
    public List<string> PerformanceWarnings { get; set; } = new();
}

/// <summary>
/// Connection pool health status
/// </summary>
public enum ConnectionPoolHealth
{
    Healthy,
    Warning,
    Critical,
    Offline
}

/// <summary>
/// Detailed connection pool status
/// </summary>
public class ConnectionPoolStatus
{
    public required string PoolId { get; set; }
    public required ConnectionPoolHealth Health { get; set; }
    public required ConnectionPoolMetrics Metrics { get; set; }
    public required bool IsOperational { get; set; }
    public required DateTime LastHealthCheck { get; set; }
    public List<string> HealthIssues { get; set; } = new();
    public Dictionary<string, object> ConfigurationSettings { get; set; } = new();
}

/// <summary>
/// Enterprise-level connection pool metrics with advanced monitoring capabilities
/// </summary>
public class EnterpriseConnectionPoolMetrics : ConnectionPoolMetrics
{
    public required Dictionary<string, int> RegionalConnectionCounts { get; set; } = new();
    public required List<string> CulturalAffinityGroups { get; set; } = new();
    public required decimal CulturalLoadBalancingEfficiency { get; set; }
    public required TimeSpan CrossRegionLatency { get; set; }
    public required bool DisasterRecoveryEnabled { get; set; }
    public required int BackupConnectionPools { get; set; }
    public required decimal FailoverSuccessRate { get; set; }
    public required Dictionary<string, object> EnterpriseCompliance { get; set; } = new();
    public required List<string> SecurityAuditFlags { get; set; } = new();
    public required bool CulturalDataProtectionCompliant { get; set; }
}