using System;
using System.Collections.Generic;
using System.Linq;

namespace LankaConnect.Domain.Common.Database;

/// <summary>
/// Enterprise-grade load balancing configuration with cultural intelligence awareness
/// for LankaConnect's distributed diaspora platform. Provides comprehensive load balancing
/// capabilities with cultural event optimization and disaster recovery integration.
/// </summary>
public sealed class LoadBalancingConfiguration : IEquatable<LoadBalancingConfiguration>
{
    /// <summary>
    /// Gets the unique configuration identifier.
    /// </summary>
    public string ConfigurationId { get; }

    /// <summary>
    /// Gets the load balancing strategy.
    /// </summary>
    public LoadBalancingStrategy Strategy { get; }

    /// <summary>
    /// Gets the health check interval.
    /// </summary>
    public TimeSpan HealthCheckInterval { get; }

    /// <summary>
    /// Gets the maximum retry attempts.
    /// </summary>
    public int MaxRetries { get; }

    /// <summary>
    /// Gets the cultural context for intelligent routing.
    /// </summary>
    public string? CulturalContext { get; }

    /// <summary>
    /// Gets the cultural profiles for event-specific load balancing.
    /// </summary>
    public Dictionary<string, CulturalLoadBalancingProfile> CulturalProfiles { get; private set; }

    /// <summary>
    /// Gets the disaster recovery integration configuration.
    /// </summary>
    public DisasterRecoveryLoadBalancingConfiguration? DisasterRecovery { get; private set; }

    /// <summary>
    /// Gets the revenue protection configuration.
    /// </summary>
    public RevenueProtectionLoadBalancingConfiguration? RevenueProtection { get; private set; }

    /// <summary>
    /// Gets whether this configuration is enterprise-grade (30s or less health checks).
    /// </summary>
    public bool IsEnterpriseGrade => HealthCheckInterval <= TimeSpan.FromSeconds(30) && MaxRetries >= 3;

    /// <summary>
    /// Gets whether this configuration is culturally aware.
    /// </summary>
    public bool IsCulturallyAware => Strategy == LoadBalancingStrategy.CulturalAware ||
                                     Strategy == LoadBalancingStrategy.DiasporaOptimized;

    /// <summary>
    /// Gets whether this configuration has cultural profiles.
    /// </summary>
    public bool HasCulturalProfiles => CulturalProfiles.Any();

    /// <summary>
    /// Gets whether disaster recovery integration is enabled.
    /// </summary>
    public bool HasDisasterRecoveryIntegration => DisasterRecovery?.IsEnabled == true;

    /// <summary>
    /// Gets whether revenue protection is enabled.
    /// </summary>
    public bool HasRevenueProtection => RevenueProtection?.IsEnabled == true;

    /// <summary>
    /// Gets whether this configuration can handle emergency capacity scaling.
    /// </summary>
    public bool CanHandleEmergencyCapacity => RevenueProtection?.EmergencyCapacityPercent > 100;

    /// <summary>
    /// Private constructor for creating LoadBalancingConfiguration instances.
    /// </summary>
    private LoadBalancingConfiguration(string configurationId, LoadBalancingStrategy strategy, 
        TimeSpan healthCheckInterval, int maxRetries, string? culturalContext)
    {
        ConfigurationId = configurationId;
        Strategy = strategy;
        HealthCheckInterval = healthCheckInterval;
        MaxRetries = maxRetries;
        CulturalContext = culturalContext;
        CulturalProfiles = new Dictionary<string, CulturalLoadBalancingProfile>();
    }

