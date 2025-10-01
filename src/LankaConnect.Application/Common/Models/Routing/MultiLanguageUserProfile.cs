using System;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.Models.Routing;

public class MultiLanguageUserProfile : BaseEntity
{
    public Guid UserId { get; set; } = Guid.NewGuid();
    public List<SouthAsianLanguage> PreferredLanguages { get; set; } = new();
    public Dictionary<SouthAsianLanguage, decimal> LanguageProficiency { get; set; } = new();
    public string CulturalBackground { get; set; } = string.Empty;
    public int Generation { get; set; } = 1;
}