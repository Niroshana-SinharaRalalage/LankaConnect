using System;
using System.Collections.Generic;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Domain.Shared;

// GenerationalCohort and SacredContentType moved to CulturalPriorityTypes.cs for comprehensive implementation


// Note: CulturalContext is defined as CulturalContextType in LankaConnect.Domain.Common.Database.MultiLanguageRoutingModels

/// <summary>
/// Cultural event enumeration for event-driven language routing
/// </summary>
public enum CulturalEvent
{
    Vesak,
    Diwali,
    Eid,
    Thaipusam,
    Vaisakhi,
    BuddhistNewYear,
    TamilNewYear,
    ChineseNewYear,
    CulturalHeritage,
    MultiCultural
}

// W2C.4 (2026-06-05): the orphan `CulturalBackground` enum that lived here was
// deleted. Zero production callers used these values (verified via grep). The
// canonical CulturalBackground lives in LankaConnect.SharedKernel.Cultural —
// see cultural-type-inventory.md §B.5 + ADR-008.

/// <summary>
/// Cultural event intensity classification
/// </summary>
public enum CulturalEventIntensity
{
    Minor,
    Moderate,
    Major,
    Critical
}

/// <summary>
/// System health status enumeration
/// </summary>
public enum SystemHealthStatus
{
    Healthy,
    Warning,
    Critical,
    Degraded,
    Offline
}

/// <summary>
/// Cultural region classification for geographic routing
/// </summary>
public enum CulturalRegion
{
    NorthAmerica,
    Europe,
    Australia,
    MiddleEast,
    SouthAsia,
    Other
}

/// <summary>
/// Language fallback strategy enumeration
/// </summary>
public enum LanguageFallbackStrategy
{
    DefaultToEnglish,
    PreferHeritage,
    UserPreference,
    CommunityBased
}

/// <summary>
/// Script complexity classification for rendering requirements
/// </summary>
public enum ScriptComplexity
{
    Low,
    Medium,
    High,
    VeryHigh
}

/// <summary>
/// Database failover mode enumeration
/// </summary>
public enum DatabaseFailoverMode
{
    LocalCache,
    ReadReplica,
    CrossRegion,
    EmergencyMode
}

/// <summary>
/// Language bridging strategy for intergenerational content
/// </summary>
public enum LanguageBridgingStrategy
{
    GradualTransition,
    BilingualPresentation,
    LearningOpportunity,
    CulturalBridge
}

/// <summary>
/// Language proficiency level classification
/// </summary>
public enum LanguageProficiencyLevel
{
    Beginner,
    Elementary,
    Intermediate,
    Advanced,
    Native
}


/// <summary>
/// Language service type enumeration
/// </summary>
public enum LanguageServiceType
{
    Translation,
    Interpretation,
    ContentCreation,
    CulturalConsultation,
    LanguageLearning
}

/// <summary>
/// Cache invalidation strategy enumeration
/// </summary>
public enum CacheInvalidationStrategy
{
    Immediate,
    Lazy,
    Scheduled,
    EventDriven
}