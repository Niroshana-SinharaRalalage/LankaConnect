using System;
using System.Collections.Generic;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.Database.MultiLanguageRoutingModels;

namespace LankaConnect.Domain.Common.Database;

/// <summary>
/// Domain models for Cultural Conflict Resolution Engine
/// Supporting multi-cultural coordination for 6M+ South Asian diaspora
/// Handles Buddhist-Hindu coexistence, Islamic-Hindu respect, Sikh inclusivity
/// Performance targets: <50ms conflict detection, <200ms resolution
/// </summary>

#region Core Enums

/// <summary>
/// Cultural event priority levels based on religious and cultural significance
/// Level 10: Supreme sacred events (Vesak, Eid Al-Fitr)
/// Level 9: Major cultural festivals (Diwali, Vaisakhi) 
/// Level 8: Important celebrations (Thaipusam, Guru Nanak Jayanti)
/// </summary>
public enum CulturalEventPriority
{
    /// <summary>Supreme religious significance - Buddha's birth/enlightenment/death (Vesak), End of Ramadan (Eid)</summary>
    Level10Sacred = 10,
    
    /// <summary>Major cultural festivals - Festival of Lights (Diwali), Sikh New Year (Vaisakhi)</summary>
    Level9MajorFestival = 9,
    
    /// <summary>Important cultural celebrations - Tamil devotion (Thaipusam), Guru celebrations</summary>
    Level8ImportantCelebration = 8,
    
    /// <summary>Regional community events - Local cultural celebrations</summary>
    Level7CommunityEvent = 7,
    
    /// <summary>Social gatherings - Community meetups and social events</summary>
    Level6SocialGathering = 6,
    
    /// <summary>General events - Non-cultural community activities</summary>
    Level5GeneralEvent = 5
}

/// <summary>
/// Religious and cultural significance levels for event prioritization
/// Used to determine appropriate handling and resource allocation
/// </summary>
public enum ReligiousSignificance
{
    /// <summary>Highest religious importance - Core doctrinal events</summary>
    Supreme,
    
    /// <summary>Fundamental religious observance - Essential practice</summary>
    Fundamental,
    
    /// <summary>Significant religious event - Important but not essential</summary>
    Important,
    
    /// <summary>Moderate religious importance - Community preference</summary>
    Moderate,
    
    /// <summary>Social/cultural importance - Non-religious significance</summary>
    Social,
    
    /// <summary>Secular events - No religious component</summary>
    Secular
}

/// <summary>
/// Cultural authority sources for validation and authenticity verification
/// Each authority provides legitimacy for specific cultural/religious decisions
/// </summary>
public enum CulturalAuthority
{
    // Buddhist Authorities
    BuddhistCouncilSriLanka,
    MahabodhiSociety,
    BuddhistCouncilAmerica,
    
    // Islamic Authorities  
    IslamicSocietyNorthAmerica,
    PakistanAssociation,
    IslamicCouncilCanada,
    
    // Hindu Authorities
    HinduSocietyNorthAmerica,
    TamilAssociationNorthAmerica,
    VishwaHinduParishad,
    
    // Sikh Authorities
    SikhAssociationNorthAmerica,
    GurudwaraCouncil,
    KhalsaCouncil,
    
    // Multi-Cultural Authorities
    SouthAsianCulturalCouncil,
    InterFaithDialogueCouncil,
    DiasporaCouncilNorthAmerica
}

/// <summary>
/// Conflict severity levels determining response urgency and resource allocation
/// Higher severity requires immediate attention and specialized resolution
/// </summary>
public enum ConflictSeverity
{
    /// <summary>Minor conflicts - Can be resolved through standard processes</summary>
    Low,
    
    /// <summary>Moderate conflicts - Require cultural sensitivity consideration</summary>
    Medium,
    
    /// <summary>High conflicts - Need immediate attention and cultural expert mediation</summary>
    High,
    
    /// <summary>Critical conflicts - Threaten community harmony, require emergency response</summary>
    Critical,
    
    /// <summary>Emergency conflicts - Risk major community discord, all resources mobilized</summary>
    Emergency
}

/// <summary>
/// Conflict complexity levels indicating resolution difficulty and resource requirements
/// More complex conflicts require specialized algorithms and expert consultation
/// </summary>
public enum ConflictComplexity
{
    /// <summary>Simple conflicts - Single issue, two communities</summary>
    Simple,
    
    /// <summary>Moderate conflicts - Multiple issues or three communities</summary>
    Moderate,
    
    /// <summary>Complex conflicts - Multiple issues and communities with historical context</summary>
    Complex,
    
    /// <summary>Unprecedented conflicts - New scenarios requiring innovative solutions</summary>
    Unprecedented,
    
    /// <summary>Multi-dimensional conflicts - Cross-cultural, cross-regional, cross-generational</summary>
    MultiDimensional
}

/// <summary>
/// Community types representing major South Asian diaspora groups
/// Each has distinct cultural practices, calendars, and sensitivities
/// </summary>
public enum CommunityType
{
    // Sri Lankan Communities
    SriLankanBuddhist,
    SriLankanTamil,
    SriLankanSinhala,
    SriLankanMultiCultural,
    
