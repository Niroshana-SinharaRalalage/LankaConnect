using System;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.BuildingBlocks.Domain.Enums;
namespace LankaConnect.BuildingBlocks.Application.Common.Models.Routing;

public class GenerationalPatternAnalysis : LegacyBaseEntity
{
    public Guid AnalysisId { get; set; } = Guid.NewGuid();
    public int Generation { get; set; } = 1;
    public List<SouthAsianLanguage> PreferredLanguages { get; set; } = new();
    public Dictionary<string, decimal> CulturalRetention { get; set; } = new();
    public decimal LanguageShiftTrend { get; set; } = 0;
}