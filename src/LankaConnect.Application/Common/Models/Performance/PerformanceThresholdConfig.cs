using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.Models.Performance;

/// <summary>
/// Performance threshold configuration for monitoring and alerting
/// Defines performance benchmarks and trigger points for system health
/// </summary>
public class PerformanceThresholdConfig
{
    public required string ConfigId { get; set; }
    public required string ConfigName { get; set; }
    public required PerformanceMetricType MetricType { get; set; }
    public required Dictionary<string, decimal> Thresholds { get; set; }
    public required PerformanceThresholdSeverity DefaultSeverity { get; set; }
    public required TimeSpan EvaluationWindow { get; set; }
    public required int SampleSize { get; set; }
    public required bool IsEnabled { get; set; }
    public required List<string> ApplicableEndpoints { get; set; }
    public required Dictionary<string, object> ConfigurationParameters { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? Description { get; set; }
    public List<string> NotificationChannels { get; set; } = new();
    public Dictionary<string, string> Tags { get; set; } = new();

    public PerformanceThresholdConfig()
    {
        Thresholds = new Dictionary<string, decimal>();
        ApplicableEndpoints = new List<string>();
        ConfigurationParameters = new Dictionary<string, object>();
        NotificationChannels = new List<string>();
        Tags = new Dictionary<string, string>();
    }

    public bool IsThresholdExceeded(string metricName, decimal value)
    {
        return Thresholds.ContainsKey(metricName) && value > Thresholds[metricName];
    }

    public bool AppliesToEndpoint(string endpoint)
    {
        return ApplicableEndpoints.Contains(endpoint) || ApplicableEndpoints.Contains("*");
    }
}

/// <summary>
/// Performance metric type enumeration
/// </summary>
public enum PerformanceMetricType
{
    ResponseTime = 1,
    Throughput = 2,
    ErrorRate = 3,
    CpuUtilization = 4,
    MemoryUtilization = 5,
    DatabaseConnections = 6,
    CacheHitRate = 7,
    CulturalContentAccuracy = 8
}

/// <summary>
/// Performance threshold severity levels
/// </summary>
public enum PerformanceThresholdSeverity
{
    Info = 1,
    Warning = 2,
    Error = 3,
    Critical = 4,
    Emergency = 5
}