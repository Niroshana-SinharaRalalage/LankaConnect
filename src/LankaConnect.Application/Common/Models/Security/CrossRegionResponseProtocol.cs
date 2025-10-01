using System;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Models.Security;

/// <summary>
/// Cross-region response protocol for coordinated incident response
/// TDD Implementation: Ensures consistent response across regions
/// </summary>
public class CrossRegionResponseProtocol : BaseEntity
{
    public Guid ProtocolId { get; set; } = Guid.NewGuid();
    public string ProtocolName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<ResponseStep> Steps { get; set; } = new();
    public TimeSpan MaxResponseTime { get; set; } = TimeSpan.FromMinutes(30);
    public List<string> RequiredRoles { get; set; } = new();
    public Dictionary<string, string> RegionSpecificInstructions { get; set; } = new();
}

public class ResponseStep
{
    public int Order { get; set; }
    public string Description { get; set; } = string.Empty;
    public TimeSpan EstimatedDuration { get; set; }
    public bool IsRequired { get; set; } = true;
}