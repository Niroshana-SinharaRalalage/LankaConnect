using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Communications.ValueObjects;

namespace LankaConnect.Domain.Common.Security;

/// <summary>
/// Domain entity representing security violations in the cultural intelligence platform
/// Follows Clean Architecture principles as a core business concept
/// Critical for SOC2, ISO27001, and cultural data protection requirements
/// </summary>
public class SecurityViolation : Entity<string>
{
    public required SecurityViolationType ViolationType { get; set; }
    public required SecuritySeverityLevel Severity { get; set; }
    public required string Description { get; set; }
    public required DateTime OccurredAt { get; set; }
    public required string AffectedResource { get; set; }
    public required string AffectedRegion { get; set; }
    public required CulturalContext CulturalContext { get; set; }
    public required SecurityViolationStatus Status { get; set; }
    public required List<SecurityViolationEvidence> Evidence { get; set; }
    public required SecurityRemediationPlan RemediationPlan { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedBy { get; set; }
    public string? ResolutionNotes { get; set; }
    public required List<string> AffectedCulturalGroups { get; set; }
    public Dictionary<string, object> ViolationMetadata { get; set; } = new();
    public bool RequiresRegulatoryCommunication { get; set; }
    public List<ComplianceRequirement> TriggeredComplianceRequirements { get; set; } = new();

    private SecurityViolation() : base(string.Empty) { }

    public SecurityViolation(string violationId) : base(violationId)
    {
        Evidence = new List<SecurityViolationEvidence>();
        AffectedCulturalGroups = new List<string>();
        ViolationMetadata = new Dictionary<string, object>();
        TriggeredComplianceRequirements = new List<ComplianceRequirement>();
    }

    public bool IsResolved => Status == SecurityViolationStatus.Resolved && ResolvedAt.HasValue;

    public bool IsCritical => Severity == SecuritySeverityLevel.Critical || Severity == SecuritySeverityLevel.Emergency;

    public TimeSpan? ResolutionTime => ResolvedAt.HasValue ? ResolvedAt.Value - OccurredAt : null;
}

// Supporting types that should also be in Domain layer
public class SecurityViolationEvidence
{
    public required string EvidenceId { get; set; }
    public required SecurityEvidenceType EvidenceType { get; set; }
    public required string Description { get; set; }
    public required DateTime CollectedAt { get; set; }
    public required string CollectedBy { get; set; }
    public byte[]? EvidenceData { get; set; }
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public class SecurityRemediationPlan
{
    public required string PlanId { get; set; }
    public required List<SecurityRemediationStep> Steps { get; set; }
    public required DateTime PlannedStartDate { get; set; }
    public required DateTime PlannedEndDate { get; set; }
    public required string ResponsibleParty { get; set; }
    public SecurityRemediationStatus Status { get; set; }
}

public class SecurityRemediationStep
{
    public required string StepId { get; set; }
    public required string Description { get; set; }
    public required DateTime PlannedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public required string ResponsibleParty { get; set; }
    public SecurityRemediationStepStatus Status { get; set; }
}

// Supporting enums
public enum SecurityViolationType
{
    Unauthorized_Access = 1,
    Data_Breach = 2,
    Policy_Violation = 3,
    Cultural_Content_Misuse = 4,
    Authentication_Failure = 5,
    Authorization_Bypass = 6,
    Data_Corruption = 7,
    System_Compromise = 8
}

public enum SecuritySeverityLevel
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4,
    Emergency = 5
}

public enum SecurityViolationStatus
{
    Open = 1,
    Investigating = 2,
    Remediating = 3,
    Resolved = 4,
    Closed = 5
}

public enum SecurityEvidenceType
{
    Log_Entry = 1,
    Screenshot = 2,
    Network_Capture = 3,
    File_Artifact = 4,
    Witness_Statement = 5,
    System_Report = 6
}

public enum SecurityRemediationStatus
{
    Planning = 1,
    In_Progress = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5
}

public enum SecurityRemediationStepStatus
{
    Pending = 1,
    In_Progress = 2,
    Completed = 3,
    Failed = 4,
    Skipped = 5
}

public class ComplianceRequirement
{
    public required string RequirementId { get; set; }
    public required string RequirementName { get; set; }
    public required string Standard { get; set; } // e.g., "SOC2", "ISO27001", "GDPR"
    public required string Description { get; set; }
    public required ComplianceRequirementSeverity Severity { get; set; }
}

public enum ComplianceRequirementSeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}