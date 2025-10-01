using System;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.Models.Security;

/// <summary>
/// Key distribution policy for multi-region security management
/// TDD Implementation: Ensures secure key distribution across regions
/// </summary>
public class KeyDistributionPolicy : BaseEntity
{
    public Guid PolicyId { get; set; } = Guid.NewGuid();
    public string PolicyName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public KeyDistributionMethod Method { get; set; } = KeyDistributionMethod.Hierarchical;
    public TimeSpan RotationInterval { get; set; } = TimeSpan.FromDays(30);
    public List<string> AuthorizedRegions { get; set; } = new();
    public Dictionary<string, KeyPermissions> RegionPermissions { get; set; } = new();
    public bool RequireMultiSignature { get; set; } = true;
    public int MinimumSignatures { get; set; } = 2;
}

public class KeyPermissions
{
    public bool CanRead { get; set; } = true;
    public bool CanWrite { get; set; } = false;
    public bool CanRotate { get; set; } = false;
    public bool CanRevoke { get; set; } = false;
    public List<string> AllowedOperations { get; set; } = new();
}

public enum KeyDistributionMethod
{
    Hierarchical = 1,
    Mesh = 2,
    HubAndSpoke = 3,
    Federation = 4
}