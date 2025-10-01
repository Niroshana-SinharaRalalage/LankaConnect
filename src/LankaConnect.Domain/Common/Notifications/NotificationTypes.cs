using System;
using System.Collections.Generic;

namespace LankaConnect.Domain.Common.Notifications;

/// <summary>
/// Stakeholder notification plan for incident management
/// Foundational type for comprehensive incident communication
/// </summary>
public sealed record StakeholderNotificationPlan
{
    public string PlanId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public IncidentSeverity Severity { get; init; }
    public IReadOnlyList<NotificationTarget> Targets { get; init; } = Array.Empty<NotificationTarget>();
    public NotificationTiming Timing { get; init; }
    public IReadOnlyList<DatabaseNotificationChannel> Channels { get; init; } = Array.Empty<DatabaseNotificationChannel>();
    public string Template { get; init; } = string.Empty;
    public bool RequiresApproval { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public string CreatedBy { get; init; } = string.Empty;
    public Dictionary<string, object> CustomParameters { get; init; } = new();
    
    public static StakeholderNotificationPlan Create(string name, IncidentSeverity severity, 
        IEnumerable<NotificationTarget> targets, NotificationTiming timing)
    {
        return new StakeholderNotificationPlan
        {
            PlanId = Guid.NewGuid().ToString(),
            Name = name,
            Severity = severity,
            Targets = targets.ToList().AsReadOnly(),
            Timing = timing,
            RequiresApproval = severity >= IncidentSeverity.High,
            Template = DetermineTemplate(severity)
        };
    }
    
    private static string DetermineTemplate(IncidentSeverity severity)
    {
        return severity switch
        {
            IncidentSeverity.Critical => "critical-incident-template",
            IncidentSeverity.High => "high-priority-template",
            IncidentSeverity.Medium => "standard-incident-template",
            IncidentSeverity.Low => "low-priority-template",
            _ => "default-template"
        };
    }
}

/// <summary>
/// Result of notification plan execution
/// </summary>
public sealed record NotificationResult
{
    public string PlanId { get; init; } = string.Empty;
    public DateTime ExecutedAt { get; init; } = DateTime.UtcNow;
    public IReadOnlyList<NotificationDeliveryResult> DeliveryResults { get; init; } = Array.Empty<NotificationDeliveryResult>();
    public string? ErrorMessage { get; init; }
    public TimeSpan ExecutionDuration { get; init; }
    public int TotalNotifications { get; init; }
    public int SuccessfulNotifications { get; init; }
    public int FailedNotifications { get; init; }
    public double SuccessRate { get; init; }
    
    public static NotificationResult Success(string planId, IEnumerable<NotificationDeliveryResult> results, TimeSpan duration)
    {
        var deliveryResults = results.ToList();
        var successful = deliveryResults.Count(r => r.Success);
        var total = deliveryResults.Count;
        
        return new NotificationResult
        {
            PlanId = planId,
            DeliveryResults = deliveryResults.AsReadOnly(),
            ExecutionDuration = duration,
            TotalNotifications = total,
            SuccessfulNotifications = successful,
            FailedNotifications = total - successful,
            SuccessRate = total > 0 ? (double)successful / total * 100 : 0
        };
    }
    
    public static NotificationResult Failure(string planId, string error, TimeSpan duration)
    {
        return new NotificationResult
        {
            PlanId = planId,
            ErrorMessage = error,
            ExecutionDuration = duration
        };
    }
}

/// <summary>
/// Target for notifications
/// </summary>
public sealed record NotificationTarget
{
    public string TargetId { get; init; } = string.Empty;
    public NotificationTargetType Type { get; init; }
    public string Address { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public StakeholderRole Role { get; init; }
    public bool IsActive { get; init; } = true;
    public IReadOnlyList<DatabaseNotificationChannel> PreferredChannels { get; init; } = Array.Empty<DatabaseNotificationChannel>();
    public Dictionary<string, string> ContactDetails { get; init; } = new();
}

/// <summary>
/// Notification delivery result
/// </summary>
public sealed record NotificationDeliveryResult
{
    public string TargetId { get; init; } = string.Empty;
    public DatabaseNotificationChannel Channel { get; init; }
    public bool Success { get; init; }
    public DateTime DeliveredAt { get; init; } = DateTime.UtcNow;
    public string? ErrorMessage { get; init; }
    public TimeSpan DeliveryTime { get; init; }
    public string? MessageId { get; init; }
    public DeliveryStatus Status { get; init; }
}

/// <summary>
/// Incident documentation requirements
/// </summary>
public sealed record DocumentationRequirements
{
    public string RequirementsId { get; init; } = string.Empty;
    public IncidentSeverity Severity { get; init; }
    public IReadOnlyList<DocumentationType> RequiredDocuments { get; init; } = Array.Empty<DocumentationType>();
    public TimeSpan DocumentationDeadline { get; init; }
    public IReadOnlyList<string> RequiredApprovers { get; init; } = Array.Empty<string>();
    public bool RequiresRootCauseAnalysis { get; init; }
    public bool RequiresPostMortem { get; init; }
    public ComplianceLevel ComplianceLevel { get; init; }
    
