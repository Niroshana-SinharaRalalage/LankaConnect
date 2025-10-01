using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Database;
using LankaConnect.Domain.Common.ValueObjects;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Domain.Common.Database;

#region Enumerations

/// <summary>
/// Defines the types of auto-scaling triggers for cultural intelligence platform
/// </summary>
public enum ScalingTriggerType
{
    CpuThreshold,
    MemoryThreshold,
    ConnectionPoolUtilization,
    DatabaseResponseTime,
    ConcurrentUserCount,
    CulturalEventTraffic,
    DiasporaTimezonePeak,
    SacredEventPriority,
    RevenueOptimization,
    GeographicRegionLoad,
    LanguageProcessingLoad,
    CommunityEngagementSpike
}

/// <summary>
/// Connection pool strategies optimized for cultural intelligence workloads
/// </summary>
public enum ConnectionPoolStrategy
{
    Conservative,
    Balanced,
    Aggressive,
    CulturalEventOptimized,
    DiasporaTimezoneBased,
    SacredEventPriority,
    RevenueMaximizing,
    RegionalLoadBalanced,
    LanguageSpecific,
    CommunityBurst
}

// CulturalLoadPattern is now a class - see AdditionalMissingModels.cs

/// <summary>
/// Performance threshold severity levels
/// </summary>
public enum ThresholdSeverity
{
    Low,
    Medium,
    High,
    Critical,
    SacredEventCritical,
    RevenueCritical
}

// ScalingDirection is now defined in AdditionalMissingModels.cs

// SlaComplianceStatus moved to DatabaseMonitoringModels.cs to avoid duplication
// Use LankaConnect.Domain.Common.Database.SlaComplianceStatus instead

/// <summary>
/// Geographic regions for multi-region scaling
/// </summary>
public enum GeographicScalingRegion
{
    NorthAmerica,
    Europe,
    SouthAsia,
    SoutheastAsia,
    Australia,
    MiddleEast,
    Global
}

/// <summary>
/// Cultural intelligence processing types
/// </summary>
public enum CulturalProcessingType
{
    LanguageTranslation,
    CulturalContextAnalysis,
    ReligiousEventProcessing,
    CommunityEngagement,
    BusinessListingCuration,
    EventRecommendation,
    DiasporaMatchmaking
}

#endregion

#region Value Objects

/// <summary>
/// Represents performance metrics for connection pools with enhanced cultural intelligence
/// Consolidates multiple metrics implementations into a single canonical ValueObject
/// </summary>
public class ConnectionPoolMetrics : ValueObject
{
    public int ActiveConnections { get; }
    public int IdleConnections { get; }
    public int MaxPoolSize { get; }
    public double UtilizationPercentage { get; }
    public TimeSpan AverageConnectionTime { get; }
    public int ConnectionTimeouts { get; }
    public int ConnectionErrors { get; }
    public DateTime MeasuredAt { get; }

    // Legacy properties for backward compatibility from ConnectionPoolModels.cs
    public string PoolId { get; }
    public int PendingRequests { get; }
    public int TotalConnectionsCreated { get; }
    public int TotalConnectionsClosed { get; }
    public TimeSpan AverageConnectionAcquisitionTime { get; }
    public double PoolEfficiency { get; }
    public DateTime LastHealthCheck { get; }
    public Dictionary<string, double> PerformanceMetrics { get; }

    private ConnectionPoolMetrics(
        int activeConnections,
        int idleConnections,
        int maxPoolSize,
        TimeSpan averageConnectionTime,
        int connectionTimeouts,
        int connectionErrors,
        DateTime measuredAt,
        string poolId = "",
        int pendingRequests = 0,
        int totalConnectionsCreated = 0,
        int totalConnectionsClosed = 0,
        TimeSpan? averageConnectionAcquisitionTime = null,
        double? poolEfficiency = null,
        DateTime? lastHealthCheck = null,
        Dictionary<string, double>? performanceMetrics = null)
    {
        if (activeConnections < 0) throw new ArgumentException("Active connections cannot be negative");
        if (idleConnections < 0) throw new ArgumentException("Idle connections cannot be negative");
        if (maxPoolSize <= 0) throw new ArgumentException("Max pool size must be positive");

        ActiveConnections = activeConnections;
        IdleConnections = idleConnections;
        MaxPoolSize = maxPoolSize;
        UtilizationPercentage = Math.Round((double)activeConnections / maxPoolSize * 100, 2);
        AverageConnectionTime = averageConnectionTime;
        ConnectionTimeouts = connectionTimeouts;
        ConnectionErrors = connectionErrors;
        MeasuredAt = measuredAt;

        // Legacy properties
        PoolId = poolId;
        PendingRequests = pendingRequests;
        TotalConnectionsCreated = totalConnectionsCreated;
        TotalConnectionsClosed = totalConnectionsClosed;
        AverageConnectionAcquisitionTime = averageConnectionAcquisitionTime ?? averageConnectionTime;
        PoolEfficiency = poolEfficiency ?? UtilizationPercentage / 100.0;
        LastHealthCheck = lastHealthCheck ?? measuredAt;
        PerformanceMetrics = performanceMetrics ?? new Dictionary<string, double>();
    }

