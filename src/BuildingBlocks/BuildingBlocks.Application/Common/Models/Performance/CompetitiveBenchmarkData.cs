using System;
using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.BuildingBlocks.Application.Common.Models.Performance;

public class CompetitiveBenchmarkData : LegacyBaseEntity
{
    public Guid BenchmarkId { get; set; } = Guid.NewGuid();
    public string CompetitorName { get; set; } = string.Empty;
    public Dictionary<string, decimal> Metrics { get; set; } = new();
    public DateTime BenchmarkDate { get; set; } = DateTime.UtcNow;
}