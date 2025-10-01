using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.ValueObjects;
using LankaConnect.Domain.Shared;

namespace LankaConnect.Infrastructure.Common.Models;

/// <summary>
/// Infrastructure-specific connection pool metrics (Infrastructure Layer)
/// TDD GREEN Phase: Foundation types for Enterprise Connection Pool Service
/// </summary>
public class ConnectionPoolMetrics
{
    public string PoolId { get; set; } = string.Empty;
    public int ActiveConnections { get; set; }
    public int IdleConnections { get; set; }
    public int MaxConnections { get; set; }
    public double AverageConnectionTime { get; set; }
    public double ConnectionSuccessRate { get; set; }
    public DateTime LastHealthCheck { get; set; }
    public ConnectionPoolHealthStatus HealthStatus { get; set; }
    public Dictionary<string, double> PerformanceMetrics { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// Enterprise-wide connection pool metrics aggregation
/// </summary>
public class EnterpriseConnectionPoolMetrics
{
    public string SystemId { get; set; } = string.Empty;
    public Dictionary<string, ConnectionPoolMetrics> PoolMetrics { get; set; } = new();
    public SystemHealthOverview SystemHealth { get; set; } = new();
    public PerformanceStatistics OverallPerformance { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
    public List<SystemAlert> ActiveAlerts { get; set; } = new();
}

/// <summary>
/// System health overview for infrastructure monitoring
/// </summary>
public class SystemHealthOverview
{
    public SystemHealthStatus OverallHealth { get; set; }
    public int TotalPools { get; set; }
    public int HealthyPools { get; set; }
    public int WarningPools { get; set; }
    public int CriticalPools { get; set; }
    public double SystemUtilization { get; set; }
}

/// <summary>
/// Performance statistics aggregation
/// </summary>
public class PerformanceStatistics
{
    public double AverageResponseTime { get; set; }
    public double ThroughputPerSecond { get; set; }
    public double ErrorRate { get; set; }
    public Dictionary<string, double> RegionalMetrics { get; set; } = new();
    public Dictionary<string, double> CulturalGroupMetrics { get; set; } = new();
}

/// <summary>
/// System alert for infrastructure monitoring
/// </summary>
public class SystemAlert
{
    public string AlertId { get; set; } = Guid.NewGuid().ToString();
    public AlertSeverity Severity { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Component { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Connection pool health status enumeration
/// </summary>
public enum ConnectionPoolHealthStatus
{
    Healthy = 0,
    Warning = 1,
    Critical = 2,
    Offline = 3
}