    // Indian Communities  
    IndianHindu,
    IndianTamil,
    IndianPunjabi,
    IndianGujarati,
    IndianBengali,
    IndianMarathi,
    IndianTelugu,
    IndianKannada,
    IndianMalayali,
    
    // Pakistani Communities
    PakistaniMuslim,
    PakistaniPunjabi,
    PakistaniSindhi,
    PakistaniBaloch,
    
    // Bangladeshi Communities
    BengaliMuslim,
    BengaliHindu,
    
    // Sikh Communities
    SikhPunjabi,
    SikhCanadian,
    SikhAmerican,
    
    // Multi-Cultural
    SouthAsianGeneral,
    DiasporaMultiCultural
}

/// <summary>
/// Types of conflicts that can arise in multi-cultural environments
/// Each type requires different resolution strategies and approaches
/// </summary>
public enum ConflictType
{
    /// <summary>Competition for limited resources (venues, funding, volunteers)</summary>
    ResourceCompetition,
    
    /// <summary>Overlapping event timing causing scheduling conflicts</summary>
    TimingConflict,
    
    /// <summary>Language or communication barriers between communities</summary>
    CommunicationBarrier,
    
    /// <summary>Need for coordination across multiple cultural celebrations</summary>
    MultiCulturalCoordination,
    
    /// <summary>Insufficient resources to meet all community needs</summary>
    ResourceShortage,
    
    /// <summary>Conflicts over sacred or culturally sensitive content/timing</summary>
    SacredContentConflict,
    
    /// <summary>Disagreements on cultural authenticity or representation</summary>
    CulturalAuthenticityDispute,
    
    /// <summary>Conflicts between traditional and modern celebration approaches</summary>
    GenerationalConflict,
    
    /// <summary>Competition for community leadership or representation</summary>
    LeadershipDispute
}

/// <summary>
/// Resolution strategies for different types of cultural conflicts
/// Each strategy leverages specific cultural values and community strengths
/// </summary>
public enum ResolutionStrategy
{
    /// <summary>Buddhist-Hindu cooperation through shared Dharmic traditions and values</summary>
    DharmicCooperation,
    
    /// <summary>Islamic-Hindu coordination through mutual respect and separate but coordinated celebrations</summary>
    MutualRespectFramework,
    
    /// <summary>Sikh-led inclusive service approach leveraging seva (selfless service) values</summary>
    SikhInclusiveService,
    
    /// <summary>Building bridges between different cultural approaches and generations</summary>
    CulturalBridging,
    
    /// <summary>Expert mediation by cultural authorities and religious leaders</summary>
    ExpertMediation,
    
    /// <summary>Resource sharing and collaborative resource management</summary>
    ResourceSharing,
    
    /// <summary>Time-based coordination with sequential or parallel event scheduling</summary>
    TemporalCoordination,
    
    /// <summary>Geographic separation with location-based community coordination</summary>
    GeographicSeparation,
    
    /// <summary>Technology-enabled solutions for coordination and communication</summary>
    TechnologyMediated,
    
    /// <summary>Community-driven consensus building through dialogue and participation</summary>
    ConsensusBuilding
}

/// <summary>
/// Performance modes for different operational requirements and SLA compliance
/// Higher performance modes require optimized algorithms and resource allocation
/// </summary>
public enum PerformanceMode
{
    /// <summary>Standard performance for regular operations</summary>
    Standard,
    
    /// <summary>Fortune 500 SLA compliance mode with <50ms detection, <200ms resolution</summary>
    FortuneToOCompliance,
    
    /// <summary>Cultural event surge mode for handling 5x+ traffic during festivals</summary>
    CulturalEventSurge,
    
    /// <summary>Disaster recovery mode with degraded but functional performance</summary>
    DisasterRecovery,
    
    /// <summary>Emergency response mode with all resources mobilized</summary>
    EmergencyResponse
}

/// <summary>
/// Geographic spread patterns for community distribution analysis
/// Affects resource allocation and coordination strategies
/// </summary>
public enum GeographicSpread
{
    /// <summary>Localized to specific neighborhoods or cities</summary>
    Local,
    
    /// <summary>Spread across multiple cities in a region</summary>
    Regional,
    
    /// <summary>National presence across multiple states/provinces</summary>
    National,
    
    /// <summary>International presence across multiple countries</summary>
    International,
    
    /// <summary>Global diaspora distribution</summary>
    Global
}

/// <summary>
/// Resource types that can be subject to conflicts and require allocation
/// Each resource type has different sharing patterns and constraints
/// </summary>
public enum ResourceType
{
    /// <summary>Physical venue space for events and celebrations</summary>
    VenueSpace,
    
    /// <summary>Community attention and participation capacity</summary>
    CommunityAttention,
    
    /// <summary>Volunteer time and effort</summary>
    VolunteerTime,
    
    /// <summary>Financial resources and funding</summary>
    Funding,
    
    /// <summary>Cultural equipment and decorations</summary>
    CulturalEquipment,
    
    /// <summary>Media coverage and publicity</summary>
    MediaCoverage,
    
    /// <summary>Leadership time and involvement</summary>
    LeadershipTime,
    
