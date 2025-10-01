using LankaConnect.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace LankaConnect.Domain.Common.Database;

/// <summary>
/// TDD GREEN Phase: Multi-Region Performance Monitoring Types Implementation
/// Enterprise-grade performance coordination for cultural intelligence platform
/// </summary>

#region MultiRegionPerformanceCoordination

/// <summary>
/// Coordinates performance monitoring across multiple geographic regions
/// for cultural intelligence diaspora communities
/// </summary>
public class MultiRegionPerformanceCoordination
{
    public Guid Id { get; private set; }
    public List<string> Regions { get; private set; } = new();
    public CoordinationStrategy Strategy { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? LastCoordinationAt { get; private set; }
    public Dictionary<string, CulturalLoadDistribution> RegionLoadDistribution { get; private set; } = new();
    public Dictionary<string, double> RegionPerformanceScores { get; private set; } = new();

    private MultiRegionPerformanceCoordination() { }

    private MultiRegionPerformanceCoordination(Guid id, List<string> regions, CoordinationStrategy strategy)
    {
        Id = id;
        Regions = new List<string>(regions);
        Strategy = strategy;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<MultiRegionPerformanceCoordination> Create(Guid id, List<string> regions, CoordinationStrategy strategy)
    {
        if (id == Guid.Empty)
            return Result<MultiRegionPerformanceCoordination>.Failure("Coordination ID cannot be empty");

        if (regions == null || regions.Count == 0)
            return Result<MultiRegionPerformanceCoordination>.Failure("At least one region must be specified");

        if (regions.Any(string.IsNullOrWhiteSpace))
            return Result<MultiRegionPerformanceCoordination>.Failure("All regions must have valid names");

        var coordination = new MultiRegionPerformanceCoordination(id, regions, strategy);
        return Result<MultiRegionPerformanceCoordination>.Success(coordination);
    }

    public Result AddRegion(string region)
    {
        if (string.IsNullOrWhiteSpace(region))
            return Result.Failure("Region name cannot be empty");

        if (Regions.Contains(region))
            return Result.Failure("Region already exists in coordination");

        Regions.Add(region);
        return Result.Success();
    }

    public Result RemoveRegion(string region)
    {
        if (string.IsNullOrWhiteSpace(region))
            return Result.Failure("Region name cannot be empty");

        if (Regions.Count <= 1)
            return Result.Failure("Cannot remove region when only one remains");

        var removed = Regions.Remove(region);
        if (!removed)
            return Result.Failure("Region not found in coordination");

        RegionLoadDistribution.Remove(region);
        RegionPerformanceScores.Remove(region);
        return Result.Success();
    }

    public CulturalLoadDistribution GetCulturalLoadDistribution(string eventType)
    {
        var distributionKey = $"{eventType}-distribution";

        if (!RegionLoadDistribution.ContainsKey(distributionKey))
        {
            var distribution = new CulturalLoadDistribution
            {
                EventType = eventType,
                RegionLoads = Regions.ToDictionary(r => r, r => 0.0),
                TotalLoad = 0.0,
                DistributionStrategy = Strategy.ToString()
            };
            RegionLoadDistribution[distributionKey] = distribution;
        }

        return RegionLoadDistribution[distributionKey];
    }

    public void UpdateRegionPerformanceScore(string region, double score)
    {
        if (Regions.Contains(region))
        {
            RegionPerformanceScores[region] = score;
            LastCoordinationAt = DateTime.UtcNow;
        }
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}

#endregion

#region SynchronizationPolicy

/// <summary>
/// Defines synchronization policies for cross-region cultural data coordination
/// </summary>
public class SynchronizationPolicy
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public SynchronizationType SyncType { get; private set; }
    public SyncPriority Priority { get; private set; }
    public bool IsEnabled { get; private set; } = true;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public TimeSpan SyncInterval { get; private set; } = TimeSpan.FromMinutes(5);
    public Dictionary<string, SyncPriority> CulturalEventPriorities { get; private set; } = new();
    public Dictionary<string, object> PolicyMetadata { get; private set; } = new();

    private SynchronizationPolicy() { }

    private SynchronizationPolicy(Guid id, string name, SynchronizationType syncType, SyncPriority priority)
    {
        Id = id;
        Name = name;
        SyncType = syncType;
        Priority = priority;
        IsEnabled = true;
        CreatedAt = DateTime.UtcNow;
    }

    public static Result<SynchronizationPolicy> Create(Guid id, string name, SynchronizationType syncType, SyncPriority priority)
    {
        if (id == Guid.Empty)
            return Result<SynchronizationPolicy>.Failure("Policy ID cannot be empty");

        if (string.IsNullOrWhiteSpace(name))
            return Result<SynchronizationPolicy>.Failure("Policy name cannot be empty");

        var policy = new SynchronizationPolicy(id, name, syncType, priority);
        return Result<SynchronizationPolicy>.Success(policy);
    }

    public void SetCulturalEventPriority(string eventType, SyncPriority priority)
    {
        if (!string.IsNullOrWhiteSpace(eventType))
        {
            CulturalEventPriorities[eventType] = priority;
        }
    }

    public void SetSyncInterval(TimeSpan interval)
    {
        if (interval > TimeSpan.Zero)
        {
            SyncInterval = interval;
        }
    }

    public SyncPriority GetCulturalEventPriority(string eventType)
    {
        return CulturalEventPriorities.TryGetValue(eventType, out var priority) ? priority : Priority;
    }

    public void Enable()
    {
        IsEnabled = true;
    }

    public void Disable()
    {
        IsEnabled = false;
    }
}

#endregion

#region RegionSyncResult

/// <summary>
/// Result of synchronization operation between regions
/// </summary>
public class RegionSyncResult
{
    public Guid SyncId { get; private set; }
    public string SourceRegion { get; private set; } = string.Empty;
    public string TargetRegion { get; private set; } = string.Empty;
    public SyncStatus Status { get; private set; }
    public TimeSpan SyncDuration { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public long DataSyncedBytes { get; private set; }
    public Dictionary<string, int> CulturalDataMetrics { get; private set; } = new();
    public List<string> SyncErrors { get; private set; } = new();

    private RegionSyncResult() { }

    private RegionSyncResult(Guid syncId, string sourceRegion, string targetRegion, SyncStatus status, TimeSpan syncDuration)
    {
        SyncId = syncId;
        SourceRegion = sourceRegion;
        TargetRegion = targetRegion;
        Status = status;
        SyncDuration = syncDuration;
        StartedAt = DateTime.UtcNow;

        if (status == SyncStatus.Success)
        {
            CompletedAt = StartedAt.Add(syncDuration);
        }
    }

    public static Result<RegionSyncResult> Create(Guid syncId, string sourceRegion, string targetRegion, SyncStatus status, TimeSpan syncDuration)
    {
        if (syncId == Guid.Empty)
            return Result<RegionSyncResult>.Failure("Sync ID cannot be empty");

        if (string.IsNullOrWhiteSpace(sourceRegion))
            return Result<RegionSyncResult>.Failure("Source region cannot be empty");

        if (string.IsNullOrWhiteSpace(targetRegion))
            return Result<RegionSyncResult>.Failure("Target region cannot be empty");

        if (syncDuration < TimeSpan.Zero)
            return Result<RegionSyncResult>.Failure("Sync duration cannot be negative");

        var result = new RegionSyncResult(syncId, sourceRegion, targetRegion, status, syncDuration);
        return Result<RegionSyncResult>.Success(result);
    }

    public void AddCulturalDataMetrics(string eventType, int recordCount)
    {
        if (!string.IsNullOrWhiteSpace(eventType))
        {
            CulturalDataMetrics[eventType] = recordCount;
        }
    }

    public void AddSyncError(string error)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            SyncErrors.Add(error);
        }
    }

    public void UpdateDataSynced(long bytes)
    {
        if (bytes >= 0)
        {
            DataSyncedBytes = bytes;
        }
    }

    public void MarkCompleted()
    {
        CompletedAt = DateTime.UtcNow;
        if (SyncErrors.Count == 0)
        {
            Status = SyncStatus.Success;
        }
        else
        {
            Status = SyncStatus.PartialFailure;
        }
    }
}

#endregion

#region PerformanceComparisonMetrics

/// <summary>
/// Metrics for comparing performance across different regions
/// </summary>
public class PerformanceComparisonMetrics
{
    public Guid Id { get; private set; }
    public string BaselineRegion { get; private set; } = string.Empty;
    public List<string> ComparisonRegions { get; private set; } = new();
    public DateTime ComparisonTime { get; private set; } = DateTime.UtcNow;
    public Dictionary<string, Dictionary<string, double>> CulturalMetrics { get; private set; } = new();
    public Dictionary<string, double> BaselineMetrics { get; private set; } = new();
    public Dictionary<string, PerformanceVariance> RegionVariances { get; private set; } = new();

