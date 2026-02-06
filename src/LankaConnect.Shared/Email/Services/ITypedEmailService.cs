using LankaConnect.Shared.Email.Contracts;

namespace LankaConnect.Shared.Email.Services;

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