    /// <summary>Technology infrastructure and digital platforms</summary>
    TechnologyInfrastructure
}

/// <summary>
/// Interaction types between different communities
/// Positive interactions build harmony, negative ones require intervention
/// </summary>
public enum InteractionType
{
    /// <summary>Communities celebrating together</summary>
    SharedCelebration,
    
    /// <summary>Communities supporting each other during events</summary>
    MutualSupport,
    
    /// <summary>Exchange of cultural knowledge and practices</summary>
    CulturalExchange,
    
    /// <summary>Collaborative projects and initiatives</summary>
    Collaboration,
    
    /// <summary>Friendly competition and parallel events</summary>
    FriendlyCompetition,
    
    /// <summary>Neutral coexistence with minimal interaction</summary>
    Coexistence,
    
    /// <summary>Misunderstandings requiring clarification</summary>
    Misunderstanding,
    
    /// <summary>Disagreements requiring mediation</summary>
    Disagreement,
    
    /// <summary>Open conflicts requiring intervention</summary>
    Conflict
}

/// <summary>
/// Community sentiment levels for feedback analysis
/// Used to gauge community reaction to conflict resolution strategies
/// </summary>
public enum Sentiment
{
    /// <summary>Very positive community response</summary>
    VeryPositive,
    
    /// <summary>Positive community response</summary>
    Positive,
    
    /// <summary>Neutral community response</summary>
    Neutral,
    
    /// <summary>Negative community response requiring attention</summary>
    Negative,
    
    /// <summary>Very negative community response requiring immediate intervention</summary>
    VeryNegative
}

/// <summary>
/// Validation levels for cultural authenticity verification
/// Higher levels require more authoritative sources and community input
/// </summary>
public enum ValidationLevel
{
    /// <summary>Basic validation through community feedback</summary>
    Community,
    
    /// <summary>Cultural organization validation</summary>
    CulturalOrganization,
    
    /// <summary>Religious authority validation for sacred content</summary>
    ReligiousAuthority,
    
    /// <summary>Academic and scholarly validation</summary>
    Academic,
    
    /// <summary>Multi-source validation combining multiple authorities</summary>
    MultiSource
}

/// <summary>
/// Service types for integration with other cultural intelligence services
/// Enables coordinated resolution across multiple platform capabilities
/// </summary>
public enum ServiceType
{
    /// <summary>Multi-language affinity routing service</summary>
    MultiLanguageRouting,
    
    /// <summary>Cultural event load distribution service</summary>
    CulturalEventDistribution,
    
    /// <summary>Geographic diaspora load balancing service</summary>
    GeographicLoadBalancing,
    
    /// <summary>Cultural calendar coordination service</summary>
    CulturalCalendar,
    
    /// <summary>Community analytics and insights service</summary>
    CommunityAnalytics,
    
    /// <summary>Revenue optimization service</summary>
    RevenueOptimization
}

#endregion

#region Core Request/Response Models

/// <summary>
/// Request context for analyzing cultural event priority and significance
/// Includes community size, geographic distribution, and authority sources
/// </summary>
public class CulturalEventAnalysisContext
{
    public CulturalEvent Event { get; set; } = CulturalEvent.Vesak;
    public int CommunitySize { get; set; }
    public GeographicSpread GeographicSpread { get; set; }
    public ReligiousSignificance ReligiousSignificance { get; set; }
    public CulturalBackground CulturalBackground { get; set; }
    public List<CulturalAuthority> AuthoritativeSources { get; set; } = new();
    public bool RequiresSpecialHandling { get; set; }
    public TimeSpan? CulturalSensitivityWindow { get; set; }
    public Dictionary<string, object>? AdditionalContext { get; set; }
}

/// <summary>
/// Response from cultural event priority analysis
/// Contains priority level, sensitivity scores, and handling requirements
/// </summary>
public class CulturalEventPriorityResult
{
    public CulturalEventPriority EventPriority { get; set; }
    public decimal CulturalSensitivityScore { get; set; }
    public decimal CulturalAuthenticityScore { get; set; }
    public bool AuthorityValidation { get; set; }
    public bool RequiresSpecialHandling { get; set; }
    public TimeSpan ConflictAvoidanceRadius { get; set; }
    public bool LunarCalendarDependency { get; set; }
    public List<string> HandlingRecommendations { get; set; } = new();
    public Dictionary<CulturalAuthority, decimal> AuthorityEndorsements { get; set; } = new();
}

/// <summary>
/// Scenario definition for multi-cultural conflict detection and analysis
/// Includes overlapping events, geographic scope, and community impact
/// </summary>
public class MultiCulturalConflictScenario
{
    public List<CulturalEventContext> OverlappingEvents { get; set; } = new();
    public List<GeographicRegion> GeographicScope { get; set; } = new();
    public TimeSpan TimeOverlap { get; set; }
    public List<ResourceType> ContendedResources { get; set; } = new();
    public PerformanceMode PerformanceMode { get; set; } = PerformanceMode.Standard;
    public bool RequiresRealTimeAnalysis { get; set; }
    public decimal ExpectedCommunityImpact { get; set; }
}

