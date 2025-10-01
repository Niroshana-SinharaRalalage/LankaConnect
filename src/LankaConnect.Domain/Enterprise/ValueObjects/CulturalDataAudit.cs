using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Domain.Enterprise.ValueObjects;

public class CulturalDataAudit : ValueObject
{
    public string AuditId { get; private set; }
    public EnterpriseClientId ClientId { get; private set; }
    public DateTime AuditDate { get; private set; }
    public IReadOnlyDictionary<GeographicRegion, int> DataDistribution { get; private set; }
    public IReadOnlyDictionary<CulturalEventType, int> EventDataCounts { get; private set; }
    public IReadOnlyList<string> CulturalDataCategories { get; private set; }
    public IReadOnlyList<string> DataQualityIssues { get; private set; }
    public IReadOnlyList<string> CulturalSensitivityConcerns { get; private set; }
    public double DataAccuracyScore { get; private set; }
    public double CulturalSensitivityScore { get; private set; }
    public double OverallAuditScore { get; private set; }
    public string AuditStatus { get; private set; }
    public IReadOnlyList<string> Recommendations { get; private set; }
    public DateTime NextAuditDate { get; private set; }
    public IReadOnlyDictionary<string, object> AuditMetrics { get; private set; }
    public bool RequiresImmediateAction => OverallAuditScore < 0.7 || CulturalSensitivityConcerns.Any();

    private CulturalDataAudit(
        string auditId,
        EnterpriseClientId clientId,
        DateTime auditDate,
        IReadOnlyDictionary<GeographicRegion, int> dataDistribution,
        IReadOnlyDictionary<CulturalEventType, int> eventDataCounts,
        IReadOnlyList<string> culturalDataCategories,
        IReadOnlyList<string> dataQualityIssues,
        IReadOnlyList<string> culturalSensitivityConcerns,
        double dataAccuracyScore,
        double culturalSensitivityScore,
        double overallAuditScore,
        string auditStatus,
        IReadOnlyList<string> recommendations,
        DateTime nextAuditDate,
        IReadOnlyDictionary<string, object> auditMetrics)
    {
        AuditId = auditId;
        ClientId = clientId;
        AuditDate = auditDate;
        DataDistribution = dataDistribution;
        EventDataCounts = eventDataCounts;
        CulturalDataCategories = culturalDataCategories;
        DataQualityIssues = dataQualityIssues;
        CulturalSensitivityConcerns = culturalSensitivityConcerns;
        DataAccuracyScore = dataAccuracyScore;
        CulturalSensitivityScore = culturalSensitivityScore;
        OverallAuditScore = overallAuditScore;
        AuditStatus = auditStatus;
        Recommendations = recommendations;
        NextAuditDate = nextAuditDate;
        AuditMetrics = auditMetrics;
    }

    public static CulturalDataAudit Create(
        string auditId,
        EnterpriseClientId clientId,
        DateTime auditDate,
        IReadOnlyDictionary<GeographicRegion, int> dataDistribution,
        IReadOnlyDictionary<CulturalEventType, int> eventDataCounts,
        IEnumerable<string> culturalDataCategories,
        IEnumerable<string> dataQualityIssues,
        IEnumerable<string> culturalSensitivityConcerns,
        double dataAccuracyScore,
        double culturalSensitivityScore,
        double overallAuditScore,
        string auditStatus,
        IEnumerable<string> recommendations,
        DateTime nextAuditDate,
        IReadOnlyDictionary<string, object>? auditMetrics = null)
    {
        if (string.IsNullOrWhiteSpace(auditId)) throw new ArgumentException("Audit ID is required", nameof(auditId));
        if (clientId == null) throw new ArgumentNullException(nameof(clientId));
        if (auditDate > DateTime.UtcNow) throw new ArgumentException("Audit date cannot be in the future", nameof(auditDate));
        if (dataDistribution == null || !dataDistribution.Any()) throw new ArgumentException("Data distribution is required", nameof(dataDistribution));
        if (eventDataCounts == null || !eventDataCounts.Any()) throw new ArgumentException("Event data counts are required", nameof(eventDataCounts));
        if (dataAccuracyScore < 0 || dataAccuracyScore > 1) throw new ArgumentException("Data accuracy score must be between 0 and 1", nameof(dataAccuracyScore));
        if (culturalSensitivityScore < 0 || culturalSensitivityScore > 1) throw new ArgumentException("Cultural sensitivity score must be between 0 and 1", nameof(culturalSensitivityScore));
        if (overallAuditScore < 0 || overallAuditScore > 1) throw new ArgumentException("Overall audit score must be between 0 and 1", nameof(overallAuditScore));
        if (string.IsNullOrWhiteSpace(auditStatus)) throw new ArgumentException("Audit status is required", nameof(auditStatus));
        if (nextAuditDate <= auditDate) throw new ArgumentException("Next audit date must be after audit date", nameof(nextAuditDate));

        var categoriesList = culturalDataCategories?.ToList() ?? throw new ArgumentNullException(nameof(culturalDataCategories));
        var qualityIssuesList = dataQualityIssues?.ToList() ?? new List<string>();
        var sensitivityConcernsList = culturalSensitivityConcerns?.ToList() ?? new List<string>();
        var recommendationsList = recommendations?.ToList() ?? new List<string>();
        var metrics = auditMetrics ?? new Dictionary<string, object>();

        if (!categoriesList.Any()) throw new ArgumentException("At least one cultural data category is required", nameof(culturalDataCategories));

        // Validate audit status
        var validStatuses = new[] { "Passed", "Failed", "Warning", "In Progress", "Pending Review" };
        if (!validStatuses.Contains(auditStatus))
            throw new ArgumentException($"Audit status must be one of: {string.Join(", ", validStatuses)}", nameof(auditStatus));

        return new CulturalDataAudit(
            auditId,
            clientId,
            auditDate,
            dataDistribution,
            eventDataCounts,
            categoriesList.AsReadOnly(),
            qualityIssuesList.AsReadOnly(),
            sensitivityConcernsList.AsReadOnly(),
            dataAccuracyScore,
            culturalSensitivityScore,
            overallAuditScore,
            auditStatus,
            recommendationsList.AsReadOnly(),
            nextAuditDate,
            metrics);
    }

