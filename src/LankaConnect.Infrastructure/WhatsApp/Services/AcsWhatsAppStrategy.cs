using Azure;
using Azure.Communication.Messages;
using Azure.Communication.Messages.Models.Channels;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Infrastructure.WhatsApp.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace LankaConnect.Infrastructure.WhatsApp.Services;

/// <summary>
/// Phase 7A: Azure Communication Services WhatsApp send strategy.
/// Handles low-level ACS SDK calls with retry logic, rate limiting, and structured logging.
/// Lazy client construction per architect review FIX C2.
/// </summary>
public class AcsWhatsAppStrategy : IWhatsAppSendStrategy
{
    private readonly WhatsAppSettings _settings;
    private readonly ILogger<AcsWhatsAppStrategy> _logger;
    private NotificationMessagesClient? _client;
    private readonly object _clientLock = new();

    public AcsWhatsAppStrategy(
        IOptions<WhatsAppSettings> settings,
        ILogger<AcsWhatsAppStrategy> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    private NotificationMessagesClient GetClient()
    {
        if (_client != null) return _client;
        lock (_clientLock)
        {
            if (_client != null) return _client;

            if (string.IsNullOrWhiteSpace(_settings.ConnectionString))
            {
                throw new InvalidOperationException(
                    "WhatsAppSettings:ConnectionString is not configured. " +
                    "Set it to the Azure Communication Services connection string.");
            }

            _client = new NotificationMessagesClient(_settings.ConnectionString);
        }
        return _client;
    }

    private static string MaskPhone(string phone) =>
        phone.Length > 7
            ? $"{phone[..4]}***{phone[^4..]}"
            : "***masked***";

    /// <param name="providerTemplateId">
    /// Phase 7B.4: accepted for interface symmetry with <see cref="TwilioWhatsAppStrategy"/>.
    /// ACS identifies templates by name (via <paramref name="templateName"/>) and does not use
    /// this parameter — it is ignored.
    /// </param>
    public async Task<Result<string>> SendTemplateMessageAsync(
        string toPhoneNumber,
        string templateName,
        IReadOnlyList<string> parameterValues,
        string language = "en",
        string? providerTemplateId = null,
        CancellationToken ct = default)
    {
        _ = providerTemplateId; // Intentionally unused — ACS doesn't need provider template ID.
        var maskedPhone = MaskPhone(toPhoneNumber);
        var sw = Stopwatch.StartNew();

        _logger.LogInformation(
            "[Phase 7A] WhatsApp template send START: Template={TemplateName}, To={MaskedPhone}, ParamCount={ParamCount}, Language={Language}",
            templateName, maskedPhone, parameterValues.Count, language);

        try
        {
            var client = GetClient();

            if (!Guid.TryParse(_settings.ChannelRegistrationId, out var channelRegistrationId))
            {
                _logger.LogError(
                    "[Phase 7A] WhatsApp send FAILED: Invalid ChannelRegistrationId={ChannelRegistrationId}",
                    _settings.ChannelRegistrationId);
                return Result<string>.Failure("Invalid ChannelRegistrationId configuration.");
            }

            var recipientList = new List<string> { toPhoneNumber };

            // Build template values (MessageTemplateText) and WhatsApp bindings
            var templateValues = new List<MessageTemplateValue>();
            var bodyBindings = new List<WhatsAppMessageTemplateBindingsComponent>();

            for (int i = 0; i < parameterValues.Count; i++)
            {
                var refName = $"param_{i}";
                templateValues.Add(new MessageTemplateText(refName, parameterValues[i]));
                bodyBindings.Add(new WhatsAppMessageTemplateBindingsComponent(refName));
            }

            var bindings = new WhatsAppMessageTemplateBindings();
            foreach (var binding in bodyBindings)
            {
                bindings.Body.Add(binding);
            }

            var template = new MessageTemplate(templateName, language);
            foreach (var value in templateValues)
            {
                template.Values.Add(value);
            }
            template.Bindings = bindings;

            var templateMessage = new TemplateNotificationContent(channelRegistrationId, recipientList, template);

            // Send with retry for 429 rate limiting
            SendMessageResult? sendResult = null;
            int retryCount = 0;
            int maxRetries = _settings.MaxRetries;

            while (retryCount <= maxRetries)
            {
                try
                {
                    var response = await client.SendAsync(templateMessage, ct);
                    sendResult = response.Value;
                    break;
                }
                catch (RequestFailedException ex) when (ex.Status == 429 && retryCount < maxRetries)
                {
                    retryCount++;
                    var delay = TimeSpan.FromSeconds(_settings.RetryDelayBaseSeconds * Math.Pow(2, retryCount - 1));

                    _logger.LogWarning(
                        "[Phase 7A] WhatsApp 429 rate limited. Retry {RetryCount}/{MaxRetries} after {DelaySeconds}s. Template={TemplateName}, To={MaskedPhone}",
                        retryCount, maxRetries, delay.TotalSeconds, templateName, maskedPhone);

                    await Task.Delay(delay, ct);
                }
            }

            if (sendResult == null)
            {
                sw.Stop();
                _logger.LogError(
                    "[Phase 7A] WhatsApp send FAILED after {MaxRetries} retries: Template={TemplateName}, To={MaskedPhone}, Duration={ElapsedMs}ms",
                    maxRetries, templateName, maskedPhone, sw.ElapsedMilliseconds);
                return Result<string>.Failure($"Failed to send WhatsApp message after {maxRetries} retries due to rate limiting.");
            }

            var messageId = sendResult.Receipts.FirstOrDefault()?.MessageId ?? "unknown";
            sw.Stop();

            _logger.LogInformation(
                "[Phase 7A] WhatsApp template send SUCCESS: Template={TemplateName}, To={MaskedPhone}, AcsMessageId={AcsMessageId}, Duration={ElapsedMs}ms",
                templateName, maskedPhone, messageId, sw.ElapsedMilliseconds);

            return Result<string>.Success(messageId);
        }
        catch (RequestFailedException ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "[Phase 7A] WhatsApp template send ACS ERROR: Template={TemplateName}, To={MaskedPhone}, StatusCode={StatusCode}, ErrorCode={ErrorCode}, Duration={ElapsedMs}ms",
                templateName, maskedPhone, ex.Status, ex.ErrorCode, sw.ElapsedMilliseconds);
            return Result<string>.Failure($"ACS error {ex.Status}: {ex.Message}");
        }
        catch (InvalidOperationException ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "[Phase 7A] WhatsApp template send CONFIG ERROR: Template={TemplateName}, To={MaskedPhone}, Duration={ElapsedMs}ms",
                templateName, maskedPhone, sw.ElapsedMilliseconds);
            return Result<string>.Failure($"Configuration error: {ex.Message}");
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "[Phase 7A] WhatsApp template send EXCEPTION: Template={TemplateName}, To={MaskedPhone}, Duration={ElapsedMs}ms",
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
            "[Phase 7A] WhatsApp text send START: To={MaskedPhone}, TextLength={TextLength}",
            maskedPhone, text.Length);

