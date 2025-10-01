using System;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Models.Security;

public class DataSovereigntyRequirements : BaseEntity
{
    public Guid RequirementsId { get; set; } = Guid.NewGuid();
    public string Jurisdiction { get; set; } = string.Empty;
    public List<string> DataCategories { get; set; } = new();
    public Dictionary<string, string> StorageRequirements { get; set; } = new();
    public bool RequiresLocalStorage { get; set; } = true;
}