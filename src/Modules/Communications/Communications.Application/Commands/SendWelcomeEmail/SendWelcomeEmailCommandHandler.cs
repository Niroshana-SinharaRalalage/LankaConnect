using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Modules.Identity.Contracts; // W4.7.b
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Services;
using Serilog.Context;
namespace LankaConnect.Modules.Communications.Application.Commands.SendWelcomeEmail;

/// <summary>
/// Handler for sending welcome emails to users
/// Phase 6A.X Observability: Enhanced with comprehensive structured logging
/// Phase 6A.100: Migrated to ITypedEmailService with WelcomeEmailParams
/// </summary>
public class SendWelcomeEmailCommandHandler : IRequestHandler<SendWelcomeEmailCommand, Result<SendWelcomeEmailResponse>>
{
    private readonly IIdentityQueries _identityQueries;
    private readonly ITypedEmailService _typedEmailService;
    private readonly ILogger<SendWelcomeEmailCommandHandler> _logger;

    public SendWelcomeEmailCommandHandler(
        IIdentityQueries identityQueries,
        ITypedEmailService typedEmailService,
        ILogger<SendWelcomeEmailCommandHandler> logger)
    {
        _identityQueries = identityQueries;
        _typedEmailService = typedEmailService;
        _logger = logger;
    }

    public async Task<Result<SendWelcomeEmailResponse>> Handle(SendWelcomeEmailCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "SendWelcomeEmail"))
        using (LogContext.PushProperty("EntityType", "User"))
        using (LogContext.PushProperty("UserId", request.UserId))
        using (LogContext.PushProperty("TriggerType", request.TriggerType))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "SendWelcomeEmail START: UserId={UserId}, TriggerType={TriggerType}",
                request.UserId,
                request.TriggerType);

            try
            {
                // Get user
                var user = await _identityQueries.GetContactInfoAsync(request.UserId, cancellationToken);
                if (user == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "SendWelcomeEmail FAILED: User not found - UserId={UserId}, Duration={ElapsedMs}ms",
                        request.UserId,
                        stopwatch.ElapsedMilliseconds);
                    return Result<SendWelcomeEmailResponse>.Failure("User not found");
                }

                // Check if user is active
                if (!user.IsActive)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "SendWelcomeEmail FAILED: User is inactive - UserId={UserId}, Duration={ElapsedMs}ms",
                        request.UserId,
                        stopwatch.ElapsedMilliseconds);
                    return Result<SendWelcomeEmailResponse>.Failure("Cannot send welcome email to inactive user");
                }

                // For email verification trigger, ensure email is verified
                if (request.TriggerType == WelcomeEmailTrigger.EmailVerification && !user.IsEmailVerified)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "SendWelcomeEmail FAILED: Email not verified for verification trigger - UserId={UserId}, Duration={ElapsedMs}ms",
                        request.UserId,
                        stopwatch.ElapsedMilliseconds);
                    return Result<SendWelcomeEmailResponse>.Failure("Cannot send verification welcome email to unverified user");
                }

                _logger.LogInformation(
                    "SendWelcomeEmail: User found - UserId={UserId}, Email={Email}, Role={Role}",
                    user.Id,
                    user.Email,
                    user.Role);

            // Phase 6A.100: Map command trigger type to typed params enum
            var triggerType = request.TriggerType switch
            {
                WelcomeEmailTrigger.Registration => WelcomeEmailTriggerType.Registration,
                WelcomeEmailTrigger.EmailVerification => WelcomeEmailTriggerType.EmailVerification,
                WelcomeEmailTrigger.AccountActivation => WelcomeEmailTriggerType.AccountActivation,
                WelcomeEmailTrigger.Manual => WelcomeEmailTriggerType.Manual,
                _ => WelcomeEmailTriggerType.Default
            };

            // Phase 6A.100: Use typed email params instead of dictionary
            var emailParams = WelcomeEmailParams.Create(
                userId: user.Id,
                recipientEmail: user.Email,
                userName: user.DisplayName,
                firstName: user.FirstName,
                userEmail: user.Email,
                triggerType: triggerType,
                userRole: user.Role.ToString(),
                isEventOrganizer: user.Role == UserRoleDto.EventOrganizer,
                isAdmin: user.Role == UserRoleDto.Admin || user.Role == UserRoleDto.AdminManager,
                customMessage: request.CustomMessage);

                // Phase 6A.100: Send welcome email using typed email service
                var sendResult = await _typedEmailService.SendEmailAsync(emailParams, cancellationToken);

                if (!sendResult.Success)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "SendWelcomeEmail FAILED: Email send failed - Email={Email}, TriggerType={TriggerType}, Errors={Errors}, Duration={ElapsedMs}ms",
                        user.Email,
                        request.TriggerType,
                        string.Join(", ", sendResult.Errors),
                        stopwatch.ElapsedMilliseconds);
                    return Result<SendWelcomeEmailResponse>.Failure("Failed to send welcome email");
                }

                var response = new SendWelcomeEmailResponse(
                    user.Id,
                    user.Email,
                    request.TriggerType,
                    DateTime.UtcNow);

                stopwatch.Stop();
                _logger.LogInformation(
                    "SendWelcomeEmail COMPLETE: Email={Email}, UserId={UserId}, TriggerType={TriggerType}, Duration={ElapsedMs}ms",
                    user.Email,
                    user.Id,
                    request.TriggerType,
                    stopwatch.ElapsedMilliseconds);

                return Result<SendWelcomeEmailResponse>.Success(response);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "SendWelcomeEmail FAILED: Unexpected error - UserId={UserId}, TriggerType={TriggerType}, Duration={ElapsedMs}ms, ErrorMessage={ErrorMessage}",
                    request.UserId,
                    request.TriggerType,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                throw;
            }
        }
    }
}