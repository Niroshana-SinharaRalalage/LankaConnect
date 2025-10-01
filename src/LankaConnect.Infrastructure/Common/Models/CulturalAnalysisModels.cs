using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared;

namespace LankaConnect.Infrastructure.Common.Models;

/// <summary>
/// Cultural analysis result for Infrastructure layer cultural intelligence processing.
/// Infrastructure-specific implementation for Sri Lankan diaspora platform analytics.
/// </summary>
public record CulturalAnalysisResult
{
    public string AnalysisId { get; init; } = string.Empty;
    public DateTime AnalysisTimestamp { get; init; } = DateTime.UtcNow;
    public string CulturalRegion { get; init; } = string.Empty;
    public string CulturalEventType { get; init; } = string.Empty;
    public double CulturalSignificanceScore { get; init; }
    public double TrafficMultiplier { get; init; }
    public IReadOnlyList<string> AffectedCommunities { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, double> MetricsByLanguage { get; init; } = new Dictionary<string, double>();
    public IReadOnlyDictionary<string, object> AdditionalMetadata { get; init; } = new Dictionary<string, object>();
    public bool RequiresPriorityHandling { get; init; }

    public static Result<CulturalAnalysisResult> Create(
        string analysisId,
        string culturalRegion,
        string culturalEventType,
        double significanceScore,
        double trafficMultiplier)
    {
        if (string.IsNullOrWhiteSpace(analysisId))
            return Result<CulturalAnalysisResult>.Failure("Analysis ID cannot be empty");

        if (significanceScore < 0 || significanceScore > 100)
            return Result<CulturalAnalysisResult>.Failure("Significance score must be between 0 and 100");

        if (trafficMultiplier < 0)
            return Result<CulturalAnalysisResult>.Failure("Traffic multiplier cannot be negative");

        return Result<CulturalAnalysisResult>.Success(new CulturalAnalysisResult
        {
            AnalysisId = analysisId,
            CulturalRegion = culturalRegion,
            CulturalEventType = culturalEventType,
            CulturalSignificanceScore = significanceScore,
            TrafficMultiplier = trafficMultiplier,
            RequiresPriorityHandling = significanceScore > 80 || trafficMultiplier > 2.0
        });
    }

    public bool IsSacredEvent() => CulturalSignificanceScore > 90;
    public bool RequiresScaling() => TrafficMultiplier > 1.5;
    public bool IsMultiCommunity() => AffectedCommunities.Count > 1;
}

/// <summary>
/// Cultural load model for Infrastructure traffic and scaling analysis.
/// </summary>
public record CulturalLoadModel
{
    public string ModelId { get; init; } = string.Empty;
    public DateTime PredictionTimestamp { get; init; } = DateTime.UtcNow;
    public string CulturalEventType { get; init; } = string.Empty;
    public TimeSpan PredictedDuration { get; init; }
    public double PeakLoadMultiplier { get; init; }
    public double BaselineLoad { get; init; }
    public double PredictedPeakLoad { get; init; }
    public IReadOnlyList<LoadPredictionPoint> LoadPredictions { get; init; } = Array.Empty<LoadPredictionPoint>();
    public IReadOnlyDictionary<string, double> RegionalDistribution { get; init; } = new Dictionary<string, double>();
    public double ConfidenceLevel { get; init; }

    public static CulturalLoadModel Create(
        string culturalEventType,
        double peakLoadMultiplier,
        double baselineLoad,
        TimeSpan predictedDuration)
    {
        return new CulturalLoadModel
        {
            ModelId = Guid.NewGuid().ToString(),
            CulturalEventType = culturalEventType,
            PeakLoadMultiplier = peakLoadMultiplier,
            BaselineLoad = baselineLoad,
            PredictedPeakLoad = baselineLoad * peakLoadMultiplier,
            PredictedDuration = predictedDuration,
            ConfidenceLevel = 0.85
        };
    }

    public bool RequiresPreemptiveScaling() => PeakLoadMultiplier > 2.0;
    public bool IsHighConfidence() => ConfidenceLevel > 0.8;
    public double GetExpectedLoadAt(DateTime timestamp) =>
        LoadPredictions.FirstOrDefault(p => p.Timestamp.Date == timestamp.Date)?.LoadMultiplier ?? BaselineLoad;
}

/// <summary>
/// Load prediction point for temporal load modeling.
/// </summary>
public record LoadPredictionPoint
{
    public DateTime Timestamp { get; init; }
    public double LoadMultiplier { get; init; }
    public double ConfidenceLevel { get; init; }
    public string CulturalContext { get; init; } = string.Empty;

