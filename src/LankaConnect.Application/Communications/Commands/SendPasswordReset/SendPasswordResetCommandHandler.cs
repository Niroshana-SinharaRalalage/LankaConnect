using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Common.Constants;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;
using Serilog.Context;

namespace LankaConnect.Application.Communications.Commands.SendPasswordReset;

/// <summary>
/// Handler for sending password reset emails
/// Phase 6A.X Observability: Enhanced with comprehensive structured logging
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
                // Validate email format
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

                // Get user by email
                var user = await _userRepository.GetByEmailAsync(emailResult.Value, cancellationToken);

                // For security reasons, we always return success even if user doesn't exist
                // but we log the attempt and don't actually send an email
                if (user == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "SendPasswordReset: User not found (security: returning success) - Email={Email}, Duration={ElapsedMs}ms",
                        request.Email,
                        stopwatch.ElapsedMilliseconds);

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
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "SendPasswordReset FAILED: Account is locked - Email={Email}, UserId={UserId}, Duration={ElapsedMs}ms",
                        request.Email,
                        user.Id,
                        stopwatch.ElapsedMilliseconds);
                    return Result<SendPasswordResetResponse>.Failure("Account is temporarily locked. Please try again later.");
                }

                // Check if recently sent (within last 5 minutes) unless forcing resend
                if (!request.ForceResend && user.PasswordResetTokenExpiresAt.HasValue)
                {
                    var tokenCreatedAt = user.PasswordResetTokenExpiresAt.Value.AddHours(-1); // Tokens expire after 1 hour
                    if (DateTime.UtcNow.Subtract(tokenCreatedAt).TotalMinutes < 5)
                    {
                        stopwatch.Stop();
                        _logger.LogInformation(
                            "SendPasswordReset: Recently sent, skipping resend - Email={Email}, UserId={UserId}, Duration={ElapsedMs}ms",
                            request.Email,
                            user.Id,
                            stopwatch.ElapsedMilliseconds);

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
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "SendPasswordReset FAILED: Set token failed - Email={Email}, UserId={UserId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.Email,
                        user.Id,
                        setTokenResult.Error,
                        stopwatch.ElapsedMilliseconds);
                    return Result<SendPasswordResetResponse>.Failure(setTokenResult.Error);
                }

                _logger.LogInformation(
                    "SendPasswordReset: Reset token generated - Email={Email}, UserId={UserId}, ExpiresAt={ExpiresAt}",
                    request.Email,
                    user.Id,
                    tokenExpiresAt);

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
                    EmailTemplateNames.PasswordReset,
                    user.Email.Value,
                    templateParameters,
                    cancellationToken);

                if (!sendResult.IsSuccess)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "SendPasswordReset FAILED: Email send failed - Email={Email}, UserId={UserId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.Email,
                        user.Id,
                        sendResult.Error,
                        stopwatch.ElapsedMilliseconds);
                    return Result<SendPasswordResetResponse>.Failure("Failed to send password reset email");
                }

                // Save user with new token
                await _unitOfWork.CommitAsync(cancellationToken);

                var successResponse = new SendPasswordResetResponse(
                    user.Id,
                    request.Email,
                    tokenExpiresAt);

                stopwatch.Stop();
                _logger.LogInformation(
                    "SendPasswordReset COMPLETE: Email={Email}, UserId={UserId}, ExpiresAt={ExpiresAt}, Duration={ElapsedMs}ms",
                    request.Email,
                    user.Id,
                    tokenExpiresAt,
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
                return Result<SendPasswordResetResponse>.Failure($"An unexpected error occurred: {ex.Message}");
            }
        }
    }
}