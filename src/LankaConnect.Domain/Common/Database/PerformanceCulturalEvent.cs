using System;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.ValueObjects;

namespace LankaConnect.Domain.Common.Database;

/// <summary>
/// Represents a cultural event that impacts database performance
/// Used for monitoring and predicting performance during cultural celebrations
/// </summary>
public class PerformanceCulturalEvent
{
    public string EventId { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public CulturalEventType EventType { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Geographic regions affected by this cultural event
    /// </summary>
    public List<string> AffectedRegions { get; set; } = new();

    /// <summary>
    /// Expected performance impact multiplier (1.0 = normal, >1.0 = increased load)
    /// </summary>
    public decimal PerformanceImpactFactor { get; set; } = 1.0m;

    /// <summary>
    /// Cultural communities participating in this event
    /// </summary>
    public List<string> ParticipatingCommunities { get; set; } = new();

    /// <summary>
    /// Estimated attendee count for load prediction
    /// </summary>
    public long EstimatedAttendees { get; set; }

    /// <summary>
    /// Cultural significance level affecting engagement
    /// </summary>
    public CulturalSignificanceLevel SignificanceLevel { get; set; }

    /// <summary>
    /// Performance metrics captured during the event
    /// </summary>
    public CulturalEventPerformanceMetrics? PerformanceMetrics { get; set; }

    /// <summary>
    /// Languages primarily used during this event
    /// </summary>
    public List<string> PrimaryLanguages { get; set; } = new();

    /// <summary>
    /// Time zone where the event primarily occurs
    /// </summary>
    public string PrimaryTimeZone { get; set; } = string.Empty;

    /// <summary>
    /// Whether this event requires special database scaling
    /// </summary>
    public bool RequiresSpecialScaling { get; set; }

    /// <summary>
    /// Historical performance data from previous occurrences
    /// </summary>
    public List<HistoricalPerformanceData> HistoricalData { get; set; } = new();
}

/// <summary>
/// Cultural significance levels for events
/// </summary>
public enum CulturalSignificanceLevel
{
    Local = 1,
    Regional = 2,
    National = 3,
    International = 4,
    GlobalDiaspora = 5
}

/// <summary>
/// Performance metrics captured during cultural events
/// </summary>
public class CulturalEventPerformanceMetrics
{
    public string EventId { get; set; } = string.Empty;
    public DateTime MetricTimestamp { get; set; }
    public decimal DatabaseCpuUtilization { get; set; }
    public decimal DatabaseMemoryUtilization { get; set; }
    public int ConcurrentConnections { get; set; }
    public decimal AverageQueryResponseTime { get; set; }
    public long RequestsPerSecond { get; set; }
    public decimal CacheHitRatio { get; set; }
    public List<string> PerformanceBottlenecks { get; set; } = new();
    public bool PerformanceThresholdsExceeded { get; set; }
}

/// <summary>
/// Historical performance data for cultural events with comprehensive analytics
/// Enhanced for cultural intelligence with rich metrics and correlations
/// </summary>
public class HistoricalPerformanceData
{
    public string DataId { get; private set; }
    public string DatasetName { get; private set; }
    public Dictionary<string, List<PerformanceDataPoint>> MetricData { get; private set; }
    public DateTimeOffset StartTime { get; private set; }
    public DateTimeOffset EndTime { get; private set; }
    public IReadOnlyList<CulturalEventCorrelation> CulturalCorrelations { get; private set; }
    public TimeSpan DataResolution { get; private set; }

    // Legacy properties for backward compatibility
    public DateTime EventDate { get; set; }
    public decimal PeakCpuUtilization { get; set; }
    public decimal PeakMemoryUtilization { get; set; }
    public long PeakRequestsPerSecond { get; set; }
    public decimal AverageResponseTime { get; set; }
    public long TotalParticipants { get; set; }
    public bool ScalingTriggered { get; set; }
    public string ScalingActions { get; set; } = string.Empty;
    public decimal PerformanceScore { get; set; }
    public List<string> LessonsLearned { get; set; } = new();

    /// <summary>
    /// Gets the total duration of historical data
    /// </summary>
    public TimeSpan DataDuration => EndTime.Subtract(StartTime);

    /// <summary>
    /// Gets whether data includes cultural event patterns
    /// </summary>
    public bool IncludesCulturalPatterns => CulturalCorrelations.Any();

    /// <summary>
    /// Gets total data points across all metrics
    /// </summary>
    public int TotalDataPoints => MetricData.Values.Sum(list => list.Count);

    /// <summary>
    /// Gets data completeness percentage
    /// </summary>
    public double DataCompleteness
    {
        get
        {
            if (!MetricData.Any()) return 0.0;
            var expectedPoints = (int)(DataDuration.Ticks / DataResolution.Ticks) * MetricData.Count;
            return expectedPoints > 0 ? (double)TotalDataPoints / expectedPoints * 100.0 : 100.0;
        }
    }

    // Default constructor for legacy usage
    public HistoricalPerformanceData()
    {
        DataId = Guid.NewGuid().ToString();
        DatasetName = "Legacy Cultural Event Data";
        MetricData = new Dictionary<string, List<PerformanceDataPoint>>();
        StartTime = DateTimeOffset.UtcNow;
        EndTime = DateTimeOffset.UtcNow;
        CulturalCorrelations = Array.Empty<CulturalEventCorrelation>().ToList().AsReadOnly();
        DataResolution = TimeSpan.FromMinutes(5);
    }

    private HistoricalPerformanceData(string datasetName, Dictionary<string, List<PerformanceDataPoint>> metricData,
        DateTimeOffset startTime, DateTimeOffset endTime, IEnumerable<CulturalEventCorrelation> culturalCorrelations,
        TimeSpan dataResolution)
    {
        DataId = Guid.NewGuid().ToString();
        DatasetName = datasetName;
        MetricData = metricData;
        StartTime = startTime;
        EndTime = endTime;
        CulturalCorrelations = culturalCorrelations.ToList().AsReadOnly();
        DataResolution = dataResolution;
    }

    /// <summary>
    /// Creates historical performance data
    /// </summary>
    public static HistoricalPerformanceData Create(string datasetName,
        Dictionary<string, List<PerformanceDataPoint>> metricData, DateTimeOffset startTime, DateTimeOffset endTime,
        IEnumerable<CulturalEventCorrelation>? culturalCorrelations = null, TimeSpan? dataResolution = null)
    {
        return new HistoricalPerformanceData(datasetName, metricData, startTime, endTime,
            culturalCorrelations ?? Array.Empty<CulturalEventCorrelation>(),
            dataResolution ?? TimeSpan.FromMinutes(5));
    }

    /// <summary>
    /// Gets performance data for a specific metric
    /// </summary>
    public List<PerformanceDataPoint> GetMetricData(string metricName)
    {
        return MetricData.TryGetValue(metricName, out var data) ? data : new List<PerformanceDataPoint>();
    }
}

/// <summary>
/// Performance data point for cultural intelligence analytics
/// </summary>
public record PerformanceDataPoint(DateTimeOffset Timestamp, double Value, Dictionary<string, object>? Metadata = null);

/// <summary>
/// Cultural event correlation for performance analysis
/// </summary>
public record CulturalEventCorrelation(CulturalEventType EventType, DateTimeOffset EventTime,
    double PerformanceImpact, string? Description = null);