    public static LoadPredictionPoint Create(DateTime timestamp, double loadMultiplier, double confidence, string context = "")
    {
        return new LoadPredictionPoint
        {
            Timestamp = timestamp,
            LoadMultiplier = loadMultiplier,
            ConfidenceLevel = confidence,
            CulturalContext = context
        };
    }

    public bool IsHighLoad() => LoadMultiplier > 2.0;
    public bool IsReliable() => ConfidenceLevel > 0.7;
}

/// <summary>
/// Cultural event traffic request for Infrastructure routing.
/// </summary>
public record CulturalEventTrafficRequest
{
    public string RequestId { get; init; } = string.Empty;
    public string CulturalEventType { get; init; } = string.Empty;
    public string SourceRegion { get; init; } = string.Empty;
    public string TargetRegion { get; init; } = string.Empty;
    public DateTime RequestTimestamp { get; init; } = DateTime.UtcNow;
    public int ExpectedConnections { get; init; }
    public double PriorityLevel { get; init; }
    public IReadOnlyList<string> RequiredLanguages { get; init; } = Array.Empty<string>();
    public bool RequiresCulturalValidation { get; init; }

    public static CulturalEventTrafficRequest Create(
        string culturalEventType,
        string sourceRegion,
        string targetRegion,
        int expectedConnections,
        double priorityLevel)
    {
        return new CulturalEventTrafficRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            CulturalEventType = culturalEventType,
            SourceRegion = sourceRegion,
            TargetRegion = targetRegion,
            ExpectedConnections = expectedConnections,
            PriorityLevel = priorityLevel,
            RequiresCulturalValidation = priorityLevel > 8.0
        };
    }

    public bool IsHighPriority() => PriorityLevel > 7.0;
    public bool IsCrossRegional() => SourceRegion != TargetRegion;
}

/// <summary>
/// Cultural event load distribution for Infrastructure load balancing.
/// </summary>
public record CulturalEventLoadDistribution
{
    public string DistributionId { get; init; } = string.Empty;
    public string CulturalEventType { get; init; } = string.Empty;
    public DateTime DistributionTimestamp { get; init; } = DateTime.UtcNow;
    public IReadOnlyDictionary<string, double> RegionalLoads { get; init; } = new Dictionary<string, double>();
    public IReadOnlyDictionary<string, int> ConnectionCounts { get; init; } = new Dictionary<string, int>();
    public double TotalLoadFactor { get; init; }
    public string PrimaryRegion { get; init; } = string.Empty;
    public IReadOnlyList<string> OverloadedRegions { get; init; } = Array.Empty<string>();

    public static CulturalEventLoadDistribution Create(
        string culturalEventType,
        IDictionary<string, double> regionalLoads,
        IDictionary<string, int> connectionCounts)
    {
        var totalLoad = regionalLoads.Values.Sum();
        var overloaded = regionalLoads.Where(kv => kv.Value > 5.0).Select(kv => kv.Key).ToList();
        var primary = regionalLoads.OrderByDescending(kv => kv.Value).FirstOrDefault().Key;

        return new CulturalEventLoadDistribution
        {
            DistributionId = Guid.NewGuid().ToString(),
            CulturalEventType = culturalEventType,
            RegionalLoads = regionalLoads.AsReadOnly(),
            ConnectionCounts = connectionCounts.AsReadOnly(),
            TotalLoadFactor = totalLoad,
            PrimaryRegion = primary ?? string.Empty,
            OverloadedRegions = overloaded.AsReadOnly()
        };
    }

    public bool RequiresLoadBalancing() => OverloadedRegions.Any();
    public bool HasUniformDistribution() => RegionalLoads.Values.Max() / RegionalLoads.Values.Min() < 2.0;
    public double GetRegionalLoad(string region) => RegionalLoads.GetValueOrDefault(region, 0.0);
}

/// <summary>
/// Validation level enumeration for Infrastructure data validation.
/// </summary>
public enum ValidationLevel
{
    Basic,
    Standard,
    Enhanced,
    Comprehensive,
    CulturallyAware
}