/// <summary>
/// Individual cultural event context within a conflict scenario
/// Represents one community's event that may conflict with others
/// </summary>
public class CulturalEventContext
{
    public CulturalEvent Event { get; set; } = CulturalEvent.Vesak;
    public CommunityType Community { get; set; }
    public int Size { get; set; }
    public CulturalEventPriority Priority { get; set; }
    public List<ResourceType> RequiredResources { get; set; } = new();
    public TimeSpan Duration { get; set; }
    public GeographicRegion PrimaryRegion { get; set; }
    public decimal CommunityEngagement { get; set; }
}

/// <summary>
/// Result of multi-cultural conflict detection analysis
/// Contains severity assessment, affected communities, and resolution requirements
/// </summary>
public class CulturalConflictDetectionResult
{
    public ConflictSeverity ConflictSeverity { get; set; }
    public ConflictComplexity ConflictComplexity { get; set; }
    public List<CommunityType> AffectedCommunities { get; set; } = new();
    public List<ResourceType> ContendedResources { get; set; } = new();
    public bool RequiresImmediateResolution { get; set; }
    public bool RequiresExpertMediation { get; set; }
    public decimal PotentialHarmonyImpact { get; set; }
    public List<ConflictType> IdentifiedConflictTypes { get; set; } = new();
    public ConflictPerformanceMetrics PerformanceMetrics { get; set; } = new();
    public TimeSpan RecommendedResolutionTimeframe { get; set; }
}

/// <summary>
/// Performance metrics for conflict detection and resolution operations
/// Used to validate SLA compliance and optimize system performance
/// </summary>
public class ConflictPerformanceMetrics
{
    public TimeSpan DetectionTime { get; set; }
    public TimeSpan ResolutionTime { get; set; }
    public decimal CacheHitRate { get; set; }
    public int DatabaseQueries { get; set; }
    public decimal CPUUsage { get; set; }
    public decimal MemoryUsage { get; set; }
    public int ConcurrentRequests { get; set; }
    public bool SLACompliance { get; set; }
}

/// <summary>
/// Request for analyzing compatibility between different community types
/// Used to predict successful coordination and identify bridge-building opportunities
/// </summary>
public class CommunityCompatibilityRequest
{
    public CommunityType PrimaryCommunity { get; set; }
    public CommunityType SecondaryCommunity { get; set; }
    public CompatibilityAnalysisDepth AnalysisDepth { get; set; }
    public bool HistoricalContext { get; set; }
    public bool CulturalBridgingOpportunities { get; set; }
    public List<InteractionType> PreviousInteractions { get; set; } = new();
    public GeographicRegion AnalysisRegion { get; set; }
}

/// <summary>
/// Analysis depth levels for compatibility assessment
/// More comprehensive analysis provides better predictions but requires more resources
/// </summary>
public enum CompatibilityAnalysisDepth
{
    Basic,
    Standard,
    Comprehensive,
    ExhaustiveWithHistoricalAnalysis
}

/// <summary>
/// Result of community compatibility analysis
/// Contains compatibility score, shared values, and bridging opportunities
/// </summary>
public class CommunityCompatibilityResult
{
    public decimal CompatibilityScore { get; set; }
    public List<string> SharedValues { get; set; } = new();
    public List<string> BridgingOpportunities { get; set; } = new();
    public List<string> PotentialFrictionPoints { get; set; } = new();
    public List<InteractionType> RecommendedInteractions { get; set; } = new();
    public decimal HistoricalSuccessRate { get; set; }
    public List<string> SuccessFactors { get; set; } = new();
    public List<string> RiskMitigationStrategies { get; set; } = new();
}

#endregion

#region Conflict Resolution Models

/// <summary>
/// Context for cultural conflict requiring resolution
/// Contains all information needed to determine optimal resolution strategy
/// </summary>
public class CulturalConflictContext
{
    public Guid ConflictId { get; set; } = Guid.NewGuid();
    public ConflictType ConflictType { get; set; }
    public List<CommunityType> InvolvedCommunities { get; set; } = new();
    public ConflictSeverity ConflictSeverity { get; set; }
    public ConflictComplexity ConflictComplexity { get; set; }
    public List<ResourceType> AvailableResources { get; set; } = new();
    public List<ResourceType> ContendedResources { get; set; } = new();
    public bool CulturalSensitivityRequired { get; set; }
    public bool RequiresSeparateSpaces { get; set; }
    public bool RequiresCarefulScheduling { get; set; }
    public bool SevaOpportunityAvailable { get; set; }
    public bool CommunityServicePotential { get; set; }
    public decimal RevenueImpact { get; set; }
    public PerformanceMode PerformanceMode { get; set; } = PerformanceMode.Standard;
    public DateTime ConflictDetectedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object>? AdditionalContext { get; set; }
}

