using MediatR;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Application.Communications.Commands.SendPasswordReset;

/// <summary>
/// Handler for sending password reset emails
/// </summary>
public class SendPasswordResetCommandHandler : IRequestHandler<SendPasswordResetCommand, Result<SendPasswordResetResponse>>
{
    private readonly LankaConnect.Domain.Users.IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SendPasswordResetCommandHandler> _logger;

    public SendPasswordResetCommandHandler(
        LankaConnect.Domain.Users.IUserRepository userRepository,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        IUnitOfWork unitOfWork,
        ILogger<SendPasswordResetCommandHandler> logger)
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<SendPasswordResetResponse>> Handle(SendPasswordResetCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate email format
            var emailResult = Email.Create(request.Email);
            if (!emailResult.IsSuccess)
            {
                return Result<SendPasswordResetResponse>.Failure("Invalid email format");
            }

            // Get user by email
            var user = await _userRepository.GetByEmailAsync(emailResult.Value, cancellationToken);
            
            // For security reasons, we always return success even if user doesn't exist
            // but we log the attempt and don't actually send an email
            if (user == null)
            {
                _logger.LogWarning("Password reset requested for non-existent email: {Email}", request.Email);
                
                // Return a success response but indicate user was not found internally
                var notFoundResponse = new SendPasswordResetResponse(
                    Guid.Empty,
                    request.Email,
                    DateTime.UtcNow.AddHours(1), // Fake expiry for consistency
                    userNotFound: true);

                return Result<SendPasswordResetResponse>.Success(notFoundResponse);
            }

            // Check if user account is locked
            if (user.IsAccountLocked)
            {
                _logger.LogWarning("Password reset requested for locked account: {Email}", request.Email);
                return Result<SendPasswordResetResponse>.Failure("Account is temporarily locked. Please try again later.");
            }

            // Check if recently sent (within last 5 minutes) unless forcing resend
            if (!request.ForceResend && user.PasswordResetTokenExpiresAt.HasValue)
            {
                var tokenCreatedAt = user.PasswordResetTokenExpiresAt.Value.AddHours(-1); // Tokens expire after 1 hour
                if (DateTime.UtcNow.Subtract(tokenCreatedAt).TotalMinutes < 5)
                {
                    var recentResponse = new SendPasswordResetResponse(
                        user.Id,
                        request.Email,
                        user.PasswordResetTokenExpiresAt.Value,
                        wasRecentlySent: true);

                    return Result<SendPasswordResetResponse>.Success(recentResponse);
                }
            }

            // Generate new reset token
            var resetToken = Guid.NewGuid().ToString("N");
            var tokenExpiresAt = DateTime.UtcNow.AddHours(1); // Short expiry for security

            // Set the token on user
            var setTokenResult = user.SetPasswordResetToken(resetToken, tokenExpiresAt);
            if (!setTokenResult.IsSuccess)
            {
                return Result<SendPasswordResetResponse>.Failure(setTokenResult.Error);
            }

            // Prepare template parameters
            var templateParameters = new Dictionary<string, object>
            {
                { "UserName", user.FullName },
                { "UserEmail", user.Email.Value },
                { "ResetToken", resetToken },
                { "ResetLink", $"https://lankaconnect.com/reset-password?token={resetToken}" },
                { "ExpiresAt", tokenExpiresAt.ToString("yyyy-MM-dd HH:mm:ss UTC") },
                { "CompanyName", "LankaConnect" },
                { "SupportEmail", "support@lankaconnect.com" }
            };

            // Send password reset email
            var sendResult = await _emailService.SendTemplatedEmailAsync(
                "password-reset",
                user.Email.Value,
                templateParameters,
                cancellationToken);

            if (!sendResult.IsSuccess)
            {
                _logger.LogError("Failed to send password reset email to {Email}: {Error}", 
                    request.Email, sendResult.Error);
                return Result<SendPasswordResetResponse>.Failure("Failed to send password reset email");
            }

            // Save user with new token
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("Password reset email sent successfully to {Email} for user {UserId}", 
                request.Email, user.Id);

            var successResponse = new SendPasswordResetResponse(
                user.Id,
                request.Email,
                tokenExpiresAt);

            return Result<SendPasswordResetResponse>.Success(successResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending password reset email for {Email}", request.Email);
            return Result<SendPasswordResetResponse>.Failure("An error occurred while sending password reset email");
        }
    }
}