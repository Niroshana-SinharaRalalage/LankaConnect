using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Shared;

namespace LankaConnect.Domain.Infrastructure.Scaling;

/// <summary>
/// Represents performance metrics used for auto-scaling decisions
/// </summary>
public class PerformanceMetrics : ValueObject
{
    public TimeSpan AverageResponseTime { get; private set; }
    public double ThroughputQps { get; private set; }
    public double BaselineQps { get; private set; }
    public double CpuUtilizationPercentage { get; private set; }
    public double MemoryUtilizationPercentage { get; private set; }
    public int ActiveConnections { get; private set; }
    public DateTime MeasurementTimestamp { get; private set; }

    private PerformanceMetrics(
        TimeSpan averageResponseTime,
        double throughputQps,
        double baselineQps,
        double cpuUtilization,
        double memoryUtilization,
        int activeConnections,
        DateTime measurementTimestamp)
    {
        AverageResponseTime = averageResponseTime;
        ThroughputQps = throughputQps;
        BaselineQps = baselineQps;
        CpuUtilizationPercentage = cpuUtilization;
        MemoryUtilizationPercentage = memoryUtilization;
        ActiveConnections = activeConnections;
        MeasurementTimestamp = measurementTimestamp;
    }

    public static Result<PerformanceMetrics> Create(
        TimeSpan averageResponseTime,
        double throughputQps,
        double baselineQps,
        double cpuUtilization,
        double memoryUtilization,
        int activeConnections,
        DateTime measurementTimestamp)
    {
        if (averageResponseTime.TotalMilliseconds < 0)
            return Result<PerformanceMetrics>.Failure("Average response time cannot be negative");

        if (throughputQps < 0 || baselineQps < 0)
            return Result<PerformanceMetrics>.Failure("QPS values cannot be negative");

        if (cpuUtilization < 0 || cpuUtilization > 100)
            return Result<PerformanceMetrics>.Failure("CPU utilization must be between 0 and 100");

        if (memoryUtilization < 0 || memoryUtilization > 100)
            return Result<PerformanceMetrics>.Failure("Memory utilization must be between 0 and 100");

        if (activeConnections < 0)
            return Result<PerformanceMetrics>.Failure("Active connections cannot be negative");

        return Result<PerformanceMetrics>.Success(new PerformanceMetrics(
            averageResponseTime, throughputQps, baselineQps, cpuUtilization, 
            memoryUtilization, activeConnections, measurementTimestamp));
    }

    public bool ExceedsResponseTimeThreshold(TimeSpan threshold)
    {
        return AverageResponseTime > threshold;
    }

    public bool IsThroughputBelowBaseline(double thresholdPercentage = 0.8)
    {
        return ThroughputQps < (BaselineQps * thresholdPercentage);
    }

    public bool IsResourceUtilizationHigh(double cpuThreshold = 80, double memoryThreshold = 85)
    {
        return CpuUtilizationPercentage > cpuThreshold || MemoryUtilizationPercentage > memoryThreshold;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return AverageResponseTime;
        yield return ThroughputQps;
        yield return BaselineQps;
        yield return CpuUtilizationPercentage;
        yield return MemoryUtilizationPercentage;
        yield return ActiveConnections;
        yield return MeasurementTimestamp;
    }
}

/// <summary>
/// Represents connection pool health metrics for auto-scaling decisions
/// </summary>
public class ConnectionPoolHealthScore : ValueObject
{
    public string PoolId { get; private set; }
    public double UtilizationPercentage { get; private set; }
    public TimeSpan AcquisitionTime { get; private set; }
    public int PendingRequests { get; private set; }
    public double HealthScore { get; private set; }
    public DateTime LastMeasurement { get; private set; }

    private ConnectionPoolHealthScore(
        string poolId,
        double utilizationPercentage,
        TimeSpan acquisitionTime,
        int pendingRequests,
        double healthScore,
        DateTime lastMeasurement)
    {
        PoolId = poolId;
        UtilizationPercentage = utilizationPercentage;
        AcquisitionTime = acquisitionTime;
        PendingRequests = pendingRequests;
        HealthScore = healthScore;
        LastMeasurement = lastMeasurement;
    }

