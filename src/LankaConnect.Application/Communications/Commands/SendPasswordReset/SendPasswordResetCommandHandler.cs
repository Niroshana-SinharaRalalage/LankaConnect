using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Common.Constants;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Services;
using Serilog.Context;

namespace LankaConnect.Application.Communications.Commands.SendPasswordReset;

/// <summary>
/// Handler for sending password reset emails
/// Phase 6A.X Observability: Enhanced with comprehensive structured logging
/// Phase 6A.87: Migrated to ITypedEmailService for hybrid email support
/// </summary>
public class SendPasswordResetCommandHandler : IRequestHandler<SendPasswordResetCommand, Result<SendPasswordResetResponse>>
{
    private readonly LankaConnect.Domain.Users.IUserRepository _userRepository;
    private readonly ITypedEmailService _typedEmailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SendPasswordResetCommandHandler> _logger;

    public SendPasswordResetCommandHandler(
        LankaConnect.Domain.Users.IUserRepository userRepository,
        ITypedEmailService typedEmailService,
        IEmailTemplateService emailTemplateService,
        IEmailUrlHelper emailUrlHelper,
        IUnitOfWork unitOfWork,
        ILogger<SendPasswordResetCommandHandler> logger)
    {
        _userRepository = userRepository;
        _typedEmailService = typedEmailService;
        _emailTemplateService = emailTemplateService;
        _emailUrlHelper = emailUrlHelper;
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

                // Phase 6A.101: CRITICAL FIX - Commit to database FIRST, before sending email
                // This prevents DbUpdateConcurrencyException when email send takes 5-10 seconds
                // and another process modifies the user record during that time
                await _unitOfWork.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "SendPasswordReset: Token saved to database - Email={Email}, UserId={UserId}",
                    request.Email,
                    user.Id);

                // Phase 6A.87: Use typed email parameters for compile-time safety
                // Phase 6A.101: Use IEmailUrlHelper for environment-aware URL building
                var emailParams = PasswordResetEmailParams.Create(
                    userId: user.Id,
                    userName: user.FullName,
                    userEmail: user.Email.Value,
                    resetToken: resetToken,
                    resetLink: _emailUrlHelper.BuildPasswordResetUrl(resetToken),
                    expiresAt: tokenExpiresAt
                );

                // Phase 6A.100: Send via typed email service
                // Phase 6A.101: Email sent AFTER database commit - if email fails, user can retry
                var typedResult = await _typedEmailService.SendEmailAsync(
                    emailParams,
                    cancellationToken);

                if (!typedResult.Success)
                {
                    stopwatch.Stop();
                    // Note: Token is already saved, user can retry password reset
                    _logger.LogWarning(
                        "SendPasswordReset: Email send failed (token saved, user can retry) - Email={Email}, UserId={UserId}, Errors={Errors}, Duration={ElapsedMs}ms",
                        request.Email,
                        user.Id,
                        string.Join(", ", typedResult.Errors),
                        stopwatch.ElapsedMilliseconds);
                    return Result<SendPasswordResetResponse>.Failure("Failed to send password reset email. Please try again.");
                }

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
                return Result<SendPasswordResetResponse>.Failure("An error occurred while sending password reset email");
            }
        }
    }
}