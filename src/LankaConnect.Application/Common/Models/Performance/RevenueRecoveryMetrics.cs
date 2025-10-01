using System;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Models.Performance;

/// <summary>
/// Revenue recovery metrics model for performance incident tracking
/// TDD Implementation: Tracks revenue recovery after performance incidents
/// </summary>
public class RevenueRecoveryMetrics : BaseEntity
{
    public Guid MetricsId { get; set; } = Guid.NewGuid();
    public Guid IncidentId { get; set; }
    public decimal PreIncidentRevenue { get; set; } = 0;
    public decimal IncidentRevenueLoss { get; set; } = 0;
    public decimal PostIncidentRevenue { get; set; } = 0;
    public decimal RecoveryPercentage { get; set; } = 0;
    public TimeSpan RecoveryTime { get; set; } = TimeSpan.Zero;
    public Dictionary<string, decimal> RecoveryBySegment { get; set; } = new();
    public List<RecoveryMilestone> Milestones { get; set; } = new();
}

public class RecoveryMilestone
{
    public DateTime Timestamp { get; set; }
    public decimal RecoveryPercentage { get; set; }
    public string Description { get; set; } = string.Empty;
}