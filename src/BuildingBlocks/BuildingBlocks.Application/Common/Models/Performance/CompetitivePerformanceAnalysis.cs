using System;
using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.BuildingBlocks.Application.Common.Models.Performance;

public class CompetitivePerformanceAnalysis : LegacyBaseEntity
{
    public Guid AnalysisId { get; set; } = Guid.NewGuid();
    public CompetitiveBenchmarkData BenchmarkData { get; set; } = new();
    public MarketPositionAnalysis PositionAnalysis { get; set; } = new();
    public decimal PerformanceGap { get; set; } = 0;
}