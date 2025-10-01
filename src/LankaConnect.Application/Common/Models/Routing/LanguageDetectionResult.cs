using System;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Application.Common.Models.Routing;

public class LanguageDetectionResult : BaseEntity
{
    public Guid DetectionId { get; set; } = Guid.NewGuid();
    public SouthAsianLanguage PrimaryLanguage { get; set; } = SouthAsianLanguage.English;
    public decimal Confidence { get; set; } = 0.95m;
    public Dictionary<SouthAsianLanguage, decimal> AlternativeLanguages { get; set; } = new();
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
}