using System;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Models.Security;

/// <summary>
/// Result of cross-region incident response execution
/// TDD Implementation: Tracks response effectiveness and metrics
/// </summary>
public class CrossRegionIncidentResponseResult : BaseEntity
{
    public Guid ResponseId { get; set; } = Guid.NewGuid();
    public Guid IncidentId { get; set; }
    public Guid ProtocolId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public ResponseStatus Status { get; set; } = ResponseStatus.InProgress;
    public List<RegionResponseStatus> RegionResponses { get; set; } = new();
    public Dictionary<string, object> Metrics { get; set; } = new();
    public List<string> Lessons { get; set; } = new();
}

public class RegionResponseStatus
{
    public string RegionId { get; set; } = string.Empty;
    public ResponseStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<string> CompletedSteps { get; set; } = new();
}

public enum ResponseStatus
{
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5
}