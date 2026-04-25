using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Infrastructure.WhatsApp.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LankaConnect.Infrastructure.WhatsApp.Services;

/// <summary>
/// Phase 7A: Phone verification service implementation.
/// Ideally sends verification codes via SMS (Azure.Communication.Sms).
/// Since the SMS NuGet is not installed, uses WhatsApp 'phone_verification' template as fallback.
/// Per plan FIX C4: SMS is preferred but WhatsApp template works as a fallback for MVP.
/// </summary>
public class SmsPhoneVerificationService : IPhoneVerificationService
{
    private readonly IWhatsAppSendStrategy _sendStrategy;
    private readonly WhatsAppSettings _settings;
    private readonly ILogger<SmsPhoneVerificationService> _logger;

    /// <summary>
    /// The WhatsApp template name for phone verification.
    /// This is template #0 in the seed data.
    /// </summary>
    private const string VerificationTemplateName = "phone_verification";

    public SmsPhoneVerificationService(
        IWhatsAppSendStrategy sendStrategy,
        IOptions<WhatsAppSettings> settings,
        ILogger<SmsPhoneVerificationService> logger)
    {
        _sendStrategy = sendStrategy;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<Result> SendVerificationCodeAsync(
        string phoneNumber,
        string code,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[Phase 7A] Phone verification START: PhoneLength={PhoneLength}",
            phoneNumber.Length);

        try
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return Result.Failure("Phone number is required for verification.");
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                return Result.Failure("Verification code is required.");
            }

            // Check global feature flag
            if (!_settings.Enabled)
            {
                _logger.LogWarning(
                    "[Phase 7A] WhatsApp DISABLED globally. Cannot send verification code. " +
                    "SMS NuGet (Azure.Communication.Sms) not installed. " +
                    "Enable WhatsApp or install SMS package for phone verification.");
                return Result.Failure("Phone verification is not available. WhatsApp messaging is disabled and SMS is not configured.");
            }

            // Use WhatsApp template as fallback (SMS NuGet not available)
            _logger.LogWarning(
                "[Phase 7A] Using WhatsApp template '{TemplateName}' for phone verification. " +
                "SMS (Azure.Communication.Sms) is preferred but NuGet not installed.",
                VerificationTemplateName);

            // The phone_verification template expects a single parameter: the verification code
            var parameterValues = new List<string> { code };

            var result = await _sendStrategy.SendTemplateMessageAsync(
                phoneNumber,
                VerificationTemplateName,
                parameterValues,
                language: "en",
                providerTemplateId: null,
                ct: ct);

            if (result.IsSuccess)
            {
                _logger.LogInformation(
                    "[Phase 7A] Phone verification code sent via WhatsApp. AcsMessageId={AcsMessageId}",
                    result.Value);
                return Result.Success();
            }

            _logger.LogError(
                "[Phase 7A] Phone verification code send FAILED: {Error}",
                result.Error);
            return Result.Failure($"Failed to send verification code: {result.Error}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Phase 7A] Phone verification EXCEPTION: PhoneLength={PhoneLength}",
                phoneNumber.Length);
            return Result.Failure($"Failed to send verification code: {ex.Message}");
        }
    }
}
