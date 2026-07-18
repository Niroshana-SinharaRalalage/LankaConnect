using System.Diagnostics;
using LankaConnect.Modules.Communications.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Application.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Modules.Identity.Contracts;
using LankaConnect.Modules.Communications.Contracts.Email.Contracts;
using LankaConnect.Modules.Communications.Contracts.Email.Services;
using Serilog.Context;
using LankaConnect.Modules.Communications.Contracts.Commands; // 4C.h Day 5
namespace LankaConnect.Modules.Communications.Application.Commands.SendPasswordReset;

/// <summary>
/// Handler for sending password reset emails.
/// Wave 4.7.b (2026-06-25): swapped IUserRepository + IUnitOfWork + direct
/// token-generation to IIdentityCommands.InitiatePasswordResetAsync. The
/// Identity-side adapter now owns token generation, expiry, persistence,
/// and the 5-minute throttle. This handler is reduced to: email format
/// validation, mutator dispatch, response shaping, email side-effect.
/// </summary>
public class SendPasswordResetCommandHandler : IRequestHandler<SendPasswordResetCommand, Result<SendPasswordResetResponse>>
{
    // Wave 4.7.b (2026-06-25): IIdentityCommands surface contract; token
    // lifetime + throttle window live inside the adapter implementation.
    private static readonly TimeSpan PasswordResetTokenLifetime = TimeSpan.FromHours(1);

    private readonly IIdentityCommands _identityCommands;
    private readonly ITypedEmailService _typedEmailService;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly ILogger<SendPasswordResetCommandHandler> _logger;

    public SendPasswordResetCommandHandler(
        IIdentityCommands identityCommands,
        ITypedEmailService typedEmailService,
        IEmailUrlHelper emailUrlHelper,
        ILogger<SendPasswordResetCommandHandler> logger)
    {
        _identityCommands = identityCommands;
        _typedEmailService = typedEmailService;
        _emailUrlHelper = emailUrlHelper;
        _logger = logger;
    }

    public async Task<Result<SendPasswordResetResponse>> Handle(SendPasswordResetCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "SendPasswordReset"))
        using (LogContext.PushProperty("EntityType", "User"))
        using (LogContext.PushProperty("Email", request.Email))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "SendPasswordReset START: Email={Email}, ForceResend={ForceResend}",
                request.Email,
                request.ForceResend);

            try
            {
                var emailResult = Email.Create(request.Email);
                if (!emailResult.IsSuccess)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "SendPasswordReset FAILED: Invalid email format - Email={Email}, Error={Error}, Duration={ElapsedMs}ms",
                        request.Email,
                        emailResult.Error,
                        stopwatch.ElapsedMilliseconds);
                    return Result<SendPasswordResetResponse>.Failure("Invalid email format");
                }

                PasswordResetInitiatedDto? initiated;
                try
                {
                    initiated = await _identityCommands.InitiatePasswordResetAsync(
                        request.Email,
                        PasswordResetTokenLifetime,
                        request.ForceResend,
                        cancellationToken);
                }
                catch (InvalidOperationException ex)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(ex,
                        "SendPasswordReset FAILED: Identity rejected initiation - Email={Email}, Duration={ElapsedMs}ms",
                        request.Email,
                        stopwatch.ElapsedMilliseconds);
                    return Result<SendPasswordResetResponse>.Failure("Failed to initiate password reset. Please try again.");
                }

                if (initiated == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "SendPasswordReset: User not found (security: returning success) - Email={Email}, Duration={ElapsedMs}ms",
                        request.Email,
                        stopwatch.ElapsedMilliseconds);

                    var notFoundResponse = new SendPasswordResetResponse(
                        Guid.Empty,
                        request.Email,
                        DateTime.UtcNow.Add(PasswordResetTokenLifetime),
                        userNotFound: true);

                    return Result<SendPasswordResetResponse>.Success(notFoundResponse);
                }

                _logger.LogInformation(
                    "SendPasswordReset: Identity initiated - Email={Email}, UserId={UserId}, ExpiresAt={ExpiresAt}, WasThrottled={WasThrottled}",
                    request.Email,
                    initiated.UserId,
                    initiated.TokenExpiresAt,
                    initiated.WasThrottled);

                var emailParams = PasswordResetEmailParams.Create(
                    userId: initiated.UserId,
                    userName: initiated.DisplayName,
                    userEmail: initiated.Email,
                    resetToken: initiated.PasswordResetToken,
                    resetLink: _emailUrlHelper.BuildPasswordResetUrl(initiated.PasswordResetToken),
                    expiresAt: initiated.TokenExpiresAt
                );

                var typedResult = await _typedEmailService.SendEmailAsync(emailParams, cancellationToken);

                if (!typedResult.Success)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "SendPasswordReset: Email send failed (token saved, user can retry) - Email={Email}, UserId={UserId}, Errors={Errors}, Duration={ElapsedMs}ms",
                        request.Email,
                        initiated.UserId,
                        string.Join(", ", typedResult.Errors),
                        stopwatch.ElapsedMilliseconds);
                    return Result<SendPasswordResetResponse>.Failure("Failed to send password reset email. Please try again.");
                }

                var successResponse = new SendPasswordResetResponse(
                    initiated.UserId,
                    request.Email,
                    initiated.TokenExpiresAt,
                    wasRecentlySent: initiated.WasThrottled);

                stopwatch.Stop();
                _logger.LogInformation(
                    "SendPasswordReset COMPLETE: Email={Email}, UserId={UserId}, ExpiresAt={ExpiresAt}, Duration={ElapsedMs}ms",
                    request.Email,
                    initiated.UserId,
                    initiated.TokenExpiresAt,
                    stopwatch.ElapsedMilliseconds);

                return Result<SendPasswordResetResponse>.Success(successResponse);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "SendPasswordReset FAILED: Unexpected error - Email={Email}, Duration={ElapsedMs}ms, ErrorMessage={ErrorMessage}",
                    request.Email,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                return Result<SendPasswordResetResponse>.Failure("An error occurred while sending password reset email");
            }
        }
    }
}
