using System;
using System.Collections.Generic;
using System.Linq;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.Notifications;

namespace LankaConnect.Domain.Common.Security;

/// <summary>
/// Security Incident for Cultural Intelligence Platform
/// Represents security incidents with cultural awareness and diaspora community protection
/// Created during emergency session to resolve 20 missing type errors
/// </summary>
public class SecurityIncident
{
    public string IncidentId { get; private set; }
    public SecurityIncidentType IncidentType { get; private set; }
    public IncidentSeverity Severity { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public DateTime DetectedAt { get; private set; }
    public IncidentStatus Status { get; private set; }
    public string Description { get; private set; }
    public string AffectedSystem { get; private set; }
    public IReadOnlyList<string> AffectedRegions { get; private set; }
    public bool InvolvesCulturalData { get; private set; }
    public bool InvolvesSacredContent { get; private set; }
    public int AffectedDiasporaCommunities { get; private set; }
    public IReadOnlyList<string> ImpactedServices { get; private set; }
    public SecurityIncidentMetadata Metadata { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public string? ResolutionNotes { get; private set; }

    private SecurityIncident(
        string incidentId,
        SecurityIncidentType incidentType,
        IncidentSeverity severity,
        DateTime occurredAt,
        DateTime detectedAt,
        string description,
        string affectedSystem,
        IEnumerable<string> affectedRegions,
        bool involvesCulturalData,
        bool involvesSacredContent,
        int affectedDiasporaCommunities,
        IEnumerable<string> impactedServices,
        SecurityIncidentMetadata? metadata)
    {
        if (string.IsNullOrWhiteSpace(incidentId))
            throw new ArgumentException("Incident ID cannot be null or empty", nameof(incidentId));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be null or empty", nameof(description));

        IncidentId = incidentId;
        IncidentType = incidentType;
        Severity = severity;
        OccurredAt = occurredAt;
        DetectedAt = detectedAt;
        Status = IncidentStatus.Detected;
        Description = description;
        AffectedSystem = affectedSystem ?? string.Empty;
        AffectedRegions = affectedRegions.ToList().AsReadOnly();
        InvolvesCulturalData = involvesCulturalData;
        InvolvesSacredContent = involvesSacredContent;
        AffectedDiasporaCommunities = affectedDiasporaCommunities;
        ImpactedServices = impactedServices.ToList().AsReadOnly();
        Metadata = metadata ?? new SecurityIncidentMetadata();
        ResolvedAt = null;
        ResolutionNotes = null;
    }

    /// <summary>
    /// Creates a new security incident
    /// </summary>
    public static Result<SecurityIncident> Create(
        SecurityIncidentType incidentType,
        IncidentSeverity severity,
        string description,
        string affectedSystem,
        IEnumerable<string> affectedRegions,
        bool involvesCulturalData = false,
        bool involvesSacredContent = false,
        int affectedDiasporaCommunities = 0,
        IEnumerable<string>? impactedServices = null,
        SecurityIncidentMetadata? metadata = null)
    {
        try
        {
            var incidentId = GenerateIncidentId(incidentType, severity);
            var now = DateTime.UtcNow;

            var incident = new SecurityIncident(
                incidentId,
                incidentType,
                severity,
                now,
                now,
                description,
                affectedSystem,
                affectedRegions ?? Enumerable.Empty<string>(),
                involvesCulturalData,
                involvesSacredContent,
                affectedDiasporaCommunities,
                impactedServices ?? Enumerable.Empty<string>(),
                metadata);

            return Result<SecurityIncident>.Success(incident);
        }
        catch (Exception ex)
        {
            return Result<SecurityIncident>.Failure($"Failed to create security incident: {ex.Message}");
        }
    }

    /// <summary>
    /// Marks the incident as contained
    /// </summary>
    public Result Contain(string containmentNotes)
    {
        if (Status == IncidentStatus.Resolved)
            return Result.Failure("Cannot contain a resolved incident");

        Status = IncidentStatus.Contained;
        Metadata = Metadata.WithContainmentTime(DateTime.UtcNow);

        return Result.Success();
    }

    /// <summary>
    /// Marks the incident as under investigation
    /// </summary>
    public Result Investigate(string investigationNotes)
    {
        if (Status == IncidentStatus.Resolved)
            return Result.Failure("Cannot investigate a resolved incident");

        Status = IncidentStatus.UnderInvestigation;

        return Result.Success();
    }

    /// <summary>
    /// Marks the incident as mitigated
    /// </summary>
    public Result Mitigate(string mitigationNotes)
    {
        if (Status == IncidentStatus.Resolved)
            return Result.Failure("Incident already resolved");

        Status = IncidentStatus.Mitigated;

        return Result.Success();
    }

    /// <summary>
    /// Resolves the incident
    /// </summary>
    public Result Resolve(string resolutionNotes)
    {
        if (Status == IncidentStatus.Detected)
            return Result.Failure("Incident must be contained before resolution");

        Status = IncidentStatus.Resolved;
        ResolvedAt = DateTime.UtcNow;
        ResolutionNotes = resolutionNotes;

        return Result.Success();
    }

    /// <summary>
    /// Escalates the incident severity
    /// </summary>
    public Result Escalate(IncidentSeverity newSeverity, string escalationReason)
    {
        if (newSeverity <= Severity)
            return Result.Failure("New severity must be higher than current severity");

        if (Status == IncidentStatus.Resolved)
            return Result.Failure("Cannot escalate a resolved incident");

        Severity = newSeverity;
        Metadata = Metadata.WithEscalation(newSeverity, escalationReason, DateTime.UtcNow);

        return Result.Success();
    }

    /// <summary>
    /// Calculates the cultural impact score (0-100)
    /// </summary>
    public double CalculateCulturalImpactScore()
    {
        var score = 0.0;

        // Base severity scoring (40 points)
        score += Severity switch
        {
            IncidentSeverity.Critical => 40.0,
            IncidentSeverity.High => 30.0,
            IncidentSeverity.Medium => 20.0,
            IncidentSeverity.Low => 10.0,
            _ => 0.0
        };

        // Cultural data involvement (30 points)
        if (InvolvesCulturalData) score += 15.0;
        if (InvolvesSacredContent) score += 15.0;

        // Diaspora community impact (20 points)
        score += Math.Min(20.0, AffectedDiasporaCommunities * 2.0);

        // Multi-region impact (10 points)
        score += Math.Min(10.0, AffectedRegions.Count * 2.0);

        return Math.Min(100.0, score);
    }

    /// <summary>
    /// Determines if immediate containment is required
    /// </summary>
    public bool RequiresImmediateContainment()
    {
        return Severity >= IncidentSeverity.High ||
               InvolvesSacredContent ||
               AffectedDiasporaCommunities > 5 ||
               AffectedRegions.Count > 3;
    }

    /// <summary>
    /// Determines if religious authorities should be notified
    /// </summary>
    public bool RequiresReligiousAuthorityNotification()
    {
        return InvolvesSacredContent ||
               (InvolvesCulturalData && Severity >= IncidentSeverity.High);
    }

    /// <summary>
    /// Gets the response time requirement based on severity
    /// </summary>
    public TimeSpan GetResponseTimeRequirement()
    {
        return Severity switch
        {
            IncidentSeverity.Critical => TimeSpan.FromMinutes(5),
            IncidentSeverity.High => TimeSpan.FromMinutes(15),
            IncidentSeverity.Medium => TimeSpan.FromMinutes(30),
            IncidentSeverity.Low => TimeSpan.FromHours(1),
            _ => TimeSpan.FromHours(4)
        };
    }

    private static string GenerateIncidentId(SecurityIncidentType type, IncidentSeverity severity)
    {
        var typePrefix = type switch
        {
            SecurityIncidentType.DataBreach => "DB",
            SecurityIncidentType.CulturalDataBreach => "CDB",
            SecurityIncidentType.SystemIntrusion => "SI",
            SecurityIncidentType.CulturalIntelligenceSystemBreach => "CISB",
            SecurityIncidentType.DiasporaDataCompromise => "DDC",
            SecurityIncidentType.CrossBorderSecurityViolation => "CBSV",
            _ => "GEN"
        };

        var severityCode = severity switch
        {
            IncidentSeverity.Critical => "C",
            IncidentSeverity.High => "H",
            IncidentSeverity.Medium => "M",
            IncidentSeverity.Low => "L",
            _ => "U"
        };

        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var random = Guid.NewGuid().ToString("N").Substring(0, 4).ToUpper();

        return $"{typePrefix}-{severityCode}-{timestamp}-{random}";
    }
}

/// <summary>
/// Security incident metadata
/// </summary>
public class SecurityIncidentMetadata
{
    public string DetectionSource { get; set; } = "Automated";
    public string? DetectionMethod { get; set; }
    public IReadOnlyList<string> SecurityTags { get; set; } = new List<string>().AsReadOnly();
    public DateTime? ContainmentTime { get; set; }
    public IReadOnlyList<EscalationRecord> EscalationHistory { get; set; } = new List<EscalationRecord>().AsReadOnly();
    public Dictionary<string, string> AdditionalProperties { get; set; } = new Dictionary<string, string>();

    public SecurityIncidentMetadata WithContainmentTime(DateTime time)
    {
        return new SecurityIncidentMetadata
        {
            DetectionSource = this.DetectionSource,
            DetectionMethod = this.DetectionMethod,
            SecurityTags = this.SecurityTags,
            ContainmentTime = time,
            EscalationHistory = this.EscalationHistory,
            AdditionalProperties = new Dictionary<string, string>(this.AdditionalProperties)
        };
    }

    public SecurityIncidentMetadata WithEscalation(IncidentSeverity newSeverity, string reason, DateTime when)
    {
        var escalations = EscalationHistory.ToList();
        escalations.Add(new EscalationRecord(newSeverity, reason, when));

        return new SecurityIncidentMetadata
        {
            DetectionSource = this.DetectionSource,
            DetectionMethod = this.DetectionMethod,
            SecurityTags = this.SecurityTags,
            ContainmentTime = this.ContainmentTime,
            EscalationHistory = escalations.AsReadOnly(),
            AdditionalProperties = new Dictionary<string, string>(this.AdditionalProperties)
        };
    }
}

/// <summary>
/// Escalation record for incident history
/// </summary>
public record EscalationRecord(
    IncidentSeverity NewSeverity,
    string Reason,
    DateTime EscalatedAt
);

/// <summary>
/// Security incident types specific to cultural intelligence platform
/// </summary>
public enum SecurityIncidentType
{
    DataBreach,
    CulturalDataBreach,
    SystemIntrusion,
    CulturalIntelligenceSystemBreach,
    DiasporaDataCompromise,
    CrossBorderSecurityViolation,
    UnauthorizedAccess,
    DataCorruption,
    ServiceDenial
}

/// <summary>
/// Security incident status
/// </summary>
public enum IncidentStatus
{
    Detected,
    Acknowledged,
    Contained,
    UnderInvestigation,
    Mitigated,
    Resolved,
    Closed
}
