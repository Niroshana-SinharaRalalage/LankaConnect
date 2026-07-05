using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Modules.Notifications.Application.DTOs;
using LankaConnect.BuildingBlocks.Domain;

namespace LankaConnect.Modules.Notifications.Application.Queries.GetUnreadNotifications;

/// <summary>
/// Query to get unread notifications for the current user
/// Phase 6A.6: Notification System
/// </summary>
public record GetUnreadNotificationsQuery : IQuery<IReadOnlyList<NotificationDto>>
{
    // Query with no parameters - uses current user from context
}