    /// <summary>
    /// Creates a new LoadBalancingConfiguration with validation.
    /// </summary>
    /// <param name="configurationId">Unique configuration identifier.</param>
    /// <param name="strategy">Load balancing strategy.</param>
    /// <param name="healthCheckInterval">Health check interval.</param>
    /// <param name="maxRetries">Maximum retry attempts.</param>
    /// <param name="culturalContext">Cultural context for intelligent routing.</param>
    /// <returns>A new LoadBalancingConfiguration instance.</returns>
    /// <exception cref="ArgumentException">Thrown when parameters are invalid.</exception>
    public static LoadBalancingConfiguration Create(string configurationId, LoadBalancingStrategy strategy,
        TimeSpan healthCheckInterval, int maxRetries, string? culturalContext)
    {
        if (string.IsNullOrWhiteSpace(configurationId))
            throw new ArgumentException("Configuration ID cannot be null or empty", nameof(configurationId));

        if (healthCheckInterval <= TimeSpan.Zero)
            throw new ArgumentException("Health check interval must be positive", nameof(healthCheckInterval));

        if (maxRetries < 0)
            throw new ArgumentException("Max retries cannot be negative", nameof(maxRetries));

        return new LoadBalancingConfiguration(configurationId, strategy, healthCheckInterval, maxRetries, culturalContext);
    }

    /// <summary>
    /// Creates a default LoadBalancingConfiguration with enterprise-grade settings.
    /// </summary>
    /// <param name="configurationId">Unique configuration identifier.</param>
    /// <returns>A new LoadBalancingConfiguration with enterprise defaults.</returns>
    public static LoadBalancingConfiguration CreateDefault(string configurationId)
    {
        if (string.IsNullOrWhiteSpace(configurationId))
            throw new ArgumentException("Configuration ID cannot be null or empty", nameof(configurationId));

        var config = new LoadBalancingConfiguration(configurationId, LoadBalancingStrategy.CulturalAware,
            TimeSpan.FromSeconds(30), 3, null);

        // Add default disaster recovery integration
        config.DisasterRecovery = new DisasterRecoveryLoadBalancingConfiguration(
            TimeSpan.FromSeconds(15), TimeSpan.FromMinutes(30), 0.95);

        return config;
    }

    /// <summary>
    /// Creates an enterprise-grade LoadBalancingConfiguration with advanced features.
    /// </summary>
    /// <param name="configurationId">Unique configuration identifier.</param>
    /// <returns>A new enterprise-grade LoadBalancingConfiguration.</returns>
    public static LoadBalancingConfiguration CreateEnterpriseGrade(string configurationId)
    {
        if (string.IsNullOrWhiteSpace(configurationId))
            throw new ArgumentException("Configuration ID cannot be null or empty", nameof(configurationId));

        var config = new LoadBalancingConfiguration(configurationId, LoadBalancingStrategy.CulturalAware,
            TimeSpan.FromSeconds(15), 5, "Enterprise Cultural Intelligence");

        // Add enterprise disaster recovery
        config.DisasterRecovery = new DisasterRecoveryLoadBalancingConfiguration(
            TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(15), 0.99);

        // Add revenue protection
        config.RevenueProtection = new RevenueProtectionLoadBalancingConfiguration(
            true, 0.99, 150);

        return config;
    }

    /// <summary>
    /// Adds cultural profiles for event-specific load balancing.
    /// </summary>
    /// <param name="profiles">Cultural profiles to add.</param>
    /// <returns>Updated LoadBalancingConfiguration.</returns>
    public LoadBalancingConfiguration WithCulturalProfiles(Dictionary<string, CulturalLoadBalancingProfile> profiles)
    {
        CulturalProfiles = new Dictionary<string, CulturalLoadBalancingProfile>(profiles);
        return this;
    }

    /// <summary>
    /// Adds disaster recovery integration.
    /// </summary>
    /// <param name="disasterRecoveryConfig">Disaster recovery configuration.</param>
    /// <returns>Updated LoadBalancingConfiguration.</returns>
    public LoadBalancingConfiguration WithDisasterRecoveryIntegration(DisasterRecoveryLoadBalancingConfiguration disasterRecoveryConfig)
    {
        DisasterRecovery = disasterRecoveryConfig;
        return this;
    }

