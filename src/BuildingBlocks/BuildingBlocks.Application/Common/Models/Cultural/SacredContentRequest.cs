using System;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.BuildingBlocks.Domain.Enums;
namespace LankaConnect.BuildingBlocks.Application.Common.Models.Cultural;

public class SacredContentRequest : LegacyBaseEntity
{
    public Guid RequestId { get; set; } = Guid.NewGuid();
    public string ContentText { get; set; } = string.Empty;
    public SouthAsianLanguage RequestedLanguage { get; set; } = SouthAsianLanguage.English;
    public string ReligiousContext { get; set; } = string.Empty;
    public string CulturalContext { get; set; } = string.Empty;
}