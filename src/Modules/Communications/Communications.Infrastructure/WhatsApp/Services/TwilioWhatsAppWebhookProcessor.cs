using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Modules.Communications.Domain;
using LankaConnect.Modules.Communications.Domain.Entities;
using LankaConnect.Modules.Communications.Infrastructure.WhatsApp.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Web;
namespace LankaConnect.Modules.Communications.Infrastructure.WhatsApp.Services;

/// <summary>
/// Phase 7B: Processes Twilio WhatsApp delivery status webhooks.
/// Validates X-Twilio-Signature, parses form-encoded status callbacks,
/// updates message records, and persists webhook events for audit.
/// </summary>
public class TwilioWhatsAppWebhookProcessor : IWhatsAppWebhookProcessor
{
    private readonly IWhatsAppMessageRepository _messageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly WhatsAppSettings _settings;
    private readonly ILogger<TwilioWhatsAppWebhookProcessor> _logger;

    public TwilioWhatsAppWebhookProcessor(
        IWhatsAppMessageRepository messageRepository,
        IUnitOfWork unitOfWork,
        IOptions<WhatsAppSettings> settings,
        ILogger<TwilioWhatsAppWebhookProcessor> logger)
    {
        _messageRepository = messageRepository;
        _unitOfWork = unitOfWork;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<Result> ProcessDeliveryStatusAsync(string payload, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[Phase 7B] Twilio webhook delivery status processing START. PayloadLength={PayloadLength}",
            payload.Length);

        try
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                _logger.LogWarning("[Phase 7B] Twilio webhook received with empty payload. Ignoring.");
                return Result.Failure("Empty webhook payload.");
            }

            // Parse form-encoded Twilio status callback
            var fields = ParseFormPayload(payload);
            if (fields == null || fields.Count == 0)
            {
                _logger.LogWarning("[Phase 7B] Could not parse Twilio webhook payload.");
                return Result.Failure("Invalid Twilio webhook payload format.");
            }

            var messageSid = fields.GetValueOrDefault("MessageSid");
            var messageStatus = fields.GetValueOrDefault("MessageStatus");
            var errorCode = fields.GetValueOrDefault("ErrorCode");
            var errorMessage = fields.GetValueOrDefault("ErrorMessage");

            _logger.LogInformation(
                "[Phase 7B] Twilio webhook parsed: MessageSid={MessageSid}, Status={Status}, ErrorCode={ErrorCode}",
                messageSid, messageStatus, errorCode);

            // Persist raw webhook event for audit/debugging
            var webhookEvent = WhatsAppWebhookEvent.Create(
                eventType: $"TwilioStatus:{messageStatus}",
                payloadJson: payload,
                acsMessageId: messageSid);

            // Lookup message record by provider message ID (stored in acs_message_id column)
            WhatsAppMessageRecord? messageRecord = null;
            if (!string.IsNullOrWhiteSpace(messageSid))
            {
                messageRecord = await _messageRepository.GetByAcsMessageIdAsync(messageSid, ct);
            }

            if (messageRecord == null)
            {
                _logger.LogWarning(
                    "[Phase 7B] No message record found for TwilioSid={TwilioSid}. Webhook stored but status not updated.",
                    messageSid);

                webhookEvent.MarkProcessed();

                try
                {
                    await _unitOfWork.CommitAsync(ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[Phase 7B] Failed to persist webhook event for unknown TwilioSid={TwilioSid}",
                        messageSid);
                }

                return Result.Success();
            }