    private PerformanceComparisonMetrics() { }

    private PerformanceComparisonMetrics(Guid id, string baselineRegion, List<string> comparisonRegions)
    {
        Id = id;
        BaselineRegion = baselineRegion;
        ComparisonRegions = new List<string>(comparisonRegions);
        ComparisonTime = DateTime.UtcNow;
    }

    public static Result<PerformanceComparisonMetrics> Create(Guid id, string baselineRegion, List<string> comparisonRegions)
    {
        if (id == Guid.Empty)
            return Result<PerformanceComparisonMetrics>.Failure("Metrics ID cannot be empty");

        if (string.IsNullOrWhiteSpace(baselineRegion))
            return Result<PerformanceComparisonMetrics>.Failure("Baseline region cannot be empty");

        if (comparisonRegions == null || comparisonRegions.Count == 0)
            return Result<PerformanceComparisonMetrics>.Failure("At least one comparison region must be specified");

        var metrics = new PerformanceComparisonMetrics(id, baselineRegion, comparisonRegions);
        return Result<PerformanceComparisonMetrics>.Success(metrics);
    }

    public void AddCulturalMetric(string culturalEventType, string metricName, double value)
    {
        if (string.IsNullOrWhiteSpace(culturalEventType) || string.IsNullOrWhiteSpace(metricName))
            return;

        if (!CulturalMetrics.ContainsKey(culturalEventType))
        {
            CulturalMetrics[culturalEventType] = new Dictionary<string, double>();
        }

        CulturalMetrics[culturalEventType][metricName] = value;
    }

