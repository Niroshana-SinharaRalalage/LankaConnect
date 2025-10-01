using LankaConnect.Domain.Business;
using LankaConnect.Domain.Common.ValueObjects;

namespace LankaConnect.Domain.Common.DisasterRecovery;

/// <summary>
/// Domain context for disaster recovery operations with cultural intelligence
/// and revenue protection features for the LankaConnect platform.
/// </summary>
public class DisasterRecoveryContext
{
    /// <summary>
    /// Gets the unique context identifier.
    /// </summary>
    public Guid ContextId { get; }

    /// <summary>
    /// Gets the disaster recovery scenario identifier.
    /// </summary>
    public string ScenarioId { get; }

    /// <summary>
    /// Gets the primary region that failed or requires failover.
    /// </summary>
    public string PrimaryRegion { get; }

    /// <summary>
    /// Gets the target failover region.
    /// </summary>
    public string FailoverRegion { get; }

    /// <summary>
    /// Gets the time when the disaster recovery was triggered.
    /// </summary>
    public DateTime TriggerTime { get; }

    /// <summary>
    /// Gets the cultural event type associated with this disaster recovery scenario.
    /// </summary>
    public string? CulturalEventType { get; private set; }

    /// <summary>
    /// Gets the communities affected by the cultural event during disaster recovery.
    /// </summary>
    public string[] AffectedCommunities { get; private set; } = Array.Empty<string>();

    /// <summary>
    /// Gets the expected traffic multiplier due to cultural events.
    /// </summary>
    public double ExpectedTrafficMultiplier { get; private set; } = 1.0;

    /// <summary>
    /// Gets whether this context includes cultural intelligence data.
    /// </summary>
    public bool HasCulturalIntelligence => !string.IsNullOrEmpty(CulturalEventType);

    /// <summary>
    /// Gets the revenue protection strategy for this disaster recovery scenario.
    /// </summary>
    public RevenueProtectionStrategy? RevenueProtectionStrategy { get; private set; }

    /// <summary>
    /// Gets the expected revenue loss during disaster recovery.
    /// </summary>
    public decimal ExpectedRevenueLoss { get; private set; }

    /// <summary>
    /// Gets the maximum acceptable revenue loss threshold.
    /// </summary>
    public decimal MaxAcceptableRevenueLoss { get; private set; }

    /// <summary>
    /// Gets whether this context requires revenue protection measures.
    /// </summary>
    public bool RequiresRevenueProtection => RevenueProtectionStrategy.HasValue;

    /// <summary>
    /// Gets the service level agreements that must be maintained during disaster recovery.
    /// </summary>
    public ServiceLevelAgreement[] ServiceLevelAgreements { get; private set; } = Array.Empty<ServiceLevelAgreement>();

    /// <summary>
    /// Gets whether this context has service level requirements.
    /// </summary>
    public bool HasServiceLevelRequirements => ServiceLevelAgreements.Any();

    /// <summary>
    /// Private constructor for creating DisasterRecoveryContext instances.
    /// </summary>
    private DisasterRecoveryContext(string scenarioId, string primaryRegion, string failoverRegion, DateTime triggerTime)
    {
        ContextId = Guid.NewGuid();
        ScenarioId = scenarioId;
        PrimaryRegion = primaryRegion;
        FailoverRegion = failoverRegion;
        TriggerTime = triggerTime;
    }

    /// <summary>
    /// Factory method to create a new DisasterRecoveryContext with validation.
    /// </summary>
    /// <param name="scenarioId">The disaster recovery scenario identifier.</param>
    /// <param name="primaryRegion">The primary region that failed.</param>
    /// <param name="failoverRegion">The target failover region.</param>
    /// <param name="triggerTime">The time when disaster recovery was triggered.</param>
    /// <returns>A new DisasterRecoveryContext instance.</returns>
    /// <exception cref="ArgumentException">Thrown when required parameters are invalid.</exception>
    public static DisasterRecoveryContext Create(string scenarioId, string primaryRegion, string failoverRegion, DateTime triggerTime)
    {
        if (string.IsNullOrWhiteSpace(scenarioId))
            throw new ArgumentException("Scenario ID cannot be null or empty", nameof(scenarioId));

        if (string.IsNullOrWhiteSpace(primaryRegion))
            throw new ArgumentException("Primary region cannot be null or empty", nameof(primaryRegion));

        if (string.IsNullOrWhiteSpace(failoverRegion))
            throw new ArgumentException("Failover region cannot be null or empty", nameof(failoverRegion));

        return new DisasterRecoveryContext(scenarioId, primaryRegion, failoverRegion, triggerTime);
    }

    /// <summary>
    /// Sets cultural intelligence context for specialized disaster recovery handling.
    /// </summary>
    /// <param name="culturalEventType">The cultural event type affecting the recovery.</param>
    /// <param name="affectedCommunities">The communities affected by the cultural event.</param>
    /// <param name="trafficMultiplier">The expected traffic multiplier during the event.</param>
    public void SetCulturalIntelligenceContext(string culturalEventType, string[] affectedCommunities, double trafficMultiplier)
    {
        CulturalEventType = culturalEventType;
        AffectedCommunities = affectedCommunities ?? Array.Empty<string>();
        ExpectedTrafficMultiplier = Math.Max(1.0, trafficMultiplier);
    }

