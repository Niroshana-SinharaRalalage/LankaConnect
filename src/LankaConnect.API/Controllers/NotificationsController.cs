using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LankaConnect.API.Extensions;
using LankaConnect.Application.Notifications.Queries.GetUnreadNotifications;
using LankaConnect.Application.Notifications.Commands.MarkNotificationAsRead;
using LankaConnect.Application.Notifications.Commands.MarkAllNotificationsAsRead;
using LankaConnect.Application.Notifications.DTOs;

namespace LankaConnect.API.Controllers;

/// <summary>
/// Notifications endpoints for in-app notification system
/// Phase 6A.6: Notification System
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class NotificationsController : BaseController<NotificationsController>
{
    public NotificationsController(IMediator mediator, ILogger<NotificationsController> logger) : base(mediator, logger)
    {
    }

    /// <summary>
    /// Get unread notifications for the current user
    /// Phase 6A.6: Returns list of unread notifications
    /// </summary>
    /// <returns>List of unread notifications</returns>
    [HttpGet("unread")]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUnreadNotifications()
    {
        Logger.LogInformation("User {UserId} retrieving unread notifications", User.TryGetUserId());

        var query = new GetUnreadNotificationsQuery();
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    /// <summary>
    /// Mark a notification as read
    /// Phase 6A.6: Marks a specific notification as read for the current user
    /// </summary>
    /// <param name="notificationId">Notification ID to mark as read</param>
    /// <returns>Success or failure result</returns>
    [HttpPost("{notificationId}/read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(Guid notificationId)
    {
        Logger.LogInformation("User {UserId} marking notification {NotificationId} as read",
            User.TryGetUserId(), notificationId);

        var command = new MarkNotificationAsReadCommand(notificationId);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Mark all notifications as read
    /// Phase 6A.6: Marks all unread notifications as read for the current user
    /// </summary>
    /// <returns>Success or failure result</returns>
    [HttpPost("read-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAllAsRead()
    {
        Logger.LogInformation("User {UserId} marking all notifications as read", User.TryGetUserId());

        var command = new MarkAllNotificationsAsReadCommand();
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }
}