    public void SetBaselineMetric(string metricName, double value)
    {
        if (!string.IsNullOrWhiteSpace(metricName))
        {
            BaselineMetrics[metricName] = value;
        }
    }

    public void CalculateVariances()
    {
        foreach (var region in ComparisonRegions)
        {
            var variance = new PerformanceVariance
            {
                Region = region,
                VariancePercentage = CalculateRegionVariance(region),
                CalculatedAt = DateTime.UtcNow
            };
            RegionVariances[region] = variance;
        }
    }

    private double CalculateRegionVariance(string region)
    {
        // Simplified variance calculation
        var culturalMetricsCount = CulturalMetrics.Values.Sum(dict => dict.Count);
        var baselineCount = BaselineMetrics.Count;

        if (baselineCount == 0) return 0.0;

        return Math.Abs((culturalMetricsCount - baselineCount) / (double)baselineCount) * 100;
    }
}

#endregion

#region Supporting Types and Enums

/// <summary>
/// Cultural load distribution across regions
/// </summary>
public class CulturalLoadDistribution
{
    public string EventType { get; set; } = string.Empty;
    public Dictionary<string, double> RegionLoads { get; set; } = new();
    public double TotalLoad { get; set; }
    public string DistributionStrategy { get; set; } = string.Empty;
}

/// <summary>
/// Performance variance between regions
/// </summary>
public class PerformanceVariance
{
    public string Region { get; set; } = string.Empty;
    public double VariancePercentage { get; set; }
    public DateTime CalculatedAt { get; set; }
}

/// <summary>
/// Coordination strategies for multi-region performance
/// </summary>
public enum CoordinationStrategy
{
    LoadBalanced = 1,
    LatencyOptimized = 2,
    CulturallyAware = 3,
    CostOptimized = 4
}

/// <summary>
/// Types of synchronization for cultural data
/// </summary>
public enum SynchronizationType
{
    RealTime = 1,
    NearRealTime = 2,
    Batch = 3,
    OnDemand = 4
}

/// <summary>
/// Priority levels for synchronization
/// </summary>
public enum SyncPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

/// <summary>
/// Status of synchronization operations
/// </summary>
public enum SyncStatus
{
    Pending = 1,
    InProgress = 2,
    Success = 3,
    Failed = 4,
    PartialFailure = 5,
    Cancelled = 6
}

#endregion