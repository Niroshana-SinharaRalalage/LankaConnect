namespace LankaConnect.Application.Common.Models.CulturalIntelligence;

public class GDPRValidationScope
{
    public required string ScopeId { get; set; }
    public required List<string> DataCategories { get; set; }
    public required Dictionary<string, object> ValidationCriteria { get; set; }
    public required string ValidationLevel { get; set; }
    public required List<string> ProcessingActivities { get; set; }
    public DateTime ScopeEffectiveDate { get; set; }
    public Dictionary<string, bool> ConsentRequirements { get; set; } = new();
}

public class GDPRComplianceResult
{
    public required bool ComplianceAchieved { get; set; }
    public required Dictionary<string, bool> ComplianceChecks { get; set; }
    public required List<string> NonComplianceItems { get; set; }
    public required DateTime ComplianceTimestamp { get; set; }
    public required string ComplianceLevel { get; set; }
    public Dictionary<string, string> RemediationActions { get; set; } = new();
    public string? ComplianceOfficerApproval { get; set; }
}

public class HIPAAValidationCriteria
{
    public required string CriteriaId { get; set; }
    public required List<string> ProtectedHealthInfoCategories { get; set; }
    public required Dictionary<string, object> SafeguardRequirements { get; set; }
    public required string ValidationStandard { get; set; }
    public required List<string> AuditRequirements { get; set; }
    public DateTime CriteriaEffectiveDate { get; set; }
    public Dictionary<string, bool> PhysicalSafeguards { get; set; } = new();
}

public class HIPAAComplianceResult
{
    public required bool ComplianceAchieved { get; set; }
    public required Dictionary<string, bool> SafeguardCompliance { get; set; }
    public required List<string> ComplianceGaps { get; set; }
    public required DateTime ComplianceTimestamp { get; set; }
    public required string ComplianceOfficer { get; set; }
    public Dictionary<string, string> CorrectiveActions { get; set; } = new();
    public bool BusinessAssociateCompliance { get; set; }
}

public class PCIDSSValidationScope
{
    public required string ScopeId { get; set; }
    public required List<string> CardDataEnvironments { get; set; }
    public required Dictionary<string, object> ValidationRequirements { get; set; }
    public required string PCILevel { get; set; }
    public required List<string> SecurityControls { get; set; }
    public DateTime ScopeAssessmentDate { get; set; }
    public Dictionary<string, bool> NetworkSegmentation { get; set; } = new();
}

public class PCIDSSComplianceResult
{
    public required bool ComplianceAchieved { get; set; }
    public required Dictionary<string, bool> ControlCompliance { get; set; }
    public required List<string> FailedControls { get; set; }
    public required DateTime ComplianceTimestamp { get; set; }
    public required string QSAValidation { get; set; }
    public Dictionary<string, string> RemediationPlan { get; set; } = new();
    public DateTime ComplianceExpiry { get; set; }
}

public class SOC2ValidationCriteria
{
    public required string CriteriaId { get; set; }
    public required List<string> TrustServiceCriteria { get; set; }
    public required Dictionary<string, object> ControlObjectives { get; set; }
    public required string AuditType { get; set; }
    public required List<string> EvidenceRequirements { get; set; }
    public DateTime CriteriaEffectiveDate { get; set; }
    public Dictionary<string, bool> SecurityControls { get; set; } = new();
}

public class DataProcessingActivity
{
    public required string ActivityId { get; set; }
    public required string ActivityName { get; set; }
    public required Dictionary<string, object> ProcessingDetails { get; set; }
    public required List<string> DataCategories { get; set; }
    public required string LegalBasis { get; set; }
    public required List<string> DataSubjects { get; set; }
    public Dictionary<string, string> RetentionPeriods { get; set; } = new();
    public bool InternationalTransfers { get; set; }
}

public class PaymentDataHandling
{
    public required string HandlingId { get; set; }
    public required Dictionary<string, object> PaymentDataTypes { get; set; }
    public required string EncryptionMethod { get; set; }
    public required List<string> ProcessingLocations { get; set; }
    public required string TokenizationStrategy { get; set; }
    public Dictionary<string, bool> ComplianceControls { get; set; } = new();
    public bool VaultStorage { get; set; }
}

public class HealthDataCategory
{
    public required string CategoryId { get; set; }
    public required string CategoryName { get; set; }
    public required Dictionary<string, object> DataElements { get; set; }
    public required string SensitivityLevel { get; set; }
    public required List<string> ProtectionRequirements { get; set; }
    public Dictionary<string, string> AccessControls { get; set; } = new();
    public bool DeIdentificationRequired { get; set; }
}

