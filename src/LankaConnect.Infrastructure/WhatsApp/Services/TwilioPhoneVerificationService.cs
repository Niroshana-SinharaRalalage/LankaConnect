using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Infrastructure.WhatsApp.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Exceptions;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace LankaConnect.Infrastructure.WhatsApp.Services;

/// <summary>
/// Phase 7B: Twilio-based phone verification service.
/// Sends verification codes via Twilio SMS API (not Verify API) to match
/// the existing flow where the backend generates the code in UserWhatsAppPreferences.
/// This is Option A from the plan: minimal disruption, same flow, different transport.
/// </summary>
public class TwilioPhoneVerificationService : IPhoneVerificationService
{
    private readonly WhatsAppSettings _settings;
    private readonly ILogger<TwilioPhoneVerificationService> _logger;
    private bool _clientInitialized;
    private readonly object _clientLock = new();

    public TwilioPhoneVerificationService(
        IOptions<WhatsAppSettings> settings,
        ILogger<TwilioPhoneVerificationService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Initialize Twilio client (thread-safe, lazy). Matches TwilioWhatsAppStrategy pattern.
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
                    "WhatsAppSettings:TwilioAccountSid is not configured.");
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

    public async Task<Result> SendVerificationCodeAsync(
        string phoneNumber,
        string code,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[Phase 7B] Twilio phone verification START: PhoneLength={PhoneLength}",
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

            if (!_settings.Enabled)
            {
                _logger.LogWarning(
                    "[Phase 7B] WhatsApp/Twilio DISABLED globally. Cannot send verification code.");
                return Result.Failure("Phone verification is not available. Messaging is disabled.");
            }

            EnsureClientInitialized();

            if (string.IsNullOrWhiteSpace(_settings.TwilioWhatsAppNumber))
            {
                _logger.LogError("[Phase 7B] Twilio phone verification FAILED: TwilioWhatsAppNumber not configured");
                return Result.Failure("TwilioWhatsAppNumber is not configured.");
            }

            // Send verification code via Twilio SMS (not WhatsApp) for better delivery
            var messageResource = await MessageResource.CreateAsync(
                from: new PhoneNumber(_settings.TwilioWhatsAppNumber),
                to: new PhoneNumber(phoneNumber),
                body: $"Your LankaConnect verification code is: {code}. This code expires in 10 minutes.");

            var messageSid = messageResource.Sid ?? "unknown";

            _logger.LogInformation(
                "[Phase 7B] Twilio phone verification code sent. TwilioSid={TwilioSid}, Status={Status}",
                messageSid, messageResource.Status);

            return Result.Success();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex,
                "[Phase 7B] Twilio phone verification API ERROR: StatusCode={StatusCode}, ErrorCode={ErrorCode}",
                ex.Status, ex.Code);
            return Result.Failure($"Failed to send verification code: Twilio API error {ex.Status}");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "[Phase 7B] Twilio phone verification CONFIG ERROR");
            return Result.Failure($"Configuration error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Phase 7B] Twilio phone verification EXCEPTION: PhoneLength={PhoneLength}",
                phoneNumber.Length);
            return Result.Failure($"Failed to send verification code: {ex.Message}");
        }
    }
}
