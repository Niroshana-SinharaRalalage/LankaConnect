namespace LankaConnect.Domain.Business;

/// <summary>
/// Enum representing revenue protection strategies for the LankaConnect platform.
/// Used for cultural event management, disaster recovery, and performance optimization.
/// </summary>
public enum RevenueProtectionStrategy
{
    /// <summary>
    /// Emergency revenue protection for critical situations.
    /// Highest priority, applies to all scenarios including cultural events.
    /// </summary>
    Emergency = 0,

    /// <summary>
    /// Gradual service reduction to maintain revenue while managing load.
    /// Medium revenue impact with controlled degradation.
    /// </summary>
    GracefulDegradation = 1,

    /// <summary>
    /// Selective request dropping to protect core revenue streams.
    /// High revenue impact but protects essential services.
    /// </summary>
    LoadShedding = 2,

    /// <summary>
    /// Cultural event prioritization for diaspora communities.
    /// Specialized strategy for cultural intelligence platform features.
    /// </summary>
    CulturalPriority = 3,

    /// <summary>
    /// Planned maintenance protection with minimal revenue impact.
    /// Not applicable for cultural events, focuses on maintenance windows.
    /// </summary>
    MaintenanceWindow = 4,

    /// <summary>
    /// Revenue continuity during failover scenarios.
    /// Ensures business continuity during disaster recovery operations.
    /// </summary>
    DisasterRecovery = 5,

    /// <summary>
    /// Performance-based protection with minimal revenue impact.
    /// Optimizes for performance while maintaining revenue streams.
    /// </summary>
    PerformanceFirst = 6
}

/// <summary>
/// Extension methods for RevenueProtectionStrategy enum to provide business logic.
/// </summary>
public static class RevenueProtectionStrategyExtensions
{
    /// <summary>
    /// Gets the business description for the revenue protection strategy.
    /// </summary>
    /// <param name="strategy">The revenue protection strategy.</param>
    /// <returns>A description explaining the business context of the strategy.</returns>
    public static string GetDescription(this RevenueProtectionStrategy strategy)
    {
        return strategy switch
        {
            RevenueProtectionStrategy.Emergency => "Emergency revenue protection for critical situations",
            RevenueProtectionStrategy.GracefulDegradation => "Gradual service reduction to maintain revenue while managing load",
            RevenueProtectionStrategy.LoadShedding => "Selective request dropping to protect core revenue streams",
            RevenueProtectionStrategy.CulturalPriority => "Cultural event prioritization for diaspora communities",
            RevenueProtectionStrategy.MaintenanceWindow => "Planned maintenance protection with minimal revenue impact",
            RevenueProtectionStrategy.DisasterRecovery => "Revenue continuity during failover scenarios",
            RevenueProtectionStrategy.PerformanceFirst => "Performance-based protection with minimal revenue impact",
            _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, "Unknown revenue protection strategy")
        };
    }

    /// <summary>
    /// Gets the business priority level for the revenue protection strategy.
    /// Higher values indicate higher priority.
    /// </summary>
    /// <param name="strategy">The revenue protection strategy.</param>
    /// <returns>The priority level (0-100 scale).</returns>
    public static int GetPriority(this RevenueProtectionStrategy strategy)
    {
        return strategy switch
        {
            RevenueProtectionStrategy.Emergency => 100,
            RevenueProtectionStrategy.CulturalPriority => 80,
            RevenueProtectionStrategy.DisasterRecovery => 70,
            RevenueProtectionStrategy.LoadShedding => 60,
            RevenueProtectionStrategy.GracefulDegradation => 50,
            RevenueProtectionStrategy.PerformanceFirst => 40,
            RevenueProtectionStrategy.MaintenanceWindow => 30,
            _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, "Unknown revenue protection strategy")
        };
    }

    /// <summary>
    /// Determines if the strategy is applicable for cultural events.
    /// </summary>
    /// <param name="strategy">The revenue protection strategy.</param>
    /// <returns>True if the strategy applies to cultural events, false otherwise.</returns>
    public static bool IsApplicableForCulturalEvents(this RevenueProtectionStrategy strategy)
    {
        return strategy switch
        {
            RevenueProtectionStrategy.Emergency => true,        // Emergency applies to all scenarios
            RevenueProtectionStrategy.CulturalPriority => true, // Specifically for cultural events
            RevenueProtectionStrategy.GracefulDegradation => true, // Can be used for cultural load
            RevenueProtectionStrategy.LoadShedding => true,     // Can protect cultural services
            RevenueProtectionStrategy.DisasterRecovery => true, // Cultural continuity
            RevenueProtectionStrategy.PerformanceFirst => true, // Cultural performance optimization
            RevenueProtectionStrategy.MaintenanceWindow => false, // Not applicable during cultural events
            _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, "Unknown revenue protection strategy")
        };
    }

    /// <summary>
    /// Gets the revenue impact level assessment for the strategy.
    /// </summary>
    /// <param name="strategy">The revenue protection strategy.</param>
    /// <returns>The revenue impact level (Low, Medium, High).</returns>
    public static string GetRevenueImpactLevel(this RevenueProtectionStrategy strategy)
    {
        return strategy switch
        {
            RevenueProtectionStrategy.LoadShedding => "High",
            RevenueProtectionStrategy.Emergency => "High",
            RevenueProtectionStrategy.GracefulDegradation => "Medium",
            RevenueProtectionStrategy.DisasterRecovery => "Medium",
            RevenueProtectionStrategy.CulturalPriority => "Medium",
            RevenueProtectionStrategy.MaintenanceWindow => "Low",
            RevenueProtectionStrategy.PerformanceFirst => "Low",
            _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, "Unknown revenue protection strategy")
        };
    }
}