    public static ConnectionPoolMetrics Create(
        int activeConnections,
        int idleConnections,
        int maxPoolSize,
        TimeSpan averageConnectionTime,
        int connectionTimeouts = 0,
        int connectionErrors = 0,
        DateTime? measuredAt = null,
        string poolId = "",
        int pendingRequests = 0,
        int totalConnectionsCreated = 0,
        int totalConnectionsClosed = 0,
        TimeSpan? averageConnectionAcquisitionTime = null,
        double? poolEfficiency = null,
        DateTime? lastHealthCheck = null,
        Dictionary<string, double>? performanceMetrics = null)
    {
        return new ConnectionPoolMetrics(
            activeConnections,
            idleConnections,
            maxPoolSize,
            averageConnectionTime,
            connectionTimeouts,
            connectionErrors,
            measuredAt ?? DateTime.UtcNow,
            poolId,
            pendingRequests,
            totalConnectionsCreated,
            totalConnectionsClosed,
            averageConnectionAcquisitionTime,
            poolEfficiency,
            lastHealthCheck,
            performanceMetrics);
    }

    /// <summary>
    /// Creates legacy-compatible ConnectionPoolMetrics from basic properties
    /// </summary>
    public static ConnectionPoolMetrics CreateLegacy(
        string poolId,
        int activeConnections,
        int idleConnections,
        int pendingRequests,
        int totalConnectionsCreated,
        int totalConnectionsClosed,
        TimeSpan averageConnectionAcquisitionTime,
        double poolEfficiency,
        DateTime? lastHealthCheck = null,
        Dictionary<string, double>? performanceMetrics = null)
    {
        return new ConnectionPoolMetrics(
            activeConnections,
            idleConnections,
            Math.Max(activeConnections + idleConnections, 100), // Estimate maxPoolSize
            averageConnectionAcquisitionTime,
            0, // connectionTimeouts
            0, // connectionErrors
            lastHealthCheck ?? DateTime.UtcNow,
            poolId,
            pendingRequests,
            totalConnectionsCreated,
            totalConnectionsClosed,
            averageConnectionAcquisitionTime,
            poolEfficiency,
            lastHealthCheck,
            performanceMetrics);
    }

    public bool IsNearCapacity(double threshold = 0.8) => UtilizationPercentage >= threshold * 100;
    public bool HasErrors() => ConnectionTimeouts > 0 || ConnectionErrors > 0;

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return ActiveConnections;
        yield return IdleConnections;
        yield return MaxPoolSize;
        yield return UtilizationPercentage;
        yield return AverageConnectionTime;
        yield return ConnectionTimeouts;
        yield return ConnectionErrors;
        yield return MeasuredAt;
        yield return PoolId;
        yield return PendingRequests;
        yield return TotalConnectionsCreated;
        yield return TotalConnectionsClosed;
        yield return AverageConnectionAcquisitionTime;
        yield return PoolEfficiency;
        yield return LastHealthCheck;
        yield return string.Join(",", PerformanceMetrics.OrderBy(x => x.Key).Select(x => $"{x.Key}:{x.Value}"));
    }
}

// PerformanceThreshold moved to LankaConnect.Domain.Common.ValueObjects

/// <summary>
/// Represents cultural event load prediction data
/// </summary>
public class CulturalEventLoadPrediction : ValueObject
{
    public CulturalLoadPattern Pattern { get; }
    public DateTime EventDateTime { get; }
    public int PredictedUserLoad { get; }
    public double LoadMultiplier { get; }
    public TimeSpan PeakDuration { get; }
    public GeographicScalingRegion PrimaryRegion { get; }
    public int SacredEventPriority { get; } // 1-10 scale (10 = most sacred)
    public double RevenueImpactFactor { get; }
    public List<CulturalProcessingType> ExpectedProcessingTypes { get; }

    private CulturalEventLoadPrediction(
        CulturalLoadPattern pattern,
        DateTime eventDateTime,
        int predictedUserLoad,
        double loadMultiplier,
        TimeSpan peakDuration,
        GeographicScalingRegion primaryRegion,
        int sacredEventPriority,
        double revenueImpactFactor,
        List<CulturalProcessingType> expectedProcessingTypes)
    {
        if (predictedUserLoad < 0) throw new ArgumentException("Predicted user load cannot be negative");
        if (loadMultiplier < 0.1) throw new ArgumentException("Load multiplier must be at least 0.1");
        if (peakDuration <= TimeSpan.Zero) throw new ArgumentException("Peak duration must be positive");
        if (sacredEventPriority < 1 || sacredEventPriority > 10) 
            throw new ArgumentException("Sacred event priority must be between 1 and 10");
        if (revenueImpactFactor < 0) throw new ArgumentException("Revenue impact factor cannot be negative");

        Pattern = pattern;
        EventDateTime = eventDateTime;
        PredictedUserLoad = predictedUserLoad;
        LoadMultiplier = loadMultiplier;
        PeakDuration = peakDuration;
        PrimaryRegion = primaryRegion;
        SacredEventPriority = sacredEventPriority;
        RevenueImpactFactor = revenueImpactFactor;
        ExpectedProcessingTypes = expectedProcessingTypes ?? new List<CulturalProcessingType>();
    }

