using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.Models.Performance;

/// <summary>
/// Revenue metrics configuration for cultural events and diaspora community revenue tracking
/// Configures comprehensive revenue monitoring and analysis for the Cultural Intelligence platform
/// </summary>
public class RevenueMetricsConfiguration
{
    public required string ConfigurationId { get; set; }
    public required string ConfigurationName { get; set; }
    public required RevenueMetricsScope Scope { get; set; }
    public required List<RevenueMetricDefinition> MetricDefinitions { get; set; }
    public required Dictionary<string, RevenueThreshold> RevenueThresholds { get; set; }
    public required TimeSpan CollectionInterval { get; set; }
    public required RevenueMetricsAggregationStrategy AggregationStrategy { get; set; }
    public required List<string> MonitoredRevenueStreams { get; set; }
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? Description { get; set; }
    public List<CulturalRevenueMetricConfig> CulturalMetricConfigs { get; set; } = new();
    public Dictionary<string, DiasporaRevenueMetricPolicy> DiasporaMetricPolicies { get; set; } = new();
    public Dictionary<string, object> ConfigurationParameters { get; set; } = new();

    public RevenueMetricsConfiguration()
    {
        MetricDefinitions = new List<RevenueMetricDefinition>();
        RevenueThresholds = new Dictionary<string, RevenueThreshold>();
        MonitoredRevenueStreams = new List<string>();
        CulturalMetricConfigs = new List<CulturalRevenueMetricConfig>();
        DiasporaMetricPolicies = new Dictionary<string, DiasporaRevenueMetricPolicy>();
        ConfigurationParameters = new Dictionary<string, object>();
    }

    public bool IsRevenueStreamMonitored(string streamId)
    {
        return IsEnabled && MonitoredRevenueStreams.Contains(streamId);
    }

    public RevenueThreshold? GetThresholdForMetric(string metricId)
    {
        return RevenueThresholds.TryGetValue(metricId, out var threshold) ? threshold : null;
    }
}

/// <summary>
/// Revenue metrics scope
/// </summary>
public enum RevenueMetricsScope
{
    CulturalEvents = 1,
    DiasporaServices = 2,
    AdvertisementRevenue = 3,
    SubscriptionServices = 4,
    TransactionFees = 5,
    AllRevenueStreams = 6,
    RegionalRevenue = 7,
    SeasonalRevenue = 8
}

/// <summary>
/// Revenue metrics aggregation strategy
/// </summary>
public enum RevenueMetricsAggregationStrategy
{
    RealTime = 1,
    Hourly = 2,
    Daily = 3,
    Weekly = 4,
    Monthly = 5,
    EventBased = 6,
    Custom = 7
}

/// <summary>
/// Revenue metric definition
/// </summary>
public class RevenueMetricDefinition
{
    public required string MetricId { get; set; }
    public required string MetricName { get; set; }
    public required RevenueMetricType Type { get; set; }
    public required RevenueMetricCalculationMethod CalculationMethod { get; set; }
    public required string Unit { get; set; }
    public required List<string> DataSources { get; set; }
    public required RevenueMetricFrequency CollectionFrequency { get; set; }
    public decimal? MinExpectedValue { get; set; }
    public decimal? MaxExpectedValue { get; set; }
    public string? Description { get; set; }
    public List<RevenueMetricDimension> Dimensions { get; set; } = new();
    public Dictionary<string, object> CalculationParameters { get; set; } = new();
}

/// <summary>
/// Revenue metric types
/// </summary>
public enum RevenueMetricType
{
    TotalRevenue = 1,
    RevenueGrowth = 2,
    RevenuePerUser = 3,
    RevenuePerEvent = 4,
    ConversionRate = 5,
    ChurnRate = 6,
    CustomerLifetimeValue = 7,
    AverageOrderValue = 8,
    RevenueBySource = 9,
    RevenueByRegion = 10
}

/// <summary>
/// Revenue metric calculation methods
/// </summary>
public enum RevenueMetricCalculationMethod
{
    Sum = 1,
    Average = 2,
    Count = 3,
    Percentage = 4,
    Rate = 5,
    Ratio = 6,
    Growth = 7,
    Variance = 8
}

/// <summary>
/// Revenue metric frequency
/// </summary>
public enum RevenueMetricFrequency
{
    RealTime = 1,
    Minutely = 2,
    Hourly = 3,
    Daily = 4,
    Weekly = 5,
    Monthly = 6,
    OnDemand = 7
}

/// <summary>
/// Revenue threshold
/// </summary>
public class RevenueThreshold
{
    public required string ThresholdId { get; set; }
    public required RevenueThresholdType Type { get; set; }
    public required decimal ThresholdValue { get; set; }
    public required RevenueThresholdSeverity Severity { get; set; }
    public required TimeSpan EvaluationWindow { get; set; }
    public string? Description { get; set; }
    public List<string> AlertActions { get; set; } = new();
    public Dictionary<string, object> ThresholdMetadata { get; set; } = new();
}

/// <summary>
/// Revenue threshold types
/// </summary>
public enum RevenueThresholdType
{
    MinimumRevenue = 1,
    MaximumRevenue = 2,
    RevenueGrowthRate = 3,
    RevenueDeclineRate = 4,
    ConversionThreshold = 5,
    ChurnThreshold = 6
}

/// <summary>
/// Revenue threshold severity
/// </summary>
public enum RevenueThresholdSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4,
    Emergency = 5
}

/// <summary>
/// Revenue metric dimension
/// </summary>
public class RevenueMetricDimension
{
    public required string DimensionId { get; set; }
    public required string DimensionName { get; set; }
    public required RevenueMetricDimensionType Type { get; set; }
    public required List<string> PossibleValues { get; set; }
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
    public Dictionary<string, object> DimensionMetadata { get; set; } = new();
}

/// <summary>
/// Revenue metric dimension types
/// </summary>
public enum RevenueMetricDimensionType
{
    Geographic = 1,
    Temporal = 2,
    Demographic = 3,
    Cultural = 4,
    Product = 5,
    Channel = 6,
    Customer = 7,
    Event = 8
}

/// <summary>
/// Cultural revenue metric configuration
/// </summary>
public class CulturalRevenueMetricConfig
{
    public required CulturalEventType EventType { get; set; }
    public required List<string> CulturalMetrics { get; set; }
    public required Dictionary<string, decimal> CommunityWeights { get; set; }
    public required List<string> SupportedLanguages { get; set; }
    public string? ReligiousConsiderations { get; set; }
    public bool RequiresSpecialCalculation { get; set; }
    public Dictionary<string, object> CulturalFactors { get; set; } = new();
}

/// <summary>
/// Diaspora revenue metric policy
/// </summary>
public class DiasporaRevenueMetricPolicy
{
    public required string RegionId { get; set; }
    public required string RegionName { get; set; }
    public required List<string> RegionalMetrics { get; set; }
    public required Dictionary<string, decimal> CurrencyConversionRates { get; set; }
    public required List<string> LocalPaymentMethods { get; set; }
    public string? TimeZone { get; set; }
    public List<string> RegulatoryRequirements { get; set; } = new();
    public Dictionary<string, object> RegionalFactors { get; set; } = new();
}