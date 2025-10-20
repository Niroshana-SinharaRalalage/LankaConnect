using System;
using System.Collections.Generic;

namespace LankaConnect.Infrastructure.Monitoring;

/// <summary>
/// Monitoring Models - Infrastructure Layer
/// Moved from Stage5MissingTypes.cs to correct architectural layer
/// </summary>

#region Historical Data and Metrics

/// <summary>
/// Historical metrics data for analytics
/// </summary>
public class HistoricalMetricsData
{
    public string DatasetId { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public double AverageResponseTime { get; set; }
    public double AverageThroughput { get; set; }
    public double AverageErrorRate { get; set; }
    public double AverageResourceUtilization { get; set; }
}

/// <summary>
/// Notification preferences for alerting
/// </summary>
public class NotificationPreferences
{
    public string PreferenceId { get; set; } = string.Empty;
    public List<string> NotificationChannels { get; set; } = new();
    public Dictionary<string, bool> ChannelEnabled { get; set; } = new();
    public string PreferredLanguage { get; set; } = string.Empty;
}

#endregion

#region Competitive Analysis

/// <summary>
/// Competitive benchmark data for market analysis
/// </summary>
public class CompetitiveBenchmarkData
{
    public string BenchmarkId { get; set; } = string.Empty;
    public Dictionary<string, double> CompetitorMetrics { get; set; } = new();
    public DateTime BenchmarkDate { get; set; }
    public string MarketSegment { get; set; } = string.Empty;
}

#endregion

#region Multi-Region Optimization

/// <summary>
/// Inter-region optimization result
/// </summary>
public class InterRegionOptimizationResult
{
    public string ResultId { get; set; } = string.Empty;
    public bool IsOptimizationSuccessful { get; set; }
    public double PerformanceImprovement { get; set; }
    public List<string> OptimizedRegions { get; set; } = new();
}

#endregion
