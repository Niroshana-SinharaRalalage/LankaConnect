using LankaConnect.Modules.Identity.Contracts; // W4.6.d.2.b: IUserRepository -> IIdentityQueries/IIdentityCommands
using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Common.Constants;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Services;
using Serilog.Context;

namespace LankaConnect.Application.Communications.Commands.ResetPassword;

/// <summary>
/// Handler for resetting user passwords using reset tokens
/// Phase 6A.X Observability: Enhanced with comprehensive structured logging
/// Phase 6A.87: Migrated to ITypedEmailService for hybrid email support
/// </summary>
public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result<ResetPasswordResponse>>
{
    private readonly LankaConnect.Domain.Users.IUserRepository _userRepository;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly ITypedEmailService _typedEmailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        LankaConnect.Domain.Users.IUserRepository userRepository,
        IPasswordHashingService passwordHashingService,
        ITypedEmailService typedEmailService,
        IUnitOfWork unitOfWork,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHashingService = passwordHashingService;
        _typedEmailService = typedEmailService;
        _unitOfWork = unitOfWork;
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
                // Phase 6A.101: Find user by token (primary) or by email (backward compatibility)
                LankaConnect.Domain.Users.User? user = null;

                // First, try to find user by reset token (preferred method)
                user = await _userRepository.GetByPasswordResetTokenAsync(request.Token, cancellationToken);

                // If not found by token and email is provided, try email lookup for backward compatibility
                if (user == null && !string.IsNullOrWhiteSpace(request.Email))
                {
                    var emailResult = Email.Create(request.Email);
                    if (emailResult.IsSuccess)
                    {
                        user = await _userRepository.GetByEmailAsync(emailResult.Value, cancellationToken);
                    }
                }

                if (user == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "ResetPassword FAILED: User not found by token or email - Email={Email}, Duration={ElapsedMs}ms",
                        request.Email ?? "not-provided",
                        stopwatch.ElapsedMilliseconds);
                    return Result<ResetPasswordResponse>.Failure("Invalid or expired reset token");
                }

                // Validate reset token
                if (!user.IsPasswordResetTokenValid(request.Token))
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "ResetPassword FAILED: Invalid or expired token - Email={Email}, UserId={UserId}, Duration={ElapsedMs}ms",
                        user.Email.Value,
                        user.Id,
                        stopwatch.ElapsedMilliseconds);
                    return Result<ResetPasswordResponse>.Failure("Invalid or expired reset token");
                }

                _logger.LogInformation(
                    "ResetPassword: Token validation passed - Email={Email}, UserId={UserId}",
                    user.Email.Value,
                    user.Id);

                // Validate password strength
                var passwordValidationResult = _passwordHashingService.ValidatePasswordStrength(request.NewPassword);
                if (!passwordValidationResult.IsSuccess)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "ResetPassword FAILED: Password validation failed - Email={Email}, UserId={UserId}, Error={Error}, Duration={ElapsedMs}ms",
                        user.Email.Value,
                        user.Id,
                        passwordValidationResult.Error,
                        stopwatch.ElapsedMilliseconds);
                    return Result<ResetPasswordResponse>.Failure(passwordValidationResult.Error);
                }

                // Hash new password
                var hashResult = _passwordHashingService.HashPassword(request.NewPassword);
                if (!hashResult.IsSuccess)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "ResetPassword FAILED: Password hashing failed - Email={Email}, UserId={UserId}, Error={Error}, Duration={ElapsedMs}ms",
                        user.Email.Value,
                        user.Id,
                        hashResult.Error,
                        stopwatch.ElapsedMilliseconds);
                    return Result<ResetPasswordResponse>.Failure(hashResult.Error);
                }

                // Change password (this will clear the reset token and failed login attempts)
                var changePasswordResult = user.ChangePassword(hashResult.Value);
                if (!changePasswordResult.IsSuccess)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "ResetPassword FAILED: Change password failed - Email={Email}, UserId={UserId}, Error={Error}, Duration={ElapsedMs}ms",
                        user.Email.Value,
                        user.Id,
                        changePasswordResult.Error,
                        stopwatch.ElapsedMilliseconds);
                    return Result<ResetPasswordResponse>.Failure(changePasswordResult.Error);
                }

                // Revoke all existing refresh tokens for security
                user.RevokeAllRefreshTokens("Password reset");

                _logger.LogInformation(
                    "ResetPassword: Password changed, tokens revoked - Email={Email}, UserId={UserId}",
                    user.Email.Value,
                    user.Id);

                // Save changes
                await _unitOfWork.CommitAsync(cancellationToken);

                // Send password change confirmation email asynchronously
                // Phase 6A.87: Capture typed email service for closure
                var typedEmailService = _typedEmailService;
                var logger = _logger;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Phase 6A.87: Use typed email parameters for compile-time safety
                        var emailParams = PasswordChangedEmailParams.Create(
                            userId: user.Id,
                            userName: user.FullName,
                            userEmail: user.Email.Value,
                            changedAt: DateTime.UtcNow
                        );

                        // Phase 6A.100: Send via typed email service
                        var typedResult = await typedEmailService.SendEmailAsync(
                            emailParams,
                            CancellationToken.None);

                        if (typedResult.Success)
                        {
                            logger.LogInformation(
                                "[Phase 6A.100] ResetPassword: Confirmation email sent - UserId={UserId}",
                                user.Id);
                        }
                        else
                        {
                            logger.LogWarning(
                                "ResetPassword: Confirmation email failed - UserId={UserId}, Errors={Errors}",
                                user.Id,
                                string.Join(", ", typedResult.Errors));
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex,
                            "ResetPassword: Failed to send confirmation email - UserId={UserId}, ErrorMessage={ErrorMessage}",
                            user.Id,
                            ex.Message);
                    }
                }, cancellationToken);

                var response = new ResetPasswordResponse(
                    user.Id,
                    user.Email.Value,
                    DateTime.UtcNow);

                stopwatch.Stop();
                _logger.LogInformation(
                    "ResetPassword COMPLETE: Email={Email}, UserId={UserId}, Duration={ElapsedMs}ms",
                    user.Email.Value,
                    user.Id,
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