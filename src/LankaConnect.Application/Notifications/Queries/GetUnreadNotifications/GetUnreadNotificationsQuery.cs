using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Notifications.DTOs;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Notifications.Queries.GetUnreadNotifications;

/// <summary>
/// Query to get unread notifications for the current user
/// Phase 6A.6: Notification System
/// </summary>
public record GetUnreadNotificationsQuery : IQuery<IReadOnlyList<NotificationDto>>
{
    // Query with no parameters - uses current user from context
}
