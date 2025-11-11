using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Notifications.Commands.MarkAllNotificationsAsRead;

/// <summary>
/// Command to mark all notifications as read for the current user
/// Phase 6A.6: Notification System
/// </summary>
public record MarkAllNotificationsAsReadCommand : ICommand;
