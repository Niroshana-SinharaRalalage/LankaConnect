using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared.ValueObjects;
using Serilog.Context;

namespace LankaConnect.Application.Communications.Commands.VerifyEmail;

/// <summary>
/// Handler for verifying user email addresses
/// Phase 6A.53: Token-only verification aligned with password reset pattern
/// Phase 6A.X Observability: Enhanced with comprehensive structured logging
/// </summary>
public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, Result<VerifyEmailResponse>>
{
    private readonly LankaConnect.Domain.Users.IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<VerifyEmailCommandHandler> _logger;

    public VerifyEmailCommandHandler(
        LankaConnect.Domain.Users.IUserRepository userRepository,
        IEmailService emailService,
        IUnitOfWork unitOfWork,
        ILogger<VerifyEmailCommandHandler> logger)
    {
        _userRepository = userRepository;
        _emailService = emailService;
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

                // Send welcome email asynchronously (fire and forget)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var templateParameters = new Dictionary<string, object>
                        {
                            { "UserName", user.FullName },
                            { "UserEmail", user.Email.Value },
                            { "CompanyName", "LankaConnect" },
                            { "LoginUrl", "https://lankaconnect.com/login" }
                        };

                        await _emailService.SendTemplatedEmailAsync(
                            "template-welcome",
                            user.Email.Value,
                            templateParameters,
                            CancellationToken.None);

                        _logger.LogInformation(
                            "VerifyEmail: Welcome email sent - UserId={UserId}",
                            user.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "VerifyEmail: Failed to send welcome email - UserId={UserId}, ErrorMessage={ErrorMessage}",
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