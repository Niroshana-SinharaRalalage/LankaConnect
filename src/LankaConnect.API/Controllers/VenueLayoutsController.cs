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
