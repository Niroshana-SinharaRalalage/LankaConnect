using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Enterprise.ValueObjects;

public class DisasterRecoveryTrigger : ValueObject
{
    public string TriggerId { get; private set; }
    public string TriggerType { get; private set; }
    public string Severity { get; private set; }
    public DateTime DetectedAt { get; private set; }
    public string Description { get; private set; }
    public IReadOnlyDictionary<string, object> TriggerMetrics { get; private set; }
    public IReadOnlyList<string> AffectedServices { get; private set; }
    public IReadOnlyList<string> AffectedRegions { get; private set; }
    public string EstimatedImpact { get; private set; }
    public TimeSpan EstimatedRecoveryTime { get; private set; }
    public bool RequiresImmediateAction { get; private set; }
    public string TriggerSource { get; private set; }
    public IReadOnlyDictionary<string, string> AdditionalContext { get; private set; }

    private DisasterRecoveryTrigger(
        string triggerId,
        string triggerType,
        string severity,
        DateTime detectedAt,
        string description,
        IReadOnlyDictionary<string, object> triggerMetrics,
        IReadOnlyList<string> affectedServices,
        IReadOnlyList<string> affectedRegions,
        string estimatedImpact,
        TimeSpan estimatedRecoveryTime,
        bool requiresImmediateAction,
        string triggerSource,
        IReadOnlyDictionary<string, string> additionalContext)
    {
        TriggerId = triggerId;
        TriggerType = triggerType;
        Severity = severity;
        DetectedAt = detectedAt;
        Description = description;
        TriggerMetrics = triggerMetrics;
        AffectedServices = affectedServices;
        AffectedRegions = affectedRegions;
        EstimatedImpact = estimatedImpact;
        EstimatedRecoveryTime = estimatedRecoveryTime;
        RequiresImmediateAction = requiresImmediateAction;
        TriggerSource = triggerSource;
        AdditionalContext = additionalContext;
    }

    public static DisasterRecoveryTrigger Create(
        string triggerId,
        string triggerType,
        string severity,
        DateTime detectedAt,
        string description,
        IReadOnlyDictionary<string, object> triggerMetrics,
        IEnumerable<string> affectedServices,
        IEnumerable<string> affectedRegions,
        string estimatedImpact,
        TimeSpan estimatedRecoveryTime,
        bool requiresImmediateAction,
        string triggerSource,
        IReadOnlyDictionary<string, string>? additionalContext = null)
    {
        if (string.IsNullOrWhiteSpace(triggerId)) throw new ArgumentException("Trigger ID is required", nameof(triggerId));
        if (string.IsNullOrWhiteSpace(triggerType)) throw new ArgumentException("Trigger type is required", nameof(triggerType));
        if (string.IsNullOrWhiteSpace(severity)) throw new ArgumentException("Severity is required", nameof(severity));
        if (detectedAt > DateTime.UtcNow.AddMinutes(1)) throw new ArgumentException("Detected at cannot be significantly in the future", nameof(detectedAt));
        if (string.IsNullOrWhiteSpace(description)) throw new ArgumentException("Description is required", nameof(description));
        if (triggerMetrics == null) throw new ArgumentNullException(nameof(triggerMetrics));
        if (string.IsNullOrWhiteSpace(estimatedImpact)) throw new ArgumentException("Estimated impact is required", nameof(estimatedImpact));
        if (estimatedRecoveryTime < TimeSpan.Zero) throw new ArgumentException("Estimated recovery time cannot be negative", nameof(estimatedRecoveryTime));
        if (string.IsNullOrWhiteSpace(triggerSource)) throw new ArgumentException("Trigger source is required", nameof(triggerSource));

        var servicesList = affectedServices?.ToList() ?? new List<string>();
        var regionsList = affectedRegions?.ToList() ?? new List<string>();
        var context = additionalContext ?? new Dictionary<string, string>();

        // Validate trigger type
        var validTriggerTypes = new[] { 
            "SystemFailure", "NetworkOutage", "DataCorruption", "SecurityBreach", 
            "PerformanceDegradation", "ResourceExhaustion", "ExternalDependencyFailure", 
            "ManualTrigger", "ScheduledMaintenance", "CulturalEventOverload" 
        };
        if (!validTriggerTypes.Contains(triggerType))
            throw new ArgumentException($"Trigger type must be one of: {string.Join(", ", validTriggerTypes)}", nameof(triggerType));

        // Validate severity
        var validSeverities = new[] { "Low", "Medium", "High", "Critical", "Emergency" };
        if (!validSeverities.Contains(severity))
            throw new ArgumentException($"Severity must be one of: {string.Join(", ", validSeverities)}", nameof(severity));

        // Validate estimated impact
        var validImpacts = new[] { "Minimal", "Low", "Medium", "High", "Severe", "Catastrophic" };
        if (!validImpacts.Contains(estimatedImpact))
            throw new ArgumentException($"Estimated impact must be one of: {string.Join(", ", validImpacts)}", nameof(estimatedImpact));

        return new DisasterRecoveryTrigger(
            triggerId,
            triggerType,
            severity,
            detectedAt,
            description,
            triggerMetrics,
            servicesList.AsReadOnly(),
            regionsList.AsReadOnly(),
            estimatedImpact,
            estimatedRecoveryTime,
            requiresImmediateAction,
            triggerSource,
            context);
    }

