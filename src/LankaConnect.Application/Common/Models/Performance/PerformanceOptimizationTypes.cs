using System;
using System.Collections.Generic;
using System.Linq;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.Monitoring;

namespace LankaConnect.Application.Common.Models.Performance;

/// <summary>
/// TDD GREEN Phase: Performance Optimization Types Implementation
/// Cultural intelligence integrated performance optimization for LankaConnect platform
/// </summary>

#region SystemHealthValidation

/// <summary>
/// System health validation with cultural intelligence awareness
/// </summary>
public class SystemHealthValidation
{
    public string ValidationId { get; private set; }
    public string ValidationName { get; private set; }
    public IReadOnlyList<string> ValidatedSystems { get; private set; }
    public Dictionary<string, HealthStatus> SystemHealthStatuses { get; private set; }
    public double OverallHealthScore { get; private set; }
    public IReadOnlyList<string> CulturalConsiderations { get; private set; }
    public DateTime ValidatedAt { get; private set; }

    /// <summary>
    /// Gets whether all systems are healthy
    /// </summary>
    public bool AllSystemsHealthy => SystemHealthStatuses.All(kvp => kvp.Value == HealthStatus.Healthy);

    /// <summary>
    /// Gets whether cultural intelligence systems are healthy
    /// </summary>
    public bool CulturalSystemsHealthy => SystemHealthStatuses
        .Where(kvp => kvp.Key.Contains("Cultural", StringComparison.OrdinalIgnoreCase))
        .All(kvp => kvp.Value == HealthStatus.Healthy);

    /// <summary>
    /// Gets systems requiring attention
    /// </summary>
    public IReadOnlyList<string> SystemsRequiringAttention => SystemHealthStatuses
        .Where(kvp => kvp.Value != HealthStatus.Healthy)
        .Select(kvp => kvp.Key)
        .ToList()
        .AsReadOnly();

    private SystemHealthValidation(string validationName, IEnumerable<string> validatedSystems,
        Dictionary<string, HealthStatus> systemHealthStatuses, IEnumerable<string> culturalConsiderations)
    {
        ValidationId = Guid.NewGuid().ToString();
        ValidationName = validationName;
        ValidatedSystems = validatedSystems.ToList().AsReadOnly();
        SystemHealthStatuses = systemHealthStatuses;
        CulturalConsiderations = culturalConsiderations.ToList().AsReadOnly();
        ValidatedAt = DateTime.UtcNow;

        // Calculate overall health score
        var healthyCount = systemHealthStatuses.Count(kvp => kvp.Value == HealthStatus.Healthy);
        OverallHealthScore = systemHealthStatuses.Count > 0 ? 
            (double)healthyCount / systemHealthStatuses.Count * 100.0 : 0.0;
    }

    /// <summary>
    /// Creates system health validation
    /// </summary>
    public static SystemHealthValidation Create(string validationName, IEnumerable<string> validatedSystems,
        Dictionary<string, HealthStatus> systemHealthStatuses, IEnumerable<string>? culturalConsiderations = null)
    {
        return new SystemHealthValidation(validationName, validatedSystems, systemHealthStatuses,
            culturalConsiderations ?? Array.Empty<string>());
    }
}

/// <summary>
/// Health status levels
/// </summary>
public enum HealthStatus
{
    Healthy,
    Warning,
    Critical,
    Unknown
}

#endregion

#region RevenueProtectionResult

/// <summary>
/// Revenue protection result with cultural event impact analysis
/// </summary>
public class RevenueProtectionResult
{
    public string ResultId { get; private set; }
    public string ProtectionStrategyId { get; private set; }
    public decimal ProtectedRevenue { get; private set; }
    public decimal PotentialLoss { get; private set; }
    public double ProtectionEffectiveness { get; private set; }
    public IReadOnlyList<string> ProtectedServices { get; private set; }
    public CulturalEventType? AssociatedCulturalEvent { get; private set; }
    public DateTime ExecutedAt { get; private set; }

    /// <summary>
    /// Gets revenue protection percentage
    /// </summary>
    public double RevenueProtectionPercentage => PotentialLoss > 0 ? 
        (double)(ProtectedRevenue / PotentialLoss) * 100.0 : 100.0;

    /// <summary>
    /// Gets whether this result is for cultural event protection
    /// </summary>
    public bool IsCulturalEventProtection => AssociatedCulturalEvent.HasValue;

    /// <summary>
    /// Gets whether protection meets Fortune 500 standards
    /// </summary>
    public bool MeetsFortune500Standards => ProtectionEffectiveness >= 0.999; // 99.9%

