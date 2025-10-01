using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Enterprise.ValueObjects;

/// <summary>
/// Usage limits and capacity constraints for enterprise clients
/// Defines request limits, cultural intelligence processing quotas, and premium feature usage
/// </summary>
public class EnterpriseUsageLimits : ValueObject
{
    public int MonthlyAPIRequests { get; }
    public int ConcurrentRequests { get; }
    public int BuddhistCalendarRequestsPerMonth { get; }
    public int CulturalAppropriatenessScansPerMonth { get; }
    public int DiasporaAnalyticsReportsPerMonth { get; }
    public int CustomAIModelTrainingHours { get; }
    public int ConsultingHoursIncluded { get; }
    public bool UnlimitedBasicFeatures { get; }
    public int DataRetentionMonths { get; }
    public int WhiteLabelDeployments { get; }

    public EnterpriseUsageLimits(
        int monthlyAPIRequests,
        int concurrentRequests,
        int buddhistCalendarRequestsPerMonth = 0,
        int culturalAppropriatenessScansPerMonth = 0,
        int diasporaAnalyticsReportsPerMonth = 0,
        int customAIModelTrainingHours = 0,
        int consultingHoursIncluded = 0,
        bool unlimitedBasicFeatures = false,
        int dataRetentionMonths = 12,
        int whiteLabelDeployments = 0)
    {
        if (monthlyAPIRequests <= 0)
            throw new ArgumentException("Monthly API requests must be positive.", nameof(monthlyAPIRequests));
            
        if (concurrentRequests <= 0)
            throw new ArgumentException("Concurrent requests must be positive.", nameof(concurrentRequests));
            
        if (dataRetentionMonths <= 0)
            throw new ArgumentException("Data retention must be positive.", nameof(dataRetentionMonths));

        MonthlyAPIRequests = monthlyAPIRequests;
        ConcurrentRequests = concurrentRequests;
        BuddhistCalendarRequestsPerMonth = Math.Max(0, buddhistCalendarRequestsPerMonth);
        CulturalAppropriatenessScansPerMonth = Math.Max(0, culturalAppropriatenessScansPerMonth);
        DiasporaAnalyticsReportsPerMonth = Math.Max(0, diasporaAnalyticsReportsPerMonth);
        CustomAIModelTrainingHours = Math.Max(0, customAIModelTrainingHours);
        ConsultingHoursIncluded = Math.Max(0, consultingHoursIncluded);
        UnlimitedBasicFeatures = unlimitedBasicFeatures;
        DataRetentionMonths = dataRetentionMonths;
        WhiteLabelDeployments = Math.Max(0, whiteLabelDeployments);
    }

    /// <summary>
    /// Creates Fortune 500 enterprise usage limits with unlimited core features
    /// </summary>
    public static EnterpriseUsageLimits CreateFortune500Limits()
        => new(
            monthlyAPIRequests: int.MaxValue, // Unlimited
            concurrentRequests: 10000,
            buddhistCalendarRequestsPerMonth: int.MaxValue,
            culturalAppropriatenessScansPerMonth: int.MaxValue,
            diasporaAnalyticsReportsPerMonth: 500,
            customAIModelTrainingHours: 200,
            consultingHoursIncluded: 100,
            unlimitedBasicFeatures: true,
            dataRetentionMonths: 84, // 7 years
            whiteLabelDeployments: 10);

    /// <summary>
    /// Creates educational institution usage limits with academic-focused quotas
    /// </summary>
    public static EnterpriseUsageLimits CreateEducationalLimits()
        => new(
            monthlyAPIRequests: 1000000,
            concurrentRequests: 5000,
            buddhistCalendarRequestsPerMonth: 50000,
            culturalAppropriatenessScansPerMonth: 25000,
            diasporaAnalyticsReportsPerMonth: 100,
            customAIModelTrainingHours: 0,
            consultingHoursIncluded: 50,
            unlimitedBasicFeatures: true,
            dataRetentionMonths: 60, // 5 years
            whiteLabelDeployments: 3);

    /// <summary>
    /// Creates government agency usage limits with civic-focused quotas
    /// </summary>
    public static EnterpriseUsageLimits CreateGovernmentLimits()
        => new(
            monthlyAPIRequests: 5000000,
            concurrentRequests: 7500,
            buddhistCalendarRequestsPerMonth: 100000,
            culturalAppropriatenessScansPerMonth: 50000,
            diasporaAnalyticsReportsPerMonth: 200,
            customAIModelTrainingHours: 0,
            consultingHoursIncluded: 75,
            unlimitedBasicFeatures: true,
            dataRetentionMonths: 120, // 10 years
            whiteLabelDeployments: 5);

    /// <summary>
    /// Creates mid-market enterprise usage limits with balanced quotas
    /// </summary>
    public static EnterpriseUsageLimits CreateMidMarketLimits()
        => new(
            monthlyAPIRequests: 500000,
            concurrentRequests: 2000,
            buddhistCalendarRequestsPerMonth: 25000,
            culturalAppropriatenessScansPerMonth: 10000,
            diasporaAnalyticsReportsPerMonth: 50,
            customAIModelTrainingHours: 20,
            consultingHoursIncluded: 25,
            unlimitedBasicFeatures: false,
            dataRetentionMonths: 36, // 3 years
            whiteLabelDeployments: 1);

    public bool IsUnlimited => MonthlyAPIRequests == int.MaxValue;
    public bool HasPremiumCalendarAccess => BuddhistCalendarRequestsPerMonth > 10000;
    public bool HasEnterpriseAnalytics => DiasporaAnalyticsReportsPerMonth > 100;
    public bool IncludesConsulting => ConsultingHoursIncluded > 0;
    public bool SupportsWhiteLabel => WhiteLabelDeployments > 0;
    public bool HasExtendedDataRetention => DataRetentionMonths > 24;

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return MonthlyAPIRequests;
        yield return ConcurrentRequests;
        yield return BuddhistCalendarRequestsPerMonth;
        yield return CulturalAppropriatenessScansPerMonth;
        yield return DiasporaAnalyticsReportsPerMonth;
        yield return CustomAIModelTrainingHours;
        yield return ConsultingHoursIncluded;
        yield return UnlimitedBasicFeatures;
        yield return DataRetentionMonths;
        yield return WhiteLabelDeployments;
    }
}