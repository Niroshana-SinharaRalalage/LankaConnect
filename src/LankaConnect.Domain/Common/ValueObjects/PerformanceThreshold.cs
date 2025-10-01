namespace LankaConnect.Domain.Common.ValueObjects;

/// <summary>
/// Consolidated performance threshold value object with cultural awareness.
/// Supports both monitoring thresholds and auto-scaling configurations.
/// </summary>
public class PerformanceThreshold : ValueObject
{
    // Multi-threshold properties (for monitoring)
    public double WarningThreshold { get; }
    public double CriticalThreshold { get; }
    public double EmergencyThreshold { get; }
    
    // Auto-scaling properties
    public ScalingTriggerType? TriggerType { get; }
    public double ThresholdValue { get; }
    public ThresholdSeverity Severity { get; }
    public TimeSpan EvaluationWindow { get; }
    public int ConsecutiveBreaches { get; }
    
    // Cultural awareness
    public bool IsCulturallyAware { get; }
    public CulturalPerformanceThreshold? CulturalLevel { get; }

    private PerformanceThreshold(
        double warningThreshold,
        double criticalThreshold,
        double emergencyThreshold,
        CulturalPerformanceThreshold? culturalLevel = null,
        ScalingTriggerType? triggerType = null,
        double? thresholdValue = null,
        ThresholdSeverity severity = ThresholdSeverity.Medium,
        TimeSpan? evaluationWindow = null,
        int consecutiveBreaches = 2,
        bool isCulturallyAware = false)
    {
        // Validation for monitoring thresholds
        if (warningThreshold >= criticalThreshold)
            throw new ArgumentException("Warning threshold must be less than critical threshold");
        
        if (criticalThreshold >= emergencyThreshold)
            throw new ArgumentException("Critical threshold must be less than emergency threshold");

        // Validation for auto-scaling thresholds
        if (thresholdValue.HasValue && thresholdValue < 0)
            throw new ArgumentException("Threshold value cannot be negative");
        
        if (evaluationWindow.HasValue && evaluationWindow <= TimeSpan.Zero)
            throw new ArgumentException("Evaluation window must be positive");
        
        if (consecutiveBreaches < 1)
            throw new ArgumentException("Consecutive breaches must be at least 1");

        WarningThreshold = warningThreshold;
        CriticalThreshold = criticalThreshold;
        EmergencyThreshold = emergencyThreshold;
        CulturalLevel = culturalLevel;
        
        TriggerType = triggerType;
        ThresholdValue = thresholdValue ?? warningThreshold;
        Severity = severity;
        EvaluationWindow = evaluationWindow ?? TimeSpan.FromMinutes(5);
        ConsecutiveBreaches = consecutiveBreaches;
        IsCulturallyAware = isCulturallyAware;
    }

    /// <summary>
    /// Creates a monitoring performance threshold with cultural awareness
    /// </summary>
    public static PerformanceThreshold CreateMonitoringThreshold(
        double warningThreshold,
        double criticalThreshold,
        double emergencyThreshold,
        CulturalPerformanceThreshold culturalLevel)
    {
        return new PerformanceThreshold(
            warningThreshold,
            criticalThreshold,
            emergencyThreshold,
            culturalLevel,
            isCulturallyAware: true);
    }

    /// <summary>
    /// Creates an auto-scaling performance threshold
    /// </summary>
    public static PerformanceThreshold CreateScalingThreshold(
        ScalingTriggerType triggerType,
        double thresholdValue,
        ThresholdSeverity severity = ThresholdSeverity.Medium,
        TimeSpan? evaluationWindow = null,
        int consecutiveBreaches = 2,
        bool isCulturallyAware = false)
    {
        return new PerformanceThreshold(
            thresholdValue * 0.7, // Warning at 70% of threshold
            thresholdValue * 0.9, // Critical at 90% of threshold
            thresholdValue,       // Emergency at threshold
            null,
            triggerType,
            thresholdValue,
            severity,
            evaluationWindow,
            consecutiveBreaches,
            isCulturallyAware);
    }

    /// <summary>
    /// Gets the alert severity for a given value (monitoring use case)
    /// </summary>
    public AlertSeverity GetSeverityForValue(double value)
    {
        if (value >= EmergencyThreshold)
            return CulturalLevel == CulturalPerformanceThreshold.Sacred
                ? AlertSeverity.Sacred
                : AlertSeverity.Critical;

        if (value >= CriticalThreshold)
            return AlertSeverity.High;

        if (value >= WarningThreshold)
            return AlertSeverity.Medium;

        return AlertSeverity.Low;
    }

    /// <summary>
    /// Checks if the threshold is breached for auto-scaling
    /// </summary>
    public bool IsBreached(double currentValue)
    {
        return currentValue >= ThresholdValue;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return WarningThreshold;
        yield return CriticalThreshold;
        yield return EmergencyThreshold;
        yield return TriggerType ?? ScalingTriggerType.None;
        yield return ThresholdValue;
        yield return Severity;
        yield return EvaluationWindow;
        yield return ConsecutiveBreaches;
        yield return IsCulturallyAware;
        yield return CulturalLevel ?? CulturalPerformanceThreshold.General;
    }
}

/// <summary>
/// Cultural performance threshold levels for sacred event protection
/// </summary>
public enum CulturalPerformanceThreshold
{
    General = 1,
    Regional = 2,
    Cultural = 3,
    Religious = 4,
    Sacred = 5
}

/// <summary>
/// Scaling trigger types for auto-scaling thresholds
/// </summary>
public enum ScalingTriggerType
{
    None = 0,
    CpuUtilization = 1,
    MemoryUtilization = 2,
    ConnectionCount = 3,
    RequestRate = 4,
    ResponseTime = 5,
    ErrorRate = 6,
    CulturalEventLoad = 7,
    GeographicLoad = 8,
    CustomMetric = 9
}

/// <summary>
/// Threshold severity levels for PerformanceThreshold configuration
/// Different from AlertSeverity which is for actual alert notifications
/// </summary>
public enum ThresholdSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4,
    Emergency = 5
}


