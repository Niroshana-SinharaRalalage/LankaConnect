using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Enterprise.ValueObjects;

public class EnterprisePerformanceReport : ValueObject
{
    public EnterpriseClientId ClientId { get; private set; }
    public DateTime ReportGeneratedAt { get; private set; }
    public DateTime ReportPeriodStart { get; private set; }
    public DateTime ReportPeriodEnd { get; private set; }
    public SLAPerformanceMetrics OverallSLAMetrics { get; private set; }
    public IReadOnlyList<SLAPerformanceMetrics> DailySLAMetrics { get; private set; }
    public double AvailabilityPercentage { get; private set; }
    public double CustomerSatisfactionScore { get; private set; }
    public int TotalRequests { get; private set; }
    public int SuccessfulRequests { get; private set; }
    public int FailedRequests { get; private set; }
    public double SuccessRate => TotalRequests > 0 ? (double)SuccessfulRequests / TotalRequests * 100 : 0;
    public string PerformanceGrade { get; private set; }
    public IReadOnlyList<string> Recommendations { get; private set; }
    public CulturalIntelligenceInsights? CulturalPerformanceInsights { get; private set; }

    private EnterprisePerformanceReport(
        EnterpriseClientId clientId,
        DateTime reportGeneratedAt,
        DateTime reportPeriodStart,
        DateTime reportPeriodEnd,
        SLAPerformanceMetrics overallSLAMetrics,
        IReadOnlyList<SLAPerformanceMetrics> dailySLAMetrics,
        double availabilityPercentage,
        double customerSatisfactionScore,
        int totalRequests,
        int successfulRequests,
        int failedRequests,
        string performanceGrade,
        IReadOnlyList<string> recommendations,
        CulturalIntelligenceInsights? culturalPerformanceInsights = null)
    {
        ClientId = clientId;
        ReportGeneratedAt = reportGeneratedAt;
        ReportPeriodStart = reportPeriodStart;
        ReportPeriodEnd = reportPeriodEnd;
        OverallSLAMetrics = overallSLAMetrics;
        DailySLAMetrics = dailySLAMetrics;
        AvailabilityPercentage = availabilityPercentage;
        CustomerSatisfactionScore = customerSatisfactionScore;
        TotalRequests = totalRequests;
        SuccessfulRequests = successfulRequests;
        FailedRequests = failedRequests;
        PerformanceGrade = performanceGrade;
        Recommendations = recommendations;
        CulturalPerformanceInsights = culturalPerformanceInsights;
    }

    public static EnterprisePerformanceReport Create(
        EnterpriseClientId clientId,
        DateTime reportGeneratedAt,
        DateTime reportPeriodStart,
        DateTime reportPeriodEnd,
        SLAPerformanceMetrics overallSLAMetrics,
        IEnumerable<SLAPerformanceMetrics> dailySLAMetrics,
        double availabilityPercentage,
        double customerSatisfactionScore,
        int totalRequests,
        int successfulRequests,
        int failedRequests,
        string performanceGrade,
        IEnumerable<string> recommendations,
        CulturalIntelligenceInsights? culturalPerformanceInsights = null)
    {
        if (clientId == null) throw new ArgumentNullException(nameof(clientId));
        if (reportGeneratedAt > DateTime.UtcNow) throw new ArgumentException("Report generated at cannot be in the future", nameof(reportGeneratedAt));
        if (reportPeriodStart >= reportPeriodEnd) throw new ArgumentException("Report period start must be before end", nameof(reportPeriodStart));
        if (overallSLAMetrics == null) throw new ArgumentNullException(nameof(overallSLAMetrics));
        if (availabilityPercentage < 0 || availabilityPercentage > 100) throw new ArgumentException("Availability percentage must be between 0 and 100", nameof(availabilityPercentage));
        if (customerSatisfactionScore < 0 || customerSatisfactionScore > 10) throw new ArgumentException("Customer satisfaction score must be between 0 and 10", nameof(customerSatisfactionScore));
        if (totalRequests < 0) throw new ArgumentException("Total requests cannot be negative", nameof(totalRequests));
        if (successfulRequests < 0) throw new ArgumentException("Successful requests cannot be negative", nameof(successfulRequests));
        if (failedRequests < 0) throw new ArgumentException("Failed requests cannot be negative", nameof(failedRequests));
        if (successfulRequests + failedRequests != totalRequests) throw new ArgumentException("Successful + failed requests must equal total requests");
        if (string.IsNullOrWhiteSpace(performanceGrade)) throw new ArgumentException("Performance grade is required", nameof(performanceGrade));

        var dailyMetricsList = dailySLAMetrics?.ToList() ?? throw new ArgumentNullException(nameof(dailySLAMetrics));
        var recommendationsList = recommendations?.ToList() ?? throw new ArgumentNullException(nameof(recommendations));

        return new EnterprisePerformanceReport(
            clientId,
            reportGeneratedAt,
            reportPeriodStart,
            reportPeriodEnd,
            overallSLAMetrics,
            dailyMetricsList.AsReadOnly(),
            availabilityPercentage,
            customerSatisfactionScore,
            totalRequests,
            successfulRequests,
            failedRequests,
            performanceGrade,
            recommendationsList.AsReadOnly(),
            culturalPerformanceInsights);
    }

    public bool MeetsSLA() => OverallSLAMetrics.SLACompliant && AvailabilityPercentage >= 99.9;

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return ClientId;
        yield return ReportGeneratedAt;
        yield return ReportPeriodStart;
        yield return ReportPeriodEnd;
        yield return OverallSLAMetrics;
        yield return AvailabilityPercentage;
        yield return CustomerSatisfactionScore;
        yield return TotalRequests;
        yield return SuccessfulRequests;
        yield return FailedRequests;
        yield return PerformanceGrade;
        yield return CulturalPerformanceInsights ?? new object();
        
        foreach (var dailyMetric in DailySLAMetrics)
            yield return dailyMetric;
        
        foreach (var recommendation in Recommendations)
            yield return recommendation;
    }
}