/// <summary>
/// Result of cultural conflict resolution process
/// Contains chosen strategy, outcomes, and metrics for success measurement
/// </summary>
public class CulturalConflictResolutionResult
{
    public ResolutionStrategy ResolutionStrategy { get; set; }
    public decimal CommunityHarmonyScore { get; set; }
    public decimal ResolutionAccuracy { get; set; }
    public decimal CommunityAcceptanceScore { get; set; }
    public bool CulturalAuthenticityPreserved { get; set; }
    public bool RequiresSeparateVenues { get; set; }
    public bool CoordinatedTiming { get; set; }
    public bool CrossCulturalVolunteering { get; set; }
    public List<string> BridgingActivities { get; set; } = new();
    public List<string> SevaActivities { get; set; } = new();
    public List<string> CommunityServiceOpportunities { get; set; } = new();
    public List<string> InterfaithDialogueOpportunities { get; set; } = new();
    public ConflictPerformanceMetrics PerformanceMetrics { get; set; } = new();
    public TimeSpan ResolutionTimeframe { get; set; }
    public List<string> FollowUpActions { get; set; } = new();
}

#endregion

#region Revenue and Business Optimization Models

/// <summary>
/// Request for optimizing conflict resolution with revenue considerations
/// Balances cultural sensitivity with business objectives
/// </summary>
public class ConflictRevenueOptimizationRequest
{
    public CulturalConflictContext ConflictScenario { get; set; } = new();
    public RevenueOptimizationCriteria OptimizationCriteria { get; set; } = new();
    public decimal TargetPlatformRevenue { get; set; }
    public List<RevenueStream> AffectedRevenueStreams { get; set; } = new();
    public bool EnterpriseClientImpact { get; set; }
    public ClientTier ClientTier { get; set; }
}

/// <summary>
/// Criteria for balancing revenue optimization with cultural sensitivity
/// Weights different factors in the optimization algorithm
/// </summary>
public class RevenueOptimizationCriteria
{
    public decimal RevenueWeight { get; set; } = 0.40m;
    public decimal CulturalSensitivityWeight { get; set; } = 0.35m;
    public decimal EngagementWeight { get; set; } = 0.25m;
    public decimal MinimumCulturalSensitivityThreshold { get; set; } = 0.80m;
    public decimal MinimumCommunityAcceptanceThreshold { get; set; } = 0.85m;
    public bool RequiresCulturalAuthorityApproval { get; set; }
}

/// <summary>
/// Revenue streams that can be affected by cultural conflicts
/// Different streams have different optimization strategies
/// </summary>
public enum RevenueStream
{
    PremiumSubscriptions,
    APIAccess,
    EventTicketing,
    BusinessDirectoryListings,
    CulturalConsultingServices,
    PartnershipRevenue,
    AdvertisingRevenue,
    EnterpriseContracts
}

/// <summary>
/// Client tier levels for differentiated service and optimization
/// Higher tiers receive priority treatment and specialized algorithms
/// </summary>
public enum ClientTier
{
    Community,
    Professional,
    Enterprise,
    FortuneToO
}

/// <summary>
/// Result of revenue-optimized conflict resolution
/// Shows financial impact while maintaining cultural sensitivity
/// </summary>
public class ConflictRevenueOptimizationResult
{
    public decimal RevenueIncrease { get; set; }
    public decimal EngagementImprovement { get; set; }
    public decimal MultiCulturalParticipationIncrease { get; set; }
    public bool CulturalSensitivityMaintained { get; set; }
    public List<RevenueStream> OptimizedStreams { get; set; } = new();
    public Dictionary<RevenueStream, decimal> StreamOptimization { get; set; } = new();
    public decimal ROIProjection { get; set; }
    public TimeSpan PaybackPeriod { get; set; }
}

/// <summary>
/// Request for enterprise-level conflict analysis and revenue impact assessment
/// Provides detailed analytics for Fortune 500 clients and strategic planning
/// </summary>
public class EnterpriseConflictAnalysisRequest
{
    public ClientTier ClientTier { get; set; }
    public List<ConflictType> ConflictTypes { get; set; } = new();
    public TimeSpan AnalysisPeriod { get; set; }
    public bool RevenueProjections { get; set; }
    public bool DiversityImpactAnalysis { get; set; }
    public bool CompetitiveAnalysis { get; set; }
    public List<GeographicRegion> AnalysisRegions { get; set; } = new();
}

/// <summary>
/// Enterprise-level analysis result with comprehensive business metrics
/// Supports strategic decision making and Fortune 500 reporting requirements
/// </summary>
public class EnterpriseConflictAnalysisResult
{
    public decimal ProjectedAnnualRevenueIncrease { get; set; }
    public decimal DiversityInitiativeValue { get; set; }
    public decimal EnterpriseClientRetention { get; set; }
    public decimal CulturalIntelligenceROI { get; set; }
    public Dictionary<ConflictType, decimal> ConflictTypeImpact { get; set; } = new();
    public Dictionary<GeographicRegion, decimal> RegionalOpportunities { get; set; } = new();
    public List<string> StrategicRecommendations { get; set; } = new();
    public CompetitiveAdvantageAnalysis CompetitiveAdvantage { get; set; } = new();
}

/// <summary>
/// Competitive advantage analysis for enterprise positioning
/// Identifies unique strengths and market differentiation opportunities
/// </summary>
public class CompetitiveAdvantageAnalysis
{
    public List<string> UniqueCapabilities { get; set; } = new();
    public decimal MarketDifferentiation { get; set; }
    public List<string> CompetitorGaps { get; set; } = new();
    public decimal BarrierToEntry { get; set; }
    public List<string> StrategicMoats { get; set; } = new();
}

