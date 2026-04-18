using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Infrastructure.WhatsApp.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LankaConnect.Infrastructure.WhatsApp.Services;

/// <summary>
/// Phase 7A: Application-level WhatsApp service implementation.
/// Orchestrates feature flag checks, preference validation, deduplication,
/// template lookup, ACS send, and message record persistence.
/// </summary>
public class WhatsAppService : IWhatsAppService
{
    private readonly IWhatsAppSendStrategy _sendStrategy;
    private readonly IWhatsAppMessageRepository _messageRepository;
    private readonly IWhatsAppTemplateRepository _templateRepository;
    private readonly IUserWhatsAppPreferencesRepository _preferencesRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly WhatsAppSettings _settings;
    private readonly ILogger<WhatsAppService> _logger;

    /// <summary>Deduplication window in minutes.</summary>
    private const int DeduplicationWindowMinutes = 5;

    public WhatsAppService(
        IWhatsAppSendStrategy sendStrategy,
        IWhatsAppMessageRepository messageRepository,
        IWhatsAppTemplateRepository templateRepository,
        IUserWhatsAppPreferencesRepository preferencesRepository,
        IUnitOfWork unitOfWork,
        IOptions<WhatsAppSettings> settings,
        ILogger<WhatsAppService> logger)
    {
        _sendStrategy = sendStrategy;
        _messageRepository = messageRepository;
        _templateRepository = templateRepository;
        _preferencesRepository = preferencesRepository;
        _unitOfWork = unitOfWork;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<Result<WhatsAppSendResult>> SendTemplateMessageAsync(
        Guid userId,
        string templateName,
        Dictionary<string, string> parameters,
        WhatsAppNotificationType notificationType,
        Guid? eventId = null,
        Guid? registrationId = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[Phase 7A] SendTemplateMessage START: UserId={UserId}, Template={TemplateName}, NotificationType={NotificationType}, EventId={EventId}",
            userId, templateName, notificationType, eventId);

        try
        {
            // Step 1: Check global feature flag
            if (!_settings.Enabled)
            {
                _logger.LogInformation("[Phase 7A] WhatsApp DISABLED globally. Skipping send for UserId={UserId}", userId);
                return Result<WhatsAppSendResult>.Success(
                    WhatsAppSendResult.Skipped("WhatsApp messaging is disabled globally."));
            }

            // Step 2: Lookup user preferences and validate opt-in
            var preferences = await _preferencesRepository.GetByUserIdAsync(userId, ct);
            if (preferences == null)
            {
                _logger.LogInformation("[Phase 7A] No WhatsApp preferences for UserId={UserId}. Skipping.", userId);
                return Result<WhatsAppSendResult>.Success(
                    WhatsAppSendResult.Skipped("User has no WhatsApp preferences configured."));
            }

            if (!preferences.ShouldNotify(notificationType))
            {
                _logger.LogInformation(
                    "[Phase 7A] User {UserId} opted out of {NotificationType}. Skipping.",
                    userId, notificationType);
                return Result<WhatsAppSendResult>.Success(
                    WhatsAppSendResult.Skipped($"User opted out of {notificationType} notifications."));
            }

            if (string.IsNullOrWhiteSpace(preferences.WhatsAppPhoneNumber))
            {
                _logger.LogWarning("[Phase 7A] User {UserId} has no verified phone number. Skipping.", userId);
                return Result<WhatsAppSendResult>.Success(
                    WhatsAppSendResult.Skipped("User has no verified WhatsApp phone number."));
            }

            // Step 3: Check deduplication (FIX E2)
            var isDuplicate = await _messageRepository.HasRecentDuplicateAsync(
                userId, eventId, templateName, DeduplicationWindowMinutes, ct);
            if (isDuplicate)
            {
                _logger.LogInformation(
                    "[Phase 7A] Duplicate detected within {Minutes}m: UserId={UserId}, Template={TemplateName}, EventId={EventId}. Skipping.",
                    DeduplicationWindowMinutes, userId, templateName, eventId);
                return Result<WhatsAppSendResult>.Success(
                    WhatsAppSendResult.Skipped($"Duplicate message detected within {DeduplicationWindowMinutes} minutes."));
            }

            // Step 4-6: Lookup template, build params, send
            return await SendViaTemplateAsync(
                preferences.WhatsAppPhoneNumber,
                templateName,
                parameters,
                preferences.PreferredLanguage,
                userId,
                eventId,
                registrationId,
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Phase 7A] SendTemplateMessage EXCEPTION: UserId={UserId}, Template={TemplateName}",
                userId, templateName);
            return Result<WhatsAppSendResult>.Failure($"Unexpected error sending WhatsApp message: {ex.Message}");
        }
    }

    public async Task<Result<WhatsAppSendResult>> SendTemplateMessageToPhoneAsync(
        string phoneNumber,
        string templateName,
        Dictionary<string, string> parameters,
        Guid? eventId = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[Phase 7A] SendTemplateMessageToPhone START: Template={TemplateName}, EventId={EventId}",
            templateName, eventId);

        try
        {
            // Step 1: Check global feature flag
            if (!_settings.Enabled)
            {
                _logger.LogInformation("[Phase 7A] WhatsApp DISABLED globally. Skipping direct phone send.");
                return Result<WhatsAppSendResult>.Success(
                    WhatsAppSendResult.Skipped("WhatsApp messaging is disabled globally."));
            }

            // No user preference check for direct phone sends (anonymous/unregistered)
            return await SendViaTemplateAsync(
                phoneNumber,
                templateName,
                parameters,
                "en",
                userId: null,
                eventId: eventId,
                registrationId: null,
                ct: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Phase 7A] SendTemplateMessageToPhone EXCEPTION: Template={TemplateName}",
                templateName);
            return Result<WhatsAppSendResult>.Failure($"Unexpected error sending WhatsApp message: {ex.Message}");
        }
    }

    public async Task<Result<int>> BroadcastToEventAttendeesAsync(
        Guid eventId,
        string templateName,
        Dictionary<string, string> parameters,
        WhatsAppNotificationType notificationType,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[Phase 7A] BroadcastToEventAttendees START: EventId={EventId}, Template={TemplateName}, NotificationType={NotificationType}",
            eventId, templateName, notificationType);

        try
        {
            // Step 1: Check global feature flag
            if (!_settings.Enabled)
            {
                _logger.LogInformation("[Phase 7A] WhatsApp DISABLED globally. Skipping broadcast for EventId={EventId}", eventId);
                return Result<int>.Success(0);
            }

            // Step 2: Get all users opted in for this notification type
            var optedInUsers = await _preferencesRepository.GetUsersOptedInForNotificationTypeAsync(notificationType, ct);

            if (optedInUsers.Count == 0)
            {
                _logger.LogInformation(
                    "[Phase 7A] No opted-in users for {NotificationType}. Broadcast skipped for EventId={EventId}.",
                    notificationType, eventId);
                return Result<int>.Success(0);
            }

            _logger.LogInformation(
                "[Phase 7A] Broadcasting to {UserCount} opted-in users for EventId={EventId}",
                optedInUsers.Count, eventId);

            int sentCount = 0;
            int skippedCount = 0;
            int failedCount = 0;

            foreach (var prefs in optedInUsers)
            {
                if (string.IsNullOrWhiteSpace(prefs.WhatsAppPhoneNumber))
                {
                    skippedCount++;
                    continue;
                }

                try
                {
                    // Check dedup per user
                    var isDuplicate = await _messageRepository.HasRecentDuplicateAsync(
                        prefs.UserId, eventId, templateName, DeduplicationWindowMinutes, ct);
                    if (isDuplicate)
                    {
                        skippedCount++;
                        continue;
                    }

                    var result = await SendViaTemplateAsync(
                        prefs.WhatsAppPhoneNumber,
                        templateName,
                        parameters,
                        prefs.PreferredLanguage,
                        prefs.UserId,
                        eventId,
                        registrationId: null,
                        ct: ct);

                    if (result.IsSuccess && !result.Value.WasSkipped)
                        sentCount++;
                    else
                        skippedCount++;
                }
                catch (Exception ex)
                {
                    failedCount++;
                    _logger.LogWarning(ex,
                        "[Phase 7A] Broadcast send failed for UserId={UserId}, EventId={EventId}. Continuing with next recipient.",
                        prefs.UserId, eventId);
                }
            }

            _logger.LogInformation(
                "[Phase 7A] BroadcastToEventAttendees COMPLETE: EventId={EventId}, Sent={SentCount}, Skipped={SkippedCount}, Failed={FailedCount}",
                eventId, sentCount, skippedCount, failedCount);

            return Result<int>.Success(sentCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Phase 7A] BroadcastToEventAttendees EXCEPTION: EventId={EventId}, Template={TemplateName}",
                eventId, templateName);
            return Result<int>.Failure($"Broadcast failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Core send logic: template lookup, parameter ordering, provider send, and persistence.
    /// </summary>
    private async Task<Result<WhatsAppSendResult>> SendViaTemplateAsync(
        string phoneNumber,
        string templateName,
        Dictionary<string, string> parameters,
        string language,
        Guid? userId,
        Guid? eventId,
        Guid? registrationId,
        CancellationToken ct)
    {
        // Step 4: Lookup template in registry
        var template = await _templateRepository.GetByNameAsync(templateName, ct);
        if (template == null)
        {
            _logger.LogWarning("[Phase 7A] Template not found: {TemplateName}", templateName);
            return Result<WhatsAppSendResult>.Failure($"WhatsApp template '{templateName}' not found in registry.");
        }

        if (!template.IsApproved)
        {
            _logger.LogWarning(
                "[Phase 7A] Template not approved: {TemplateName}, Status={Status}",
                templateName, template.Status);
            return Result<WhatsAppSendResult>.Failure($"WhatsApp template '{templateName}' is not approved (Status: {template.Status}).");
        }

        // Step 5: Build ordered parameter values from template's parameter names
        var parameterNames = template.GetParameterNames();
        var orderedValues = new List<string>();

        foreach (var paramName in parameterNames)
        {
            if (parameters.TryGetValue(paramName, out var value))
            {
                orderedValues.Add(value);
            }
            else
            {
                _logger.LogWarning(
                    "[Phase 7A] Missing parameter '{ParamName}' for template '{TemplateName}'. Using empty string.",
                    paramName, templateName);
                orderedValues.Add(string.Empty);
            }
        }

        // Step 7: Create message record for persistence
        var record = WhatsAppMessageRecord.Create(
            fromPhoneNumber: _settings.SenderPhoneNumber,
            toPhoneNumber: phoneNumber,
            messageType: WhatsAppMessageType.Template,
            templateName: templateName,
            parameters: parameters,
            language: language,
            userId: userId,
            eventId: eventId,
            registrationId: registrationId);

        // Phase 7B: Track which provider sent this message
        record.SetProvider(_settings.Provider);

        await _messageRepository.AddAsync(record, ct);

        // Step 6: Send via provider strategy (ACS or Twilio).
        // Phase 7B.4: pass template.TwilioContentSid as providerTemplateId so the Twilio
        // strategy can use the Content API. ACS ignores this argument.
        var sendResult = await _sendStrategy.SendTemplateMessageAsync(
            phoneNumber,
            templateName,
            orderedValues,
            language,
            template.TwilioContentSid,
            ct);

        // Step 8-9: Update record based on result
        if (sendResult.IsSuccess)
        {
            record.MarkAsSent(sendResult.Value);
            _logger.LogInformation(
                "[Phase 7A] Message record {RecordId} marked as SENT. Provider={Provider}, ProviderMessageId={ProviderMessageId}",
                record.Id, _settings.Provider, sendResult.Value);
        }
        else
        {
            record.MarkAsFailed(sendResult.Error);
            _logger.LogWarning(
                "[Phase 7A] Message record {RecordId} marked as FAILED. Error={Error}",
                record.Id, sendResult.Error);
        }

        // Persist the record state
        try
        {
            await _unitOfWork.CommitAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Phase 7A] Failed to persist WhatsApp message record {RecordId}. Message was {SendStatus}.",
                record.Id, sendResult.IsSuccess ? "sent" : "not sent");
            // Don't fail the overall operation if persistence fails but the message was sent
        }

        // Step 10: Return result
        if (sendResult.IsSuccess)
        {
            return Result<WhatsAppSendResult>.Success(
                WhatsAppSendResult.Sent(record.Id, sendResult.Value));
        }

        return Result<WhatsAppSendResult>.Failure(sendResult.Error);
    }
}
