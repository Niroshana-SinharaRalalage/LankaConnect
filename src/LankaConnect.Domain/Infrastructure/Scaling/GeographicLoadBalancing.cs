using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared;

namespace LankaConnect.Domain.Infrastructure.Scaling;

/// <summary>
/// Represents geographic distribution analysis for diaspora communities
/// </summary>
public class GeographicDistribution : ValueObject
{
    public string CommunityId { get; private set; }
    public string PrimaryRegion { get; private set; }
    public IReadOnlyList<string> SecondaryRegions { get; private set; }
    public Dictionary<string, int> UserDistribution { get; private set; }
    public Dictionary<string, string> TimeZoneDistribution { get; private set; }
    public Dictionary<string, IReadOnlyList<int>> PeakUsageHours { get; private set; }

    private GeographicDistribution(
        string communityId,
        string primaryRegion,
        IEnumerable<string> secondaryRegions,
        Dictionary<string, int> userDistribution,
        Dictionary<string, string> timeZoneDistribution,
        Dictionary<string, IReadOnlyList<int>> peakUsageHours)
    {
        CommunityId = communityId;
        PrimaryRegion = primaryRegion;
        SecondaryRegions = secondaryRegions.ToList().AsReadOnly();
        UserDistribution = new Dictionary<string, int>(userDistribution);
        TimeZoneDistribution = new Dictionary<string, string>(timeZoneDistribution);
        PeakUsageHours = new Dictionary<string, IReadOnlyList<int>>(peakUsageHours);
    }

    public static Result<GeographicDistribution> Create(
        string communityId,
        string primaryRegion,
        IEnumerable<string> secondaryRegions,
        Dictionary<string, int> userDistribution,
        Dictionary<string, string> timeZoneDistribution,
        Dictionary<string, IReadOnlyList<int>> peakUsageHours)
    {
        if (string.IsNullOrWhiteSpace(communityId))
            return Result<GeographicDistribution>.Failure("Community ID cannot be empty");

        if (string.IsNullOrWhiteSpace(primaryRegion))
            return Result<GeographicDistribution>.Failure("Primary region cannot be empty");

        if (!userDistribution.Any())
            return Result<GeographicDistribution>.Failure("User distribution cannot be empty");

        if (userDistribution.Any(kvp => kvp.Value < 0))
            return Result<GeographicDistribution>.Failure("User counts cannot be negative");

        return Result<GeographicDistribution>.Success(new GeographicDistribution(
            communityId, primaryRegion, secondaryRegions, userDistribution, 
            timeZoneDistribution, peakUsageHours));
    }

    public int GetTotalUsers()
    {
        return UserDistribution.Values.Sum();
    }

    public double GetRegionUserPercentage(string region)
    {
        var totalUsers = GetTotalUsers();
        if (totalUsers == 0) return 0;

        return UserDistribution.TryGetValue(region, out var regionUsers) 
            ? (double)regionUsers / totalUsers * 100 
            : 0;
    }

    public IEnumerable<string> GetRegionsByUserCount()
    {
        return UserDistribution.OrderByDescending(kvp => kvp.Value).Select(kvp => kvp.Key);
    }

    public bool IsPeakUsageTime(string region, int hour)
    {
        return PeakUsageHours.TryGetValue(region, out var hours) && hours.Contains(hour);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return CommunityId;
        yield return PrimaryRegion;
        yield return string.Join(",", SecondaryRegions.OrderBy(r => r));
        yield return string.Join(",", UserDistribution.OrderBy(kvp => kvp.Key).Select(kvp => $"{kvp.Key}:{kvp.Value}"));
    }
}

/// <summary>
/// Represents cultural affinity mapping for load balancing decisions
/// </summary>
public class CulturalAffinityMapping : ValueObject
{
    public string Region { get; private set; }
    public Dictionary<string, double> CommunityAffinityScores { get; private set; }
    public Dictionary<string, double> CulturalEventWeights { get; private set; }
    public Dictionary<string, double> LanguageAffinityScores { get; private set; }
    public double RegionalCulturalStrength { get; private set; }

