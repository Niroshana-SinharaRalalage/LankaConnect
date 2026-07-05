using System;
using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.BuildingBlocks.Application.Common.Models.Security;

public class RegionalKeyRotationSchedule : LegacyBaseEntity
{
    public Guid ScheduleId { get; set; } = Guid.NewGuid();
    public string RegionId { get; set; } = string.Empty;
    public TimeSpan RotationInterval { get; set; } = TimeSpan.FromDays(30);
    public DateTime NextRotation { get; set; } = DateTime.UtcNow.AddDays(30);
    public bool IsEnabled { get; set; } = true;
}