#endregion

#region Community Harmony and Authenticity Models

/// <summary>
/// Request for validating cultural authenticity in conflict resolution
/// Ensures solutions maintain religious and cultural integrity
/// </summary>
public class CulturalAuthenticityValidationRequest
{
    public List<SacredEventContext> SacredEvents { get; set; } = new();
    public ValidationLevel ValidationLevel { get; set; }
    public bool CommunityFeedbackRequired { get; set; }
    public List<CulturalAuthority> RequiredAuthorities { get; set; } = new();
    public bool CrossCulturalValidation { get; set; }
    public TimeSpan ValidationTimeframe { get; set; }
}

/// <summary>
/// Context for sacred events requiring special validation
/// Contains authority sources and community sensitivity requirements
/// </summary>
public class SacredEventContext
{
    public CulturalEvent Event { get; set; } = CulturalEvent.Vesak;
    public ReligiousSignificance Significance { get; set; }
    public CulturalAuthority[] AuthoritySources { get; set; } = Array.Empty<CulturalAuthority>();
    public List<CommunityType> AffectedCommunities { get; set; } = new();
    public bool RequiresSpecialHandling { get; set; }
    public TimeSpan SacredPeriod { get; set; }
}

/// <summary>
/// Result of cultural authenticity validation process
/// Provides scores and approval status from relevant authorities
/// </summary>
public class CulturalAuthenticityValidationResult
{
    public decimal AuthenticityScore { get; set; }
    public bool ReligiousAuthorityApproval { get; set; }
    public decimal CommunityAcceptanceScore { get; set; }
    public bool CulturalIntegrityMaintained { get; set; }
    public Dictionary<CulturalAuthority, decimal> AuthorityScores { get; set; } = new();
    public List<string> ValidationComments { get; set; } = new();
    public List<string> RequiredModifications { get; set; } = new();
    public bool RequiresRevalidation { get; set; }
}

/// <summary>
/// Request for generating community harmony metrics and analysis
/// Tracks positive outcomes and areas for improvement
/// </summary>
public class CommunityHarmonyMetricsRequest
{
    public List<CommunityInteraction> CommunityInteractions { get; set; } = new();
    public TimeSpan MeasurementPeriod { get; set; }
    public MetricsDepth MetricsDepth { get; set; }
    public List<GeographicRegion> Regions { get; set; } = new();
    public bool TrendAnalysis { get; set; }
    public bool PredictiveAnalytics { get; set; }
}

/// <summary>
/// Individual community interaction record for harmony analysis
/// Tracks how different communities interact and collaborate
/// </summary>
public class CommunityInteraction
{
    public CommunityType Community1 { get; set; }
    public CommunityType Community2 { get; set; }
    public InteractionType InteractionType { get; set; }
    public DateTime InteractionDate { get; set; }
    public decimal SuccessScore { get; set; }
    public List<string> PositiveOutcomes { get; set; } = new();
    public List<string> Challenges { get; set; } = new();
    public GeographicRegion Region { get; set; }
}

/// <summary>
/// Depth levels for metrics analysis
/// More comprehensive analysis provides better insights but requires more resources
/// </summary>
public enum MetricsDepth
{
    Basic,
    Standard,
    Comprehensive,
    PredictiveAnalytics
}

/// <summary>
/// Result of community harmony metrics analysis
/// Provides comprehensive view of inter-community relationships
/// </summary>
public class CommunityHarmonyMetricsResult
{
    public decimal OverallHarmonyScore { get; set; }
    public decimal CrossCulturalEngagement { get; set; }
    public decimal ConflictResolutionSuccess { get; set; }
    public Dictionary<CommunityType, decimal> CommunityHarmonyScores { get; set; } = new();
    public Dictionary<(CommunityType, CommunityType), decimal> PairwiseCompatibility { get; set; } = new();
    public List<string> CommunityBridgingActivities { get; set; } = new();
    public List<string> SuccessFactors { get; set; } = new();
    public List<string> ImprovementAreas { get; set; } = new();
    public HarmonyTrendAnalysis? TrendAnalysis { get; set; }
}

/// <summary>
/// Trend analysis for community harmony over time
/// Identifies patterns and predicts future harmony levels
/// </summary>
public class HarmonyTrendAnalysis
{
    public decimal TrendDirection { get; set; } // Positive = improving, Negative = declining
    public List<HarmonyDataPoint> HistoricalData { get; set; } = new();
    public List<HarmonyDataPoint> PredictedData { get; set; } = new();
    public List<string> TrendDrivers { get; set; } = new();
    public List<string> RiskFactors { get; set; } = new();
}

/// <summary>
/// Individual data point for harmony trend analysis
/// Represents harmony metrics at a specific point in time
/// </summary>
public class HarmonyDataPoint
{
    public DateTime Timestamp { get; set; }
    public decimal HarmonyScore { get; set; }
    public List<string> ContributingFactors { get; set; } = new();
}

#endregion

#region Advanced Algorithm and Integration Models

