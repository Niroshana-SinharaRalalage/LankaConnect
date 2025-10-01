using System;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Models.Security;

public class RegionalKeyRotationSchedule : BaseEntity
{
    public Guid ScheduleId { get; set; } = Guid.NewGuid();
    public string RegionId { get; set; } = string.Empty;
    public TimeSpan RotationInterval { get; set; } = TimeSpan.FromDays(30);
    public DateTime NextRotation { get; set; } = DateTime.UtcNow.AddDays(30);
    public bool IsEnabled { get; set; } = true;
}