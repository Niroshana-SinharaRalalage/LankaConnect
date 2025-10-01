using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Performance;

/// <summary>
/// TDD GREEN Phase: Performance Monitoring Result Types Implementation
/// Comprehensive result patterns for Cultural Intelligence performance monitoring, coordination, and analysis
/// </summary>

#region Multi-Region Performance Coordination

/// <summary>
/// Result type for multi-region performance coordination operations
/// </summary>
public class MultiRegionPerformanceCoordination : Result<PerformanceCoordinationMetrics>
{
    public PerformanceCoordinationMetrics? CoordinationMetrics => IsSuccess ? Value : null;
    public bool IsHealthy => CoordinationMetrics?.IsHealthy ?? false;
    public int TotalRegions => CoordinationMetrics?.TotalRegions ?? 0;
    public decimal SuccessRate => CoordinationMetrics?.CoordinationSuccessRate ?? 0m;

    private MultiRegionPerformanceCoordination(bool isSuccess, IEnumerable<string> errors, PerformanceCoordinationMetrics? value = null)
        : base(isSuccess, errors, value) { }

    public static new MultiRegionPerformanceCoordination Success(PerformanceCoordinationMetrics metrics)
        => new(true, Array.Empty<string>(), metrics);

    public static new MultiRegionPerformanceCoordination Failure(string error)
        => new(false, new[] { error });

    public static new MultiRegionPerformanceCoordination Failure(IEnumerable<string> errors)
        => new(false, errors);
}

/// <summary>
/// Metrics for multi-region performance coordination
/// </summary>
public class PerformanceCoordinationMetrics
{
    public int TotalRegions { get; init; }
    public int ActiveRegions { get; init; }
    public decimal AverageLatencyMs { get; init; }
    public long ThroughputPerSecond { get; init; }
    public decimal CoordinationSuccessRate { get; init; }
    public DateTime LastCoordinationTimestamp { get; init; } = DateTime.UtcNow;
    public List<string> RegionDetails { get; init; } = new();
    public Dictionary<string, decimal> RegionLatencies { get; init; } = new();

    /// <summary>
    /// Determines if the coordination is healthy based on thresholds
    /// </summary>
    public bool IsHealthy => 
        ActiveRegions == TotalRegions && 
        AverageLatencyMs < 100m && 
        CoordinationSuccessRate >= 95m;
}

#endregion

#region Region Synchronization Results

/// <summary>
/// Result type for region synchronization operations
/// </summary>
public class RegionSyncResult : Result<RegionSyncSummary>
{
    public RegionSyncSummary? SyncSummary => IsSuccess ? Value : null;
    public bool IsSynced => SyncSummary?.IsSynced ?? false;
    public long RecordsSynced => SyncSummary?.RecordsSynced ?? 0;
    public int SyncDurationSeconds => (int)((SyncSummary?.SyncDurationMs ?? 0) / 1000);

    private RegionSyncResult(bool isSuccess, IEnumerable<string> errors, RegionSyncSummary? value = null)
        : base(isSuccess, errors, value) { }

    public static new RegionSyncResult Success(RegionSyncSummary summary)
        => new(true, Array.Empty<string>(), summary);

    public static new RegionSyncResult Failure(string error)
        => new(false, new[] { error });

    public static new RegionSyncResult Failure(IEnumerable<string> errors)
        => new(false, errors);
}

/// <summary>
/// Summary of region synchronization operations
/// </summary>
public class RegionSyncSummary
{
    public required string SourceRegion { get; init; }
    public required string TargetRegion { get; init; }
    public long RecordsSynced { get; init; }
    public long SyncDurationMs { get; init; }
    public decimal DataTransferredGB { get; init; }
    public decimal SyncSuccessRate { get; init; }
    public DateTime LastSyncTimestamp { get; init; } = DateTime.UtcNow;
    public List<string> SyncDetails { get; init; } = new();
    public Dictionary<string, object> SyncMetadata { get; init; } = new();

    /// <summary>
    /// Determines if the sync is complete based on success rate threshold
    /// </summary>
    public bool IsSynced => SyncSuccessRate >= 95m;
}

#endregion

#region Regional Performance Analysis

/// <summary>
/// Result type for regional performance analysis operations
/// </summary>
public class RegionalPerformanceAnalysis : Result<PerformanceAnalysisData>
{
    public PerformanceAnalysisData? AnalysisData => IsSuccess ? Value : null;
    public bool IsPerformant => AnalysisData?.IsPerformant ?? false;
    public string RegionName => AnalysisData?.RegionName ?? string.Empty;
    public decimal EngagementScore => AnalysisData?.DiasporaEngagementScore ?? 0m;

