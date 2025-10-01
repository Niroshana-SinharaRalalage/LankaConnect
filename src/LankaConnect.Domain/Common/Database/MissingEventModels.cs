using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Domain.Common.Database;

/// <summary>
/// Missing model types referenced by Application layer interfaces
/// </summary>

// Cultural Event Load Distribution Models - CulturalEventLoadDistributionRequest/Response already exist in GeographicCulturalRoutingModels.cs

public class PredictiveScalingPlanRequest
{
    public CulturalEventType EventType { get; set; }
    public DateTime EventDateTime { get; set; }
    public string[] GeographicRegions { get; set; } = Array.Empty<string>();
    public bool UseHistoricalData { get; set; }
    public bool UseMachineLearning { get; set; }
}

public class PredictiveScalingPlan
{
    public CulturalEventType EventType { get; set; }
    public DateTime EventDateTime { get; set; }
    public decimal PredictedTrafficMultiplier { get; set; }
    public TimeSpan PredictedDuration { get; set; }
    public ScalingRecommendation[] Recommendations { get; set; } = Array.Empty<ScalingRecommendation>();
    public decimal ConfidenceLevel { get; set; }
}

public class ScalingRecommendation
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int RecommendedServerCount { get; set; }
    public string ResourceType { get; set; } = string.Empty;
    public string Rationale { get; set; } = string.Empty;
}

// Cultural Event Conflict Resolution Models
public class CulturalEventConflictResolutionRequest
{
    public CulturalEventSchedule[] ConflictingEvents { get; set; } = Array.Empty<CulturalEventSchedule>();
    public int TotalAvailableCapacity { get; set; }
    public bool UseAutomaticResolution { get; set; }
    public string[] PreferredCommunities { get; set; } = Array.Empty<string>();
}

public class CulturalEventConflictResolution
{
    public bool IsResolved { get; set; }
    public ResourceAllocation[] ResourceAllocations { get; set; } = Array.Empty<ResourceAllocation>();
    public string ResolutionStrategy { get; set; } = string.Empty;
    public string[] AffectedCommunities { get; set; } = Array.Empty<string>();
    public string ResolutionRationale { get; set; } = string.Empty;
}

// CulturalEventSchedule is now defined in AdditionalMissingModels.cs

