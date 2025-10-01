using System;
using System.Collections.Generic;
using System.Linq;
using LankaConnect.Domain.Common.Enums;

namespace LankaConnect.Domain.Common.Monitoring;

/// <summary>
/// Comprehensive alerting and notification system types for LankaConnect's
/// cultural intelligence platform with Fortune 500 enterprise capabilities.
/// </summary>

#region Core Alert Processing

/// <summary>
/// Result of alert processing operations.
/// </summary>
public record AlertProcessingResult(
    string AlertId,
    bool IsProcessed,
    TimeSpan ProcessingDuration,
    string? CulturalContext = null,
    DateTime ProcessedAt = default,
    List<string>? ProcessingSteps = null)
{
    /// <summary>
    /// Gets whether the alert has cultural significance.
    /// </summary>
    public bool HasCulturalSignificance => !string.IsNullOrEmpty(CulturalContext);

    /// <summary>
    /// Gets the processing rate (alerts per second).
    /// </summary>
    public double ProcessingRate => ProcessingDuration.TotalSeconds > 0 ? 1 / ProcessingDuration.TotalSeconds : 0;

    /// <summary>
    /// Creates a successful alert processing result.
    /// </summary>
    public static AlertProcessingResult Create(string alertId, bool isProcessed, 
        TimeSpan processingDuration, string? culturalContext = null)
    {
        return new AlertProcessingResult(alertId, isProcessed, processingDuration, 
            culturalContext, DateTime.UtcNow, new List<string>());
    }

    /// <summary>
    /// Creates a failed alert processing result.
    /// </summary>
    public static AlertProcessingResult CreateFailed(string alertId, string errorMessage)
    {
        return new AlertProcessingResult(alertId, false, TimeSpan.Zero, errorMessage, DateTime.UtcNow);
    }
}

/// <summary>
/// Context for alert processing with cultural intelligence awareness.
/// </summary>
public class AlertProcessingContext
{
    /// <summary>
    /// Gets the context identifier.
    /// </summary>
    public string ContextId { get; private set; }

    /// <summary>
    /// Gets the cultural event type.
    /// </summary>
    public CulturalEventType EventType { get; private set; }

    /// <summary>
    /// Gets the alert priority.
    /// </summary>
    public AlertPriority Priority { get; private set; }

    /// <summary>
    /// Gets the affected regions.
    /// </summary>
    public List<string> AffectedRegions { get; private set; }

    /// <summary>
    /// Gets additional context data.
    /// </summary>
    public Dictionary<string, object> ContextData { get; private set; }

    /// <summary>
    /// Gets whether this context is culturally significant.
    /// </summary>
    public bool IsCulturallySignificant => EventType != CulturalEventType.None && 
                                          Priority == AlertPriority.CulturalEvent;

    /// <summary>
    /// Gets whether special handling is required.
    /// </summary>
    public bool RequiresSpecialHandling => IsCulturallySignificant || Priority == AlertPriority.Critical;

    private AlertProcessingContext(string contextId, CulturalEventType eventType, AlertPriority priority, List<string> affectedRegions)
    {
        ContextId = contextId;
        EventType = eventType;
        Priority = priority;
        AffectedRegions = affectedRegions;
        ContextData = new Dictionary<string, object>();
    }

    /// <summary>
    /// Creates a cultural event alert processing context.
    /// </summary>
    public static AlertProcessingContext CreateCultural(string contextId, CulturalEventType eventType, 
        AlertPriority priority, List<string> affectedRegions)
    {
        return new AlertProcessingContext(contextId, eventType, priority, affectedRegions);
    }

    /// <summary>
    /// Creates a standard alert processing context.
    /// </summary>
    public static AlertProcessingContext CreateStandard(string contextId, AlertPriority priority)
    {
        return new AlertProcessingContext(contextId, CulturalEventType.None, priority, new List<string>());
    }
}

