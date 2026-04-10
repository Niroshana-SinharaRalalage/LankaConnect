using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;

namespace LankaConnect.Application.Communications.WhatsApp.Commands.RequestPhoneVerification;

/// <summary>
/// Phase 7A.2: Command to request a phone verification code for WhatsApp.
/// Generates a 6-digit code and sends it via IPhoneVerificationService.
/// </summary>
public record RequestPhoneVerificationCommand(Guid UserId) : ICommand;

/// <summary>
/// Phase 7A.2: Handler for RequestPhoneVerificationCommand.
/// Gets preferences, generates verification code, persists, and sends via IPhoneVerificationService.
/// Respects lockout (VerificationLockedUntil) before generating a new code.
/// </summary>
public class RequestPhoneVerificationCommandHandler : IRequestHandler<RequestPhoneVerificationCommand, Result>
{
    private readonly IUserWhatsAppPreferencesRepository _preferencesRepository;
    private readonly IPhoneVerificationService _phoneVerificationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RequestPhoneVerificationCommandHandler> _logger;

    public RequestPhoneVerificationCommandHandler(
        IUserWhatsAppPreferencesRepository preferencesRepository,
        IPhoneVerificationService phoneVerificationService,
        IUnitOfWork unitOfWork,
        ILogger<RequestPhoneVerificationCommandHandler> logger)
    {
        _preferencesRepository = preferencesRepository;
        _phoneVerificationService = phoneVerificationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(
        RequestPhoneVerificationCommand request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "RequestPhoneVerification"))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "RequestPhoneVerification START: UserId={UserId}",
                request.UserId);

            try
            {
                var preferences = await _preferencesRepository.GetByUserIdAsync(request.UserId, cancellationToken);

                if (preferences is null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "RequestPhoneVerification FAILED: No WhatsApp preferences found - UserId={UserId}, Duration={ElapsedMs}ms",
                        request.UserId, stopwatch.ElapsedMilliseconds);
                    return Result.Failure("WhatsApp preferences not found. Please enable WhatsApp first.");
                }

                if (preferences.IsLocked)
                {
                    stopwatch.Stop();
                    var minutesLeft = (int)Math.Ceiling(
                        (preferences.VerificationLockedUntil!.Value - DateTime.UtcNow).TotalMinutes);
                    _logger.LogWarning(
                        "RequestPhoneVerification FAILED: Account locked - UserId={UserId}, LockedMinutes={LockedMinutes}, Duration={ElapsedMs}ms",
                        request.UserId, minutesLeft, stopwatch.ElapsedMilliseconds);
                    return Result.Failure($"Too many verification attempts. Please try again in {minutesLeft} minute(s).");
                }

                var generateResult = preferences.GenerateVerificationCode();
                if (generateResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "RequestPhoneVerification FAILED: Code generation failed - UserId={UserId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.UserId, generateResult.Error, stopwatch.ElapsedMilliseconds);
                    return Result.Failure(generateResult.Error);
                }

                // Persist before sending so the code is saved even if SMS send fails
                await _unitOfWork.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "RequestPhoneVerification: Verification code generated and persisted - UserId={UserId}, Attempt={Attempt}",
                    request.UserId, preferences.VerificationAttempts);

                // Send verification code via phone verification service
                try
                {
                    var sendResult = await _phoneVerificationService.SendVerificationCodeAsync(
                        preferences.WhatsAppPhoneNumber!,
                        preferences.VerificationCode!,
                        cancellationToken);

                    if (sendResult.IsFailure)
                    {
                        _logger.LogWarning(
                            "RequestPhoneVerification: Verification code send FAILED - UserId={UserId}, Error={Error}. Code was saved but delivery failed.",
                            request.UserId, sendResult.Error);
                        return Result.Failure("Verification code generated but failed to send. Please try again.");
                    }

                    _logger.LogInformation(
                        "RequestPhoneVerification: Verification code sent successfully - UserId={UserId}",
                        request.UserId);
                }
                catch (Exception smsEx)
                {
                    _logger.LogError(smsEx,
                        "RequestPhoneVerification: Failed to send verification code - UserId={UserId}, Phone={Phone}. Code was saved but delivery failed.",
                        request.UserId, preferences.WhatsAppPhoneNumber);
                    return Result.Failure("Verification code generated but failed to send. Please try again.");
                }

                stopwatch.Stop();
                _logger.LogInformation(
                    "RequestPhoneVerification COMPLETE: UserId={UserId}, Duration={ElapsedMs}ms",
                    request.UserId, stopwatch.ElapsedMilliseconds);

                return Result.Success();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "RequestPhoneVerification FAILED: Unexpected error - UserId={UserId}, Duration={ElapsedMs}ms",
                    request.UserId, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
