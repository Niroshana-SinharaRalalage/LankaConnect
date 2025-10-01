using LankaConnect.Domain.Common;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Enterprise;
using LankaConnect.Domain.Common.Models;
using LankaConnect.Domain.Common.Monitoring;
using LankaConnect.Domain.Common.ValueObjects;
using LankaConnect.Domain.Common.Security;
using LankaConnect.Domain.Common.Recovery;
using LankaConnect.Domain.Common.Database;
using LankaConnect.Domain.Common.Enums;
using MultiLanguageModels = LankaConnect.Domain.Common.Database.MultiLanguageRoutingModels;

namespace LankaConnect.Application.Common.Interfaces;

/// <summary>
/// Service interface for sending emails with templates and attachments
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email message asynchronously
    /// </summary>
    /// <param name="emailMessage">The email message to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> SendEmailAsync(EmailMessageDto emailMessage, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an email using a template with parameters
    /// </summary>
    /// <param name="templateName">Name of the email template</param>
    /// <param name="recipientEmail">Recipient email address</param>
    /// <param name="parameters">Template parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating success or failure</returns>
    Task<Result> SendTemplatedEmailAsync(string templateName, string recipientEmail, 
        Dictionary<string, object> parameters, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends bulk emails asynchronously
    /// </summary>
    /// <param name="emailMessages">Collection of email messages to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with bulk send status</returns>
    Task<Result<BulkEmailResult>> SendBulkEmailAsync(IEnumerable<EmailMessageDto> emailMessages, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates email template exists and is properly configured
    /// </summary>
    /// <param name="templateName">Name of the template to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result indicating template validity</returns>
    Task<Result> ValidateTemplateAsync(string templateName, CancellationToken cancellationToken = default);
}

/// <summary>
/// Represents an email message to be sent (DTO for service layer)
/// </summary>
public class EmailMessageDto
{
    public string ToEmail { get; set; } = string.Empty;
    public string ToName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string? PlainTextBody { get; set; }
    public string? FromEmail { get; set; }
    public string? FromName { get; set; }
    public List<EmailAttachment>? Attachments { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    public int Priority { get; set; } = 1; // 1 = High, 2 = Normal, 3 = Low
}

/// <summary>
/// Represents an email attachment
/// </summary>
public class EmailAttachment
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/octet-stream";
}

/// <summary>
/// Result of bulk email sending operation
/// </summary>
public class BulkEmailResult
{
    public int TotalEmails { get; set; }
    public int SuccessfulSends { get; set; }
    public int FailedSends { get; set; }
    public List<string> Errors { get; set; } = new();
}