    public static CulturalEventLoadPrediction Create(
        CulturalLoadPattern pattern,
        DateTime eventDateTime,
        int predictedUserLoad,
        double loadMultiplier,
        TimeSpan peakDuration,
        GeographicScalingRegion primaryRegion,
        int sacredEventPriority = 5,
        double revenueImpactFactor = 1.0,
        List<CulturalProcessingType>? expectedProcessingTypes = null)
    {
        return new CulturalEventLoadPrediction(
            pattern,
            eventDateTime,
            predictedUserLoad,
            loadMultiplier,
            peakDuration,
            primaryRegion,
            sacredEventPriority,
            revenueImpactFactor,
            expectedProcessingTypes ?? new List<CulturalProcessingType>());
    }

    public bool IsHighPrioritySacredEvent() => SacredEventPriority >= 8;
    public bool HasSignificantRevenueImpact() => RevenueImpactFactor >= 1.5;
    public bool IsImminentEvent() => EventDateTime <= DateTime.UtcNow.AddHours(24);

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Pattern;
        yield return EventDateTime;
        yield return PredictedUserLoad;
        yield return LoadMultiplier;
        yield return PeakDuration;
        yield return PrimaryRegion;
        yield return SacredEventPriority;
        yield return RevenueImpactFactor;
        yield return string.Join(",", ExpectedProcessingTypes.OrderBy(x => x));
    }
}

/// <summary>
/// Represents diaspora traffic patterns for predictive scaling
/// </summary>
public class DiasporaTrafficPattern : ValueObject
{
    public GeographicScalingRegion Region { get; }
    public TimeZoneInfo TimeZone { get; }
    public Dictionary<DayOfWeek, double[]> HourlyTrafficMultipliers { get; } // 24-hour array
    public double BaselineTraffic { get; }
    public List<CulturalLoadPattern> ActivePatterns { get; }
    public DateTime LastUpdated { get; }

    private DiasporaTrafficPattern(
        GeographicScalingRegion region,
        TimeZoneInfo timeZone,
        Dictionary<DayOfWeek, double[]> hourlyTrafficMultipliers,
        double baselineTraffic,
        List<CulturalLoadPattern> activePatterns,
        DateTime lastUpdated)
    {
        if (baselineTraffic < 0) throw new ArgumentException("Baseline traffic cannot be negative");
        if (hourlyTrafficMultipliers.Values.Any(hours => hours.Length != 24))
            throw new ArgumentException("Each day must have exactly 24 hourly multipliers");

        Region = region;
        TimeZone = timeZone;
        HourlyTrafficMultipliers = hourlyTrafficMultipliers;
        BaselineTraffic = baselineTraffic;
        ActivePatterns = activePatterns ?? new List<CulturalLoadPattern>();
        LastUpdated = lastUpdated;
    }

    public static DiasporaTrafficPattern Create(
        GeographicScalingRegion region,
        TimeZoneInfo timeZone,
        Dictionary<DayOfWeek, double[]> hourlyTrafficMultipliers,
        double baselineTraffic,
        List<CulturalLoadPattern>? activePatterns = null,
        DateTime? lastUpdated = null)
    {
        return new DiasporaTrafficPattern(
            region,
            timeZone,
            hourlyTrafficMultipliers,
            baselineTraffic,
            activePatterns ?? new List<CulturalLoadPattern>(),
            lastUpdated ?? DateTime.UtcNow);
    }

    public double GetCurrentTrafficMultiplier()
    {
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZone);
        if (HourlyTrafficMultipliers.TryGetValue(now.DayOfWeek, out var hours))
        {
            return hours[now.Hour];
        }
        return 1.0;
    }

    public double GetPredictedTraffic(DateTime utcDateTime)
    {
        var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, TimeZone);
        if (HourlyTrafficMultipliers.TryGetValue(localTime.DayOfWeek, out var hours))
        {
            return BaselineTraffic * hours[localTime.Hour];
        }
        return BaselineTraffic;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Region;
        yield return TimeZone.Id;
        yield return BaselineTraffic;
        yield return string.Join(",", ActivePatterns.OrderBy(x => x));
        yield return LastUpdated;
        
        foreach (var kvp in HourlyTrafficMultipliers.OrderBy(x => x.Key))
        {
            yield return kvp.Key;
            yield return string.Join(",", kvp.Value);
        }
    }
}

/// <summary>
/// Represents SLA compliance metrics for cultural intelligence platform
/// </summary>
public class SlaComplianceMetrics : ValueObject
{
    public double ResponseTimeThreshold { get; } // milliseconds
    public double ActualAverageResponseTime { get; } // milliseconds
    public double AvailabilityThreshold { get; } // percentage (99.9 = 99.9%)
    public double ActualAvailability { get; } // percentage
    public double CulturalEventResponseTimeThreshold { get; } // Special threshold during cultural events
    public SlaComplianceStatus ComplianceStatus { get; }
    public TimeSpan EvaluationPeriod { get; }
    public DateTime MeasuredAt { get; }
    public Dictionary<CulturalProcessingType, double> ProcessingTypePerformance { get; }