/// <summary>
/// Request for predicting future cultural conflicts using machine learning
/// Enables proactive conflict prevention and resource planning
/// </summary>
public class ConflictPredictionRequest
{
    public TimeSpan PredictionHorizon { get; set; }
    public TimeSpan HistoricalDataPeriod { get; set; }
    public bool CommunityGrowthTrends { get; set; }
    public bool SeasonalPatterns { get; set; }
    public bool MachineLearningEnabled { get; set; }
    public List<CommunityType> FocusCommunities { get; set; } = new();
    public List<GeographicRegion> PredictionRegions { get; set; } = new();
    public decimal ConfidenceThreshold { get; set; } = 0.80m;
}

/// <summary>
/// Result of conflict prediction analysis
/// Provides future conflict probabilities and prevention strategies
/// </summary>
public class ConflictPredictionResult
{
    public decimal PredictionAccuracy { get; set; }
    public List<PredictedConflict> PredictedConflicts { get; set; } = new();
    public List<string> PreventionRecommendations { get; set; } = new();
    public List<string> ResourceAllocationSuggestions { get; set; } = new();
    public Dictionary<ConflictType, decimal> ConflictTypeProbabilities { get; set; } = new();
    public List<string> SeasonalPatterns { get; set; } = new();
    public PredictionModelMetrics ModelMetrics { get; set; } = new();
}

/// <summary>
/// Individual predicted conflict with timing and probability
/// Used for proactive conflict prevention and preparation
/// </summary>
public class PredictedConflict
{
    public DateTime PredictedDate { get; set; }
    public decimal Probability { get; set; }
    public ConflictType ConflictType { get; set; }
    public List<CommunityType> InvolvedCommunities { get; set; } = new();
    public ConflictSeverity ExpectedSeverity { get; set; }
    public List<string> PreventionStrategies { get; set; } = new();
    public decimal ConfidenceLevel { get; set; }
}

/// <summary>
/// Metrics for evaluating prediction model performance
/// Used to assess and improve machine learning algorithms
/// </summary>
public class PredictionModelMetrics
{
    public decimal Accuracy { get; set; }
    public decimal Precision { get; set; }
    public decimal Recall { get; set; }
    public decimal F1Score { get; set; }
    public int TrainingDataPoints { get; set; }
    public DateTime LastTrainingUpdate { get; set; }
    public List<string> FeatureImportance { get; set; } = new();
}

/// <summary>
/// Request for machine learning optimization of conflict resolution
/// Continuously improves resolution strategies based on outcomes
/// </summary>
public class MachineLearningOptimizationRequest
{
    public List<ConflictPattern> ConflictPatterns { get; set; } = new();
    public LearningMode LearningMode { get; set; }
    public OptimizationTarget OptimizationTarget { get; set; }
    public decimal LearningRate { get; set; } = 0.01m;
    public int TrainingIterations { get; set; } = 1000;
    public bool CrossValidation { get; set; } = true;
}

/// <summary>
/// Pattern of conflicts and their resolution outcomes
/// Used to train machine learning models for better resolution strategies
/// </summary>
public class ConflictPattern
{
    public string Pattern { get; set; } = string.Empty;
    public decimal SuccessRate { get; set; }
    public List<ConflictType> ConflictTypes { get; set; } = new();
    public List<CommunityType> CommunityTypes { get; set; } = new();
    public ResolutionStrategy BestStrategy { get; set; }
    public int OccurrenceCount { get; set; }
    public TimeSpan AverageResolutionTime { get; set; }
}

/// <summary>
/// Learning modes for machine learning optimization
/// Different modes optimize for different outcomes
/// </summary>
public enum LearningMode
{
    SupervisedLearning,
    ReinforcementLearning,
    ContinuousImprovement,
    AdaptiveLearning
}

/// <summary>
/// Optimization targets for machine learning algorithms
/// Determines what metric the algorithm optimizes for
/// </summary>
public enum OptimizationTarget
{
    CommunityHarmony,
    ResolutionSpeed,
    CulturalAuthenticity,
    RevenueOptimization,
    MultiObjective
}

/// <summary>
/// Result of machine learning optimization process
/// Shows improvement metrics and updated model performance
/// </summary>
public class MachineLearningOptimizationResult
{
    public decimal OptimizationImprovement { get; set; }
    public decimal PatternRecognitionAccuracy { get; set; }
    public bool AdaptiveLearningEnabled { get; set; }
    public bool ContinuousImprovementActive { get; set; }
    public Dictionary<string, decimal> FeatureWeights { get; set; } = new();
    public List<string> ModelUpdates { get; set; } = new();
    public PredictionModelMetrics UpdatedMetrics { get; set; } = new();
    public DateTime NextOptimizationScheduled { get; set; }
}

#endregion

#region Service Integration Models

/// <summary>
/// Request for integrating with other cultural intelligence services
/// Enables coordinated resolution across multiple platform capabilities
/// </summary>
public class ServiceIntegrationRequest
{
    public CulturalConflictContext ConflictContext { get; set; } = new();
    public List<ServiceType> RequiredServices { get; set; } = new();
    public bool RealTimeCoordination { get; set; }
    public bool DataSynchronization { get; set; }
    public PerformanceMode PerformanceMode { get; set; } = PerformanceMode.Standard;
}

