using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Enterprise.ValueObjects;

public class ComplianceValidation : ValueObject
{
    public string ValidationId { get; private set; }
    public EnterpriseClientId ClientId { get; private set; }
    public DateTime ValidationDate { get; private set; }
    public IReadOnlyList<string> ComplianceFrameworks { get; private set; }
    public IReadOnlyDictionary<string, bool> ComplianceChecks { get; private set; }
    public bool OverallCompliance { get; private set; }
    public IReadOnlyList<string> Violations { get; private set; }
    public IReadOnlyList<string> Recommendations { get; private set; }
    public string ComplianceScore { get; private set; }
    public DateTime NextReviewDate { get; private set; }
    public string CertificationStatus { get; private set; }
    public IReadOnlyDictionary<string, object> AdditionalMetadata { get; private set; }
    public bool RequiresImmediateAction => !OverallCompliance || Violations.Any();

    private ComplianceValidation(
        string validationId,
        EnterpriseClientId clientId,
        DateTime validationDate,
        IReadOnlyList<string> complianceFrameworks,
        IReadOnlyDictionary<string, bool> complianceChecks,
        bool overallCompliance,
        IReadOnlyList<string> violations,
        IReadOnlyList<string> recommendations,
        string complianceScore,
        DateTime nextReviewDate,
        string certificationStatus,
        IReadOnlyDictionary<string, object> additionalMetadata)
    {
        ValidationId = validationId;
        ClientId = clientId;
        ValidationDate = validationDate;
        ComplianceFrameworks = complianceFrameworks;
        ComplianceChecks = complianceChecks;
        OverallCompliance = overallCompliance;
        Violations = violations;
        Recommendations = recommendations;
        ComplianceScore = complianceScore;
        NextReviewDate = nextReviewDate;
        CertificationStatus = certificationStatus;
        AdditionalMetadata = additionalMetadata;
    }

    public static ComplianceValidation Create(
        string validationId,
        EnterpriseClientId clientId,
        DateTime validationDate,
        IEnumerable<string> complianceFrameworks,
        IReadOnlyDictionary<string, bool> complianceChecks,
        bool overallCompliance,
        IEnumerable<string> violations,
        IEnumerable<string> recommendations,
        string complianceScore,
        DateTime nextReviewDate,
        string certificationStatus,
        IReadOnlyDictionary<string, object>? additionalMetadata = null)
    {
        if (string.IsNullOrWhiteSpace(validationId)) throw new ArgumentException("Validation ID is required", nameof(validationId));
        if (clientId == null) throw new ArgumentNullException(nameof(clientId));
        if (validationDate > DateTime.UtcNow) throw new ArgumentException("Validation date cannot be in the future", nameof(validationDate));
        if (complianceChecks == null || !complianceChecks.Any()) throw new ArgumentException("Compliance checks are required", nameof(complianceChecks));
        if (string.IsNullOrWhiteSpace(complianceScore)) throw new ArgumentException("Compliance score is required", nameof(complianceScore));
        if (nextReviewDate <= validationDate) throw new ArgumentException("Next review date must be after validation date", nameof(nextReviewDate));
        if (string.IsNullOrWhiteSpace(certificationStatus)) throw new ArgumentException("Certification status is required", nameof(certificationStatus));

        var frameworksList = complianceFrameworks?.ToList() ?? throw new ArgumentNullException(nameof(complianceFrameworks));
        var violationsList = violations?.ToList() ?? new List<string>();
        var recommendationsList = recommendations?.ToList() ?? new List<string>();
        var metadata = additionalMetadata ?? new Dictionary<string, object>();

        if (!frameworksList.Any()) throw new ArgumentException("At least one compliance framework is required", nameof(complianceFrameworks));

        // Validate certification status
        var validStatuses = new[] { "Certified", "Provisional", "Non-Compliant", "Under Review", "Expired" };
        if (!validStatuses.Contains(certificationStatus))
            throw new ArgumentException($"Certification status must be one of: {string.Join(", ", validStatuses)}", nameof(certificationStatus));

        // Validate compliance score format (should be percentage or grade)
        var validScorePatterns = new[] { "A+", "A", "A-", "B+", "B", "B-", "C+", "C", "C-", "D", "F" };
        var isPercentage = complianceScore.EndsWith('%') && 
                          double.TryParse(complianceScore.TrimEnd('%'), out var percentage) && 
                          percentage >= 0 && percentage <= 100;
        var isGrade = validScorePatterns.Contains(complianceScore);
        
        if (!isPercentage && !isGrade)
            throw new ArgumentException("Compliance score must be a valid percentage (0%-100%) or grade (A+ to F)", nameof(complianceScore));

        return new ComplianceValidation(
            validationId,
            clientId,
            validationDate,
            frameworksList.AsReadOnly(),
            complianceChecks,
            overallCompliance,
            violationsList.AsReadOnly(),
            recommendationsList.AsReadOnly(),
            complianceScore,
            nextReviewDate,
            certificationStatus,
            metadata);
    }

    public double GetCompliancePercentage()
    {
        var passedChecks = ComplianceChecks.Count(c => c.Value);
        var totalChecks = ComplianceChecks.Count;
        return totalChecks > 0 ? (double)passedChecks / totalChecks * 100 : 0;
    }

    public IReadOnlyList<string> GetFailedChecks()
    {
        return ComplianceChecks
            .Where(c => !c.Value)
            .Select(c => c.Key)
            .ToList()
            .AsReadOnly();
    }

    public bool IsFrameworkCompliant(string framework)
    {
        return ComplianceFrameworks.Contains(framework, StringComparer.OrdinalIgnoreCase) && OverallCompliance;
    }

    public TimeSpan TimeUntilNextReview()
    {
        return NextReviewDate > DateTime.UtcNow 
            ? NextReviewDate - DateTime.UtcNow 
            : TimeSpan.Zero;
    }

    public string GetComplianceRisk()
    {
        var failedChecksCount = GetFailedChecks().Count;
        var violationsCount = Violations.Count;
        
        if (!OverallCompliance || violationsCount > 5 || failedChecksCount > 10)
            return "High";
        if (violationsCount > 2 || failedChecksCount > 5)
            return "Medium";
        if (violationsCount > 0 || failedChecksCount > 0)
            return "Low";
        return "None";
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return ValidationId;
        yield return ClientId;
        yield return ValidationDate;
        yield return OverallCompliance;
        yield return ComplianceScore;
        yield return NextReviewDate;
        yield return CertificationStatus;
        
        foreach (var framework in ComplianceFrameworks)
            yield return framework;
        
        foreach (var check in ComplianceChecks.OrderBy(x => x.Key))
        {
            yield return check.Key;
            yield return check.Value;
        }
        
        foreach (var violation in Violations)
            yield return violation;
        
        foreach (var recommendation in Recommendations)
            yield return recommendation;
        
        foreach (var metadata in AdditionalMetadata.OrderBy(x => x.Key))
        {
            yield return metadata.Key;
            yield return metadata.Value;
        }
    }
}