    private SlaComplianceMetrics(
        double responseTimeThreshold,
        double actualAverageResponseTime,
        double availabilityThreshold,
        double actualAvailability,
        double culturalEventResponseTimeThreshold,
        SlaComplianceStatus complianceStatus,
        TimeSpan evaluationPeriod,
        DateTime measuredAt,
        Dictionary<CulturalProcessingType, double> processingTypePerformance)
    {
        if (responseTimeThreshold <= 0) throw new ArgumentException("Response time threshold must be positive");
        if (actualAverageResponseTime < 0) throw new ArgumentException("Actual response time cannot be negative");
        if (availabilityThreshold < 0 || availabilityThreshold > 100) 
            throw new ArgumentException("Availability threshold must be between 0 and 100");
        if (actualAvailability < 0 || actualAvailability > 100) 
            throw new ArgumentException("Actual availability must be between 0 and 100");

        ResponseTimeThreshold = responseTimeThreshold;
        ActualAverageResponseTime = actualAverageResponseTime;
        AvailabilityThreshold = availabilityThreshold;
        ActualAvailability = actualAvailability;
        CulturalEventResponseTimeThreshold = culturalEventResponseTimeThreshold;
        ComplianceStatus = complianceStatus;
        EvaluationPeriod = evaluationPeriod;
        MeasuredAt = measuredAt;
        ProcessingTypePerformance = processingTypePerformance ?? new Dictionary<CulturalProcessingType, double>();
    }

    public static SlaComplianceMetrics Create(
        double responseTimeThreshold,
        double actualAverageResponseTime,
        double availabilityThreshold,
        double actualAvailability,
        double? culturalEventResponseTimeThreshold = null,
        TimeSpan? evaluationPeriod = null,
        DateTime? measuredAt = null,
        Dictionary<CulturalProcessingType, double>? processingTypePerformance = null)
    {
        var status = DetermineComplianceStatus(
            responseTimeThreshold, 
            actualAverageResponseTime, 
            availabilityThreshold, 
            actualAvailability);

        return new SlaComplianceMetrics(
            responseTimeThreshold,
            actualAverageResponseTime,
            availabilityThreshold,
            actualAvailability,
            culturalEventResponseTimeThreshold ?? responseTimeThreshold * 0.8, // 20% faster for cultural events
            status,
            evaluationPeriod ?? TimeSpan.FromMinutes(15),
            measuredAt ?? DateTime.UtcNow,
            processingTypePerformance ?? new Dictionary<CulturalProcessingType, double>());
    }

    private static SlaComplianceStatus DetermineComplianceStatus(
        double responseTimeThreshold,
        double actualResponseTime,
        double availabilityThreshold,
        double actualAvailability)
    {
        var responseTimeRatio = actualResponseTime / responseTimeThreshold;
        var availabilityGap = availabilityThreshold - actualAvailability;

        if (responseTimeRatio > 2.0 || availabilityGap > 1.0)
            return SlaComplianceStatus.CriticalViolation;
        if (responseTimeRatio > 1.5 || availabilityGap > 0.5)
            return SlaComplianceStatus.Violation;
        if (responseTimeRatio > 1.2 || availabilityGap > 0.1)
            return SlaComplianceStatus.Warning;
        
        return SlaComplianceStatus.Compliant;
    }

    public bool IsCompliant() => ComplianceStatus == SlaComplianceStatus.Compliant;
    public bool RequiresImmediateScaling() => 
        ComplianceStatus == SlaComplianceStatus.CriticalViolation || 
        ComplianceStatus == SlaComplianceStatus.RevenueImpacting;

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return ResponseTimeThreshold;
        yield return ActualAverageResponseTime;
        yield return AvailabilityThreshold;
        yield return ActualAvailability;
        yield return CulturalEventResponseTimeThreshold;
        yield return ComplianceStatus;
        yield return EvaluationPeriod;
        yield return MeasuredAt;
        
        foreach (var kvp in ProcessingTypePerformance.OrderBy(x => x.Key))
        {
            yield return kvp.Key;
            yield return kvp.Value;
        }
    }
}

/// <summary>
/// Enterprise-level connection pool metrics aggregation
/// </summary>
public class EnterpriseConnectionPoolMetrics : ValueObject
{
    public int TotalActivePools { get; }
    public int TotalActiveConnections { get; }
    public double SystemWideEfficiency { get; }
    public TimeSpan AverageConnectionAcquisitionTime { get; }
    public Dictionary<string, ConnectionPoolMetrics> PoolMetrics { get; }
    public List<string> SystemWideOptimizations { get; }

