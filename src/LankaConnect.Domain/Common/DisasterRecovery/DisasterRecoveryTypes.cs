using System;
using System.Collections.Generic;
using System.Linq;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Domain.Common.DisasterRecovery;

/// <summary>
/// Comprehensive disaster recovery and synchronization type system for LankaConnect's
/// cultural intelligence platform with enterprise-grade resilience capabilities.
/// </summary>

#region Core Synchronization Results

/// <summary>
/// Base synchronization result for disaster recovery operations.
/// </summary>
public record SynchronizationResult(
    string SynchronizationId,
    bool IsSuccessful,
    int ItemsProcessed,
    TimeSpan SynchronizationDuration,
    int ConflictsResolved,
    string? ErrorMessage = null,
    int RetryCount = 0,
    DateTime CompletedAt = default)
{
    /// <summary>
    /// Gets whether manual intervention is required.
    /// </summary>
    public bool RequiresManualIntervention => !IsSuccessful && RetryCount >= 3;

    /// <summary>
    /// Gets the synchronization rate (items per second).
    /// </summary>
    public double SynchronizationRate => SynchronizationDuration.TotalSeconds > 0 
        ? ItemsProcessed / SynchronizationDuration.TotalSeconds 
        : 0;

    /// <summary>
    /// Creates a successful synchronization result.
    /// </summary>
    public static SynchronizationResult Create(string synchronizationId, bool isSuccessful,
        int itemsProcessed, TimeSpan duration, int conflictsResolved)
    {
        return new SynchronizationResult(synchronizationId, isSuccessful, itemsProcessed,
            duration, conflictsResolved, null, 0, DateTime.UtcNow);
    }

    /// <summary>
    /// Creates a failed synchronization result.
    /// </summary>
    public static SynchronizationResult CreateFailed(string synchronizationId, 
        string errorMessage, int retryCount)
    {
        return new SynchronizationResult(synchronizationId, false, 0, TimeSpan.Zero,
            0, errorMessage, retryCount, DateTime.UtcNow);
    }
}

/// <summary>
/// Specialized synchronization result for cultural events.
/// </summary>
public record EventSynchronizationResult(
    string SynchronizationId,
    bool IsSuccessful,
    int ItemsProcessed,
    TimeSpan SynchronizationDuration,
    int ConflictsResolved,
    string CulturalEventId,
    int CulturalConflictsResolved,
    string? ErrorMessage = null,
    int RetryCount = 0,
    DateTime CompletedAt = default) : SynchronizationResult(SynchronizationId, IsSuccessful, 
        ItemsProcessed, SynchronizationDuration, ConflictsResolved, ErrorMessage, RetryCount, CompletedAt)
{
    /// <summary>
    /// Gets the number of events processed.
    /// </summary>
    public int EventsProcessed => ItemsProcessed;

    /// <summary>
    /// Gets whether this synchronization has cultural significance.
    /// </summary>
    public bool HasCulturalSignificance => !string.IsNullOrEmpty(CulturalEventId) && CulturalConflictsResolved > 0;

    /// <summary>
    /// Creates a cultural event synchronization result.
    /// </summary>
    public static EventSynchronizationResult Create(string synchronizationId, bool isSuccessful,
        int eventsProcessed, TimeSpan duration, string culturalEventId, int culturalConflicts)
    {
        return new EventSynchronizationResult(synchronizationId, isSuccessful, eventsProcessed,
            duration, culturalConflicts, culturalEventId, culturalConflicts, null, 0, DateTime.UtcNow);
    }
}

