using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Shared.Email.Observability;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Infrastructure.Email.Services;

/// <summary>
/// Phase 6A.87: Decorator that wraps IEmailService to record metrics for ALL email sends.
///
/// Problem Solved:
/// - AzureEmailService sends emails but doesn't call IEmailMetrics
/// - TypedEmailServiceAdapter calls IEmailMetrics but only 1 handler uses it
/// - 30+ handlers call IEmailService directly, bypassing metrics
///
/// Solution:
/// - This decorator wraps AzureEmailService
/// - ALL calls to IEmailService now go through this decorator
/// - Decorator records metrics before/after delegating to actual service
/// - No changes required to existing handlers
///
/// Metrics Recorded:
/// - RecordEmailSent: template name, duration, success/failure
/// - RecordHandlerUsage: tracks that direct IEmailService was used (not typed)
/// - RecordFailedEmail: detailed failure info for dashboard
/// - RecordTemplateNotFound: when template lookup fails
/// </summary>
public class MetricsRecordingEmailServiceDecorator : IEmailService
{
    private readonly IEmailService _innerService;
    private readonly IEmailMetrics _metrics;
    private readonly ILogger<MetricsRecordingEmailServiceDecorator> _logger;

    public MetricsRecordingEmailServiceDecorator(
        IEmailService innerService,
        IEmailMetrics metrics,
        ILogger<MetricsRecordingEmailServiceDecorator> logger)
    {
        _innerService = innerService ?? throw new ArgumentNullException(nameof(innerService));
        _metrics = metrics ?? throw new ArgumentNullException(nameof(metrics));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result> SendEmailAsync(EmailMessageDto emailMessage, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        const string templateName = "direct-email"; // No template for direct sends

        try
        {
            _logger.LogDebug("[Metrics] Starting SendEmailAsync to {ToEmail}", emailMessage.ToEmail);

            var result = await _innerService.SendEmailAsync(emailMessage, cancellationToken);

            stopwatch.Stop();
            var durationMs = (int)stopwatch.ElapsedMilliseconds;

            // Record metrics
            _metrics.RecordEmailSent(templateName, durationMs, result.IsSuccess);
            _metrics.RecordHandlerUsage("DirectEmailService", usedTypedParameters: false);

            if (!result.IsSuccess)
            {
                _metrics.RecordFailedEmail(
                    Guid.NewGuid().ToString(),
                    templateName,
                    MaskEmail(emailMessage.ToEmail),
                    result.Error ?? "Unknown error",
                    "DirectEmailService");
            }

            _logger.LogDebug("[Metrics] SendEmailAsync completed. Success={Success}, Duration={DurationMs}ms",
                result.IsSuccess, durationMs);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var durationMs = (int)stopwatch.ElapsedMilliseconds;

            _metrics.RecordEmailSent(templateName, durationMs, success: false);
            _metrics.RecordFailedEmail(
                Guid.NewGuid().ToString(),
                templateName,
                MaskEmail(emailMessage.ToEmail),
                ex.Message,
                "DirectEmailService");

            _logger.LogError(ex, "[Metrics] SendEmailAsync failed after {DurationMs}ms", durationMs);
            throw;
        }
    }

    public async Task<Result> SendTemplatedEmailAsync(
        string templateName,
        string recipientEmail,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            _logger.LogDebug("[Metrics] [{CorrelationId}] Starting SendTemplatedEmailAsync. Template={TemplateName}, Recipient={RecipientEmail}",
                correlationId, templateName, recipientEmail);

            var result = await _innerService.SendTemplatedEmailAsync(
                templateName, recipientEmail, parameters, cancellationToken);

            stopwatch.Stop();
            var durationMs = (int)stopwatch.ElapsedMilliseconds;

            // Record metrics
            _metrics.RecordEmailSent(templateName, durationMs, result.IsSuccess);
            _metrics.RecordHandlerUsage("DirectIEmailService", usedTypedParameters: false);

            if (!result.IsSuccess)
            {
                // Check if it's a template not found error
                if (result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                {
                    _metrics.RecordTemplateNotFound(templateName);
                }

                _metrics.RecordFailedEmail(
                    correlationId,
                    templateName,
                    MaskEmail(recipientEmail),
                    result.Error ?? "Unknown error",
                    "DirectIEmailService");
            }

            _logger.LogDebug("[Metrics] [{CorrelationId}] SendTemplatedEmailAsync completed. Success={Success}, Duration={DurationMs}ms",
                correlationId, result.IsSuccess, durationMs);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var durationMs = (int)stopwatch.ElapsedMilliseconds;

            _metrics.RecordEmailSent(templateName, durationMs, success: false);
            _metrics.RecordFailedEmail(
                correlationId,
                templateName,
                MaskEmail(recipientEmail),
                ex.Message,
                "DirectIEmailService");

            _logger.LogError(ex, "[Metrics] [{CorrelationId}] SendTemplatedEmailAsync failed after {DurationMs}ms",
                correlationId, durationMs);
            throw;
        }
    }

    public async Task<Result> SendTemplatedEmailAsync(
        string templateName,
        string recipientEmail,
        Dictionary<string, object> parameters,
        List<EmailAttachment>? attachments,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Guid.NewGuid().ToString();

        try
        {
            _logger.LogDebug("[Metrics] [{CorrelationId}] Starting SendTemplatedEmailAsync with attachments. Template={TemplateName}, Recipient={RecipientEmail}, Attachments={AttachmentCount}",
                correlationId, templateName, recipientEmail, attachments?.Count ?? 0);

            var result = await _innerService.SendTemplatedEmailAsync(
                templateName, recipientEmail, parameters, attachments, cancellationToken);

            stopwatch.Stop();
            var durationMs = (int)stopwatch.ElapsedMilliseconds;

            // Record metrics
            _metrics.RecordEmailSent(templateName, durationMs, result.IsSuccess);
            _metrics.RecordHandlerUsage("DirectIEmailService", usedTypedParameters: false);

            if (!result.IsSuccess)
            {
                if (result.Error?.Contains("not found", StringComparison.OrdinalIgnoreCase) == true)
                {
                    _metrics.RecordTemplateNotFound(templateName);
                }

                _metrics.RecordFailedEmail(
                    correlationId,
                    templateName,
                    MaskEmail(recipientEmail),
                    result.Error ?? "Unknown error",
                    "DirectIEmailService");
            }

            _logger.LogDebug("[Metrics] [{CorrelationId}] SendTemplatedEmailAsync with attachments completed. Success={Success}, Duration={DurationMs}ms",
                correlationId, result.IsSuccess, durationMs);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var durationMs = (int)stopwatch.ElapsedMilliseconds;

            _metrics.RecordEmailSent(templateName, durationMs, success: false);
            _metrics.RecordFailedEmail(
                correlationId,
                templateName,
                MaskEmail(recipientEmail),
                ex.Message,
                "DirectIEmailService");

            _logger.LogError(ex, "[Metrics] [{CorrelationId}] SendTemplatedEmailAsync with attachments failed after {DurationMs}ms",
                correlationId, durationMs);
            throw;
        }
    }

    public async Task<Result<BulkEmailResult>> SendBulkEmailAsync(
        IEnumerable<EmailMessageDto> emailMessages,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        const string templateName = "bulk-email";
        var messageList = emailMessages.ToList();

        try
        {
            _logger.LogDebug("[Metrics] Starting SendBulkEmailAsync for {Count} emails", messageList.Count);

            var result = await _innerService.SendBulkEmailAsync(messageList, cancellationToken);

            stopwatch.Stop();
            var durationMs = (int)stopwatch.ElapsedMilliseconds;

            if (result.IsSuccess)
            {
                var bulkResult = result.Value;

                // Record metrics for each email in the bulk send
                // Use average duration per email
                var avgDurationPerEmail = messageList.Count > 0 ? durationMs / messageList.Count : durationMs;

                // Record successes
                for (int i = 0; i < bulkResult.SuccessfulSends; i++)
                {
                    _metrics.RecordEmailSent(templateName, avgDurationPerEmail, success: true);
                }

                // Record failures
                for (int i = 0; i < bulkResult.FailedSends; i++)
                {
                    _metrics.RecordEmailSent(templateName, avgDurationPerEmail, success: false);
                }

                _metrics.RecordHandlerUsage("BulkEmailService", usedTypedParameters: false);
            }

            _logger.LogDebug("[Metrics] SendBulkEmailAsync completed. Success={Success}, Duration={DurationMs}ms",
                result.IsSuccess, durationMs);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var durationMs = (int)stopwatch.ElapsedMilliseconds;

            // Record failure for all emails
            foreach (var _ in messageList)
            {
                _metrics.RecordEmailSent(templateName, durationMs / Math.Max(1, messageList.Count), success: false);
            }

            _logger.LogError(ex, "[Metrics] SendBulkEmailAsync failed after {DurationMs}ms", durationMs);
            throw;
        }
    }

    public async Task<Result> ValidateTemplateAsync(string templateName, CancellationToken cancellationToken = default)
    {
        // No metrics needed for validation - just pass through
        return await _innerService.ValidateTemplateAsync(templateName, cancellationToken);
    }

    /// <summary>
    /// Masks email for privacy in logs and metrics.
    /// Example: test@example.com -> t***@e***.com
    /// </summary>
    private static string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email) || !email.Contains('@'))
            return "***@***.***";

        var parts = email.Split('@');
        var localPart = parts[0];
        var domainParts = parts[1].Split('.');

        var maskedLocal = localPart.Length > 1
            ? localPart[0] + new string('*', Math.Min(3, localPart.Length - 1))
            : "*";

        var maskedDomain = domainParts[0].Length > 1
            ? domainParts[0][0] + new string('*', Math.Min(3, domainParts[0].Length - 1))
            : "*";

        var tld = domainParts.Length > 1 ? domainParts[^1] : "***";

        return $"{maskedLocal}@{maskedDomain}.{tld}";
    }
}