            // Map Twilio status to internal status and update message record
            switch (messageStatus?.ToLowerInvariant())
            {
                case "delivered":
                    messageRecord.MarkAsDelivered();
                    _logger.LogInformation(
                        "[Phase 7B] Message {RecordId} marked as DELIVERED. TwilioSid={TwilioSid}",
                        messageRecord.Id, messageSid);
                    break;

                case "read":
                    messageRecord.MarkAsRead();
                    _logger.LogInformation(
                        "[Phase 7B] Message {RecordId} marked as READ. TwilioSid={TwilioSid}",
                        messageRecord.Id, messageSid);
                    break;

                case "failed":
                case "undelivered":
                    var failReason = !string.IsNullOrEmpty(errorMessage)
                        ? $"Twilio error {errorCode}: {errorMessage}"
                        : $"Delivery failed (ErrorCode: {errorCode ?? "unknown"})";
                    messageRecord.MarkAsFailed(failReason);
                    _logger.LogWarning(
                        "[Phase 7B] Message {RecordId} marked as FAILED. TwilioSid={TwilioSid}, ErrorCode={ErrorCode}, Error={Error}",
                        messageRecord.Id, messageSid, errorCode, errorMessage);
                    break;

                case "sent":
                    _logger.LogInformation(
                        "[Phase 7B] Message {RecordId} confirmed SENT. TwilioSid={TwilioSid}",
                        messageRecord.Id, messageSid);
                    break;

                case "queued":
                case "accepted":
                    _logger.LogInformation(
                        "[Phase 7B] Message {RecordId} status '{Status}'. TwilioSid={TwilioSid}",
                        messageRecord.Id, messageStatus, messageSid);
                    break;

                default:
                    _logger.LogInformation(
                        "[Phase 7B] Unhandled Twilio status '{Status}' for TwilioSid={TwilioSid}. Recorded but no state change.",
                        messageStatus, messageSid);
                    break;
            }

            webhookEvent.MarkProcessed();

            try
            {
                await _unitOfWork.CommitAsync(ct);
                _logger.LogInformation(
                    "[Phase 7B] Twilio webhook processing COMPLETE. TwilioSid={TwilioSid}, Status={Status}",
                    messageSid, messageStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Phase 7B] Failed to persist Twilio webhook status update for TwilioSid={TwilioSid}",
                    messageSid);
                return Result.Failure($"Failed to persist delivery status: {ex.Message}");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Phase 7B] Twilio webhook processing EXCEPTION. PayloadLength={PayloadLength}",
                payload.Length);
            return Result.Failure($"Webhook processing failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Validate Twilio webhook signature using HMAC-SHA1.
    /// Call this from the controller before ProcessDeliveryStatusAsync.
    /// </summary>
    public bool ValidateSignature(string url, Dictionary<string, string> formData, string signature)
    {
        if (string.IsNullOrWhiteSpace(_settings.TwilioAuthToken))
        {
            _logger.LogWarning("[Phase 7B] TwilioAuthToken not configured. Signature validation skipped.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(signature))
        {
            _logger.LogWarning("[Phase 7B] Missing X-Twilio-Signature header.");
            return false;
        }

        // Twilio signature validation:
        // 1. Start with the full URL
        // 2. Sort POST parameters alphabetically by key
        // 3. Append each key and value (no separator)
        // 4. HMAC-SHA1 hash with auth token, Base64 encode
        var builder = new StringBuilder(url);
        foreach (var kvp in formData.OrderBy(k => k.Key))
        {
            builder.Append(kvp.Key);
            builder.Append(kvp.Value);
        }

        var dataToSign = builder.ToString();
        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(_settings.TwilioAuthToken));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataToSign));
        var computedSignature = Convert.ToBase64String(hash);

        var isValid = string.Equals(computedSignature, signature, StringComparison.Ordinal);

        if (!isValid)
        {
            _logger.LogWarning("[Phase 7B] Twilio signature validation FAILED.");
        }

        return isValid;
    }

    /// <summary>
    /// Parse form-encoded payload into key-value pairs.
    /// Twilio sends status callbacks as application/x-www-form-urlencoded.
    /// </summary>
    private static Dictionary<string, string>? ParseFormPayload(string payload)
    {
        try
        {
            var result = new Dictionary<string, string>();
            var pairs = payload.Split('&', StringSplitOptions.RemoveEmptyEntries);

            foreach (var pair in pairs)
            {
                var parts = pair.Split('=', 2);
                if (parts.Length == 2)
                {
                    var key = HttpUtility.UrlDecode(parts[0]);
                    var value = HttpUtility.UrlDecode(parts[1]);
                    result[key] = value;
                }
            }

            return result.Count > 0 ? result : null;
        }
        catch
        {
            return null;
        }
    }
}
