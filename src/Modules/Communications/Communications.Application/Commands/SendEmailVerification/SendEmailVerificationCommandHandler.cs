using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Modules.Identity.Contracts;
using Serilog.Context;
using LankaConnect.Modules.Communications.Contracts.Commands; // 4C.h Day 5: SendEmailVerificationCommand promoted
namespace LankaConnect.Modules.Communications.Application.Commands.SendEmailVerification;

/// <summary>
/// Handler for sending email verification emails.
/// Wave 4.7.b (2026-06-25): swapped IUserRepository + IUnitOfWork to
/// IIdentityCommands.InitiateEmailVerificationAsync. The Identity-side adapter
/// owns token generation, expiry, the GenerateEmailVerificationToken state
/// transition (raises MemberVerificationRequestedEvent inside the User
/// aggregate), and persistence. The already-verified short-circuit + 5-minute
/// throttle still live here because they are caller-visible response shaping.
/// </summary>
public class SendEmailVerificationCommandHandler : IRequestHandler<SendEmailVerificationCommand, Result<SendEmailVerificationResponse>>
{
    private static readonly TimeSpan EmailVerificationTokenLifetime = TimeSpan.FromHours(24);

    private readonly IIdentityQueries _identityQueries;
    private readonly IIdentityCommands _identityCommands;
    private readonly ILogger<SendEmailVerificationCommandHandler> _logger;

    public SendEmailVerificationCommandHandler(
        IIdentityQueries identityQueries,
        IIdentityCommands identityCommands,
        ILogger<SendEmailVerificationCommandHandler> logger)
    {
        _identityQueries = identityQueries;
        _identityCommands = identityCommands;
        _logger = logger;
    }

    public async Task<Result<SendEmailVerificationResponse>> Handle(SendEmailVerificationCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "SendEmailVerification"))
        using (LogContext.PushProperty("EntityType", "User"))
        using (LogContext.PushProperty("UserId", request.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "SendEmailVerification START: UserId={UserId}, ForceResend={ForceResend}",
                request.UserId,
                request.ForceResend);

            try
            {
                var user = await _identityQueries.GetContactInfoAsync(request.UserId, cancellationToken);
                if (user == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "SendEmailVerification FAILED: User not found - UserId={UserId}, Duration={ElapsedMs}ms",
                        request.UserId,
                        stopwatch.ElapsedMilliseconds);
                    return Result<SendEmailVerificationResponse>.Failure("User not found");
                }

                if (user.IsEmailVerified && !request.ForceResend)
                {
                    stopwatch.Stop();
                    _logger.LogInformation(
                        "SendEmailVerification: Email already verified - UserId={UserId}, Email={Email}, Duration={ElapsedMs}ms",
                        user.Id,
                        user.Email,
                        stopwatch.ElapsedMilliseconds);

                    var alreadyVerifiedResponse = new SendEmailVerificationResponse(
                        user.Id,
                        user.Email,
                        user.EmailVerificationTokenExpiresAt ?? DateTime.UtcNow,
                        wasRecentlySent: false);

                    return Result<SendEmailVerificationResponse>.Success(alreadyVerifiedResponse);
                }

                var targetEmail = request.Email ?? user.Email;

                if (!request.ForceResend && user.EmailVerificationTokenExpiresAt.HasValue)
                {
                    var tokenCreatedAt = user.EmailVerificationTokenExpiresAt.Value.Subtract(EmailVerificationTokenLifetime);
                    if (DateTime.UtcNow.Subtract(tokenCreatedAt).TotalMinutes < 5)
                    {
                        stopwatch.Stop();
                        _logger.LogInformation(
                            "SendEmailVerification: Recently sent, skipping resend - UserId={UserId}, Email={Email}, Duration={ElapsedMs}ms",
                            user.Id,
                            targetEmail,
                            stopwatch.ElapsedMilliseconds);

                        var response = new SendEmailVerificationResponse(
                            user.Id,
                            targetEmail,
                            user.EmailVerificationTokenExpiresAt.Value,
                            wasRecentlySent: true);

                        return Result<SendEmailVerificationResponse>.Success(response);
                    }
                }

                EmailVerificationInitiatedDto? initiated;
                try
                {
                    initiated = await _identityCommands.InitiateEmailVerificationAsync(
                        request.UserId,
                        EmailVerificationTokenLifetime,
                        cancellationToken);
                }
                catch (InvalidOperationException ex)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(ex,
                        "SendEmailVerification FAILED: Identity rejected initiation - UserId={UserId}, Duration={ElapsedMs}ms",
                        request.UserId,
                        stopwatch.ElapsedMilliseconds);
                    return Result<SendEmailVerificationResponse>.Failure("Failed to initiate email verification. Please try again.");
                }

                if (initiated == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "SendEmailVerification FAILED: Identity returned null - UserId={UserId}, Duration={ElapsedMs}ms",
                        request.UserId,
                        stopwatch.ElapsedMilliseconds);
                    return Result<SendEmailVerificationResponse>.Failure("User not found");
                }

                _logger.LogInformation(
                    "SendEmailVerification: Identity initiated - UserId={UserId}, Email={Email}, ExpiresAt={ExpiresAt}",
                    initiated.UserId,
                    initiated.Email,
                    initiated.TokenExpiresAt);

                var successResponse = new SendEmailVerificationResponse(
                    initiated.UserId,
                    targetEmail,
                    initiated.TokenExpiresAt);

                stopwatch.Stop();
                _logger.LogInformation(
                    "SendEmailVerification COMPLETE: UserId={UserId}, Email={Email}, Duration={ElapsedMs}ms",
                    initiated.UserId,
                    targetEmail,
                    stopwatch.ElapsedMilliseconds);

                return Result<SendEmailVerificationResponse>.Success(successResponse);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "SendEmailVerification FAILED: Unexpected error - UserId={UserId}, Duration={ElapsedMs}ms, ErrorMessage={ErrorMessage}",
                    request.UserId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                throw;
            }
        }
    }
}
