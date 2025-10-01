using MediatR;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Communications.Commands.SendWelcomeEmail;

/// <summary>
/// Handler for sending welcome emails to users
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
        try
        {
            // Get user
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                return Result<SendWelcomeEmailResponse>.Failure("User not found");
            }

            // Check if user is active
            if (!user.IsActive)
            {
                return Result<SendWelcomeEmailResponse>.Failure("Cannot send welcome email to inactive user");
            }

            // For email verification trigger, ensure email is verified
            if (request.TriggerType == WelcomeEmailTrigger.EmailVerification && !user.IsEmailVerified)
            {
                return Result<SendWelcomeEmailResponse>.Failure("Cannot send verification welcome email to unverified user");
            }

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
            templateParameters.Add("IsBusinessUser", user.Role == Domain.Users.Enums.UserRole.BusinessOwner);
            templateParameters.Add("IsAdmin", user.Role == Domain.Users.Enums.UserRole.Admin);

            // Send welcome email
            var sendResult = await _emailService.SendTemplatedEmailAsync(
                templateName,
                user.Email.Value,
                templateParameters,
                cancellationToken);

            if (!sendResult.IsSuccess)
            {
                _logger.LogError("Failed to send welcome email to {Email} for trigger {TriggerType}: {Error}", 
                    user.Email.Value, request.TriggerType, sendResult.Error);
                return Result<SendWelcomeEmailResponse>.Failure("Failed to send welcome email");
            }

            _logger.LogInformation("Welcome email sent successfully to {Email} for user {UserId}, trigger: {TriggerType}", 
                user.Email.Value, user.Id, request.TriggerType);

            var response = new SendWelcomeEmailResponse(
                user.Id,
                user.Email.Value,
                request.TriggerType,
                DateTime.UtcNow);

            return Result<SendWelcomeEmailResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending welcome email for user {UserId}, trigger: {TriggerType}", 
                request.UserId, request.TriggerType);
            return Result<SendWelcomeEmailResponse>.Failure("An error occurred while sending welcome email");
        }
    }
}