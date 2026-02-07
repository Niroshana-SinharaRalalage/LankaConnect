using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Common.Constants;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Services;
using Serilog.Context;

namespace LankaConnect.Application.Communications.Commands.VerifyEmail;

/// <summary>
/// Handler for verifying user email addresses
/// Phase 6A.53: Token-only verification aligned with password reset pattern
/// Phase 6A.X Observability: Enhanced with comprehensive structured logging
/// Phase 6A.100: Migrated to ITypedEmailService with WelcomeEmailParams
/// </summary>
public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, Result<VerifyEmailResponse>>
{
    private readonly LankaConnect.Domain.Users.IUserRepository _userRepository;
    private readonly ITypedEmailService _typedEmailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<VerifyEmailCommandHandler> _logger;

    public VerifyEmailCommandHandler(
        LankaConnect.Domain.Users.IUserRepository userRepository,
        ITypedEmailService typedEmailService,
        IUnitOfWork unitOfWork,
        ILogger<VerifyEmailCommandHandler> logger)
    {
        _userRepository = userRepository;
        _typedEmailService = typedEmailService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<VerifyEmailResponse>> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "VerifyEmail"))
        using (LogContext.PushProperty("EntityType", "User"))
        using (LogContext.PushProperty("Token", request.Token?.Substring(0, Math.Min(8, request.Token?.Length ?? 0))))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "VerifyEmail START: Token={TokenPreview}",
                request.Token?.Substring(0, Math.Min(8, request.Token?.Length ?? 0)));

            try
            {
                // Check for cancellation at the start
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(request.Token))
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "VerifyEmail FAILED: Empty token - Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                    return Result<VerifyEmailResponse>.Failure("Invalid or expired verification token");
                }

                // Phase 6A.53: Get user by verification token (token-only lookup)
                // Aligns with password reset pattern (GetByPasswordResetTokenAsync)
                var user = await _userRepository.GetByEmailVerificationTokenAsync(request.Token, cancellationToken);
                if (user == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "VerifyEmail FAILED: Invalid or expired token - Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                    return Result<VerifyEmailResponse>.Failure("Invalid or expired verification token");
                }

                _logger.LogInformation(
                    "VerifyEmail: User found by token - UserId={UserId}, Email={Email}, IsVerified={IsVerified}",
                    user.Id,
                    user.Email.Value,
                    user.IsEmailVerified);

                // Check if already verified
                if (user.IsEmailVerified)
                {
                    stopwatch.Stop();
                    _logger.LogInformation(
                        "VerifyEmail: Email already verified - UserId={UserId}, Email={Email}, Duration={ElapsedMs}ms",
                        user.Id,
                        user.Email.Value,
                        stopwatch.ElapsedMilliseconds);

                    var alreadyVerifiedResponse = new VerifyEmailResponse(
                        user.Id,
                        user.Email.Value,
                        DateTime.UtcNow,
                        wasAlreadyVerified: true);

                    return Result<VerifyEmailResponse>.Success(alreadyVerifiedResponse);
                }

                // Phase 6A.53: Verify email with token validation (moved into VerifyEmail method)
                var verifyResult = user.VerifyEmail(request.Token);
                if (!verifyResult.IsSuccess)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "VerifyEmail FAILED: Verification failed - UserId={UserId}, Error={Error}, Duration={ElapsedMs}ms",
                        user.Id,
                        verifyResult.Error,
                        stopwatch.ElapsedMilliseconds);
                    return Result<VerifyEmailResponse>.Failure(verifyResult.Error);
                }

                // Save changes
                await _unitOfWork.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "VerifyEmail: Email verification successful - UserId={UserId}, Email={Email}",
                    user.Id,
                    user.Email.Value);

                // Phase 6A.100: Send welcome email asynchronously (fire and forget) using typed email service
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var emailParams = WelcomeEmailParams.Create(
                            userId: user.Id,
                            recipientEmail: user.Email.Value,
                            userName: user.FullName,
                            firstName: user.FirstName,
                            userEmail: user.Email.Value,
                            triggerType: WelcomeEmailTriggerType.EmailVerification);

                        var result = await _typedEmailService.SendEmailAsync(emailParams, CancellationToken.None);

                        if (result.Success)
                        {
                            _logger.LogInformation(
                                "[Phase 6A.100] VerifyEmail: Welcome email sent - UserId={UserId}",
                                user.Id);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "[Phase 6A.100] VerifyEmail: Welcome email failed - UserId={UserId}, Errors={Errors}",
                                user.Id, string.Join(", ", result.Errors));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "[Phase 6A.100] VerifyEmail: Failed to send welcome email - UserId={UserId}, ErrorMessage={ErrorMessage}",
                            user.Id,
                            ex.Message);
                    }
                }, cancellationToken);

                var response = new VerifyEmailResponse(
                    user.Id,
                    user.Email.Value,
                    DateTime.UtcNow);

                stopwatch.Stop();
                _logger.LogInformation(
                    "VerifyEmail COMPLETE: UserId={UserId}, Email={Email}, Duration={ElapsedMs}ms",
                    user.Id,
                    user.Email.Value,
                    stopwatch.ElapsedMilliseconds);

                return Result<VerifyEmailResponse>.Success(response);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "VerifyEmail FAILED: Unexpected error - Token={TokenPreview}, Duration={ElapsedMs}ms, ErrorMessage={ErrorMessage}",
                    request.Token?.Substring(0, Math.Min(8, request.Token?.Length ?? 0)),
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                throw;
            }
        }
    }
}