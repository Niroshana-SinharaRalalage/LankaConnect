using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Enterprise.ValueObjects;

/// <summary>
/// Cultural Intelligence features available to enterprise clients
/// Defines access levels for Buddhist/Hindu calendar, cultural appropriateness scoring,
/// diaspora analytics, and custom AI model development
/// </summary>
public class CulturalIntelligenceFeatures : ValueObject
{
    public bool BuddhistCalendarPremium { get; }
    public bool HinduCalendarPremium { get; }
    public bool CulturalAppropriatenessScoring { get; }
    public bool DiasporaAnalyticsEnterprise { get; }
    public bool CustomAIModelDevelopment { get; }
    public bool WhiteLabelLicensing { get; }
    public bool CulturalConsultingServices { get; }
    public bool RealTimeCulturalModeration { get; }
    public bool MultiLanguageCulturalSupport { get; }
    public bool CulturalEventPlanningAI { get; }
    public bool CommunityEngagementAnalytics { get; }
    public bool CulturalTrendPrediction { get; }

    public CulturalIntelligenceFeatures(
        bool buddhistCalendarPremium = false,
        bool hinduCalendarPremium = false,
        bool culturalAppropriatenessScoring = false,
        bool diasporaAnalyticsEnterprise = false,
        bool customAIModelDevelopment = false,
        bool whiteLabelLicensing = false,
        bool culturalConsultingServices = false,
        bool realTimeCulturalModeration = false,
        bool multiLanguageCulturalSupport = false,
        bool culturalEventPlanningAI = false,
        bool communityEngagementAnalytics = false,
        bool culturalTrendPrediction = false)
    {
        BuddhistCalendarPremium = buddhistCalendarPremium;
        HinduCalendarPremium = hinduCalendarPremium;
        CulturalAppropriatenessScoring = culturalAppropriatenessScoring;
        DiasporaAnalyticsEnterprise = diasporaAnalyticsEnterprise;
        CustomAIModelDevelopment = customAIModelDevelopment;
        WhiteLabelLicensing = whiteLabelLicensing;
        CulturalConsultingServices = culturalConsultingServices;
        RealTimeCulturalModeration = realTimeCulturalModeration;
        MultiLanguageCulturalSupport = multiLanguageCulturalSupport;
        CulturalEventPlanningAI = culturalEventPlanningAI;
        CommunityEngagementAnalytics = communityEngagementAnalytics;
        CulturalTrendPrediction = culturalTrendPrediction;
    }

    /// <summary>
    /// Creates Fortune 500 enterprise feature set with full cultural intelligence capabilities
    /// </summary>
    public static CulturalIntelligenceFeatures CreateFortune500FeatureSet()
        => new(
            buddhistCalendarPremium: true,
            hinduCalendarPremium: true,
            culturalAppropriatenessScoring: true,
            diasporaAnalyticsEnterprise: true,
            customAIModelDevelopment: true,
            whiteLabelLicensing: true,
            culturalConsultingServices: true,
            realTimeCulturalModeration: true,
            multiLanguageCulturalSupport: true,
            culturalEventPlanningAI: true,
            communityEngagementAnalytics: true,
            culturalTrendPrediction: true);

    /// <summary>
    /// Creates educational institution feature set with academic-focused cultural intelligence
    /// </summary>
    public static CulturalIntelligenceFeatures CreateEducationalInstitutionFeatureSet()
        => new(
            buddhistCalendarPremium: true,
            hinduCalendarPremium: true,
            culturalAppropriatenessScoring: true,
            diasporaAnalyticsEnterprise: false,
            customAIModelDevelopment: false,
            whiteLabelLicensing: false,
            culturalConsultingServices: true,
            realTimeCulturalModeration: true,
            multiLanguageCulturalSupport: true,
            culturalEventPlanningAI: true,
            communityEngagementAnalytics: true,
            culturalTrendPrediction: false);

    /// <summary>
    /// Creates government agency feature set with civic-focused cultural analytics
    /// </summary>
    public static CulturalIntelligenceFeatures CreateGovernmentFeatureSet()
        => new(
            buddhistCalendarPremium: true,
            hinduCalendarPremium: true,
            culturalAppropriatenessScoring: true,
            diasporaAnalyticsEnterprise: true,
            customAIModelDevelopment: false,
            whiteLabelLicensing: false,
            culturalConsultingServices: true,
            realTimeCulturalModeration: false,
            multiLanguageCulturalSupport: true,
            culturalEventPlanningAI: true,
            communityEngagementAnalytics: true,
            culturalTrendPrediction: true);

    public bool IsEnterpriseTier => CustomAIModelDevelopment && WhiteLabelLicensing && CulturalConsultingServices;
    public bool HasAdvancedAnalytics => DiasporaAnalyticsEnterprise && CommunityEngagementAnalytics && CulturalTrendPrediction;
    public bool HasFullCalendarAccess => BuddhistCalendarPremium && HinduCalendarPremium;
    public int FeatureCount => new[] { BuddhistCalendarPremium, HinduCalendarPremium, CulturalAppropriatenessScoring, 
        DiasporaAnalyticsEnterprise, CustomAIModelDevelopment, WhiteLabelLicensing, CulturalConsultingServices,
        RealTimeCulturalModeration, MultiLanguageCulturalSupport, CulturalEventPlanningAI, 
        CommunityEngagementAnalytics, CulturalTrendPrediction }.Count(f => f);

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return BuddhistCalendarPremium;
        yield return HinduCalendarPremium;
        yield return CulturalAppropriatenessScoring;
        yield return DiasporaAnalyticsEnterprise;
        yield return CustomAIModelDevelopment;
        yield return WhiteLabelLicensing;
        yield return CulturalConsultingServices;
        yield return RealTimeCulturalModeration;
        yield return MultiLanguageCulturalSupport;
        yield return CulturalEventPlanningAI;
        yield return CommunityEngagementAnalytics;
        yield return CulturalTrendPrediction;
    }
}