    public static Result<ConnectionPoolHealthScore> Create(
        string poolId,
        double utilizationPercentage,
        TimeSpan acquisitionTime,
        int pendingRequests,
        double healthScore,
        DateTime lastMeasurement)
    {
        if (string.IsNullOrWhiteSpace(poolId))
            return Result<ConnectionPoolHealthScore>.Failure("Pool ID cannot be empty");

        if (utilizationPercentage < 0 || utilizationPercentage > 100)
            return Result<ConnectionPoolHealthScore>.Failure("Utilization percentage must be between 0 and 100");

        if (acquisitionTime.TotalMilliseconds < 0)
            return Result<ConnectionPoolHealthScore>.Failure("Acquisition time cannot be negative");

        if (pendingRequests < 0)
            return Result<ConnectionPoolHealthScore>.Failure("Pending requests cannot be negative");

        if (healthScore < 0 || healthScore > 1)
            return Result<ConnectionPoolHealthScore>.Failure("Health score must be between 0 and 1");

        return Result<ConnectionPoolHealthScore>.Success(new ConnectionPoolHealthScore(
            poolId, utilizationPercentage, acquisitionTime, pendingRequests, healthScore, lastMeasurement));
    }

    public bool RequiresAttention(double utilizationThreshold = 85, TimeSpan acquisitionThreshold = default)
    {
        if (acquisitionThreshold == default)
            acquisitionThreshold = TimeSpan.FromMilliseconds(10);

        return UtilizationPercentage > utilizationThreshold || 
               AcquisitionTime > acquisitionThreshold || 
               PendingRequests > 50;
    }

    public ScalingUrgencyLevel GetUrgencyLevel()
    {
        if (HealthScore < 0.5 || UtilizationPercentage > 95 || PendingRequests > 100)
            return ScalingUrgencyLevel.Critical;
        
        if (HealthScore < 0.7 || UtilizationPercentage > 85 || PendingRequests > 50)
            return ScalingUrgencyLevel.High;
        
        if (HealthScore < 0.85 || UtilizationPercentage > 75)
            return ScalingUrgencyLevel.Medium;
        
        return ScalingUrgencyLevel.Low;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return PoolId;
        yield return UtilizationPercentage;
        yield return AcquisitionTime;
        yield return PendingRequests;
        yield return HealthScore;
    }
}

/// <summary>
/// Represents analysis of connection pool health across multiple pools
/// </summary>
public class ConnectionPoolHealthAnalysis : ValueObject
{
    public Dictionary<string, ConnectionPoolHealthScore> PoolHealthScores { get; private set; }
    public double OverallSystemHealth { get; private set; }
    public DateTime AnalysisTimestamp { get; private set; }
    public IReadOnlyList<string> CriticalPools { get; private set; }
    public IReadOnlyList<string> HealthyPools { get; private set; }

    private ConnectionPoolHealthAnalysis(
        Dictionary<string, ConnectionPoolHealthScore> poolHealthScores,
        double overallSystemHealth,
        DateTime analysisTimestamp,
        IEnumerable<string> criticalPools,
        IEnumerable<string> healthyPools)
    {
        PoolHealthScores = new Dictionary<string, ConnectionPoolHealthScore>(poolHealthScores);
        OverallSystemHealth = overallSystemHealth;
        AnalysisTimestamp = analysisTimestamp;
        CriticalPools = criticalPools.ToList().AsReadOnly();
        HealthyPools = healthyPools.ToList().AsReadOnly();
    }

