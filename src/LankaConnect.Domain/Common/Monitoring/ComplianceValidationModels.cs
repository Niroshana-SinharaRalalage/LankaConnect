using LankaConnect.Domain.Common.ValueObjects;

namespace LankaConnect.Domain.Common.Monitoring;

/// <summary>
/// GREEN PHASE: Minimal implementation for ComplianceValidationResult (20 references)
/// Fortune 500 compliance validation with Sri Lankan cultural data protection
/// </summary>
public record ComplianceValidationResult(
    bool IsCompliant,
    double OverallComplianceScore,
    IReadOnlyList<ComplianceValidationViolation> Violations,
    ComplianceMetrics ComplianceMetrics,
    DateTime ValidationTimestamp,
    string ValidationContext
)
{
    public static ComplianceValidationResult Create(bool isCompliant, double overallComplianceScore, IReadOnlyList<ComplianceValidationViolation> violations, ComplianceMetrics complianceMetrics, DateTime validationTimestamp, string validationContext)
    {
        if (overallComplianceScore < 0.0 || overallComplianceScore > 100.0)
            throw new ArgumentException("Compliance score must be between 0.0 and 100.0");

        if (isCompliant && violations.Any())
            throw new InvalidOperationException("Cannot be compliant with existing violations");

        if (string.IsNullOrWhiteSpace(validationContext))
            throw new ArgumentException("Validation context cannot be empty");

        return new ComplianceValidationResult(isCompliant, overallComplianceScore, violations, complianceMetrics, validationTimestamp, validationContext);
    }

    /// <summary>
    /// Checks if there are any critical violations requiring immediate action
    /// </summary>
    public bool HasCriticalViolations()
    {
        return Violations.Any(v => v.Severity == ComplianceSeverity.Critical);
    }

    /// <summary>
    /// Gets only critical violations
    /// </summary>
    public IReadOnlyList<ComplianceValidationViolation> GetCriticalViolations()
    {
        return Violations.Where(v => v.Severity == ComplianceSeverity.Critical).ToList();
    }

    /// <summary>
    /// Gets violations related to cultural data protection
    /// </summary>
    public IReadOnlyList<ComplianceValidationViolation> GetCulturalViolations()
    {
        return Violations.Where(v => v.Code.StartsWith("SLDP-") || v.Code.StartsWith("CULT-")).ToList();
    }

    /// <summary>
    /// Determines if compliance meets Fortune 500 standards
    /// </summary>
    public bool IsFortuneReadyCompliance()
    {
        return IsCompliant &&
               OverallComplianceScore >= 90.0 &&
               !HasCriticalViolations() &&
               ComplianceMetrics.GdprCompliance >= 95.0 &&
               ComplianceMetrics.Soc2Compliance >= 90.0;
    }

    /// <summary>
    /// Checks if Sri Lankan cultural protection standards are met
    /// </summary>
    public bool MeetsSriLankanCulturalStandards()
    {
        return ComplianceMetrics.SriLankanDataProtectionCompliance >= 70.0;
    }

    /// <summary>
    /// Determines if immediate action is required
    /// </summary>
    public bool RequiresImmediateAction()
    {
        return HasCriticalViolations() || OverallComplianceScore < 50.0;
    }

    /// <summary>
    /// Determines if cultural sensitivity review is needed
    /// </summary>
    public bool RequiresCulturalSensitivityReview()
    {
        return !MeetsSriLankanCulturalStandards() || GetCulturalViolations().Any();
    }

    /// <summary>
    /// Generates action plan for compliance improvement
    /// </summary>
    public ComplianceActionPlan GenerateActionPlan()
    {
        var priorityActions = GetCriticalViolations().Select(v => $"CRITICAL: {v.Description}").ToList();
        var standardActions = Violations.Where(v => v.Severity == ComplianceSeverity.High)
            .Select(v => $"HIGH: {v.Description}").ToList();
        var minorActions = Violations.Where(v => v.Severity <= ComplianceSeverity.Medium)
            .Select(v => $"MEDIUM/LOW: {v.Description}").ToList();

        var estimatedTime = TimeSpan.FromHours(priorityActions.Count * 8 + standardActions.Count * 4 + minorActions.Count * 2);

        return new ComplianceActionPlan(priorityActions, standardActions, minorActions, estimatedTime);
    }
}

/// <summary>
/// Compliance violation details
/// </summary>
public record ComplianceValidationViolation(string Code, string Description, ComplianceSeverity Severity)
{
    public static ComplianceValidationViolation Create(string code, string description, ComplianceSeverity severity)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Violation code cannot be empty");

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Violation description cannot be empty");

        return new ComplianceValidationViolation(code, description, severity);
    }
}

/// <summary>
/// Comprehensive compliance metrics
/// </summary>
public record ComplianceMetrics(
    double OverallScore,
    double GdprCompliance,
    double Soc2Compliance,
    double SriLankanDataProtectionCompliance
)
{
    public static ComplianceMetrics Create(double overallScore, double gdprCompliance, double soc2Compliance, double sriLankanDataProtectionCompliance)
    {
        if (overallScore < 0.0 || overallScore > 100.0)
            throw new ArgumentException("Overall score must be between 0.0 and 100.0");

        if (gdprCompliance < 0.0 || gdprCompliance > 100.0)
            throw new ArgumentException("GDPR compliance must be between 0.0 and 100.0");

        if (soc2Compliance < 0.0 || soc2Compliance > 100.0)
            throw new ArgumentException("SOC2 compliance must be between 0.0 and 100.0");

        if (sriLankanDataProtectionCompliance < 0.0 || sriLankanDataProtectionCompliance > 100.0)
            throw new ArgumentException("Sri Lankan data protection compliance must be between 0.0 and 100.0");

        return new ComplianceMetrics(overallScore, gdprCompliance, soc2Compliance, sriLankanDataProtectionCompliance);
    }
}

/// <summary>
/// Action plan for compliance remediation
/// </summary>
public record ComplianceActionPlan(
    IReadOnlyList<string> PriorityActions,
    IReadOnlyList<string> StandardActions,
    IReadOnlyList<string> MinorActions,
    TimeSpan EstimatedResolutionTime
);

/// <summary>
/// Severity levels for compliance violations
/// </summary>
public enum ComplianceSeverity
{
    Low,
    Medium,
    High,
    Critical
}