    private RegionalPerformanceAnalysis(bool isSuccess, IEnumerable<string> errors, PerformanceAnalysisData? value = null)
        : base(isSuccess, errors, value) { }

    public static new RegionalPerformanceAnalysis Success(PerformanceAnalysisData data)
        => new(true, Array.Empty<string>(), data);

    public static new RegionalPerformanceAnalysis Failure(string error)
        => new(false, new[] { error });

    public static new RegionalPerformanceAnalysis Failure(IEnumerable<string> errors)
        => new(false, errors);
}

/// <summary>
/// Data for regional performance analysis
/// </summary>
public class PerformanceAnalysisData
{
    public required string RegionName { get; init; }
    public int AnalysisPeriodHours { get; init; }
    public long TotalRequests { get; init; }
    public decimal AverageResponseTimeMs { get; init; }
    public decimal P95ResponseTimeMs { get; init; }
    public decimal ErrorRate { get; init; }
    public long CulturalEventProcessingRate { get; init; }
    public decimal DiasporaEngagementScore { get; init; }
    public DateTime AnalysisTimestamp { get; init; } = DateTime.UtcNow;
    public Dictionary<string, decimal> PerformanceBreakdown { get; init; } = new();

    /// <summary>
    /// Determines if the region's performance meets quality thresholds
    /// </summary>
    public bool IsPerformant => 
        AverageResponseTimeMs < 200m && 
        ErrorRate < 1.0m && 
        DiasporaEngagementScore >= 80m;
}

#endregion

#region Performance Comparison Metrics

/// <summary>
/// Value object for performance comparison between regions
/// </summary>
public class PerformanceComparisonMetrics
{
    public required string BaselineRegion { get; init; }
    public required string ComparisonRegion { get; init; }
    public decimal LatencyDifferenceMs { get; init; }
    public decimal ThroughputDifferencePercent { get; init; }
    public decimal ErrorRateDifferencePercent { get; init; }
    public decimal CulturalAccuracyDifferencePercent { get; init; }
    public decimal OverallPerformanceScore { get; init; }
    public DateTime ComparisonTimestamp { get; init; } = DateTime.UtcNow;
    public Dictionary<string, decimal> DetailedMetrics { get; init; } = new();

    /// <summary>
    /// Determines if there are significant performance differences
    /// </summary>
    public bool HasSignificantDifference =>
        Math.Abs(LatencyDifferenceMs) > 10m ||
        Math.Abs(ThroughputDifferencePercent) > 5m ||
        Math.Abs(ErrorRateDifferencePercent) > 0.5m ||
        Math.Abs(CulturalAccuracyDifferencePercent) > 2m;
}

#endregion

#region Additional Supporting Types

/// <summary>
/// Result type for global performance metrics operations
/// </summary>
public class GlobalPerformanceMetrics : Result<object>
{
    private GlobalPerformanceMetrics(bool isSuccess, IEnumerable<string> errors, object? value = null)
        : base(isSuccess, errors, value) { }

    public static new GlobalPerformanceMetrics Success()
        => new(true, Array.Empty<string>());

    public static new GlobalPerformanceMetrics Failure(string error)
        => new(false, new[] { error });
}

/// <summary>
/// Result type for timezone-aware performance reporting
/// </summary>
public class TimezoneAwarePerformanceReport : Result<object>
{
    private TimezoneAwarePerformanceReport(bool isSuccess, IEnumerable<string> errors, object? value = null)
        : base(isSuccess, errors, value) { }

    public static new TimezoneAwarePerformanceReport Success()
        => new(true, Array.Empty<string>());

    public static new TimezoneAwarePerformanceReport Failure(string error)
        => new(false, new[] { error });
}

/// <summary>
/// Result type for region failover operations
/// </summary>
public class RegionFailoverResult : Result<object>
{
    private RegionFailoverResult(bool isSuccess, IEnumerable<string> errors, object? value = null)
        : base(isSuccess, errors, value) { }

    public static new RegionFailoverResult Success()
        => new(true, Array.Empty<string>());

    public static new RegionFailoverResult Failure(string error)
        => new(false, new[] { error });
}

/// <summary>
/// Result type for regional compliance status
/// </summary>
public class RegionalComplianceStatus : Result<object>
{
    private RegionalComplianceStatus(bool isSuccess, IEnumerable<string> errors, object? value = null)
        : base(isSuccess, errors, value) { }

    public static new RegionalComplianceStatus Success()
        => new(true, Array.Empty<string>());

    public static new RegionalComplianceStatus Failure(string error)
        => new(false, new[] { error });
}

#endregion