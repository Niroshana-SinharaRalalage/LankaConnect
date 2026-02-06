using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LankaConnect.API.Extensions;
using LankaConnect.Application.Users.Commands.AdminActivateUser;
using LankaConnect.Application.Users.Commands.AdminDeactivateUser;
using LankaConnect.Application.Users.Commands.AdminLockUser;
using LankaConnect.Application.Users.Commands.AdminUnlockUser;
using LankaConnect.Application.Users.DTOs;
using LankaConnect.Application.Users.Queries.GetAdminUserDetails;
using LankaConnect.Application.Users.Queries.GetAdminUsersPaged;
using LankaConnect.Application.Users.Queries.GetAdminUserStatistics;
using LankaConnect.Domain.Users.Enums;

namespace LankaConnect.API.Controllers;

/// <summary>
/// Admin user management endpoints
/// Phase 6A.90: Admin User Management System
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Produces("application/json")]
[Authorize(Policy = "RequireAdmin")]
public class AdminUsersController : BaseController<AdminUsersController>
{
    public AdminUsersController(IMediator mediator, ILogger<AdminUsersController> logger) : base(mediator, logger)
    {
    }

    /// <summary>
    /// Get paginated list of users for admin management
    /// Phase 6A.90: Returns filtered/searched users with pagination
    /// </summary>
    /// <param name="page">Page number (1-based, default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20, max: 100)</param>
    /// <param name="search">Search term for name/email</param>
    /// <param name="role">Filter by role</param>
    /// <param name="isActive">Filter by active status</param>
    /// <returns>Paginated list of users</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResultDto<AdminUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] UserRole? role = null,
        [FromQuery] bool? isActive = null)
    {
        Logger.LogInformation(
            "Admin {AdminUserId} retrieving users - Page={Page}, PageSize={PageSize}, Search={Search}, Role={Role}, IsActive={IsActive}",
            User.TryGetUserId(), page, pageSize, search, role, isActive);

        var query = new GetAdminUsersPagedQuery
        {
            Page = page,
            PageSize = pageSize,
            SearchTerm = search,
            RoleFilter = role,
            IsActiveFilter = isActive
        };

        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Get detailed user information for admin view
    /// Phase 6A.90: Returns full user details including account status
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>User details</returns>
    [HttpGet("{userId:guid}")]
    [ProducesResponseType(typeof(AdminUserDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUserDetails(Guid userId)
    {
        Logger.LogInformation(
            "Admin {AdminUserId} retrieving user details - TargetUserId={TargetUserId}",
            User.TryGetUserId(), userId);

        var query = new GetAdminUserDetailsQuery(userId);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Get user statistics for admin dashboard
    /// Phase 6A.90: Returns counts by role, active/inactive, locked accounts
    /// </summary>
    /// <returns>User statistics</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(AdminUserStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStatistics()
    {
        Logger.LogInformation(
            "Admin {AdminUserId} retrieving user statistics",
            User.TryGetUserId());

        var query = new GetAdminUserStatisticsQuery();
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Deactivate a user account
    /// Phase 6A.90: Prevents user from logging in or performing actions
    /// </summary>
    /// <param name="userId">User ID to deactivate</param>
    /// <returns>Success or failure result</returns>
    [HttpPost("{userId:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeactivateUser(Guid userId)
    {
        var (ipAddress, userAgent) = GetClientInfo();

        Logger.LogInformation(
            "Admin {AdminUserId} deactivating user - TargetUserId={TargetUserId}, IP={IpAddress}",
            User.TryGetUserId(), userId, ipAddress);

        var command = new AdminDeactivateUserCommand(userId, ipAddress, userAgent);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Activate a user account
    /// Phase 6A.90: Allows user to login and perform actions again
    /// </summary>
    /// <param name="userId">User ID to activate</param>
    /// <returns>Success or failure result</returns>
    [HttpPost("{userId:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ActivateUser(Guid userId)
    {
        var (ipAddress, userAgent) = GetClientInfo();

        Logger.LogInformation(
            "Admin {AdminUserId} activating user - TargetUserId={TargetUserId}, IP={IpAddress}",
            User.TryGetUserId(), userId, ipAddress);

        var command = new AdminActivateUserCommand(userId, ipAddress, userAgent);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Lock a user account
    /// Phase 6A.90: Temporarily locks account until specified date
    /// </summary>
    /// <param name="userId">User ID to lock</param>
    /// <param name="request">Lock request with duration and reason</param>
    /// <returns>Success or failure result</returns>
    [HttpPost("{userId:guid}/lock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> LockUser(Guid userId, [FromBody] LockUserRequest request)
    {
        var (ipAddress, userAgent) = GetClientInfo();

        Logger.LogInformation(
            "Admin {AdminUserId} locking user - TargetUserId={TargetUserId}, LockUntil={LockUntil}, IP={IpAddress}",
            User.TryGetUserId(), userId, request.LockUntil, ipAddress);

        var command = new AdminLockUserCommand(userId, request.LockUntil, request.Reason, ipAddress, userAgent);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Unlock a user account
    /// Phase 6A.90: Clears lock and resets failed login attempts
    /// </summary>
    /// <param name="userId">User ID to unlock</param>
    /// <returns>Success or failure result</returns>
    [HttpPost("{userId:guid}/unlock")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UnlockUser(Guid userId)
    {
        var (ipAddress, userAgent) = GetClientInfo();

        Logger.LogInformation(
            "Admin {AdminUserId} unlocking user - TargetUserId={TargetUserId}, IP={IpAddress}",
            User.TryGetUserId(), userId, ipAddress);

        var command = new AdminUnlockUserCommand(userId, ipAddress, userAgent);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    #region Private Helpers

    private (string? IpAddress, string? UserAgent) GetClientInfo()
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        // Check for forwarded IP addresses (when behind a proxy/load balancer)
        if (Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor))
        {
            ipAddress = forwardedFor.ToString().Split(',')[0].Trim();
        }

        var userAgent = Request.Headers.UserAgent.ToString();
        if (userAgent.Length > 500)
        {
            userAgent = userAgent.Substring(0, 500);
        }

        return (ipAddress, userAgent);
    }

    #endregion
}

/// <summary>
/// Request body for locking a user account
/// Phase 6A.90: Admin User Management
/// </summary>
public record LockUserRequest
{
    /// <summary>
    /// Date/time until which the account will be locked
    /// </summary>
    public DateTime LockUntil { get; init; }

    /// <summary>
    /// Optional reason for the lock (for audit trail)
    /// </summary>
    public string? Reason { get; init; }
}