    private EnterpriseConnectionPoolMetrics(
        int totalActivePools,
        int totalActiveConnections,
        double systemWideEfficiency,
        TimeSpan averageConnectionAcquisitionTime,
        Dictionary<string, ConnectionPoolMetrics> poolMetrics,
        List<string> systemWideOptimizations)
    {
        TotalActivePools = totalActivePools;
        TotalActiveConnections = totalActiveConnections;
        SystemWideEfficiency = systemWideEfficiency;
        AverageConnectionAcquisitionTime = averageConnectionAcquisitionTime;
        PoolMetrics = poolMetrics ?? new Dictionary<string, ConnectionPoolMetrics>();
        SystemWideOptimizations = systemWideOptimizations ?? new List<string>();
    }

    public static EnterpriseConnectionPoolMetrics Create(
        int totalActivePools,
        int totalActiveConnections,
        double systemWideEfficiency,
        TimeSpan averageConnectionAcquisitionTime,
        Dictionary<string, ConnectionPoolMetrics>? poolMetrics = null,
        List<string>? systemWideOptimizations = null)
    {
        return new EnterpriseConnectionPoolMetrics(
            totalActivePools,
            totalActiveConnections,
            systemWideEfficiency,
            averageConnectionAcquisitionTime,
            poolMetrics ?? new Dictionary<string, ConnectionPoolMetrics>(),
            systemWideOptimizations ?? new List<string>());
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return TotalActivePools;
        yield return TotalActiveConnections;
        yield return SystemWideEfficiency;
        yield return AverageConnectionAcquisitionTime;
        yield return string.Join(",", PoolMetrics.Keys.OrderBy(x => x));
        yield return string.Join(",", SystemWideOptimizations.OrderBy(x => x));
    }
}

#endregion

#region Entities and Aggregates

/// <summary>
/// Represents an auto-scaling request with cultural intelligence
/// </summary>
public class AutoScalingRequest : BaseEntity
{
    public ScalingTriggerType TriggerType { get; private set; }
    public ScalingDirection RequestedDirection { get; private set; }
    public int CurrentInstances { get; private set; }
    public int RequestedInstances { get; private set; }
    public ConnectionPoolStrategy PoolStrategy { get; private set; }
    public PerformanceThreshold? Threshold { get; init; }
    public CulturalEventLoadPrediction? CulturalPrediction { get; private set; }
    public GeographicScalingRegion TargetRegion { get; private set; }
    public string Justification { get; set; } = string.Empty;
    public DateTime RequestedAt { get; private set; }
    public bool IsEmergencyScaling { get; private set; }
    public double EstimatedCostImpact { get; private set; }
    public double RevenueProtectionFactor { get; private set; }

    private AutoScalingRequest() { } // EF Core constructor

    private AutoScalingRequest(
        ScalingTriggerType triggerType,
        ScalingDirection requestedDirection,
        int currentInstances,
        int requestedInstances,
        ConnectionPoolStrategy poolStrategy,
        PerformanceThreshold threshold,
        GeographicScalingRegion targetRegion,
        string justification,
        bool isEmergencyScaling,
        double estimatedCostImpact,
        double revenueProtectionFactor,
        CulturalEventLoadPrediction? culturalPrediction = null) : base()
    {
        if (currentInstances < 0) throw new ArgumentException("Current instances cannot be negative");
        if (requestedInstances < 0) throw new ArgumentException("Requested instances cannot be negative");
        if (string.IsNullOrWhiteSpace(justification)) throw new ArgumentException("Justification is required");
        if (estimatedCostImpact < 0) throw new ArgumentException("Cost impact cannot be negative");
        if (revenueProtectionFactor < 0) throw new ArgumentException("Revenue protection factor cannot be negative");

        TriggerType = triggerType;
        RequestedDirection = requestedDirection;
        CurrentInstances = currentInstances;
        RequestedInstances = requestedInstances;
        PoolStrategy = poolStrategy;
        Threshold = threshold;
        CulturalPrediction = culturalPrediction;
        TargetRegion = targetRegion;
        Justification = justification;
        RequestedAt = DateTime.UtcNow;
        IsEmergencyScaling = isEmergencyScaling;
        EstimatedCostImpact = estimatedCostImpact;
        RevenueProtectionFactor = revenueProtectionFactor;

        RaiseDomainEvent(new AutoScalingRequestedEvent(Id, TriggerType, RequestedDirection, IsEmergencyScaling));
    }

    public static AutoScalingRequest Create(
        ScalingTriggerType triggerType,
        ScalingDirection requestedDirection,
        int currentInstances,
        int requestedInstances,
        ConnectionPoolStrategy poolStrategy,
        PerformanceThreshold threshold,
        GeographicScalingRegion targetRegion,
        string justification,
        bool isEmergencyScaling = false,
        double estimatedCostImpact = 0.0,
        double revenueProtectionFactor = 1.0,
        CulturalEventLoadPrediction? culturalPrediction = null)
    {
        return new AutoScalingRequest(triggerType, requestedDirection, currentInstances, requestedInstances, poolStrategy, threshold, targetRegion, justification, isEmergencyScaling, estimatedCostImpact, revenueProtectionFactor, culturalPrediction);
    }

