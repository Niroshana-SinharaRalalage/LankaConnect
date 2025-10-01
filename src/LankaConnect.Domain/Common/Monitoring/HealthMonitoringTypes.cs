using System;
using System.Collections.Generic;
using System.Linq;

namespace LankaConnect.Domain.Common.Monitoring;

/// <summary>
/// Health monitoring and trend analysis types for LankaConnect's
/// cultural intelligence platform with enterprise-grade capabilities.
/// </summary>

#region Health Monitoring

/// <summary>
/// Health issue detection and tracking.
/// </summary>
public class HealthIssue
{
    /// <summary>
    /// Gets the issue identifier.
    /// </summary>
    public string IssueId { get; private set; }

    /// <summary>
    /// Gets the affected component.
    /// </summary>
    public string Component { get; private set; }

    /// <summary>
    /// Gets the issue severity.
    /// </summary>
    public HealthIssueSeverity Severity { get; private set; }

    /// <summary>
    /// Gets the issue description.
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// Gets the affected metrics.
    /// </summary>
    public List<string> AffectedMetrics { get; private set; }

    /// <summary>
    /// Gets when the issue was detected.
    /// </summary>
    public DateTime DetectedAt { get; private set; }

    /// <summary>
    /// Gets whether the issue is critical.
    /// </summary>
    public bool IsCritical => Severity == HealthIssueSeverity.Critical;

    /// <summary>
    /// Gets whether immediate action is required.
    /// </summary>
    public bool RequiresImmediateAction => Severity >= HealthIssueSeverity.Major;

    /// <summary>
    /// Gets whether this is related to cultural intelligence.
    /// </summary>
    public bool IsCulturalIntelligenceRelated => Component.Contains("Cultural", StringComparison.OrdinalIgnoreCase) ||
                                                Description.Contains("Cultural", StringComparison.OrdinalIgnoreCase);

    private HealthIssue(string issueId, string component, HealthIssueSeverity severity, 
        string description, List<string> affectedMetrics)
    {
        IssueId = issueId;
        Component = component;
        Severity = severity;
        Description = description;
        AffectedMetrics = affectedMetrics;
        DetectedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a health issue.
    /// </summary>
    public static HealthIssue Create(string issueId, string component, HealthIssueSeverity severity, 
        string description, List<string> affectedMetrics)
    {
        return new HealthIssue(issueId, component, severity, description, affectedMetrics);
    }
}

/// <summary>
/// Health improvement recommendations.
/// </summary>
public class HealthRecommendation
{
    /// <summary>
    /// Gets the recommendation identifier.
    /// </summary>
    public string RecommendationId { get; private set; }

    /// <summary>
    /// Gets the related issue identifier.
    /// </summary>
    public string RelatedIssueId { get; private set; }

    /// <summary>
    /// Gets the recommendation title.
    /// </summary>
    public string Title { get; private set; }

    /// <summary>
    /// Gets the recommendation description.
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// Gets the recommendation priority.
    /// </summary>
    public RecommendationPriority Priority { get; private set; }

    /// <summary>
    /// Gets the estimated impact.
    /// </summary>
    public string EstimatedImpact { get; private set; }

    /// <summary>
    /// Gets the recommended action steps.
    /// </summary>
    public List<string> ActionSteps { get; private set; }

    /// <summary>
    /// Gets whether this is high priority.
    /// </summary>
    public bool IsHighPriority => Priority >= RecommendationPriority.High;

    /// <summary>
    /// Gets whether the recommendation is actionable.
    /// </summary>
    public bool IsActionable => ActionSteps.Any();

    private HealthRecommendation(string recommendationId, string relatedIssueId, string title, 
        string description, RecommendationPriority priority, string estimatedImpact, List<string> actionSteps)
    {
        RecommendationId = recommendationId;
        RelatedIssueId = relatedIssueId;
        Title = title;
        Description = description;
        Priority = priority;
        EstimatedImpact = estimatedImpact;
        ActionSteps = actionSteps;
    }

