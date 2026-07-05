using LankaConnect.Modules.Communications.Contracts.Email.Contracts;

namespace LankaConnect.Modules.Communications.Contracts.Email.Services;

/// <summary>
/// Phase 6A.100: Interface for typed email sending operations.
///
/// Purpose:
/// - Provides strongly-typed email sending API
/// - Single approach - all handlers use typed parameters
/// - Enables compile-time parameter verification
/// - Automatic validation before sending
///
/// Usage:
/// All handlers use: typedEmailService.SendEmailAsync(typedParams, cancellationToken)
/// </summary>
public interface ITypedEmailService
{
    /// <summary>
    /// Sends an email using strongly-typed parameters.
    ///
    /// Process:
    /// 1. Validate parameters (throws validation errors if invalid)
    /// 2. Convert to dictionary via ToDictionary()
    /// 3. Send via underlying email service
    /// 4. Record metrics for dashboard
    /// </summary>
    /// <param name="emailParams">Strongly-typed email parameters</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with success/failure and any errors</returns>
    Task<TypedEmailSendResult> SendEmailAsync(
        IEmailParameters emailParams,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Phase 6A.100: Sends an email with attachments using strongly-typed parameters.
    /// Used for paid event confirmations with ticket PDF attachments.
    ///
    /// Process:
    /// 1. Validate parameters
    /// 2. Convert to dictionary via ToDictionary()
    /// 3. Send with attachments via underlying email service
    /// 4. Record metrics for dashboard
    /// </summary>
    /// <param name="emailParams">Strongly-typed email parameters</param>
    /// <param name="attachments">List of email attachments (e.g., ticket PDFs)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with success/failure and any errors</returns>
    Task<TypedEmailSendResult> SendEmailWithAttachmentsAsync(
        IEmailParameters emailParams,
        List<EmailAttachmentDto> attachments,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a typed email send operation.
/// </summary>
public class TypedEmailSendResult
{
    /// <summary>
    /// Whether the email was sent successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Correlation ID for tracing this email operation.
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Error messages if the send failed.
    /// </summary>
    public List<string> Errors { get; set; } = new();


    /// <summary>
    /// Duration of the send operation in milliseconds.
    /// </summary>
    public int DurationMs { get; set; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static TypedEmailSendResult Ok(string correlationId, int durationMs)
    {
        return new TypedEmailSendResult
        {
            Success = true,
            CorrelationId = correlationId,
            DurationMs = durationMs
        };
    }

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    public static TypedEmailSendResult Fail(string correlationId, List<string> errors)
    {
        return new TypedEmailSendResult
        {
            Success = false,
            CorrelationId = correlationId,
            Errors = errors
        };
    }

    /// <summary>
    /// Creates a failed result from exception.
    /// </summary>
    public static TypedEmailSendResult Fail(string correlationId, Exception ex)
    {
        return new TypedEmailSendResult
        {
            Success = false,
            CorrelationId = correlationId,
            Errors = new List<string> { ex.Message }
        };
    }
}

/// <summary>
/// DTO for email attachments (e.g., ticket PDFs).
/// Phase 6A.100: Moved from IEmailServiceBridge after bridge pattern removal.
/// </summary>
public class EmailAttachmentDto
{
    /// <summary>
    /// The file name for the attachment.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// The binary content of the attachment.
    /// </summary>
    public byte[] Content { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// The MIME content type of the attachment.
    /// </summary>
    public string ContentType { get; set; } = "application/octet-stream";

    /// <summary>
    /// Optional Content-ID for inline attachments.
    /// </summary>
    public string? ContentId { get; set; }
}
