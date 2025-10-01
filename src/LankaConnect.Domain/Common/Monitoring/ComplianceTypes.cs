using System;
using System.Collections.Generic;
using System.Linq;

namespace LankaConnect.Domain.Common.Monitoring;

/// <summary>
/// Compliance monitoring and SLA management types for LankaConnect's
/// cultural intelligence platform with Fortune 500 enterprise capabilities.
/// </summary>

#region SLA Compliance

/// <summary>
/// SLA compliance status tracking.
/// </summary>
public class SLAComplianceStatus
{
    /// <summary>
    /// Gets the SLA identifier.
    /// </summary>
    public string SLAId { get; private set; }

    /// <summary>
    /// Gets the service name.
    /// </summary>
    public string ServiceName { get; private set; }

    /// <summary>
    /// Gets the target uptime percentage.
    /// </summary>
    public double TargetUptime { get; private set; }

    /// <summary>
    /// Gets the actual uptime percentage.
    /// </summary>
    public double ActualUptime { get; private set; }

    /// <summary>
    /// Gets whether cultural events are supported.
    /// </summary>
    public bool SupportsCulturalEvents { get; private set; }

    /// <summary>
    /// Gets the compliance period.
    /// </summary>
    public string CompliancePeriod { get; private set; }

    /// <summary>
    /// Gets when the status was measured.
    /// </summary>
    public DateTime MeasuredAt { get; private set; }

    /// <summary>
    /// Gets the compliance percentage.
    /// </summary>
    public double CompliancePercentage => TargetUptime > 0 ? (ActualUptime / TargetUptime) * 100 : 0;

    /// <summary>
    /// Gets whether the SLA is compliant.
    /// </summary>
    public bool IsCompliant => ActualUptime >= TargetUptime;

    /// <summary>
    /// Gets whether this meets Fortune 500 compliance (99.9%+).
    /// </summary>
    public bool IsFortune500Compliant => ActualUptime >= 99.9;

    private SLAComplianceStatus(string slaId, string serviceName, double targetUptime, 
        double actualUptime, bool supportsCulturalEvents, string compliancePeriod)
    {
        SLAId = slaId;
        ServiceName = serviceName;
        TargetUptime = targetUptime;
        ActualUptime = actualUptime;
        SupportsCulturalEvents = supportsCulturalEvents;
        CompliancePeriod = compliancePeriod;
        MeasuredAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates an SLA compliance status.
    /// </summary>
    public static SLAComplianceStatus Create(string slaId, string serviceName, double targetUptime, 
        double actualUptime, bool supportsCulturalEvents, string compliancePeriod)
    {
        return new SLAComplianceStatus(slaId, serviceName, targetUptime, actualUptime, 
            supportsCulturalEvents, compliancePeriod);
    }
}

/// <summary>
/// Overall compliance score aggregation.
/// </summary>
public class OverallComplianceScore
{
    /// <summary>
    /// Gets the score identifier.
    /// </summary>
    public string ScoreId { get; private set; }

    /// <summary>
    /// Gets the measurement period.
    /// </summary>
    public string Period { get; private set; }

    /// <summary>
    /// Gets the individual service scores.
    /// </summary>
    public Dictionary<string, double> ServiceScores { get; private set; }

    /// <summary>
    /// Gets the cultural event bonus.
    /// </summary>
    public double CulturalEventBonus { get; private set; }

    /// <summary>
    /// Gets the overall compliance score.
    /// </summary>
    public double OverallScore { get; private set; }

    /// <summary>
    /// Gets when the score was calculated.
    /// </summary>
    public DateTime CalculatedAt { get; private set; }

    /// <summary>
    /// Gets whether this meets Fortune 500 grade requirements.
    /// </summary>
    public bool IsFortune500Grade => OverallScore >= 99.9;

    /// <summary>
    /// Gets whether cultural event support is included.
    /// </summary>
    public bool HasCulturalEventSupport => CulturalEventBonus > 0;