    private CulturalAffinityMapping(
        string region,
        Dictionary<string, double> communityAffinityScores,
        Dictionary<string, double> culturalEventWeights,
        Dictionary<string, double> languageAffinityScores,
        double regionalCulturalStrength)
    {
        Region = region;
        CommunityAffinityScores = new Dictionary<string, double>(communityAffinityScores);
        CulturalEventWeights = new Dictionary<string, double>(culturalEventWeights);
        LanguageAffinityScores = new Dictionary<string, double>(languageAffinityScores);
        RegionalCulturalStrength = regionalCulturalStrength;
    }

    public static Result<CulturalAffinityMapping> Create(
        string region,
        Dictionary<string, double> communityAffinityScores,
        Dictionary<string, double> culturalEventWeights,
        Dictionary<string, double> languageAffinityScores,
        double regionalCulturalStrength)
    {
        if (string.IsNullOrWhiteSpace(region))
            return Result<CulturalAffinityMapping>.Failure("Region cannot be empty");

        if (communityAffinityScores.Any(kvp => kvp.Value < 0 || kvp.Value > 1))
            return Result<CulturalAffinityMapping>.Failure("Community affinity scores must be between 0 and 1");

        if (culturalEventWeights.Any(kvp => kvp.Value < 0))
            return Result<CulturalAffinityMapping>.Failure("Cultural event weights cannot be negative");

        if (languageAffinityScores.Any(kvp => kvp.Value < 0 || kvp.Value > 1))
            return Result<CulturalAffinityMapping>.Failure("Language affinity scores must be between 0 and 1");

        if (regionalCulturalStrength < 0 || regionalCulturalStrength > 1)
            return Result<CulturalAffinityMapping>.Failure("Regional cultural strength must be between 0 and 1");

        return Result<CulturalAffinityMapping>.Success(new CulturalAffinityMapping(
            region, communityAffinityScores, culturalEventWeights, languageAffinityScores, regionalCulturalStrength));
    }

    public double GetCommunityAffinityScore(string communityId)
    {
        return CommunityAffinityScores.TryGetValue(communityId, out var score) ? score : 0.0;
    }

    public double GetCulturalEventWeight(string eventType)
    {
        return CulturalEventWeights.TryGetValue(eventType, out var weight) ? weight : 1.0;
    }

    public double GetLanguageAffinityScore(string language)
    {
        return LanguageAffinityScores.TryGetValue(language, out var score) ? score : 0.0;
    }

    public IEnumerable<string> GetStrongestCommunityAffinities(int topN = 5)
    {
        return CommunityAffinityScores
            .OrderByDescending(kvp => kvp.Value)
            .Take(topN)
            .Select(kvp => kvp.Key);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Region;
        yield return RegionalCulturalStrength;
        yield return string.Join(",", CommunityAffinityScores.OrderBy(kvp => kvp.Key).Select(kvp => $"{kvp.Key}:{kvp.Value:F2}"));
    }
}

/// <summary>
/// Represents load metrics for a specific region
/// </summary>
public class RegionLoadMetrics : ValueObject
{
    public string Region { get; private set; }
    public double CurrentLoadPercentage { get; private set; }
    public int ActiveConnections { get; private set; }
    public double QueriesPerSecond { get; private set; }
    public TimeSpan AverageResponseTime { get; private set; }
    public Dictionary<string, double> ResourceUtilization { get; private set; }
    public DateTime MeasurementTimestamp { get; private set; }

    private RegionLoadMetrics(
        string region,
        double currentLoadPercentage,
        int activeConnections,
        double queriesPerSecond,
        TimeSpan averageResponseTime,
        Dictionary<string, double> resourceUtilization,
        DateTime measurementTimestamp)
    {
        Region = region;
        CurrentLoadPercentage = currentLoadPercentage;
        ActiveConnections = activeConnections;
        QueriesPerSecond = queriesPerSecond;
        AverageResponseTime = averageResponseTime;
        ResourceUtilization = new Dictionary<string, double>(resourceUtilization);
        MeasurementTimestamp = measurementTimestamp;
    }

