using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Shared;
using CulturalContextType = LankaConnect.Domain.Common.Database.MultiLanguageRoutingModels.CulturalContextType;

namespace LankaConnect.Domain.Infrastructure.Scaling;

/// <summary>
/// Represents a cultural event with significance and timing information for predictive scaling
/// </summary>
public class CulturalEvent : ValueObject
{
    public string Id { get; private set; }
    public string Name { get; private set; }
    public CulturalEventType EventType { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }
    public CulturalSignificance Significance { get; private set; }
    public IReadOnlyList<string> AffectedCommunities { get; private set; }
    public CulturalContextType CulturalContextType { get; private set; }

    private CulturalEvent(
        string id,
        string name,
        CulturalEventType eventType,
        DateTime startDate,
        DateTime endDate,
        CulturalSignificance significance,
        IEnumerable<string> affectedCommunities,
        CulturalContextType culturalContext)
    {
        Id = id;
        Name = name;
        EventType = eventType;
        StartDate = startDate;
        EndDate = endDate;
        Significance = significance;
        AffectedCommunities = affectedCommunities.ToList().AsReadOnly();
        CulturalContextType = culturalContext;
    }

    public static Result<CulturalEvent> Create(
        string id,
        string name,
        CulturalEventType eventType,
        DateTime startDate,
        DateTime endDate,
        CulturalSignificance significance,
        IEnumerable<string> affectedCommunities,
        CulturalContextType culturalContext)
    {
        if (string.IsNullOrWhiteSpace(id))
            return Result<CulturalEvent>.Failure("Cultural event ID cannot be empty");

        if (string.IsNullOrWhiteSpace(name))
            return Result<CulturalEvent>.Failure("Cultural event name cannot be empty");

        if (startDate >= endDate)
            return Result<CulturalEvent>.Failure("Start date must be before end date");

        if (!affectedCommunities.Any())
            return Result<CulturalEvent>.Failure("At least one affected community must be specified");

        return Result<CulturalEvent>.Success(new CulturalEvent(
            id, name, eventType, startDate, endDate, significance, affectedCommunities, culturalContext));
    }

    public TimeSpan Duration => EndDate - StartDate;

    public bool IsActiveAt(DateTime timestamp)
    {
        return timestamp >= StartDate && timestamp <= EndDate;
    }

    public bool AffectsCommunity(string communityId)
    {
        return AffectedCommunities.Contains(communityId);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Id;
        yield return Name;
        yield return EventType;
        yield return StartDate;
        yield return EndDate;
        yield return Significance;
    }
}

/// <summary>
/// Represents traffic prediction data for a cultural event
/// </summary>
public class TrafficPrediction : ValueObject
{
    public string EventId { get; private set; }
    public double ExpectedTrafficMultiplier { get; private set; }
    public DateTime PeakTrafficTime { get; private set; }
    public TimeSpan DurationHours { get; private set; }
    public IReadOnlyList<string> AffectedRegions { get; private set; }
    public double ConfidenceScore { get; private set; }

    private TrafficPrediction(
        string eventId,
        double expectedTrafficMultiplier,
        DateTime peakTrafficTime,
        TimeSpan durationHours,
        IEnumerable<string> affectedRegions,
        double confidenceScore)
    {
        EventId = eventId;
        ExpectedTrafficMultiplier = expectedTrafficMultiplier;
        PeakTrafficTime = peakTrafficTime;
        DurationHours = durationHours;
        AffectedRegions = affectedRegions.ToList().AsReadOnly();
        ConfidenceScore = confidenceScore;
    }

    public static Result<TrafficPrediction> Create(
        string eventId,
        double expectedTrafficMultiplier,
        DateTime peakTrafficTime,
        TimeSpan durationHours,
        IEnumerable<string> affectedRegions,
        double confidenceScore)
    {
        if (string.IsNullOrWhiteSpace(eventId))
            return Result<TrafficPrediction>.Failure("Event ID cannot be empty");

        if (expectedTrafficMultiplier <= 0)
            return Result<TrafficPrediction>.Failure("Traffic multiplier must be positive");

        if (durationHours.TotalHours <= 0)
            return Result<TrafficPrediction>.Failure("Duration must be positive");

        if (confidenceScore < 0 || confidenceScore > 1)
            return Result<TrafficPrediction>.Failure("Confidence score must be between 0 and 1");

        return Result<TrafficPrediction>.Success(new TrafficPrediction(
            eventId, expectedTrafficMultiplier, peakTrafficTime, durationHours, affectedRegions, confidenceScore));
    }

    public bool RequiresScaling(double scalingThreshold = 1.5)
    {
        return ExpectedTrafficMultiplier > scalingThreshold;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return EventId;
        yield return ExpectedTrafficMultiplier;
        yield return PeakTrafficTime;
        yield return DurationHours;
        yield return ConfidenceScore;
    }
}