/// <summary>
/// Result of alert escalation operations.
/// </summary>
public record EscalationResult(
    string EscalationId,
    List<string> EscalationPath,
    string FinalEscalationLevel,
    TimeSpan TotalEscalationTime,
    DateTime EscalatedAt = default)
{
    /// <summary>
    /// Gets the number of escalation levels.
    /// </summary>
    public int EscalationCount => EscalationPath.Count;

    /// <summary>
    /// Gets whether management intervention was required.
    /// </summary>
    public bool RequiredManagementIntervention => FinalEscalationLevel.Contains("Management", StringComparison.OrdinalIgnoreCase) ||
                                                 FinalEscalationLevel.Contains("Executive", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Gets whether escalation was resolved quickly.
    /// </summary>
    public bool IsQuickResolution => TotalEscalationTime <= TimeSpan.FromMinutes(15);

    /// <summary>
    /// Creates an escalation result.
    /// </summary>
    public static EscalationResult Create(string escalationId, List<string> escalationPath, 
        string finalLevel, TimeSpan totalTime)
    {
        return new EscalationResult(escalationId, escalationPath, finalLevel, totalTime, DateTime.UtcNow);
    }
}

/// <summary>
/// Specialized escalation result for cultural events.
/// </summary>
public record AlertEscalationResult(
    string AlertId,
    string? CulturalEventId,
    string EscalationReason,
    List<string> NotifiedStakeholders,
    DateTime EscalatedAt = default) : EscalationResult(
        $"escalation-{AlertId}", 
        new List<string> { "L1-Cultural", "L2-Engineering", "L3-Management" },
        "L3-Management",
        TimeSpan.FromMinutes(30),
        EscalatedAt)
{
    /// <summary>
    /// Gets whether this is a cultural event escalation.
    /// </summary>
    public bool IsCulturalEvent => !string.IsNullOrEmpty(CulturalEventId);

    /// <summary>
    /// Gets whether cultural expertise is required.
    /// </summary>
    public bool RequiresCulturalExpertise => IsCulturalEvent && 
        NotifiedStakeholders.Any(s => s.Contains("Cultural", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Creates a cultural event escalation result.
    /// </summary>
    public static AlertEscalationResult CreateCultural(string alertId, string culturalEventId, 
        string escalationReason, List<string> stakeholders)
    {
        return new AlertEscalationResult(alertId, culturalEventId, escalationReason, stakeholders, DateTime.UtcNow);
    }
}

#endregion

#region Notification and Acknowledgment

/// <summary>
/// User notification preferences with cultural settings.
/// </summary>
public class NotificationPreferences
{
    /// <summary>
    /// Gets the user identifier.
    /// </summary>
    public string UserId { get; private set; }

    /// <summary>
    /// Gets the notification channel settings.
    /// </summary>
    public Dictionary<NotificationChannel, bool> ChannelSettings { get; private set; }

    /// <summary>
    /// Gets the cultural notification settings.
    /// </summary>
    public CulturalNotificationSettings? CulturalSettings { get; private set; }

    /// <summary>
    /// Gets whether cultural notifications are supported.
    /// </summary>
    public bool SupportsCulturalNotifications => CulturalSettings?.EnableCulturalNotifications == true;

    private NotificationPreferences(string userId, Dictionary<NotificationChannel, bool> channelSettings, 
        CulturalNotificationSettings? culturalSettings)
    {
        UserId = userId;
        ChannelSettings = channelSettings;
        CulturalSettings = culturalSettings;
    }

    /// <summary>
    /// Creates notification preferences.
    /// </summary>
    public static NotificationPreferences Create(string userId, Dictionary<NotificationChannel, bool> channels, 
        CulturalNotificationSettings? culturalSettings = null)
    {
        return new NotificationPreferences(userId, channels, culturalSettings);
    }

    /// <summary>
    /// Checks if a notification channel is enabled.
    /// </summary>
    public bool IsChannelEnabled(NotificationChannel channel)
    {
        return ChannelSettings.TryGetValue(channel, out var enabled) && enabled;
    }
}

/// <summary>
/// Cultural notification settings.
/// </summary>
public record CulturalNotificationSettings(
    string Language,
    string CulturalBackground,
    bool EnableCulturalNotifications,
    List<string>? CulturalEventSubscriptions = null);

/// <summary>
/// Alert acknowledgment tracking.
/// </summary>
public record AlertAcknowledgment(
    string AlertId,
    string AcknowledgedBy,
    string AcknowledgmentMessage,
    TimeSpan EstimatedResolutionTime,
    DateTime AcknowledgedAt = default)
{
    /// <summary>
    /// Gets whether the acknowledgment is valid.
    /// </summary>
    public bool IsValid => !string.IsNullOrEmpty(AlertId) && !string.IsNullOrEmpty(AcknowledgedBy) &&
                          EstimatedResolutionTime > TimeSpan.Zero;

    /// <summary>
    /// Gets whether resolution is overdue.
    /// </summary>
    public bool IsOverdue => DateTime.UtcNow > AcknowledgedAt.Add(EstimatedResolutionTime);

    /// <summary>
    /// Creates an alert acknowledgment.
    /// </summary>
    public static AlertAcknowledgment Create(string alertId, string acknowledgedBy, 
        string message, TimeSpan estimatedResolution)
    {
        return new AlertAcknowledgment(alertId, acknowledgedBy, message, estimatedResolution, DateTime.UtcNow);
    }
}

/// <summary>
/// Alert resolution tracking.
/// </summary>
public class AlertResolutionResult
{
    /// <summary>
    /// Gets the alert identifier.
    /// </summary>
    public string AlertId { get; private set; }

    /// <summary>
    /// Gets who resolved the alert.
    /// </summary>
    public string ResolvedBy { get; private set; }

    /// <summary>
    /// Gets the resolution summary.
    /// </summary>
    public string ResolutionSummary { get; private set; }

    /// <summary>
    /// Gets the actions taken to resolve the alert.
    /// </summary>
    public List<string> ActionsTaken { get; private set; }

    /// <summary>
    /// Gets the preventive measures implemented.
    /// </summary>
    public List<string> PreventiveMeasures { get; private set; }

    /// <summary>
    /// Gets when the alert was resolved.
    /// </summary>
    public DateTime ResolvedAt { get; private set; }

    /// <summary>
    /// Gets the resolution duration.
    /// </summary>
    public TimeSpan ResolutionDuration { get; private set; }

    /// <summary>
    /// Gets whether preventive measures were implemented.
    /// </summary>
    public bool HasPreventiveMeasures => PreventiveMeasures.Any();

    private AlertResolutionResult(string alertId, string resolvedBy, string summary, 
        List<string> actions, List<string> preventive)
    {
        AlertId = alertId;
        ResolvedBy = resolvedBy;
        ResolutionSummary = summary;
        ActionsTaken = actions;
        PreventiveMeasures = preventive;
        ResolvedAt = DateTime.UtcNow;
        ResolutionDuration = TimeSpan.FromMinutes(new Random().Next(5, 60)); // Simulated duration
    }

    /// <summary>
    /// Creates an alert resolution result.
    /// </summary>
    public static AlertResolutionResult Create(string alertId, string resolvedBy, 
        string summary, List<string> actions, List<string> preventive)
    {
        return new AlertResolutionResult(alertId, resolvedBy, summary, actions, preventive);
    }
}

#endregion

#region Enumerations

/// <summary>
/// Cultural event types for alert processing.
/// </summary>
// CulturalEventType is now imported from Domain.Common.Enums

/// <summary>
/// Alert priority levels with cultural awareness.
/// </summary>
public enum AlertPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3,
    CulturalEvent = 4
}

/// <summary>
/// Notification channel types.
/// </summary>
public enum NotificationChannel
{
    Email = 0,
    SMS = 1,
    Teams = 2,
    Slack = 3,
    PushNotification = 4,
    WebhookCallback = 5
}

/// <summary>
/// Health issue severity levels.
/// </summary>
public enum HealthIssueSeverity
{
    Info = 0,
    Warning = 1,
    Minor = 2,
    Major = 3,
    Critical = 4
}

/// <summary>
/// Recommendation priority levels.
/// </summary>
public enum RecommendationPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Urgent = 3
}

/// <summary>
/// Trend direction indicators.
/// </summary>
public enum TrendDirection
{
    Declining = -1,
    Stable = 0,
    Improving = 1
}

/// <summary>
/// Analytics widget types.
/// </summary>
public enum AnalyticsWidgetType
{
    LineChart = 0,
    BarChart = 1,
    PieChart = 2,
    MetricCard = 3,
    CulturalEngagementChart = 4,
    DiasporaDistributionMap = 5,
    LanguageUsageChart = 6
}

/// <summary>
/// Performance insight categories.
/// </summary>
public enum InsightCategory
{
    Performance = 0,
    Usage = 1,
    CulturalEngagement = 2,
    UserBehavior = 3,
    SystemHealth = 4,
    BusinessImpact = 5
}

/// <summary>
/// Recommendation categories.
/// </summary>
public enum RecommendationCategory
{
    Performance = 0,
    Security = 1,
    Scalability = 2,
    UserExperience = 3,
    CulturalOptimization = 4,
    CostOptimization = 5
}

/// <summary>
/// Action priority levels.
/// </summary>
public enum ActionPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

/// <summary>
/// Compliance violation types with cultural intelligence support.
/// </summary>
public enum ComplianceViolationType
{
    AvailabilityBreach = 0,
    PerformanceDegradation = 1,
    SecurityViolation = 2,
    DataIntegrityIssue = 3,
    CulturalSensitivityBreach = 4,
    GDPR = 5,
    HIPAA = 6,
    SOC2 = 7,
    PCI_DSS = 8,
    ISO27001 = 9,
    CulturalDataProtection = 10,
    MultiJurisdictional = 11,
    SacredContentViolation = 12,
    CrossBorderDataTransfer = 13
}

/// <summary>
/// Violation severity levels.
/// </summary>
public enum ViolationSeverity
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

/// <summary>
/// Revenue risk types.
/// </summary>
public enum RevenueRiskType
{
    ServiceDowntime = 0,
    PerformanceDegradation = 1,
    CulturalEventCapacity = 2,
    SecurityBreach = 3,
    DataLoss = 4
}

/// <summary>
/// Risk severity levels.
/// </summary>
public enum RiskSeverity
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

/// <summary>
/// Revenue protection types.
/// </summary>
public enum RevenueProtectionType
{
    AutoScaling = 0,
    LoadBalancing = 1,
    CulturalEventLoadBalancing = 2,
    FailoverSystems = 3,
    BackupSystems = 4
}

#endregion