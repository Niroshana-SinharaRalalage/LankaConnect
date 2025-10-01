using System;
using System.Collections.Generic;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Domain.Shared;

/// <summary>
/// Heritage language preservation request
/// </summary>
public class HeritageLanguagePreservationRequest
{
    public SouthAsianLanguage TargetLanguage { get; set; }
    public Guid CommunityId { get; set; }
    public CulturalRegion Region { get; set; }
    public TimeSpan AnalysisPeriod { get; set; }
    public List<GenerationalCohort> TargetCohorts { get; set; } = new();
}

/// <summary>
/// Heritage language preservation result
/// </summary>
public class HeritageLanguagePreservationResult
{
    public decimal LanguageVitality { get; set; }
    public decimal YouthEngagement { get; set; }
    public decimal ElderKnowledgeTransfer { get; set; }
    public Dictionary<GenerationalCohort, decimal> GenerationalDecline { get; set; } = new();
    public List<string> PreservationRecommendations { get; set; } = new();
    public List<string> CommunityEngagementOpportunities { get; set; } = new();
}

/// <summary>
/// Intergenerational content request
/// </summary>
public class IntergenerationalContentRequest
{
    public SouthAsianLanguage FirstGenerationLanguage { get; set; }
    public SouthAsianLanguage YoungerGenerationLanguage { get; set; }
    public LanguageBridgingStrategy BridgingStrategy { get; set; }
    public CulturalBackground FamilyCulturalBackground { get; set; }
    public ContentType ContentType { get; set; }
}

/// <summary>
/// Intergenerational content result
/// </summary>
public class IntergenerationalContentResult
{
    public bool BilingualContent { get; set; }
    public decimal GenerationalEngagement { get; set; }
    public Dictionary<SouthAsianLanguage, decimal> LanguageBalance { get; set; } = new();
    public List<string> LanguageLearningOpportunities { get; set; } = new();
    public List<string> CulturalConnectionPoints { get; set; } = new();
}

/// <summary>
/// Heritage language learning recommendations
/// </summary>
public class HeritageLanguageLearningRecommendations
{
    public List<string> RecommendedCourses { get; set; } = new();
    public List<CulturalEvent> LearningOpportunityEvents { get; set; } = new();
    public Dictionary<string, decimal> LearningPathProgress { get; set; } = new();
    public List<string> CommunityResources { get; set; } = new();
}

/// <summary>
/// Cultural education pathway
/// </summary>
public class CulturalEducationPathway
{
    public List<string> EducationModules { get; set; } = new();
    public TimeSpan EstimatedCompletionTime { get; set; }
    public Dictionary<string, decimal> ModuleProgress { get; set; } = new();
    public List<string> CulturalMilestones { get; set; } = new();
}

/// <summary>
/// Language revenue analysis request
/// </summary>
public class LanguageRevenueAnalysisRequest
{
    public List<SouthAsianLanguage> TargetLanguages { get; set; } = new();
    public List<CulturalRegion> TargetRegions { get; set; } = new();
    public TimeSpan AnalysisPeriod { get; set; }
    public List<ContentType> ContentTypes { get; set; } = new();
}

/// <summary>
/// Language revenue analysis result
/// </summary>
public class LanguageRevenueAnalysisResult
{
    public decimal EngagementIncrease { get; set; }
    public decimal RevenueMultiplier { get; set; }
    public Dictionary<SouthAsianLanguage, decimal> LanguageRevenueImpact { get; set; } = new();
    public List<string> NewRevenueStreams { get; set; } = new();
    public decimal ProjectedROI { get; set; }
}

/// <summary>
/// Business language matching request
/// </summary>
public class BusinessLanguageMatchingRequest
{
    public Guid UserId { get; set; }
    public List<SouthAsianLanguage> UserLanguages { get; set; } = new();
    public CulturalBackground UserCulturalBackground { get; set; }
    public string BusinessCategory { get; set; } = string.Empty;
    public CulturalRegion UserRegion { get; set; }
}

/// <summary>
/// Business language matching result
/// </summary>
public class BusinessLanguageMatchingResult
{
    public decimal LanguageMatchScore { get; set; }
    public decimal CulturalRelevanceScore { get; set; }
    public decimal ConversionProbability { get; set; }
    public List<BusinessDirectoryEntry> RecommendedBusinesses { get; set; } = new();
}

/// <summary>
/// Business directory entry
/// </summary>
public class BusinessDirectoryEntry
{
    public Guid BusinessId { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<SouthAsianLanguage> SupportedLanguages { get; set; } = new();
    public decimal LanguageMatchScore { get; set; }
    public string Location { get; set; } = string.Empty;
    public CulturalBackground PrimaryCulturalAffiliation { get; set; }
}

/// <summary>
/// Premium content strategy
/// </summary>
public class PremiumContentStrategy
{
    public List<SouthAsianLanguage> TargetLanguages { get; set; } = new();
    public List<ContentType> ContentTypes { get; set; } = new();
    public decimal ExpectedRevenueIncrease { get; set; }
    public decimal EstimatedDevelopmentCost { get; set; }
    public decimal ProjectedROI { get; set; }
    public List<string> ContentRecommendations { get; set; } = new();
}

/// <summary>
/// Cultural event monetization strategy
/// </summary>
public class CulturalEventMonetizationStrategy
{
    public List<CulturalEvent> CulturalEvents { get; set; } = new();
    public List<LanguageServiceType> ServiceTypes { get; set; } = new();
    public Dictionary<CulturalEvent, decimal> EventRevenueProjections { get; set; } = new();
    public List<string> MonetizationOpportunities { get; set; } = new();
}