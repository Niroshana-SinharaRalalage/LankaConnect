using System.Diagnostics;
using LankaConnect.Shared.Email.Configuration;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Observability;

namespace LankaConnect.Shared.Email.Services;

/// <summary>
/// Phase 6A.87: Adapter that bridges typed email parameters with existing email service.
///
/// Purpose:
/// - Wraps existing email sending functionality
/// - Uses feature flags to control typed vs Dictionary approach
/// - Provides logging with correlation IDs
/// - Records metrics for dashboard
/// - Validates parameters before sending
///
/// Feature Flag Flow:
/// 1. Check EmailFeatureFlags.IsEnabledForHandler(handlerName)
/// 2. Log the feature flag decision
/// 3. If enabled: Use typed parameters, validate, convert via ToDictionary()
/// 4. If disabled: Skip validation, convert via ToDictionary()
/// 5. Send via underlying email service
/// 6. Record metrics
///
/// Error Handling:
/// - Validation failures logged and returned (don't send if invalid)
/// - Service exceptions caught, logged, and returned as failure
/// - All operations use correlation ID for tracing
///
/// Metrics:
/// - RecordHandlerUsage: Tracks typed vs Dictionary usage per handler
/// - RecordEmailSent: Tracks success/failure rate per template
/// - RecordParameterValidationFailure: Tracks validation issues
/// </summary>
public class TypedEmailServiceAdapter : ITypedEmailService
{
    private readonly IEmailServiceBridge _emailService;
    private readonly EmailFeatureFlags _featureFlags;
    private readonly IEmailLogger _logger;
    private readonly IEmailMetrics _metrics;

    public TypedEmailServiceAdapter(
        IEmailServiceBridge emailService,
        EmailFeatureFlags featureFlags,
        IEmailLogger logger,
        IEmailMetrics metrics)
    {
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _featureFlags = featureFlags ?? throw new ArgumentNullException(nameof(featureFlags));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    }

    /// <inheritdoc />
    public async Task<TypedEmailSendResult> SendEmailAsync(
        IEmailParameters emailParams,
        string handlerName,
        CancellationToken cancellationToken = default)
    {
        var correlationId = _logger.GenerateCorrelationId();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Check feature flag for this handler
            var useTypedParameters = _featureFlags.IsEnabledForHandler(handlerName);
            var flagReason = GetFeatureFlagReason(handlerName, useTypedParameters);

            // Log feature flag decision
            _logger.LogFeatureFlagCheck(handlerName, useTypedParameters, flagReason);

            // Record handler usage for migration tracking
            _metrics.RecordHandlerUsage(handlerName, useTypedParameters);

            // Validate parameters if validation is enabled
            if (_featureFlags.EnableValidation)
            {
                if (!emailParams.Validate(out var validationErrors))
                {
                    // Log validation failure
                    _logger.LogParameterValidationFailure(
                        correlationId,
                        emailParams.TemplateName,
                        validationErrors);

                    // Record validation failure metric
                    _metrics.RecordParameterValidationFailure(emailParams.TemplateName);

                    return TypedEmailSendResult.Fail(correlationId, validationErrors, useTypedParameters);
                }
            }

            // Log email send start
            _logger.LogEmailSendStart(correlationId, emailParams.TemplateName, emailParams.RecipientEmail);

            // Convert to dictionary (both approaches use this for now)
            var parameters = emailParams.ToDictionary();

            // Send email via underlying service
            var success = await _emailService.SendTemplatedEmailAsync(
                emailParams.TemplateName,
                emailParams.RecipientEmail,
                parameters,
                cancellationToken);

            stopwatch.Stop();
            var durationMs = (int)stopwatch.ElapsedMilliseconds;

            // Record metrics
            _metrics.RecordEmailSent(emailParams.TemplateName, durationMs, success);

            if (success)
            {
                // Log success
                _logger.LogEmailSendSuccess(correlationId, emailParams.TemplateName, durationMs);
                return TypedEmailSendResult.Ok(correlationId, useTypedParameters, durationMs);
            }
            else
            {
                // Log failure
                _logger.LogEmailSendFailure(correlationId, emailParams.TemplateName, "Email service returned failure", null);
                return TypedEmailSendResult.Fail(correlationId, new List<string> { "Email service returned failure" }, useTypedParameters);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var durationMs = (int)stopwatch.ElapsedMilliseconds;

            // Log exception
            _logger.LogEmailSendFailure(correlationId, emailParams.TemplateName, ex.Message, ex);

            // Record failure metric
            _metrics.RecordEmailSent(emailParams.TemplateName, durationMs, false);

            return TypedEmailSendResult.Fail(correlationId, ex);
        }
    }

    /// <summary>
    /// Gets a human-readable reason for the feature flag decision.
    /// Used for logging to help understand why a particular approach was used.
    /// </summary>
    private string GetFeatureFlagReason(string handlerName, bool useTyped)
    {
        // Check if there's a handler-specific override
        if (_featureFlags.HandlerOverrides.ContainsKey(handlerName))
        {
            return useTyped
                ? $"Handler override enabled for {handlerName}"
                : $"Handler override disabled for {handlerName}";
        }

        // Using global setting
        return useTyped
            ? "Global UseTypedParameters flag is enabled"
            : "Global UseTypedParameters flag is disabled";
    }
}

/// <summary>
/// Bridge interface that abstracts the existing IEmailService.
/// This allows testing the adapter without depending on the full Result type.
///
/// In production, an implementation of this interface wraps the real IEmailService:
/// - Calls IEmailService.SendTemplatedEmailAsync
/// - Converts Result to bool (success/failure)
///
/// This separation allows:
/// - TypedEmailServiceAdapter to be tested in isolation
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
}