    private OverallComplianceScore(string scoreId, string period, Dictionary<string, double> serviceScores, 
        double culturalEventBonus)
    {
        ScoreId = scoreId;
        Period = period;
        ServiceScores = serviceScores;
        CulturalEventBonus = culturalEventBonus;
        CalculatedAt = DateTime.UtcNow;
        
        // Calculate overall score with cultural bonus
        OverallScore = ServiceScores.Values.Any() 
            ? ServiceScores.Values.Average() + culturalEventBonus
            : 0;
    }

    /// <summary>
    /// Calculates overall compliance score.
    /// </summary>
    public static OverallComplianceScore Calculate(string scoreId, string period, 
        Dictionary<string, double> serviceScores, double culturalEventBonus = 0)
    {
        return new OverallComplianceScore(scoreId, period, serviceScores, culturalEventBonus);
    }
}

/// <summary>
/// Enhanced compliance violation tracking with cultural intelligence support.
/// Implements domain entity with cultural compliance capabilities.
/// </summary>
public class ComplianceViolation
{
    private readonly List<object> _domainEvents = new();
    private readonly List<ViolationHistoryEntry> _violationHistory = new();

    /// <summary>
    /// Gets the violation identifier.
    /// </summary>
    public string ViolationId { get; private set; }

    /// <summary>
    /// Gets the related SLA identifier.
    /// </summary>
    public string SLAId { get; private set; }

    /// <summary>
    /// Gets the violation type.
    /// </summary>
    public ComplianceViolationType ViolationType { get; private set; }

    /// <summary>
    /// Gets the violation severity.
    /// </summary>
    public ViolationSeverity Severity { get; private set; }

    /// <summary>
    /// Gets the violation description.
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// Gets the cultural impact description.
    /// </summary>
    public string? CulturalImpact { get; private set; }

    /// <summary>
    /// Gets the affected cultural data category.
    /// </summary>
    public CulturalDataCategory? AffectedCulturalCategory { get; private set; }

    /// <summary>
    /// Gets the violation duration.
    /// </summary>
    public TimeSpan ViolationDuration { get; private set; }

    /// <summary>
    /// Gets when the violation occurred.
    /// </summary>
    public DateTime OccurredAt { get; private set; }

    /// <summary>
    /// Gets the current violation status.
    /// </summary>
    public ViolationStatus Status { get; private set; }

    /// <summary>
    /// Gets the violation history entries.
    /// </summary>
    public IReadOnlyList<ViolationHistoryEntry> ViolationHistory => _violationHistory.AsReadOnly();

    /// <summary>
    /// Gets the domain events.
    /// </summary>
    public IReadOnlyList<object> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Gets whether the violation has cultural impact.
    /// </summary>
    public bool HasCulturalImpact => !string.IsNullOrEmpty(CulturalImpact);

    /// <summary>
    /// Gets whether the violation is culturally significant.
    /// </summary>
    public bool IsCulturallySignificant => AffectedCulturalCategory?.CulturalSignificance >= CulturalSignificance.High;

    /// <summary>
    /// Gets whether executive attention is required.
    /// </summary>
    public bool RequiresExecutiveAttention => Severity >= ViolationSeverity.High || HasCulturalImpact;

    /// <summary>
    /// Gets whether immediate action is required.
    /// </summary>
    public bool RequiresImmediateAction => Severity >= ViolationSeverity.High;

    private ComplianceViolation(string violationId, string slaId, ComplianceViolationType violationType,
        ViolationSeverity severity, string description, string? culturalImpact, TimeSpan duration,
        CulturalDataCategory? culturalCategory = null)
    {
        ViolationId = violationId;
        SLAId = slaId;
        ViolationType = violationType;
        Severity = severity;
        Description = description;
        CulturalImpact = culturalImpact;
        AffectedCulturalCategory = culturalCategory;
        ViolationDuration = duration;
        OccurredAt = DateTime.UtcNow;
        Status = ViolationStatus.Detected;

        // Raise domain event
        _domainEvents.Add(new ComplianceViolationDetectedEvent
        {
            ViolationId = violationId,
            Severity = severity,
            ViolationType = violationType,
            OccurredAt = OccurredAt
        });
    }

