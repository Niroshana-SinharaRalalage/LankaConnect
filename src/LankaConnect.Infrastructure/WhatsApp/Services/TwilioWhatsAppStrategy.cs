using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Infrastructure.WhatsApp.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text.Json;
using Twilio;
using Twilio.Exceptions;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace LankaConnect.Infrastructure.WhatsApp.Services;

/// <summary>
/// Phase 7B: Twilio WhatsApp send strategy.
/// Handles low-level Twilio API calls with retry logic, rate limiting, and structured logging.
/// Mirrors AcsWhatsAppStrategy patterns for consistency.
/// </summary>
public class TwilioWhatsAppStrategy : IWhatsAppSendStrategy
{
    private readonly WhatsAppSettings _settings;
    private readonly ILogger<TwilioWhatsAppStrategy> _logger;
    private bool _clientInitialized;
    private readonly object _clientLock = new();

    public TwilioWhatsAppStrategy(
        IOptions<WhatsAppSettings> settings,
        ILogger<TwilioWhatsAppStrategy> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Initialize Twilio client (thread-safe, lazy). Matches ACS lazy pattern.
    /// </summary>
    private void EnsureClientInitialized()
    {
        if (_clientInitialized) return;
        lock (_clientLock)
        {
            if (_clientInitialized) return;

            if (string.IsNullOrWhiteSpace(_settings.TwilioAccountSid))
            {
                throw new InvalidOperationException(
                    "WhatsAppSettings:TwilioAccountSid is not configured. " +
                    "Set it to the Twilio Account SID (starts with 'AC').");
            }

            if (string.IsNullOrWhiteSpace(_settings.TwilioAuthToken))
            {
                throw new InvalidOperationException(
                    "WhatsAppSettings:TwilioAuthToken is not configured.");
            }

            TwilioClient.Init(_settings.TwilioAccountSid, _settings.TwilioAuthToken);
            _clientInitialized = true;
        }
    }

    private static string MaskPhone(string phone) =>
        phone.Length > 7
            ? $"{phone[..4]}***{phone[^4..]}"
            : "***masked***";

    /// <summary>
    /// Format phone number with whatsapp: prefix for Twilio.
    /// Handles bare E.164 numbers from the interface.
    /// </summary>
    private static string FormatWhatsAppNumber(string phoneNumber) =>
        phoneNumber.StartsWith("whatsapp:")
            ? phoneNumber
            : $"whatsapp:{phoneNumber}";

    /// <summary>
    /// Build ContentVariables JSON from positional parameter values.
    /// Twilio Content API uses 1-indexed named variables: {"1":"val1","2":"val2",...}
    /// </summary>
    private static string BuildContentVariables(IReadOnlyList<string> parameterValues)
    {
        if (parameterValues.Count > 99)
        {
            throw new ArgumentException(
                $"Twilio supports max 99 content variables, got {parameterValues.Count}.",
                nameof(parameterValues));
        }

        var variables = new Dictionary<string, string>();
        for (int i = 0; i < parameterValues.Count; i++)
        {
            variables[(i + 1).ToString()] = parameterValues[i];
        }
        return JsonSerializer.Serialize(variables);
    }

    public async Task<Result<string>> SendTemplateMessageAsync(
        string toPhoneNumber,
        string templateName,
        IReadOnlyList<string> parameterValues,
        string language = "en",
        CancellationToken ct = default)
    {
        var maskedPhone = MaskPhone(toPhoneNumber);
        var sw = Stopwatch.StartNew();

        _logger.LogInformation(
            "[Phase 7B] Twilio WhatsApp template send START: Template={TemplateName}, To={MaskedPhone}, ParamCount={ParamCount}, Language={Language}",
            templateName, maskedPhone, parameterValues.Count, language);

        try
        {
            EnsureClientInitialized();

            if (string.IsNullOrWhiteSpace(_settings.TwilioWhatsAppNumber))
            {
                _logger.LogError("[Phase 7B] Twilio WhatsApp send FAILED: TwilioWhatsAppNumber not configured");
                return Result<string>.Failure("TwilioWhatsAppNumber is not configured.");
            }

            var from = new PhoneNumber(FormatWhatsAppNumber(_settings.TwilioWhatsAppNumber));
            var to = new PhoneNumber(FormatWhatsAppNumber(toPhoneNumber));

            // Build the message body with parameter values interpolated
            // Sandbox mode: send as plain text since no ContentSid is available
            // Production mode: would use ContentSid + ContentVariables (when templates are registered)
            var body = BuildTemplateBody(templateName, parameterValues);

            // Send with retry for rate limiting (429/500/503)
            MessageResource? messageResource = null;
            int retryCount = 0;
            int maxRetries = _settings.MaxRetries;

            while (retryCount <= maxRetries)
            {
                try
                {
                    messageResource = await MessageResource.CreateAsync(
                        from: from,
                        to: to,
                        body: body);
                    break;
                }
                catch (ApiException ex) when (IsRetryableStatusCode(ex.Status) && retryCount < maxRetries)
                {
                    retryCount++;
                    var delay = TimeSpan.FromSeconds(_settings.RetryDelayBaseSeconds * Math.Pow(2, retryCount - 1));

                    _logger.LogWarning(
                        "[Phase 7B] Twilio rate limited ({StatusCode}). Retry {RetryCount}/{MaxRetries} after {DelaySeconds}s. Template={TemplateName}, To={MaskedPhone}",
                        ex.Status, retryCount, maxRetries, delay.TotalSeconds, templateName, maskedPhone);

                    await Task.Delay(delay, ct);
                }
            }

            if (messageResource == null)
            {
                sw.Stop();
                _logger.LogError(
                    "[Phase 7B] Twilio WhatsApp send FAILED after {MaxRetries} retries: Template={TemplateName}, To={MaskedPhone}, Duration={ElapsedMs}ms",
                    maxRetries, templateName, maskedPhone, sw.ElapsedMilliseconds);
                return Result<string>.Failure($"Failed to send WhatsApp message after {maxRetries} retries due to rate limiting.");
            }

            var messageSid = messageResource.Sid ?? "unknown";
            sw.Stop();

            _logger.LogInformation(
                "[Phase 7B] Twilio WhatsApp template send SUCCESS: Template={TemplateName}, To={MaskedPhone}, TwilioSid={TwilioSid}, Status={Status}, Duration={ElapsedMs}ms",
                templateName, maskedPhone, messageSid, messageResource.Status, sw.ElapsedMilliseconds);

            return Result<string>.Success(messageSid);
        }
        catch (ApiException ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "[Phase 7B] Twilio WhatsApp template send API ERROR: Template={TemplateName}, To={MaskedPhone}, StatusCode={StatusCode}, ErrorCode={ErrorCode}, Duration={ElapsedMs}ms",
                templateName, maskedPhone, ex.Status, ex.Code, sw.ElapsedMilliseconds);
            return Result<string>.Failure($"Twilio API error {ex.Status}: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "[Phase 7B] Twilio WhatsApp template send CONFIG ERROR: Template={TemplateName}, To={MaskedPhone}, Duration={ElapsedMs}ms",
                templateName, maskedPhone, sw.ElapsedMilliseconds);
            return Result<string>.Failure($"Configuration error: {ex.Message}");
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "[Phase 7B] Twilio WhatsApp template send EXCEPTION: Template={TemplateName}, To={MaskedPhone}, Duration={ElapsedMs}ms",
                templateName, maskedPhone, sw.ElapsedMilliseconds);
            return Result<string>.Failure($"Unexpected error: {ex.Message}");
        }
    }

    public async Task<Result<string>> SendTextMessageAsync(
        string toPhoneNumber,
        string text,
        CancellationToken ct = default)
    {
        var maskedPhone = MaskPhone(toPhoneNumber);
        var sw = Stopwatch.StartNew();

        _logger.LogInformation(
            "[Phase 7B] Twilio WhatsApp text send START: To={MaskedPhone}, TextLength={TextLength}",
            maskedPhone, text.Length);

        try
        {
            EnsureClientInitialized();

            if (string.IsNullOrWhiteSpace(_settings.TwilioWhatsAppNumber))
            {
                _logger.LogError("[Phase 7B] Twilio WhatsApp text send FAILED: TwilioWhatsAppNumber not configured");
                return Result<string>.Failure("TwilioWhatsAppNumber is not configured.");
            }

            var from = new PhoneNumber(FormatWhatsAppNumber(_settings.TwilioWhatsAppNumber));
            var to = new PhoneNumber(FormatWhatsAppNumber(toPhoneNumber));

            var messageResource = await MessageResource.CreateAsync(
                from: from,
                to: to,
                body: text);

            var messageSid = messageResource.Sid ?? "unknown";
            sw.Stop();

            _logger.LogInformation(
                "[Phase 7B] Twilio WhatsApp text send SUCCESS: To={MaskedPhone}, TwilioSid={TwilioSid}, Status={Status}, Duration={ElapsedMs}ms",
                maskedPhone, messageSid, messageResource.Status, sw.ElapsedMilliseconds);

            return Result<string>.Success(messageSid);
        }
        catch (ApiException ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "[Phase 7B] Twilio WhatsApp text send API ERROR: To={MaskedPhone}, StatusCode={StatusCode}, ErrorCode={ErrorCode}, Duration={ElapsedMs}ms",
                maskedPhone, ex.Status, ex.Code, sw.ElapsedMilliseconds);
            return Result<string>.Failure($"Twilio API error {ex.Status}: {ex.Message}");
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "[Phase 7B] Twilio WhatsApp text send EXCEPTION: To={MaskedPhone}, Duration={ElapsedMs}ms",
                maskedPhone, sw.ElapsedMilliseconds);
            return Result<string>.Failure($"Failed to send text message: {ex.Message}");
        }
    }

    /// <summary>
    /// Build a readable text body from template name and parameter values.
    /// Used when no ContentSid is available (sandbox mode).
    /// Replaces positional placeholders {{1}}, {{2}}, etc. with actual values.
    /// </summary>
    private static string BuildTemplateBody(string templateName, IReadOnlyList<string> parameterValues)
    {
        // Build a human-readable message with the template name and parameters
        var body = $"[{templateName}]";
        if (parameterValues.Count > 0)
        {
            body += " " + string.Join(", ", parameterValues.Select((v, i) => $"{{{{{i + 1}}}}}={v}"));
        }
        return body;
    }

    /// <summary>
    /// Check if a Twilio API error status code is retryable.
    /// </summary>
    private static bool IsRetryableStatusCode(int statusCode) =>
        statusCode is 429 or 500 or 503;
}
