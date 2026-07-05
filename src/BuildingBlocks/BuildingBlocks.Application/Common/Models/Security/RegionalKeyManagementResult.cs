using System;
using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.BuildingBlocks.Application.Common.Models.Security;

public class RegionalKeyManagementResult : LegacyBaseEntity
{
    public Guid ResultId { get; set; } = Guid.NewGuid();
    public string RegionId { get; set; } = string.Empty;
    public bool Success { get; set; } = true;
    public List<string> Messages { get; set; } = new();
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
}