    /// <summary>
    /// Creates a standard compliance violation.
    /// </summary>
    public static ComplianceViolation Create(string violationId, string slaId,
        ComplianceViolationType violationType, ViolationSeverity severity,
        string description, string? culturalImpact, TimeSpan duration)
    {
        return new ComplianceViolation(violationId, slaId, violationType, severity,
            description, culturalImpact, duration);
    }

    /// <summary>
    /// Creates a compliance violation with cultural context.
    /// </summary>
    public static ComplianceViolation CreateWithCulturalContext(string violationId, string slaId,
        ComplianceViolationType violationType, ViolationSeverity severity,
        string description, CulturalDataCategory culturalContext, TimeSpan duration)
    {
        var culturalImpact = $"Cultural data category '{culturalContext.CategoryId}' affected with {culturalContext.CulturalSignificance} significance";
        return new ComplianceViolation(violationId, slaId, violationType, severity,
            description, culturalImpact, duration, culturalContext);
    }

    /// <summary>
    /// Calculates the compliance impact score based on severity and cultural factors.
    /// </summary>
    public double CalculateComplianceImpactScore()
    {
        var baseScore = Severity switch
        {
            ViolationSeverity.Low => 0.25,
            ViolationSeverity.Medium => 0.5,
            ViolationSeverity.High => 0.75,
            ViolationSeverity.Critical => 1.0,
            _ => 0.0
        };

        // Cultural significance multiplier
        if (AffectedCulturalCategory != null)
        {
            var culturalMultiplier = AffectedCulturalCategory.CulturalSignificance switch
            {
                CulturalSignificance.Sacred => 1.5,
                CulturalSignificance.CriticalHeritage => 1.3,
                CulturalSignificance.High => 1.2,
                CulturalSignificance.Medium => 1.1,
                _ => 1.0
            };
            baseScore *= culturalMultiplier;
        }

        // Duration impact
        var durationHours = ViolationDuration.TotalHours;
        var durationMultiplier = durationHours switch
        {
            >= 24 => 1.4,  // Over 24 hours
            >= 4 => 1.2,   // 4-24 hours
            >= 1 => 1.1,   // 1-4 hours
            _ => 1.0       // Less than 1 hour
        };

        return Math.Min(baseScore * durationMultiplier, 1.0);
    }

    /// <summary>
    /// Validates cultural compliance requirements.
    /// </summary>
    public CulturalComplianceValidation ValidateCulturalCompliance()
    {
        var validation = new CulturalComplianceValidation
        {
            ValidatedAt = DateTime.UtcNow,
            GDPRCompliant = ViolationType != ComplianceViolationType.GDPR,
            CulturalSensitivityCompliant = !IsCulturallySignificant,
            RequiresDualRemediation = ViolationType == ComplianceViolationType.GDPR && IsCulturallySignificant
        };

        if (!validation.GDPRCompliant)
            validation.ComplianceGaps.Add("GDPR compliance breach detected");

        if (!validation.CulturalSensitivityCompliant)
            validation.ComplianceGaps.Add("Cultural sensitivity requirements not met");

        return validation;
    }