    /// <summary>
    /// Creates a health recommendation.
    /// </summary>
    public static HealthRecommendation Create(string recommendationId, string relatedIssueId, 
        string title, string description, RecommendationPriority priority, 
        string estimatedImpact, List<string> actionSteps)
    {
        return new HealthRecommendation(recommendationId, relatedIssueId, title, 
            description, priority, estimatedImpact, actionSteps);
    }
}

/// <summary>
/// Health trend analysis and forecasting.
/// </summary>
public class HealthTrend
{
    /// <summary>
    /// Gets the trend identifier.
    /// </summary>
    public string TrendId { get; private set; }

    /// <summary>
    /// Gets the metric name being analyzed.
    /// </summary>
    public string MetricName { get; private set; }

    /// <summary>
    /// Gets the trend data points.
    /// </summary>
    public List<HealthTrendDataPoint> DataPoints { get; private set; }

    /// <summary>
    /// Gets the analysis window.
    /// </summary>
    public TimeSpan AnalysisWindow { get; private set; }

    /// <summary>
    /// Gets the trend direction.
    /// </summary>
    public TrendDirection TrendDirection { get; private set; }

    /// <summary>
    /// Gets the confidence score (0.0 to 1.0).
    /// </summary>
    public double ConfidenceScore { get; private set; }

    /// <summary>
    /// Gets the predicted next value.
    /// </summary>
    public double PredictedNextValue { get; private set; }

    /// <summary>
    /// Gets whether this is a significant trend.
    /// </summary>
    public bool IsSignificantTrend => ConfidenceScore > 0.7;

    /// <summary>
    /// Gets when the analysis was performed.
    /// </summary>
    public DateTime AnalyzedAt { get; private set; }

    private HealthTrend(string trendId, string metricName, List<HealthTrendDataPoint> dataPoints, 
        TimeSpan analysisWindow)
    {
        TrendId = trendId;
        MetricName = metricName;
        DataPoints = dataPoints;
        AnalysisWindow = analysisWindow;
        AnalyzedAt = DateTime.UtcNow;
        
        // Calculate trend direction and confidence
        CalculateTrend();
    }

    /// <summary>
    /// Analyzes health trend from data points.
    /// </summary>
    public static HealthTrend Analyze(string trendId, string metricName, 
        List<HealthTrendDataPoint> dataPoints, TimeSpan analysisWindow)
    {
        return new HealthTrend(trendId, metricName, dataPoints, analysisWindow);
    }

    private void CalculateTrend()
    {
        if (DataPoints.Count < 2)
        {
            TrendDirection = TrendDirection.Stable;
            ConfidenceScore = 0.0;
            PredictedNextValue = DataPoints.FirstOrDefault()?.Value ?? 0;
            return;
        }

        var values = DataPoints.Select(dp => dp.Value).ToList();
        var firstValue = values.First();
        var lastValue = values.Last();

        // Simple trend calculation
        var change = (lastValue - firstValue) / firstValue;
        
        if (change > 0.05) // 5% improvement threshold
        {
            TrendDirection = TrendDirection.Improving;
            ConfidenceScore = Math.Min(0.95, 0.5 + Math.Abs(change));
        }
        else if (change < -0.05) // 5% decline threshold
        {
            TrendDirection = TrendDirection.Declining;
            ConfidenceScore = Math.Min(0.95, 0.5 + Math.Abs(change));
        }
        else
        {
            TrendDirection = TrendDirection.Stable;
            ConfidenceScore = 0.6;
        }

        // Simple linear prediction
        var avgGrowth = values.Skip(1).Select((value, index) => value - values[index]).Average();
        PredictedNextValue = lastValue + avgGrowth;
    }
}

/// <summary>
/// Health trend data point.
/// </summary>
public record HealthTrendDataPoint(
    string Timestamp,
    double Value,
    Dictionary<string, object>? Metadata = null)
{
    public DateTime ParsedTimestamp => DateTime.TryParse(Timestamp, out var dt) ? dt : DateTime.MinValue;
}

#endregion

#region Performance Analytics

/// <summary>
/// Analytics dashboard widget configuration.
/// </summary>
public class AnalyticsWidget
{
    /// <summary>
    /// Gets the widget identifier.
    /// </summary>
    public string WidgetId { get; private set; }

