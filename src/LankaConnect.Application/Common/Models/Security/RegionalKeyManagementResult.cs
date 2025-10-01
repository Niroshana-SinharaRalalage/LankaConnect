using System;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Models.Security;

public class RegionalKeyManagementResult : BaseEntity
{
    public Guid ResultId { get; set; } = Guid.NewGuid();
    public string RegionId { get; set; } = string.Empty;
    public bool Success { get; set; } = true;
    public List<string> Messages { get; set; } = new();
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}