public class AuditPeriod
{
    public required string PeriodId { get; set; }
    public required DateTime StartDate { get; set; }
    public required DateTime EndDate { get; set; }
    public required string AuditType { get; set; }
    public required List<string> AuditScope { get; set; }
    public Dictionary<string, object> AuditParameters { get; set; } = new();
    public string? AuditorAssigned { get; set; }
}

public class ConflictPatternAnalysisRequest
{
    public required string RequestId { get; set; }
    public required List<string> ConflictSources { get; set; }
    public required Dictionary<string, object> AnalysisParameters { get; set; }
    public required string PatternType { get; set; }
    public required TimeSpan AnalysisWindow { get; set; }
    public DateTime RequestTimestamp { get; set; }
    public Dictionary<string, double> WeightingFactors { get; set; } = new();
}

public class ConflictPatternAnalysisResult
{
    public required string AnalysisId { get; set; }
    public required Dictionary<string, object> IdentifiedPatterns { get; set; }
    public required List<string> PatternCategories { get; set; }
    public required DateTime AnalysisTimestamp { get; set; }
    public required double PatternConfidence { get; set; }
    public Dictionary<string, string> PatternInsights { get; set; } = new();
    public List<string> PreventionRecommendations { get; set; } = new();
}

public class DiasporaActivityMetrics
{
    public required string MetricsId { get; set; }
    public required Dictionary<string, double> ActivityLevels { get; set; }
    public required List<string> ActiveCommunities { get; set; }
    public required DateTime MetricsTimestamp { get; set; }
    public required Dictionary<string, object> EngagementPatterns { get; set; }
    public Dictionary<string, double> GrowthRates { get; set; } = new();
    public string? TrendAnalysis { get; set; }
}

public class GeographicCoordinationRequest
{
    public required string RequestId { get; set; }
    public required List<string> CoordinatingRegions { get; set; }
    public required Dictionary<string, object> CoordinationParameters { get; set; }
    public required string CoordinationType { get; set; }
    public required DateTime RequestTimestamp { get; set; }
    public Dictionary<string, double> RegionPriorities { get; set; } = new();
    public bool EmergencyCoordination { get; set; }
}

public class ROICalculationParameters
{
    public required string CalculationId { get; set; }
    public required Dictionary<string, double> InvestmentCosts { get; set; }
    public required Dictionary<string, double> ExpectedReturns { get; set; }
    public required TimeSpan CalculationPeriod { get; set; }
    public required string DiscountRate { get; set; }
    public Dictionary<string, object> RiskFactors { get; set; } = new();
    public DateTime CalculationTimestamp { get; set; }
}

public class ROICalculationResult
{
    public required string CalculationId { get; set; }
    public required double ROIPercentage { get; set; }
    public required Dictionary<string, double> PeriodReturns { get; set; }
    public required DateTime CalculationTimestamp { get; set; }
    public required TimeSpan PaybackPeriod { get; set; }
    public Dictionary<string, object> SensitivityAnalysis { get; set; } = new();
    public double NetPresentValue { get; set; }
}

public class DynamicPricingConfiguration
{
    public required string ConfigurationId { get; set; }
    public required Dictionary<string, double> PricingRules { get; set; }
    public required List<string> PricingFactors { get; set; }
    public required string PricingStrategy { get; set; }
    public required TimeSpan PriceUpdateInterval { get; set; }
    public bool MarketBasedPricing { get; set; }
    public Dictionary<string, object> PricingConstraints { get; set; } = new();
}

public class DynamicPricingResult
{
    public required bool PricingUpdateSuccessful { get; set; }
    public required Dictionary<string, double> UpdatedPrices { get; set; }
    public required DateTime PricingTimestamp { get; set; }
    public required string PricingJustification { get; set; }
    public Dictionary<string, double> RevenueImpact { get; set; } = new();
    public string? PricingWarnings { get; set; }
}

public class PricingOptimizationConfiguration
{
    public required string ConfigurationId { get; set; }
    public required Dictionary<string, object> OptimizationCriteria { get; set; }
    public required List<string> OptimizationTargets { get; set; }
    public required string OptimizationAlgorithm { get; set; }
    public required Dictionary<string, double> PriceConstraints { get; set; }
    public bool CompetitorPriceTracking { get; set; }
    public Dictionary<string, object> MarketConditions { get; set; } = new();
}

public class PricingOptimizationResult
{
    public required bool OptimizationSuccessful { get; set; }
    public required Dictionary<string, double> OptimizedPrices { get; set; }
    public required double ExpectedRevenueIncrease { get; set; }
    public required DateTime OptimizationTimestamp { get; set; }
    public Dictionary<string, object> OptimizationMetrics { get; set; } = new();
    public List<string> OptimizationInsights { get; set; } = new();
}
