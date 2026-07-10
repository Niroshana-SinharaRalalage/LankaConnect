using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;

namespace LankaConnect.Modules.Notifications.Application.Commands.MarkAllNotificationsAsRead;

/// <summary>
/// Command to mark all notifications as read for the current user
/// Phase 6A.6: Notification System
/// </summary>
public record MarkAllNotificationsAsReadCommand : ICommand;
