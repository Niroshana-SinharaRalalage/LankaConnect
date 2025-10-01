using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Domain.Events.ValueObjects;
using GeographicRegion = LankaConnect.Domain.Common.Enums.GeographicRegion;
using CulturalConflictEvents = LankaConnect.Domain.Events.ValueObjects.CulturalConflict;
using CulturalConflict = LankaConnect.Domain.Communications.ValueObjects.CulturalConflict;

namespace LankaConnect.Domain.Communications.Services;

/// <summary>
/// Multi-Cultural Calendar Engine for Phase 8 Global Platform Expansion
/// Enables $8.5M-$15.2M additional revenue through South Asian diaspora market
/// Target: 6M+ South Asian Americans across Indian, Pakistani, Bangladeshi, Sikh communities
/// </summary>
public interface IMultiCulturalCalendarEngine
{
    /// <summary>
    /// Get comprehensive cultural calendar for specific community and year
    /// Supports Indian Hindu (North/South), Pakistani Islamic, Bangladeshi Bengali, Sikh calendars
    /// </summary>
    Task<Result<MultiCulturalCalendar>> GetCulturalCalendarAsync(
        CulturalCommunity community, 
        int year, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get cross-cultural events that span multiple South Asian communities
    /// Enables community bridge-building and cultural integration initiatives
    /// </summary>
    Task<Result<IEnumerable<CrossCulturalEvent>>> GetCrossCulturalEventsAsync(
        IEnumerable<CulturalCommunity> communities,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detect cultural conflicts for proposed events across multiple communities
    /// Critical for enterprise Fortune 500 diversity initiatives and HR integration
    /// </summary>
    Task<Result<CulturalConflictAnalysis>> DetectCrossCulturalConflictsAsync(
        CulturalEvent proposedEvent,
        IEnumerable<CulturalCommunity> targetCommunities,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate cultural appropriateness of multi-cultural content
    /// Ensures cultural sensitivity across diverse South Asian diaspora communities
    /// </summary>
    Task<Result<CulturalAppropriatenessAssessment>> ValidateMultiCulturalContentAsync(
        MultiCulturalContent content,
        IEnumerable<CulturalCommunity> targetCommunities,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate cultural intelligence recommendations for cross-community engagement
    /// Powers AI-driven community matching and cultural bridge-building
    /// </summary>
    Task<Result<CulturalIntelligenceRecommendation>> GetCulturalIntelligenceRecommendationsAsync(
        MultiCulturalCommunity sourceCommunity,
        IEnumerable<CulturalCommunity> targetCommunities,
        CulturalEngagementContext context,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get diaspora community clustering analysis for geographic market expansion
    /// Supports Fortune 500 employee population analysis and community outreach
    /// </summary>
    Task<Result<DiasporaClusteringAnalysis>> GetDiasporaClusteringAnalysisAsync(
        GeographicRegion region,
        IEnumerable<CulturalCommunity> communities,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate cultural calendar synchronization for multi-community events
    /// Optimizes timing for maximum cultural participation and engagement
    /// </summary>
    Task<Result<CulturalCalendarSynchronization>> CalculateCalendarSynchronizationAsync(
        IEnumerable<CulturalCommunity> communities,
        CulturalEventType eventType,
        DateTimeOffset proposedDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate multi-cultural enterprise analytics for Fortune 500 clients
    /// Revenue driver: $2.5M-$4.8M from enhanced enterprise diversity contracts
    /// </summary>
    Task<Result<MultiCulturalEnterpriseAnalytics>> GenerateEnterpriseAnalyticsAsync(
        EnterpriseClientProfile clientProfile,
        IEnumerable<CulturalCommunity> employeeCommunities,
        AnalyticsTimeframe timeframe,
        CancellationToken cancellationToken = default);
}

// Supporting types for Multi-Cultural Calendar Engine
public record MultiCulturalCalendar(
    CulturalCommunity Community,
    int Year,
    IEnumerable<CulturalEvent> MajorFestivals,
    IEnumerable<CulturalEvent> ReligiousObservances,
    IEnumerable<CulturalEvent> CulturalCelebrations,
    IEnumerable<CulturalEvent> RegionalEvents,
    CalendarSystem PrimaryCalendarSystem,
    IEnumerable<CalendarSystem> SupportedCalendarSystems);

public record CulturalConflictAnalysis(
    bool HasConflicts,
    IEnumerable<CulturalConflict> IdentifiedConflicts,
    IEnumerable<CulturalResolution> SuggestedResolutions,
    decimal ConflictSeverityScore,
    IEnumerable<CulturalCommunity> AffectedCommunities);

public record CulturalAppropriatenessAssessment(
    decimal AppropriatenessScore,
    IEnumerable<CulturalCommunity> AppropriateCommunities,
    IEnumerable<CulturalCommunity> ProblematicCommunities,
    IEnumerable<CulturalSensitivityIssue> IdentifiedIssues,
    IEnumerable<CulturalAdaptation> SuggestedAdaptations);

public record CulturalIntelligenceRecommendation(
    IEnumerable<CrossCulturalEngagementStrategy> Strategies,
    IEnumerable<CulturalBridgeBuilding> BridgeOpportunities,
    decimal EngagementSuccessProbability,
    IEnumerable<CulturalRisk> PotentialRisks,
    IEnumerable<CulturalBenefit> ExpectedBenefits);

public record DiasporaClusteringAnalysis(
    GeographicRegion Region,
    IEnumerable<CommunityCluster> CommunityDistribution,
    decimal CulturalDiversityIndex,
    IEnumerable<CrossCulturalOpportunity> IntegrationOpportunities,
    MarketExpansionPotential ExpansionPotential);

public record CulturalCalendarSynchronization(
    DateTimeOffset OptimalDate,
    IEnumerable<CulturalCommunity> OptimalCommunities,
    decimal ParticipationProbability,
    IEnumerable<CulturalConflict> MinorConflicts,
    IEnumerable<CulturalEnhancement> SynchronizationBenefits);

public record MultiCulturalEnterpriseAnalytics(
    EnterpriseClientProfile Client,
    CulturalDiversityMetrics DiversityMetrics,
    IEnumerable<CulturalEngagementOpportunity> EngagementOpportunities,
    CulturalROIProjection ROIProjection,
    IEnumerable<CulturalRisk> ComplianceRisks);

// CulturalEventType enum removed - using LankaConnect.Domain.Common.Enums.CulturalEventType

// Note: GeographicRegion enum moved to LankaConnect.Domain.Common.Enums for consolidation
// US Metro regions now mapped to the consolidated enum values

public enum AnalyticsTimeframe
{
    Monthly = 1,
    Quarterly = 2,
    Annual = 3,
    MultiYear = 4
}

public record CulturalEngagementContext(
    string EngagementType,
    string BusinessContext,
    IEnumerable<string> RequiredOutcomes);

public record EnterpriseClientProfile(
    string CompanyName,
    string Industry,
    int EmployeeCount,
    IEnumerable<CulturalCommunity> EmployeeCommunities,
    CulturalMaturityLevel CulturalMaturity);

public enum CulturalMaturityLevel
{
    Basic = 1,
    Developing = 2,
    Advanced = 3,
    Leading = 4
}