using MediatR;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Communications.Commands.SendBusinessNotification;

/// <summary>
/// Handler for sending business notification emails
/// </summary>
public class SendBusinessNotificationCommandHandler : IRequestHandler<SendBusinessNotificationCommand, Result<SendBusinessNotificationResponse>>
{
    private readonly LankaConnect.Domain.Users.IUserRepository _userRepository;
    private readonly IBusinessRepository _businessRepository;
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly ILogger<SendBusinessNotificationCommandHandler> _logger;

    public SendBusinessNotificationCommandHandler(
        LankaConnect.Domain.Users.IUserRepository userRepository,
        IBusinessRepository businessRepository,
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        ILogger<SendBusinessNotificationCommandHandler> logger)
    {
        _userRepository = userRepository;
        _businessRepository = businessRepository;
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _logger = logger;
    }

    public async Task<Result<SendBusinessNotificationResponse>> Handle(SendBusinessNotificationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Get user
            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
            if (user == null)
            {
                return Result<SendBusinessNotificationResponse>.Failure("User not found");
            }

            // Check if user is active
            if (!user.IsActive)
            {
                return Result<SendBusinessNotificationResponse>.Failure("Cannot send notification to inactive user");
            }

            // Get business
            var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
            if (business == null)
            {
                return Result<SendBusinessNotificationResponse>.Failure("Business not found");
            }

            // Determine template based on notification type
            var templateName = GetTemplateNameForNotificationType(request.NotificationType);

            // Prepare base template parameters
            var templateParameters = new Dictionary<string, object>
            {
                { "UserName", user.FullName },
                { "FirstName", user.FirstName },
                { "UserEmail", user.Email.Value },
                { "BusinessName", business.Profile.Name },
                { "BusinessId", business.Id },
                { "BusinessCategory", business.Category.ToString() },
                { "BusinessStatus", business.Status.ToString() },
                { "NotificationType", request.NotificationType.ToString() },
                { "Subject", request.Subject },
                { "CompanyName", "LankaConnect" },
                { "NotificationDate", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC") },
                { "DashboardUrl", "https://lankaconnect.com/dashboard" },
                { "BusinessUrl", $"https://lankaconnect.com/business/{business.Id}" },
                { "SupportEmail", "support@lankaconnect.com" }
            };

            // Add notification-specific data
            if (request.Data != null)
            {
                foreach (var kvp in request.Data)
                {
                    templateParameters[kvp.Key] = kvp.Value;
                }
            }

            // Add notification-specific parameters
            AddNotificationSpecificParameters(templateParameters, request.NotificationType, business);

            // Send notification email with high priority for critical notifications
            var emailMessage = new EmailMessageDto
            {
                ToEmail = user.Email.Value,
                ToName = user.FullName,
                Subject = request.Subject,
                Priority = GetEmailPriorityForNotificationType(request.NotificationType)
            };

            // Render template and set email body
            var renderResult = await _emailTemplateService.RenderTemplateAsync(templateName, templateParameters, cancellationToken);
            if (!renderResult.IsSuccess)
            {
                _logger.LogError("Failed to render template {TemplateName} for notification {NotificationType}: {Error}", 
                    templateName, request.NotificationType, renderResult.Error);
                return Result<SendBusinessNotificationResponse>.Failure("Failed to render email template");
            }

            emailMessage.HtmlBody = renderResult.Value.HtmlBody;
            emailMessage.PlainTextBody = renderResult.Value.PlainTextBody;

            // Send email
            var sendResult = await _emailService.SendEmailAsync(emailMessage, cancellationToken);
            if (!sendResult.IsSuccess)
            {
                _logger.LogError("Failed to send business notification to {Email} for business {BusinessId}: {Error}", 
                    user.Email.Value, request.BusinessId, sendResult.Error);
                return Result<SendBusinessNotificationResponse>.Failure("Failed to send notification email");
            }

            _logger.LogInformation("Business notification sent successfully to {Email} for business {BusinessId}, type: {NotificationType}", 
                user.Email.Value, request.BusinessId, request.NotificationType);

            var response = new SendBusinessNotificationResponse(
                request.BusinessId,
                request.UserId,
                user.Email.Value,
                request.NotificationType,
                request.Subject,
                DateTime.UtcNow);

            return Result<SendBusinessNotificationResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending business notification for business {BusinessId}, user {UserId}, type: {NotificationType}", 
                request.BusinessId, request.UserId, request.NotificationType);
            return Result<SendBusinessNotificationResponse>.Failure("An error occurred while sending business notification");
        }
    }

    private static string GetTemplateNameForNotificationType(BusinessNotificationType notificationType)
    {
        return notificationType switch
        {
            BusinessNotificationType.BusinessCreated => "business-created",
            BusinessNotificationType.BusinessUpdated => "business-updated",
            BusinessNotificationType.BusinessApproved => "business-approved",
            BusinessNotificationType.BusinessRejected => "business-rejected",
            BusinessNotificationType.BusinessSuspended => "business-suspended",
            BusinessNotificationType.BusinessReactivated => "business-reactivated",
            BusinessNotificationType.NewReview => "business-new-review",
            BusinessNotificationType.ReviewResponse => "business-review-response",
            BusinessNotificationType.ServiceAdded => "business-service-added",
            BusinessNotificationType.ServiceUpdated => "business-service-updated",
            BusinessNotificationType.PaymentReceived => "business-payment-received",
            BusinessNotificationType.PaymentFailed => "business-payment-failed",
            BusinessNotificationType.SubscriptionExpiring => "business-subscription-expiring",
            BusinessNotificationType.SubscriptionRenewed => "business-subscription-renewed",
            BusinessNotificationType.PerformanceReport => "business-performance-report",
            _ => "business-notification-default"
        };
    }

    private static int GetEmailPriorityForNotificationType(BusinessNotificationType notificationType)
    {
        return notificationType switch
        {
            BusinessNotificationType.BusinessRejected => 1, // High priority
            BusinessNotificationType.BusinessSuspended => 1, // High priority
            BusinessNotificationType.PaymentFailed => 1, // High priority
            BusinessNotificationType.SubscriptionExpiring => 1, // High priority
            BusinessNotificationType.BusinessApproved => 2, // Normal priority
            BusinessNotificationType.NewReview => 2, // Normal priority
            BusinessNotificationType.PaymentReceived => 2, // Normal priority
            BusinessNotificationType.SubscriptionRenewed => 2, // Normal priority
            _ => 3 // Low priority
        };
    }

    private static void AddNotificationSpecificParameters(Dictionary<string, object> parameters, 
        BusinessNotificationType notificationType, Domain.Business.Business business)
    {
        switch (notificationType)
        {
            case BusinessNotificationType.BusinessApproved:
                parameters.Add("IsApproval", true);
                parameters.Add("NextSteps", "Your business is now live on LankaConnect!");
                break;
            case BusinessNotificationType.BusinessRejected:
                parameters.Add("IsRejection", true);
                parameters.Add("NextSteps", "Please review and update your business information.");
                break;
            case BusinessNotificationType.BusinessSuspended:
                parameters.Add("IsSuspension", true);
                parameters.Add("IsCritical", true);
                break;
            case BusinessNotificationType.NewReview:
                parameters.Add("RequiresAction", true);
                parameters.Add("ActionUrl", $"https://lankaconnect.com/business/{business.Id}/reviews");
                break;
            case BusinessNotificationType.PerformanceReport:
                parameters.Add("IsReport", true);
                parameters.Add("ReportUrl", $"https://lankaconnect.com/business/{business.Id}/analytics");
                break;
        }
    }
}