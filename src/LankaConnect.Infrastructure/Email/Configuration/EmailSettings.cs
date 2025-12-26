namespace LankaConnect.Infrastructure.Email.Configuration;

public class EmailSettings
{
    public const string SectionName = "EmailSettings";

    /// <summary>
    /// Email provider to use: "Azure" (default), "SMTP"
    /// </summary>
    public string Provider { get; set; } = "Azure";

    // Azure Communication Services settings
    /// <summary>
    /// Azure Communication Services connection string
    /// Format: endpoint=https://xxx.communication.azure.com/;accesskey=xxx
    /// </summary>
    public string AzureConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Azure sender email address (from verified domain)
    /// Example: DoNotReply@xxx.azurecomm.net
    /// </summary>
    public string AzureSenderAddress { get; set; } = string.Empty;

    // SMTP settings (for non-Azure providers like SendGrid, Gmail, etc.)
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    public int TimeoutInSeconds { get; set; } = 30;

    // Compatibility aliases for EmailService (maps to SMTP settings)
    public string Host => SmtpServer;
    public int Port => SmtpPort;
    public string FromEmail => !string.IsNullOrEmpty(AzureSenderAddress) ? AzureSenderAddress : SenderEmail;
    public string FromName => SenderName;
    
    // Queue settings
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelayInMinutes { get; set; } = 5;
    public int BatchSize { get; set; } = 10;
    public int ProcessingIntervalInSeconds { get; set; } = 30;
    
    // Template settings
    public string TemplateBasePath { get; set; } = "Templates/Email";
    public bool CacheTemplates { get; set; } = true;
    public int TemplateCacheExpiryInMinutes { get; set; } = 60;
    
    // Development settings
    public bool IsDevelopment { get; set; } = false;
    public bool SaveEmailsToFile { get; set; } = false;
    public string EmailSaveDirectory { get; set; } = "EmailOutput";

    // Email verification settings (Phase 6A.53)
    public EmailVerificationSettings EmailVerification { get; set; } = new();

    // Organizer email settings (Phase 6A.50)
    public OrganizerEmailSettings OrganizerEmail { get; set; } = new();
}

/// <summary>
/// Settings for member email verification system.
/// Phase 6A.53 - Email Verification
/// </summary>
public sealed class EmailVerificationSettings
{
    /// <summary>
    /// Email verification token expiry in hours (default: 24 hours)
    /// After this period, user must request a new verification email
    /// </summary>
    public int TokenExpiryInHours { get; set; } = 24;

    /// <summary>
    /// Maximum number of verification emails per user per day (default: 3)
    /// Prevents abuse and spam
    /// </summary>
    public int MaxVerificationEmailsPerDay { get; set; } = 3;

    /// <summary>
    /// Whether to automatically send verification email on registration (default: true)
    /// Set to false if verification should be triggered manually
    /// </summary>
    public bool SendOnRegistration { get; set; } = true;

    /// <summary>
    /// Whether to block unverified users from certain actions (default: false)
    /// Phase 1: Allow unverified users to browse/register
    /// Phase 2+: May restrict event creation to verified users
    /// </summary>
    public bool RequireVerificationForActions { get; set; } = false;
}

/// <summary>
/// Settings for organizer manual email sending feature.
/// Phase 6A.50 - Manual Email Sending
/// </summary>
public sealed class OrganizerEmailSettings
{
    /// <summary>
    /// Maximum number of emails organizer can send per event per day (default: 5)
    /// Prevents abuse and excessive email sending
    /// </summary>
    public int MaxEmailsPerEventPerDay { get; set; } = 5;

    /// <summary>
    /// Maximum recipients per email (default: 1000)
    /// Prevents overloading email service with too many recipients
    /// </summary>
    public int MaxRecipientsPerEmail { get; set; } = 1000;

    /// <summary>
    /// Minimum time between emails in minutes (default: 60 minutes)
    /// Prevents rapid-fire email sending to same event attendees
    /// </summary>
    public int MinTimeBetweenEmailsInMinutes { get; set; } = 60;

    /// <summary>
    /// Whether to allow organizers to send emails to non-attendees (default: false)
    /// If true, organizers can send to email groups beyond just attendees/signups
    /// </summary>
    public bool AllowEmailToNonAttendees { get; set; } = false;

    /// <summary>
    /// Maximum email body length in characters (default: 5000)
    /// Prevents extremely large email content
    /// </summary>
    public int MaxEmailBodyLength { get; set; } = 5000;
}