    public static Result<RegionLoadMetrics> Create(
        string region,
        double currentLoadPercentage,
        int activeConnections,
        double queriesPerSecond,
        TimeSpan averageResponseTime,
        Dictionary<string, double> resourceUtilization,
        DateTime measurementTimestamp)
    {
        if (string.IsNullOrWhiteSpace(region))
            return Result<RegionLoadMetrics>.Failure("Region cannot be empty");

        if (currentLoadPercentage < 0 || currentLoadPercentage > 100)
            return Result<RegionLoadMetrics>.Failure("Load percentage must be between 0 and 100");

        if (activeConnections < 0)
            return Result<RegionLoadMetrics>.Failure("Active connections cannot be negative");

        if (queriesPerSecond < 0)
            return Result<RegionLoadMetrics>.Failure("QPS cannot be negative");

        if (averageResponseTime.TotalMilliseconds < 0)
            return Result<RegionLoadMetrics>.Failure("Average response time cannot be negative");

        return Result<RegionLoadMetrics>.Success(new RegionLoadMetrics(
            region, currentLoadPercentage, activeConnections, queriesPerSecond, 
            averageResponseTime, resourceUtilization, measurementTimestamp));
    }

    public bool IsOverloaded(double thresholdPercentage = 85)
    {
        return CurrentLoadPercentage > thresholdPercentage;
    }

    public bool HasHighLatency(TimeSpan latencyThreshold = default)
    {
        if (latencyThreshold == default)
            latencyThreshold = TimeSpan.FromMilliseconds(200);
        
        return AverageResponseTime > latencyThreshold;
    }

    public double GetResourceUtilization(string resourceType)
    {
        return ResourceUtilization.TryGetValue(resourceType, out var utilization) ? utilization : 0.0;
    }

    public LoadBalancingRecommendation GetLoadBalancingRecommendation()
    {
        if (CurrentLoadPercentage > 90)
            return LoadBalancingRecommendation.ScaleOut;
        
        if (CurrentLoadPercentage > 75)
            return LoadBalancingRecommendation.RedirectTraffic;
        
        if (CurrentLoadPercentage < 30)
            return LoadBalancingRecommendation.ScaleIn;
        
        return LoadBalancingRecommendation.Maintain;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Region;
        yield return CurrentLoadPercentage;
        yield return ActiveConnections;
        yield return QueriesPerSecond;
        yield return AverageResponseTime;
        yield return MeasurementTimestamp;
    }
}

/// <summary>
/// Represents the optimal load distribution configuration for a region
/// </summary>
public class RegionalLoadDistribution : ValueObject
{
    public string Region { get; private set; }
    public int OptimalShardCount { get; private set; }
    public ConnectionPoolConfiguration ConnectionPoolConfiguration { get; private set; }
    public IReadOnlyList<CulturalRoutingRule> CulturalRoutingRules { get; private set; }
    public Dictionary<string, double> LoadBalancingWeights { get; private set; }

    private RegionalLoadDistribution(
        string region,
        int optimalShardCount,
        ConnectionPoolConfiguration connectionPoolConfiguration,
        IEnumerable<CulturalRoutingRule> culturalRoutingRules,
        Dictionary<string, double> loadBalancingWeights)
    {
        Region = region;
        OptimalShardCount = optimalShardCount;
        ConnectionPoolConfiguration = connectionPoolConfiguration;
        CulturalRoutingRules = culturalRoutingRules.ToList().AsReadOnly();
        LoadBalancingWeights = new Dictionary<string, double>(loadBalancingWeights);
    }

    public static Result<RegionalLoadDistribution> Create(
        string region,
        int optimalShardCount,
        ConnectionPoolConfiguration connectionPoolConfiguration,
        IEnumerable<CulturalRoutingRule> culturalRoutingRules,
        Dictionary<string, double> loadBalancingWeights)
    {
        if (string.IsNullOrWhiteSpace(region))
            return Result<RegionalLoadDistribution>.Failure("Region cannot be empty");

        if (optimalShardCount <= 0)
            return Result<RegionalLoadDistribution>.Failure("Optimal shard count must be positive");

        if (connectionPoolConfiguration == null)
            return Result<RegionalLoadDistribution>.Failure("Connection pool configuration cannot be null");

        if (loadBalancingWeights.Any(kvp => kvp.Value < 0))
            return Result<RegionalLoadDistribution>.Failure("Load balancing weights cannot be negative");

        return Result<RegionalLoadDistribution>.Success(new RegionalLoadDistribution(
            region, optimalShardCount, connectionPoolConfiguration, culturalRoutingRules, loadBalancingWeights));
    }