    private RevenueProtectionResult(string protectionStrategyId, decimal protectedRevenue, decimal potentialLoss,
        double protectionEffectiveness, IEnumerable<string> protectedServices, CulturalEventType? associatedCulturalEvent)
    {
        ResultId = Guid.NewGuid().ToString();
        ProtectionStrategyId = protectionStrategyId;
        ProtectedRevenue = protectedRevenue;
        PotentialLoss = potentialLoss;
        ProtectionEffectiveness = protectionEffectiveness;
        ProtectedServices = protectedServices.ToList().AsReadOnly();
        AssociatedCulturalEvent = associatedCulturalEvent;
        ExecutedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates revenue protection result
    /// </summary>
    public static RevenueProtectionResult Create(string protectionStrategyId, decimal protectedRevenue,
        decimal potentialLoss, double protectionEffectiveness, IEnumerable<string> protectedServices,
        CulturalEventType? associatedCulturalEvent = null)
    {
        return new RevenueProtectionResult(protectionStrategyId, protectedRevenue, potentialLoss,
            protectionEffectiveness, protectedServices, associatedCulturalEvent);
    }
}

#endregion

#region ReportingPeriod

/// <summary>
/// Reporting period with cultural calendar integration
/// </summary>
public class ReportingPeriod
{
    public string PeriodId { get; private set; }
    public string PeriodName { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public ReportingFrequency Frequency { get; private set; }
    public IReadOnlyList<CulturalEventType> IncludedCulturalEvents { get; private set; }
    public TimeZoneInfo TimeZone { get; private set; }

    /// <summary>
    /// Gets the duration of the reporting period
    /// </summary>
    public TimeSpan Duration => EndDate.Subtract(StartDate);

    /// <summary>
    /// Gets whether the period includes cultural events
    /// </summary>
    public bool IncludesCulturalEvents => IncludedCulturalEvents.Any();

    /// <summary>
    /// Gets whether the period is currently active
    /// </summary>
    public bool IsActive
    {
        get
        {
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZone);
            return now >= StartDate && now <= EndDate;
        }
    }

    /// <summary>
    /// Gets days remaining in the period
    /// </summary>
    public int DaysRemaining => IsActive ? (int)Math.Ceiling((EndDate - DateTime.UtcNow).TotalDays) : 0;

    private ReportingPeriod(string periodName, DateTime startDate, DateTime endDate,
        ReportingFrequency frequency, IEnumerable<CulturalEventType> includedCulturalEvents, TimeZoneInfo timeZone)
    {
        PeriodId = Guid.NewGuid().ToString();
        PeriodName = periodName;
        StartDate = startDate;
        EndDate = endDate;
        Frequency = frequency;
        IncludedCulturalEvents = includedCulturalEvents.ToList().AsReadOnly();
        TimeZone = timeZone;
    }

    /// <summary>
    /// Creates reporting period
    /// </summary>
    public static ReportingPeriod Create(string periodName, DateTime startDate, DateTime endDate,
        ReportingFrequency frequency, IEnumerable<CulturalEventType>? includedCulturalEvents = null,
        TimeZoneInfo? timeZone = null)
    {
        return new ReportingPeriod(periodName, startDate, endDate, frequency,
            includedCulturalEvents ?? Array.Empty<CulturalEventType>(),
            timeZone ?? TimeZoneInfo.Utc);
    }