public class ResourceAllocation
{
    public string ResourceId { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public int AllocatedCapacity { get; set; }
    public decimal AllocationPercentage { get; set; }
    public CulturalEventType AssignedEventType { get; set; }
    public string Rationale { get; set; } = string.Empty;
}

// Geographic and Cultural Scope Models
public class GeographicCulturalScope
{
    public string Region { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string[] CulturalCommunities { get; set; } = Array.Empty<string>();
    public decimal CulturalDensity { get; set; }
}

// Fortune 500 Performance Models
public class FortuneHundredPerformanceMonitoringRequest
{
    public CulturalEventType EventType { get; set; }
    public DateTime MonitoringStartTime { get; set; }
    public DateTime MonitoringEndTime { get; set; }
    public string[] MetricsToTrack { get; set; } = Array.Empty<string>();
    public bool RealTimeMonitoring { get; set; }
}

public class FortuneHundredPerformanceMetrics
{
    public TimeSpan AverageResponseTime { get; set; }
    public decimal UptimePercentage { get; set; }
    public int ThroughputPerSecond { get; set; }
    public decimal ErrorRatePercentage { get; set; }
    public bool SlaCompliant { get; set; }
    public DateTime MeasurementTimestamp { get; set; }
    public string[] PerformanceIssues { get; set; } = Array.Empty<string>();
}

// Auto Scaling Models
public class DiasporaContext
{
    public string CommunityId { get; set; } = string.Empty;
    public string GeographicRegion { get; set; } = string.Empty;
    public int CommunitySize { get; set; }
    public decimal EngagementLevel { get; set; }
    public CulturalEventType[] PreferredEvents { get; set; } = Array.Empty<CulturalEventType>();
}

public class DiasporaEngagementPrediction
{
    public string CommunityId { get; set; } = string.Empty;
    public decimal PredictedEngagementScore { get; set; }
    public int PredictedUserCount { get; set; }
    public DateTime PredictionDateTime { get; set; }
    public decimal ConfidenceLevel { get; set; }
    public string[] EngagementFactors { get; set; } = Array.Empty<string>();
}

public class ConnectionPoolInitializationResult
{
    public bool IsSuccessful { get; set; }
    public int InitialPoolSize { get; set; }
    public int MaxPoolSize { get; set; }
    public TimeSpan InitializationTime { get; set; }
    public string[] Warnings { get; set; } = Array.Empty<string>();
}

// Security Models
public class SecurityPolicySet
{
    public string PolicyId { get; set; } = string.Empty;
    public string[] SecurityRules { get; set; } = Array.Empty<string>();
    public string CulturalContext { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
    public bool IsActive { get; set; }
}

public class MultiCulturalSecurityResult
{
    public bool IsSecurityCompliant { get; set; }
    public string[] AppliedPolicies { get; set; } = Array.Empty<string>();
    public string[] SecurityViolations { get; set; } = Array.Empty<string>();
    public string[] CulturalConsiderations { get; set; } = Array.Empty<string>();
}

public class CulturalContent
{
    public string ContentId { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string CulturalContext { get; set; } = string.Empty;
    public string[] Tags { get; set; } = Array.Empty<string>();
    public decimal SensitivityScore { get; set; }
}

public class SecurityValidationCriteria
{
    public string[] RequiredPolicies { get; set; } = Array.Empty<string>();
    public string CulturalSensitivityLevel { get; set; } = string.Empty;
    public bool RequiresCulturalApproval { get; set; }
    public string[] ExclusionRules { get; set; } = Array.Empty<string>();
}

public class CulturalContentSecurityResult
{
    public bool IsApproved { get; set; }
    public string[] AppliedSecurityMeasures { get; set; } = Array.Empty<string>();
    public string[] CulturalWarnings { get; set; } = Array.Empty<string>();
    public decimal SecurityScore { get; set; }
}

// Additional missing models from the error list
public class PoolOptimizationStrategy
{
    public string StrategyName { get; set; } = string.Empty;
    public string[] OptimizationParameters { get; set; } = Array.Empty<string>();
    public decimal EfficiencyTarget { get; set; }
    public bool IsAdaptive { get; set; }
}

public class PoolOptimizationResult
{
    public bool IsSuccessful { get; set; }
    public decimal EfficiencyImprovement { get; set; }
    public string[] AppliedOptimizations { get; set; } = Array.Empty<string>();
    public TimeSpan OptimizationDuration { get; set; }
}

public class HealthCheckParameters
{
    public TimeSpan CheckInterval { get; set; }
    public string[] HealthMetrics { get; set; } = Array.Empty<string>();
    public decimal ThresholdValues { get; set; }
    public bool EnableAutoHealing { get; set; }
}

public class PoolHealthMetrics
{
    public decimal OverallHealthScore { get; set; }
    public int ActiveConnections { get; set; }
    public int IdleConnections { get; set; }
    public TimeSpan AverageConnectionTime { get; set; }
    public string[] HealthIssues { get; set; } = Array.Empty<string>();
}

public class PoolScalingParameters
{
    public int MinPoolSize { get; set; }
    public int MaxPoolSize { get; set; }
    public decimal ScalingTriggerThreshold { get; set; }
    public TimeSpan ScalingCooldown { get; set; }
    public bool EnablePredictiveScaling { get; set; }
}

public class PoolScalingResult
{
    public bool IsSuccessful { get; set; }
    public int NewPoolSize { get; set; }
    public string ScalingReason { get; set; } = string.Empty;
    public TimeSpan ScalingDuration { get; set; }
    public decimal PerformanceImpact { get; set; }
}

public class LifecycleConfiguration
{
    public TimeSpan MaxConnectionLifetime { get; set; }
    public TimeSpan IdleTimeout { get; set; }
    public bool EnableConnectionValidation { get; set; }
    public string[] MaintenanceTasks { get; set; } = Array.Empty<string>();
}

public class LifecycleManagementResult
{
    public bool IsSuccessful { get; set; }
    public int ConnectionsRecycled { get; set; }
    public int ConnectionsValidated { get; set; }
    public string[] MaintenanceActions { get; set; } = Array.Empty<string>();
    public TimeSpan MaintenanceDuration { get; set; }
}

public class CulturalRequirements
{
    public string[] RequiredCulturalSupport { get; set; } = Array.Empty<string>();
    public string[] CulturalConstraints { get; set; } = Array.Empty<string>();
    public decimal CulturalComplianceLevel { get; set; }
    public bool RequiresCulturalValidation { get; set; }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public string[] ValidationErrors { get; set; } = Array.Empty<string>();
    public string[] ValidationWarnings { get; set; } = Array.Empty<string>();
    public decimal ValidationScore { get; set; }
}