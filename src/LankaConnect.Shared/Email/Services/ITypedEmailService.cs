using LankaConnect.Shared.Email.Contracts;

namespace LankaConnect.Shared.Email.Services;

/// <summary>
/// Phase 6A.87: Interface for typed email sending operations.
///
/// Purpose:
/// - Provides strongly-typed email sending API
/// - Alternative to Dictionary-based IEmailService
/// - Enables compile-time parameter verification
/// - Supports gradual migration via feature flags
///
/// Usage:
/// Handlers can use either:
/// 1. Old way (Dictionary): emailService.SendTemplatedEmailAsync(templateName, email, dict)
/// 2. New way (Typed): typedEmailService.SendEmailAsync(typedParams, handlerName)
///
/// Feature flags control which approach is active per handler.
/// </summary>
public interface ITypedEmailService
{
    /// <summary>
    /// Sends an email using strongly-typed parameters.
    ///
    /// Process:
    /// 1. Check feature flag for handler
    /// 2. If typed enabled: Validate parameters, log, send via adapter
    /// 3. If typed disabled: Convert to Dictionary, send via existing service
    /// 4. Record metrics for dashboard
    /// </summary>
    /// <param name="emailParams">Strongly-typed email parameters</param>
    /// <param name="handlerName">Handler class name (for feature flag lookup)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result with success/failure and any errors</returns>
    Task<TypedEmailSendResult> SendEmailAsync(
        IEmailParameters emailParams,
        string handlerName,
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
    /// Whether typed parameters were used (vs Dictionary fallback).
    /// </summary>
    public bool UsedTypedParameters { get; set; }

    /// <summary>
    /// Duration of the send operation in milliseconds.
    /// </summary>
    public int DurationMs { get; set; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static TypedEmailSendResult Ok(string correlationId, bool usedTyped, int durationMs)
    {
        return new TypedEmailSendResult
        {
            Success = true,
            CorrelationId = correlationId,
            UsedTypedParameters = usedTyped,
            DurationMs = durationMs
        };
    }

    /// <summary>
    /// Creates a failed result with errors.
    /// </summary>
    public static TypedEmailSendResult Fail(string correlationId, List<string> errors, bool usedTyped = false)
    {
        return new TypedEmailSendResult
        {
            Success = false,
            CorrelationId = correlationId,
            Errors = errors,
            UsedTypedParameters = usedTyped
        };
    }

    /// <summary>
    /// Creates a failed result from exception.
    /// </summary>
    public static TypedEmailSendResult Fail(string correlationId, Exception ex, bool usedTyped = false)
    {
        return new TypedEmailSendResult
        {
            Success = false,
            CorrelationId = correlationId,
            Errors = new List<string> { ex.Message },
            UsedTypedParameters = usedTyped
        };
    }
}
