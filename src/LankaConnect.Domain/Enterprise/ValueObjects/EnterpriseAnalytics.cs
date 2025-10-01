using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Enterprise.ValueObjects;

public class EnterpriseAnalytics : ValueObject
{
    public EnterpriseClientId ClientId { get; private set; }
    public DateTime GeneratedAt { get; private set; }
    public TimeSpan ReportingPeriod { get; private set; }
    public IReadOnlyDictionary<string, object> Metrics { get; private set; }
    public IReadOnlyDictionary<string, object> Dimensions { get; private set; }
    public CulturalIntelligenceInsights? CulturalInsights { get; private set; }
    public SLAPerformanceMetrics SLAMetrics { get; private set; }
    public string DataQualityScore { get; private set; }

    private EnterpriseAnalytics(
        EnterpriseClientId clientId,
        DateTime generatedAt,
        TimeSpan reportingPeriod,
        IReadOnlyDictionary<string, object> metrics,
        IReadOnlyDictionary<string, object> dimensions,
        SLAPerformanceMetrics slaMetrics,
        string dataQualityScore,
        CulturalIntelligenceInsights? culturalInsights = null)
    {
        ClientId = clientId;
        GeneratedAt = generatedAt;
        ReportingPeriod = reportingPeriod;
        Metrics = metrics;
        Dimensions = dimensions;
        CulturalInsights = culturalInsights;
        SLAMetrics = slaMetrics;
        DataQualityScore = dataQualityScore;
    }

    public static EnterpriseAnalytics Create(
        EnterpriseClientId clientId,
        DateTime generatedAt,
        TimeSpan reportingPeriod,
        IReadOnlyDictionary<string, object> metrics,
        IReadOnlyDictionary<string, object> dimensions,
        SLAPerformanceMetrics slaMetrics,
        string dataQualityScore,
        CulturalIntelligenceInsights? culturalInsights = null)
    {
        if (clientId == null) throw new ArgumentNullException(nameof(clientId));
        if (generatedAt > DateTime.UtcNow) throw new ArgumentException("Generated at cannot be in the future", nameof(generatedAt));
        if (reportingPeriod <= TimeSpan.Zero) throw new ArgumentException("Reporting period must be positive", nameof(reportingPeriod));
        if (metrics == null || !metrics.Any()) throw new ArgumentException("Metrics are required", nameof(metrics));
        if (dimensions == null || !dimensions.Any()) throw new ArgumentException("Dimensions are required", nameof(dimensions));
        if (slaMetrics == null) throw new ArgumentNullException(nameof(slaMetrics));
        if (string.IsNullOrWhiteSpace(dataQualityScore)) throw new ArgumentException("Data quality score is required", nameof(dataQualityScore));

        return new EnterpriseAnalytics(
            clientId,
            generatedAt,
            reportingPeriod,
            metrics,
            dimensions,
            slaMetrics,
            dataQualityScore,
            culturalInsights);
    }

    public T GetMetric<T>(string metricName, T defaultValue = default!)
    {
        return Metrics.TryGetValue(metricName, out var value) && value is T typedValue
            ? typedValue
            : defaultValue;
    }

    public T GetDimension<T>(string dimensionName, T defaultValue = default!)
    {
        return Dimensions.TryGetValue(dimensionName, out var value) && value is T typedValue
            ? typedValue
            : defaultValue;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return ClientId;
        yield return GeneratedAt;
        yield return ReportingPeriod;
        yield return SLAMetrics;
        yield return DataQualityScore;
        yield return CulturalInsights ?? new object();
        
        foreach (var metric in Metrics.OrderBy(x => x.Key))
        {
            yield return metric.Key;
            yield return metric.Value;
        }
        
        foreach (var dimension in Dimensions.OrderBy(x => x.Key))
        {
            yield return dimension.Key;
            yield return dimension.Value;
        }
    }
}