    /// <summary>
    /// Generates a remediation plan for the violation.
    /// </summary>
    public RemediationPlan GenerateRemediationPlan()
    {
        var plan = new RemediationPlan
        {
            CreatedAt = DateTime.UtcNow,
            RequiresCulturalExpertConsultation = IsCulturallySignificant,
            EstimatedDuration = CalculateRemediationDuration()
        };

        // Technical remediation always required
        plan.RemediationSteps.Add(new RemediationStep
        {
            StepType = RemediationStepType.TechnicalRemediation,
            Description = $"Address {ViolationType} violation",
            EstimatedDuration = TimeSpan.FromHours(2),
            IsCritical = Severity >= ViolationSeverity.High
        });

        // Cultural sensitivity review for cultural violations
        if (IsCulturallySignificant)
        {
            plan.RemediationSteps.Add(new RemediationStep
            {
                StepType = RemediationStepType.CulturalSensitivityReview,
                Description = "Review cultural impact and sensitivity requirements",
                EstimatedDuration = TimeSpan.FromHours(4),
                IsCritical = true
            });
        }

        // Stakeholder notification for high severity
        if (RequiresExecutiveAttention)
        {
            plan.RemediationSteps.Add(new RemediationStep
            {
                StepType = RemediationStepType.StakeholderNotification,
                Description = "Notify executive team and stakeholders",
                EstimatedDuration = TimeSpan.FromMinutes(30),
                IsCritical = true
            });
        }

        return plan;
    }

    /// <summary>
    /// Adds a history entry to track violation progress.
    /// </summary>
    public void AddHistoryEntry(ViolationHistoryEntry entry)
    {
        _violationHistory.Add(entry);
        Status = entry.Status;
    }

    /// <summary>
    /// Resolves the violation with resolution details.
    /// </summary>
    public void ResolveViolation(ViolationResolutionDetails resolutionDetails)
    {
        Status = ViolationStatus.Resolved;
        resolutionDetails.ResolvedAt = DateTime.UtcNow;

        AddHistoryEntry(ViolationHistoryEntry.Create(
            $"Violation resolved: {resolutionDetails.ResolutionNotes}",
            ViolationStatus.Resolved));

        // Raise domain event
        _domainEvents.Add(new ComplianceViolationResolvedEvent
        {
            ViolationId = ViolationId,
            ResolutionMethod = resolutionDetails.ResolutionMethod,
            ResolvedAt = resolutionDetails.ResolvedAt
        });
    }

    /// <summary>
    /// Notifies stakeholders about the compliance violation.
    /// </summary>
    public void NotifyStakeholders(object notificationService, object auditLogger, object culturalAdvisor)
    {
        // This is a placeholder for the actual notification logic
        // The real implementation would use the injected services
        // London School TDD approach - verify behavior through mocks in tests
    }

    /// <summary>
    /// Escalates the violation when remediation timeline is exceeded.
    /// </summary>
    public void EscalateViolation(object escalationService, object complianceManager)
    {
        Status = ViolationStatus.Escalated;

        AddHistoryEntry(ViolationHistoryEntry.Create(
            "Violation escalated due to exceeded remediation timeline",
            ViolationStatus.Escalated));

        // This is a placeholder for the actual escalation logic
        // The real implementation would use the injected services
        // London School TDD approach - verify behavior through mocks in tests
    }

    private TimeSpan CalculateRemediationDuration()
    {
        var baseDuration = Severity switch
        {
            ViolationSeverity.Critical => TimeSpan.FromHours(4),
            ViolationSeverity.High => TimeSpan.FromHours(8),
            ViolationSeverity.Medium => TimeSpan.FromHours(24),
            ViolationSeverity.Low => TimeSpan.FromDays(3),
            _ => TimeSpan.FromDays(1)
        };

        // Cultural violations require additional time
        if (IsCulturallySignificant)
        {
            baseDuration = baseDuration.Add(TimeSpan.FromHours(4));
        }

        return baseDuration;
    }
}

/// <summary>
/// Compliance trend analysis.
/// </summary>
public class ComplianceTrend
{
    /// <summary>
    /// Gets the trend identifier.
    /// </summary>
    public string TrendId { get; private set; }

    /// <summary>
    /// Gets the analysis period.
    /// </summary>
    public string AnalysisPeriod { get; private set; }

    /// <summary>
    /// Gets the trend data points.
    /// </summary>
    public List<ComplianceTrendPoint> TrendData { get; private set; }

    /// <summary>
    /// Gets the average compliance.
    /// </summary>
    public double AverageCompliance { get; private set; }

    /// <summary>
    /// Gets the minimum compliance.
    /// </summary>
    public double MinimumCompliance { get; private set; }

