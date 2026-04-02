using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;

namespace LankaConnect.Application.Communications.WhatsApp.Commands.VerifyWhatsAppPhone;

/// <summary>
/// Phase 7A.2: Command to verify a WhatsApp phone number with a 6-digit code.
/// </summary>
public record VerifyWhatsAppPhoneCommand(Guid UserId, string Code) : ICommand<VerifyWhatsAppPhoneResponse>;

/// <summary>
/// Response returned after a phone verification attempt.
/// </summary>
public class VerifyWhatsAppPhoneResponse
{
    public bool PhoneVerified { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Phase 7A.2: Handler for VerifyWhatsAppPhoneCommand.
/// Gets preferences, calls VerifyPhone(code), handles lockout, and persists.
/// </summary>
public class VerifyWhatsAppPhoneCommandHandler : IRequestHandler<VerifyWhatsAppPhoneCommand, Result<VerifyWhatsAppPhoneResponse>>
{
    private readonly IUserWhatsAppPreferencesRepository _preferencesRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<VerifyWhatsAppPhoneCommandHandler> _logger;

    public VerifyWhatsAppPhoneCommandHandler(
        IUserWhatsAppPreferencesRepository preferencesRepository,
        IUnitOfWork unitOfWork,
        ILogger<VerifyWhatsAppPhoneCommandHandler> logger)
    {
        _preferencesRepository = preferencesRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<VerifyWhatsAppPhoneResponse>> Handle(
        VerifyWhatsAppPhoneCommand request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "VerifyWhatsAppPhone"))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "VerifyWhatsAppPhone START: UserId={UserId}",
                request.UserId);

            try
            {
                var preferences = await _preferencesRepository.GetByUserIdAsync(request.UserId, cancellationToken);

                if (preferences is null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "VerifyWhatsAppPhone FAILED: No WhatsApp preferences found - UserId={UserId}, Duration={ElapsedMs}ms",
                        request.UserId, stopwatch.ElapsedMilliseconds);
                    return Result<VerifyWhatsAppPhoneResponse>.Failure("WhatsApp preferences not found. Please enable WhatsApp first.");
                }

                if (preferences.IsLocked)
                {
                    var minutesLeft = (int)Math.Ceiling(
                        (preferences.VerificationLockedUntil!.Value - DateTime.UtcNow).TotalMinutes);
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "VerifyWhatsAppPhone FAILED: Account locked - UserId={UserId}, LockedMinutes={LockedMinutes}, Duration={ElapsedMs}ms",
                        request.UserId, minutesLeft, stopwatch.ElapsedMilliseconds);

                    var lockedResponse = new VerifyWhatsAppPhoneResponse
                    {
                        PhoneVerified = false,
                        ErrorMessage = $"Too many verification attempts. Please try again in {minutesLeft} minute(s)."
                    };
                    return Result<VerifyWhatsAppPhoneResponse>.Success(lockedResponse);
                }

                var verifyResult = preferences.VerifyPhone(request.Code);

                // Always persist — VerifyPhone may have updated VerificationAttempts even on failure
                await _unitOfWork.CommitAsync(cancellationToken);

                if (verifyResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "VerifyWhatsAppPhone FAILED: Verification failed - UserId={UserId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.UserId, verifyResult.Error, stopwatch.ElapsedMilliseconds);

                    var failedResponse = new VerifyWhatsAppPhoneResponse
                    {
                        PhoneVerified = false,
                        ErrorMessage = verifyResult.Error
                    };
                    return Result<VerifyWhatsAppPhoneResponse>.Success(failedResponse);
                }

                var response = new VerifyWhatsAppPhoneResponse
                {
                    PhoneVerified = true,
                    ErrorMessage = null
                };

                stopwatch.Stop();
                _logger.LogInformation(
                    "VerifyWhatsAppPhone COMPLETE: UserId={UserId}, PhoneVerified=true, Duration={ElapsedMs}ms",
                    request.UserId, stopwatch.ElapsedMilliseconds);

                return Result<VerifyWhatsAppPhoneResponse>.Success(response);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "VerifyWhatsAppPhone FAILED: Unexpected error - UserId={UserId}, Duration={ElapsedMs}ms",
                    request.UserId, stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
