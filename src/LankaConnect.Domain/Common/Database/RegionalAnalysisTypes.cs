using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Common.Database;

/// <summary>
/// TDD GREEN Phase: Regional Analysis Types Implementation
/// Performance analysis and failover types for cultural intelligence platform
/// </summary>

#region RegionalPerformanceAnalysis

/// <summary>
/// Analyzes performance disparities across different geographic regions for cultural events
/// </summary>
public class RegionalPerformanceAnalysis
{
    public Guid Id { get; private set; }
    public List<string> AnalyzedRegions { get; private set; } = new();
    public DateTime AnalysisStartTime { get; private set; }
    public DateTime AnalysisEndTime { get; private set; }
    public Dictionary<string, RegionSpecificPerformanceMetrics> RegionMetrics { get; private set; } = new();
    public List<PerformanceDisparity> IdentifiedDisparities { get; private set; } = new();
    public Dictionary<string, CulturalEventPerformance> CulturalEventAnalysis { get; private set; } = new();
    public AnalysisStatus Status { get; private set; } = AnalysisStatus.InProgress;

    private RegionalPerformanceAnalysis() { }

    private RegionalPerformanceAnalysis(Guid id, List<string> regions, DateTime startTime, DateTime endTime)
    {
        Id = id;
        AnalyzedRegions = new List<string>(regions);
        AnalysisStartTime = startTime;
        AnalysisEndTime = endTime;
        Status = AnalysisStatus.InProgress;
    }

    public static Result<RegionalPerformanceAnalysis> Create(Guid id, List<string> regions, DateTime startTime, DateTime endTime)
    {
        if (id == Guid.Empty)
            return Result<RegionalPerformanceAnalysis>.Failure("Analysis ID cannot be empty");

        if (regions == null || regions.Count < 2)
            return Result<RegionalPerformanceAnalysis>.Failure("At least two regions must be specified for comparison");

        if (startTime >= endTime)
            return Result<RegionalPerformanceAnalysis>.Failure("Start time must be before end time");

        var analysis = new RegionalPerformanceAnalysis(id, regions, startTime, endTime);
        return Result<RegionalPerformanceAnalysis>.Success(analysis);
    }

    public void AddRegionMetrics(string region, RegionSpecificPerformanceMetrics metrics)
    {
        if (!string.IsNullOrWhiteSpace(region) && AnalyzedRegions.Contains(region))
        {
            RegionMetrics[region] = metrics;
        }
    }

    public void AddCulturalEventAnalysis(string eventType, CulturalEventPerformance performance)
    {
        if (!string.IsNullOrWhiteSpace(eventType))
        {
            CulturalEventAnalysis[eventType] = performance;
        }
    }

    public void AnalyzePerformanceDisparities()
    {
        IdentifiedDisparities.Clear();

        var regionMetricsList = RegionMetrics.ToList();
        for (int i = 0; i < regionMetricsList.Count; i++)
        {
            for (int j = i + 1; j < regionMetricsList.Count; j++)
            {
                var region1 = regionMetricsList[i];
                var region2 = regionMetricsList[j];

                var disparity = CalculateDisparity(region1.Key, region1.Value, region2.Key, region2.Value);
                if (disparity.DisparityLevel > DisparityLevel.Low)
                {
                    IdentifiedDisparities.Add(disparity);
                }
            }
        }
    }

    public void CompleteAnalysis()
    {
        AnalyzePerformanceDisparities();
        Status = AnalysisStatus.Completed;
    }

    private PerformanceDisparity CalculateDisparity(string region1, RegionSpecificPerformanceMetrics metrics1,
                                                   string region2, RegionSpecificPerformanceMetrics metrics2)
    {
        var responseTimeDiff = Math.Abs(metrics1.AverageResponseTime - metrics2.AverageResponseTime);
        var throughputDiff = Math.Abs(metrics1.Throughput - metrics2.Throughput);

        var disparityLevel = DisparityLevel.Low;
        if (responseTimeDiff > 1000 || throughputDiff > 100) // ms and requests/sec thresholds
            disparityLevel = DisparityLevel.High;
        else if (responseTimeDiff > 500 || throughputDiff > 50)
            disparityLevel = DisparityLevel.Medium;

        return new PerformanceDisparity
        {
            Region1 = region1,
            Region2 = region2,
            DisparityLevel = disparityLevel,
            ResponseTimeDifference = responseTimeDiff,
            ThroughputDifference = throughputDiff,
            IdentifiedAt = DateTime.UtcNow
        };
    }
}

#endregion

#region FailoverTriggerCriteria

