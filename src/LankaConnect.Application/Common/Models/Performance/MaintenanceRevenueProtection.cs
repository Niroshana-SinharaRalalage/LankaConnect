using System;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Models.Performance;

/// <summary>
/// Maintenance revenue protection model
/// TDD Implementation: Protects revenue during maintenance windows
/// </summary>
public class MaintenanceRevenueProtection : BaseEntity
{
    public Guid ProtectionId { get; set; } = Guid.NewGuid();
    public Guid MaintenanceId { get; set; }
    public DateTime MaintenanceStart { get; set; }
    public DateTime MaintenanceEnd { get; set; }
    public List<ProtectionStrategy> Strategies { get; set; } = new();
    public decimal ExpectedRevenueLoss { get; set; } = 0;
    public decimal ActualRevenueLoss { get; set; } = 0;
    public decimal ProtectionEffectiveness => ExpectedRevenueLoss > 0 ?
        Math.Max(0, 1 - (ActualRevenueLoss / ExpectedRevenueLoss)) : 1;
}

public class ProtectionStrategy
{
    public string StrategyName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public StrategyType Type { get; set; } = StrategyType.TrafficRedirection;
    public bool IsEnabled { get; set; } = true;
    public decimal EffectivenessScore { get; set; } = 0;
}

public enum StrategyType
{
    TrafficRedirection = 1,
    CacheWarming = 2,
    GracefulDegradation = 3,
    BackupServices = 4,
    UserNotification = 5
}