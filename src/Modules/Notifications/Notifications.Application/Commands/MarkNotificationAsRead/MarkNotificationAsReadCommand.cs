using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;

namespace LankaConnect.Modules.Notifications.Application.Commands.MarkNotificationAsRead;

/// <summary>
/// Command to mark a notification as read
/// Phase 6A.6: Notification System
/// </summary>
public record MarkNotificationAsReadCommand(Guid NotificationId) : ICommand;