    public double GetLoadBalancingWeight(string communityId)
    {
        return LoadBalancingWeights.TryGetValue(communityId, out var weight) ? weight : 1.0;
    }

    public IEnumerable<CulturalRoutingRule> GetRoutingRulesForCommunity(string communityId)
    {
        return CulturalRoutingRules.Where(rule => rule.AppliesTo(communityId));
    }

    public bool RequiresLoadBalancingAdjustment()
    {
        // Check if weights are severely imbalanced
        var maxWeight = LoadBalancingWeights.Values.Max();
        var minWeight = LoadBalancingWeights.Values.Min();
        return (maxWeight - minWeight) > 0.5; // 50% difference threshold
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Region;
        yield return OptimalShardCount;
        yield return ConnectionPoolConfiguration.PoolName;
        yield return string.Join(",", LoadBalancingWeights.OrderBy(kvp => kvp.Key).Select(kvp => $"{kvp.Key}:{kvp.Value:F2}"));
    }
}

/// <summary>
/// Represents a cultural routing rule for load balancing
/// </summary>
public class CulturalRoutingRule : ValueObject
{
    public string RuleId { get; private set; }
    public string CommunityPattern { get; private set; }
    public string TargetRegion { get; private set; }
    public double Priority { get; private set; }
    public Dictionary<string, object> RoutingConditions { get; private set; }
    public bool IsActive { get; private set; }

    private CulturalRoutingRule(
        string ruleId,
        string communityPattern,
        string targetRegion,
        double priority,
        Dictionary<string, object> routingConditions,
        bool isActive)
    {
        RuleId = ruleId;
        CommunityPattern = communityPattern;
        TargetRegion = targetRegion;
        Priority = priority;
        RoutingConditions = new Dictionary<string, object>(routingConditions);
        IsActive = isActive;
    }

    public static Result<CulturalRoutingRule> Create(
        string ruleId,
        string communityPattern,
        string targetRegion,
        double priority,
        Dictionary<string, object> routingConditions,
        bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(ruleId))
            return Result<CulturalRoutingRule>.Failure("Rule ID cannot be empty");

        if (string.IsNullOrWhiteSpace(communityPattern))
            return Result<CulturalRoutingRule>.Failure("Community pattern cannot be empty");

        if (string.IsNullOrWhiteSpace(targetRegion))
            return Result<CulturalRoutingRule>.Failure("Target region cannot be empty");

        if (priority < 0)
            return Result<CulturalRoutingRule>.Failure("Priority cannot be negative");

        return Result<CulturalRoutingRule>.Success(new CulturalRoutingRule(
            ruleId, communityPattern, targetRegion, priority, routingConditions, isActive));
    }

    public bool AppliesTo(string communityId)
    {
        if (!IsActive) return false;
        
        // Simple pattern matching - can be enhanced with regex or more sophisticated matching
        if (CommunityPattern == "*") return true;
        
        return communityId.Contains(CommunityPattern, StringComparison.OrdinalIgnoreCase);
    }

    public bool MatchesConditions(Dictionary<string, object> currentConditions)
    {
        foreach (var condition in RoutingConditions)
        {
            if (!currentConditions.TryGetValue(condition.Key, out var value) || !value.Equals(condition.Value))
            {
                return false;
            }
        }
        return true;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return RuleId;
        yield return CommunityPattern;
        yield return TargetRegion;
        yield return Priority;
        yield return IsActive;
    }
}

/// <summary>
/// Represents the result of diaspora load balancing calculations
/// </summary>
public class DiasporaLoadBalancingResult : ValueObject
{
    public Dictionary<string, RegionalLoadDistribution> OptimalDistribution { get; private set; }
    public IReadOnlyList<LoadBalancingAction> LoadBalancingActions { get; private set; }
    public double ExpectedPerformanceImprovement { get; private set; }
    public double CulturalAffinityScore { get; private set; }
    public double GeographicEfficiencyScore { get; private set; }
    public DateTime CalculationTimestamp { get; private set; }

