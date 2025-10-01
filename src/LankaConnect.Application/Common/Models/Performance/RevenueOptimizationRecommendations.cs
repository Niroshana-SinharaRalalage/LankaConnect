using System;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Common.Models.Performance;

/// <summary>
/// Revenue optimization recommendations model
/// TDD Implementation: Provides AI-driven recommendations for revenue optimization
/// </summary>
public class RevenueOptimizationRecommendations : BaseEntity
{
    public Guid RecommendationsId { get; set; } = Guid.NewGuid();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public List<OptimizationRecommendation> Recommendations { get; set; } = new();
    public decimal ExpectedRevenueImpact { get; set; } = 0;
    public decimal ImplementationCost { get; set; } = 0;
    public decimal ROI => ImplementationCost > 0 ? ExpectedRevenueImpact / ImplementationCost : 0;
    public RecommendationPriority Priority { get; set; } = RecommendationPriority.Medium;
    public TimeSpan ImplementationTime { get; set; } = TimeSpan.FromDays(30);
}

public class OptimizationRecommendation
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public RecommendationType Type { get; set; } = RecommendationType.Performance;
    public decimal Impact { get; set; } = 0;
    public decimal Effort { get; set; } = 0;
    public List<string> Requirements { get; set; } = new();
}

public enum RecommendationType
{
    Performance = 1,
    Infrastructure = 2,
    Pricing = 3,
    Features = 4,
    UserExperience = 5
}

public enum RecommendationPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}