    /// <summary>
    /// Gets the trend direction.
    /// </summary>
    public TrendDirection TrendDirection { get; private set; }

    /// <summary>
    /// Gets whether there's cultural event correlation.
    /// </summary>
    public bool HasCulturalEventCorrelation { get; private set; }

    /// <summary>
    /// Gets when the analysis was performed.
    /// </summary>
    public DateTime AnalyzedAt { get; private set; }

    private ComplianceTrend(string trendId, string analysisPeriod, List<ComplianceTrendPoint> trendData)
    {
        TrendId = trendId;
        AnalysisPeriod = analysisPeriod;
        TrendData = trendData;
        AnalyzedAt = DateTime.UtcNow;
        
        CalculateTrendMetrics();
    }

    /// <summary>
    /// Analyzes compliance trend.
    /// </summary>
    public static ComplianceTrend Analyze(string trendId, string period, List<ComplianceTrendPoint> trendData)
    {
        return new ComplianceTrend(trendId, period, trendData);
    }

    private void CalculateTrendMetrics()
    {
        if (!TrendData.Any())
        {
            AverageCompliance = 0;
            MinimumCompliance = 0;
            TrendDirection = TrendDirection.Stable;
            HasCulturalEventCorrelation = false;
            return;
        }

        var complianceValues = TrendData.Select(td => td.CompliancePercentage).ToList();
        AverageCompliance = complianceValues.Average();
        MinimumCompliance = complianceValues.Min();

        // Detect cultural event correlation
        HasCulturalEventCorrelation = TrendData.Any(td => 
            !string.IsNullOrEmpty(td.CulturalContext) && 
            td.CompliancePercentage < AverageCompliance);

        // Calculate trend direction
        if (complianceValues.Count >= 2)
        {
            var firstHalf = complianceValues.Take(complianceValues.Count / 2).Average();
            var secondHalf = complianceValues.Skip(complianceValues.Count / 2).Average();
            var change = (secondHalf - firstHalf) / firstHalf;

            TrendDirection = change switch
            {
                > 0.01 => TrendDirection.Improving,
                < -0.01 => TrendDirection.Declining,
                _ => TrendDirection.Stable
            };
        }
        else
        {
            TrendDirection = TrendDirection.Stable;
        }
    }
}

/// <summary>
/// Compliance trend data point.
/// </summary>
public record ComplianceTrendPoint(
    DateTime Timestamp,
    double CompliancePercentage,
    string? CulturalContext = null);

#endregion

#region Revenue Protection

/// <summary>
/// Revenue risk factor identification and assessment.
/// </summary>
public class RevenueRiskFactor
{
    /// <summary>
    /// Gets the risk identifier.
    /// </summary>
    public string RiskId { get; private set; }

    /// <summary>
    /// Gets the risk type.
    /// </summary>
    public RevenueRiskType RiskType { get; private set; }

    /// <summary>
    /// Gets the risk severity.
    /// </summary>
    public RiskSeverity Severity { get; private set; }

    /// <summary>
    /// Gets the risk description.
    /// </summary>
    public string Description { get; private set; }

    /// <summary>
    /// Gets the estimated revenue impact.
    /// </summary>
    public decimal EstimatedRevenueImpact { get; private set; }

    /// <summary>
    /// Gets the probability (0.0 to 1.0).
    /// </summary>
    public double Probability { get; private set; }

    /// <summary>
    /// Gets the mitigation strategies.
    /// </summary>
    public List<string> MitigationStrategies { get; private set; }

    /// <summary>
    /// Gets when the risk was identified.
    /// </summary>
    public DateTime IdentifiedAt { get; private set; }

    /// <summary>
    /// Gets the expected value (impact * probability).
    /// </summary>
    public decimal ExpectedValue => EstimatedRevenueImpact * (decimal)Probability;