/// <summary>
/// Represents a scaling action to be executed for cultural events
/// </summary>
public class ScalingAction : ValueObject
{
    public ScalingActionType ActionType { get; private set; }
    public ScalingTrigger Trigger { get; private set; }
    public double TargetCapacityMultiplier { get; private set; }
    public DateTime ScheduledExecutionTime { get; private set; }
    public IReadOnlyList<string> AffectedRegions { get; private set; }
    public CulturalContextType CulturalContextType { get; private set; }
    public TimeSpan? EstimatedDuration { get; private set; }

    private ScalingAction(
        ScalingActionType actionType,
        ScalingTrigger trigger,
        double targetCapacityMultiplier,
        DateTime scheduledExecutionTime,
        IEnumerable<string> affectedRegions,
        CulturalContextType culturalContext,
        TimeSpan? estimatedDuration = null)
    {
        ActionType = actionType;
        Trigger = trigger;
        TargetCapacityMultiplier = targetCapacityMultiplier;
        ScheduledExecutionTime = scheduledExecutionTime;
        AffectedRegions = affectedRegions.ToList().AsReadOnly();
        CulturalContextType = culturalContext;
        EstimatedDuration = estimatedDuration;
    }

    public static Result<ScalingAction> Create(
        ScalingActionType actionType,
        ScalingTrigger trigger,
        double targetCapacityMultiplier,
        DateTime scheduledExecutionTime,
        IEnumerable<string> affectedRegions,
        CulturalContextType culturalContext,
        TimeSpan? estimatedDuration = null)
    {
        if (targetCapacityMultiplier <= 0)
            return Result<ScalingAction>.Failure("Target capacity multiplier must be positive");

        if (scheduledExecutionTime <= DateTime.UtcNow)
            return Result<ScalingAction>.Failure("Scheduled execution time must be in the future");

        if (!affectedRegions.Any())
            return Result<ScalingAction>.Failure("At least one affected region must be specified");

        return Result<ScalingAction>.Success(new ScalingAction(
            actionType, trigger, targetCapacityMultiplier, scheduledExecutionTime, 
            affectedRegions, culturalContext, estimatedDuration));
    }

    public bool IsScheduledFor(DateTime timestamp, TimeSpan tolerance)
    {
        var timeDifference = Math.Abs((timestamp - ScheduledExecutionTime).TotalSeconds);
        return timeDifference <= tolerance.TotalSeconds;
    }

    public bool AffectsRegion(string region)
    {
        return AffectedRegions.Contains(region);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return ActionType;
        yield return Trigger;
        yield return TargetCapacityMultiplier;
        yield return ScheduledExecutionTime;
        yield return CulturalContextType;
    }
}

/// <summary>
/// Represents a comprehensive predictive scaling plan based on cultural intelligence
/// </summary>
public class PredictiveScalingPlan : Entity<string>
{
    public TimeSpan PredictionWindow { get; private set; }
    public IReadOnlyList<CulturalEvent> CulturalEvents { get; private set; }
    public Dictionary<CulturalEvent, TrafficPrediction> TrafficPredictions { get; private set; }
    public IReadOnlyList<ScalingAction> ScalingActions { get; private set; }
    public decimal EstimatedCost { get; private set; }
    public double ConfidenceScore { get; private set; }
    public new DateTime CreatedAt { get; private set; }
    public string CreatedBy { get; private set; }

    private PredictiveScalingPlan(
        string id,
        TimeSpan predictionWindow,
        IEnumerable<CulturalEvent> culturalEvents,
        Dictionary<CulturalEvent, TrafficPrediction> trafficPredictions,
        IEnumerable<ScalingAction> scalingActions,
        decimal estimatedCost,
        double confidenceScore,
        string createdBy) : base(id)
    {
        PredictionWindow = predictionWindow;
        CulturalEvents = culturalEvents.ToList().AsReadOnly();
        TrafficPredictions = new Dictionary<CulturalEvent, TrafficPrediction>(trafficPredictions);
        ScalingActions = scalingActions.ToList().AsReadOnly();
        EstimatedCost = estimatedCost;
        ConfidenceScore = confidenceScore;
        CreatedAt = DateTime.UtcNow;
        CreatedBy = createdBy;
    }

