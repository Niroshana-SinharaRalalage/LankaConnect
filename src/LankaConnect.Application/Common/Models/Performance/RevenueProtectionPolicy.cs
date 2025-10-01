using System;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Notifications;

namespace LankaConnect.Application.Common.Models.Performance;

/// <summary>
/// Revenue protection policy for performance incident response
/// TDD Implementation: Defines policies for protecting revenue during incidents
/// </summary>
public class RevenueProtectionPolicy : BaseEntity
{
    public Guid PolicyId { get; set; } = Guid.NewGuid();
    public string PolicyName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ProtectionAction> Actions { get; set; } = new();
    public Dictionary<IncidentSeverity, List<string>> SeverityActions { get; set; } = new();
    public decimal MaxAcceptableRevenueLoss { get; set; } = 0;
    public TimeSpan MaxResponseTime { get; set; } = TimeSpan.FromMinutes(15);
    public bool AutomaticActivation { get; set; } = false;
}

public class ProtectionAction
{
    public string ActionName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ActionType Type { get; set; } = ActionType.Mitigation;
    public int Priority { get; set; } = 1;
    public TimeSpan ExecutionTime { get; set; } = TimeSpan.FromMinutes(5);
}

public enum ActionType
{
    Prevention = 1,
    Mitigation = 2,
    Recovery = 3,
    Notification = 4
}