    public void UpdateCulturalPrediction(CulturalEventLoadPrediction prediction)
    {
        CulturalPrediction = prediction;
        MarkAsUpdated();
        RaiseDomainEvent(new CulturalPredictionUpdatedEvent(Id, prediction));
    }

    public void MarkAsEmergency(string reason)
    {
        IsEmergencyScaling = true;
        Justification = $"{Justification} | EMERGENCY: {reason}";
        MarkAsUpdated();
        RaiseDomainEvent(new EmergencyScalingTriggeredEvent(Id, reason));
    }

    public bool ShouldPrioritizeForCulturalEvent() =>
        CulturalPrediction?.IsHighPrioritySacredEvent() ?? false ||
        TriggerType == ScalingTriggerType.SacredEventPriority;

    public bool HasRevenueImpact() =>
        RevenueProtectionFactor > 1.0 || 
        CulturalPrediction?.HasSignificantRevenueImpact() == true;
}

/// <summary>
/// Represents an auto-scaling response with execution details
/// </summary>
public class AutoScalingResponse : BaseEntity
{
    public Guid RequestId { get; private set; }
    public bool IsApproved { get; private set; }
    public int ActualInstancesAllocated { get; private set; }
    public ConnectionPoolMetrics? PreScalingMetrics { get; init; }
    public ConnectionPoolMetrics? PostScalingMetrics { get; private set; }
    public string ExecutionDetails { get; init; } = string.Empty;
    public TimeSpan ExecutionDuration { get; private set; }
    public DateTime ExecutedAt { get; private set; }
    public SlaComplianceMetrics? ComplianceImpact { get; init; }
    public double ActualCostImpact { get; private set; }
    public List<string> ExecutionWarnings { get; init; } = new();
    public bool IsRollbackRequired { get; private set; }

    private AutoScalingResponse() { } // EF Core constructor

    private AutoScalingResponse(
        Guid requestId,
        bool isApproved,
        int actualInstancesAllocated,
        ConnectionPoolMetrics preScalingMetrics,
        string executionDetails,
        SlaComplianceMetrics complianceImpact,
        double actualCostImpact,
        List<string> executionWarnings) : base()
    {
        if (requestId == Guid.Empty) throw new ArgumentException("Request ID cannot be empty");
        if (actualInstancesAllocated < 0) throw new ArgumentException("Actual instances cannot be negative");
        if (string.IsNullOrWhiteSpace(executionDetails)) throw new ArgumentException("Execution details are required");
        if (actualCostImpact < 0) throw new ArgumentException("Actual cost impact cannot be negative");

        RequestId = requestId;
        IsApproved = isApproved;
        ActualInstancesAllocated = actualInstancesAllocated;
        PreScalingMetrics = preScalingMetrics;
        ExecutionDetails = executionDetails;
        ExecutionDuration = TimeSpan.Zero;
        ExecutedAt = DateTime.UtcNow;
        ComplianceImpact = complianceImpact;
        ActualCostImpact = actualCostImpact;
        ExecutionWarnings = executionWarnings ?? new List<string>();
        IsRollbackRequired = false;

        RaiseDomainEvent(new AutoScalingExecutedEvent(Id, RequestId, IsApproved, ActualInstancesAllocated));
    }

    public static AutoScalingResponse Create(
        Guid requestId,
        bool isApproved,
        int actualInstancesAllocated,
        ConnectionPoolMetrics preScalingMetrics,
        string executionDetails,
        SlaComplianceMetrics complianceImpact,
        double actualCostImpact = 0.0,
        List<string>? executionWarnings = null)
    {
        return new AutoScalingResponse(requestId, isApproved, actualInstancesAllocated, preScalingMetrics, executionDetails, complianceImpact, actualCostImpact, executionWarnings ?? new List<string>());
    }

    public void CompleteExecution(
        ConnectionPoolMetrics postScalingMetrics,
        TimeSpan executionDuration,
        bool requiresRollback = false)
    {
        PostScalingMetrics = postScalingMetrics;
        ExecutionDuration = executionDuration;
        IsRollbackRequired = requiresRollback;
        MarkAsUpdated();

        RaiseDomainEvent(new AutoScalingCompletedEvent(Id, ExecutionDuration, requiresRollback));
    }

    public void AddWarning(string warning)
    {
        if (!string.IsNullOrWhiteSpace(warning))
        {
            ExecutionWarnings.Add($"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}: {warning}");
            MarkAsUpdated();
        }
    }

    public bool WasSuccessful() => IsApproved && !IsRollbackRequired && PostScalingMetrics != null;
    public bool ImprovedPerformance() => 
        PostScalingMetrics != null && 
        PostScalingMetrics.UtilizationPercentage < PreScalingMetrics?.UtilizationPercentage;
}

