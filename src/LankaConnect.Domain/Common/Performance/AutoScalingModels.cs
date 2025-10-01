using LankaConnect.Domain.Common.ValueObjects;

namespace LankaConnect.Domain.Common.Performance;

/// <summary>
/// GREEN PHASE: Minimal implementation for AutoScalingDecision (26 references)
/// Cultural intelligence-aware auto-scaling decision for Sri Lankan diaspora platform
/// </summary>
public record AutoScalingDecision(
    ScaleDirection Direction,
    int RecommendedCapacity,
    IReadOnlyList<CulturalFactor> CulturalFactors,
    TimeSpan EstimatedDuration,
    double ConfidenceScore
)
{
    public static AutoScalingDecision Create(ScaleDirection direction, int recommendedCapacity, IReadOnlyList<CulturalFactor> culturalFactors, TimeSpan estimatedDuration, double confidenceScore)
    {
        if (recommendedCapacity <= 0)
            throw new ArgumentException("Recommended capacity must be positive");

        if (confidenceScore < 0.0 || confidenceScore > 1.0)
            throw new ArgumentException("Confidence score must be between 0.0 and 1.0");

        return new AutoScalingDecision(direction, recommendedCapacity, culturalFactors, estimatedDuration, confidenceScore);
    }

    /// <summary>
    /// Calculates overall cultural impact from all factors
    /// </summary>
    public double CalculateOverallCulturalImpact()
    {
        if (!CulturalFactors.Any()) return 0.0;
        return CulturalFactors.Average(f => f.Impact);
    }

    /// <summary>
    /// Gets the cultural event with highest impact
    /// </summary>
    public string GetHighestImpactCulturalEvent()
    {
        return CulturalFactors
            .OrderByDescending(f => f.Impact)
            .FirstOrDefault()?.Name ?? string.Empty;
    }

    /// <summary>
    /// Determines if emergency scaling is required based on cultural factors
    /// </summary>
    public bool RequiresEmergencyScaling()
    {
        return CulturalFactors.Any(f => f.Impact > 0.9) ||
               CalculateOverallCulturalImpact() > 0.85;
    }
}

/// <summary>
/// Cultural factor that influences auto-scaling decisions
/// </summary>
public record CulturalFactor(string Name, double Impact)
{
    public static CulturalFactor Create(string name, double impact)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Cultural factor name cannot be empty");

        if (impact < 0.0 || impact > 1.0)
            throw new ArgumentException("Impact must be between 0.0 and 1.0");

        return new CulturalFactor(name, impact);
    }

    public static CulturalFactor CreateFromSriLankanEvent(string eventName, double impact) =>
        new(eventName, Math.Clamp(impact, 0.0, 1.0));
}

/// <summary>
/// Direction for scaling operations
/// </summary>
public enum ScaleDirection
{
    Down,
    Maintain,
    Up
}