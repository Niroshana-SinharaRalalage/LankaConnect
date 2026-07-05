using System;
using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.BuildingBlocks.Application.Common.Models.Security;

public class DataSovereigntyRequirements : LegacyBaseEntity
{
    public Guid RequirementsId { get; set; } = Guid.NewGuid();
    public string Jurisdiction { get; set; } = string.Empty;
    public List<string> DataCategories { get; set; } = new();
    public Dictionary<string, string> StorageRequirements { get; set; } = new();
    public bool RequiresLocalStorage { get; set; } = true;
}