/// <summary>
/// Criteria for triggering regional failover during performance degradation
/// </summary>
public class FailoverTriggerCriteria
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public double ResponseTimeThresholdMs { get; private set; }
    public double ErrorRateThresholdPercent { get; private set; }
    public double ThroughputThresholdPercent { get; private set; }
    public TimeSpan EvaluationWindow { get; private set; }
    public int ConsecutiveFailuresRequired { get; private set; }
    public bool IsEnabled { get; private set; } = true;
    public Dictionary<string, CulturalEventCriteria> CulturalEventSpecificCriteria { get; private set; } = new();
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private FailoverTriggerCriteria() { }

    private FailoverTriggerCriteria(Guid id, string name, double responseTimeThreshold,
                                   double errorRateThreshold, double throughputThreshold,
                                   TimeSpan evaluationWindow, int consecutiveFailures)
    {
        Id = id;
        Name = name;
        ResponseTimeThresholdMs = responseTimeThreshold;
        ErrorRateThresholdPercent = errorRateThreshold;
        ThroughputThresholdPercent = throughputThreshold;
        EvaluationWindow = evaluationWindow;
        ConsecutiveFailuresRequired = consecutiveFailures;
        IsEnabled = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<FailoverTriggerCriteria> Create(Guid id, string name, double responseTimeThreshold,
                                                        double errorRateThreshold, double throughputThreshold,
                                                        TimeSpan evaluationWindow, int consecutiveFailures)
    {
        if (id == Guid.Empty)
            return Result<FailoverTriggerCriteria>.Failure("Criteria ID cannot be empty");

        if (string.IsNullOrWhiteSpace(name))
            return Result<FailoverTriggerCriteria>.Failure("Criteria name cannot be empty");

        if (responseTimeThreshold <= 0)
            return Result<FailoverTriggerCriteria>.Failure("Response time threshold must be positive");

        if (errorRateThreshold < 0 || errorRateThreshold > 100)
            return Result<FailoverTriggerCriteria>.Failure("Error rate threshold must be between 0 and 100");

        if (evaluationWindow <= TimeSpan.Zero)
            return Result<FailoverTriggerCriteria>.Failure("Evaluation window must be positive");

        if (consecutiveFailures <= 0)
            return Result<FailoverTriggerCriteria>.Failure("Consecutive failures must be positive");

        var criteria = new FailoverTriggerCriteria(id, name, responseTimeThreshold, errorRateThreshold,
                                                  throughputThreshold, evaluationWindow, consecutiveFailures);
        return Result<FailoverTriggerCriteria>.Success(criteria);
    }

    public void AddCulturalEventCriteria(string eventType, CulturalEventCriteria criteria)
    {
        if (!string.IsNullOrWhiteSpace(eventType))
        {
            CulturalEventSpecificCriteria[eventType] = criteria;
        }
    }

    public bool ShouldTriggerFailover(RegionalPerformanceMetrics currentMetrics, string culturalEventType = "")
    {
        if (!IsEnabled)
            return false;

        var criteria = GetEffectiveCriteria(culturalEventType);

        if (currentMetrics.ResponseTimeMs > criteria.ResponseTimeThreshold)
            return true;

        if (currentMetrics.ErrorRatePercent > criteria.ErrorRateThreshold)
            return true;

        if (currentMetrics.ThroughputPercent < criteria.ThroughputThreshold)
            return true;

        return false;
    }

    private EffectiveCriteria GetEffectiveCriteria(string culturalEventType)
    {
        if (!string.IsNullOrWhiteSpace(culturalEventType) &&
            CulturalEventSpecificCriteria.TryGetValue(culturalEventType, out var eventCriteria))
        {
            return new EffectiveCriteria
            {
                ResponseTimeThreshold = eventCriteria.ResponseTimeThresholdMs ?? ResponseTimeThresholdMs,
                ErrorRateThreshold = eventCriteria.ErrorRateThresholdPercent ?? ErrorRateThresholdPercent,
                ThroughputThreshold = eventCriteria.ThroughputThresholdPercent ?? ThroughputThresholdPercent
            };
        }

        return new EffectiveCriteria
        {
            ResponseTimeThreshold = ResponseTimeThresholdMs,
            ErrorRateThreshold = ErrorRateThresholdPercent,
            ThroughputThreshold = ThroughputThresholdPercent
        };
    }

    public void Enable() => IsEnabled = true;
    public void Disable() => IsEnabled = false;
}

#endregion

#region RegionFailoverResult

/// <summary>
/// Result of regional failover operation with cultural event context
/// </summary>
public class RegionFailoverResult
{
    public Guid Id { get; private set; }
    public string PrimaryRegion { get; private set; } = string.Empty;
    public string FailoverRegion { get; private set; } = string.Empty;
    public FailoverStatus Status { get; private set; }
    public DateTime InitiatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public TimeSpan FailoverDuration { get; private set; }
    public string TriggerReason { get; private set; } = string.Empty;
    public List<string> CulturalEventsAffected { get; private set; } = new();
    public Dictionary<string, int> DataMigrationMetrics { get; private set; } = new();
    public List<string> FailoverErrors { get; private set; } = new();

    private RegionFailoverResult() { }

