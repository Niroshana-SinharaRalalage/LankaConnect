using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Enterprise.ValueObjects;

public class GDPRComplianceCheck : ValueObject
{
    public string CheckId { get; private set; }
    public EnterpriseClientId ClientId { get; private set; }
    public DateTime CheckDate { get; private set; }
    public IReadOnlyDictionary<string, bool> DataProcessingChecks { get; private set; }
    public IReadOnlyDictionary<string, bool> ConsentChecks { get; private set; }
    public IReadOnlyDictionary<string, bool> DataSubjectRightsChecks { get; private set; }
    public bool OverallGDPRCompliance { get; private set; }
    public IReadOnlyList<string> Violations { get; private set; }
    public IReadOnlyList<string> Recommendations { get; private set; }
    public string DataProtectionOfficerContact { get; private set; }
    public DateTime NextAuditDate { get; private set; }
    public IReadOnlyDictionary<string, object> AdditionalMetadata { get; private set; }
    public bool RequiresImmediateAction => !OverallGDPRCompliance || Violations.Any();

    private GDPRComplianceCheck(
        string checkId,
        EnterpriseClientId clientId,
        DateTime checkDate,
        IReadOnlyDictionary<string, bool> dataProcessingChecks,
        IReadOnlyDictionary<string, bool> consentChecks,
        IReadOnlyDictionary<string, bool> dataSubjectRightsChecks,
        bool overallGDPRCompliance,
        IReadOnlyList<string> violations,
        IReadOnlyList<string> recommendations,
        string dataProtectionOfficerContact,
        DateTime nextAuditDate,
        IReadOnlyDictionary<string, object> additionalMetadata)
    {
        CheckId = checkId;
        ClientId = clientId;
        CheckDate = checkDate;
        DataProcessingChecks = dataProcessingChecks;
        ConsentChecks = consentChecks;
        DataSubjectRightsChecks = dataSubjectRightsChecks;
        OverallGDPRCompliance = overallGDPRCompliance;
        Violations = violations;
        Recommendations = recommendations;
        DataProtectionOfficerContact = dataProtectionOfficerContact;
        NextAuditDate = nextAuditDate;
        AdditionalMetadata = additionalMetadata;
    }

    public static GDPRComplianceCheck Create(
        string checkId,
        EnterpriseClientId clientId,
        DateTime checkDate,
        IReadOnlyDictionary<string, bool> dataProcessingChecks,
        IReadOnlyDictionary<string, bool> consentChecks,
        IReadOnlyDictionary<string, bool> dataSubjectRightsChecks,
        bool overallGDPRCompliance,
        IEnumerable<string> violations,
        IEnumerable<string> recommendations,
        string dataProtectionOfficerContact,
        DateTime nextAuditDate,
        IReadOnlyDictionary<string, object>? additionalMetadata = null)
    {
        if (string.IsNullOrWhiteSpace(checkId)) throw new ArgumentException("Check ID is required", nameof(checkId));
        if (clientId == null) throw new ArgumentNullException(nameof(clientId));
        if (checkDate > DateTime.UtcNow) throw new ArgumentException("Check date cannot be in the future", nameof(checkDate));
        if (dataProcessingChecks == null || !dataProcessingChecks.Any()) throw new ArgumentException("Data processing checks are required", nameof(dataProcessingChecks));
        if (consentChecks == null || !consentChecks.Any()) throw new ArgumentException("Consent checks are required", nameof(consentChecks));
        if (dataSubjectRightsChecks == null || !dataSubjectRightsChecks.Any()) throw new ArgumentException("Data subject rights checks are required", nameof(dataSubjectRightsChecks));
        if (string.IsNullOrWhiteSpace(dataProtectionOfficerContact)) throw new ArgumentException("Data Protection Officer contact is required", nameof(dataProtectionOfficerContact));
        if (nextAuditDate <= checkDate) throw new ArgumentException("Next audit date must be after check date", nameof(nextAuditDate));

        var violationsList = violations?.ToList() ?? new List<string>();
        var recommendationsList = recommendations?.ToList() ?? new List<string>();
        var metadata = additionalMetadata ?? new Dictionary<string, object>();

        return new GDPRComplianceCheck(
            checkId,
            clientId,
            checkDate,
            dataProcessingChecks,
            consentChecks,
            dataSubjectRightsChecks,
            overallGDPRCompliance,
            violationsList.AsReadOnly(),
            recommendationsList.AsReadOnly(),
            dataProtectionOfficerContact,
            nextAuditDate,
            metadata);
    }

    public double GetDataProcessingCompliancePercentage()
    {
        var passedChecks = DataProcessingChecks.Count(c => c.Value);
        var totalChecks = DataProcessingChecks.Count;
        return totalChecks > 0 ? (double)passedChecks / totalChecks * 100 : 0;
    }

    public double GetConsentCompliancePercentage()
    {
        var passedChecks = ConsentChecks.Count(c => c.Value);
        var totalChecks = ConsentChecks.Count;
        return totalChecks > 0 ? (double)passedChecks / totalChecks * 100 : 0;
    }

    public double GetDataSubjectRightsCompliancePercentage()
    {
        var passedChecks = DataSubjectRightsChecks.Count(c => c.Value);
        var totalChecks = DataSubjectRightsChecks.Count;
        return totalChecks > 0 ? (double)passedChecks / totalChecks * 100 : 0;
    }

    public double GetOverallCompliancePercentage()
    {
        var totalChecks = DataProcessingChecks.Count + ConsentChecks.Count + DataSubjectRightsChecks.Count;
        var passedChecks = DataProcessingChecks.Count(c => c.Value) + 
                          ConsentChecks.Count(c => c.Value) + 
                          DataSubjectRightsChecks.Count(c => c.Value);
        return totalChecks > 0 ? (double)passedChecks / totalChecks * 100 : 0;
    }

    public IReadOnlyList<string> GetFailedChecks()
    {
        var failedChecks = new List<string>();
        failedChecks.AddRange(DataProcessingChecks.Where(c => !c.Value).Select(c => $"Data Processing: {c.Key}"));
        failedChecks.AddRange(ConsentChecks.Where(c => !c.Value).Select(c => $"Consent: {c.Key}"));
        failedChecks.AddRange(DataSubjectRightsChecks.Where(c => !c.Value).Select(c => $"Data Subject Rights: {c.Key}"));
        return failedChecks.AsReadOnly();
    }

    public string GetComplianceRisk()
    {
        var overallPercentage = GetOverallCompliancePercentage();
        var violationsCount = Violations.Count;

        if (!OverallGDPRCompliance || violationsCount > 5 || overallPercentage < 70)
            return "High";
        if (violationsCount > 2 || overallPercentage < 85)
            return "Medium";
        if (violationsCount > 0 || overallPercentage < 95)
            return "Low";
        return "None";
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return CheckId;
        yield return ClientId;
        yield return CheckDate;
        yield return OverallGDPRCompliance;
        yield return DataProtectionOfficerContact;
        yield return NextAuditDate;
        
        foreach (var check in DataProcessingChecks.OrderBy(x => x.Key))
        {
            yield return check.Key;
            yield return check.Value;
        }
        
        foreach (var check in ConsentChecks.OrderBy(x => x.Key))
        {
            yield return check.Key;
            yield return check.Value;
        }
        
        foreach (var check in DataSubjectRightsChecks.OrderBy(x => x.Key))
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