using System.Security.Claims;
using LankaConnect.Domain.Common;
using LankaConnect.Modules.Notifications.Application.Commands.MarkAllNotificationsAsRead;
using LankaConnect.Modules.Notifications.Application.Commands.MarkNotificationAsRead;
using LankaConnect.Modules.Notifications.Application.DTOs;
using LankaConnect.Modules.Notifications.Application.Queries.GetUnreadNotifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

namespace LankaConnect.Modules.Notifications.Api.Controllers;

/// <summary>
/// Notifications endpoints for the in-app notification surface. Moved into
/// <c>Notifications.Api</c> during Phase A W3.6 (2026-06-03) without behavior
/// change. The host (<c>LankaConnect.API</c>) discovers this controller via
/// the implicit ApplicationPart added by the <c>Notifications.Api</c>
/// ProjectReference + MVC's default controller discovery.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why not inherit BaseController</b>: <c>LankaConnect.API.Controllers.BaseController&lt;T&gt;</c>
/// lives in the legacy host project. Inheriting it from this module would
/// require a Notifications.Api → LankaConnect.API edge — that would close a
/// hard cycle with the existing LankaConnect.API → Notifications.Api edge.
/// The handful of helpers are inlined here instead. Future work elevates a
/// reusable <c>ModuleControllerBase</c> into <c>BuildingBlocks.Web</c>.
/// </para>
/// <para>
/// <b>Feature flag</b>: <see cref="FlagName"/> (<c>Refactor.Notifications.UseNewModule</c>)
/// is observed for end-to-end visibility — each endpoint logs the flag value
/// so staging soak can confirm the flag pipeline is wired. The flag has no
/// functional effect during W3 because the new and legacy code paths produce
/// identical queries against the same physical table — but the flag exists
/// per ADR-004 and provides a future hook if W3.x dual-paths emerge. Sunset
/// W7 alongside the legacy <c>NotificationRepository</c> cleanup.
/// </para>
/// </remarks>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class NotificationsController : ControllerBase
{
    /// <summary>Feature flag name observed by every endpoint (W3.7 registry).</summary>
    public const string FlagName = "Refactor.Notifications.UseNewModule";

    private readonly IMediator _mediator;
    private readonly ILogger<NotificationsController> _logger;
    private readonly IFeatureManager _featureManager;

    public NotificationsController(
        IMediator mediator,
        ILogger<NotificationsController> logger,
        IFeatureManager featureManager)
    {
        _mediator = mediator;
        _logger = logger;
        _featureManager = featureManager;
    }

    /// <summary>Get unread notifications for the current user.</summary>
    [HttpGet("unread")]
    [ProducesResponseType(typeof(IReadOnlyList<NotificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUnreadNotifications()
    {
        var useNewModule = await _featureManager.IsEnabledAsync(FlagName);
        _logger.LogInformation(
            "User {UserId} retrieving unread notifications (UseNewModule={UseNewModule})",
            TryGetUserId(), useNewModule);

        var query = new GetUnreadNotificationsQuery();
        var result = await _mediator.Send(query);

        return HandleResult(result);
    }

    /// <summary>Mark a notification as read.</summary>
    [HttpPost("{notificationId}/read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(Guid notificationId)
    {
        var useNewModule = await _featureManager.IsEnabledAsync(FlagName);
        _logger.LogInformation(
            "User {UserId} marking notification {NotificationId} as read (UseNewModule={UseNewModule})",
            TryGetUserId(), notificationId, useNewModule);

        var command = new MarkNotificationAsReadCommand(notificationId);
        var result = await _mediator.Send(command);

        return HandleResultUnit(result);
    }

    /// <summary>Mark all notifications as read.</summary>
    [HttpPost("read-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var useNewModule = await _featureManager.IsEnabledAsync(FlagName);
        _logger.LogInformation(
            "User {UserId} marking all notifications as read (UseNewModule={UseNewModule})",
            TryGetUserId(), useNewModule);

        var command = new MarkAllNotificationsAsReadCommand();
        var result = await _mediator.Send(command);

        return HandleResultUnit(result);
    }

    // ---------- Inlined helpers (see <remarks> for why not BaseController) ----------

    private Guid? TryGetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim))
        {
            return null;
        }
        return Guid.TryParse(userIdClaim, out var id) ? id : null;
    }

    private IActionResult HandleResult<TResult>(Result<TResult> result)
        => result.IsSuccess ? Ok(result.Value) : BuildProblem(result.Error);

    private IActionResult HandleResultUnit(Result result)
        => result.IsSuccess ? Ok() : BuildProblem(result.Error);

    private IActionResult BuildProblem(string error)
        => Problem(detail: error, statusCode: StatusCodes.Status400BadRequest);
}
