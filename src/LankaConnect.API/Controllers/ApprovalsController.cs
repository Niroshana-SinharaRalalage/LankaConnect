using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LankaConnect.API.Extensions;
using LankaConnect.Application.Users.Queries.GetPendingRoleUpgrades;
using LankaConnect.Application.Users.Commands.ApproveRoleUpgrade;
using LankaConnect.Application.Users.Commands.RejectRoleUpgrade;
using LankaConnect.Application.Users.DTOs;

namespace LankaConnect.API.Controllers;

/// <summary>
/// Admin approval endpoints for role upgrade requests
/// Phase 6A.5: Admin Approval Workflow
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Policy = "RequireAdmin")]
public class ApprovalsController : BaseController<ApprovalsController>
{
    public ApprovalsController(IMediator mediator, ILogger<ApprovalsController> logger) : base(mediator, logger)
    {
    }

    /// <summary>
    /// Get all pending role upgrade requests
    /// Phase 6A.5: Returns list of users awaiting admin approval for role upgrades
    /// </summary>
    /// <returns>List of pending role upgrade requests</returns>
    [HttpGet("pending")]
    [ProducesResponseType(typeof(IReadOnlyList<PendingRoleUpgradeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPendingApprovals()
    {
        Logger.LogInformation("Admin {UserId} retrieving pending role upgrade approvals", User.TryGetUserId());

        var query = new GetPendingRoleUpgradesQuery();
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    /// <summary>
    /// Approve a role upgrade request
    /// Phase 6A.5: Approves pending upgrade and starts free trial for Event Organizers
    /// </summary>
    /// <param name="userId">User ID to approve</param>
    /// <returns>Success or failure result</returns>
    [HttpPost("{userId}/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ApproveRoleUpgrade(Guid userId)
    {
        Logger.LogInformation("Admin {AdminUserId} approving role upgrade for user {UserId}", User.TryGetUserId(), userId);

        var command = new ApproveRoleUpgradeCommand(userId);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Reject a role upgrade request
    /// Phase 6A.5: Rejects pending upgrade with optional reason
    /// </summary>
    /// <param name="userId">User ID to reject</param>
    /// <param name="request">Rejection request with optional reason</param>
    /// <returns>Success or failure result</returns>
    [HttpPost("{userId}/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RejectRoleUpgrade(Guid userId, [FromBody] RejectRoleUpgradeRequest request)
    {
        Logger.LogInformation("Admin {AdminUserId} rejecting role upgrade for user {UserId}, reason: {Reason}",
            User.TryGetUserId(), userId, request.Reason);

        var command = new RejectRoleUpgradeCommand(userId, request.Reason);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }
}

/// <summary>
/// Request body for rejecting a role upgrade
/// </summary>
public record RejectRoleUpgradeRequest
{
    public string? Reason { get; init; }
}
