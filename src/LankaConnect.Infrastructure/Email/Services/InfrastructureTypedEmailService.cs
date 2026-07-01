using System.Diagnostics;
using System.Text.Json;
using LankaConnect.Application.Common.DTOs;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Infrastructure.Data;
using LankaConnect.Infrastructure.Email.Configuration;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Helpers;
using LankaConnect.Shared.Email.Observability;
using LankaConnect.Shared.Email.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
///
/// Phase 6A.148.W5.6.B.OBS2 — for any params implementing <see cref="IDispatchLoggable"/>
/// (today: every refund-flow email), writes a durable row to <c>communications.email_dispatch_log</c>
/// BEFORE the provider hand-off so the operator post-mortem flow has evidence even after
/// container stdout retention (~25 min) expires. The log write is BEST-EFFORT — a write
/// failure must NOT block the actual email send (the email is the user-visible side
/// effect; the log row is for operators only).
/// </summary>
public class InfrastructureTypedEmailService : ITypedEmailService
{
    private readonly AzureEmailService _emailService;
    private readonly IEmailLogger _logger;
    private readonly IEmailMetrics _metrics;
    private readonly BrandingOptions _branding;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<InfrastructureTypedEmailService> _serviceLogger;

    public InfrastructureTypedEmailService(
        AzureEmailService emailService,
        IEmailLogger logger,
        IEmailMetrics metrics,
        IOptions<BrandingOptions> brandingOptions,
        AppDbContext dbContext,
        ILogger<InfrastructureTypedEmailService> serviceLogger)
    {
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _branding = brandingOptions?.Value ?? throw new ArgumentNullException(nameof(brandingOptions));
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _serviceLogger = serviceLogger ?? throw new ArgumentNullException(nameof(serviceLogger));
    }

