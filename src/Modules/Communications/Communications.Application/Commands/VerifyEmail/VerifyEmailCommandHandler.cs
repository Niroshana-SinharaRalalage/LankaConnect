using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using LankaConnect.Domain.Common;
using LankaConnect.Modules.Identity.Contracts;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Services;
using Serilog.Context;
namespace LankaConnect.Modules.Communications.Application.Commands.VerifyEmail;

/// <summary>
/// Handler for verifying user email addresses.
/// Wave 4.7.b (2026-06-25): swapped IUserRepository + IUnitOfWork to
/// IIdentityCommands.CompleteEmailVerificationAsync. The Identity-side adapter
/// owns token validation, the User aggregate MarkEmailAsVerified transition,
/// and persistence. The caller still owns: empty-token guard, response shaping,
/// fire-and-forget welcome email side-effect.
/// </summary>
public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, Result<VerifyEmailResponse>>
{
    private readonly IIdentityCommands _identityCommands;
    private readonly ITypedEmailService _typedEmailService;
    private readonly ILogger<VerifyEmailCommandHandler> _logger;

    public VerifyEmailCommandHandler(
        IIdentityCommands identityCommands,
        ITypedEmailService typedEmailService,
        ILogger<VerifyEmailCommandHandler> logger)
    {
        _identityCommands = identityCommands;
        _typedEmailService = typedEmailService;
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
                cancellationToken.ThrowIfCancellationRequested();

                if (string.IsNullOrWhiteSpace(request.Token))
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "VerifyEmail FAILED: Empty token - Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                    return Result<VerifyEmailResponse>.Failure("Invalid or expired verification token");
                }

                EmailVerificationCompletedDto completed;
                try
                {
                    completed = await _identityCommands.CompleteEmailVerificationAsync(
                        request.Token,
                        cancellationToken);
                }
                catch (InvalidOperationException ex)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(ex,
                        "VerifyEmail FAILED: Identity rejected completion - Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                    return Result<VerifyEmailResponse>.Failure("Invalid or expired verification token");
                }

                _logger.LogInformation(
                    "VerifyEmail: Identity completed - UserId={UserId}, Email={Email}",
                    completed.UserId,
                    completed.Email);

                var capturedDto = completed;
                var capturedEmailService = _typedEmailService;
                var capturedLogger = _logger;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var emailParams = WelcomeEmailParams.Create(
                            userId: capturedDto.UserId,
                            recipientEmail: capturedDto.Email,
                            userName: capturedDto.DisplayName,
                            firstName: capturedDto.DisplayName,
                            userEmail: capturedDto.Email,
                            triggerType: WelcomeEmailTriggerType.EmailVerification);

                        var result = await capturedEmailService.SendEmailAsync(emailParams, CancellationToken.None);

                        if (result.Success)
                        {
                            capturedLogger.LogInformation(
                                "VerifyEmail: Welcome email sent - UserId={UserId}",
                                capturedDto.UserId);
                        }
                        else
                        {
                            capturedLogger.LogWarning(
                                "VerifyEmail: Welcome email failed - UserId={UserId}, Errors={Errors}",
                                capturedDto.UserId, string.Join(", ", result.Errors));
                        }
                    }
                    catch (Exception ex)
                    {
                        capturedLogger.LogError(ex,
                            "VerifyEmail: Failed to send welcome email - UserId={UserId}, ErrorMessage={ErrorMessage}",
                            capturedDto.UserId,
                            ex.Message);
                    }
                }, cancellationToken);

                var response = new VerifyEmailResponse(
                    completed.UserId,
                    completed.Email,
                    DateTime.UtcNow);

                stopwatch.Stop();
                _logger.LogInformation(
                    "VerifyEmail COMPLETE: UserId={UserId}, Email={Email}, Duration={ElapsedMs}ms",
                    completed.UserId,
                    completed.Email,
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
