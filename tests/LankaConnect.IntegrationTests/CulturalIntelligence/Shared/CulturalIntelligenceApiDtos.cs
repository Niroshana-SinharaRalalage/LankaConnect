namespace LankaConnect.IntegrationTests.CulturalIntelligence.Shared;

/// <summary>
/// Shared DTOs for Cultural Intelligence API Integration Tests
/// These represent the API response contracts for all Cultural Intelligence endpoints
/// </summary>

#region Common DTOs

public class CulturalIntelligenceMetadata
{
    public string Version { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public int ProcessingTimeMs { get; set; }
}

public class GeographicScoreDto
{
    public double ProximityScore { get; set; }
    public double CommunityDensityScore { get; set; }
    public double AccessibilityScore { get; set; }
}

public class CalendarValidationDto
{
    public bool IsPoyadayConflict { get; set; }
    public bool IsFestivalConflict { get; set; }
    public List<string> ConflictDetails { get; set; } = new();
    public string Recommendation { get; set; } = string.Empty;
}

public class FestivalPeriodDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<DateTime> PoyadayDates { get; set; } = new();
    public string FestivalType { get; set; } = string.Empty;
}

public class CulturalAlignmentDto
{
    public bool IsAppropriateForBuddhist { get; set; }
    public bool IsAppropriateForHindu { get; set; }
    public double AppropriatenessScore { get; set; }
}

public class CommunityOptimizedEventDto
{
    public Guid EventId { get; set; }
    public string Title { get; set; } = string.Empty;
    public double DiasporaFriendlinessScore { get; set; }
    public double CommunityRelevanceScore { get; set; }
}

public class CommunityClusterAnalysisDto
{
    public double SriLankanDensity { get; set; }
    public int EstimatedCommunitySize { get; set; }
    public List<string> PredominantNeighborhoods { get; set; } = new();
}

public class CulturalSignificanceDto
{
    public string Description { get; set; } = string.Empty;
    public string ReligiousImportance { get; set; } = string.Empty;
    public List<string> TraditionalObservances { get; set; } = new();
}

public class RecommendedTimingDto
{
    public DateTime OptimalStartTime { get; set; }
    public DateTime OptimalEndTime { get; set; }
    public string Reasoning { get; set; } = string.Empty;
}

#endregion

#region Communication DTOs

public class PoyadayConflictAnalysisDto
{
    public bool HasConflicts { get; set; }
    public List<DateTime> ConflictingPoyadays { get; set; } = new();
    public List<string> AlternativeTimes { get; set; } = new();
}

public class EmailTemplateDto
{
    public Guid TemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> SupportedLanguages { get; set; } = new();
}

public class OptimizationMetricsDto
{
    public double CulturalSensitivityScore { get; set; }
    public double LanguageOptimizationScore { get; set; }
    public double TimingOptimizationScore { get; set; }
    public Dictionary<string, double> SegmentEffectiveness { get; set; } = new();
}

#endregion

#region Diaspora DTOs

public class DemographicsDto
{
    public int TotalPopulation { get; set; }
    public double AverageAge { get; set; }
    public Dictionary<string, double> EducationLevels { get; set; } = new();
    public Dictionary<string, double> IncomeDistribution { get; set; } = new();
}

public class LanguageRetentionDto
{
    public double SinhalaRetention { get; set; }
    public double TamilRetention { get; set; }
    public double EnglishProficiency { get; set; }
    public Dictionary<string, double> GenerationalDifferences { get; set; } = new();
}

public class EventPreferencesDto
{
    public double ReligiousEventImportance { get; set; }
    public double CulturalEventImportance { get; set; }
    public List<string> PreferredFestivals { get; set; } = new();
    public Dictionary<string, double> EventTypePreferences { get; set; } = new();
}

public class CommunicationPreferencesDto
{
    public List<string> PreferredLanguages { get; set; } = new();
    public string PreferredContactTime { get; set; } = string.Empty;
    public bool PrefersCulturallyAdaptedContent { get; set; }
}

public class CulturalRetentionDto
{
    public double OverallRetentionScore { get; set; }
    public Dictionary<string, double> CategoryScores { get; set; } = new();
    public List<string> StrongRetentionAreas { get; set; } = new();
    public List<string> WeakRetentionAreas { get; set; } = new();
}

public class IntegrationPatternsDto
{
    public bool BilingualPreference { get; set; }
    public double ModernTraditionalBalance { get; set; }
    public string IntegrationLevel { get; set; } = string.Empty;
}

public class OptimalStrategyDto
{
    public string OverallApproach { get; set; } = string.Empty;
    public List<string> KeyPrinciples { get; set; } = new();
    public Dictionary<string, string> ChannelStrategies { get; set; } = new();
}

public class CommunityTacticDto
{
    public string Location { get; set; } = string.Empty;
    public string CulturalApproach { get; set; } = string.Empty;
    public string LanguageStrategy { get; set; } = string.Empty;
    public List<string> RecommendedChannels { get; set; } = new();
}

public class CulturalRetentionTrendsDto
{
    public double BuddhistObservance { get; set; }
    public double HinduObservance { get; set; }
    public double TraditionalFoodPreferences { get; set; }
    public Dictionary<string, double> TrendsByGeneration { get; set; } = new();
}

public class LanguagePatternsDto
{
    public Dictionary<string, double> LanguageUsageByGeneration { get; set; } = new();
    public Dictionary<string, double> LanguageProficiencyScores { get; set; } = new();
    public List<string> EmergingPatterns { get; set; } = new();
}

public class PopulationProjectionDto
{
    public int Year { get; set; }
    public int ProjectedPopulation { get; set; }
    public Dictionary<string, int> AgeGroupBreakdown { get; set; } = new();
    public List<string> KeyAssumptions { get; set; } = new();
}

public class CulturalEvolutionDto
{
    public Dictionary<string, double> ProjectedRetentionScores { get; set; } = new();
    public List<string> EmergingTrends { get; set; } = new();
    public string OverallDirection { get; set; } = string.Empty;
}

public class IntegrationPatternsDetailDto
{
    public List<GenerationalProgressionDto> GenerationalProgression { get; set; } = new();
    public string OverallIntegrationLevel { get; set; } = string.Empty;
    public List<string> IntegrationChallenges { get; set; } = new();
}

public class GenerationalProgressionDto
{
    public string Generation { get; set; } = string.Empty;
    public double IntegrationScore { get; set; }
    public Dictionary<string, double> Metrics { get; set; } = new();
}

public class LanguageShiftAnalysisDto
{
    public List<GenerationalLanguageDataDto> GenerationalData { get; set; } = new();
    public string OverallTrend { get; set; } = string.Empty;
    public double ShiftRate { get; set; }
}

public class GenerationalLanguageDataDto
{
    public string Generation { get; set; } = string.Empty;
    public double NativeLanguageRetention { get; set; }
    public double EnglishProficiency { get; set; }
    public bool IsBilingual { get; set; }
}

public class CulturalRetentionAnalysisDto
{
    public Dictionary<string, double> RetentionByCategory { get; set; } = new();
    public List<string> StrongestRetentionAreas { get; set; } = new();
    public List<string> WeakestRetentionAreas { get; set; } = new();
    public Dictionary<string, string> RetentionFactors { get; set; } = new();
}

#endregion