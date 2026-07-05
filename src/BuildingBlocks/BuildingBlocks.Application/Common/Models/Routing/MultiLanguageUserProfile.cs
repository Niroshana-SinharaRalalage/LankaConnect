using System;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.BuildingBlocks.Domain.Enums;
namespace LankaConnect.BuildingBlocks.Application.Common.Models.Routing;

public class MultiLanguageUserProfile : LegacyBaseEntity
{
    public Guid UserId { get; set; } = Guid.NewGuid();
    public List<SouthAsianLanguage> PreferredLanguages { get; set; } = new();
    public Dictionary<SouthAsianLanguage, decimal> LanguageProficiency { get; set; } = new();
    public string CulturalBackground { get; set; } = string.Empty;
    public int Generation { get; set; } = 1;
}