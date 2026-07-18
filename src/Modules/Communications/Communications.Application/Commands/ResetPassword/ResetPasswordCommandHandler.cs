using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Modules.Identity.Contracts;
using LankaConnect.Modules.Communications.Contracts.Email.Contracts;
using LankaConnect.Modules.Communications.Contracts.Email.Services;
using Serilog.Context;
using LankaConnect.Modules.Communications.Contracts.Commands; // 4C.h Day 5
namespace LankaConnect.Modules.Communications.Application.Commands.ResetPassword;

/// <summary>
/// Handler for resetting user passwords using reset tokens.
/// Wave 4.7.b (2026-06-25): swapped IUserRepository + IPasswordHashingService +
/// IUnitOfWork to IIdentityCommands.CompletePasswordResetAsync. The Identity-side
/// adapter now owns token validation, password hashing, ChangePassword aggregate
/// transition, RevokeAllRefreshTokens, and persistence. This handler is reduced
/// to: dispatch, response shaping, fire-and-forget confirmation email.
/// </summary>
public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result<ResetPasswordResponse>>
{
    private readonly IIdentityCommands _identityCommands;
    private readonly ITypedEmailService _typedEmailService;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        IIdentityCommands identityCommands,
        ITypedEmailService typedEmailService,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _identityCommands = identityCommands;
        _typedEmailService = typedEmailService;
        _logger = logger;
    }

    public async Task<Result<ResetPasswordResponse>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "ResetPassword"))
        using (LogContext.PushProperty("EntityType", "User"))
        using (LogContext.PushProperty("Email", request.Email ?? "not-provided"))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "ResetPassword START: Email={Email}, HasToken={HasToken}",
                request.Email ?? "not-provided",
                !string.IsNullOrEmpty(request.Token));

            try
            {
                PasswordResetCompletedDto completed;
                try
                {
                    completed = await _identityCommands.CompletePasswordResetAsync(
                        request.Token,
                        request.Email,
                        request.NewPassword,
                        cancellationToken);
                }
                catch (InvalidOperationException ex)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(ex,
                        "ResetPassword FAILED: Identity rejected completion - Email={Email}, Duration={ElapsedMs}ms",
                        request.Email ?? "not-provided",
                        stopwatch.ElapsedMilliseconds);
                    return Result<ResetPasswordResponse>.Failure("Invalid or expired reset token");
                }

                _logger.LogInformation(
                    "ResetPassword: Identity completed - UserId={UserId}, Email={Email}",
                    completed.UserId,
                    completed.Email);

                var typedEmailService = _typedEmailService;
                var logger = _logger;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var emailParams = PasswordChangedEmailParams.Create(
                            userId: completed.UserId,
                            userName: completed.DisplayName,
                            userEmail: completed.Email,
                            changedAt: DateTime.UtcNow
                        );

                        var typedResult = await typedEmailService.SendEmailAsync(
                            emailParams,
                            CancellationToken.None);

                        if (typedResult.Success)
                        {
                            logger.LogInformation(
                                "ResetPassword: Confirmation email sent - UserId={UserId}",
                                completed.UserId);
                        }
                        else
                        {
                            logger.LogWarning(
                                "ResetPassword: Confirmation email failed - UserId={UserId}, Errors={Errors}",
                                completed.UserId,
                                string.Join(", ", typedResult.Errors));
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex,
                            "ResetPassword: Failed to send confirmation email - UserId={UserId}, ErrorMessage={ErrorMessage}",
                            completed.UserId,
                            ex.Message);
                    }
                }, cancellationToken);

                var response = new ResetPasswordResponse(
                    completed.UserId,
                    completed.Email,
                    DateTime.UtcNow);

                stopwatch.Stop();
                _logger.LogInformation(
                    "ResetPassword COMPLETE: Email={Email}, UserId={UserId}, Duration={ElapsedMs}ms",
                    completed.Email,
                    completed.UserId,
                    stopwatch.ElapsedMilliseconds);

                return Result<ResetPasswordResponse>.Success(response);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "ResetPassword FAILED: Unexpected error - Email={Email}, Duration={ElapsedMs}ms, ErrorMessage={ErrorMessage}",
                    request.Email ?? "not-provided",
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                return Result<ResetPasswordResponse>.Failure("An error occurred while resetting password");
            }
        }
    }
}