    /// <summary>
    /// Adds revenue protection configuration.
    /// </summary>
    /// <param name="revenueProtectionConfig">Revenue protection configuration.</param>
    /// <returns>Updated LoadBalancingConfiguration.</returns>
    public LoadBalancingConfiguration WithRevenueProtection(RevenueProtectionLoadBalancingConfiguration revenueProtectionConfig)
    {
        RevenueProtection = revenueProtectionConfig;
        return this;
    }

    /// <summary>
    /// Gets a cultural profile by event name.
    /// </summary>
    /// <param name="eventName">Cultural event name.</param>
    /// <returns>Cultural profile or null if not found.</returns>
    public CulturalLoadBalancingProfile? GetCulturalProfile(string eventName)
    {
        return CulturalProfiles.TryGetValue(eventName, out var profile) ? profile : null;
    }

    /// <summary>
    /// Gets the health check configuration.
    /// </summary>
    /// <returns>Health check configuration.</returns>
    public LoadBalancingHealthCheckConfiguration GetHealthCheckConfiguration()
    {
        return new LoadBalancingHealthCheckConfiguration(
            HealthCheckInterval, 
            MaxRetries,
            TimeSpan.FromSeconds(Math.Max(5, HealthCheckInterval.TotalSeconds * 0.5)));
    }

    /// <summary>
    /// Validates the configuration settings.
    /// </summary>
    /// <returns>Validation result.</returns>
    public ConfigurationValidationResult ValidateConfiguration()
    {
        var errors = new List<string>();

        if (HealthCheckInterval < TimeSpan.FromSeconds(1))
            errors.Add("HealthCheckInterval must be at least 1 second");

        if (MaxRetries < 1)
            errors.Add("MaxRetries must be at least 1");

        if (HealthCheckInterval > TimeSpan.FromMinutes(10))
            errors.Add("HealthCheckInterval should not exceed 10 minutes for optimal performance");

        return new ConfigurationValidationResult(errors.Count == 0, errors);
    }

    /// <summary>
    /// Calculates optimal capacity for current load and cultural context.
    /// </summary>
    /// <param name="currentLoad">Current load metrics.</param>
    /// <param name="culturalEventName">Cultural event name for context-aware scaling.</param>
    /// <returns>Capacity recommendation.</returns>
    public CapacityRecommendation CalculateOptimalCapacity(LoadMetrics currentLoad, string? culturalEventName = null)
    {
        var baseCapacity = currentLoad.ActiveConnections;
        var multiplier = 1.0;

        // Apply cultural event multiplier if available
        if (!string.IsNullOrEmpty(culturalEventName) && CulturalProfiles.TryGetValue(culturalEventName, out var profile))
        {
            multiplier = profile.TrafficMultiplier;
        }

        // Apply revenue protection emergency scaling if enabled
        if (HasRevenueProtection && RevenueProtection!.IsEnabled)
        {
            multiplier = Math.Max(multiplier, RevenueProtection.EmergencyCapacityPercent / 100.0);
        }

        var recommendedCapacity = (int)(baseCapacity * multiplier);
        var isScalingRequired = recommendedCapacity > baseCapacity * 1.1; // 10% threshold

        return new CapacityRecommendation(
            recommendedCapacity,
            isScalingRequired,
            culturalEventName,
            multiplier,
            $"Capacity calculation: {baseCapacity} * {multiplier:F2} = {recommendedCapacity}");
    }

    /// <summary>
    /// Returns a string representation of the load balancing configuration.
    /// </summary>
    public override string ToString()
    {
        var result = $"{ConfigurationId} (Strategy: {Strategy}, Interval: {HealthCheckInterval.TotalSeconds}s)";

        if (!string.IsNullOrEmpty(CulturalContext))
            result += $" - {CulturalContext}";

        if (IsEnterpriseGrade)
            result += " [Enterprise]";

        if (HasDisasterRecoveryIntegration)
            result += " [DR]";

        if (HasRevenueProtection)
            result += " [Revenue Protected]";

        return result;
    }