    private DiasporaLoadBalancingResult(
        Dictionary<string, RegionalLoadDistribution> optimalDistribution,
        IEnumerable<LoadBalancingAction> loadBalancingActions,
        double expectedPerformanceImprovement,
        double culturalAffinityScore,
        double geographicEfficiencyScore,
        DateTime calculationTimestamp)
    {
        OptimalDistribution = new Dictionary<string, RegionalLoadDistribution>(optimalDistribution);
        LoadBalancingActions = loadBalancingActions.ToList().AsReadOnly();
        ExpectedPerformanceImprovement = expectedPerformanceImprovement;
        CulturalAffinityScore = culturalAffinityScore;
        GeographicEfficiencyScore = geographicEfficiencyScore;
        CalculationTimestamp = calculationTimestamp;
    }

    public static Result<DiasporaLoadBalancingResult> Create(
        Dictionary<string, RegionalLoadDistribution> optimalDistribution,
        IEnumerable<LoadBalancingAction> loadBalancingActions,
        double expectedPerformanceImprovement,
        double culturalAffinityScore,
        double geographicEfficiencyScore)
    {
        if (!optimalDistribution.Any())
            return Result<DiasporaLoadBalancingResult>.Failure("Optimal distribution cannot be empty");

        if (expectedPerformanceImprovement < 0)
            return Result<DiasporaLoadBalancingResult>.Failure("Expected performance improvement cannot be negative");

        if (culturalAffinityScore < 0 || culturalAffinityScore > 1)
            return Result<DiasporaLoadBalancingResult>.Failure("Cultural affinity score must be between 0 and 1");

        if (geographicEfficiencyScore < 0 || geographicEfficiencyScore > 1)
            return Result<DiasporaLoadBalancingResult>.Failure("Geographic efficiency score must be between 0 and 1");

        return Result<DiasporaLoadBalancingResult>.Success(new DiasporaLoadBalancingResult(
            optimalDistribution, loadBalancingActions, expectedPerformanceImprovement, 
            culturalAffinityScore, geographicEfficiencyScore, DateTime.UtcNow));
    }

    public double GetOverallOptimizationScore()
    {
        return (ExpectedPerformanceImprovement * 0.4) + 
               (CulturalAffinityScore * 0.35) + 
               (GeographicEfficiencyScore * 0.25);
    }

    public IEnumerable<LoadBalancingAction> GetHighPriorityActions()
    {
        return LoadBalancingActions.Where(action => action.Priority >= LoadBalancingActionPriority.High);
    }

    public RegionalLoadDistribution? GetDistributionForRegion(string region)
    {
        return OptimalDistribution.TryGetValue(region, out var distribution) ? distribution : null;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return ExpectedPerformanceImprovement;
        yield return CulturalAffinityScore;
        yield return GeographicEfficiencyScore;
        yield return CalculationTimestamp;
    }
}

/// <summary>
/// Represents a load balancing action to be executed
/// </summary>
public class LoadBalancingAction : ValueObject
{
    public string ActionId { get; private set; }
    public LoadBalancingActionType ActionType { get; private set; }
    public string SourceRegion { get; private set; }
    public string TargetRegion { get; private set; }
    public double TrafficPercentage { get; private set; }
    public LoadBalancingActionPriority Priority { get; private set; }
    public TimeSpan EstimatedExecutionTime { get; private set; }
    public string Reason { get; private set; }

    private LoadBalancingAction(
        string actionId,
        LoadBalancingActionType actionType,
        string sourceRegion,
        string targetRegion,
        double trafficPercentage,
        LoadBalancingActionPriority priority,
        TimeSpan estimatedExecutionTime,
        string reason)
    {
        ActionId = actionId;
        ActionType = actionType;
        SourceRegion = sourceRegion;
        TargetRegion = targetRegion;
        TrafficPercentage = trafficPercentage;
        Priority = priority;
        EstimatedExecutionTime = estimatedExecutionTime;
        Reason = reason;
    }

