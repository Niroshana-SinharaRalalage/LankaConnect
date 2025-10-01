using System;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Models.Performance;

public class MarketPositionAnalysis : BaseEntity
{
    public Guid AnalysisId { get; set; } = Guid.NewGuid();
    public decimal MarketShare { get; set; } = 0;
    public int MarketRank { get; set; } = 1;
    public List<string> CompetitiveAdvantages { get; set; } = new();
}