    /// <inheritdoc />
    public async Task<TypedEmailSendResult> SendEmailAsync(
        IEmailParameters emailParams,
        CancellationToken cancellationToken = default)
    {
        var correlationId = _logger.GenerateCorrelationId();
        var stopwatch = Stopwatch.StartNew();

        // Wave 9.h.10.5 stdout probe (architect-ruled 2026-07-01, diagnostic-only, no
        // behavior change). Bypass the ILogger filter chain to guarantee capture by
        // `az containerapp logs show --follow`. Removes after Session 2 fix lands.
        Console.WriteLine($"[W9H10-EMAIL-PROBE] ENTRY correlationId={correlationId} template={emailParams.TemplateName} recipient={emailParams.RecipientEmail}");

        try
        {
            // Validate parameters (mandatory)
            if (!emailParams.Validate(out var validationErrors))
            {
                Console.WriteLine($"[W9H10-EMAIL-PROBE] VALIDATION-FAIL correlationId={correlationId} template={emailParams.TemplateName} errors={string.Join(";", validationErrors)}");

                // Log validation failure
                _logger.LogParameterValidationFailure(
                    correlationId,
                    emailParams.TemplateName,
                    validationErrors);

                // Record validation failure metric
                _metrics.RecordParameterValidationFailure(emailParams.TemplateName);

                return TypedEmailSendResult.Fail(correlationId, validationErrors);
            }

            Console.WriteLine($"[W9H10-EMAIL-PROBE] VALIDATION-OK correlationId={correlationId} template={emailParams.TemplateName}");

            // Log email send start
            _logger.LogEmailSendStart(correlationId, emailParams.TemplateName, emailParams.RecipientEmail);

            // Convert to dictionary for template rendering
            var parameters = emailParams.ToDictionary();

            // Override SupportEmail with the authoritative value from BrandingOptions config.
            // This ensures all email templates show the correct support address regardless of
            // the hardcoded defaults in individual EmailParams classes.
            parameters["SupportEmail"] = _branding.SupportEmail;

            // Phase 6A.148.W5.6.B.OBS2 — write durable dispatch log BEFORE provider hand-off.
            // Best-effort: failure here must NOT abort the email send.
            var dispatchLogId = await TryWriteDispatchLogForSendAsync(
                emailParams, parameters, correlationId, cancellationToken);

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

            Console.WriteLine($"[W9H10-EMAIL-PROBE] PROVIDER-INVOKE correlationId={correlationId} template={emailParams.TemplateName}");

            // Send email via AzureEmailService directly (no bridge)
            var result = await _emailService.SendTemplatedEmailAsync(
                emailParams.TemplateName,
                emailParams.RecipientEmail,
                parameters,
                emailHeaders,
                cancellationToken);

            stopwatch.Stop();
            var durationMs = (int)stopwatch.ElapsedMilliseconds;

            Console.WriteLine($"[W9H10-EMAIL-PROBE] PROVIDER-RESULT correlationId={correlationId} template={emailParams.TemplateName} success={result.IsSuccess} error={result.Error ?? "<none>"} durationMs={durationMs}");

            // Record metrics
            _metrics.RecordEmailSent(emailParams.TemplateName, durationMs, result.IsSuccess);

            // Phase 6A.148.W5.6.B.OBS2 — update dispatch log with provider response (best-effort).
            await TryUpdateDispatchLogProviderResponseAsync(
                dispatchLogId,
                providerMessageId: null, // SendTemplatedEmailAsync result does not expose ACS message id today
                providerStatus: result.IsSuccess ? "sent" : "failed",
                cancellationToken);

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

            Console.WriteLine($"[W9H10-EMAIL-PROBE] EXCEPTION correlationId={correlationId} template={emailParams.TemplateName} exceptionType={ex.GetType().FullName} message={ex.Message}");
            Console.WriteLine($"[W9H10-EMAIL-PROBE] EXCEPTION-STACK correlationId={correlationId} stack={ex.StackTrace}");

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

            // Phase 6A.148.W5.6.B.OBS2 — write durable dispatch log BEFORE provider hand-off (best-effort).
            var dispatchLogId = await TryWriteDispatchLogForSendAsync(
                emailParams, parameters, correlationId, cancellationToken);

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

            await TryUpdateDispatchLogProviderResponseAsync(
                dispatchLogId,
                providerMessageId: null,
                providerStatus: result.IsSuccess ? "sent" : "failed",
                cancellationToken);

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

    /// <summary>
    /// Phase 6A.148.W5.6.B.OBS2 — writes a row to <c>communications.email_dispatch_log</c>
    /// for params that opt in via <see cref="IDispatchLoggable"/>. Returns the new row id on
    /// success (so caller can update it post-send), or null on opt-out / persist failure.
    /// </summary>
    private async Task<Guid?> TryWriteDispatchLogForSendAsync(
        IEmailParameters emailParams,
        Dictionary<string, object> parameters,
        string correlationId,
        CancellationToken cancellationToken)
    {
        if (emailParams is not IDispatchLoggable loggable)
        {
            return null;
        }

        try
        {
            var correlationGuid = Guid.TryParse(correlationId, out var parsed) ? parsed : Guid.NewGuid();
            var payloadJson = SerializeParametersBestEffort(parameters);

            var row = EmailDispatchLog.ForSend(
                correlationId: correlationGuid,
                templateName: emailParams.TemplateName,
                recipientEmail: emailParams.RecipientEmail,
                recipientName: emailParams.RecipientName,
                subjectRendered: null, // not exposed by AzureEmailService at this layer
                payloadJson: payloadJson,
                refundRequestId: loggable.DispatchRefundRequestId,
                entityType: loggable.DispatchEntityType,
                entityId: loggable.DispatchEntityId,
                providerMessageId: null,
                providerStatus: "pending");

            _dbContext.EmailDispatchLogs.Add(row);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return row.Id;
        }
        catch (Exception ex)
        {
            // Best-effort: log + continue. The user-visible email send MUST NOT fail
            // because of an audit-log persistence problem.
            _serviceLogger.LogWarning(ex,
                "[EmailDispatchLog] Failed to persist 'send' row for template {Template} recipient {Recipient} correlation {Correlation}",
                emailParams.TemplateName, emailParams.RecipientEmail, correlationId);
            return null;
        }
    }

    private async Task TryUpdateDispatchLogProviderResponseAsync(
        Guid? dispatchLogId,
        string? providerMessageId,
        string? providerStatus,
        CancellationToken cancellationToken)
    {
        if (!dispatchLogId.HasValue)
        {
            return;
        }

        try
        {
            var row = await _dbContext.EmailDispatchLogs.FindAsync(new object[] { dispatchLogId.Value }, cancellationToken);
            if (row is null) return;
            row.RecordProviderResponse(providerMessageId, providerStatus);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _serviceLogger.LogWarning(ex,
                "[EmailDispatchLog] Failed to update provider response for dispatch-log row {DispatchLogId}",
                dispatchLogId.Value);
        }
    }

    private static string? SerializeParametersBestEffort(Dictionary<string, object> parameters)
    {
        try
        {
            // Serialize only primitive-friendly subset; skip values that would blow up JsonSerializer
            // (templates occasionally pass IEnumerable of complex view-models).
            var safe = parameters
                .Where(kv => kv.Value is null
                          || kv.Value is string
                          || kv.Value is bool
                          || kv.Value is int or long or short or byte
                          || kv.Value is decimal or double or float
                          || kv.Value is Guid
                          || kv.Value is DateTime
                          || kv.Value is DateTimeOffset)
                .ToDictionary(kv => kv.Key, kv => kv.Value);
            return JsonSerializer.Serialize(safe);
        }
        catch
        {
            return null;
        }
    }
}