/// <summary>
/// Represents scaling decision context for cultural intelligence
/// </summary>
public class ScalingDecisionContext : BaseEntity
{
    public List<PerformanceThreshold> ActiveThresholds { get; init; } = new();
    public ConnectionPoolMetrics? CurrentMetrics { get; set; }
    public List<CulturalEventLoadPrediction> UpcomingEvents { get; init; } = new();
    public DiasporaTrafficPattern? TrafficPattern { get; init; }
    public SlaComplianceMetrics? CurrentSlaStatus { get; set; }
    public GeographicScalingRegion PrimaryRegion { get; private set; }
    public Dictionary<string, object> AdditionalContext { get; init; } = new();
    public DateTime EvaluatedAt { get; private set; }
    public bool IsInMaintenanceWindow { get; private set; }
    public double CurrentRevenueConcern { get; private set; } // 0.0 to 1.0 scale

    private ScalingDecisionContext() { } // EF Core constructor

    private ScalingDecisionContext(
        List<PerformanceThreshold> activeThresholds,
        ConnectionPoolMetrics currentMetrics,
        DiasporaTrafficPattern trafficPattern,
        SlaComplianceMetrics currentSlaStatus,
        GeographicScalingRegion primaryRegion,
        bool isInMaintenanceWindow,
        double currentRevenueConcern,
        List<CulturalEventLoadPrediction>? upcomingEvents = null,
        Dictionary<string, object>? additionalContext = null) : base()
    {
        if (currentRevenueConcern < 0.0 || currentRevenueConcern > 1.0)
            throw new ArgumentException("Revenue concern must be between 0.0 and 1.0");

        ActiveThresholds = activeThresholds ?? new List<PerformanceThreshold>();
        CurrentMetrics = currentMetrics;
        UpcomingEvents = upcomingEvents ?? new List<CulturalEventLoadPrediction>();
        TrafficPattern = trafficPattern;
        CurrentSlaStatus = currentSlaStatus;
        PrimaryRegion = primaryRegion;
        AdditionalContext = additionalContext ?? new Dictionary<string, object>();
        EvaluatedAt = DateTime.UtcNow;
        IsInMaintenanceWindow = isInMaintenanceWindow;
        CurrentRevenueConcern = currentRevenueConcern;

        RaiseDomainEvent(new ScalingDecisionEvaluatedEvent(Id, PrimaryRegion, CurrentSlaStatus.ComplianceStatus));
    }

    public static ScalingDecisionContext Create(
        List<PerformanceThreshold> activeThresholds,
        ConnectionPoolMetrics currentMetrics,
        DiasporaTrafficPattern trafficPattern,
        SlaComplianceMetrics currentSlaStatus,
        GeographicScalingRegion primaryRegion,
        bool isInMaintenanceWindow = false,
        double currentRevenueConcern = 0.0,
        List<CulturalEventLoadPrediction>? upcomingEvents = null,
        Dictionary<string, object>? additionalContext = null)
    {
        return new ScalingDecisionContext(activeThresholds, currentMetrics, trafficPattern, currentSlaStatus, primaryRegion, isInMaintenanceWindow, currentRevenueConcern, upcomingEvents, additionalContext);
    }

    public void UpdateContext(
        ConnectionPoolMetrics newMetrics,
        SlaComplianceMetrics newSlaStatus,
        double newRevenueConcern)
    {
        CurrentMetrics = newMetrics;
        CurrentSlaStatus = newSlaStatus;
        CurrentRevenueConcern = Math.Clamp(newRevenueConcern, 0.0, 1.0);
        EvaluatedAt = DateTime.UtcNow;
        MarkAsUpdated();
    }

    public void AddUpcomingEvent(CulturalEventLoadPrediction prediction)
    {
        if (prediction != null && !UpcomingEvents.Any(e => e.EventDateTime == prediction.EventDateTime))
        {
            UpcomingEvents.Add(prediction);
            MarkAsUpdated();
            RaiseDomainEvent(new CulturalEventDetectedEvent(Id, prediction));
        }
    }

    public bool HasImminentCulturalEvents() => UpcomingEvents.Any(e => e.IsImminentEvent());
    public bool HasHighPrioritySacredEvents() => UpcomingEvents.Any(e => e.IsHighPrioritySacredEvent());
    public bool RequiresEmergencyScaling() => 
        CurrentSlaStatus?.RequiresImmediateScaling() == true || 
        CurrentRevenueConcern >= 0.8 ||
        HasHighPrioritySacredEvents();

    public ScalingDirection RecommendedDirection()
    {
        if (RequiresEmergencyScaling()) return ScalingDirection.Up;
        if (HasImminentCulturalEvents()) return ScalingDirection.CulturalBoost;
        if (CurrentRevenueConcern >= 0.6) return ScalingDirection.RevenueBoost;
        if (CurrentMetrics?.UtilizationPercentage >= 80) return ScalingDirection.Up;
        if (CurrentMetrics?.UtilizationPercentage <= 30 && !HasImminentCulturalEvents()) return ScalingDirection.Down;
        
        return ScalingDirection.Maintain;
    }
}

#endregion

#region Domain Events

