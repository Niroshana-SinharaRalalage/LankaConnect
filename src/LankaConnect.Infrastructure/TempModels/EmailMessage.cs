using System;
using System.Collections.Generic;

namespace LankaConnect.Infrastructure.TempModels;

public partial class EmailMessage
{
    public Guid Id { get; set; }

    public string FromEmail { get; set; } = null!;

    public string Subject { get; set; } = null!;

    public string TextContent { get; set; } = null!;

    public string? HtmlContent { get; set; }

    public string Type { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime? SentAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime? OpenedAt { get; set; }

    public DateTime? ClickedAt { get; set; }

    public DateTime? FailedAt { get; set; }

    public DateTime? NextRetryAt { get; set; }

    public string? ErrorMessage { get; set; }

    public int RetryCount { get; set; }

    public int MaxRetries { get; set; }

    public string? TemplateName { get; set; }

    public string? TemplateData { get; set; }

    public int Priority { get; set; }

    public string? MessageId { get; set; }

    public string BccEmails { get; set; } = null!;

    public string CcEmails { get; set; } = null!;

    public string ToEmails { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public double? BackoffMultiplier { get; set; }

    public string? BypassReason { get; set; }

    public int ConcurrentAccessAttempts { get; set; }

    public bool CulturalDelayBypassed { get; set; }

    public string? CulturalDelayReason { get; set; }

    public bool CulturalTimingOptimized { get; set; }

    public bool DeliveryConfirmationReceived { get; set; }

    public bool DiasporaOptimized { get; set; }

    public string? FestivalContext { get; set; }

    public bool GeographicOptimization { get; set; }

    public int? GeographicRegion { get; set; }

    public bool HasAllRecipientsDelivered { get; set; }

    public DateTime? LastStateTransition { get; set; }

    public DateTime? LocalizedSendTime { get; set; }

    public DateTime? OptimalSendTime { get; set; }

    public string? PermanentFailureReason { get; set; }

    public string? PostponementReason { get; set; }

    public bool ReligiousObservanceConsidered { get; set; }

    public string? RetryStrategy { get; set; }

    public DateTime? SendingStartedAt { get; set; }

    public string? TargetTimezone { get; set; }

    public string? RecipientStatuses { get; set; }

    public string? CulturalContext { get; set; }
}
