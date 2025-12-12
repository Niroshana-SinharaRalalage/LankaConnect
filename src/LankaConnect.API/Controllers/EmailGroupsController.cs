using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LankaConnect.API.Extensions;
using LankaConnect.Application.Communications.Common;
using LankaConnect.Application.Communications.Queries.GetEmailGroups;
using LankaConnect.Application.Communications.Queries.GetEmailGroupById;
using LankaConnect.Application.Communications.Commands.CreateEmailGroup;
using LankaConnect.Application.Communications.Commands.UpdateEmailGroup;
using LankaConnect.Application.Communications.Commands.DeleteEmailGroup;

namespace LankaConnect.API.Controllers;

/// <summary>
/// Email Groups Management endpoints
/// Phase 6A.25: Allows Event Organizers and Admins to manage email groups
/// for event announcements, invitations, and marketing communications.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class EmailGroupsController : BaseController<EmailGroupsController>
{
    public EmailGroupsController(IMediator mediator, ILogger<EmailGroupsController> logger) : base(mediator, logger)
    {
    }

    /// <summary>
    /// Get email groups for the current user
    /// - Regular users see only their own groups
    /// - Admins can see all groups when includeAll=true
    /// </summary>
    /// <param name="includeAll">If true and user is admin, returns all groups across platform</param>
    /// <returns>List of email groups</returns>
    [HttpGet]
    [Authorize(Roles = "EventOrganizer,Admin,AdminManager")]
    [ProducesResponseType(typeof(IReadOnlyList<EmailGroupDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetEmailGroups([FromQuery] bool includeAll = false)
    {
        Logger.LogInformation("User {UserId} retrieving email groups (includeAll: {IncludeAll})",
            User.TryGetUserId(), includeAll);

        var query = new GetEmailGroupsQuery { IncludeAll = includeAll };
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    /// <summary>
    /// Get an email group by ID
    /// </summary>
    /// <param name="id">Email group ID</param>
    /// <returns>Email group details</returns>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "EventOrganizer,Admin,AdminManager")]
    [ProducesResponseType(typeof(EmailGroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEmailGroupById(Guid id)
    {
        Logger.LogInformation("User {UserId} retrieving email group {EmailGroupId}",
            User.TryGetUserId(), id);

        var query = new GetEmailGroupByIdQuery { Id = id };
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    /// <summary>
    /// Create a new email group
    /// Requires EventOrganizer, Admin, or AdminManager role
    /// </summary>
    /// <param name="request">Email group creation request</param>
    /// <returns>Created email group</returns>
    [HttpPost]
    [Authorize(Roles = "EventOrganizer,Admin,AdminManager")]
    [ProducesResponseType(typeof(EmailGroupDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateEmailGroup([FromBody] CreateEmailGroupRequest request)
    {
        Logger.LogInformation("User {UserId} creating email group: {Name}",
            User.TryGetUserId(), request.Name);

        var command = new CreateEmailGroupCommand
        {
            Name = request.Name,
            Description = request.Description,
            EmailAddresses = request.EmailAddresses
        };

        var result = await Mediator.Send(command);

        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetEmailGroupById), new { id = result.Value.Id }, result.Value);
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Update an email group
    /// Requires ownership or Admin/AdminManager role
    /// </summary>
    /// <param name="id">Email group ID</param>
    /// <param name="request">Updated email group properties</param>
    /// <returns>Updated email group</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "EventOrganizer,Admin,AdminManager")]
    [ProducesResponseType(typeof(EmailGroupDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEmailGroup(Guid id, [FromBody] UpdateEmailGroupRequest request)
    {
        Logger.LogInformation("User {UserId} updating email group {EmailGroupId}",
            User.TryGetUserId(), id);

        var command = new UpdateEmailGroupCommand
        {
            Id = id,
            Name = request.Name,
            Description = request.Description,
            EmailAddresses = request.EmailAddresses
        };

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Delete an email group (soft delete)
    /// Requires ownership or Admin/AdminManager role
    /// </summary>
    /// <param name="id">Email group ID</param>
    /// <returns>Success or failure</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "EventOrganizer,Admin,AdminManager")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEmailGroup(Guid id)
    {
        Logger.LogInformation("User {UserId} deleting email group {EmailGroupId}",
            User.TryGetUserId(), id);

        var command = new DeleteEmailGroupCommand { Id = id };
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }
}

/// <summary>
/// Request model for creating an email group
/// </summary>
public record CreateEmailGroupRequest
{
    /// <summary>
    /// Name of the email group
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Optional description
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Comma-separated list of email addresses
    /// </summary>
    public string EmailAddresses { get; init; } = string.Empty;
}

/// <summary>
/// Request model for updating an email group
/// </summary>
public record UpdateEmailGroupRequest
{
    /// <summary>
    /// Name of the email group
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Optional description
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Comma-separated list of email addresses
    /// </summary>
    public string EmailAddresses { get; init; } = string.Empty;
}