/// <summary>
/// Domain event raised when auto-scaling is requested
/// </summary>
public record AutoScalingRequestedEvent(
    Guid RequestId,
    ScalingTriggerType TriggerType,
    ScalingDirection Direction,
    bool IsEmergency,
    DateTime OccurredAt) : IDomainEvent
{
    public AutoScalingRequestedEvent(Guid RequestId, ScalingTriggerType TriggerType, ScalingDirection Direction, bool IsEmergency)
        : this(RequestId, TriggerType, Direction, IsEmergency, DateTime.UtcNow) { }
}

/// <summary>
/// Domain event raised when cultural prediction is updated
/// </summary>
public record CulturalPredictionUpdatedEvent(
    Guid RequestId,
    CulturalEventLoadPrediction Prediction,
    DateTime OccurredAt) : IDomainEvent
{
    public CulturalPredictionUpdatedEvent(Guid RequestId, CulturalEventLoadPrediction Prediction)
        : this(RequestId, Prediction, DateTime.UtcNow) { }
}

/// <summary>
/// Domain event raised when emergency scaling is triggered
/// </summary>
public record EmergencyScalingTriggeredEvent(
    Guid RequestId,
    string Reason,
    DateTime OccurredAt) : IDomainEvent
{
    public EmergencyScalingTriggeredEvent(Guid RequestId, string Reason)
        : this(RequestId, Reason, DateTime.UtcNow) { }
}

/// <summary>
/// Domain event raised when auto-scaling is executed
/// </summary>
public record AutoScalingExecutedEvent(
    Guid ResponseId,
    Guid RequestId,
    bool IsApproved,
    int InstancesAllocated,
    DateTime OccurredAt) : IDomainEvent
{
    public AutoScalingExecutedEvent(Guid ResponseId, Guid RequestId, bool IsApproved, int InstancesAllocated)
        : this(ResponseId, RequestId, IsApproved, InstancesAllocated, DateTime.UtcNow) { }
}

/// <summary>
/// Domain event raised when auto-scaling execution is completed
/// </summary>
public record AutoScalingCompletedEvent(
    Guid ResponseId,
    TimeSpan ExecutionDuration,
    bool RequiredRollback,
    DateTime OccurredAt) : IDomainEvent
{
    public AutoScalingCompletedEvent(Guid ResponseId, TimeSpan ExecutionDuration, bool RequiredRollback)
        : this(ResponseId, ExecutionDuration, RequiredRollback, DateTime.UtcNow) { }
}

/// <summary>
/// Domain event raised when scaling decision is evaluated
/// </summary>
public record ScalingDecisionEvaluatedEvent(
    Guid ContextId,
    GeographicScalingRegion Region,
    SlaComplianceStatus SlaStatus,
    DateTime OccurredAt) : IDomainEvent
{
    public ScalingDecisionEvaluatedEvent(Guid ContextId, GeographicScalingRegion Region, SlaComplianceStatus SlaStatus)
        : this(ContextId, Region, SlaStatus, DateTime.UtcNow) { }
}

/// <summary>
/// Domain event raised when cultural event is detected
/// </summary>
public record CulturalEventDetectedEvent(
    Guid ContextId,
    CulturalEventLoadPrediction EventPrediction,
    DateTime OccurredAt) : IDomainEvent
{
    public CulturalEventDetectedEvent(Guid ContextId, CulturalEventLoadPrediction EventPrediction)
        : this(ContextId, EventPrediction, DateTime.UtcNow) { }
}

#endregion

#region Repository Interfaces

/// <summary>
/// Repository interface for auto-scaling requests
/// </summary>
public interface IAutoScalingRequestRepository : IRepository<AutoScalingRequest>
{
    Task<List<AutoScalingRequest>> GetPendingRequestsAsync();
    Task<List<AutoScalingRequest>> GetRequestsByTriggerTypeAsync(ScalingTriggerType triggerType);
    Task<List<AutoScalingRequest>> GetEmergencyRequestsAsync();
    Task<List<AutoScalingRequest>> GetRequestsByRegionAsync(GeographicScalingRegion region);
    Task<AutoScalingRequest?> GetMostRecentRequestAsync(ScalingTriggerType triggerType);
}

/// <summary>
/// Repository interface for auto-scaling responses
/// </summary>
public interface IAutoScalingResponseRepository : IRepository<AutoScalingResponse>
{
    Task<AutoScalingResponse?> GetByRequestIdAsync(Guid requestId);
    Task<List<AutoScalingResponse>> GetSuccessfulResponsesAsync(DateTime since);
    Task<List<AutoScalingResponse>> GetFailedResponsesAsync(DateTime since);
    Task<List<AutoScalingResponse>> GetResponsesRequiringRollbackAsync();
}

/// <summary>
/// Repository interface for scaling decision contexts
/// </summary>
public interface IScalingDecisionContextRepository : IRepository<ScalingDecisionContext>
{
    Task<ScalingDecisionContext?> GetLatestContextAsync(GeographicScalingRegion region);
    Task<List<ScalingDecisionContext>> GetContextsWithImminentEventsAsync();
    Task<List<ScalingDecisionContext>> GetContextsRequiringEmergencyScalingAsync();
}

#endregion