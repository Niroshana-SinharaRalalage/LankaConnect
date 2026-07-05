using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Infrastructure.WhatsApp.Configuration;
using LankaConnect.Shared.WhatsApp.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
namespace LankaConnect.Modules.Communications.Infrastructure.WhatsApp.Services;

/// <summary>
/// Phase 7B.4 bug-fix: Twilio-based phone verification now delivers the code via the
/// <c>phone_verification</c> <b>WhatsApp</b> Content template, not SMS. The original
/// design sent SMS from the WhatsApp sender number and failed with Twilio error 21660
/// because the WABA number has no SMS capability.
///
/// Verification still generates the code upstream in <c>UserWhatsAppPreferences</c>; this
/// service only transports it. It delegates the send to <see cref="IWhatsAppSendStrategy"/>
/// so the Twilio Content API path (logging, retries, masking) is reused verbatim.
///
/// The required ContentSid is looked up in <c>WhatsAppSettings.TwilioContentSids</c>
/// (seeded from env vars at startup). If the ContentSid is missing we fail fast with a
/// configuration error rather than attempt an unroutable send.
/// </summary>
public class TwilioPhoneVerificationService : IPhoneVerificationService
{
    private readonly WhatsAppSettings _settings;
    private readonly IWhatsAppSendStrategy _sendStrategy;
    private readonly ILogger<TwilioPhoneVerificationService> _logger;

    public TwilioPhoneVerificationService(
        IOptions<WhatsAppSettings> settings,
        IWhatsAppSendStrategy sendStrategy,
        ILogger<TwilioPhoneVerificationService> logger)
    {
        _settings = settings.Value;
        _sendStrategy = sendStrategy;
        _logger = logger;
    }

    public async Task<Result> SendVerificationCodeAsync(
        string phoneNumber,
        string code,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[Phase 7B.4] Twilio phone verification START (via WhatsApp template): PhoneLength={PhoneLength}",
            phoneNumber?.Length ?? 0);

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return Result.Failure("Phone number is required for verification.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return Result.Failure("Verification code is required.");
        }

        if (!_settings.Enabled)
        {
            _logger.LogWarning(
                "[Phase 7B.4] WhatsApp/Twilio globally disabled. Cannot send verification code.");
            return Result.Failure("Phone verification is not available. Messaging is disabled.");
        }

        const string templateName = WhatsAppTemplateContract.TemplateNames.PhoneVerification;

        if (!_settings.TwilioContentSids.TryGetValue(templateName, out var contentSid)
            || string.IsNullOrWhiteSpace(contentSid))
        {
            _logger.LogError(
                "[Phase 7B.4] Twilio phone verification CONFIG ERROR: no ContentSid configured for template '{TemplateName}'. " +
                "Populate WhatsAppSettings:TwilioContentSids:{TemplateName} with the Meta-approved HX-sid.",
                templateName, templateName);
            return Result.Failure(
                $"Missing Twilio ContentSid for template '{templateName}'. Configure WhatsAppSettings:TwilioContentSids.");
        }

        try
        {
            var result = await _sendStrategy.SendTemplateMessageAsync(
                toPhoneNumber: phoneNumber,
                templateName: templateName,
                parameterValues: new[] { code },
                language: "en",
                providerTemplateId: contentSid,
                ct: ct);

            if (!result.IsSuccess)
            {
                _logger.LogError(
                    "[Phase 7B.4] Twilio phone verification FAILED via strategy: {Error}",
                    result.Error);
                return Result.Failure(result.Error ?? "Failed to send verification code.");
            }

            _logger.LogInformation(
                "[Phase 7B.4] Twilio phone verification sent via WhatsApp template. TwilioSid={TwilioSid}",
                result.Value);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Phase 7B.4] Twilio phone verification EXCEPTION: PhoneLength={PhoneLength}",
                phoneNumber.Length);
            return Result.Failure($"Failed to send verification code: {ex.Message}");
        }
    }
}