    /// <summary>
    /// Sets revenue protection context for disaster recovery operations.
    /// </summary>
    /// <param name="strategy">The revenue protection strategy to apply.</param>
    /// <param name="expectedLoss">The expected revenue loss.</param>
    /// <param name="maxAcceptableLoss">The maximum acceptable revenue loss.</param>
    public void SetRevenueProtectionContext(RevenueProtectionStrategy strategy, decimal expectedLoss, decimal maxAcceptableLoss)
    {
        RevenueProtectionStrategy = strategy;
        ExpectedRevenueLoss = Math.Max(0, expectedLoss);
        MaxAcceptableRevenueLoss = Math.Max(0, maxAcceptableLoss);
    }

    /// <summary>
    /// Sets the service level agreements that must be maintained during disaster recovery.
    /// </summary>
    /// <param name="slas">The service level agreements to maintain.</param>
    public void SetServiceLevelAgreements(ServiceLevelAgreement[] slas)
    {
        ServiceLevelAgreements = slas ?? Array.Empty<ServiceLevelAgreement>();
    }

    /// <summary>
    /// Gets the optimal failover window considering cultural intelligence.
    /// </summary>
    /// <returns>A DateRange representing the optimal failover window.</returns>
    public LankaConnect.Domain.Common.ValueObjects.DateRange GetOptimalFailoverWindow()
    {
        var baseWindow = LankaConnect.Domain.Common.ValueObjects.DateRange.Create(TriggerTime, TriggerTime.AddHours(4));

        if (HasCulturalIntelligence)
        {
            // Extend window for cultural events to minimize impact
            var culturalBuffer = TimeSpan.FromHours(ExpectedTrafficMultiplier);
            baseWindow = baseWindow.Expand(culturalBuffer);
            baseWindow.SetCulturalContext(CulturalEventType!, string.Join(",", AffectedCommunities));
        }

        return baseWindow;
    }

    /// <summary>
    /// Calculates the recovery priority based on cultural intelligence and business context.
    /// </summary>
    /// <returns>A priority score (0-100, higher is more urgent).</returns>
    public int CalculateRecoveryPriority()
    {
        var basePriority = 50; // Default priority

        // Increase priority for cultural events
        if (HasCulturalIntelligence)
        {
            basePriority += (int)(ExpectedTrafficMultiplier * 10);
            basePriority += AffectedCommunities.Length * 5;
        }

        // Increase priority based on revenue impact
        if (RequiresRevenueProtection)
        {
            var revenueFactor = (double)(ExpectedRevenueLoss / Math.Max(1, MaxAcceptableRevenueLoss));
            basePriority += (int)(revenueFactor * 20);
        }

        // Increase priority based on SLA requirements
        if (HasServiceLevelRequirements)
        {
            basePriority += ServiceLevelAgreements.Length * 3;
        }

        return Math.Min(100, Math.Max(0, basePriority));
    }

    /// <summary>
    /// Determines if this is a scheduled failover (future trigger time).
    /// </summary>
    /// <returns>True if the failover is scheduled for the future.</returns>
    public bool IsScheduledFailover()
    {
        return TriggerTime > DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the time remaining until the scheduled failover.
    /// </summary>
    /// <returns>The time remaining, or TimeSpan.Zero if already triggered.</returns>
    public TimeSpan GetTimeUntilFailover()
    {
        if (!IsScheduledFailover())
            return TimeSpan.Zero;

        return TriggerTime - DateTime.UtcNow;
    }

    /// <summary>
    /// Generates a comprehensive context report for monitoring and logging.
    /// </summary>
    /// <returns>A formatted string containing the context details.</returns>
    public string GenerateContextReport()
    {
        var report = new System.Text.StringBuilder();
        report.AppendLine($"Disaster Recovery Context Report");
        report.AppendLine($"Context ID: {ContextId}");
        report.AppendLine($"Scenario: {ScenarioId}");
        report.AppendLine($"Primary Region: {PrimaryRegion}");
        report.AppendLine($"Failover Region: {FailoverRegion}");
        report.AppendLine($"Trigger Time: {TriggerTime:yyyy-MM-dd HH:mm:ss} UTC");
        report.AppendLine($"Priority Score: {CalculateRecoveryPriority()}");

        if (HasCulturalIntelligence)
        {
            report.AppendLine($"Cultural Event: {CulturalEventType}");
            report.AppendLine($"Affected Communities: {string.Join(", ", AffectedCommunities)}");
            report.AppendLine($"Traffic Multiplier: {ExpectedTrafficMultiplier:F2}x");
        }

        if (RequiresRevenueProtection)
        {
            report.AppendLine($"Revenue Protection: {RevenueProtectionStrategy}");
            report.AppendLine($"Expected Loss: ${ExpectedRevenueLoss:N2}");
            report.AppendLine($"Max Acceptable Loss: ${MaxAcceptableRevenueLoss:N2}");
        }

        if (HasServiceLevelRequirements)
        {
            report.AppendLine($"SLA Count: {ServiceLevelAgreements.Length}");
        }

        return report.ToString();
    }
}