    /// <summary>
    /// Gets the widget type.
    /// </summary>
    public AnalyticsWidgetType WidgetType { get; private set; }

    /// <summary>
    /// Gets the widget title.
    /// </summary>
    public string Title { get; private set; }

    /// <summary>
    /// Gets the data source.
    /// </summary>
    public string DataSource { get; private set; }

    /// <summary>
    /// Gets the refresh interval.
    /// </summary>
    public TimeSpan RefreshInterval { get; private set; }

    /// <summary>
    /// Gets the cultural filters.
    /// </summary>
    public Dictionary<string, object> CulturalFilters { get; private set; }

    /// <summary>
    /// Gets whether this is a cultural widget.
    /// </summary>
    public bool IsCulturalWidget => WidgetType == AnalyticsWidgetType.CulturalEngagementChart ||
                                   WidgetType == AnalyticsWidgetType.DiasporaDistributionMap ||
                                   WidgetType == AnalyticsWidgetType.LanguageUsageChart ||
                                   CulturalFilters.Any();

    /// <summary>
    /// Gets whether this is a real-time widget.
    /// </summary>
    public bool IsRealTime => RefreshInterval <= TimeSpan.FromMinutes(10);

    private AnalyticsWidget(string widgetId, AnalyticsWidgetType widgetType, string title, 
        string dataSource, TimeSpan refreshInterval, Dictionary<string, object> culturalFilters)
    {
        WidgetId = widgetId;
        WidgetType = widgetType;
        Title = title;
        DataSource = dataSource;
        RefreshInterval = refreshInterval;
        CulturalFilters = culturalFilters;
    }

    /// <summary>
    /// Creates an analytics widget.
    /// </summary>
    public static AnalyticsWidget Create(string widgetId, AnalyticsWidgetType widgetType, 
        string title, string dataSource, TimeSpan refreshInterval, 
        Dictionary<string, object>? culturalFilters = null)
    {
        return new AnalyticsWidget(widgetId, widgetType, title, dataSource, 
            refreshInterval, culturalFilters ?? new Dictionary<string, object>());
    }
}

/// <summary>
/// Performance insights with actionable intelligence.
/// </summary>
public class PerformanceInsight
{
    /// <summary>
    /// Gets the insight identifier.
    /// </summary>
    public string InsightId { get; private set; }

    /// <summary>
    /// Gets the insight category.
    /// </summary>
    public InsightCategory Category { get; private set; }

    /// <summary>
    /// Gets the insight title.
    /// </summary>
    public string Title { get; private set; }

    /// <summary>
    /// Gets the insight description.
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// Gets the confidence level (0.0 to 1.0).
    /// </summary>
    public double Confidence { get; private set; }

    /// <summary>
    /// Gets the recommended actions.
    /// </summary>
    public List<string> Recommendations { get; private set; }

    /// <summary>
    /// Gets when the insight was generated.
    /// </summary>
    public DateTime GeneratedAt { get; private set; }

    /// <summary>
    /// Gets whether this is a high confidence insight.
    /// </summary>
    public bool IsHighConfidence => Confidence >= 0.8;

