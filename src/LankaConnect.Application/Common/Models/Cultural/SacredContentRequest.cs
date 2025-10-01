using System;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.Models.Cultural;

public class SacredContentRequest : BaseEntity
{
    public Guid RequestId { get; set; } = Guid.NewGuid();
    public string ContentText { get; set; } = string.Empty;
    public SouthAsianLanguage RequestedLanguage { get; set; } = SouthAsianLanguage.English;
    public string ReligiousContext { get; set; } = string.Empty;
    public string CulturalContext { get; set; } = string.Empty;
}