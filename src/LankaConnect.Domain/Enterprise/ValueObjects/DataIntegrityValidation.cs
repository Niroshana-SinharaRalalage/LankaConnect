using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Enterprise.ValueObjects;

public class DataIntegrityValidation : ValueObject
{
    public string ValidationId { get; private set; }
    public EnterpriseClientId ClientId { get; private set; }
    public DateTime ValidationDate { get; private set; }
    public IReadOnlyDictionary<string, bool> IntegrityChecks { get; private set; }
    public IReadOnlyDictionary<string, double> DataQualityScores { get; private set; }
    public IReadOnlyList<string> IntegrityViolations { get; private set; }
    public IReadOnlyList<string> CorruptedDataSources { get; private set; }
    public IReadOnlyList<string> RecommendedActions { get; private set; }
    public double OverallIntegrityScore { get; private set; }
    public string ValidationStatus { get; private set; }
    public TimeSpan ValidationDuration { get; private set; }
    public int TotalRecordsValidated { get; private set; }
    public int CorruptedRecords { get; private set; }
    public int RepairedRecords { get; private set; }
    public DateTime NextValidationDate { get; private set; }
    public IReadOnlyDictionary<string, object> ValidationMetrics { get; private set; }
    public bool RequiresImmediateAction => OverallIntegrityScore < 0.8 || CorruptedDataSources.Any();

    private DataIntegrityValidation(
        string validationId,
        EnterpriseClientId clientId,
        DateTime validationDate,
        IReadOnlyDictionary<string, bool> integrityChecks,
        IReadOnlyDictionary<string, double> dataQualityScores,
        IReadOnlyList<string> integrityViolations,
        IReadOnlyList<string> corruptedDataSources,
        IReadOnlyList<string> recommendedActions,
        double overallIntegrityScore,
        string validationStatus,
        TimeSpan validationDuration,
        int totalRecordsValidated,
        int corruptedRecords,
        int repairedRecords,
        DateTime nextValidationDate,
        IReadOnlyDictionary<string, object> validationMetrics)
    {
        ValidationId = validationId;
        ClientId = clientId;
        ValidationDate = validationDate;
        IntegrityChecks = integrityChecks;
        DataQualityScores = dataQualityScores;
        IntegrityViolations = integrityViolations;
        CorruptedDataSources = corruptedDataSources;
        RecommendedActions = recommendedActions;
        OverallIntegrityScore = overallIntegrityScore;
        ValidationStatus = validationStatus;
        ValidationDuration = validationDuration;
        TotalRecordsValidated = totalRecordsValidated;
        CorruptedRecords = corruptedRecords;
        RepairedRecords = repairedRecords;
        NextValidationDate = nextValidationDate;
        ValidationMetrics = validationMetrics;
    }

    public static DataIntegrityValidation Create(
        string validationId,
        EnterpriseClientId clientId,
        DateTime validationDate,
        IReadOnlyDictionary<string, bool> integrityChecks,
        IReadOnlyDictionary<string, double> dataQualityScores,
        IEnumerable<string> integrityViolations,
        IEnumerable<string> corruptedDataSources,
        IEnumerable<string> recommendedActions,
        double overallIntegrityScore,
        string validationStatus,
        TimeSpan validationDuration,
        int totalRecordsValidated,
        int corruptedRecords,
        int repairedRecords,
        DateTime nextValidationDate,
        IReadOnlyDictionary<string, object>? validationMetrics = null)
    {
        if (string.IsNullOrWhiteSpace(validationId)) throw new ArgumentException("Validation ID is required", nameof(validationId));
        if (clientId == null) throw new ArgumentNullException(nameof(clientId));
        if (validationDate > DateTime.UtcNow) throw new ArgumentException("Validation date cannot be in the future", nameof(validationDate));
        if (integrityChecks == null || !integrityChecks.Any()) throw new ArgumentException("Integrity checks are required", nameof(integrityChecks));
        if (dataQualityScores == null || !dataQualityScores.Any()) throw new ArgumentException("Data quality scores are required", nameof(dataQualityScores));
        if (overallIntegrityScore < 0 || overallIntegrityScore > 1) throw new ArgumentException("Overall integrity score must be between 0 and 1", nameof(overallIntegrityScore));
        if (string.IsNullOrWhiteSpace(validationStatus)) throw new ArgumentException("Validation status is required", nameof(validationStatus));
        if (validationDuration < TimeSpan.Zero) throw new ArgumentException("Validation duration cannot be negative", nameof(validationDuration));
        if (totalRecordsValidated < 0) throw new ArgumentException("Total records validated cannot be negative", nameof(totalRecordsValidated));
        if (corruptedRecords < 0) throw new ArgumentException("Corrupted records cannot be negative", nameof(corruptedRecords));
        if (repairedRecords < 0) throw new ArgumentException("Repaired records cannot be negative", nameof(repairedRecords));
        if (nextValidationDate <= validationDate) throw new ArgumentException("Next validation date must be after current validation date", nameof(nextValidationDate));
        if (corruptedRecords > totalRecordsValidated) throw new ArgumentException("Corrupted records cannot exceed total records validated", nameof(corruptedRecords));
        if (repairedRecords > corruptedRecords) throw new ArgumentException("Repaired records cannot exceed corrupted records", nameof(repairedRecords));

        // Validate data quality scores
        foreach (var score in dataQualityScores.Values)
        {
            if (score < 0 || score > 1)
                throw new ArgumentException("All data quality scores must be between 0 and 1", nameof(dataQualityScores));
        }

        var violationsList = integrityViolations?.ToList() ?? new List<string>();
        var corruptedSourcesList = corruptedDataSources?.ToList() ?? new List<string>();
        var actionsList = recommendedActions?.ToList() ?? new List<string>();
        var metrics = validationMetrics ?? new Dictionary<string, object>();

        // Validate validation status
        var validStatuses = new[] { "Passed", "Failed", "Warning", "In Progress", "Cancelled", "Partial" };
        if (!validStatuses.Contains(validationStatus))
            throw new ArgumentException($"Validation status must be one of: {string.Join(", ", validStatuses)}", nameof(validationStatus));

        return new DataIntegrityValidation(
            validationId,
            clientId,
            validationDate,
            integrityChecks,
            dataQualityScores,
            violationsList.AsReadOnly(),
            corruptedSourcesList.AsReadOnly(),
            actionsList.AsReadOnly(),
            overallIntegrityScore,
            validationStatus,
            validationDuration,
            totalRecordsValidated,
            corruptedRecords,
            repairedRecords,
            nextValidationDate,
            metrics);
    }