    /// <summary>
    /// Gets whether this is a cultural insight.
    /// </summary>
    public bool IsCulturalInsight => Category == InsightCategory.CulturalEngagement ||
                                    Title.Contains("Cultural", StringComparison.OrdinalIgnoreCase) ||
                                    Description.Contains("Cultural", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets whether the insight is actionable.
    /// </summary>
    public bool IsActionable => Recommendations.Any();

    private PerformanceInsight(string insightId, InsightCategory category, string title, 
        string description, double confidence, List<string> recommendations)
    {
        InsightId = insightId;
        Category = category;
        Title = title;
        Description = description;
        Confidence = confidence;
        Recommendations = recommendations;
        GeneratedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a performance insight.
    /// </summary>
    public static PerformanceInsight Create(string insightId, InsightCategory category, 
        string title, string description, double confidence, List<string> recommendations)
    {
        return new PerformanceInsight(insightId, category, title, description, confidence, recommendations);
    }
}

/// <summary>
/// Executive performance summary.
/// </summary>
public class PerformanceSummary
{
    /// <summary>
    /// Gets the summary identifier.
    /// </summary>
    public string SummaryId { get; private set; }

    /// <summary>
    /// Gets the reporting period.
    /// </summary>
    public string Period { get; private set; }

    /// <summary>
    /// Gets the key performance metrics.
    /// </summary>
    public Dictionary<string, double> KeyMetrics { get; private set; }

    /// <summary>
    /// Gets the cultural highlights.
    /// </summary>
    public List<string> CulturalHighlights { get; private set; }

    /// <summary>
    /// Gets the overall health score.
    /// </summary>
    public double OverallHealthScore { get; private set; }

    /// <summary>
    /// Gets when the summary was generated.
    /// </summary>
    public DateTime GeneratedAt { get; private set; }

    /// <summary>
    /// Gets whether there are cultural highlights.
    /// </summary>
    public bool HasCulturalHighlights => CulturalHighlights.Any();

    /// <summary>
    /// Gets whether performance is positive.
    /// </summary>
    public bool IsPositivePerformance => OverallHealthScore >= 80;

    private PerformanceSummary(string summaryId, string period, Dictionary<string, double> keyMetrics, 
        List<string> culturalHighlights)
    {
        SummaryId = summaryId;
        Period = period;
        KeyMetrics = keyMetrics;
        CulturalHighlights = culturalHighlights;
        GeneratedAt = DateTime.UtcNow;
        
        // Calculate overall health score from key metrics
        OverallHealthScore = KeyMetrics.Values.Any() ? KeyMetrics.Values.Average() : 0;
    }

    /// <summary>
    /// Creates a performance summary.
    /// </summary>
    public static PerformanceSummary Create(string summaryId, string period, 
        Dictionary<string, double> keyMetrics, List<string> culturalHighlights)
    {
        return new PerformanceSummary(summaryId, period, keyMetrics, culturalHighlights);
    }
}

/// <summary>
/// Actionable performance recommendations.
/// </summary>
public class ActionableRecommendation
{
    /// <summary>
    /// Gets the recommendation identifier.
    /// </summary>
    public string RecommendationId { get; private set; }

    /// <summary>
    /// Gets the recommendation title.
    /// </summary>
    public string Title { get; private set; }

    /// <summary>
    /// Gets the recommendation category.
    /// </summary>
    public RecommendationCategory Category { get; private set; }

    /// <summary>
    /// Gets the action priority.
    /// </summary>
    public ActionPriority Priority { get; private set; }

    /// <summary>
    /// Gets the estimated impact.
    /// </summary>
    public string EstimatedImpact { get; private set; }

    /// <summary>
    /// Gets the implementation guidance.
    /// </summary>
    public ImplementationGuidance Implementation { get; private set; }

    /// <summary>
    /// Gets whether this is high priority.
    /// </summary>
    public bool IsHighPriority => Priority >= ActionPriority.High;

    /// <summary>
    /// Gets whether the recommendation is implementable.
    /// </summary>
    public bool IsImplementable => Implementation != null && Implementation.Resources.Any();

    /// <summary>
    /// Gets whether this has cultural context.
    /// </summary>
    public bool HasCulturalContext => Category == RecommendationCategory.CulturalOptimization ||
                                     Title.Contains("Cultural", StringComparison.OrdinalIgnoreCase);

    private ActionableRecommendation(string recommendationId, string title, RecommendationCategory category, 
        ActionPriority priority, string estimatedImpact, ImplementationGuidance implementation)
    {
        RecommendationId = recommendationId;
        Title = title;
        Category = category;
        Priority = priority;
        EstimatedImpact = estimatedImpact;
        Implementation = implementation;
    }

    /// <summary>
    /// Creates an actionable recommendation.
    /// </summary>
    public static ActionableRecommendation Create(string recommendationId, string title, 
        RecommendationCategory category, ActionPriority priority, string estimatedImpact, 
        ImplementationGuidance implementation)
    {
        return new ActionableRecommendation(recommendationId, title, category, priority, estimatedImpact, implementation);
    }
}

/// <summary>
/// Implementation guidance for recommendations.
/// </summary>
public record ImplementationGuidance(
    string Effort,
    List<string> Resources,
    List<string> Risks);

#endregion