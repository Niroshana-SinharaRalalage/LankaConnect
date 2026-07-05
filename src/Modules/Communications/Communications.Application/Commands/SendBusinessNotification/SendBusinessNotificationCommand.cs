using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Shared.Email.Contracts;
namespace LankaConnect.Modules.Communications.Application.Commands.SendBusinessNotification;

/// <summary>
/// Command to send business-related notification emails
/// </summary>
/// <param name="BusinessId">The ID of the business related to the notification</param>
/// <param name="UserId">The ID of the user to notify (business owner or admin)</param>
/// <param name="NotificationType">The type of business notification</param>
/// <param name="Subject">Custom subject for the notification</param>
/// <param name="Data">Additional data specific to the notification type</param>
public record SendBusinessNotificationCommand(
    Guid BusinessId,
    Guid UserId,
    BusinessNotificationType NotificationType,
    string Subject,
    Dictionary<string, object>? Data = null) : ICommand<SendBusinessNotificationResponse>;

/// <summary>
/// Response for send business notification command
/// </summary>
public class SendBusinessNotificationResponse
{
    public Guid BusinessId { get; init; }
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public BusinessNotificationType NotificationType { get; init; }
    public string Subject { get; init; } = string.Empty;
    public DateTime SentAt { get; init; }

    public SendBusinessNotificationResponse(Guid businessId, Guid userId, string email,
        BusinessNotificationType notificationType, string subject, DateTime sentAt)
    {
        BusinessId = businessId;
        UserId = userId;
        Email = email;
        NotificationType = notificationType;
        Subject = subject;
        SentAt = sentAt;
    }
}

// Note: BusinessNotificationType is now defined in LankaConnect.Shared.Email.Contracts