        try
        {
            var client = GetClient();

            if (!Guid.TryParse(_settings.ChannelRegistrationId, out var channelRegistrationId))
            {
                _logger.LogError(
                    "[Phase 7A] WhatsApp text send FAILED: Invalid ChannelRegistrationId={ChannelRegistrationId}",
                    _settings.ChannelRegistrationId);
                return Result<string>.Failure("Invalid ChannelRegistrationId configuration.");
            }

            var recipientList = new List<string> { toPhoneNumber };
            var textContent = new TextNotificationContent(channelRegistrationId, recipientList, text);

            var response = await client.SendAsync(textContent, ct);
            var messageId = response.Value.Receipts.FirstOrDefault()?.MessageId ?? "unknown";

            sw.Stop();
            _logger.LogInformation(
                "[Phase 7A] WhatsApp text send SUCCESS: To={MaskedPhone}, AcsMessageId={AcsMessageId}, Duration={ElapsedMs}ms",
                maskedPhone, messageId, sw.ElapsedMilliseconds);

            return Result<string>.Success(messageId);
        }
        catch (RequestFailedException ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "[Phase 7A] WhatsApp text send ACS ERROR: To={MaskedPhone}, StatusCode={StatusCode}, ErrorCode={ErrorCode}, Duration={ElapsedMs}ms",
                maskedPhone, ex.Status, ex.ErrorCode, sw.ElapsedMilliseconds);
            return Result<string>.Failure($"ACS error {ex.Status}: {ex.Message}");
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "[Phase 7A] WhatsApp text send EXCEPTION: To={MaskedPhone}, Duration={ElapsedMs}ms",
                maskedPhone, sw.ElapsedMilliseconds);
            return Result<string>.Failure($"Failed to send text message: {ex.Message}");
        }
    }
}