    public static Result<PredictiveScalingPlan> Create(
        string id,
        TimeSpan predictionWindow,
        IEnumerable<CulturalEvent> culturalEvents,
        Dictionary<CulturalEvent, TrafficPrediction> trafficPredictions,
        IEnumerable<ScalingAction> scalingActions,
        decimal estimatedCost,
        double confidenceScore,
        string createdBy)
    {
        if (string.IsNullOrWhiteSpace(id))
            return Result<PredictiveScalingPlan>.Failure("Scaling plan ID cannot be empty");

        if (predictionWindow.TotalHours <= 0)
            return Result<PredictiveScalingPlan>.Failure("Prediction window must be positive");

        if (confidenceScore < 0 || confidenceScore > 1)
            return Result<PredictiveScalingPlan>.Failure("Confidence score must be between 0 and 1");

        if (string.IsNullOrWhiteSpace(createdBy))
            return Result<PredictiveScalingPlan>.Failure("Creator must be specified");

        return Result<PredictiveScalingPlan>.Success(new PredictiveScalingPlan(
            id, predictionWindow, culturalEvents, trafficPredictions, 
            scalingActions, estimatedCost, confidenceScore, createdBy) { Id = id });
    }

    public IEnumerable<ScalingAction> GetScheduledActionsFor(DateTime timestamp, TimeSpan tolerance)
    {
        return ScalingActions.Where(action => action.IsScheduledFor(timestamp, tolerance));
    }

    public IEnumerable<CulturalEvent> GetActiveEventsAt(DateTime timestamp)
    {
        return CulturalEvents.Where(culturalEvent => culturalEvent.IsActiveAt(timestamp));
    }

    public double GetTotalExpectedTrafficIncrease()
    {
        return TrafficPredictions.Values.Sum(prediction => prediction.ExpectedTrafficMultiplier - 1.0);
    }

    public bool IsHighConfidence(double threshold = 0.8)
    {
        return ConfidenceScore >= threshold;
    }
}

/// <summary>
/// Enumerations for cultural intelligence scaling - renamed to avoid conflicts
/// </summary>
public enum CulturalScalingCategory
{
    ReligiousHoliday,
    CulturalFestival,
    NationalHoliday,
    CommunityGathering,
    DiasporaCelebration,
    TraditionalObservance
}

public enum CulturalSignificance
{
    Low,
    Medium,
    High,
    Critical,
    Sacred
}

public enum ScalingActionType
{
    DatabaseScaleUp,
    DatabaseScaleDown,
    ConnectionPoolExpansion,
    ConnectionPoolContraction,
    ShardAddition,
    ShardRebalancing,
    CrossRegionReplication,
    LoadBalancerReconfiguration
}

public enum ScalingTrigger
{
    CulturalEvent,
    PerformanceThreshold,
    PredictiveAnalysis,
    ManualOverride,
    ScheduledMaintenance,
    DisasterRecovery
}

/// <summary>
/// Context information for cultural scaling operations
/// </summary>
public class CulturalScalingContext : ValueObject
{
    public IReadOnlyList<string> Communities { get; private set; }
    public string PrimaryRegion { get; private set; }
    public IReadOnlyList<string> SecondaryRegions { get; private set; }
    public TimeSpan MonitoringWindow { get; private set; }
    public Dictionary<string, object> ScalingParameters { get; private set; }

    private CulturalScalingContext(
        IEnumerable<string> communities,
        string primaryRegion,
        IEnumerable<string> secondaryRegions,
        TimeSpan monitoringWindow,
        Dictionary<string, object> scalingParameters)
    {
        Communities = communities.ToList().AsReadOnly();
        PrimaryRegion = primaryRegion;
        SecondaryRegions = secondaryRegions.ToList().AsReadOnly();
        MonitoringWindow = monitoringWindow;
        ScalingParameters = new Dictionary<string, object>(scalingParameters);
    }

    public static Result<CulturalScalingContext> Create(
        IEnumerable<string> communities,
        string primaryRegion,
        IEnumerable<string> secondaryRegions,
        TimeSpan monitoringWindow,
        Dictionary<string, object>? scalingParameters = null)
    {
        if (!communities.Any())
            return Result<CulturalScalingContext>.Failure("At least one community must be specified");

        if (string.IsNullOrWhiteSpace(primaryRegion))
            return Result<CulturalScalingContext>.Failure("Primary region cannot be empty");

        if (monitoringWindow.TotalMinutes <= 0)
            return Result<CulturalScalingContext>.Failure("Monitoring window must be positive");

        return Result<CulturalScalingContext>.Success(new CulturalScalingContext(
            communities, primaryRegion, secondaryRegions, monitoringWindow, scalingParameters ?? new Dictionary<string, object>()));
    }

    public bool IncludesCommunity(string communityId)
    {
        return Communities.Contains(communityId);
    }

    public bool IncludesRegion(string region)
    {
        return PrimaryRegion == region || SecondaryRegions.Contains(region);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return string.Join(",", Communities);
        yield return PrimaryRegion;
        yield return string.Join(",", SecondaryRegions);
        yield return MonitoringWindow;
    }
}