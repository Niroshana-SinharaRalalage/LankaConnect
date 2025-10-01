using System;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Notifications;

namespace LankaConnect.Application.Common.Models.Performance;

/// <summary>
/// Performance incident model for tracking and response
/// TDD Implementation: Tracks performance incidents and their revenue impact
/// </summary>
public class PerformanceIncident : BaseEntity
{
    public Guid IncidentId { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public IncidentSeverity Severity { get; set; } = IncidentSeverity.Medium;
    public IncidentStatus Status { get; set; } = IncidentStatus.Open;
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public List<string> AffectedServices { get; set; } = new();
    public decimal EstimatedRevenueLoss { get; set; } = 0;
    public Dictionary<string, object> Metrics { get; set; } = new();
}

// Note: IncidentSeverity enum moved to canonical location: LankaConnect.Domain.Common.Notifications.IncidentSeverity
// Use Domain enum instead for consistency

public enum IncidentStatus
{
    Open = 1,
    InProgress = 2,
    Resolved = 3,
    Closed = 4
}