/// <summary>
/// Result of service integration for conflict resolution
/// Shows coordination success and unified cultural intelligence
/// </summary>
public class ServiceIntegrationResult
{
    public bool IntegrationSuccess { get; set; }
    public decimal ServiceCoordination { get; set; }
    public bool CrossServiceOptimization { get; set; }
    public bool UnifiedCulturalIntelligence { get; set; }
    public Dictionary<ServiceType, decimal> ServicePerformance { get; set; } = new();
    public List<string> IntegrationBenefits { get; set; } = new();
    public List<string> CoordinationChallenges { get; set; } = new();
}

/// <summary>
/// Request for coordinating with event load distribution during conflicts
/// Optimizes resource allocation and traffic management
/// </summary>
public class EventDistributionCoordinationRequest
{
    public CulturalConflictContext ConflictScenario { get; set; } = new();
    public bool LoadBalancingRequired { get; set; }
    public bool DynamicResourceAllocation { get; set; }
    public decimal ExpectedTrafficMultiplier { get; set; } = 1.0m;
    public List<ResourceType> CriticalResources { get; set; } = new();
}

/// <summary>
/// Result of event distribution coordination
/// Shows resource optimization and conflict mitigation outcomes
/// </summary>
public class EventDistributionCoordinationResult
{
    public decimal ResourceOptimization { get; set; }
    public decimal LoadBalancingEfficiency { get; set; }
    public bool ConflictMitigation { get; set; }
    public bool ServiceContinuity { get; set; }
    public Dictionary<ResourceType, decimal> ResourceUtilization { get; set; } = new();
    public List<string> OptimizationActions { get; set; } = new();
    public ConflictPerformanceMetrics PerformanceMetrics { get; set; } = new();
}

#endregion

#region Error Handling and Resilience Models

/// <summary>
/// Context for handling conflict resolution failures
/// Contains failure information and community feedback for recovery
/// </summary>
public class ConflictResolutionFailureContext
{
    public ResolutionFailureType FailureType { get; set; }
    public ResolutionStrategy OriginalStrategy { get; set; }
    public string FailureReason { get; set; } = string.Empty;
    public List<CommunityFeedback> CommunityFeedback { get; set; } = new();
    public DateTime FailureTimestamp { get; set; } = DateTime.UtcNow;
    public ConflictSeverity ImpactSeverity { get; set; }
    public bool RequiresImmediateAction { get; set; }
}

/// <summary>
/// Types of resolution failures that can occur
/// Each type requires different recovery strategies
/// </summary>
public enum ResolutionFailureType
{
    CommunityRejection,
    AuthorityDisapproval,
    ResourceUnavailability,
    TechnicalFailure,
    CulturalSensitivityViolation,
    TimelineImpracticality,
    BudgetConstraints
}

/// <summary>
/// Community feedback on conflict resolution attempts
/// Used to gauge acceptance and guide alternative strategies
/// </summary>
public class CommunityFeedback
{
    public CommunityType Community { get; set; }
    public Sentiment Sentiment { get; set; }
    public List<string> SpecificComments { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();
    public decimal AcceptanceScore { get; set; }
    public bool WillingToReconsider { get; set; }
}

/// <summary>
/// Result of conflict resolution failure handling
/// Provides alternative strategies and recovery plan
/// </summary>
public class ConflictResolutionFailureResult
{
    public ResolutionStrategy FallbackStrategy { get; set; }
    public List<string> CommunityEngagementPlan { get; set; } = new();
    public bool MediationRequired { get; set; }
    public bool ServiceContinuity { get; set; }
    public List<string> RecoveryActions { get; set; } = new();
    public TimeSpan ExpectedRecoveryTime { get; set; }
    public decimal RecoveryProbability { get; set; }
    public List<string> PreventionMeasures { get; set; } = new();
}

/// <summary>
/// Disaster recovery scenario for system failures
/// Ensures cultural data preservation and service continuity
/// </summary>
public class DisasterRecoveryScenario
{
    public SystemFailureType FailureType { get; set; }
    public List<DisasterRecoveryCulturalDataType> AffectedCulturalData { get; set; } = new();
    public TimeSpan RecoveryTimeObjective { get; set; }
    public TimeSpan RecoveryPointObjective { get; set; }
    public GeographicRegion AffectedRegion { get; set; }
    public List<CommunityType> AffectedCommunities { get; set; } = new();
}

/// <summary>
/// Types of system failures that can affect conflict resolution
/// Each type requires different disaster recovery procedures
/// </summary>
public enum SystemFailureType
{
    DatabasePartitionFailure,
    NetworkConnectivityLoss,
    ServiceOverload,
    DataCorruption,
    SecurityBreach,
    RegionalOutage
}

/// <summary>
/// Types of cultural data that must be preserved during disasters
/// Critical for maintaining cultural intelligence and service continuity
/// NOTE: Uses CulturalDataType from Domain.Common.Enums for consistency
/// </summary>
public enum DisasterRecoveryCulturalDataType
{
    SacredEventCalendar,
    CommunityProfiles,
    ConflictResolutionHistory,
    CulturalAuthorityContacts,
    AuthenticityValidations,
    HarmonyMetrics
}


#endregion