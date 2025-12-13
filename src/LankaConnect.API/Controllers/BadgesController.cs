using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LankaConnect.API.Extensions;
using LankaConnect.Application.Badges.DTOs;
using LankaConnect.Application.Badges.Queries.GetBadges;
using LankaConnect.Application.Badges.Queries.GetBadgeById;
using LankaConnect.Application.Badges.Queries.GetEventBadges;
using LankaConnect.Application.Badges.Commands.CreateBadge;
using LankaConnect.Application.Badges.Commands.UpdateBadge;
using LankaConnect.Application.Badges.Commands.UpdateBadgeImage;
using LankaConnect.Application.Badges.Commands.DeleteBadge;
using LankaConnect.Application.Badges.Commands.AssignBadgeToEvent;
using LankaConnect.Application.Badges.Commands.RemoveBadgeFromEvent;
using LankaConnect.Domain.Badges.Enums;

namespace LankaConnect.API.Controllers;

/// <summary>
/// Badge Management endpoints for event promotional overlays
/// Phase 6A.25: Badge Management System
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class BadgesController : BaseController<BadgesController>
{
    public BadgesController(IMediator mediator, ILogger<BadgesController> logger) : base(mediator, logger)
    {
    }

    /// <summary>
    /// Get all badges with optional filters
    /// Phase 6A.27: Added forManagement and forAssignment parameters for role-based filtering
    /// </summary>
    /// <param name="activeOnly">If true, returns only active badges (default). If false, returns all badges.</param>
    /// <param name="forManagement">If true, filters for Badge Management UI (Admin: all badges, EventOrganizer: own custom badges)</param>
    /// <param name="forAssignment">If true, filters for Badge Assignment UI (EventOrganizer: own + system, Admin: system only). Excludes expired badges.</param>
    /// <returns>List of badges</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BadgeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetBadges(
        [FromQuery] bool activeOnly = true,
        [FromQuery] bool forManagement = false,
        [FromQuery] bool forAssignment = false)
    {
        Logger.LogInformation("User {UserId} retrieving badges (activeOnly: {ActiveOnly}, forManagement: {ForManagement}, forAssignment: {ForAssignment})",
            User.TryGetUserId(), activeOnly, forManagement, forAssignment);

        var query = new GetBadgesQuery
        {
            ActiveOnly = activeOnly,
            ForManagement = forManagement,
            ForAssignment = forAssignment
        };
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    /// <summary>
    /// Get a badge by ID
    /// </summary>
    /// <param name="id">Badge ID</param>
    /// <returns>Badge details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BadgeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBadgeById(Guid id)
    {
        Logger.LogInformation("User {UserId} retrieving badge {BadgeId}",
            User.TryGetUserId(), id);

        var query = new GetBadgeByIdQuery { BadgeId = id };
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    /// <summary>
    /// Create a new badge with uploaded image
    /// Phase 6A.27: Admin creates System badges, EventOrganizer creates Custom badges
    /// Phase 6A.28: Changed expiresAt to defaultDurationDays (duration-based expiration)
    /// Requires EventOrganizer, Admin, or AdminManager role
    /// </summary>
    /// <param name="name">Badge name</param>
    /// <param name="position">Position on event image (TopLeft, TopRight, BottomLeft, BottomRight)</param>
    /// <param name="defaultDurationDays">Optional default duration in days for badge assignments (null = never expires)</param>
    /// <param name="file">Badge image file (PNG recommended for transparency)</param>
    /// <returns>Created badge</returns>
    [HttpPost]
    [Authorize(Roles = "EventOrganizer,Admin,AdminManager")]
    [ProducesResponseType(typeof(BadgeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateBadge(
        [FromForm] string name,
        [FromForm] BadgePosition position,
        [FromForm] int? defaultDurationDays,
        IFormFile file)
    {
        Logger.LogInformation("User {UserId} creating badge: {Name} (defaultDurationDays: {DefaultDurationDays})",
            User.TryGetUserId(), name, defaultDurationDays);

        if (file == null || file.Length == 0)
            return BadRequest(new ProblemDetails { Detail = "Badge image is required", Status = 400 });

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);

        var command = new CreateBadgeCommand
        {
            Name = name,
            Position = position,
            DefaultDurationDays = defaultDurationDays,
            ImageData = memoryStream.ToArray(),
            FileName = file.FileName
        };

        var result = await Mediator.Send(command);

        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetBadgeById), new { id = result.Value.Id }, result.Value);
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Update a badge's details (name, position, displayOrder, isActive, defaultDurationDays)
    /// Phase 6A.27: Ownership validation - Admin can edit any, EventOrganizer only their own
    /// Phase 6A.28: Changed expiresAt to defaultDurationDays (duration-based expiration)
    /// Requires EventOrganizer, Admin, or AdminManager role
    /// </summary>
    /// <param name="id">Badge ID</param>
    /// <param name="dto">Updated badge properties</param>
    /// <returns>Updated badge</returns>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "EventOrganizer,Admin,AdminManager")]
    [ProducesResponseType(typeof(BadgeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBadge(Guid id, [FromBody] UpdateBadgeDto dto)
    {
        Logger.LogInformation("User {UserId} updating badge {BadgeId}",
            User.TryGetUserId(), id);

        var command = new UpdateBadgeCommand
        {
            BadgeId = id,
            Name = dto.Name,
            Position = dto.Position,
            IsActive = dto.IsActive,
            DisplayOrder = dto.DisplayOrder,
            DefaultDurationDays = dto.DefaultDurationDays,
            ClearDuration = dto.ClearDuration
        };

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Update a badge's image
    /// Requires EventOrganizer, Admin, or AdminManager role
    /// </summary>
    /// <param name="id">Badge ID</param>
    /// <param name="file">New badge image file</param>
    /// <returns>Updated badge</returns>
    [HttpPut("{id:guid}/image")]
    [Authorize(Roles = "EventOrganizer,Admin,AdminManager")]
    [ProducesResponseType(typeof(BadgeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateBadgeImage(Guid id, IFormFile file)
    {
        Logger.LogInformation("User {UserId} updating image for badge {BadgeId}",
            User.TryGetUserId(), id);

        if (file == null || file.Length == 0)
            return BadRequest(new ProblemDetails { Detail = "Badge image is required", Status = 400 });

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);

        var command = new UpdateBadgeImageCommand
        {
            BadgeId = id,
            ImageData = memoryStream.ToArray(),
            FileName = file.FileName
        };

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Delete a badge
    /// Phase 6A.27: Ownership validation - Admin can delete any, EventOrganizer only their own
    /// System badges are only deactivated, not deleted
    /// Requires EventOrganizer, Admin, or AdminManager role
    /// </summary>
    /// <param name="id">Badge ID</param>
    /// <returns>Success or failure</returns>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "EventOrganizer,Admin,AdminManager")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBadge(Guid id)
    {
        Logger.LogInformation("User {UserId} deleting badge {BadgeId}",
            User.TryGetUserId(), id);

        var command = new DeleteBadgeCommand { BadgeId = id };
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Get badges assigned to an event
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <returns>List of badges assigned to the event</returns>
    [HttpGet("events/{eventId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<EventBadgeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEventBadges(Guid eventId)
    {
        Logger.LogInformation("User {UserId} retrieving badges for event {EventId}",
            User.TryGetUserId(), eventId);

        var query = new GetEventBadgesQuery { EventId = eventId };
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    /// <summary>
    /// Assign a badge to an event
    /// Phase 6A.28: Added durationDays parameter for duration override
    /// Requires EventOrganizer, Admin, or AdminManager role
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="badgeId">Badge ID to assign</param>
    /// <param name="durationDays">Optional duration override in days (null = use badge default, cannot exceed badge's default)</param>
    /// <returns>Event badge assignment</returns>
    [HttpPost("events/{eventId:guid}/badges/{badgeId:guid}")]
    [Authorize(Roles = "EventOrganizer,Admin,AdminManager")]
    [ProducesResponseType(typeof(EventBadgeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignBadgeToEvent(Guid eventId, Guid badgeId, [FromQuery] int? durationDays = null)
    {
        Logger.LogInformation("User {UserId} assigning badge {BadgeId} to event {EventId} (durationDays: {DurationDays})",
            User.TryGetUserId(), badgeId, eventId, durationDays);

        var command = new AssignBadgeToEventCommand
        {
            EventId = eventId,
            BadgeId = badgeId,
            DurationDays = durationDays
        };
        var result = await Mediator.Send(command);

        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetEventBadges), new { eventId }, result.Value);
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Remove a badge from an event
    /// Requires EventOrganizer, Admin, or AdminManager role
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="badgeId">Badge ID to remove</param>
    /// <returns>Success or failure</returns>
    [HttpDelete("events/{eventId:guid}/badges/{badgeId:guid}")]
    [Authorize(Roles = "EventOrganizer,Admin,AdminManager")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveBadgeFromEvent(Guid eventId, Guid badgeId)
    {
        Logger.LogInformation("User {UserId} removing badge {BadgeId} from event {EventId}",
            User.TryGetUserId(), badgeId, eventId);

        var command = new RemoveBadgeFromEventCommand { EventId = eventId, BadgeId = badgeId };
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }
}