    /// <summary>
    /// Creates quarterly reporting period with cultural events
    /// </summary>
    public static ReportingPeriod CreateQuarterly(string periodName, DateTime quarterStart,
        IEnumerable<CulturalEventType>? culturalEvents = null)
    {
        var quarterEnd = quarterStart.AddMonths(3).AddDays(-1);
        return Create(periodName, quarterStart, quarterEnd, ReportingFrequency.Quarterly,
            culturalEvents, TimeZoneInfo.Local);
    }
}

/// <summary>
/// Reporting frequency options
/// </summary>
public enum ReportingFrequency
{
    Daily,
    Weekly,
    Monthly,
    Quarterly,
    Annually,
    CulturalEventBased
}

#endregion

#region OptimizationObjective

/// <summary>
/// Optimization objective with cultural intelligence considerations
/// </summary>
public class OptimizationObjective
{
    public string ObjectiveId { get; private set; }
    public string ObjectiveName { get; private set; }
    public OptimizationTarget Target { get; private set; }
    public Dictionary<string, double> TargetMetrics { get; private set; }
    public IReadOnlyList<string> OptimizationScopes { get; private set; }
    public OptimizationPriority Priority { get; private set; }
    public CulturalEventType? CulturalContext { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets whether this is a cultural event optimization
    /// </summary>
    public bool IsCulturalOptimization => CulturalContext.HasValue;

    /// <summary>
    /// Gets priority score for optimization scheduling
    /// </summary>
    public int PriorityScore => Priority switch
    {
        OptimizationPriority.Critical => 100,
        OptimizationPriority.CulturalEvent => 90,
        OptimizationPriority.High => 75,
        OptimizationPriority.Medium => 50,
        OptimizationPriority.Low => 25,
        _ => 0
    };

    /// <summary>
    /// Gets whether objective has quantifiable targets
    /// </summary>
    public bool HasQuantifiableTargets => TargetMetrics.Any();

    private OptimizationObjective(string objectiveName, OptimizationTarget target,
        Dictionary<string, double> targetMetrics, IEnumerable<string> optimizationScopes,
        OptimizationPriority priority, CulturalEventType? culturalContext)
    {
        ObjectiveId = Guid.NewGuid().ToString();
        ObjectiveName = objectiveName;
        Target = target;
        TargetMetrics = targetMetrics;
        OptimizationScopes = optimizationScopes.ToList().AsReadOnly();
        Priority = priority;
        CulturalContext = culturalContext;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates optimization objective
    /// </summary>
    public static OptimizationObjective Create(string objectiveName, OptimizationTarget target,
        Dictionary<string, double> targetMetrics, IEnumerable<string> optimizationScopes,
        OptimizationPriority priority, CulturalEventType? culturalContext = null)
    {
        return new OptimizationObjective(objectiveName, target, targetMetrics, optimizationScopes,
            priority, culturalContext);
    }

    /// <summary>
    /// Creates cultural event optimization objective
    /// </summary>
    public static OptimizationObjective CreateCultural(string objectiveName,
        CulturalEventType culturalEventType, Dictionary<string, double> targetMetrics,
        IEnumerable<string> optimizationScopes)
    {
        return Create(objectiveName, OptimizationTarget.CulturalEventPerformance, targetMetrics,
            optimizationScopes, OptimizationPriority.CulturalEvent, culturalEventType);
    }
}

/// <summary>
/// Optimization targets
/// </summary>
public enum OptimizationTarget
{
    Performance,
    Cost,
    Reliability,
    Scalability,
    CulturalEventPerformance,
    UserExperience
}

/// <summary>
/// Optimization priority levels
/// </summary>
public enum OptimizationPriority
{
    Low,
    Medium,
    High,
    Critical,
    CulturalEvent
}

#endregion


#region AutoScalingConfiguration

/// <summary>
/// Auto-scaling configuration with cultural event awareness
/// </summary>
public class AutoScalingConfiguration
{
    public string ConfigurationId { get; private set; }
    public string ConfigurationName { get; private set; }
    public IReadOnlyList<string> ManagedServices { get; private set; }
    public Dictionary<string, ScalingTrigger> ScalingTriggers { get; private set; }
    public ScalingPolicy DefaultPolicy { get; private set; }
    public ScalingPolicy CulturalEventPolicy { get; private set; }
    public bool IsCulturalEventAware { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets whether configuration supports aggressive scaling
    /// </summary>
    public bool SupportsAggressiveScaling => CulturalEventPolicy.MaxInstances > DefaultPolicy.MaxInstances * 2;

    /// <summary>
    /// Gets estimated cultural event capacity multiplier
    /// </summary>
    public double CulturalEventCapacityMultiplier => IsCulturalEventAware && DefaultPolicy.MaxInstances > 0 ?
        (double)CulturalEventPolicy.MaxInstances / DefaultPolicy.MaxInstances : 1.0;

    private AutoScalingConfiguration(string configurationName, IEnumerable<string> managedServices,
        Dictionary<string, ScalingTrigger> scalingTriggers, ScalingPolicy defaultPolicy,
        ScalingPolicy culturalEventPolicy, bool isCulturalEventAware)
    {
        ConfigurationId = Guid.NewGuid().ToString();
        ConfigurationName = configurationName;
        ManagedServices = managedServices.ToList().AsReadOnly();
        ScalingTriggers = scalingTriggers;
        DefaultPolicy = defaultPolicy;
        CulturalEventPolicy = culturalEventPolicy;
        IsCulturalEventAware = isCulturalEventAware;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates auto-scaling configuration
    /// </summary>
    public static AutoScalingConfiguration Create(string configurationName, IEnumerable<string> managedServices,
        Dictionary<string, ScalingTrigger> scalingTriggers, ScalingPolicy defaultPolicy,
        ScalingPolicy? culturalEventPolicy = null, bool isCulturalEventAware = false)
    {
        return new AutoScalingConfiguration(configurationName, managedServices, scalingTriggers, defaultPolicy,
            culturalEventPolicy ?? defaultPolicy, isCulturalEventAware);
    }

    /// <summary>
    /// Creates cultural event-aware auto-scaling configuration
    /// </summary>
    public static AutoScalingConfiguration CreateCulturalAware(string configurationName, 
        IEnumerable<string> managedServices, Dictionary<string, ScalingTrigger> scalingTriggers,
        ScalingPolicy defaultPolicy, ScalingPolicy culturalEventPolicy)
    {
        return Create(configurationName, managedServices, scalingTriggers, defaultPolicy, culturalEventPolicy, true);
    }
}

/// <summary>
/// Scaling trigger configuration
/// </summary>
public record ScalingTrigger(string MetricName, double ScaleUpThreshold, double ScaleDownThreshold, 
    TimeSpan EvaluationWindow, string? CulturalContext = null);

/// <summary>
/// Scaling policy configuration
/// </summary>
public record ScalingPolicy(int MinInstances, int MaxInstances, int ScaleUpIncrement, int ScaleDownIncrement,
    TimeSpan CooldownPeriod);

#endregion