    public static Result<ConnectionPoolHealthAnalysis> Create(
        Dictionary<string, ConnectionPoolHealthScore> poolHealthScores,
        DateTime analysisTimestamp)
    {
        if (!poolHealthScores.Any())
            return Result<ConnectionPoolHealthAnalysis>.Failure("At least one pool health score must be provided");

        var overallSystemHealth = poolHealthScores.Values.Average(score => score.HealthScore);
        
        var criticalPools = poolHealthScores
            .Where(kvp => kvp.Value.GetUrgencyLevel() == ScalingUrgencyLevel.Critical)
            .Select(kvp => kvp.Key)
            .ToList();

        var healthyPools = poolHealthScores
            .Where(kvp => kvp.Value.GetUrgencyLevel() == ScalingUrgencyLevel.Low)
            .Select(kvp => kvp.Key)
            .ToList();

        return Result<ConnectionPoolHealthAnalysis>.Success(new ConnectionPoolHealthAnalysis(
            poolHealthScores, overallSystemHealth, analysisTimestamp, criticalPools, healthyPools));
    }

    public bool RequiresImmediateScaling()
    {
        return CriticalPools.Any() || OverallSystemHealth < 0.6;
    }

    public ScalingUrgencyLevel GetSystemUrgencyLevel()
    {
        if (CriticalPools.Any())
            return ScalingUrgencyLevel.Critical;
        
        if (OverallSystemHealth < 0.7)
            return ScalingUrgencyLevel.High;
        
        if (OverallSystemHealth < 0.85)
            return ScalingUrgencyLevel.Medium;
        
        return ScalingUrgencyLevel.Low;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return OverallSystemHealth;
        yield return AnalysisTimestamp;
        yield return string.Join(",", CriticalPools.OrderBy(p => p));
        yield return string.Join(",", HealthyPools.OrderBy(p => p));
    }
}

/// <summary>
/// Represents an active cultural event detected by the system
/// </summary>
public class ActiveCulturalEvent : ValueObject
{
    public string EventId { get; private set; }
    public string Name { get; private set; }
    public CulturalEventType EventType { get; private set; }
    public CulturalSignificance Significance { get; private set; }
    public DateTime StartTime { get; private set; }
    public DateTime EstimatedEndTime { get; private set; }
    public double CurrentTrafficMultiplier { get; private set; }
    public IReadOnlyList<string> AffectedCommunities { get; private set; }
    public DateTime DetectedAt { get; private set; }

    private ActiveCulturalEvent(
        string eventId,
        string name,
        CulturalEventType eventType,
        CulturalSignificance significance,
        DateTime startTime,
        DateTime estimatedEndTime,
        double currentTrafficMultiplier,
        IEnumerable<string> affectedCommunities,
        DateTime detectedAt)
    {
        EventId = eventId;
        Name = name;
        EventType = eventType;
        Significance = significance;
        StartTime = startTime;
        EstimatedEndTime = estimatedEndTime;
        CurrentTrafficMultiplier = currentTrafficMultiplier;
        AffectedCommunities = affectedCommunities.ToList().AsReadOnly();
        DetectedAt = detectedAt;
    }

    public static Result<ActiveCulturalEvent> Create(
        string eventId,
        string name,
        CulturalEventType eventType,
        CulturalSignificance significance,
        DateTime startTime,
        DateTime estimatedEndTime,
        double currentTrafficMultiplier,
        IEnumerable<string> affectedCommunities,
        DateTime detectedAt)
    {
        if (string.IsNullOrWhiteSpace(eventId))
            return Result<ActiveCulturalEvent>.Failure("Event ID cannot be empty");

        if (string.IsNullOrWhiteSpace(name))
            return Result<ActiveCulturalEvent>.Failure("Event name cannot be empty");

        if (startTime >= estimatedEndTime)
            return Result<ActiveCulturalEvent>.Failure("Start time must be before estimated end time");

        if (currentTrafficMultiplier < 0)
            return Result<ActiveCulturalEvent>.Failure("Traffic multiplier cannot be negative");

        if (!affectedCommunities.Any())
            return Result<ActiveCulturalEvent>.Failure("At least one affected community must be specified");

        return Result<ActiveCulturalEvent>.Success(new ActiveCulturalEvent(
            eventId, name, eventType, significance, startTime, estimatedEndTime,
            currentTrafficMultiplier, affectedCommunities, detectedAt));
    }

    public bool IsCurrentlyActive(DateTime currentTime)
    {
        return currentTime >= StartTime && currentTime <= EstimatedEndTime;
    }

