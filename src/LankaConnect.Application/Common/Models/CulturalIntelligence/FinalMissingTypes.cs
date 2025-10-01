namespace LankaConnect.Application.Common.Models.CulturalIntelligence;

public class GeographicCoordinationResult
{
    public required bool CoordinationSuccessful { get; set; }
    public required Dictionary<string, string> RegionStatuses { get; set; }
    public required List<string> CoordinatedRegions { get; set; }
    public required DateTime CoordinationTimestamp { get; set; }
    public required string CoordinationOutcome { get; set; }
    public Dictionary<string, object> CoordinationMetrics { get; set; } = new();
    public string? CoordinationIssues { get; set; }
}

public class ConflictResolutionPerformanceMetrics
{
    public required string MetricsId { get; set; }
    public required Dictionary<string, double> PerformanceIndicators { get; set; }
    public required double OverallPerformanceScore { get; set; }
    public required DateTime MetricsTimestamp { get; set; }
    public required List<string> PerformanceCategories { get; set; }
    public Dictionary<string, object> BenchmarkComparisons { get; set; } = new();
    public string? PerformanceInsights { get; set; }
}

public class ConflictAnalyticsRequest
{
    public required string RequestId { get; set; }
    public required List<string> AnalyticsDimensions { get; set; }
    public required Dictionary<string, object> AnalyticsParameters { get; set; }
    public required string AnalyticsType { get; set; }
    public required DateTime RequestTimestamp { get; set; }
    public Dictionary<string, string> DataSources { get; set; } = new();
    public bool RealTimeAnalytics { get; set; }
}

public class ConflictResolutionAnalytics
{
    public required string AnalyticsId { get; set; }
    public required Dictionary<string, object> AnalyticsResults { get; set; }
    public required List<string> KeyInsights { get; set; }
    public required DateTime AnalyticsTimestamp { get; set; }
    public required double AnalyticsConfidence { get; set; }
    public Dictionary<string, string> TrendAnalysis { get; set; } = new();
    public List<string> ActionableInsights { get; set; } = new();
}

public class ConflictResolutionSystemHealth
{
    public required string HealthId { get; set; }
    public required Dictionary<string, string> SystemComponents { get; set; }
    public required string OverallHealthStatus { get; set; }
    public required DateTime HealthCheckTimestamp { get; set; }
    public required List<string> HealthIssues { get; set; }
    public Dictionary<string, object> PerformanceMetrics { get; set; } = new();
    public double SystemReliability { get; set; }
}

public class CulturalEventBenchmarkScenario
{
    public required string ScenarioId { get; set; }
    public required string ScenarioName { get; set; }
    public required Dictionary<string, object> BenchmarkParameters { get; set; }
    public required string EventType { get; set; }
    public required List<string> PerformanceTargets { get; set; }
    public Dictionary<string, double> ExpectedOutcomes { get; set; } = new();
    public TimeSpan ScenarioDuration { get; set; }
}

public class ConflictResolutionBenchmarkResult
{
    public required string BenchmarkId { get; set; }
    public required Dictionary<string, double> BenchmarkResults { get; set; }
    public required bool BenchmarkPassed { get; set; }
    public required DateTime BenchmarkTimestamp { get; set; }
    public required string BenchmarkCategory { get; set; }
    public Dictionary<string, object> PerformanceComparison { get; set; } = new();
    public List<string> ImprovementAreas { get; set; } = new();
}

public class SecurityPerformanceAnalyticsResult
{
    public required string AnalyticsId { get; set; }
    public required Dictionary<string, double> SecurityMetrics { get; set; }
    public required Dictionary<string, double> PerformanceMetrics { get; set; }
    public required DateTime AnalyticsTimestamp { get; set; }
    public required string AnalyticsOutcome { get; set; }
    public Dictionary<string, object> OptimizationRecommendations { get; set; } = new();
    public double SecurityPerformanceScore { get; set; }
}

public class RealTimeInteractionConfiguration
{
    public required string ConfigurationId { get; set; }
    public required Dictionary<string, object> InteractionSettings { get; set; }
    public required TimeSpan ResponseTimeTargets { get; set; }
    public required List<string> InteractionChannels { get; set; }
    public bool RealTimeEnabled { get; set; }
    public Dictionary<string, double> LatencyThresholds { get; set; } = new();
}

public class LatencyOptimizationTargets
{
    public required string TargetsId { get; set; }
    public required Dictionary<string, double> TargetLatencies { get; set; }
    public required List<string> OptimizationPriorities { get; set; }
    public required string OptimizationStrategy { get; set; }
    public Dictionary<string, object> OptimizationConstraints { get; set; } = new();
    public bool AdaptiveOptimization { get; set; }
}

