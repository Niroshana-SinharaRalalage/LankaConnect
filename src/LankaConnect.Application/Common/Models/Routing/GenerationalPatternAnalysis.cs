using System;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.Models.Routing;

public class GenerationalPatternAnalysis : BaseEntity
{
    public Guid AnalysisId { get; set; } = Guid.NewGuid();
    public int Generation { get; set; } = 1;
    public List<SouthAsianLanguage> PreferredLanguages { get; set; } = new();
    public Dictionary<string, decimal> CulturalRetention { get; set; } = new();
    public decimal LanguageShiftTrend { get; set; } = 0;
}