namespace LankaConnect.Application.Common.Models.Performance;

/// <summary>
/// Represents the health status of a system component with cultural intelligence context
/// </summary>
public class ComponentHealth
{
    /// <summary>
    /// Name of the component
    /// </summary>
    public string ComponentName { get; set; } = string.Empty;

    /// <summary>
    /// Overall health status (Healthy, Degraded, Critical, Unknown)
    /// </summary>
    public string HealthStatus { get; set; } = "Unknown";

    /// <summary>
    /// Health score from 0 (critical) to 100 (perfect health)
    /// </summary>
    public int HealthScore { get; set; }

    /// <summary>
    /// Timestamp of last health check
    /// </summary>
    public DateTime LastCheckTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Cultural events currently affecting this component's health
    /// </summary>
    public List<string> CulturalInfluences { get; set; } = new();

    /// <summary>
    /// Performance metrics contributing to health assessment
    /// </summary>
    public Dictionary<string, decimal> PerformanceMetrics { get; set; } = new();

    /// <summary>
    /// Identified issues affecting component health
    /// </summary>
    public List<string> HealthIssues { get; set; } = new();

    /// <summary>
    /// Recommended actions to improve health
    /// </summary>
    public List<string> RecommendedActions { get; set; } = new();

    /// <summary>
    /// Next scheduled health check time
    /// </summary>
    public DateTime NextCheckTime { get; set; }
}