using LankaConnect.Domain.Common.Database;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Enterprise;
using LankaConnect.Domain.Common.Models;
using LankaConnect.Domain.Common.Monitoring;
using LankaConnect.Domain.Common.ValueObjects;
using LankaConnect.Domain.Common.Security;
using LankaConnect.Domain.Common.Recovery;
using LankaConnect.Domain.Common;
using MultiLanguageModels = LankaConnect.Domain.Common.Database.MultiLanguageRoutingModels;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Cultural Event Load Distribution Service interface for managing traffic distribution during cultural festivals
/// Architect recommendation: Core service interface with cultural event modeling approach
/// 
/// Key Features:
/// - Predictive scaling for cultural events (Buddhist calendar, Hindu festivals, Islamic observances)
/// - Multi-cultural event conflict resolution (overlapping festivals)
/// - Fortune 500 SLA compliance (<200ms response time, 99.9% uptime)
/// - Integration with existing Cultural Affinity Geographic Load Balancer
/// </summary>
public interface ICulturalEventLoadDistributionService : IDisposable
{
    /// <summary>
    /// Distributes load for a cultural event with intelligent routing and scaling
    /// Handles festival-specific traffic multipliers: Vesak 5x, Diwali 4.5x, Eid 4x
    /// </summary>
    /// <param name="request">Cultural event load distribution request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Load distribution response with optimal server allocations</returns>
    Task<CulturalEventLoadDistributionResponse> DistributeLoadAsync(
        CulturalEventLoadDistributionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates predictive scaling plan for cultural events based on historical data and ML predictions
    /// Prediction Accuracy: Vesak 95%, Diwali 90%, Eid 88% (lunar calendar precision)
    /// </summary>
    /// <param name="request">Predictive scaling plan request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Comprehensive scaling plan with timing and resource allocation</returns>
    Task<PredictiveScalingPlan> GenerateScalingPlanAsync(
        PredictiveScalingPlanRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves conflicts between overlapping cultural events using intelligent prioritization
    /// Priority Matrix: Sacred Events (Vesak, Eid): Priority 10, Major Festivals (Diwali): Priority 9
    /// </summary>
    /// <param name="request">Cultural event conflict resolution request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Conflict resolution with resource allocations and community notifications</returns>
    Task<CulturalEventConflictResolution> ResolveEventConflictsAsync(
        CulturalEventConflictResolutionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Monitors performance during cultural events to ensure Fortune 500 SLA compliance
    /// Performance Guarantees: <200ms response time under 5x traffic load, 99.9% uptime
    /// </summary>
    /// <param name="request">Performance monitoring request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Real-time performance metrics and SLA compliance status</returns>
    Task<FortuneHundredPerformanceMetrics> MonitorPerformanceAsync(
        FortuneHundredPerformanceMonitoringRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Cultural Event Prediction Engine interface for ML-powered event prediction
/// Architect recommendation: Feature engineering with cultural calendar data, diaspora demographics
/// </summary>
public interface ICulturalEventPredictionEngine : IDisposable
{
    /// <summary>
    /// Distributes load for a cultural event using machine learning predictions
    /// Integrates Buddhist/Hindu/Islamic calendar APIs for precise timing
    /// </summary>
    /// <param name="request">Load distribution request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Optimized load distribution response</returns>
    Task<CulturalEventLoadDistributionResponse> DistributeLoadForCulturalEventAsync(
        CulturalEventLoadDistributionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates predictive scaling plan using ML algorithms and cultural calendar integration
    /// Continuous model improvement with real-time traffic feedback
    /// </summary>
    /// <param name="request">Scaling plan request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>ML-generated scaling plan</returns>
    Task<PredictiveScalingPlan> GeneratePredictiveScalingPlanAsync(
        PredictiveScalingPlanRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Predicts traffic patterns for cultural events based on historical data
    /// Cultural significance scoring for priority-based scaling
    /// </summary>
    /// <param name="culturalEventType">Type of cultural event</param>
    /// <param name="eventDateTime">Event date and time</param>
    /// <param name="geographicScope">Geographic scope</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Traffic prediction with confidence interval</returns>
    Task<TrafficPrediction> PredictTrafficAsync(
        CulturalEventType culturalEventType,
        DateTime eventDateTime,
        GeographicCulturalScope geographicScope,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Cultural Conflict Resolver interface for multi-cultural event conflict resolution
/// Architect recommendation: Automated resolution of overlapping festivals
/// </summary>
public interface ICulturalConflictResolver : IDisposable
{
    /// <summary>
    /// Resolves conflicts between multiple cultural events using prioritization strategies
    /// Sacred Event Priority: Vesak and Eid receive highest priority (Level 10)
    /// </summary>
    /// <param name="request">Conflict resolution request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Conflict resolution with resource allocation matrix</returns>
    Task<CulturalEventConflictResolution> ResolveMultiCulturalConflictsAsync(
        CulturalEventConflictResolutionRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates optimal resource allocation for conflicting events
    /// Dynamic resource distribution based on cultural significance
    /// </summary>
    /// <param name="conflictingEvents">List of conflicting events</param>
    /// <param name="totalCapacity">Total available capacity</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Optimal resource allocation plan</returns>
    Task<List<ResourceAllocation>> CalculateResourceAllocationAsync(
        List<CulturalEventSchedule> conflictingEvents,
        int totalCapacity,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates community notification messages for event conflicts
    /// Cross-cultural communication for affected communities
    /// </summary>
    /// <param name="resolution">Conflict resolution details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Localized notification messages for communities</returns>
    Task<List<CommunityNotification>> GenerateCommunityNotificationsAsync(
        CulturalEventConflictResolution resolution,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Fortune 500 Performance Optimizer interface for SLA compliance
/// Architect recommendation: Sub-200ms Response Time Architecture with intelligent optimization
/// </summary>
public interface IFortuneHundredPerformanceOptimizer : IDisposable
{
    /// <summary>
    /// Monitors performance metrics in real-time during cultural events
    /// Intelligent query optimization with cultural event-specific indexing
    /// </summary>
    /// <param name="request">Performance monitoring request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Comprehensive performance metrics</returns>
    Task<FortuneHundredPerformanceMetrics> MonitorPerformanceAsync(
        FortuneHundredPerformanceMonitoringRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Optimizes system performance for cultural event traffic patterns
    /// Connection pool optimization based on predicted traffic patterns
    /// </summary>
    /// <param name="culturalEventType">Type of cultural event</param>
    /// <param name="expectedTrafficMultiplier">Expected traffic increase</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Performance optimization results</returns>
    Task<PerformanceOptimizationResult> OptimizeForCulturalEventAsync(
        CulturalEventType culturalEventType,
        decimal expectedTrafficMultiplier,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates SLA compliance during high-traffic cultural events
    /// Continuous monitoring with automatic alerts for SLA violations
    /// </summary>
    /// <param name="performanceMetrics">Current performance metrics</param>
    /// <param name="slaRequirements">SLA requirements</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>SLA compliance validation result</returns>
    Task<SlaComplianceResult> ValidateSlaComplianceAsync(
        FortuneHundredPerformanceMetrics performanceMetrics,
        SlaRequirements slaRequirements,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Traffic prediction result from ML models
/// </summary>
public class TrafficPrediction
{
    /// <summary>
    /// Predicted traffic multiplier
    /// </summary>
    public decimal TrafficMultiplier { get; set; }

    /// <summary>
    /// Confidence level of prediction (0.0 to 1.0)
    /// </summary>
    public decimal ConfidenceLevel { get; set; }

    /// <summary>
    /// Peak traffic time prediction
    /// </summary>
    public DateTime PredictedPeakTime { get; set; }

    /// <summary>
    /// Expected duration of high traffic
    /// </summary>
    public TimeSpan ExpectedDuration { get; set; }
}

/// <summary>
/// Community notification for cultural events
/// </summary>
public class CommunityNotification
{
    /// <summary>
    /// Target community (e.g., "Buddhist", "Hindu", "Islamic")
    /// </summary>
    public string TargetCommunity { get; set; } = string.Empty;

    /// <summary>
    /// Notification message in appropriate language
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Language code (e.g., "si-LK", "ta-LK", "hi-IN")
    /// </summary>
    public string LanguageCode { get; set; } = string.Empty;

    /// <summary>
    /// Cultural appropriateness score (0.0 to 1.0)
    /// </summary>
    public decimal CulturalAppropriatenessScore { get; set; }
}

/// <summary>
/// Performance optimization result
/// </summary>
public class PerformanceOptimizationResult
{
    /// <summary>
    /// Whether optimization was successful
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Applied optimizations
    /// </summary>
    public List<string> AppliedOptimizations { get; set; } = new();

    /// <summary>
    /// Expected performance improvement
    /// </summary>
    public decimal ExpectedImprovementPercentage { get; set; }

    /// <summary>
    /// Optimization execution time
    /// </summary>
    public TimeSpan ExecutionTime { get; set; }
}

/// <summary>
/// SLA compliance validation result
/// </summary>
public class SlaComplianceResult
{
    /// <summary>
    /// Whether SLA requirements are met
    /// </summary>
    public bool IsCompliant { get; set; }

    /// <summary>
    /// SLA violations (if any)
    /// </summary>
    public List<SlaViolation> Violations { get; set; } = new();

    /// <summary>
    /// Compliance percentage
    /// </summary>
    public decimal CompliancePercentage { get; set; }

    /// <summary>
    /// Recommended actions for compliance
    /// </summary>
    public List<string> RecommendedActions { get; set; } = new();
}

/// <summary>
/// SLA violation details
/// </summary>
public class SlaViolation
{
    /// <summary>
    /// Type of violation (e.g., "ResponseTime", "Uptime", "Throughput")
    /// </summary>
    public string ViolationType { get; set; } = string.Empty;

    /// <summary>
    /// Expected value per SLA
    /// </summary>
    public string ExpectedValue { get; set; } = string.Empty;

    /// <summary>
    /// Actual measured value
    /// </summary>
    public string ActualValue { get; set; } = string.Empty;

    /// <summary>
    /// Severity level (1-10, 10 being critical)
    /// </summary>
    public int SeverityLevel { get; set; }
}

/// <summary>
/// SLA requirements specification
/// </summary>
public class SlaRequirements
{
    /// <summary>
    /// Maximum allowed response time
    /// </summary>
    public TimeSpan MaxResponseTime { get; set; }

    /// <summary>
    /// Minimum required uptime percentage
    /// </summary>
    public decimal MinUptimePercentage { get; set; }

    /// <summary>
    /// Minimum required throughput per second
    /// </summary>
    public int MinThroughputPerSecond { get; set; }

    /// <summary>
    /// Maximum allowed error rate percentage
    /// </summary>
    public decimal MaxErrorRatePercentage { get; set; }
}