using System;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.Models.Security;

/// <summary>
/// Multi-region security incident model for cross-region incident response
/// TDD Implementation: Supports Fortune 500 security requirements
/// </summary>
public class MultiRegionIncident : BaseEntity
{
    public Guid IncidentId { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SecuritySeverity Severity { get; set; } = SecuritySeverity.Medium;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public List<string> AffectedRegions { get; set; } = new();
    public IncidentStatus Status { get; set; } = IncidentStatus.Open;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public enum SecuritySeverity
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum IncidentStatus
{
    Open = 1,
    InProgress = 2,
    Resolved = 3,
    Closed = 4
}