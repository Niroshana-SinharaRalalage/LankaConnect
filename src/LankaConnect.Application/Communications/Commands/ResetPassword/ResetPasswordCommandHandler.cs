using MediatR;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Application.Communications.Commands.ResetPassword;

/// <summary>
/// Handler for resetting user passwords using reset tokens
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
        try
        {
            // Validate email format
            var emailResult = Email.Create(request.Email);
            if (!emailResult.IsSuccess)
            {
                return Result<ResetPasswordResponse>.Failure("Invalid email format");
            }

            // Get user by email
            var user = await _userRepository.GetByEmailAsync(emailResult.Value, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Password reset attempted for non-existent email: {Email}", request.Email);
                return Result<ResetPasswordResponse>.Failure("Invalid reset token or email");
            }

            // Validate reset token
            if (!user.IsPasswordResetTokenValid(request.Token))
            {
                _logger.LogWarning("Invalid or expired password reset token provided for user {UserId}", user.Id);
                return Result<ResetPasswordResponse>.Failure("Invalid or expired reset token");
            }

            // Validate password strength
            var passwordValidationResult = _passwordHashingService.ValidatePasswordStrength(request.NewPassword);
            if (!passwordValidationResult.IsSuccess)
            {
                return Result<ResetPasswordResponse>.Failure(passwordValidationResult.Error);
            }

            // Hash new password
            var hashResult = _passwordHashingService.HashPassword(request.NewPassword);
            if (!hashResult.IsSuccess)
            {
                return Result<ResetPasswordResponse>.Failure(hashResult.Error);
            }

            // Change password (this will clear the reset token and failed login attempts)
            var changePasswordResult = user.ChangePassword(hashResult.Value);
            if (!changePasswordResult.IsSuccess)
            {
                return Result<ResetPasswordResponse>.Failure(changePasswordResult.Error);
            }

            // Revoke all existing refresh tokens for security
            user.RevokeAllRefreshTokens("Password reset");

            // Save changes
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Password reset successfully for user {UserId}: {Email}", 
                user.Id, user.Email.Value);

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
                        "password-changed-confirmation",
                        user.Email.Value,
                        templateParameters,
                        CancellationToken.None);

                    _logger.LogInformation("Password change confirmation email sent to user {UserId}", user.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send password change confirmation email to user {UserId}", user.Id);
                }
            }, cancellationToken);

            var response = new ResetPasswordResponse(
                user.Id,
                user.Email.Value,
                DateTime.UtcNow);

            return Result<ResetPasswordResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for email {Email}", request.Email);
            return Result<ResetPasswordResponse>.Failure("An error occurred while resetting password");
        }
    }
}