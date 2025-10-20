using System;
using System.Collections.Generic;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Infrastructure.DisasterRecovery;

/// <summary>
/// Disaster Recovery Models - Infrastructure Layer
/// Moved from Stage5MissingTypes.cs to correct architectural layer
/// </summary>

/// <summary>
/// Critical types for emergency disaster recovery operations
/// </summary>
public class CriticalTypes
{
    public string TypeId { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public CriticalityLevel CriticalityLevel { get; set; }
    public DateTime IdentifiedAt { get; set; }
}

/// <summary>
/// Disaster recovery result for failover operations
/// </summary>
public class DisasterRecoveryResult
{
    public string ResultId { get; set; } = string.Empty;
    public bool IsRecoverySuccessful { get; set; }
    public TimeSpan RecoveryTime { get; set; }
    public List<string> RecoveredServices { get; set; } = new();
    public double DataIntegrityScore { get; set; }
}

/// <summary>
/// Criticality level for disaster recovery prioritization
/// </summary>
public enum CriticalityLevel
{
    Low,
    Medium,
    High,
    Critical,
    Emergency
}
