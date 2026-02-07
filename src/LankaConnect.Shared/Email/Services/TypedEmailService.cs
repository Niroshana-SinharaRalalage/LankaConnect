using System.Diagnostics;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Observability;

namespace LankaConnect.Shared.Email.Services;

/// <summary>
/// Phase 6A.100: Simplified implementation of ITypedEmailService.
///
/// Purpose:
/// - Single email sending approach (no feature flags)
/// - Always validates parameters before sending
/// - Always uses typed parameters converted via ToDictionary()
/// - Provides logging with correlation IDs
/// - Records metrics for dashboard
///
/// This replaces TypedEmailServiceAdapter which had feature flag complexity.
/// </summary>
public class TypedEmailService : ITypedEmailService
{
    private readonly IEmailServiceBridge _emailService;
    private readonly IEmailLogger _logger;
    private readonly IEmailMetrics _metrics;

    public TypedEmailService(
        IEmailServiceBridge emailService,
        IEmailLogger logger,
        IEmailMetrics metrics)
    {
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
    }

    /// <inheritdoc />
    public async Task<TypedEmailSendResult> SendEmailAsync(
        IEmailParameters emailParams,
        CancellationToken cancellationToken = default)
    {
        var correlationId = _logger.GenerateCorrelationId();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Validate parameters (mandatory)
            if (!emailParams.Validate(out var validationErrors))
            {
                // Log validation failure
                _logger.LogParameterValidationFailure(
                    correlationId,
                    emailParams.TemplateName,
                    validationErrors);

                // Record validation failure metric
                _metrics.RecordParameterValidationFailure(emailParams.TemplateName);

                return TypedEmailSendResult.Fail(correlationId, validationErrors);
            }

            // Log email send start
            _logger.LogEmailSendStart(correlationId, emailParams.TemplateName, emailParams.RecipientEmail);

            // Convert to dictionary for template rendering
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
                return TypedEmailSendResult.Ok(correlationId, durationMs);
            }
            else
            {
                // Log failure
                _logger.LogEmailSendFailure(correlationId, emailParams.TemplateName, "Email service returned failure", null);
                return TypedEmailSendResult.Fail(correlationId, new List<string> { "Email service returned failure" });
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

    /// <inheritdoc />
    public async Task<TypedEmailSendResult> SendEmailWithAttachmentsAsync(
        IEmailParameters emailParams,
        List<EmailAttachmentDto> attachments,
        CancellationToken cancellationToken = default)
    {
        var correlationId = _logger.GenerateCorrelationId();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // Validate parameters (mandatory)
            if (!emailParams.Validate(out var validationErrors))
            {
                // Log validation failure
                _logger.LogParameterValidationFailure(
                    correlationId,
                    emailParams.TemplateName,
                    validationErrors);

                // Record validation failure metric
                _metrics.RecordParameterValidationFailure(emailParams.TemplateName);

                return TypedEmailSendResult.Fail(correlationId, validationErrors);
            }

            // Log email send start
            _logger.LogEmailSendStart(correlationId, emailParams.TemplateName, emailParams.RecipientEmail);

            // Convert to dictionary for template rendering
            var parameters = emailParams.ToDictionary();

            // Get recipient name from parameters or use email
            var recipientName = emailParams.RecipientName;

            // Send email with attachments via underlying service
            var success = await _emailService.SendTemplatedEmailWithAttachmentsAsync(
                emailParams.TemplateName,
                emailParams.RecipientEmail,
                recipientName,
                parameters,
                attachments,
                cancellationToken);

            stopwatch.Stop();
            var durationMs = (int)stopwatch.ElapsedMilliseconds;

            // Record metrics
            _metrics.RecordEmailSent(emailParams.TemplateName, durationMs, success);

            if (success)
            {
                // Log success
                _logger.LogEmailSendSuccess(correlationId, emailParams.TemplateName, durationMs);
                return TypedEmailSendResult.Ok(correlationId, durationMs);
            }
            else
            {
                // Log failure
                _logger.LogEmailSendFailure(correlationId, emailParams.TemplateName, "Email service returned failure", null);
                return TypedEmailSendResult.Fail(correlationId, new List<string> { "Email service returned failure" });
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
}
