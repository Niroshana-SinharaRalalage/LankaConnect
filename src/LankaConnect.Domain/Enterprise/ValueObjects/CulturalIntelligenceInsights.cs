using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Domain.Enterprise.ValueObjects;

public class CulturalIntelligenceInsights : ValueObject
{
    public IReadOnlyDictionary<GeographicRegion, double> RegionalEngagement { get; private set; }
    public IReadOnlyDictionary<CulturalEventType, int> EventPopularity { get; private set; }
    public IReadOnlyList<string> CulturalTrends { get; private set; }
    public IReadOnlyList<string> RecommendedActions { get; private set; }
    public double CulturalSensitivityScore { get; private set; }
    public string PreferredCommunicationStyle { get; private set; }
    public DateTime AnalysisDate { get; private set; }
    public TimeSpan AnalysisPeriod { get; private set; }
    public int TotalCulturalInteractions { get; private set; }
    public double CulturalAdaptationRate { get; private set; }

    private CulturalIntelligenceInsights(
        IReadOnlyDictionary<GeographicRegion, double> regionalEngagement,
        IReadOnlyDictionary<CulturalEventType, int> eventPopularity,
        IReadOnlyList<string> culturalTrends,
        IReadOnlyList<string> recommendedActions,
        double culturalSensitivityScore,
        string preferredCommunicationStyle,
        DateTime analysisDate,
        TimeSpan analysisPeriod,
        int totalCulturalInteractions,
        double culturalAdaptationRate)
    {
        RegionalEngagement = regionalEngagement;
        EventPopularity = eventPopularity;
        CulturalTrends = culturalTrends;
        RecommendedActions = recommendedActions;
        CulturalSensitivityScore = culturalSensitivityScore;
        PreferredCommunicationStyle = preferredCommunicationStyle;
        AnalysisDate = analysisDate;
        AnalysisPeriod = analysisPeriod;
        TotalCulturalInteractions = totalCulturalInteractions;
        CulturalAdaptationRate = culturalAdaptationRate;
    }

    public static CulturalIntelligenceInsights Create(
        IReadOnlyDictionary<GeographicRegion, double> regionalEngagement,
        IReadOnlyDictionary<CulturalEventType, int> eventPopularity,
        IEnumerable<string> culturalTrends,
        IEnumerable<string> recommendedActions,
        double culturalSensitivityScore,
        string preferredCommunicationStyle,
        DateTime analysisDate,
        TimeSpan analysisPeriod,
        int totalCulturalInteractions,
        double culturalAdaptationRate)
    {
        if (regionalEngagement == null || !regionalEngagement.Any()) 
            throw new ArgumentException("Regional engagement data is required", nameof(regionalEngagement));
        if (eventPopularity == null || !eventPopularity.Any()) 
            throw new ArgumentException("Event popularity data is required", nameof(eventPopularity));
        if (culturalSensitivityScore < 0 || culturalSensitivityScore > 1) 
            throw new ArgumentException("Cultural sensitivity score must be between 0 and 1", nameof(culturalSensitivityScore));
        if (string.IsNullOrWhiteSpace(preferredCommunicationStyle)) 
            throw new ArgumentException("Preferred communication style is required", nameof(preferredCommunicationStyle));
        if (analysisDate > DateTime.UtcNow) 
            throw new ArgumentException("Analysis date cannot be in the future", nameof(analysisDate));
        if (analysisPeriod <= TimeSpan.Zero) 
            throw new ArgumentException("Analysis period must be positive", nameof(analysisPeriod));
        if (totalCulturalInteractions < 0) 
            throw new ArgumentException("Total cultural interactions cannot be negative", nameof(totalCulturalInteractions));
        if (culturalAdaptationRate < 0 || culturalAdaptationRate > 1) 
            throw new ArgumentException("Cultural adaptation rate must be between 0 and 1", nameof(culturalAdaptationRate));

        var trendsList = culturalTrends?.ToList() ?? throw new ArgumentNullException(nameof(culturalTrends));
        var actionsList = recommendedActions?.ToList() ?? throw new ArgumentNullException(nameof(recommendedActions));

        return new CulturalIntelligenceInsights(
            regionalEngagement,
            eventPopularity,
            trendsList.AsReadOnly(),
            actionsList.AsReadOnly(),
            culturalSensitivityScore,
            preferredCommunicationStyle,
            analysisDate,
            analysisPeriod,
            totalCulturalInteractions,
            culturalAdaptationRate);
    }

    public GeographicRegion GetMostEngagedRegion()
    {
        return RegionalEngagement.OrderByDescending(x => x.Value).FirstOrDefault().Key;
    }

    public CulturalEventType GetMostPopularEventType()
    {
        return EventPopularity.OrderByDescending(x => x.Value).FirstOrDefault().Key;
    }

    public bool HasHighCulturalSensitivity() => CulturalSensitivityScore >= 0.8;

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return CulturalSensitivityScore;
        yield return PreferredCommunicationStyle;
        yield return AnalysisDate;
        yield return AnalysisPeriod;
        yield return TotalCulturalInteractions;
        yield return CulturalAdaptationRate;
        
        foreach (var engagement in RegionalEngagement.OrderBy(x => x.Key))
        {
            yield return engagement.Key;
            yield return engagement.Value;
        }
        
        foreach (var popularity in EventPopularity.OrderBy(x => x.Key))
        {
            yield return popularity.Key;
            yield return popularity.Value;
        }
        
        foreach (var trend in CulturalTrends)
            yield return trend;
        
        foreach (var action in RecommendedActions)
            yield return action;
    }
}