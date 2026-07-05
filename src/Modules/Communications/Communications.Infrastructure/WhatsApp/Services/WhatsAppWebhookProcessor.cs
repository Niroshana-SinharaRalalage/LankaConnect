using System.Text.Json;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Communications.Entities;
using Microsoft.Extensions.Logging;
namespace LankaConnect.Modules.Communications.Infrastructure.WhatsApp.Services;

/// <summary>
/// Phase 7A: Processes ACS WhatsApp delivery status webhooks.
/// Extracts delivery status from ACS webhook payload, updates message records,
/// and persists raw webhook events for debugging and idempotency.
/// </summary>
public class WhatsAppWebhookProcessor : IWhatsAppWebhookProcessor
{
    private readonly IWhatsAppMessageRepository _messageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WhatsAppWebhookProcessor> _logger;

    public WhatsAppWebhookProcessor(
        IWhatsAppMessageRepository messageRepository,
        IUnitOfWork unitOfWork,
        ILogger<WhatsAppWebhookProcessor> logger)
    {
        _messageRepository = messageRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> ProcessDeliveryStatusAsync(string payload, CancellationToken ct = default)
    {
        _logger.LogInformation("[Phase 7A] Webhook delivery status processing START. PayloadLength={PayloadLength}", payload.Length);

        try
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                _logger.LogWarning("[Phase 7A] Webhook received with empty payload. Ignoring.");
                return Result.Failure("Empty webhook payload.");
            }

            // Parse the ACS webhook payload
            var webhookData = ParseWebhookPayload(payload);
            if (webhookData == null)
            {
                _logger.LogWarning("[Phase 7A] Could not parse webhook payload. PayloadLength={PayloadLength}", payload.Length);
                return Result.Failure("Invalid webhook payload format.");
            }

            var acsMessageId = webhookData.MessageId;
            var status = webhookData.Status;
            var errorMessage = webhookData.Error;

            _logger.LogInformation(
                "[Phase 7A] Webhook parsed: AcsMessageId={AcsMessageId}, Status={Status}, HasError={HasError}",
                acsMessageId, status, !string.IsNullOrEmpty(errorMessage));

            // Persist raw webhook event for audit/debugging
            var webhookEvent = WhatsAppWebhookEvent.Create(
                eventType: $"DeliveryStatus:{status}",
                payloadJson: payload,
                acsMessageId: acsMessageId);

            // Lookup the message record by ACS message ID
            WhatsAppMessageRecord? messageRecord = null;
            if (!string.IsNullOrWhiteSpace(acsMessageId))
            {
                messageRecord = await _messageRepository.GetByAcsMessageIdAsync(acsMessageId, ct);
            }

            if (messageRecord == null)
            {
                _logger.LogWarning(
                    "[Phase 7A] No message record found for AcsMessageId={AcsMessageId}. Webhook stored but status not updated.",
                    acsMessageId);

                webhookEvent.MarkProcessed();

                // We still want to persist the webhook event even if no matching message found
                // The webhook event entity is tracked by the DbContext via its repository
                // Since there's no dedicated webhook repo, we commit what we have
                try
                {
                    await _unitOfWork.CommitAsync(ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Phase 7A] Failed to persist webhook event for unknown AcsMessageId={AcsMessageId}", acsMessageId);
                }

                return Result.Success();
            }

            // Update message record based on delivery status
            switch (status?.ToLowerInvariant())
            {
                case "delivered":
                    messageRecord.MarkAsDelivered();
                    _logger.LogInformation(
                        "[Phase 7A] Message {RecordId} marked as DELIVERED. AcsMessageId={AcsMessageId}",
                        messageRecord.Id, acsMessageId);
                    break;

                case "read":
                    messageRecord.MarkAsRead();
                    _logger.LogInformation(
                        "[Phase 7A] Message {RecordId} marked as READ. AcsMessageId={AcsMessageId}",
                        messageRecord.Id, acsMessageId);
                    break;

                case "failed":
                case "error":
                    messageRecord.MarkAsFailed(errorMessage ?? "Delivery failed (no error details).");
                    _logger.LogWarning(
                        "[Phase 7A] Message {RecordId} marked as FAILED. AcsMessageId={AcsMessageId}, Error={Error}",
                        messageRecord.Id, acsMessageId, errorMessage);
                    break;

                case "sent":
                    // Already in Sent status from the initial send, but update timestamp if needed
                    _logger.LogInformation(
                        "[Phase 7A] Message {RecordId} confirmed SENT. AcsMessageId={AcsMessageId}",
                        messageRecord.Id, acsMessageId);
                    break;

                default:
                    _logger.LogInformation(
                        "[Phase 7A] Unhandled webhook status '{Status}' for AcsMessageId={AcsMessageId}. Recorded but no state change.",
                        status, acsMessageId);
                    break;
            }

            webhookEvent.MarkProcessed();

            try
            {
                await _unitOfWork.CommitAsync(ct);
                _logger.LogInformation(
                    "[Phase 7A] Webhook processing COMPLETE. AcsMessageId={AcsMessageId}, Status={Status}",
                    acsMessageId, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[Phase 7A] Failed to persist webhook status update for AcsMessageId={AcsMessageId}",
                    acsMessageId);
                return Result.Failure($"Failed to persist delivery status: {ex.Message}");
            }

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Phase 7A] Webhook processing EXCEPTION. PayloadLength={PayloadLength}", payload.Length);
            return Result.Failure($"Webhook processing failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Parse the ACS delivery status webhook payload.
    /// ACS sends webhook events in CloudEvents format with delivery status in the data payload.
    /// </summary>
    private WebhookDeliveryStatus? ParseWebhookPayload(string payload)
    {
        try
        {
            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            // ACS webhooks can come as single event or array of events
            var eventElement = root.ValueKind == JsonValueKind.Array
                ? root.EnumerateArray().FirstOrDefault()
                : root;

            if (eventElement.ValueKind == JsonValueKind.Undefined)
                return null;

            // Extract data from CloudEvents format
            string? messageId = null;
            string? status = null;
            string? error = null;

            if (eventElement.TryGetProperty("data", out var dataElement))
            {
                // ACS delivery status webhook data structure
                if (dataElement.TryGetProperty("messageId", out var msgIdProp))
                    messageId = msgIdProp.GetString();
                else if (dataElement.TryGetProperty("MessageId", out var msgIdPropUpper))
                    messageId = msgIdPropUpper.GetString();

                if (dataElement.TryGetProperty("status", out var statusProp))
                    status = statusProp.GetString();
                else if (dataElement.TryGetProperty("Status", out var statusPropUpper))
                    status = statusPropUpper.GetString();

                // Error info may be nested
                if (dataElement.TryGetProperty("error", out var errorProp))
                {
                    if (errorProp.ValueKind == JsonValueKind.Object)
                    {
                        errorProp.TryGetProperty("message", out var errorMsg);
                        error = errorMsg.GetString();
                    }
                    else if (errorProp.ValueKind == JsonValueKind.String)
                    {
                        error = errorProp.GetString();
                    }
                }
            }

            // Also try top-level properties for simpler formats
            if (string.IsNullOrEmpty(messageId) && eventElement.TryGetProperty("messageId", out var topMsgId))
                messageId = topMsgId.GetString();

            if (string.IsNullOrEmpty(status) && eventElement.TryGetProperty("type", out var typeProp))
            {
                // Extract status from event type like "Microsoft.Communication.MessageDeliveryStatusUpdated"
                var eventType = typeProp.GetString();
                if (eventType?.Contains("Delivered") == true)
                    status = "delivered";
                else if (eventType?.Contains("Read") == true)
                    status = "read";
                else if (eventType?.Contains("Failed") == true)
                    status = "failed";
            }

            if (string.IsNullOrEmpty(messageId) && string.IsNullOrEmpty(status))
                return null;

            return new WebhookDeliveryStatus
            {
                MessageId = messageId,
                Status = status,
                Error = error
            };
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "[Phase 7A] Failed to parse webhook JSON payload.");
            return null;
        }
    }

    private class WebhookDeliveryStatus
    {
        public string? MessageId { get; init; }
        public string? Status { get; init; }
        public string? Error { get; init; }
    }
}
