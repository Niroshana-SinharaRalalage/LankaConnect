using System;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Models.Performance;

public class CompetitivePerformanceAnalysis : BaseEntity
{
    public Guid AnalysisId { get; set; } = Guid.NewGuid();
    public CompetitiveBenchmarkData BenchmarkData { get; set; } = new();
    public MarketPositionAnalysis PositionAnalysis { get; set; } = new();
    public decimal PerformanceGap { get; set; } = 0;
}