using System;
using System.Collections.Generic;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Infrastructure.Database.LoadBalancing;

/// <summary>
/// Load Balancing Models - Infrastructure Layer
/// Moved from Stage5MissingTypes.cs to correct architectural layer
/// </summary>

#region Server and Database Management

/// <summary>
/// Server instance for load balancing and routing
/// </summary>
public class ServerInstance
{
    public string InstanceId { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public ServerStatus Status { get; set; }
    public double LoadPercentage { get; set; }
    public DateTime LastHealthCheck { get; set; }
}

/// <summary>
/// Domain-specific database instance reference
/// </summary>
public class DomainDatabase
{
    public string DatabaseId { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string ConnectionString { get; set; } = string.Empty;
    public DatabaseType DatabaseType { get; set; }
}

#endregion

#region Cultural Affinity Load Balancing

/// <summary>
/// Cached affinity score for performance optimization
/// </summary>
public class CachedAffinityScore
{
    public string ScoreId { get; set; } = string.Empty;
    public double AffinityScore { get; set; }
    public DateTime CachedAt { get; set; }
    public TimeSpan TimeToLive { get; set; }
    public string CulturalContext { get; set; } = string.Empty;
}

/// <summary>
/// Cultural affinity score collection for routing decisions
/// </summary>
public class CulturalAffinityScoreCollection
{
    public string CollectionId { get; set; } = string.Empty;
    public Dictionary<string, double> AffinityScores { get; set; } = new();
    public DateTime CalculatedAt { get; set; }
    public string BaseCulturalContext { get; set; } = string.Empty;
}

/// <summary>
/// Cultural load balancing metrics
/// </summary>
public class CulturalLoadBalancingMetrics
{
    public string MetricsId { get; set; } = string.Empty;
    public Dictionary<string, double> RegionalLoad { get; set; } = new();
    public double AverageCulturalAffinityScore { get; set; }
    public int RoutingDecisionsCount { get; set; }
    public DateTime MeasuredAt { get; set; }
}

#endregion

#region Supporting Enums

/// <summary>
/// Server status for load balancing
/// </summary>
public enum ServerStatus
{
    Online,
    Offline,
    Degraded,
    Maintenance,
    Failed
}

/// <summary>
/// Database type for infrastructure management
/// </summary>
public enum DatabaseType
{
    SqlServer,
    PostgreSQL,
    MySQL,
    MongoDB,
    Redis
}

#endregion
