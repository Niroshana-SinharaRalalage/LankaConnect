using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Domain.Common.Monitoring;

/// <summary>
/// Cultural Intelligence endpoint configuration for monitoring and metrics collection
/// Represents endpoint-specific configuration for cultural data processing
/// </summary>
public class CulturalIntelligenceEndpoint : Entity<string>
{
    public required string EndpointName { get; set; }
    public required string EndpointUrl { get; set; }
    public required CulturalIntelligenceEndpointType EndpointType { get; set; }
    public required CulturalIntelligenceEndpointStatus Status { get; set; }
    public required List<string> SupportedCulturalContexts { get; set; }
    public required Dictionary<string, object> EndpointConfiguration { get; set; }
    public required CulturalIntelligenceSecurityLevel SecurityLevel { get; set; }
    public required TimeSpan ResponseTimeThreshold { get; set; }
    public required int MaxConcurrentConnections { get; set; }
    public required bool IsLoadBalanced { get; set; }
    public required List<string> AssignedRegions { get; set; }
    public DateTime LastHealthCheck { get; set; }
    public new DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public new DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public Dictionary<string, object> EndpointMetadata { get; set; } = new();

    private CulturalIntelligenceEndpoint() : base(string.Empty) { }

    public CulturalIntelligenceEndpoint(string endpointId) : base(endpointId)
    {
        SupportedCulturalContexts = new List<string>();
        EndpointConfiguration = new Dictionary<string, object>();
        AssignedRegions = new List<string>();
        EndpointMetadata = new Dictionary<string, object>();
    }

    public bool IsHealthy => Status == CulturalIntelligenceEndpointStatus.Healthy
                          && LastHealthCheck > DateTime.UtcNow.AddMinutes(-5);

    public bool SupportsRegion(string region) => AssignedRegions.Contains(region);

    public bool SupportsCulturalContext(string culturalContext) =>
        SupportedCulturalContexts.Contains(culturalContext);
}

/// <summary>
/// Cultural Intelligence alert type for monitoring and notifications
/// </summary>
public enum CulturalIntelligenceAlertType
{
    Information = 1,
    Warning = 2,
    Error = 3,
    Critical = 4,
    CulturalSensitivity = 5,
    PerformanceDegradation = 6,
    SecurityThreat = 7
}

/// <summary>
/// Cultural Intelligence endpoint type classification
/// </summary>
public enum CulturalIntelligenceEndpointType
{
    DataProcessing = 1,
    Analytics = 2,
    Monitoring = 3,
    ApiGateway = 4,
    CulturalContent = 5,
    MachineLearning = 6,
    EventProcessing = 7
}

/// <summary>
/// Cultural Intelligence endpoint status
/// </summary>
public enum CulturalIntelligenceEndpointStatus
{
    Healthy = 1,
    Degraded = 2,
    Unhealthy = 3,
    Offline = 4,
    Maintenance = 5
}

/// <summary>
/// Cultural Intelligence security level for endpoint access control
/// </summary>
public enum CulturalIntelligenceSecurityLevel
{
    Public = 1,
    Internal = 2,
    Confidential = 3,
    Restricted = 4,
    Sacred = 5
}