public class SecurityLatencyOptimizationResult
{
    public required bool OptimizationSuccessful { get; set; }
    public required Dictionary<string, double> OptimizedLatencies { get; set; }
    public required double LatencyImprovement { get; set; }
    public required DateTime OptimizationTimestamp { get; set; }
    public Dictionary<string, object> SecurityImpact { get; set; } = new();
    public string? OptimizationLimitations { get; set; }
}

public class CulturalEventPattern
{
    public required string PatternId { get; set; }
    public required string PatternName { get; set; }
    public required Dictionary<string, object> PatternCharacteristics { get; set; }
    public required string EventCategory { get; set; }
    public required List<string> AffectedRegions { get; set; }
    public Dictionary<string, double> LoadPredictions { get; set; } = new();
    public bool RecurringPattern { get; set; }
}

public class ResourceScalingPolicy
{
    public required string PolicyId { get; set; }
    public required Dictionary<string, object> ScalingRules { get; set; }
    public required List<string> TriggerConditions { get; set; }
    public required string ScalingStrategy { get; set; }
    public required Dictionary<string, int> ResourceLimits { get; set; }
    public bool AutoScalingEnabled { get; set; }
    public TimeSpan CooldownPeriod { get; set; }
}

public class SecurityResourceScalingResult
{
    public required bool ScalingSuccessful { get; set; }
    public required Dictionary<string, int> ResourceAllocation { get; set; }
    public required string ScalingAction { get; set; }
    public required DateTime ScalingTimestamp { get; set; }
    public Dictionary<string, object> SecurityCompliance { get; set; } = new();
    public string? ScalingIssues { get; set; }
}

public class PerformanceAnalyticsConfiguration
{
    public required string ConfigurationId { get; set; }
    public required List<string> AnalyticsMetrics { get; set; }
    public required Dictionary<string, object> AnalyticsSettings { get; set; }
    public required TimeSpan AnalyticsInterval { get; set; }
    public bool RealTimeAnalytics { get; set; }
    public Dictionary<string, double> AnalyticsThresholds { get; set; } = new();
}

public class AnalyticsPeriod
{
    public required string PeriodId { get; set; }
    public required DateTime StartTime { get; set; }
    public required DateTime EndTime { get; set; }
    public required string PeriodType { get; set; }
    public Dictionary<string, object> PeriodParameters { get; set; } = new();
    public bool CustomPeriod { get; set; }
}

public class PrivacyPreferences
{
    public required string PreferencesId { get; set; }
    public required Dictionary<string, bool> PrivacySettings { get; set; }
    public required List<string> DataCategories { get; set; }
    public required string ConsentLevel { get; set; }
    public DateTime PreferencesUpdated { get; set; }
    public Dictionary<string, string> CustomPreferences { get; set; } = new();
}

public class CulturalGroup
{
    public required string GroupId { get; set; }
    public required string GroupName { get; set; }
    public required Dictionary<string, object> CulturalAttributes { get; set; }
    public required List<string> GroupMembers { get; set; }
    public required string Region { get; set; }
    public Dictionary<string, string> GroupPreferences { get; set; } = new();
    public bool ActiveGroup { get; set; }
}

public class SecurityPolicy
{
    public required string PolicyId { get; set; }
    public required string PolicyName { get; set; }
    public required Dictionary<string, object> PolicyRules { get; set; }
    public required string PolicyType { get; set; }
    public required DateTime PolicyEffectiveDate { get; set; }
    public Dictionary<string, bool> ComplianceRequirements { get; set; } = new();
    public bool PolicyActive { get; set; }
}

public class EncryptionConfiguration
{
    public required string ConfigurationId { get; set; }
    public required Dictionary<string, string> EncryptionMethods { get; set; }
    public required List<string> EncryptedDataTypes { get; set; }
    public required string KeyManagementStrategy { get; set; }
    public bool HardwareEncryption { get; set; }
    public Dictionary<string, int> EncryptionStrengths { get; set; } = new();
}

public class OptimizationRecommendation
{
    public required string RecommendationId { get; set; }
    public required string RecommendationType { get; set; }
    public required Dictionary<string, object> RecommendationDetails { get; set; }
    public required double ImpactScore { get; set; }
    public required string ImplementationComplexity { get; set; }
    public Dictionary<string, string> ImplementationSteps { get; set; } = new();
    public TimeSpan EstimatedImplementationTime { get; set; }
}
