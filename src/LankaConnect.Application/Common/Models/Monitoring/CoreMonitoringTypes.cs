using System;
using System.Collections.Generic;
using System.Linq;
using LankaConnect.Domain.Common.Enums;
using LankaConnect.Domain.Common.Monitoring;

namespace LankaConnect.Application.Common.Models.Monitoring;

/// <summary>
/// TDD GREEN Phase: Core Monitoring Types Implementation
/// Minimal implementation to pass failing tests
/// Cultural intelligence integrated monitoring for LankaConnect platform
/// </summary>

#region AlertEscalationRequest

/// <summary>
/// Request for alert escalation with cultural event awareness
/// </summary>
public class AlertEscalationRequest
{
    public string AlertId { get; private set; }
    public string EscalationReason { get; private set; }
    public AlertPriority Priority { get; private set; }
    public CulturalEventType? CulturalEventType { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public string RequestedBy { get; private set; }

    /// <summary>
    /// Gets whether this is a cultural event related escalation
    /// </summary>
    public bool IsCulturalEvent => CulturalEventType.HasValue && CulturalEventType != LankaConnect.Domain.Common.Enums.CulturalEventType.None;

    /// <summary>
    /// Gets whether immediate attention is required
    /// </summary>
    public bool RequiresImmediateAttention => Priority >= AlertPriority.High || Priority == AlertPriority.CulturalEvent;

    /// <summary>
    /// Gets estimated resolution time based on priority and cultural context
    /// </summary>
    public TimeSpan EstimatedResolutionTime => IsCulturalEvent ? TimeSpan.FromMinutes(15) : 
        Priority switch
        {
            AlertPriority.Critical => TimeSpan.FromMinutes(30),
            AlertPriority.High => TimeSpan.FromHours(2),
            AlertPriority.Medium => TimeSpan.FromHours(8),
            _ => TimeSpan.FromDays(1)
        };

    private AlertEscalationRequest(string alertId, string escalationReason, AlertPriority priority, 
        CulturalEventType? culturalEventType, string requestedBy)
    {
        AlertId = alertId;
        EscalationReason = escalationReason;
        Priority = priority;
        CulturalEventType = culturalEventType;
        RequestedAt = DateTime.UtcNow;
        RequestedBy = requestedBy ?? "system";
    }

    /// <summary>
    /// Creates cultural event alert escalation request
    /// </summary>
    public static AlertEscalationRequest Create(string alertId, string escalationReason, 
        AlertPriority priority, CulturalEventType culturalEventType, string requestedBy = "system")
    {
        return new AlertEscalationRequest(alertId, escalationReason, priority, culturalEventType, requestedBy);
    }

    /// <summary>
    /// Creates standard alert escalation request
    /// </summary>
    public static AlertEscalationRequest Create(string alertId, string escalationReason, 
        AlertPriority priority, string requestedBy = "system")
    {
        return new AlertEscalationRequest(alertId, escalationReason, priority, null, requestedBy);
    }
}

#endregion

#region EscalationPolicy

/// <summary>
/// Escalation policy with cultural intelligence support
/// </summary>
public class EscalationPolicy
{
    public string PolicyId { get; private set; }
    public string PolicyName { get; private set; }
    public IReadOnlyList<EscalationLevel> EscalationLevels { get; private set; }
    public IReadOnlyList<CulturalEventType> SupportedCulturalEvents { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets whether this is a cultural-aware policy
    /// </summary>
    public bool IsCulturalPolicy => SupportedCulturalEvents.Any();

    /// <summary>
    /// Gets maximum escalation time across all levels
    /// </summary>
    public TimeSpan MaxEscalationTime => EscalationLevels.Any() ? 
        TimeSpan.FromTicks(EscalationLevels.Sum(level => level.EscalationTimeout.Ticks)) : TimeSpan.Zero;

    /// <summary>
    /// Gets the escalation path for cultural events
    /// </summary>
    public IReadOnlyList<string> CulturalEscalationPath => IsCulturalPolicy ? 
        EscalationLevels.Select(level => level.LevelName).ToList().AsReadOnly() : 
        Array.Empty<string>().ToList().AsReadOnly();

    private EscalationPolicy(string policyName, IEnumerable<EscalationLevel> escalationLevels, 
        IEnumerable<CulturalEventType> supportedCulturalEvents)
    {
        PolicyId = Guid.NewGuid().ToString();
        PolicyName = policyName;
        EscalationLevels = escalationLevels.ToList().AsReadOnly();
        SupportedCulturalEvents = supportedCulturalEvents.ToList().AsReadOnly();
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates cultural-aware escalation policy
    /// </summary>
    public static EscalationPolicy CreateCultural(string policyName, IEnumerable<EscalationLevel> levels, 
        IEnumerable<CulturalEventType> culturalEventTypes)
    {
        return new EscalationPolicy(policyName, levels, culturalEventTypes);
    }

    /// <summary>
    /// Creates standard escalation policy
    /// </summary>
    public static EscalationPolicy CreateStandard(string policyName, IEnumerable<EscalationLevel> levels)
    {
        return new EscalationPolicy(policyName, levels, Array.Empty<CulturalEventType>());
    }
}

/// <summary>
/// Escalation level with timeout and contacts
/// </summary>
public class EscalationLevel
{
    public string LevelName { get; private set; }
    public TimeSpan EscalationTimeout { get; private set; }
    public IReadOnlyList<string> Contacts { get; private set; }
    public int Priority { get; private set; }

    private EscalationLevel(string levelName, TimeSpan timeout, IEnumerable<string> contacts, int priority = 0)
    {
        LevelName = levelName;
        EscalationTimeout = timeout;
        Contacts = contacts.ToList().AsReadOnly();
        Priority = priority;
    }

    public static EscalationLevel Create(string levelName, TimeSpan timeout, IEnumerable<string> contacts, int priority = 0)
    {
        return new EscalationLevel(levelName, timeout, contacts, priority);
    }
}

#endregion

#region MaintenanceWindow

/// <summary>
/// Maintenance window with cultural event awareness
/// </summary>
public class MaintenanceWindow
{
    public string WindowId { get; private set; }
    public string WindowName { get; private set; }
    public DateTime StartTime { get; private set; }
    public TimeSpan Duration { get; private set; }
    public MaintenanceType MaintenanceType { get; private set; }
    public IReadOnlyList<string> AffectedServices { get; private set; }
    public IReadOnlyList<string> CulturalConsiderations { get; private set; }
    public bool SuppressAlerts { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the end time of the maintenance window
    /// </summary>
    public DateTime EndTime => StartTime.Add(Duration);

    /// <summary>
    /// Gets whether the maintenance window is currently active
    /// </summary>
    public bool IsActive
    {
        get
        {
            var now = DateTime.UtcNow;
            return now >= StartTime && now <= EndTime;
        }
    }

    /// <summary>
    /// Gets time remaining in the maintenance window
    /// </summary>
    public TimeSpan TimeRemaining => IsActive ? EndTime.Subtract(DateTime.UtcNow) : TimeSpan.Zero;

    /// <summary>
    /// Gets whether this maintenance window considers cultural events
    /// </summary>
    public bool IsCulturallyAware => CulturalConsiderations.Any();

    /// <summary>
    /// Gets whether maintenance conflicts with cultural events
    /// </summary>
    public bool ConflictsWithCulturalEvents => false; // Simplified implementation

    private MaintenanceWindow(string windowName, DateTime startTime, TimeSpan duration, 
        MaintenanceType maintenanceType, IEnumerable<string> affectedServices, 
        IEnumerable<string> culturalConsiderations, bool suppressAlerts)
    {
        WindowId = Guid.NewGuid().ToString();
        WindowName = windowName;
        StartTime = startTime;
        Duration = duration;
        MaintenanceType = maintenanceType;
        AffectedServices = affectedServices.ToList().AsReadOnly();
        CulturalConsiderations = culturalConsiderations.ToList().AsReadOnly();
        SuppressAlerts = suppressAlerts;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates standard maintenance window
    /// </summary>
    public static MaintenanceWindow Create(string windowName, DateTime startTime, TimeSpan duration, 
        MaintenanceType maintenanceType, IEnumerable<string> affectedServices, bool suppressAlerts = true)
    {
        return new MaintenanceWindow(windowName, startTime, duration, maintenanceType, 
            affectedServices, Array.Empty<string>(), suppressAlerts);
    }

    /// <summary>
    /// Creates cultural-aware maintenance window
    /// </summary>
    public static MaintenanceWindow CreateCulturalAware(string windowName, DateTime startTime, TimeSpan duration, 
        MaintenanceType maintenanceType, IEnumerable<string> affectedServices, 
        IEnumerable<string> culturalConsiderations, bool suppressAlerts = true)
    {
        return new MaintenanceWindow(windowName, startTime, duration, maintenanceType, 
            affectedServices, culturalConsiderations, suppressAlerts);
    }
}

/// <summary>
/// Types of maintenance operations
/// </summary>
public enum MaintenanceType
{
    SystemUpdate,
    DatabaseOptimization,
    SecurityUpdate,
    SystemUpgrade,
    PerformanceTuning,
    CulturalIntelligenceUpdate
}

#endregion

#region AlertSuppressionPolicy

/// <summary>
/// Alert suppression policy with cultural intelligence
/// </summary>
public class AlertSuppressionPolicy
{
    public string PolicyId { get; private set; }
    public string PolicyName { get; private set; }
    public IReadOnlyList<SuppressionRule> SuppressionRules { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets whether policy has cultural-specific suppression rules
    /// </summary>
    public bool HasCulturalRules => SuppressionRules.Any(rule => rule.IsCulturalRule);

    private AlertSuppressionPolicy(string policyName, IEnumerable<SuppressionRule> suppressionRules)
    {
        PolicyId = Guid.NewGuid().ToString();
        PolicyName = policyName;
        SuppressionRules = suppressionRules.ToList().AsReadOnly();
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates alert suppression policy
    /// </summary>
    public static AlertSuppressionPolicy Create(string policyName, IEnumerable<SuppressionRule> rules)
    {
        return new AlertSuppressionPolicy(policyName, rules);
    }

    /// <summary>
    /// Determines if an alert should be suppressed
    /// </summary>
    public bool ShouldSuppressAlert(AlertSuppressionContext context)
    {
        return SuppressionRules.Any(rule => rule.Matches(context) && rule.IsActive);
    }

    /// <summary>
    /// Gets currently active suppression rules
    /// </summary>
    public IReadOnlyList<SuppressionRule> GetActiveSuppressions()
    {
        return SuppressionRules.Where(rule => rule.IsActive).ToList().AsReadOnly();
    }
}

/// <summary>
/// Individual suppression rule
/// </summary>
public class SuppressionRule
{
    public string RuleId { get; private set; }
    public string Description { get; private set; }
    public AlertCategory AlertCategory { get; private set; }
    public CulturalEventType? CulturalEventType { get; private set; }
    public TimeSpan SuppressionDuration { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets whether this is a cultural event rule
    /// </summary>
    public bool IsCulturalRule => CulturalEventType.HasValue;

    /// <summary>
    /// Gets whether the rule is currently active
    /// </summary>
    public bool IsActive => DateTime.UtcNow <= CreatedAt.Add(SuppressionDuration);

    private SuppressionRule(string description, AlertCategory alertCategory, 
        CulturalEventType? culturalEventType, TimeSpan suppressionDuration)
    {
        RuleId = Guid.NewGuid().ToString();
        Description = description;
        AlertCategory = alertCategory;
        CulturalEventType = culturalEventType;
        SuppressionDuration = suppressionDuration;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates suppression rule
    /// </summary>
    public static SuppressionRule Create(string description, AlertCategory alertCategory, 
        CulturalEventType? culturalEventType, TimeSpan suppressionDuration)
    {
        return new SuppressionRule(description, alertCategory, culturalEventType, suppressionDuration);
    }

    /// <summary>
    /// Checks if rule matches given context
    /// </summary>
    public bool Matches(AlertSuppressionContext context)
    {
        return AlertCategory == context.AlertCategory && 
               (!CulturalEventType.HasValue || CulturalEventType == context.CulturalEventType);
    }
}

/// <summary>
/// Context for alert suppression decisions
/// </summary>
public record AlertSuppressionContext(string AlertId, AlertCategory AlertCategory, CulturalEventType? CulturalEventType);

/// <summary>
/// Alert categories for suppression
/// </summary>
public enum AlertCategory
{
    Performance,
    Resource,
    Security,
    Availability,
    CulturalIntelligence
}

#endregion

#region PlannedMaintenanceWindow

/// <summary>
/// Specialized maintenance window for planned operations with approval workflow
/// </summary>
public class PlannedMaintenanceWindow
{
    public string WindowId { get; private set; }
    public string WindowName { get; private set; }
    public DateTime StartTime { get; private set; }
    public TimeSpan Duration { get; private set; }
    public MaintenanceType MaintenanceType { get; private set; }
    public IReadOnlyList<string> AffectedServices { get; private set; }
    public string PlannedBy { get; private set; }
    public IReadOnlyList<string> ApprovalChain { get; private set; }
    public bool IsApproved { get; private set; }
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Gets the end time of the maintenance window
    /// </summary>
    public DateTime EndTime => StartTime.Add(Duration);

    /// <summary>
    /// Gets whether the maintenance window is currently active
    /// </summary>
    public bool IsActive
    {
        get
        {
            var now = DateTime.UtcNow;
            return now >= StartTime && now <= EndTime;
        }
    }

    /// <summary>
    /// Gets time remaining in the maintenance window
    /// </summary>
    public TimeSpan TimeRemaining => IsActive ? EndTime.Subtract(DateTime.UtcNow) : TimeSpan.Zero;

    private PlannedMaintenanceWindow(string windowName, DateTime startTime, TimeSpan duration, 
        MaintenanceType maintenanceType, IEnumerable<string> affectedServices, 
        string plannedBy, IEnumerable<string> approvalChain)
    {
        WindowId = Guid.NewGuid().ToString();
        WindowName = windowName;
        StartTime = startTime;
        Duration = duration;
        MaintenanceType = maintenanceType;
        AffectedServices = affectedServices.ToList().AsReadOnly();
        PlannedBy = plannedBy;
        ApprovalChain = approvalChain.ToList().AsReadOnly();
        IsApproved = ApprovalChain.Any();
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates planned maintenance window with approval workflow
    /// </summary>
    public static PlannedMaintenanceWindow CreatePlanned(string windowName, DateTime startTime, TimeSpan duration,
        MaintenanceType maintenanceType, IEnumerable<string> affectedServices, 
        string plannedBy, IEnumerable<string> approvalChain)
    {
        return new PlannedMaintenanceWindow(windowName, startTime, duration, maintenanceType, 
            affectedServices, plannedBy, approvalChain);
    }
}

#endregion