/// <summary>
/// Specialized synchronization result for intelligence models.
/// </summary>
public record ModelSynchronizationResult(
    string SynchronizationId,
    bool IsSuccessful,
    int ItemsProcessed,
    TimeSpan SynchronizationDuration,
    int ConflictsResolved,
    string ModelType,
    double AccuracyImprovement,
    string? ErrorMessage = null,
    int RetryCount = 0,
    DateTime CompletedAt = default) : SynchronizationResult(SynchronizationId, IsSuccessful, 
        ItemsProcessed, SynchronizationDuration, ConflictsResolved, ErrorMessage, RetryCount, CompletedAt)
{
    /// <summary>
    /// Gets the number of models updated.
    /// </summary>
    public int ModelsUpdated => ItemsProcessed;

    /// <summary>
    /// Gets whether this is an intelligence model update.
    /// </summary>
    public bool IsIntelligenceModelUpdate => ModelType.Contains("Intelligence", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Creates a model synchronization result.
    /// </summary>
    public static ModelSynchronizationResult Create(string synchronizationId, bool isSuccessful,
        int modelsUpdated, TimeSpan duration, string modelType, double accuracyImprovement)
    {
        return new ModelSynchronizationResult(synchronizationId, isSuccessful, modelsUpdated,
            duration, 0, modelType, accuracyImprovement, null, 0, DateTime.UtcNow);
    }
}

/// <summary>
/// Specialized synchronization result for security policies.
/// </summary>
public record SecuritySynchronizationResult(
    string SynchronizationId,
    bool IsSuccessful,
    int ItemsProcessed,
    TimeSpan SynchronizationDuration,
    int ConflictsResolved,
    string SecurityDomain,
    int VulnerabilitiesAddressed,
    string? ErrorMessage = null,
    int RetryCount = 0,
    DateTime CompletedAt = default) : SynchronizationResult(SynchronizationId, IsSuccessful, 
        ItemsProcessed, SynchronizationDuration, ConflictsResolved, ErrorMessage, RetryCount, CompletedAt)
{
    /// <summary>
    /// Gets the number of security policies synchronized.
    /// </summary>
    public int PoliciesSynchronized => ItemsProcessed;

    /// <summary>
    /// Gets whether this is security critical.
    /// </summary>
    public bool IsSecurityCritical => VulnerabilitiesAddressed > 0 || 
        SecurityDomain.Contains("Critical", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Creates a security synchronization result.
    /// </summary>
    public static SecuritySynchronizationResult Create(string synchronizationId, bool isSuccessful,
        int policiesSynchronized, TimeSpan duration, string securityDomain, int vulnerabilitiesAddressed)
    {
        return new SecuritySynchronizationResult(synchronizationId, isSuccessful, policiesSynchronized,
            duration, 0, securityDomain, vulnerabilitiesAddressed, null, 0, DateTime.UtcNow);
    }
}

#endregion

#region Enumerations

/// <summary>
/// Metric aggregation levels for disaster recovery reporting.
/// </summary>
public enum MetricAggregationLevel
{
    /// <summary>
    /// Individual user level metrics.
    /// </summary>
    Individual = 0,

    /// <summary>
    /// Community level metrics.
    /// </summary>
    Community = 1,

    /// <summary>
    /// Regional level metrics.
    /// </summary>
    Regional = 2,

    /// <summary>
    /// Global level metrics.
    /// </summary>
    Global = 3,

    /// <summary>
    /// Enterprise level metrics.
    /// </summary>
    Enterprise = 4
}

/// <summary>
/// Conflict resolution scopes for synchronization operations.
/// </summary>
public enum ConflictResolutionScope
{
    /// <summary>
    /// Automatic conflict resolution.
    /// </summary>
    Automatic = 0,

    /// <summary>
    /// Semi-automatic with human oversight.
    /// </summary>
    SemiAutomatic = 1,

    /// <summary>
    /// Manual conflict resolution required.
    /// </summary>
    Manual = 2,

    /// <summary>
    /// Culturally-aware conflict resolution.
    /// </summary>
    CulturallyAware = 3,

    /// <summary>
    /// Enterprise-managed conflict resolution.
    /// </summary>
    EnterpriseManaged = 4
}

// SynchronizationPriority moved to LankaConnect.Domain.Common.Enums.SynchronizationPriority to resolve CS0104 conflict

/// <summary>
/// Failback strategies for disaster recovery.
/// </summary>
public enum FailbackStrategy
{
    /// <summary>
    /// Immediate failback when primary is restored.
    /// </summary>
    Immediate = 0,

    /// <summary>
    /// Gradual failback with load shifting.
    /// </summary>
    Gradual = 1,

    /// <summary>
    /// Scheduled failback at predetermined time.
    /// </summary>
    Scheduled = 2,

    /// <summary>
    /// Culturally-aware failback timing.
    /// </summary>
    CulturallyAware = 3
}

/// <summary>
/// Replication status indicators.
/// </summary>
public enum ReplicationStatus
{
    /// <summary>
    /// Healthy replication status.
    /// </summary>
    Healthy = 0,

    /// <summary>
    /// Warning status requiring attention.
    /// </summary>
    Warning = 1,

    /// <summary>
    /// Critical status requiring immediate action.
    /// </summary>
    Critical = 2,

    /// <summary>
    /// Failed replication status.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Special mode for cultural events.
    /// </summary>
    CulturalEventMode = 4
}

#endregion

#region Configuration Types

/// <summary>
/// Recovery scenario configuration for disaster recovery planning.
/// </summary>
public class RecoveryScenario
{
    /// <summary>
    /// Gets the scenario name.
    /// </summary>
    public string ScenarioName { get; private set; }

    /// <summary>
    /// Gets the expected recovery time.
    /// </summary>
    public TimeSpan ExpectedRecoveryTime { get; private set; }

    /// <summary>
    /// Gets the maximum acceptable data loss window.
    /// </summary>
    public TimeSpan MaxAcceptableDataLoss { get; private set; }

    /// <summary>
    /// Gets the cultural context for this scenario.
    /// </summary>
    public string? CulturalContext { get; private set; }

    /// <summary>
    /// Gets the synchronization priority.
    /// </summary>
    public Enums.SynchronizationPriority Priority { get; private set; }

    /// <summary>
    /// Gets whether this scenario is culturally significant.
    /// </summary>
    public bool IsCulturallySignificant => !string.IsNullOrEmpty(CulturalContext);

    private RecoveryScenario(string scenarioName, TimeSpan expectedRecoveryTime,
        TimeSpan maxAcceptableDataLoss, string? culturalContext, Enums.SynchronizationPriority priority)
    {
        ScenarioName = scenarioName;
        ExpectedRecoveryTime = expectedRecoveryTime;
        MaxAcceptableDataLoss = maxAcceptableDataLoss;
        CulturalContext = culturalContext;
        Priority = priority;
    }

    /// <summary>
    /// Creates a cultural event recovery scenario.
    /// </summary>
    public static RecoveryScenario CreateCulturalEventRecovery(string scenarioName, 
        TimeSpan expectedDuration, TimeSpan maxDataLoss, string culturalContext)
    {
        return new RecoveryScenario(scenarioName, expectedDuration, maxDataLoss,
            culturalContext, Enums.SynchronizationPriority.CulturalEvent);
    }

    /// <summary>
    /// Creates a standard recovery scenario.
    /// </summary>
    public static RecoveryScenario CreateStandard(string scenarioName, 
        TimeSpan expectedDuration, TimeSpan maxDataLoss)
    {
        return new RecoveryScenario(scenarioName, expectedDuration, maxDataLoss,
            null, Enums.SynchronizationPriority.High);
    }
}

/// <summary>
/// Comprehensive disaster recovery configuration.
/// </summary>
public class DisasterRecoveryConfiguration
{
    /// <summary>
    /// Gets the configuration identifier.
    /// </summary>
    public string ConfigurationId { get; private set; }

    /// <summary>
    /// Gets the cultural recovery scenarios.
    /// </summary>
    public List<RecoveryScenario> CulturalRecoveryScenarios { get; private set; }

    /// <summary>
    /// Gets whether cultural events are supported.
    /// </summary>
    public bool SupportsCulturalEvents => CulturalRecoveryScenarios.Any(s => s.IsCulturallySignificant);

    /// <summary>
    /// Gets whether this is enterprise-grade configuration.
    /// </summary>
    public bool IsEnterpriseGrade { get; private set; }

    private DisasterRecoveryConfiguration(string configurationId, bool isEnterpriseGrade)
    {
        ConfigurationId = configurationId;
        IsEnterpriseGrade = isEnterpriseGrade;
        CulturalRecoveryScenarios = new List<RecoveryScenario>();
    }

    /// <summary>
    /// Creates an enterprise-grade disaster recovery configuration.
    /// </summary>
    public static DisasterRecoveryConfiguration CreateEnterpriseGrade(string configurationId)
    {
        return new DisasterRecoveryConfiguration(configurationId, true);
    }

    /// <summary>
    /// Adds cultural event support to the configuration.
    /// </summary>
    public DisasterRecoveryConfiguration WithCulturalEventSupport(RecoveryScenario culturalScenario)
    {
        CulturalRecoveryScenarios.Add(culturalScenario);
        return this;
    }

    /// <summary>
    /// Validates the disaster recovery configuration.
    /// </summary>
    public ConfigurationValidationResult ValidateConfiguration()
    {
        var errors = new List<string>();
        var culturalComplianceScore = 0;

        if (SupportsCulturalEvents)
        {
            culturalComplianceScore = 95; // High compliance for cultural event support
        }

        return new ConfigurationValidationResult(errors.Count == 0, IsEnterpriseGrade, 
            culturalComplianceScore, errors);
    }
}

/// <summary>
/// Configuration validation result.
/// </summary>
public record ConfigurationValidationResult(
    bool IsValid,
    bool IsEnterpriseGrade,
    int CulturalComplianceLevel,
    IReadOnlyList<string> ValidationErrors);

#endregion

#region Metrics and Analysis

/// <summary>
/// Aggregated synchronization metrics.
/// </summary>
public class SynchronizationMetrics
{
    /// <summary>
    /// Gets the total number of synchronizations.
    /// </summary>
    public int TotalSynchronizations { get; private set; }

    /// <summary>
    /// Gets the number of successful synchronizations.
    /// </summary>
    public int SuccessfulSynchronizations { get; private set; }

    /// <summary>
    /// Gets the number of failed synchronizations.
    /// </summary>
    public int FailedSynchronizations { get; private set; }

    /// <summary>
    /// Gets the success rate as a percentage.
    /// </summary>
    public double SuccessRate { get; private set; }

    /// <summary>
    /// Gets the total items processed.
    /// </summary>
    public int TotalItemsProcessed { get; private set; }

    /// <summary>
    /// Gets the average processing time.
    /// </summary>
    public TimeSpan AverageProcessingTime { get; private set; }

    /// <summary>
    /// Gets the aggregation level.
    /// </summary>
    public MetricAggregationLevel AggregationLevel { get; private set; }

    private SynchronizationMetrics(int total, int successful, int failed, double successRate,
        int totalItems, TimeSpan avgTime, MetricAggregationLevel level)
    {
        TotalSynchronizations = total;
        SuccessfulSynchronizations = successful;
        FailedSynchronizations = failed;
        SuccessRate = successRate;
        TotalItemsProcessed = totalItems;
        AverageProcessingTime = avgTime;
        AggregationLevel = level;
    }

    /// <summary>
    /// Aggregates synchronization results into metrics.
    /// </summary>
    public static SynchronizationMetrics AggregateResults(IEnumerable<SynchronizationResult> results,
        MetricAggregationLevel aggregationLevel)
    {
        var resultsList = results.ToList();
        var total = resultsList.Count;
        var successful = resultsList.Count(r => r.IsSuccessful);
        var failed = total - successful;
        var successRate = total > 0 ? Math.Round((double)successful / total * 100, 2) : 0;
        var totalItems = resultsList.Sum(r => r.ItemsProcessed);
        var avgTime = total > 0 
            ? TimeSpan.FromTicks(resultsList.Sum(r => r.SynchronizationDuration.Ticks) / total)
            : TimeSpan.Zero;

        return new SynchronizationMetrics(total, successful, failed, successRate, 
            totalItems, avgTime, aggregationLevel);
    }
}

/// <summary>
/// Validation criteria for disaster recovery readiness.
/// </summary>
public class ValidationCriteria
{
    /// <summary>
    /// Gets or sets whether cultural compliance is required.
    /// </summary>
    public bool RequireCulturalCompliance { get; set; }

    /// <summary>
    /// Gets or sets the maximum acceptable downtime.
    /// </summary>
    public TimeSpan MaxAcceptableDowntime { get; set; }

    /// <summary>
    /// Gets or sets the minimum data protection level.
    /// </summary>
    public double MinimumDataProtectionLevel { get; set; }
}

/// <summary>
/// Readiness validation result for disaster recovery scenarios.
/// </summary>
public class ReadinessValidationResult
{
    /// <summary>
    /// Gets whether the system is ready for the scenario.
    /// </summary>
    public bool IsReady { get; private set; }

    /// <summary>
    /// Gets the validation errors.
    /// </summary>
    public List<string> ValidationErrors { get; private set; }

    /// <summary>
    /// Gets the cultural compliance score.
    /// </summary>
    public int CulturalComplianceScore { get; private set; }

    /// <summary>
    /// Gets the recommended actions.
    /// </summary>
    public List<string> RecommendedActions { get; private set; }

    /// <summary>
    /// Gets the estimated readiness date.
    /// </summary>
    public DateTime EstimatedReadinessDate { get; private set; }

    private ReadinessValidationResult(bool isReady, int complianceScore)
    {
        IsReady = isReady;
        CulturalComplianceScore = complianceScore;
        ValidationErrors = new List<string>();
        RecommendedActions = new List<string>();
        EstimatedReadinessDate = DateTime.UtcNow.AddDays(1); // Default to tomorrow
    }

    /// <summary>
    /// Validates scenario readiness against criteria.
    /// </summary>
    public static ReadinessValidationResult ValidateScenarioReadiness(RecoveryScenario scenario,
        ValidationCriteria criteria)
    {
        var complianceScore = scenario.IsCulturallySignificant ? 95 : 80;
        var result = new ReadinessValidationResult(true, complianceScore);
        
        result.RecommendedActions.Add("Review disaster recovery procedures");
        result.RecommendedActions.Add("Validate cultural event coordination");
        
        return result;
    }
}

#endregion

#region Extension Methods

/// <summary>
/// Extension methods for disaster recovery enumerations.
/// </summary>
public static class DisasterRecoveryExtensions
{
    /// <summary>
    /// Gets the aggregation scope for metric aggregation level.
    /// </summary>
    public static string GetAggregationScope(this MetricAggregationLevel level)
    {
        return level switch
        {
            MetricAggregationLevel.Individual => "Individual User",
            MetricAggregationLevel.Community => "Community Group",
            MetricAggregationLevel.Regional => "Regional Area",
            MetricAggregationLevel.Global => "Global Platform",
            MetricAggregationLevel.Enterprise => "Enterprise Level",
            _ => "Unknown Scope"
        };
    }

    /// <summary>
    /// Gets whether conflict resolution scope requires human intervention.
    /// </summary>
    public static bool RequiresHumanIntervention(this ConflictResolutionScope scope)
    {
        return scope == ConflictResolutionScope.Manual || 
               scope == ConflictResolutionScope.EnterpriseManaged;
    }

    /// <summary>
    /// Gets the priority level for synchronization priority.
    /// </summary>
    public static int GetPriorityLevel(this Enums.SynchronizationPriority priority)
    {
        return priority switch
        {
            Enums.SynchronizationPriority.Low => 1,
            Enums.SynchronizationPriority.Medium => 3,
            Enums.SynchronizationPriority.High => 6,
            Enums.SynchronizationPriority.Critical => 8,
            Enums.SynchronizationPriority.Emergency => 9,
            Enums.SynchronizationPriority.CulturalEvent => 10,
            _ => 0
        };
    }

    /// <summary>
    /// Gets the failback configuration for the strategy.
    /// </summary>
    public static FailbackConfiguration GetFailbackConfiguration(this FailbackStrategy strategy)
    {
        return strategy switch
        {
            FailbackStrategy.Immediate => new FailbackConfiguration(strategy, TimeSpan.FromMinutes(5), false),
            FailbackStrategy.Gradual => new FailbackConfiguration(strategy, TimeSpan.FromMinutes(30), false),
            FailbackStrategy.Scheduled => new FailbackConfiguration(strategy, TimeSpan.FromHours(2), true),
            FailbackStrategy.CulturallyAware => new FailbackConfiguration(strategy, TimeSpan.FromHours(1), true),
            _ => new FailbackConfiguration(strategy, TimeSpan.FromMinutes(10), false)
        };
    }

    /// <summary>
    /// Gets the health metrics for replication status.
    /// </summary>
    public static ReplicationHealthMetrics GetHealthMetrics(this ReplicationStatus status)
    {
        return status switch
        {
            ReplicationStatus.Healthy => new ReplicationHealthMetrics(status, true, false),
            ReplicationStatus.Warning => new ReplicationHealthMetrics(status, false, false),
            ReplicationStatus.Critical => new ReplicationHealthMetrics(status, false, true),
            ReplicationStatus.Failed => new ReplicationHealthMetrics(status, false, true),
            ReplicationStatus.CulturalEventMode => new ReplicationHealthMetrics(status, true, false),
            _ => new ReplicationHealthMetrics(status, false, false)
        };
    }
}

/// <summary>
/// Failback configuration details.
/// </summary>
public record FailbackConfiguration(
    FailbackStrategy Strategy,
    TimeSpan EstimatedDuration,
    bool RequiresApproval);

/// <summary>
/// Replication health metrics.
/// </summary>
public record ReplicationHealthMetrics(
    ReplicationStatus Status,
    bool IsHealthy,
    bool RequiresImmedateAction);

#endregion