    public bool RequiresScaling(double scalingThreshold = 1.5)
    {
        return CurrentTrafficMultiplier > scalingThreshold;
    }

    public TimeSpan GetRemainingDuration(DateTime currentTime)
    {
        if (currentTime >= EstimatedEndTime)
            return TimeSpan.Zero;
        
        return EstimatedEndTime - currentTime;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return EventId;
        yield return Name;
        yield return EventType;
        yield return Significance;
        yield return StartTime;
        yield return EstimatedEndTime;
        yield return CurrentTrafficMultiplier;
        yield return DetectedAt;
    }
}

/// <summary>
/// Represents the urgency score calculated for auto-scaling decisions
/// </summary>
public class AutoScalingUrgencyScore : ValueObject
{
    public double PerformanceScore { get; set; }
    public double ConnectionPoolScore { get; set; }
    public double CulturalEventScore { get; set; }
    public double OverallUrgency { get; set; }
    public DateTime CalculatedAt { get; private set; }
    public Dictionary<string, double> DetailedMetrics { get; private set; }

    public AutoScalingUrgencyScore()
    {
        CalculatedAt = DateTime.UtcNow;
        DetailedMetrics = new Dictionary<string, double>();
    }

    public static Result<AutoScalingUrgencyScore> Create(
        double performanceScore,
        double connectionPoolScore,
        double culturalEventScore,
        double overallUrgency,
        Dictionary<string, double>? detailedMetrics = null)
    {
        if (performanceScore < 0 || performanceScore > 1)
            return Result<AutoScalingUrgencyScore>.Failure("Performance score must be between 0 and 1");

        if (connectionPoolScore < 0 || connectionPoolScore > 1)
            return Result<AutoScalingUrgencyScore>.Failure("Connection pool score must be between 0 and 1");

        if (culturalEventScore < 0 || culturalEventScore > 1)
            return Result<AutoScalingUrgencyScore>.Failure("Cultural event score must be between 0 and 1");

        if (overallUrgency < 0 || overallUrgency > 1)
            return Result<AutoScalingUrgencyScore>.Failure("Overall urgency must be between 0 and 1");

        var score = new AutoScalingUrgencyScore
        {
            PerformanceScore = performanceScore,
            ConnectionPoolScore = connectionPoolScore,
            CulturalEventScore = culturalEventScore,
            OverallUrgency = overallUrgency,
            DetailedMetrics = detailedMetrics != null ? new Dictionary<string, double>(detailedMetrics) : new Dictionary<string, double>()
        };

        return Result<AutoScalingUrgencyScore>.Success(score);
    }

    public ScalingUrgencyLevel GetUrgencyLevel()
    {
        return OverallUrgency switch
        {
            > 0.8 => ScalingUrgencyLevel.Critical,
            > 0.6 => ScalingUrgencyLevel.High,
            > 0.4 => ScalingUrgencyLevel.Medium,
            _ => ScalingUrgencyLevel.Low
        };
    }

    public bool RequiresImmediateAction()
    {
        return OverallUrgency > 0.8;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return PerformanceScore;
        yield return ConnectionPoolScore;
        yield return CulturalEventScore;
        yield return OverallUrgency;
        yield return CalculatedAt;
    }
}

/// <summary>
/// Represents an auto-scaling decision based on analyzed metrics
/// </summary>
public class AutoScalingDecision : Entity<string>
{
    public DateTime DecisionTimestamp { get; private set; }
    public AutoScalingUrgencyScore UrgencyScore { get; private set; }
    public bool ScalingRequired { get; set; }
    public ScalingPriority ScalingPriority { get; set; }
    public IReadOnlyList<ScalingAction> RecommendedActions { get; set; }
    public string DecisionReason { get; private set; }
    public Dictionary<string, object> DecisionMetadata { get; private set; }

    private AutoScalingDecision(
        string id,
        DateTime decisionTimestamp,
        AutoScalingUrgencyScore urgencyScore,
        string decisionReason) : base(id)
    {
        DecisionTimestamp = decisionTimestamp;
        UrgencyScore = urgencyScore;
        DecisionReason = decisionReason;
        RecommendedActions = new List<ScalingAction>().AsReadOnly();
        DecisionMetadata = new Dictionary<string, object>();
    }

