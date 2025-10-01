using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Enterprise.ValueObjects;

public class CapacityAssessment : ValueObject
{
    public EnterpriseClientId ClientId { get; private set; }
    public DateTime AssessmentDate { get; private set; }
    public int CurrentCapacity { get; private set; }
    public int MaxCapacity { get; private set; }
    public int RecommendedCapacity { get; private set; }
    public double UtilizationPercentage { get; private set; }
    public TimeSpan ProjectedTimeToCapacity { get; private set; }
    public IReadOnlyList<string> CapacityConstraints { get; private set; }
    public IReadOnlyList<string> ScalingRecommendations { get; private set; }
    public IReadOnlyDictionary<string, int> ResourceBreakdown { get; private set; }
    public double GrowthRate { get; private set; }
    public string RiskLevel { get; private set; }
    public bool NeedsImmediateAttention => UtilizationPercentage > 85 || RiskLevel == "High";

    private CapacityAssessment(
        EnterpriseClientId clientId,
        DateTime assessmentDate,
        int currentCapacity,
        int maxCapacity,
        int recommendedCapacity,
        double utilizationPercentage,
        TimeSpan projectedTimeToCapacity,
        IReadOnlyList<string> capacityConstraints,
        IReadOnlyList<string> scalingRecommendations,
        IReadOnlyDictionary<string, int> resourceBreakdown,
        double growthRate,
        string riskLevel)
    {
        ClientId = clientId;
        AssessmentDate = assessmentDate;
        CurrentCapacity = currentCapacity;
        MaxCapacity = maxCapacity;
        RecommendedCapacity = recommendedCapacity;
        UtilizationPercentage = utilizationPercentage;
        ProjectedTimeToCapacity = projectedTimeToCapacity;
        CapacityConstraints = capacityConstraints;
        ScalingRecommendations = scalingRecommendations;
        ResourceBreakdown = resourceBreakdown;
        GrowthRate = growthRate;
        RiskLevel = riskLevel;
    }

    public static CapacityAssessment Create(
        EnterpriseClientId clientId,
        DateTime assessmentDate,
        int currentCapacity,
        int maxCapacity,
        int recommendedCapacity,
        double utilizationPercentage,
        TimeSpan projectedTimeToCapacity,
        IEnumerable<string> capacityConstraints,
        IEnumerable<string> scalingRecommendations,
        IReadOnlyDictionary<string, int> resourceBreakdown,
        double growthRate,
        string riskLevel)
    {
        if (clientId == null) throw new ArgumentNullException(nameof(clientId));
        if (assessmentDate > DateTime.UtcNow) throw new ArgumentException("Assessment date cannot be in the future", nameof(assessmentDate));
        if (currentCapacity < 0) throw new ArgumentException("Current capacity cannot be negative", nameof(currentCapacity));
        if (maxCapacity <= 0) throw new ArgumentException("Max capacity must be positive", nameof(maxCapacity));
        if (recommendedCapacity <= 0) throw new ArgumentException("Recommended capacity must be positive", nameof(recommendedCapacity));
        if (currentCapacity > maxCapacity) throw new ArgumentException("Current capacity cannot exceed max capacity", nameof(currentCapacity));
        if (utilizationPercentage < 0 || utilizationPercentage > 100) throw new ArgumentException("Utilization percentage must be between 0 and 100", nameof(utilizationPercentage));
        if (projectedTimeToCapacity < TimeSpan.Zero) throw new ArgumentException("Projected time to capacity cannot be negative", nameof(projectedTimeToCapacity));
        if (resourceBreakdown == null || !resourceBreakdown.Any()) throw new ArgumentException("Resource breakdown is required", nameof(resourceBreakdown));
        if (growthRate < 0) throw new ArgumentException("Growth rate cannot be negative", nameof(growthRate));
        if (string.IsNullOrWhiteSpace(riskLevel)) throw new ArgumentException("Risk level is required", nameof(riskLevel));

        var constraintsList = capacityConstraints?.ToList() ?? new List<string>();
        var recommendationsList = scalingRecommendations?.ToList() ?? new List<string>();

        // Validate risk level
        var validRiskLevels = new[] { "Low", "Medium", "High", "Critical" };
        if (!validRiskLevels.Contains(riskLevel))
            throw new ArgumentException($"Risk level must be one of: {string.Join(", ", validRiskLevels)}", nameof(riskLevel));

        return new CapacityAssessment(
            clientId,
            assessmentDate,
            currentCapacity,
            maxCapacity,
            recommendedCapacity,
            utilizationPercentage,
            projectedTimeToCapacity,
            constraintsList.AsReadOnly(),
            recommendationsList.AsReadOnly(),
            resourceBreakdown,
            growthRate,
            riskLevel);
    }

    public int GetAvailableCapacity() => MaxCapacity - CurrentCapacity;

    public TimeSpan GetEstimatedScalingTime()
    {
        // Estimate scaling time based on utilization and growth rate
        if (UtilizationPercentage < 50) return TimeSpan.FromDays(30);
        if (UtilizationPercentage < 75) return TimeSpan.FromDays(14);
        if (UtilizationPercentage < 85) return TimeSpan.FromDays(7);
        return TimeSpan.FromDays(1); // Immediate attention needed
    }

    public string GetCapacityStatus()
    {
        return UtilizationPercentage switch
        {
            >= 95 => "Critical - Immediate scaling required",
            >= 85 => "High - Scaling recommended within 7 days",
            >= 75 => "Medium - Scaling recommended within 2 weeks",
            >= 50 => "Normal - Monitor growth trends",
            _ => "Low - Sufficient capacity available"
        };
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return ClientId;
        yield return AssessmentDate;
        yield return CurrentCapacity;
        yield return MaxCapacity;
        yield return RecommendedCapacity;
        yield return UtilizationPercentage;
        yield return ProjectedTimeToCapacity;
        yield return GrowthRate;
        yield return RiskLevel;
        
        foreach (var constraint in CapacityConstraints)
            yield return constraint;
        
        foreach (var recommendation in ScalingRecommendations)
            yield return recommendation;
        
        foreach (var resource in ResourceBreakdown.OrderBy(x => x.Key))
        {
            yield return resource.Key;
            yield return resource.Value;
        }
    }
}