    public static DocumentationRequirements Create(IncidentSeverity severity)
    {
        var requirements = DetermineRequirements(severity);
        return new DocumentationRequirements
        {
            RequirementsId = Guid.NewGuid().ToString(),
            Severity = severity,
            RequiredDocuments = requirements.Documents,
            DocumentationDeadline = requirements.Deadline,
            RequiresRootCauseAnalysis = severity >= IncidentSeverity.Medium,
            RequiresPostMortem = severity >= IncidentSeverity.High,
            ComplianceLevel = requirements.ComplianceLevel
        };
    }
    
    private static (IReadOnlyList<DocumentationType> Documents, TimeSpan Deadline, ComplianceLevel ComplianceLevel) DetermineRequirements(IncidentSeverity severity)
    {
        return severity switch
        {
            IncidentSeverity.Critical => (new[] { DocumentationType.IncidentReport, DocumentationType.ImpactAssessment, 
                DocumentationType.TimelineAnalysis, DocumentationType.RootCauseAnalysis, DocumentationType.PostMortem }.ToList().AsReadOnly(),
                TimeSpan.FromHours(4), ComplianceLevel.Regulatory),
            IncidentSeverity.High => (new[] { DocumentationType.IncidentReport, DocumentationType.ImpactAssessment, 
                DocumentationType.RootCauseAnalysis }.ToList().AsReadOnly(),
                TimeSpan.FromHours(8), ComplianceLevel.Corporate),
            IncidentSeverity.Medium => (new[] { DocumentationType.IncidentReport, DocumentationType.ImpactAssessment }.ToList().AsReadOnly(),
                TimeSpan.FromHours(24), ComplianceLevel.Standard),
            _ => (new[] { DocumentationType.IncidentReport }.ToList().AsReadOnly(),
                TimeSpan.FromDays(1), ComplianceLevel.Basic)
        };
    }
}

/// <summary>
/// Result of incident documentation
/// </summary>
public sealed record IncidentDocumentationResult
{
    public string RequirementsId { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public IReadOnlyList<DocumentationItem> CompletedDocuments { get; init; } = Array.Empty<DocumentationItem>();
    public string? ErrorMessage { get; init; }
    public double CompletionPercentage { get; init; }
    public bool MeetsComplianceRequirements { get; init; }
    public IReadOnlyList<string> MissingDocuments { get; init; } = Array.Empty<string>();
    
    public static IncidentDocumentationResult Success(string requirementsId, IEnumerable<DocumentationItem> documents)
    {
        var completedDocs = documents.ToList();
        return new IncidentDocumentationResult
        {
            RequirementsId = requirementsId,
            CompletedDocuments = completedDocs.AsReadOnly(),
            CompletionPercentage = 100.0,
            MeetsComplianceRequirements = true
        };
    }
}

/// <summary>
/// Documentation item
/// </summary>
public sealed record DocumentationItem
{
    public string DocumentId { get; init; } = string.Empty;
    public DocumentationType Type { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string CreatedBy { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public IReadOnlyList<string> Approvers { get; init; } = Array.Empty<string>();
    public bool IsApproved { get; init; }
}

// Enumerations
public enum IncidentSeverity { Low = 1, Medium = 2, High = 3, Critical = 4 }
public enum NotificationTargetType { Individual, Group, Role, System }
public enum StakeholderRole { TechnicalLead, Manager, Executive, Compliance, External }
public enum DatabaseNotificationChannel { Email, SMS, Slack, Teams, Phone, PagerDuty }
public enum NotificationTiming { Immediate, Delayed, Scheduled }
public enum DeliveryStatus { Pending, Sent, Delivered, Failed, Bounced }
public enum DocumentationType { IncidentReport, ImpactAssessment, TimelineAnalysis, RootCauseAnalysis, PostMortem }
public enum ComplianceLevel { Basic, Standard, Corporate, Regulatory }