    public bool IsCriticalTrigger()
    {
        return Severity == "Critical" || 
               Severity == "Emergency" || 
               RequiresImmediateAction ||
               EstimatedImpact == "Severe" ||
               EstimatedImpact == "Catastrophic";
    }

    public bool IsSystemWideFailure()
    {
        return AffectedServices.Count > 5 || 
               AffectedRegions.Count > 2 ||
               TriggerType == "SystemFailure" ||
               EstimatedImpact == "Catastrophic";
    }

    public string GetPriorityLevel()
    {
        if (IsCriticalTrigger()) return "P1 - Critical";
        if (Severity == "High" || RequiresImmediateAction) return "P2 - High";
        if (Severity == "Medium") return "P3 - Medium";
        return "P4 - Low";
    }

    public TimeSpan GetResponseTimeTarget()
    {
        return Severity switch
        {
            "Emergency" => TimeSpan.FromMinutes(1),
            "Critical" => TimeSpan.FromMinutes(5),
            "High" => TimeSpan.FromMinutes(15),
            "Medium" => TimeSpan.FromMinutes(30),
            _ => TimeSpan.FromHours(1)
        };
    }

    public IReadOnlyList<string> GetRecommendedActions()
    {
        var actions = new List<string>();
        
        if (IsCriticalTrigger())
        {
            actions.Add("Initiate emergency response protocol");
            actions.Add("Notify all stakeholders immediately");
        }

        if (IsSystemWideFailure())
        {
            actions.Add("Activate primary disaster recovery site");
            actions.Add("Implement full traffic rerouting");
        }

        switch (TriggerType)
        {
            case "SecurityBreach":
                actions.Add("Isolate affected systems");
                actions.Add("Enable security lockdown mode");
                break;
            case "DataCorruption":
                actions.Add("Stop all write operations");
                actions.Add("Initiate data integrity checks");
                break;
            case "CulturalEventOverload":
                actions.Add("Activate cultural event load balancing");
                actions.Add("Scale out regional capacity");
                break;
        }

        if (!actions.Any())
        {
            actions.Add("Monitor system metrics");
            actions.Add("Prepare contingency plans");
        }

        return actions.AsReadOnly();
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return TriggerId;
        yield return TriggerType;
        yield return Severity;
        yield return DetectedAt;
        yield return Description;
        yield return EstimatedImpact;
        yield return EstimatedRecoveryTime;
        yield return RequiresImmediateAction;
        yield return TriggerSource;
        
        foreach (var service in AffectedServices)
            yield return service;
        
        foreach (var region in AffectedRegions)
            yield return region;
        
        foreach (var metric in TriggerMetrics.OrderBy(x => x.Key))
        {
            yield return metric.Key;
            yield return metric.Value;
        }
        
        foreach (var context in AdditionalContext.OrderBy(x => x.Key))
        {
            yield return context.Key;
            yield return context.Value;
        }
    }
}