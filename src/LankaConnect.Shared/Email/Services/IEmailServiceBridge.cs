namespace LankaConnect.Shared.Email.Services;

/// <summary>
/// Phase 6A.100: Bridge interface that abstracts the existing IEmailService.
/// This allows testing the TypedEmailService without depending on the full Result type.
///
/// In production, an implementation of this interface wraps the real IEmailService:
/// - Calls IEmailService.SendTemplatedEmailAsync
/// - Converts Result to bool (success/failure)
///
/// This separation allows:
/// - TypedEmailService to be tested in isolation
/// - Gradual integration with existing email infrastructure
/// - Clear boundary between shared email module and application layer
/// </summary>
public interface IEmailServiceBridge
{
    /// <summary>
    /// Sends a templated email asynchronously.
    /// </summary>
    /// <param name="templateName">Name of the email template</param>
    /// <param name="recipientEmail">Recipient's email address</param>
    /// <param name="parameters">Template parameters as dictionary</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if email sent successfully, false otherwise</returns>
    Task<bool> SendTemplatedEmailAsync(
        string templateName,
        string recipientEmail,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 6A.100: Sends a templated email with attachments asynchronously.
    /// Used for paid event confirmations with ticket PDF attachments.
    /// </summary>
    /// <param name="templateName">Name of the email template</param>
    /// <param name="recipientEmail">Recipient's email address</param>
    /// <param name="recipientName">Recipient's display name</param>
    /// <param name="parameters">Template parameters as dictionary</param>
    /// <param name="attachments">List of email attachments (e.g., ticket PDFs)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if email sent successfully, false otherwise</returns>
    Task<bool> SendTemplatedEmailWithAttachmentsAsync(
        string templateName,
        string recipientEmail,
        string recipientName,
        Dictionary<string, object> parameters,
        List<EmailAttachmentDto> attachments,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Phase 6A.100: DTO for email attachments in the shared module.
/// Mirrors EmailAttachment from Application layer but without domain dependencies.
/// </summary>
public class EmailAttachmentDto
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/octet-stream";
    public string? ContentId { get; set; }
}