    private RegionFailoverResult(Guid id, string primaryRegion, string failoverRegion, string triggerReason)
    {
        Id = id;
        PrimaryRegion = primaryRegion;
        FailoverRegion = failoverRegion;
        TriggerReason = triggerReason;
        Status = FailoverStatus.Initiated;
        InitiatedAt = DateTime.UtcNow;
    }

    public static Result<RegionFailoverResult> Create(Guid id, string primaryRegion, string failoverRegion, string triggerReason)
    {
        if (id == Guid.Empty)
            return Result<RegionFailoverResult>.Failure("Failover result ID cannot be empty");

        if (string.IsNullOrWhiteSpace(primaryRegion))
            return Result<RegionFailoverResult>.Failure("Primary region cannot be empty");

        if (string.IsNullOrWhiteSpace(failoverRegion))
            return Result<RegionFailoverResult>.Failure("Failover region cannot be empty");

        if (primaryRegion.Equals(failoverRegion, StringComparison.OrdinalIgnoreCase))
            return Result<RegionFailoverResult>.Failure("Primary and failover regions cannot be the same");

        if (string.IsNullOrWhiteSpace(triggerReason))
            return Result<RegionFailoverResult>.Failure("Trigger reason cannot be empty");

        var result = new RegionFailoverResult(id, primaryRegion, failoverRegion, triggerReason);
        return Result<RegionFailoverResult>.Success(result);
    }

    public void AddAffectedCulturalEvent(string eventType)
    {
        if (!string.IsNullOrWhiteSpace(eventType) && !CulturalEventsAffected.Contains(eventType))
        {
            CulturalEventsAffected.Add(eventType);
        }
    }

    public void AddDataMigrationMetric(string dataType, int recordCount)
    {
        if (!string.IsNullOrWhiteSpace(dataType))
        {
            DataMigrationMetrics[dataType] = recordCount;
        }
    }

    public void AddError(string error)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            FailoverErrors.Add($"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC: {error}");
        }
    }

    public void MarkInProgress()
    {
        Status = FailoverStatus.InProgress;
    }

    public void MarkCompleted()
    {
        Status = FailoverStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        FailoverDuration = CompletedAt.Value - InitiatedAt;
    }

    public void MarkFailed(string reason)
    {
        Status = FailoverStatus.Failed;
        CompletedAt = DateTime.UtcNow;
        AddError($"Failover failed: {reason}");
    }
}

#endregion

#region Supporting Types and Enums

/// <summary>
/// Performance metrics for a specific region
/// </summary>
public class RegionSpecificPerformanceMetrics
{
    public string Region { get; set; } = string.Empty;
    public double AverageResponseTime { get; set; }
    public double Throughput { get; set; }
    public double ErrorRate { get; set; }
    public double CpuUtilization { get; set; }
    public double MemoryUtilization { get; set; }
    public Dictionary<string, double> CulturalEventMetrics { get; set; } = new();
}

/// <summary>
/// Performance analysis for cultural events
/// </summary>
public class CulturalEventPerformance
{
    public string EventType { get; set; } = string.Empty;
    public Dictionary<string, double> RegionPerformance { get; set; } = new();
    public double AveragePerformance { get; set; }
    public string BestPerformingRegion { get; set; } = string.Empty;
    public string WorstPerformingRegion { get; set; } = string.Empty;
}

/// <summary>
/// Performance disparity between regions
/// </summary>
public class PerformanceDisparity
{
    public string Region1 { get; set; } = string.Empty;
    public string Region2 { get; set; } = string.Empty;
    public DisparityLevel DisparityLevel { get; set; }
    public double ResponseTimeDifference { get; set; }
    public double ThroughputDifference { get; set; }
    public DateTime IdentifiedAt { get; set; }
}

/// <summary>
/// Cultural event specific criteria
/// </summary>
public class CulturalEventCriteria
{
    public double? ResponseTimeThresholdMs { get; set; }
    public double? ErrorRateThresholdPercent { get; set; }
    public double? ThroughputThresholdPercent { get; set; }
    public bool RequiresSacredDataProtection { get; set; }
}

/// <summary>
/// Effective criteria for evaluation
/// </summary>
public class EffectiveCriteria
{
    public double ResponseTimeThreshold { get; set; }
    public double ErrorRateThreshold { get; set; }
    public double ThroughputThreshold { get; set; }
}

/// <summary>
/// Current regional performance metrics
/// </summary>
public class RegionalPerformanceMetrics
{
    public double ResponseTimeMs { get; set; }
    public double ErrorRatePercent { get; set; }
    public double ThroughputPercent { get; set; }
    public DateTime MeasuredAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Analysis status
/// </summary>
public enum AnalysisStatus
{
    InProgress = 1,
    Completed = 2,
    Failed = 3,
    Cancelled = 4
}

/// <summary>
/// Disparity levels
/// </summary>
public enum DisparityLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

/// <summary>
/// Failover status
/// </summary>
public enum FailoverStatus
{
    Initiated = 1,
    InProgress = 2,
    Completed = 3,
    Failed = 4,
    RolledBack = 5
}

#endregion