    /// <summary>
    /// Gets whether this is cultural event related.
    /// </summary>
    public bool IsCulturalEventRelated => RiskType == RevenueRiskType.CulturalEventCapacity ||
        Description.Contains("Cultural", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets whether this is high impact.
    /// </summary>
    public bool IsHighImpact => EstimatedRevenueImpact >= 100000m; // $100K threshold

    private RevenueRiskFactor(string riskId, RevenueRiskType riskType, RiskSeverity severity, 
        string description, decimal estimatedImpact, double probability, List<string> mitigationStrategies)
    {
        RiskId = riskId;
        RiskType = riskType;
        Severity = severity;
        Description = description;
        EstimatedRevenueImpact = estimatedImpact;
        Probability = probability;
        MitigationStrategies = mitigationStrategies;
        IdentifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a revenue risk factor.
    /// </summary>
    public static RevenueRiskFactor Create(string riskId, RevenueRiskType riskType, 
        RiskSeverity severity, string description, decimal estimatedImpact, 
        double probability, List<string> mitigationStrategies)
    {
        return new RevenueRiskFactor(riskId, riskType, severity, description, 
            estimatedImpact, probability, mitigationStrategies);
    }
}

/// <summary>
/// Revenue protection system status.
/// </summary>
public class RevenueProtectionStatus
{
    /// <summary>
    /// Gets the status identifier.
    /// </summary>
    public string StatusId { get; private set; }

    /// <summary>
    /// Gets the protection type.
    /// </summary>
    public RevenueProtectionType ProtectionType { get; private set; }

    /// <summary>
    /// Gets whether protection is active.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Gets the protected revenue amount.
    /// </summary>
    public decimal ProtectedRevenue { get; private set; }

    /// <summary>
    /// Gets the protection effectiveness (0.0 to 1.0).
    /// </summary>
    public double ProtectionEffectiveness { get; private set; }

    /// <summary>
    /// Gets the cultural events supported.
    /// </summary>
    public List<string> CulturalEventsSupported { get; private set; }

    /// <summary>
    /// Gets when protection was last activated.
    /// </summary>
    public DateTime LastActivation { get; private set; }

    /// <summary>
    /// Gets when the status was updated.
    /// </summary>
    public DateTime StatusUpdatedAt { get; private set; }

    /// <summary>
    /// Gets whether protection is highly effective.
    /// </summary>
    public bool IsHighlyEffective => ProtectionEffectiveness >= 0.9;

    /// <summary>
    /// Gets whether cultural events are supported.
    /// </summary>
    public bool SupportsCulturalEvents => CulturalEventsSupported.Any();

    /// <summary>
    /// Gets whether protection was recently activated.
    /// </summary>
    public bool RecentlyActivated => (DateTime.UtcNow - LastActivation).TotalDays <= 7;

    private RevenueProtectionStatus(string statusId, RevenueProtectionType protectionType, 
        bool isActive, decimal protectedRevenue, double effectiveness, 
        List<string> culturalEvents, DateTime lastActivation)
    {
        StatusId = statusId;
        ProtectionType = protectionType;
        IsActive = isActive;
        ProtectedRevenue = protectedRevenue;
        ProtectionEffectiveness = effectiveness;
        CulturalEventsSupported = culturalEvents;
        LastActivation = lastActivation;
        StatusUpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a revenue protection status.
    /// </summary>
    public static RevenueProtectionStatus Create(string statusId, RevenueProtectionType protectionType, 
        bool isActive, decimal protectedRevenue, double effectiveness, 
        List<string> culturalEvents, DateTime lastActivation)
    {
        return new RevenueProtectionStatus(statusId, protectionType, isActive,
            protectedRevenue, effectiveness, culturalEvents, lastActivation);
    }
}

#endregion

#region Enhanced Compliance Enums and Value Objects

// ComplianceViolationType and ViolationSeverity enums are defined in AlertingTypes.cs

/// <summary>
/// Cultural significance levels for data categories.
/// </summary>
public enum CulturalSignificance
{
    Low,
    Medium,
    High,
    Sacred,
    CriticalHeritage
}

/// <summary>
/// Data protection levels for cultural content.
/// </summary>
public enum DataProtectionLevel
{
    Basic,
    Standard,
    Enhanced,
    GDPR,
    Maximum
}

/// <summary>
/// Violation status tracking.
/// </summary>
public enum ViolationStatus
{
    Detected,
    InvestigationInProgress,
    UnderRemediation,
    Resolved,
    Escalated
}

/// <summary>
/// Remediation step types for cultural compliance.
/// </summary>
public enum RemediationStepType
{
    TechnicalRemediation,
    ProcessImprovement,
    CulturalSensitivityReview,
    StakeholderNotification,
    RegulatoryReporting,
    TrainingAndAwareness
}

/// <summary>
/// Resolution methods for compliance violations.
/// </summary>
public enum ResolutionMethod
{
    TechnicalFix,
    ProcessImprovement,
    PolicyUpdate,
    Training,
    SystemUpgrade,
    ManualRemediation
}

/// <summary>
/// Cultural data category for enhanced protection.
/// </summary>
public class CulturalDataCategory
{
    public string CategoryId { get; set; } = string.Empty;
    public CulturalSignificance CulturalSignificance { get; set; }
    public DataProtectionLevel RequiredProtectionLevel { get; set; }
    public List<string> ProtectedRegions { get; set; } = new();
    public List<string> CulturalGroups { get; set; } = new();
}

/// <summary>
/// Cultural compliance validation result.
/// </summary>
public class CulturalComplianceValidation
{
    public bool GDPRCompliant { get; set; }
    public bool CulturalSensitivityCompliant { get; set; }
    public bool RequiresDualRemediation { get; set; }
    public List<string> ComplianceGaps { get; set; } = new();
    public DateTime ValidatedAt { get; set; }
}

/// <summary>
/// Violation history entry for tracking.
/// </summary>
public class ViolationHistoryEntry
{
    public string Description { get; private set; }
    public ViolationStatus Status { get; private set; }
    public DateTime Timestamp { get; private set; }

    private ViolationHistoryEntry(string description, ViolationStatus status)
    {
        Description = description;
        Status = status;
        Timestamp = DateTime.UtcNow;
    }

    public static ViolationHistoryEntry Create(string description, ViolationStatus status)
    {
        return new ViolationHistoryEntry(description, status);
    }
}

/// <summary>
/// Remediation plan for compliance violations.
/// </summary>
public class RemediationPlan
{
    public bool RequiresCulturalExpertConsultation { get; set; }
    public List<RemediationStep> RemediationSteps { get; set; } = new();
    public TimeSpan EstimatedDuration { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Individual remediation step.
/// </summary>
public class RemediationStep
{
    public RemediationStepType StepType { get; set; }
    public string Description { get; set; } = string.Empty;
    public TimeSpan EstimatedDuration { get; set; }
    public bool IsCritical { get; set; }
}

/// <summary>
/// Violation resolution details.
/// </summary>
public class ViolationResolutionDetails
{
    public string ResolvedBy { get; set; } = string.Empty;
    public ResolutionMethod ResolutionMethod { get; set; }
    public string ResolutionNotes { get; set; } = string.Empty;
    public DateTime ResolvedAt { get; set; }
}

// CulturalImpactAssessment moved to LankaConnect.Domain.Common.Database.FailoverModels.CulturalImpactAssessment to resolve CS0104 conflict

#endregion

#region Domain Events

/// <summary>
/// Domain event raised when a compliance violation is detected.
/// </summary>
public class ComplianceViolationDetectedEvent
{
    public string ViolationId { get; set; } = string.Empty;
    public ViolationSeverity Severity { get; set; }
    public ComplianceViolationType ViolationType { get; set; }
    public DateTime OccurredAt { get; set; }
}

/// <summary>
/// Domain event raised when a compliance violation is resolved.
/// </summary>
public class ComplianceViolationResolvedEvent
{
    public string ViolationId { get; set; } = string.Empty;
    public ResolutionMethod ResolutionMethod { get; set; }
    public DateTime ResolvedAt { get; set; }
}

#endregion