    public static Result<AutoScalingDecision> Create(
        string id,
        AutoScalingUrgencyScore urgencyScore,
        string decisionReason)
    {
        if (string.IsNullOrWhiteSpace(id))
            return Result<AutoScalingDecision>.Failure("Decision ID cannot be empty");

        if (string.IsNullOrWhiteSpace(decisionReason))
            return Result<AutoScalingDecision>.Failure("Decision reason cannot be empty");

        return Result<AutoScalingDecision>.Success(new AutoScalingDecision(id, DateTime.UtcNow, urgencyScore, decisionReason) { Id = id });
    }

    public void UpdateRecommendedActions(IEnumerable<ScalingAction> actions)
    {
        RecommendedActions = actions.ToList().AsReadOnly();
    }

    public void AddMetadata(string key, object value)
    {
        DecisionMetadata[key] = value;
    }

    public bool HasHighPriorityActions()
    {
        return ScalingPriority == ScalingPriority.Critical || ScalingPriority == ScalingPriority.High;
    }

    public IEnumerable<ScalingAction> GetActionsByType(ScalingActionType actionType)
    {
        return RecommendedActions.Where(action => action.ActionType == actionType);
    }
}

/// <summary>
/// Context information for auto-scaling operations
/// </summary>
public class CulturalAutoScalingContext : ValueObject
{
    public IReadOnlyList<string> Communities { get; private set; }
    public IReadOnlyList<string> TargetPools { get; private set; }
    public IReadOnlyList<string> TargetRegions { get; private set; }
    public TimeSpan MonitoringWindow { get; private set; }
    public Dictionary<string, object> ScalingThresholds { get; private set; }

    private CulturalAutoScalingContext(
        IEnumerable<string> communities,
        IEnumerable<string> targetPools,
        IEnumerable<string> targetRegions,
        TimeSpan monitoringWindow,
        Dictionary<string, object> scalingThresholds)
    {
        Communities = communities.ToList().AsReadOnly();
        TargetPools = targetPools.ToList().AsReadOnly();
        TargetRegions = targetRegions.ToList().AsReadOnly();
        MonitoringWindow = monitoringWindow;
        ScalingThresholds = new Dictionary<string, object>(scalingThresholds);
    }

    public static Result<CulturalAutoScalingContext> Create(
        IEnumerable<string> communities,
        IEnumerable<string> targetPools,
        IEnumerable<string> targetRegions,
        TimeSpan monitoringWindow,
        Dictionary<string, object>? scalingThresholds = null)
    {
        if (!communities.Any())
            return Result<CulturalAutoScalingContext>.Failure("At least one community must be specified");

        if (!targetPools.Any())
            return Result<CulturalAutoScalingContext>.Failure("At least one target pool must be specified");

        if (!targetRegions.Any())
            return Result<CulturalAutoScalingContext>.Failure("At least one target region must be specified");

        if (monitoringWindow.TotalMinutes <= 0)
            return Result<CulturalAutoScalingContext>.Failure("Monitoring window must be positive");

        return Result<CulturalAutoScalingContext>.Success(new CulturalAutoScalingContext(
            communities, targetPools, targetRegions, monitoringWindow, 
            scalingThresholds ?? new Dictionary<string, object>()));
    }

    public T? GetThreshold<T>(string thresholdName, T? defaultValue = default(T))
    {
        if (ScalingThresholds.TryGetValue(thresholdName, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return string.Join(",", Communities.OrderBy(c => c));
        yield return string.Join(",", TargetPools.OrderBy(p => p));
        yield return string.Join(",", TargetRegions.OrderBy(r => r));
        yield return MonitoringWindow;
    }
}

/// <summary>
/// Enumerations for auto-scaling operations
/// </summary>
public enum ScalingUrgencyLevel
{
    Low,
    Medium,
    High,
    Critical
}

public enum ScalingPriority
{
    Low,
    Medium,
    High,
    Critical
}