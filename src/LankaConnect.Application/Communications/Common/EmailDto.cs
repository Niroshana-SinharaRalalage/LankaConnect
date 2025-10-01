namespace LankaConnect.Application.Communications.Common;

/// <summary>
/// Data transfer object for email message information
/// </summary>
public class EmailMessageDto
{
    public string ToEmail { get; set; } = string.Empty;
    public string ToName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public EmailType Type { get; set; }
    public EmailPriority Priority { get; set; } = EmailPriority.Normal;
    public DateTime? ScheduledAt { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Data transfer object for email template information
/// </summary>
public class EmailTemplateDto
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlTemplate { get; set; } = string.Empty;
    public string PlainTextTemplate { get; set; } = string.Empty;
    public EmailTemplateCategory Category { get; set; }
    public List<string> RequiredParameters { get; set; } = new();
    public List<string> OptionalParameters { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastModified { get; set; }
}

/// <summary>
/// Data transfer object for email verification information
/// </summary>
public class EmailVerificationDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool IsEmailVerified { get; set; }
    public DateTime? VerificationTokenExpiresAt { get; set; }
    public DateTime? LastVerificationSentAt { get; set; }
    public int VerificationAttempts { get; set; }
}

/// <summary>
/// Data transfer object for password reset information
/// </summary>
public class PasswordResetDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool HasActiveResetToken { get; set; }
    public DateTime? ResetTokenExpiresAt { get; set; }
    public DateTime? LastResetRequestAt { get; set; }
    public int ResetAttempts { get; set; }
}

/// <summary>
/// Data transfer object for user email preferences
/// </summary>
public class UserEmailPreferencesDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool ReceiveWelcomeEmails { get; set; } = true;
    public bool ReceiveBusinessNotifications { get; set; } = true;
    public bool ReceiveMarketingEmails { get; set; } = false;
    public bool ReceiveSystemAlerts { get; set; } = true;
    public bool ReceivePasswordAlerts { get; set; } = true;
    public EmailFrequency NotificationFrequency { get; set; } = EmailFrequency.Immediate;
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Data transfer object for email status information
/// </summary>
public class EmailStatusDto
{
    public Guid EmailId { get; set; }
    public string ToEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public EmailStatus Status { get; set; }
    public EmailType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public string? FailureReason { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextRetryAt { get; set; }
}

/// <summary>
/// Email types for categorization
/// </summary>
public enum EmailType
{
    EmailVerification = 1,
    PasswordReset = 2,
    Welcome = 3,
    BusinessNotification = 4,
    SystemAlert = 5,
    Marketing = 6
}

/// <summary>
/// Email priority levels
/// </summary>
public enum EmailPriority
{
    Low = 3,
    Normal = 2,
    High = 1
}

/// <summary>
/// Email template categories
/// </summary>
public enum EmailTemplateCategory
{
    Authentication = 1,
    Business = 2,
    System = 3,
    Marketing = 4,
    Notification = 5
}

/// <summary>
/// Email delivery status
/// </summary>
public enum EmailStatus
{
    Pending = 1,
    Queued = 2,
    Sending = 3,
    Sent = 4,
    Delivered = 5,
    Failed = 6,
    Bounced = 7,
    Cancelled = 8
}

/// <summary>
/// Email notification frequency settings
/// </summary>
public enum EmailFrequency
{
    Immediate = 1,
    Hourly = 2,
    Daily = 3,
    Weekly = 4,
    Monthly = 5,
    Never = 6
}

/// <summary>
/// Data transfer object for email subscription information
/// </summary>
public class EmailSubscriptionDto
{
    public string SubscriptionType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public EmailFrequency Frequency { get; set; }
    public bool IsRequired { get; set; }
    public DateTime? LastUpdated { get; set; }
}

/// <summary>
/// Response for get user email preferences query
/// </summary>
public class GetUserEmailPreferencesResponse
{
    public UserEmailPreferencesDto Preferences { get; set; }
    public EmailVerificationDto Verification { get; set; }
    public List<EmailSubscriptionDto> Subscriptions { get; set; }

    public GetUserEmailPreferencesResponse(
        UserEmailPreferencesDto preferences,
        EmailVerificationDto verification,
        List<EmailSubscriptionDto> subscriptions)
    {
        Preferences = preferences;
        Verification = verification;
        Subscriptions = subscriptions;
    }
}