    public double GetCorruptionRate()
    {
        return TotalRecordsValidated > 0 ? (double)CorruptedRecords / TotalRecordsValidated : 0;
    }

    public double GetRepairRate()
    {
        return CorruptedRecords > 0 ? (double)RepairedRecords / CorruptedRecords : 0;
    }

    public double GetIntegrityCheckPassRate()
    {
        var totalChecks = IntegrityChecks.Count;
        var passedChecks = IntegrityChecks.Count(c => c.Value);
        return totalChecks > 0 ? (double)passedChecks / totalChecks : 0;
    }

    public double GetAverageDataQualityScore()
    {
        return DataQualityScores.Any() ? DataQualityScores.Values.Average() : 0;
    }

    public IReadOnlyList<string> GetFailedIntegrityChecks()
    {
        return IntegrityChecks
            .Where(c => !c.Value)
            .Select(c => c.Key)
            .ToList()
            .AsReadOnly();
    }

    public IReadOnlyList<string> GetLowQualityDataSources()
    {
        return DataQualityScores
            .Where(s => s.Value < 0.7)
            .Select(s => s.Key)
            .ToList()
            .AsReadOnly();
    }

    public string GetValidationGrade()
    {
        return OverallIntegrityScore switch
        {
            >= 0.98 => "A+",
            >= 0.95 => "A",
            >= 0.90 => "A-",
            >= 0.85 => "B+",
            >= 0.80 => "B",
            >= 0.75 => "B-",
            >= 0.70 => "C+",
            >= 0.65 => "C",
            >= 0.60 => "C-",
            >= 0.50 => "D",
            _ => "F"
        };
    }

    public string GetDataHealthStatus()
    {
        if (OverallIntegrityScore >= 0.95 && !CorruptedDataSources.Any()) return "Excellent";
        if (OverallIntegrityScore >= 0.90 && CorruptedDataSources.Count <= 1) return "Good";
        if (OverallIntegrityScore >= 0.80 && CorruptedDataSources.Count <= 3) return "Fair";
        if (OverallIntegrityScore >= 0.70) return "Poor";
        return "Critical";
    }

    public string GetRiskLevel()
    {
        var corruptionRate = GetCorruptionRate();
        var failedChecks = GetFailedIntegrityChecks().Count;
        
        if (OverallIntegrityScore < 0.6 || corruptionRate > 0.1 || failedChecks > 5)
            return "High";
        if (OverallIntegrityScore < 0.8 || corruptionRate > 0.05 || failedChecks > 2)
            return "Medium";
        if (OverallIntegrityScore < 0.95 || corruptionRate > 0.01 || failedChecks > 0)
            return "Low";
        return "None";
    }

    public TimeSpan GetTimeUntilNextValidation()
    {
        return NextValidationDate > DateTime.UtcNow 
            ? NextValidationDate - DateTime.UtcNow 
            : TimeSpan.Zero;
    }

    public IReadOnlyList<string> GetPriorityActions()
    {
        var actions = new List<string>();

        if (RequiresImmediateAction)
        {
            actions.Add("Schedule immediate data repair");
            actions.Add("Investigate data corruption sources");
        }

        var lowQualitySources = GetLowQualityDataSources();
        if (lowQualitySources.Any())
        {
            actions.Add($"Address data quality issues in: {string.Join(", ", lowQualitySources.Take(3))}");
        }

        var failedChecks = GetFailedIntegrityChecks();
        if (failedChecks.Any())
        {
            actions.Add($"Resolve failed integrity checks: {string.Join(", ", failedChecks.Take(3))}");
        }

        if (GetCorruptionRate() > 0.05)
        {
            actions.Add("Implement enhanced data validation procedures");
        }

        if (!actions.Any())
        {
            actions.Add("Continue regular monitoring and maintenance");
        }

        return actions.AsReadOnly();
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return ValidationId;
        yield return ClientId;
        yield return ValidationDate;
        yield return OverallIntegrityScore;
        yield return ValidationStatus;
        yield return ValidationDuration;
        yield return TotalRecordsValidated;
        yield return CorruptedRecords;
        yield return RepairedRecords;
        yield return NextValidationDate;
        
        foreach (var check in IntegrityChecks.OrderBy(x => x.Key))
        {
            yield return check.Key;
            yield return check.Value;
        }
        
        foreach (var score in DataQualityScores.OrderBy(x => x.Key))
        {
            yield return score.Key;
            yield return score.Value;
        }
        
        foreach (var violation in IntegrityViolations)
            yield return violation;
        
        foreach (var source in CorruptedDataSources)
            yield return source;
        
        foreach (var action in RecommendedActions)
            yield return action;
        
        foreach (var metric in ValidationMetrics.OrderBy(x => x.Key))
        {
            yield return metric.Key;
            yield return metric.Value;
        }
    }
}