    #region Equality

    /// <summary>
    /// Determines equality with another LoadBalancingConfiguration.
    /// </summary>
    public bool Equals(LoadBalancingConfiguration? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return ConfigurationId == other.ConfigurationId &&
               Strategy == other.Strategy &&
               HealthCheckInterval.Equals(other.HealthCheckInterval) &&
               MaxRetries == other.MaxRetries &&
               CulturalContext == other.CulturalContext;
    }

    /// <summary>
    /// Determines equality with another object.
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is LoadBalancingConfiguration other && Equals(other);
    }

    /// <summary>
    /// Gets the hash code for this LoadBalancingConfiguration.
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(ConfigurationId, Strategy, HealthCheckInterval, MaxRetries, CulturalContext);
    }

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(LoadBalancingConfiguration? left, LoadBalancingConfiguration? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Inequality operator.
    /// </summary>
    public static bool operator !=(LoadBalancingConfiguration? left, LoadBalancingConfiguration? right)
    {
        return !Equals(left, right);
    }

    #endregion
}

/// <summary>
/// Load balancing strategies for cultural intelligence-aware routing.
/// </summary>
public enum LoadBalancingStrategy
{
    /// <summary>
    /// Round-robin distribution.
    /// </summary>
    RoundRobin = 0,

    /// <summary>
    /// Weighted round-robin distribution.
    /// </summary>
    WeightedRoundRobin = 1,

    /// <summary>
    /// Least connections distribution.
    /// </summary>
    LeastConnections = 2,

    /// <summary>
    /// Cultural intelligence-aware routing.
    /// </summary>
    CulturalAware = 3,

    /// <summary>
    /// Diaspora community-optimized routing.
    /// </summary>
    DiasporaOptimized = 4
}

/// <summary>
/// Cultural load balancing profile for event-specific optimization.
/// </summary>
public record CulturalLoadBalancingProfile(
    string EventName,
    double TrafficMultiplier,
    TimeSpan ExpectedDuration);

/// <summary>
/// Disaster recovery integration configuration for load balancing.
/// </summary>
public record DisasterRecoveryLoadBalancingConfiguration(
    TimeSpan FailoverDetectionTime,
    TimeSpan RecoveryWindow,
    double HealthThreshold)
{
    /// <summary>
    /// Gets whether this configuration meets enterprise-grade requirements.
    /// </summary>
    public bool IsEnterpriseGrade => FailoverDetectionTime <= TimeSpan.FromSeconds(15) && HealthThreshold >= 0.95;

    /// <summary>
    /// Gets whether disaster recovery is enabled.
    /// </summary>
    public bool IsEnabled => HealthThreshold > 0;
}

/// <summary>
/// Revenue protection configuration for load balancing.
/// </summary>
public record RevenueProtectionLoadBalancingConfiguration(
    bool IsEnabled,
    double ProtectionThreshold,
    int EmergencyCapacityPercent);

/// <summary>
/// Health check configuration for load balancing.
/// </summary>
public record LoadBalancingHealthCheckConfiguration(
    TimeSpan Interval,
    int MaxRetries,
    TimeSpan TimeoutThreshold);

/// <summary>
/// Configuration validation result.
/// </summary>
public record ConfigurationValidationResult(
    bool IsValid,
    IReadOnlyList<string> ValidationErrors);

/// <summary>
/// Load metrics for capacity calculations.
/// </summary>
public record LoadMetrics(
    int ActiveConnections,
    int RequestsPerSecond,
    TimeSpan AverageResponseTime);

/// <summary>
/// Capacity recommendation for load balancing scaling.
/// </summary>
public record CapacityRecommendation(
    int RecommendedCapacity,
    bool IsScalingRequired,
    string? CulturalEventContext,
    double ScalingMultiplier,
    string Justification);