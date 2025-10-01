using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Communications.Enums;

namespace LankaConnect.Domain.Communications.ValueObjects;

/// <summary>
/// Supporting value objects for Multi-Cultural Calendar Engine Phase 8 expansion
/// Contains all supporting types for cultural intelligence and enterprise analytics
/// </summary>

public record CulturalSensitivityIssue(
    CulturalCommunity CommunityAffected,
    string IssueDescription,
    CulturalSensitivityLevel SensitivityLevel,
    string RecommendedAction);

public record CulturalResolution(
    string ResolutionStrategy,
    IEnumerable<CulturalCommunity> BenefitingCommunities,
    decimal EffectivenessScore,
    string ImplementationSteps);

public record CrossCulturalEngagementStrategy(
    string StrategyName,
    IEnumerable<CulturalCommunity> ApplicableCommunities,
    decimal SuccessProbability,
    string EngagementApproach,
    IEnumerable<string> KeySuccessFactors);

public record CulturalBridgeBuilding(
    string BridgeType,
    CulturalCommunity SourceCommunity,
    CulturalCommunity TargetCommunity,
    IEnumerable<string> BridgingActivities,
    decimal BridgingPotentialScore);

public record CulturalRisk(
    string RiskType,
    CulturalCommunity AffectedCommunity,
    decimal RiskLevel,
    string RiskDescription,
    IEnumerable<string> MitigationStrategies);

public record CulturalBenefit(
    string BenefitType,
    IEnumerable<CulturalCommunity> BenefitingCommunities,
    decimal ImpactLevel,
    string BenefitDescription,
    string MeasurementCriteria);

public record CommunityCluster(
    CulturalCommunity Community,
    IEnumerable<string> ConcentrationAreas,
    int EstimatedPopulation,
    decimal ConcentrationIndex,
    IEnumerable<string> MajorOrganizations,
    GeographicDistribution Distribution);

public record CrossCulturalOpportunity(
    string OpportunityType,
    IEnumerable<CulturalCommunity> InvolvedCommunities,
    decimal ImpactPotential,
    string OpportunityDescription,
    IEnumerable<string> RequiredActions);

public record MarketExpansionPotential(
    decimal RevenuePotential,
    int TargetPopulation,
    decimal MarketPenetrationRate,
    string MarketSegment,
    IEnumerable<string> GrowthDrivers);

public record CulturalDiversityMetrics(
    decimal CulturalDiversityScore,
    int NumberOfCommunities,
    decimal IntegrationIndex,
    decimal CulturalCompetencyScore,
    IEnumerable<CommunityEngagementMetric> CommunityMetrics);

public record CulturalEngagementOpportunity(
    CulturalEventType EventType,
    string OpportunityDescription,
    IEnumerable<CulturalCommunity> TargetCommunities,
    decimal EngagementPotential,
    DateTime OptimalTiming,
    IEnumerable<string> RequiredResources);

public record CulturalROIProjection(
    decimal EstimatedAnnualRevenue,
    decimal EmployeeEngagementImprovement,
    decimal CulturalComplianceScore,
    decimal BrandReputationImpact,
    IEnumerable<ROIMetric> DetailedMetrics);

public record CulturalEnhancement(
    string EnhancementType,
    IEnumerable<CulturalCommunity> BenefitingCommunities,
    decimal ImpactScore,
    string EnhancementDescription,
    DateTime RecommendedImplementationDate);

public record CommunityEngagementMetric(
    CulturalCommunity Community,
    decimal EngagementLevel,
    int ParticipationCount,
    decimal SatisfactionScore);

public record ROIMetric(
    string MetricName,
    decimal CurrentValue,
    decimal ProjectedValue,
    string MeasurementUnit,
    TimeSpan MeasurementPeriod);

public enum CulturalSensitivityLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

// Multi-Cultural Content for appropriateness assessment
public record MultiCulturalContent(
    string Description,
    IEnumerable<string> Keywords,
    IEnumerable<string> VisualElements,
    IEnumerable<string>? AudioElements = null,
    IEnumerable<CulturalReference>? CulturalReferences = null,
    string ContentType = "General")
{
    public IEnumerable<string> AudioElements { get; init; } = AudioElements ?? new List<string>();
    public IEnumerable<CulturalReference> CulturalReferences { get; init; } = CulturalReferences ?? new List<CulturalReference>();
}

public record CulturalReference(
    string ReferenceType,
    CulturalCommunity OriginCommunity,
    string Description,
    CulturalSensitivityLevel SensitivityLevel);

// Enterprise analytics supporting types
public enum CulturalMaturityLevel
{
    Basic = 1,
    Developing = 2,
    Advanced = 3,
    Leading = 4,
    IndustryLeader = 5
}

public record EnterpriseClientProfile(
    string CompanyName,
    string Industry,
    int EmployeeCount,
    IEnumerable<CulturalCommunity> EmployeeCommunities,
    CulturalMaturityLevel CulturalMaturity,
    IEnumerable<string>? CurrentInitiatives = null,
    decimal AnnualDiversityBudget = 0m)
{
    public IEnumerable<string> CurrentInitiatives { get; init; } = CurrentInitiatives ?? new List<string>();
}

// Calendar synchronization supporting types
// CulturalEventType enum moved to LankaConnect.Domain.Common.Enums

// Geographic and engagement context types
// Note: GeographicRegion enum moved to LankaConnect.Domain.Common.Enums for consolidation
// US Metro regions now mapped to the consolidated enum values

public enum AnalyticsTimeframe
{
    Weekly = 1,
    Monthly = 2,
    Quarterly = 3,
    Annual = 4,
    MultiYear = 5
}

public record CulturalEngagementContext(
    string EngagementType,
    string BusinessContext,
    IEnumerable<string> RequiredOutcomes,
    decimal Budget = 0m,
    TimeSpan Timeline = default,
    IEnumerable<string>? Constraints = null)
{
    public IEnumerable<string> Constraints { get; init; } = Constraints ?? new List<string>();
}

// Additional supporting types for comprehensive cultural intelligence
// Note: ReligiousObservanceLevel is defined in GoogleCalendarCulturalEvent.cs

public enum CulturalLanguagePreference
{
    English = 1,
    Native = 2,
    Bilingual = 3,
    Multilingual = 4
}