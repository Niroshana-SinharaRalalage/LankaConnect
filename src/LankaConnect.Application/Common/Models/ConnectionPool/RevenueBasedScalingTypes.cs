namespace LankaConnect.Application.Common.Models.ConnectionPool;

public class RevenueBasedScalingConfiguration
{
    public required string ConfigurationId { get; set; }
    public required Dictionary<string, double> RevenueThresholds { get; set; }
    public required string RevenueScalingStrategy { get; set; }
    public required Dictionary<string, double> RevenueTiers { get; set; }
    public required List<string> RevenueMetrics { get; set; }
    public bool DynamicPricingEnabled { get; set; }
    public Dictionary<string, object> RevenueOptimizationRules { get; set; } = new();
    public TimeSpan RevenueMonitoringInterval { get; set; }
}

public class RevenueBasedScalingResult
{
    public required bool ScalingTriggered { get; set; }
    public required Dictionary<string, double> RevenueImpact { get; set; }
    public required double ProjectedRevenueIncrease { get; set; }
    public required DateTime ScalingTimestamp { get; set; }
    public required string ScalingJustification { get; set; }
    public Dictionary<string, object> RevenueMetrics { get; set; } = new();
    public double ROIProjection { get; set; }
}

public class PremiumServiceScalingConfiguration
{
    public required string ConfigurationId { get; set; }
    public required List<string> PremiumTiers { get; set; }
    public required Dictionary<string, double> TierMultipliers { get; set; }
    public required string ScalingPreference { get; set; }
    public required Dictionary<string, object> ServiceLevelAgreements { get; set; }
    public bool PriorityBasedScaling { get; set; }
    public Dictionary<string, double> PremiumThresholds { get; set; } = new();
}

public class PremiumServiceScalingResult
{
    public required bool ScalingSuccessful { get; set; }
    public required Dictionary<string, int> TierAllocations { get; set; }
    public required List<string> AffectedTiers { get; set; }
    public required DateTime ScalingTimestamp { get; set; }
    public required Dictionary<string, double> ServiceLevelMaintenance { get; set; }
    public Dictionary<string, object> PremiumMetrics { get; set; } = new();
    public string? ScalingLimitations { get; set; }
}

public class CustomerWillingnessAnalysisRequest
{
    public required string AnalysisId { get; set; }
    public required List<string> TargetCustomerSegments { get; set; }
    public required Dictionary<string, double> PricePoints { get; set; }
    public required string AnalysisMethod { get; set; }
    public required DateTime AnalysisTimestamp { get; set; }
    public Dictionary<string, object> MarketContext { get; set; } = new();
    public List<string> CompetitiveFactors { get; set; } = new();
}

public class CustomerWillingnessToPayAnalysis
{
    public required string AnalysisId { get; set; }
    public required Dictionary<string, double> WillingnessScores { get; set; }
    public required Dictionary<string, double> OptimalPricePoints { get; set; }
    public required DateTime AnalysisTimestamp { get; set; }
    public required double OverallWillingnessIndex { get; set; }
    public Dictionary<string, object> SegmentInsights { get; set; } = new();
    public List<string> PricingRecommendations { get; set; } = new();
}

public class CulturalComplianceRequirements
{
    public required string RequirementId { get; set; }
    public required List<string> ComplianceFrameworks { get; set; }
    public required Dictionary<string, object> CulturalConstraints { get; set; }
    public required string ComplianceLevel { get; set; }
    public required List<string> AuditRequirements { get; set; }
    public DateTime ComplianceEffectiveDate { get; set; }
    public Dictionary<string, string> RegionalVariations { get; set; } = new();
    public bool ContinuousMonitoring { get; set; }
}

public class RegionalComplianceResult
{
    public required bool ComplianceAchieved { get; set; }
    public required Dictionary<string, bool> RegionCompliance { get; set; }
    public required List<string> NonCompliantRegions { get; set; }
    public required DateTime ComplianceTimestamp { get; set; }
    public required Dictionary<string, object> ComplianceMetrics { get; set; }
    public Dictionary<string, string> RemediationActions { get; set; } = new();
    public List<string> ComplianceRisks { get; set; } = new();
}

public class ValidationScope
{
    public required string ScopeId { get; set; }
    public required List<string> ValidationTargets { get; set; }
    public required Dictionary<string, object> ValidationCriteria { get; set; }
    public required string ValidationLevel { get; set; }
    public required List<string> ValidationRules { get; set; }
    public DateTime ValidationTimestamp { get; set; }
    public Dictionary<string, bool> ScopeConstraints { get; set; } = new();
    public bool StrictValidation { get; set; }
}
