using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Infrastructure.WhatsApp.Services;

namespace LankaConnect.API.Controllers;

/// <summary>
/// Phase 7A/7B: Webhook endpoints for WhatsApp delivery status callbacks.
/// Supports both ACS (Event Grid CloudEvents) and Twilio (HMAC-SHA1 form-encoded).
/// AllowAnonymous because provider systems call these endpoints directly.
/// </summary>
[ApiController]
[Route("api/webhooks/whatsapp")]
[AllowAnonymous]
public class WhatsAppWebhookController : ControllerBase
{
    private readonly IWhatsAppWebhookProcessor _processor;
    private readonly TwilioWhatsAppWebhookProcessor _twilioProcessor;
    private readonly ILogger<WhatsAppWebhookController> _logger;

    public WhatsAppWebhookController(
        IWhatsAppWebhookProcessor processor,
        TwilioWhatsAppWebhookProcessor twilioProcessor,
        ILogger<WhatsAppWebhookController> logger)
    {
        _processor = processor;
        _twilioProcessor = twilioProcessor;
        _logger = logger;
    }

    /// <summary>
    /// Handle WhatsApp delivery status webhook from Azure Event Grid.
    /// Supports Event Grid subscription validation handshake and delivery status processing.
    /// </summary>
    /// <param name="payload">Raw JSON payload from Event Grid</param>
    /// <param name="eventType">Event Grid event type header (aeg-event-type)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>200 OK on success, 500 on processing failure</returns>
    [HttpPost("status")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> HandleDeliveryStatus(
        [FromBody] JsonElement payload,
        [FromHeader(Name = "aeg-event-type")] string? eventType,
        CancellationToken ct)
    {
        try
        {
            // Step 1: Handle Azure Event Grid subscription validation handshake.
            // When registering a webhook, Event Grid sends a SubscriptionValidation event
            // that we must echo back the validationCode to confirm ownership.
            if (string.Equals(eventType, "SubscriptionValidation", StringComparison.OrdinalIgnoreCase))
            {
                return HandleSubscriptionValidation(payload);
            }

            // Step 2: Process delivery status update via the webhook processor.
            // This handles Sent, Delivered, Read, and Failed status callbacks.
            var rawPayload = payload.GetRawText();

            _logger.LogInformation(
                "[Phase 7A] Processing WhatsApp delivery status webhook - EventType={EventType}, PayloadLength={PayloadLength}",
                eventType, rawPayload.Length);

            var result = await _processor.ProcessDeliveryStatusAsync(rawPayload, ct);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "[Phase 7A] WhatsApp delivery status webhook processed successfully");
                return Ok();
            }

            _logger.LogWarning(
                "[Phase 7A] WhatsApp delivery status webhook processing failed: {Error}",
                result.Errors.FirstOrDefault());

            // Return 200 even on processing failure to prevent Event Grid retries
            // that could cause duplicate processing. Log the error for investigation.
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Phase 7A] Unhandled exception processing WhatsApp delivery status webhook");

            // Return 500 only for truly unexpected errors so Event Grid can retry
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Phase 7B: Handle WhatsApp delivery status webhook from Twilio.
    /// Twilio sends form-encoded POST with X-Twilio-Signature for validation.
    /// Separate endpoint because Twilio uses different auth/payload format than ACS.
    /// </summary>
    [HttpPost("twilio-status")]
    [Consumes("application/x-www-form-urlencoded")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> HandleTwilioDeliveryStatus(
        [FromForm] Dictionary<string, string> formData,
        [FromHeader(Name = "X-Twilio-Signature")] string? twilioSignature,
        CancellationToken ct)
    {
        try
        {
            _logger.LogInformation(
                "[Phase 7B] Processing Twilio WhatsApp delivery status webhook - FieldCount={FieldCount}",
                formData.Count);

            // Validate Twilio signature
            var requestUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}";
            if (!string.IsNullOrWhiteSpace(twilioSignature))
            {
                var isValid = _twilioProcessor.ValidateSignature(requestUrl, formData, twilioSignature);
                if (!isValid)
                {
                    _logger.LogWarning("[Phase 7B] Twilio signature validation failed. Rejecting webhook.");
                    return Forbid();
                }
            }
            else
            {
                _logger.LogWarning("[Phase 7B] No X-Twilio-Signature header. Processing anyway (staging mode).");
            }

            // Convert form data to a payload string for the processor
            var payload = string.Join("&",
                formData.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

            var result = await _twilioProcessor.ProcessDeliveryStatusAsync(payload, ct);

            if (result.IsSuccess)
            {
                _logger.LogInformation("[Phase 7B] Twilio webhook processed successfully");
            }
            else
            {
                _logger.LogWarning(
                    "[Phase 7B] Twilio webhook processing failed: {Error}",
                    result.Errors.FirstOrDefault());
            }

            // Always return 200 to prevent Twilio retries
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Phase 7B] Unhandled exception processing Twilio delivery status webhook");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Handles the Event Grid subscription validation handshake.
    /// Returns the validation code to confirm webhook endpoint ownership.
    /// </summary>
    private IActionResult HandleSubscriptionValidation(JsonElement payload)
    {
        try
        {
            // Event Grid sends an array of events for validation
            // The validation event data is either at root level or in an array
            string? validationCode = null;

            if (payload.ValueKind == JsonValueKind.Array)
            {
                var firstEvent = payload.EnumerateArray().FirstOrDefault();
                if (firstEvent.TryGetProperty("data", out var data) &&
                    data.TryGetProperty("validationCode", out var code))
                {
                    validationCode = code.GetString();
                }
            }
            else if (payload.TryGetProperty("data", out var data) &&
                     data.TryGetProperty("validationCode", out var code))
            {
                validationCode = code.GetString();
            }

            if (string.IsNullOrEmpty(validationCode))
            {
                _logger.LogWarning(
                    "[Phase 7A] Event Grid subscription validation: validationCode not found in payload");
                return BadRequest(new ProblemDetails
                {
                    Title = "Bad Request",
                    Detail = "Validation code not found in Event Grid subscription validation payload",
                    Status = 400
                });
            }

            _logger.LogInformation(
                "[Phase 7A] Event Grid subscription validation successful");

            return Ok(new { validationResponse = validationCode });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Phase 7A] Error processing Event Grid subscription validation");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
