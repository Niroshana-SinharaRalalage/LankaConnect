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
}