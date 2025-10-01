using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Domain.Common.Database;

/// <summary>
/// Event argument classes for various system events
/// </summary>

public class CulturalScalingTriggeredEventArgs : EventArgs
{
    public CulturalEventType EventType { get; set; }
    public decimal ScalingFactor { get; set; }
    public string Region { get; set; } = string.Empty;
    public DateTime TriggeredAt { get; set; }
    public string Reason { get; set; } = string.Empty;
}

public class ConnectionPoolScalingEventArgs : EventArgs
{
    public int OldPoolSize { get; set; }
    public int NewPoolSize { get; set; }
    public string ScalingDirection { get; set; } = string.Empty; // UP, DOWN
    public string Reason { get; set; } = string.Empty;
    public DateTime ScalingTime { get; set; }
    public TimeSpan ScalingDuration { get; set; }
}

public class PerformanceThresholdBreachedEventArgs : EventArgs
{
    public string MetricName { get; set; } = string.Empty;
    public decimal ThresholdValue { get; set; }
    public decimal ActualValue { get; set; }
    public string Severity { get; set; } = string.Empty;
    public DateTime BreachedAt { get; set; }
    public string[] AffectedServices { get; set; } = Array.Empty<string>();
}

public class SLAComplianceStatusChangedEventArgs : EventArgs
{
    public bool IsCompliant { get; set; }
    public decimal CompliancePercentage { get; set; }
    public string[] ViolatedMetrics { get; set; } = Array.Empty<string>();
    public DateTime StatusChangedAt { get; set; }
    public string ClientId { get; set; } = string.Empty;
}

public class MultiRegionCoordinationEventArgs : EventArgs
{
    public string[] Regions { get; set; } = Array.Empty<string>();
    public string CoordinationType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CoordinationTime { get; set; }
    public Dictionary<string, object> CoordinationMetadata { get; set; } = new();
}

public class RevenueOptimizationEventArgs : EventArgs
{
    public decimal PreviousRevenue { get; set; }
    public decimal OptimizedRevenue { get; set; }
    public decimal ImprovementPercentage { get; set; }
    public string OptimizationStrategy { get; set; } = string.Empty;
    public DateTime OptimizationTime { get; set; }
    public string[] AppliedOptimizations { get; set; } = Array.Empty<string>();
}

public class ErrorHandlingActivatedEventArgs : EventArgs
{
    public string ErrorType { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string HandlingStrategy { get; set; } = string.Empty;
    public DateTime ErrorOccurredAt { get; set; }
    public string[] AffectedComponents { get; set; } = Array.Empty<string>();
    public bool WasHandledSuccessfully { get; set; }
}

// Additional missing model types
public class CulturalScalingParameters
{
    public CulturalEventType EventType { get; set; }
    public decimal MinScalingFactor { get; set; }
    public decimal MaxScalingFactor { get; set; }
    public TimeSpan ScalingWindow { get; set; }
    public string[] AllowedRegions { get; set; } = Array.Empty<string>();
    public bool EnablePredictiveScaling { get; set; }
    public Dictionary<string, object> AdditionalParameters { get; set; } = new();
}

public class PolicyConfigurationResult
{
    public bool IsSuccessful { get; set; }
    public string PolicyId { get; set; } = string.Empty;
    public string[] AppliedPolicies { get; set; } = Array.Empty<string>();
    public string[] ValidationErrors { get; set; } = Array.Empty<string>();
    public DateTime ConfigurationTime { get; set; }
    public string ConfigurationDetails { get; set; } = string.Empty;
}

public class CulturalEventScalingPolicy
{
    public string PolicyId { get; set; } = string.Empty;
    public CulturalEventType EventType { get; set; }
    public decimal DefaultScalingFactor { get; set; }
    public Dictionary<string, decimal> RegionSpecificFactors { get; set; } = new();
    public TimeSpan CooldownPeriod { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastModified { get; set; }
}