using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Common.Constants;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;
using Serilog.Context;

namespace LankaConnect.Application.Communications.Commands.ResetPassword;

/// <summary>
/// Handler for resetting user passwords using reset tokens
/// Phase 6A.X Observability: Enhanced with comprehensive structured logging
/// </summary>
public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result<ResetPasswordResponse>>
{
    private readonly LankaConnect.Domain.Users.IUserRepository _userRepository;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        LankaConnect.Domain.Users.IUserRepository userRepository,
        IPasswordHashingService passwordHashingService,
        IEmailService emailService,
        IUnitOfWork unitOfWork,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _userRepository = userRepository;
        _passwordHashingService = passwordHashingService;
        _emailService = emailService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<ResetPasswordResponse>> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "ResetPassword"))
        using (LogContext.PushProperty("EntityType", "User"))
        using (LogContext.PushProperty("Email", request.Email))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "ResetPassword START: Email={Email}",
                request.Email);

            try
            {
                // Validate email format
                var emailResult = Email.Create(request.Email);
                if (!emailResult.IsSuccess)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "ResetPassword FAILED: Invalid email format - Email={Email}, Error={Error}, Duration={ElapsedMs}ms",
                        request.Email,
                        emailResult.Error,
                        stopwatch.ElapsedMilliseconds);
                    return Result<ResetPasswordResponse>.Failure("Invalid email format");
                }

                // Get user by email
                var user = await _userRepository.GetByEmailAsync(emailResult.Value, cancellationToken);
                if (user == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "ResetPassword FAILED: User not found - Email={Email}, Duration={ElapsedMs}ms",
                        request.Email,
                        stopwatch.ElapsedMilliseconds);
                    return Result<ResetPasswordResponse>.Failure("Invalid reset token or email");
                }

                // Validate reset token
                if (!user.IsPasswordResetTokenValid(request.Token))
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "ResetPassword FAILED: Invalid or expired token - Email={Email}, UserId={UserId}, Duration={ElapsedMs}ms",
                        request.Email,
                        user.Id,
                        stopwatch.ElapsedMilliseconds);
                    return Result<ResetPasswordResponse>.Failure("Invalid or expired reset token");
                }

                _logger.LogInformation(
                    "ResetPassword: Token validation passed - Email={Email}, UserId={UserId}",
                    request.Email,
                    user.Id);

                // Validate password strength
                var passwordValidationResult = _passwordHashingService.ValidatePasswordStrength(request.NewPassword);
                if (!passwordValidationResult.IsSuccess)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "ResetPassword FAILED: Password validation failed - Email={Email}, UserId={UserId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.Email,
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
                        request.Email,
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
                        request.Email,
                        user.Id,
                        changePasswordResult.Error,
                        stopwatch.ElapsedMilliseconds);
                    return Result<ResetPasswordResponse>.Failure(changePasswordResult.Error);
                }

                // Revoke all existing refresh tokens for security
                user.RevokeAllRefreshTokens("Password reset");

                _logger.LogInformation(
                    "ResetPassword: Password changed, tokens revoked - Email={Email}, UserId={UserId}",
                    request.Email,
                    user.Id);

                // Save changes
                await _unitOfWork.CommitAsync(cancellationToken);

                // Send password change confirmation email asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var templateParameters = new Dictionary<string, object>
                        {
                            { "UserName", user.FullName },
                            { "UserEmail", user.Email.Value },
                            { "ChangeDate", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC") },
                            { "CompanyName", "LankaConnect" },
                            { "SupportEmail", "support@lankaconnect.com" },
                            { "LoginUrl", "https://lankaconnect.com/login" }
                        };

                        await _emailService.SendTemplatedEmailAsync(
                            EmailTemplateNames.PasswordChangeConfirmation,
                            user.Email.Value,
                            templateParameters,
                            CancellationToken.None);

                        _logger.LogInformation(
                            "ResetPassword: Confirmation email sent - UserId={UserId}",
                            user.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
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
                    request.Email,
                    user.Id,
                    stopwatch.ElapsedMilliseconds);

                return Result<ResetPasswordResponse>.Success(response);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "ResetPassword FAILED: Unexpected error - Email={Email}, Duration={ElapsedMs}ms, ErrorMessage={ErrorMessage}",
                    request.Email,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                return Result<ResetPasswordResponse>.Failure($"An unexpected error occurred: {ex.Message}");
            }
        }
    }
}