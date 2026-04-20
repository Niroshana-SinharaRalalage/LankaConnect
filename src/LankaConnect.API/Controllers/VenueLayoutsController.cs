using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Events.Commands.CreateVenueLayout;
using LankaConnect.Application.Events.Commands.GenerateSeats;
using LankaConnect.Application.Events.Commands.HoldSeats;
using LankaConnect.Application.Events.Commands.ReleaseSeats;
using LankaConnect.Application.Events.Commands.AssignLayoutToEvent;
using LankaConnect.Application.Events.Commands.UpdateLayout;
using LankaConnect.Application.Events.Commands.UpdateZone;
using LankaConnect.Application.Events.Commands.DeleteZone;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Application.Events.Queries.GetVenueLayout;
using LankaConnect.Application.Events.Queries.GetSeatAvailability;
using LankaConnect.Application.Events.Common;
using LankaConnect.API.Extensions;

namespace LankaConnect.API.Controllers;

[Route("api/venue-layouts")]
public class VenueLayoutsController : BaseController<VenueLayoutsController>
{
    public VenueLayoutsController(IMediator mediator, ILogger<VenueLayoutsController> logger)
        : base(mediator, logger)
    {
    }

    // ==================== LAYOUT MANAGEMENT (Organizer) ====================

    /// <summary>
    /// Create a new venue layout with zones
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(VenueLayoutDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateLayout([FromBody] CreateVenueLayoutRequest request)
    {
        var userId = User.GetUserId();

        Logger.LogInformation(
            "Creating venue layout '{Name}' for event {EventId} by user {UserId}",
            request.Name, request.EventId, userId);

        var command = new CreateVenueLayoutCommand(
            request.Name,
            request.LayoutType,
            userId,
            request.EventId,
            request.IsTemplate,
            request.Zones);

        var result = await Mediator.Send(command);

        return HandleResultWithCreated(result, nameof(GetLayout), new { id = result.IsSuccess ? result.Value!.Id : Guid.Empty });
    }

    /// <summary>
    /// Get a venue layout by ID (with zones and seats)
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(VenueLayoutDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetLayout(Guid id)
    {
        var query = new GetVenueLayoutQuery(id, null);
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    /// <summary>
    /// Get the venue layout assigned to an event
    /// </summary>
    [HttpGet("by-event/{eventId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(VenueLayoutDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetLayoutByEvent(Guid eventId)
    {
        var query = new GetVenueLayoutQuery(null, eventId);
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    /// <summary>
    /// Update a venue layout's name and/or canvas configuration.
    /// Requires the <c>If-Match</c> header carrying the RowVersion from the last GET
    /// — stale values return HTTP 409. At least one of <c>name</c> or <c>canvas</c>
    /// must be supplied.
    /// Slice 5 Chunk 4.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateLayout(Guid id, [FromBody] UpdateLayoutRequest request)
    {
        if (!TryParseIfMatch(out var expectedRowVersion, out var badRequest))
        {
            return badRequest!;
        }

        Logger.LogInformation(
            "Updating venue layout {LayoutId}: ExpectedRowVersion={ExpectedRowVersion}",
            id, expectedRowVersion);

        var command = new UpdateLayoutCommand(id, expectedRowVersion, request.Name, request.Canvas);
        var result = await Mediator.Send(command);

        return HandleResultNoContent(result);
    }

    /// <summary>
    /// Update a zone (name, color, sort order, and/or canvas shape + geometry).
    /// Structural changes (shape/geometry) are rejected with HTTP 422 when seats are held/reserved.
    /// Requires the <c>If-Match</c> header. Slice 5 Chunk 5.
    /// </summary>
    [HttpPatch("{id:guid}/zones/{zoneId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateZone(Guid id, Guid zoneId, [FromBody] UpdateZoneRequest request)
    {
        if (!TryParseIfMatch(out var expectedRowVersion, out var badRequest))
        {
            return badRequest!;
        }

        ZoneShape? shape = null;
        if (!string.IsNullOrWhiteSpace(request.Shape))
        {
            if (!Enum.TryParse<ZoneShape>(request.Shape, ignoreCase: true, out var parsed))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Bad Request",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = $"Invalid zone shape: '{request.Shape}'. Valid: Rect, Curve, Polygon"
                });
            }
            shape = parsed;
        }

        Logger.LogInformation(
            "Updating zone {ZoneId} in layout {LayoutId}: ExpectedRowVersion={RowVersion}",
            zoneId, id, expectedRowVersion);

        var command = new UpdateZoneCommand(
            id, zoneId, expectedRowVersion,
            request.Name, request.Color, request.SortOrder,
            shape, request.Geometry);

        var result = await Mediator.Send(command);

        return HandleResultNoContent(result);
    }

    /// <summary>
    /// Delete a zone from the layout. Rejected with HTTP 422 if any seat is held or reserved.
    /// Requires the <c>If-Match</c> header. Slice 5 Chunk 5.
    /// </summary>
    [HttpDelete("{id:guid}/zones/{zoneId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DeleteZone(Guid id, Guid zoneId)
    {
        if (!TryParseIfMatch(out var expectedRowVersion, out var badRequest))
        {
            return badRequest!;
        }

        Logger.LogInformation(
            "Deleting zone {ZoneId} from layout {LayoutId}: ExpectedRowVersion={RowVersion}",
            zoneId, id, expectedRowVersion);

        var command = new DeleteZoneCommand(id, zoneId, expectedRowVersion);
        var result = await Mediator.Send(command);

        return HandleResultNoContent(result);
    }

    /// <summary>
    /// Parses the <c>If-Match</c> header into a <see cref="uint"/> RowVersion.
    /// Accepts either raw numeric form ("42") or quoted ETag form ("\"42\"").
    /// On failure, sets <paramref name="problem"/> to a 400 ProblemDetails response.
    /// </summary>
    private bool TryParseIfMatch(out uint expectedRowVersion, out IActionResult? problem)
    {
        expectedRowVersion = 0u;
        problem = null;

        var header = Request.Headers["If-Match"].ToString();
        if (string.IsNullOrWhiteSpace(header))
        {
            problem = BadRequest(new ProblemDetails
            {
                Title = "Bad Request",
                Status = StatusCodes.Status400BadRequest,
                Detail = "If-Match header is required for optimistic concurrency control"
            });
            return false;
        }

        var trimmed = header.Trim().Trim('"');
        if (!uint.TryParse(trimmed, out expectedRowVersion))
        {
            problem = BadRequest(new ProblemDetails
            {
                Title = "Bad Request",
                Status = StatusCodes.Status400BadRequest,
                Detail = "If-Match header must be an unsigned integer matching the layout's RowVersion"
            });
            return false;
        }

        return true;
    }

    /// <summary>
    /// Generate seats for a zone (theater rows or banquet tables)
    /// </summary>
    [HttpPost("{layoutId:guid}/zones/{zoneId:guid}/generate-seats")]
    [Authorize]
    [ProducesResponseType(typeof(VenueLayoutDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GenerateSeats(Guid layoutId, Guid zoneId, [FromBody] GenerateSeatsRequest request)
    {
        Logger.LogInformation(
            "Generating {Type} seats for zone {ZoneId} in layout {LayoutId}: {Rows}x{SeatsPerUnit}",
            request.GenerationType, zoneId, layoutId, request.RowsOrTables, request.SeatsPerUnit);

        var command = new GenerateSeatsCommand(
            layoutId,
            zoneId,
            request.GenerationType,
            request.RowsOrTables,
            request.SeatsPerUnit,
            request.StartLabel);

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Assign a layout to an event (sets SeatingMode to AssignedSeating)
    /// </summary>
    [HttpPost("assign")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AssignLayoutToEvent([FromBody] AssignLayoutRequest request)
    {
        Logger.LogInformation(
            "Assigning layout {LayoutId} to event {EventId}",
            request.LayoutId, request.EventId);

        var command = new AssignLayoutToEventCommand(request.EventId, request.LayoutId);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    // ==================== SEAT AVAILABILITY (Public) ====================

    /// <summary>
    /// Get seat availability for an event (combines structural + runtime state)
    /// </summary>
    [HttpGet("events/{eventId:guid}/seats")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(List<SeatAvailabilityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetSeatAvailability(Guid eventId)
    {
        var query = new GetSeatAvailabilityQuery(eventId);
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    // ==================== SEAT HOLD/RELEASE (User) ====================

    /// <summary>
    /// Hold seats for a user (10-minute hold, max 10 seats per session)
    /// </summary>
    [HttpPost("events/{eventId:guid}/seats/hold")]
    [Authorize]
    [ProducesResponseType(typeof(HoldSeatsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> HoldSeats(Guid eventId, [FromBody] HoldSeatsRequest request)
    {
        var userId = User.GetUserId();

        Logger.LogInformation(
            "Holding {Count} seats for event {EventId} by user {UserId}, session {SessionId}",
            request.SeatIds.Count, eventId, userId, request.SessionId);

        var command = new HoldSeatsCommand(
            eventId,
            userId,
            request.SessionId,
            request.SeatIds);

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Release all held seats for a session (idempotent)
    /// </summary>
    [HttpPost("events/{eventId:guid}/seats/release")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ReleaseSeats(Guid eventId, [FromBody] ReleaseSeatsRequest request)
    {
        var userId = User.GetUserId();

        Logger.LogInformation(
            "Releasing seats for session {SessionId} by user {UserId}",
            request.SessionId, userId);

        var command = new ReleaseSeatsCommand(request.SessionId, userId);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }
}

// ==================== REQUEST DTOs ====================

public record CreateVenueLayoutRequest(
    string Name,
    string LayoutType,
    Guid? EventId,
    bool IsTemplate,
    List<CreateVenueZoneRequest> Zones);

/// <summary>
/// Slice 5 Chunk 4: PUT body. Both fields optional — at least one must be provided.
/// <c>If-Match</c> carries the RowVersion separately for optimistic concurrency.
/// </summary>
public record UpdateLayoutRequest(
    string? Name,
    UpdateLayoutCanvasRequest? Canvas);

/// <summary>
/// Slice 5 Chunk 5: PATCH zone body. All fields optional — at least one must be provided.
/// Shape is accepted as a string for JSON-friendliness and parsed against <c>ZoneShape</c>
/// at the controller layer.
/// </summary>
public record UpdateZoneRequest(
    string? Name,
    string? Color,
    int? SortOrder,
    string? Shape,
    string? Geometry);

public record GenerateSeatsRequest(
    string GenerationType,
    int RowsOrTables,
    int SeatsPerUnit,
    string? StartLabel = null);

public record AssignLayoutRequest(
    Guid EventId,
    Guid LayoutId);

public record HoldSeatsRequest(
    string SessionId,
    List<Guid> SeatIds);

public record ReleaseSeatsRequest(
    string SessionId);
