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
namespace LankaConnect.Modules.Communications.Infrastructure.WhatsApp.Services;

/// <summary>
/// Phase 7B: Twilio WhatsApp send strategy.
/// Phase 7B.4: wires Twilio Content API via ContentSid + ContentVariables.
/// Sandbox mode (<see cref="WhatsAppSettings.SandboxMode"/>) allows dev-only fallback
/// to a plain-text debug body when no ContentSid is supplied — never in production.
/// Mirrors AcsWhatsAppStrategy patterns (retry, masking, structured logging).
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

    private static string FormatWhatsAppNumber(string phoneNumber) =>
        phoneNumber.StartsWith("whatsapp:")
            ? phoneNumber
            : $"whatsapp:{phoneNumber}";

    /// <summary>
    /// Build ContentVariables JSON from positional parameter values.
    /// Twilio Content API uses 1-indexed named variables: {"1":"val1","2":"val2",...}.
    /// Internal so unit tests can verify 1-indexing and quote escaping (T4).
    /// Precondition: caller has already guarded <c>parameterValues.Count &lt;= 99</c>.
    /// </summary>
    internal static string BuildContentVariables(IReadOnlyList<string> parameterValues)
    {
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
        string? providerTemplateId = null,
        CancellationToken ct = default)
    {
        var maskedPhone = MaskPhone(toPhoneNumber);
        var sw = Stopwatch.StartNew();

        _logger.LogInformation(
            "[Phase 7B] Twilio WhatsApp template send START: Template={TemplateName}, To={MaskedPhone}, ParamCount={ParamCount}, Language={Language}, HasContentSid={HasContentSid}, SandboxMode={SandboxMode}",
            templateName, maskedPhone, parameterValues.Count, language,
            !string.IsNullOrEmpty(providerTemplateId), _settings.SandboxMode);

        // Phase 7B.4 T5: pre-SDK guard — >99 params returns Failure, never throws.
        if (parameterValues.Count > 99)
        {
            sw.Stop();
            _logger.LogError(
                "[Phase 7B.4] Twilio WhatsApp send REJECTED: Template={TemplateName}, ParamCount={ParamCount} exceeds Twilio max 99",
                templateName, parameterValues.Count);
            return Result<string>.Failure(
                $"Twilio Content API supports max 99 content variables, got {parameterValues.Count} for template '{templateName}'.");
        }

        // Phase 7B.4 T2: pre-SDK guard — require ContentSid in production.
        // SandboxMode=true is dev-only and must never be enabled in production env.
        var hasContentSid = !string.IsNullOrWhiteSpace(providerTemplateId);
        if (!hasContentSid && !_settings.SandboxMode)
        {
            sw.Stop();
            _logger.LogError(
                "[Phase 7B.4] Twilio WhatsApp send REJECTED: Template={TemplateName} has no TwilioContentSid and SandboxMode=false. " +
                "Seed the Twilio ContentSid via TwilioTemplateSeeder or enable SandboxMode for local development.",
                templateName);
            return Result<string>.Failure(
                $"Cannot send template '{templateName}': TwilioContentSid is missing and SandboxMode is disabled. " +
                "Configure WhatsAppSettings:TwilioContentSids or enable SandboxMode for dev.");
        }

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

            // Send with retry for rate limiting (429/500/503)
            MessageResource? messageResource = null;
            int retryCount = 0;
            int maxRetries = _settings.MaxRetries;

            while (retryCount <= maxRetries)
            {
                try
                {
                    if (hasContentSid)
                    {
                        // Production path: Twilio Content API with ContentSid + ContentVariables.
                        var contentVariables = BuildContentVariables(parameterValues);
                        messageResource = await MessageResource.CreateAsync(
                            to: to,
                            from: from,
                            contentSid: providerTemplateId,
                            contentVariables: contentVariables);
                    }
                    else
                    {
                        // SandboxMode=true ONLY: debug text fallback. Never reached in prod
                        // because the pre-SDK guard above returns Failure first.
                        var debugBody = BuildSandboxDebugBody(templateName, parameterValues);
                        messageResource = await MessageResource.CreateAsync(
                            from: from,
                            to: to,
                            body: debugBody);
                    }
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
                "[Phase 7B] Twilio WhatsApp template send SUCCESS: Template={TemplateName}, To={MaskedPhone}, TwilioSid={TwilioSid}, ContentSid={ContentSid}, Status={Status}, Duration={ElapsedMs}ms",
                templateName, maskedPhone, messageSid, providerTemplateId ?? "(none)", messageResource.Status, sw.ElapsedMilliseconds);

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
    /// Sandbox-only debug body for local development. Emits template name + positional
    /// placeholders so devs can verify wiring without Content API registration.
    /// NEVER reachable when <see cref="WhatsAppSettings.SandboxMode"/> is false.
    /// </summary>
    private static string BuildSandboxDebugBody(string templateName, IReadOnlyList<string> parameterValues)
    {
        var body = $"[SANDBOX][{templateName}]";
        if (parameterValues.Count > 0)
        {
            body += " " + string.Join(", ", parameterValues.Select((v, i) => $"{{{{{i + 1}}}}}={v}"));
        }
        return body;
    }

    private static bool IsRetryableStatusCode(int statusCode) =>
        statusCode is 429 or 500 or 503;
}