    public GeographicRegion GetMostRepresentedRegion()
    {
        return DataDistribution.OrderByDescending(x => x.Value).FirstOrDefault().Key;
    }

    public CulturalEventType GetMostCommonEventType()
    {
        return EventDataCounts.OrderByDescending(x => x.Value).FirstOrDefault().Key;
    }

    public int GetTotalDataPoints()
    {
        return DataDistribution.Values.Sum() + EventDataCounts.Values.Sum();
    }

    public double GetRegionalDataBalance()
    {
        if (!DataDistribution.Any()) return 0;
        
        var values = DataDistribution.Values.ToArray();
        var average = values.Average();
        var variance = values.Sum(v => Math.Pow(v - average, 2)) / values.Length;
        var standardDeviation = Math.Sqrt(variance);
        
        // Lower standard deviation means better balance (normalized to 0-1 scale)
        return Math.Max(0, 1 - (standardDeviation / average));
    }

    public string GetAuditGrade()
    {
        return OverallAuditScore switch
        {
            >= 0.95 => "A+",
            >= 0.90 => "A",
            >= 0.85 => "A-",
            >= 0.80 => "B+",
            >= 0.75 => "B",
            >= 0.70 => "B-",
            >= 0.65 => "C+",
            >= 0.60 => "C",
            >= 0.55 => "C-",
            >= 0.50 => "D",
            _ => "F"
        };
    }

    public string GetDataQualityRisk()
    {
        var issueCount = DataQualityIssues.Count;
        var concernCount = CulturalSensitivityConcerns.Count;
        
        if (DataAccuracyScore < 0.6 || CulturalSensitivityScore < 0.6 || issueCount > 10 || concernCount > 5)
            return "High";
        if (DataAccuracyScore < 0.8 || CulturalSensitivityScore < 0.8 || issueCount > 5 || concernCount > 2)
            return "Medium";
        if (issueCount > 0 || concernCount > 0)
            return "Low";
        return "None";
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return AuditId;
        yield return ClientId;
        yield return AuditDate;
        yield return DataAccuracyScore;
        yield return CulturalSensitivityScore;
        yield return OverallAuditScore;
        yield return AuditStatus;
        yield return NextAuditDate;
        
        foreach (var distribution in DataDistribution.OrderBy(x => x.Key))
        {
            yield return distribution.Key;
            yield return distribution.Value;
        }
        
        foreach (var eventCount in EventDataCounts.OrderBy(x => x.Key))
        {
            yield return eventCount.Key;
            yield return eventCount.Value;
        }
        
        foreach (var category in CulturalDataCategories)
            yield return category;
        
        foreach (var issue in DataQualityIssues)
            yield return issue;
        
        foreach (var concern in CulturalSensitivityConcerns)
            yield return concern;
        
        foreach (var recommendation in Recommendations)
            yield return recommendation;
        
        foreach (var metric in AuditMetrics.OrderBy(x => x.Key))
        {
            yield return metric.Key;
            yield return metric.Value;
        }
    }
}