    public static Result<LoadBalancingAction> Create(
        string actionId,
        LoadBalancingActionType actionType,
        string sourceRegion,
        string targetRegion,
        double trafficPercentage,
        LoadBalancingActionPriority priority,
        TimeSpan estimatedExecutionTime,
        string reason)
    {
        if (string.IsNullOrWhiteSpace(actionId))
            return Result<LoadBalancingAction>.Failure("Action ID cannot be empty");

        if (string.IsNullOrWhiteSpace(sourceRegion))
            return Result<LoadBalancingAction>.Failure("Source region cannot be empty");

        if (string.IsNullOrWhiteSpace(targetRegion))
            return Result<LoadBalancingAction>.Failure("Target region cannot be empty");

        if (trafficPercentage < 0 || trafficPercentage > 100)
            return Result<LoadBalancingAction>.Failure("Traffic percentage must be between 0 and 100");

        if (estimatedExecutionTime.TotalSeconds < 0)
            return Result<LoadBalancingAction>.Failure("Execution time cannot be negative");

        if (string.IsNullOrWhiteSpace(reason))
            return Result<LoadBalancingAction>.Failure("Reason cannot be empty");

        return Result<LoadBalancingAction>.Success(new LoadBalancingAction(
            actionId, actionType, sourceRegion, targetRegion, trafficPercentage, 
            priority, estimatedExecutionTime, reason));
    }

    public bool IsHighPriority()
    {
        return Priority >= LoadBalancingActionPriority.High;
    }

    public bool RequiresImmediateExecution()
    {
        return Priority == LoadBalancingActionPriority.Critical;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return ActionId;
        yield return ActionType;
        yield return SourceRegion;
        yield return TargetRegion;
        yield return TrafficPercentage;
        yield return Priority;
        yield return Reason;
    }
}

/// <summary>
/// Request for diaspora load balancing calculation
/// </summary>
public class DiasporaLoadBalancingRequest : ValueObject
{
    public IReadOnlyList<string> Communities { get; private set; }
    public IReadOnlyList<string> TargetRegions { get; private set; }
    public DateTime RequestTimestamp { get; private set; }
    public string RequestedBy { get; private set; }
    public Dictionary<string, object> OptimizationParameters { get; private set; }

    private DiasporaLoadBalancingRequest(
        IEnumerable<string> communities,
        IEnumerable<string> targetRegions,
        string requestedBy,
        Dictionary<string, object> optimizationParameters)
    {
        Communities = communities.ToList().AsReadOnly();
        TargetRegions = targetRegions.ToList().AsReadOnly();
        RequestTimestamp = DateTime.UtcNow;
        RequestedBy = requestedBy;
        OptimizationParameters = new Dictionary<string, object>(optimizationParameters);
    }

    public static Result<DiasporaLoadBalancingRequest> Create(
        IEnumerable<string> communities,
        IEnumerable<string> targetRegions,
        string requestedBy,
        Dictionary<string, object>? optimizationParameters = null)
    {
        if (!communities.Any())
            return Result<DiasporaLoadBalancingRequest>.Failure("At least one community must be specified");

        if (!targetRegions.Any())
            return Result<DiasporaLoadBalancingRequest>.Failure("At least one target region must be specified");

        if (string.IsNullOrWhiteSpace(requestedBy))
            return Result<DiasporaLoadBalancingRequest>.Failure("Requested by cannot be empty");

        return Result<DiasporaLoadBalancingRequest>.Success(new DiasporaLoadBalancingRequest(
            communities, targetRegions, requestedBy, optimizationParameters ?? new Dictionary<string, object>()));
    }

    public T? GetOptimizationParameter<T>(string parameterName, T? defaultValue = default(T))
    {
        if (OptimizationParameters.TryGetValue(parameterName, out var value) && value is T typedValue)
        {
            return typedValue;
        }
        return defaultValue;
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return string.Join(",", Communities.OrderBy(c => c));
        yield return string.Join(",", TargetRegions.OrderBy(r => r));
        yield return RequestedBy;
        yield return RequestTimestamp;
    }
}

/// <summary>
/// Enumerations for geographic load balancing
/// </summary>
public enum LoadBalancingRecommendation
{
    Maintain,
    ScaleOut,
    ScaleIn,
    RedirectTraffic,
    LoadBalance
}

public enum LoadBalancingActionType
{
    TrafficRedirection,
    RegionScaling,
    ConnectionPoolAdjustment,
    ShardRebalancing,
    FailoverActivation,
    CulturalRoutingUpdate
}

public enum LoadBalancingActionPriority
{
    Low,
    Medium,
    High,
    Critical
}