using System.Diagnostics;
using LankaConnect.Application.Common.DTOs;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Helpers;
using LankaConnect.Shared.Email.Observability;
using LankaConnect.Shared.Email.Services;

namespace LankaConnect.Infrastructure.Email.Services;

/// <summary>
/// Phase 6A.100: Infrastructure implementation of ITypedEmailService.
///
/// Purpose:
/// - Single email sending approach using typed parameters (no Dictionary approach)
/// - Directly uses AzureEmailService for email delivery (no bridge pattern)
/// - Validates parameters before sending
/// - Provides logging with correlation IDs
/// - Records metrics for dashboard
///
/// This replaces the Shared.TypedEmailService + IEmailServiceBridge pattern
/// with a simpler direct implementation in Infrastructure.
/// </summary>
public class InfrastructureTypedEmailService : ITypedEmailService
{
    private readonly AzureEmailService _emailService;
    private readonly IEmailLogger _logger;
    private readonly IEmailMetrics _metrics;

    public InfrastructureTypedEmailService(
        AzureEmailService emailService,
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

            // Build custom email headers for deliverability compliance
            Dictionary<string, string>? emailHeaders = null;

            // List-Unsubscribe headers for marketing emails (RFC 2369 + RFC 8058)
            if (emailParams is IUnsubscribeableEmail unsub && !string.IsNullOrWhiteSpace(unsub.UnsubscribeUrl))
            {
                emailHeaders = ListUnsubscribeHeaderBuilder.BuildHeaders(unsub.UnsubscribeUrl);
            }

            // Feedback-ID header for Google Postmaster Tools reputation tracking
            // Format: campaignType:correlationId:domain:ESP (per Google guidelines)
            emailHeaders ??= new Dictionary<string, string>();
            emailHeaders["Feedback-ID"] = $"{emailParams.TemplateName}:{correlationId}:lankaconnect.app:acs";

            // Send email via AzureEmailService directly (no bridge)
            var result = await _emailService.SendTemplatedEmailAsync(
                emailParams.TemplateName,
                emailParams.RecipientEmail,
                parameters,
                emailHeaders,
                cancellationToken);

            stopwatch.Stop();
            var durationMs = (int)stopwatch.ElapsedMilliseconds;

            // Record metrics
            _metrics.RecordEmailSent(emailParams.TemplateName, durationMs, result.IsSuccess);

            if (result.IsSuccess)
            {
                // Log success
                _logger.LogEmailSendSuccess(correlationId, emailParams.TemplateName, durationMs);
                return TypedEmailSendResult.Ok(correlationId, durationMs);
            }
            else
            {
                // Log failure
                _logger.LogEmailSendFailure(correlationId, emailParams.TemplateName, result.Error ?? "Unknown error", null);
                return TypedEmailSendResult.Fail(correlationId, new List<string> { result.Error ?? "Email service returned failure" });
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

            // Convert DTO attachments to EmailAttachment
            var emailAttachments = attachments?.Select(a => new EmailAttachment
            {
                FileName = a.FileName,
                Content = a.Content,
                ContentType = a.ContentType,
                ContentId = a.ContentId
            }).ToList();

            // Build custom email headers for deliverability compliance
            Dictionary<string, string>? attachEmailHeaders = null;

            // List-Unsubscribe headers for marketing emails (RFC 2369 + RFC 8058)
            if (emailParams is IUnsubscribeableEmail unsubAttach && !string.IsNullOrWhiteSpace(unsubAttach.UnsubscribeUrl))
            {
                attachEmailHeaders = ListUnsubscribeHeaderBuilder.BuildHeaders(unsubAttach.UnsubscribeUrl);
            }

            // Feedback-ID header for Google Postmaster Tools reputation tracking
            attachEmailHeaders ??= new Dictionary<string, string>();
            attachEmailHeaders["Feedback-ID"] = $"{emailParams.TemplateName}:{correlationId}:lankaconnect.app:acs";

            // Send email with attachments via AzureEmailService directly (no bridge)
            var result = await _emailService.SendTemplatedEmailAsync(
                emailParams.TemplateName,
                emailParams.RecipientEmail,
                parameters,
                emailAttachments,
                attachEmailHeaders,
                cancellationToken);

            stopwatch.Stop();
            var durationMs = (int)stopwatch.ElapsedMilliseconds;

            // Record metrics
            _metrics.RecordEmailSent(emailParams.TemplateName, durationMs, result.IsSuccess);

            if (result.IsSuccess)
            {
                // Log success
                _logger.LogEmailSendSuccess(correlationId, emailParams.TemplateName, durationMs);
                return TypedEmailSendResult.Ok(correlationId, durationMs);
            }
            else
            {
                // Log failure
                _logger.LogEmailSendFailure(correlationId, emailParams.TemplateName, result.Error ?? "Unknown error", null);
                return TypedEmailSendResult.Fail(correlationId, new List<string> { result.Error ?? "Email service returned failure" });
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
