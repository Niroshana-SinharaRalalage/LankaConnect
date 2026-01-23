using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using Serilog.Context;

namespace LankaConnect.Application.Communications.Commands.SendWelcomeEmail;

/// <summary>
/// Handler for sending welcome emails to users
/// Phase 6A.X Observability: Enhanced with comprehensive structured logging
/// </summary>
public class SendWelcomeEmailCommandHandler : IRequestHandler<SendWelcomeEmailCommand, Result<SendWelcomeEmailResponse>>
{
    private readonly LankaConnect.Domain.Users.IUserRepository _userRepository;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly ILogger<SendWelcomeEmailCommandHandler> _logger;

    public SendWelcomeEmailCommandHandler(
        LankaConnect.Domain.Users.IUserRepository userRepository,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        ILogger<SendWelcomeEmailCommandHandler> logger)
    {
        _userRepository = userRepository;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
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
                var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
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
                    user.Email.Value,
                    user.Role);

            // Determine template based on trigger type
            var templateName = request.TriggerType switch
            {
                WelcomeEmailTrigger.Registration => "welcome-registration",
                WelcomeEmailTrigger.EmailVerification => "welcome-verification",
                WelcomeEmailTrigger.AccountActivation => "welcome-activation",
                WelcomeEmailTrigger.Manual => "welcome-manual",
                _ => "welcome-default"
            };

            // Prepare template parameters
            var templateParameters = new Dictionary<string, object>
            {
                { "UserName", user.FullName },
                { "FirstName", user.FirstName },
                { "UserEmail", user.Email.Value },
                { "CompanyName", "LankaConnect" },
                { "LoginUrl", "https://lankaconnect.com/login" },
                { "DashboardUrl", "https://lankaconnect.com/dashboard" },
                { "SupportEmail", "support@lankaconnect.com" },
                { "TriggerType", request.TriggerType.ToString() },
                { "WelcomeDate", DateTime.UtcNow.ToString("yyyy-MM-dd") }
            };

            // Add custom message if provided
            if (!string.IsNullOrWhiteSpace(request.CustomMessage))
            {
                templateParameters.Add("CustomMessage", request.CustomMessage);
                templateParameters.Add("HasCustomMessage", true);
            }
            else
            {
                templateParameters.Add("HasCustomMessage", false);
            }

            // Add role-specific content
            templateParameters.Add("UserRole", user.Role.ToString());
            templateParameters.Add("IsEventOrganizer", user.Role == Domain.Users.Enums.UserRole.EventOrganizer);
            templateParameters.Add("IsAdmin", user.Role == Domain.Users.Enums.UserRole.Admin || user.Role == Domain.Users.Enums.UserRole.AdminManager);

                // Send welcome email
                var sendResult = await _emailService.SendTemplatedEmailAsync(
                    templateName,
                    user.Email.Value,
                    templateParameters,
                    cancellationToken);

                if (!sendResult.IsSuccess)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "SendWelcomeEmail FAILED: Email send failed - Email={Email}, TriggerType={TriggerType}, Error={Error}, Duration={ElapsedMs}ms",
                        user.Email.Value,
                        request.TriggerType,
                        sendResult.Error,
                        stopwatch.ElapsedMilliseconds);
                    return Result<SendWelcomeEmailResponse>.Failure("Failed to send welcome email");
                }

                var response = new SendWelcomeEmailResponse(
                    user.Id,
                    user.Email.Value,
                    request.TriggerType,
                    DateTime.UtcNow);

                stopwatch.Stop();
                _logger.LogInformation(
                    "SendWelcomeEmail COMPLETE: Email={Email}, UserId={UserId}, TriggerType={TriggerType}, Duration={ElapsedMs}ms",
                    user.Email.Value,
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