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
using LankaConnect.Application.Events.Commands.DeleteLayout;
using LankaConnect.Application.Events.Commands.UpdateZone;
using LankaConnect.Application.Events.Commands.DeleteZone;
using LankaConnect.Application.Events.Commands.AddTable;
using LankaConnect.Application.Events.Commands.UpdateTable;
using LankaConnect.Application.Events.Commands.DeleteTable;
using LankaConnect.Application.Events.Commands.AddDecoration;
using LankaConnect.Application.Events.Commands.AssignTier;
using LankaConnect.Application.Events.Commands.RemoveTierAssignment;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Application.Events.Commands.UpdateDecoration;
using LankaConnect.Application.Events.Commands.DeleteDecoration;
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
    /// Hard-delete a venue layout (and cascade its zones/tables/decorations/seats).
    /// Rejected with HTTP 422 when any seat is held or reserved, or when the owning
    /// event has preliminary/confirmed registrations. Event is detached (seating mode
    /// flipped back to GA) before the delete commits. Requires <c>If-Match</c>.
    /// Slice 5 Chunk 9.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DeleteLayout(Guid id)
    {
        if (!TryParseIfMatch(out var expectedRowVersion, out var badRequest))
        {
            return badRequest!;
        }

        Logger.LogInformation(
            "Deleting venue layout {LayoutId}: ExpectedRowVersion={ExpectedRowVersion}",
            id, expectedRowVersion);

        var command = new DeleteLayoutCommand(id, expectedRowVersion);
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
    /// Add a table (round/square/rect) to a layout. Seats are auto-generated
    /// based on shape and capacity. Requires the <c>If-Match</c> header.
    /// Slice 5 Chunk 6.
    /// </summary>
    [HttpPost("{id:guid}/tables")]
    [Authorize]
    [ProducesResponseType(typeof(AddTableResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddTable(Guid id, [FromBody] AddTableRequest request)
    {
        if (!TryParseIfMatch(out var expectedRowVersion, out var badRequest))
        {
            return badRequest!;
        }

        if (!Enum.TryParse<TableShape>(request.Shape, ignoreCase: true, out var shape))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Bad Request",
                Status = StatusCodes.Status400BadRequest,
                Detail = $"Invalid table shape: '{request.Shape}'. Valid: Round, Square, Rect"
            });
        }

        Logger.LogInformation(
            "Adding table to layout {LayoutId}: Label={Label}, Shape={Shape}, Capacity={Capacity}, ExpectedRowVersion={RowVersion}",
            id, request.Label, shape, request.Capacity, expectedRowVersion);

        var command = new AddTableCommand(
            id,
            expectedRowVersion,
            request.Label,
            shape,
            request.Capacity,
            request.SortOrder,
            request.ZoneId,
            request.Geometry,
            request.StartAngleDeg ?? 0);

        var result = await Mediator.Send(command);

        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetLayout), new { id }, new AddTableResponse(result.Value));
        }
        return HandleResultNoContent(result);
    }

    /// <summary>
    /// Update a table's metadata and/or geometry. Structural changes
    /// (shape, capacity, geometry) are rejected with HTTP 422 when seats are
    /// held/reserved. Requires the <c>If-Match</c> header. Slice 5 Chunk 6.
    /// </summary>
    [HttpPatch("{id:guid}/tables/{tableId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateTable(Guid id, Guid tableId, [FromBody] UpdateTableRequest request)
    {
        if (!TryParseIfMatch(out var expectedRowVersion, out var badRequest))
        {
            return badRequest!;
        }

        TableShape? shape = null;
        if (!string.IsNullOrWhiteSpace(request.Shape))
        {
            if (!Enum.TryParse<TableShape>(request.Shape, ignoreCase: true, out var parsed))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Bad Request",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = $"Invalid table shape: '{request.Shape}'. Valid: Round, Square, Rect"
                });
            }
            shape = parsed;
        }

        Logger.LogInformation(
            "Updating table {TableId} in layout {LayoutId}: ExpectedRowVersion={RowVersion}",
            tableId, id, expectedRowVersion);

        var command = new UpdateTableCommand(
            id, tableId, expectedRowVersion,
            request.Label,
            shape,
            request.Capacity,
            request.SortOrder,
            request.ZoneId,
            request.ClearZoneId ?? false,
            request.Geometry);

        var result = await Mediator.Send(command);

        return HandleResultNoContent(result);
    }

    /// <summary>
    /// Delete a table from the layout. Rejected with HTTP 422 when any seat
    /// on the table is held or reserved. Requires the <c>If-Match</c> header.
    /// Slice 5 Chunk 6.
    /// </summary>
    [HttpDelete("{id:guid}/tables/{tableId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DeleteTable(Guid id, Guid tableId)
    {
        if (!TryParseIfMatch(out var expectedRowVersion, out var badRequest))
        {
            return badRequest!;
        }

        Logger.LogInformation(
            "Deleting table {TableId} from layout {LayoutId}: ExpectedRowVersion={RowVersion}",
            tableId, id, expectedRowVersion);

        var command = new DeleteTableCommand(id, tableId, expectedRowVersion);
        var result = await Mediator.Send(command);

        return HandleResultNoContent(result);
    }

    /// <summary>
    /// Add a decoration (stage, dance floor, aisle, door, wall, text, image)
    /// to a layout. Decorations have no seats — no structural guard runs.
    /// Requires the <c>If-Match</c> header. Slice 5 Chunk 7.
    /// </summary>
    [HttpPost("{id:guid}/decorations")]
    [Authorize]
    [ProducesResponseType(typeof(AddDecorationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddDecoration(Guid id, [FromBody] AddDecorationRequest request)
    {
        if (!TryParseIfMatch(out var expectedRowVersion, out var badRequest))
        {
            return badRequest!;
        }

        if (!Enum.TryParse<DecorationKind>(request.Kind, ignoreCase: true, out var kind))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Bad Request",
                Status = StatusCodes.Status400BadRequest,
                Detail = $"Invalid decoration kind: '{request.Kind}'. Valid: Stage, DanceFloor, Aisle, Door, Wall, Text, Image"
            });
        }

        Logger.LogInformation(
            "Adding decoration to layout {LayoutId}: Kind={Kind}, Label={Label}, ExpectedRowVersion={RowVersion}",
            id, kind, request.Label, expectedRowVersion);

        var command = new AddDecorationCommand(
            id,
            expectedRowVersion,
            kind,
            request.Label,
            request.SortOrder,
            request.Geometry,
            request.Properties);

        var result = await Mediator.Send(command);

        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetLayout), new { id }, new AddDecorationResponse(result.Value));
        }
        return HandleResultNoContent(result);
    }

    /// <summary>
    /// Update a decoration's kind, label, sort order, geometry, or properties.
    /// All fields are optional — at least one must be supplied. Pass
    /// <c>clearLabel: true</c> to explicitly detach the label (not valid for
    /// Text kind). Requires the <c>If-Match</c> header. Slice 5 Chunk 7.
    /// </summary>
    [HttpPatch("{id:guid}/decorations/{decorationId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateDecoration(Guid id, Guid decorationId, [FromBody] UpdateDecorationRequest request)
    {
        if (!TryParseIfMatch(out var expectedRowVersion, out var badRequest))
        {
            return badRequest!;
        }

        DecorationKind? kind = null;
        if (!string.IsNullOrWhiteSpace(request.Kind))
        {
            if (!Enum.TryParse<DecorationKind>(request.Kind, ignoreCase: true, out var parsed))
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Bad Request",
                    Status = StatusCodes.Status400BadRequest,
                    Detail = $"Invalid decoration kind: '{request.Kind}'. Valid: Stage, DanceFloor, Aisle, Door, Wall, Text, Image"
                });
            }
            kind = parsed;
        }

        Logger.LogInformation(
            "Updating decoration {DecorationId} in layout {LayoutId}: ExpectedRowVersion={RowVersion}",
            decorationId, id, expectedRowVersion);

        var command = new UpdateDecorationCommand(
            id, decorationId, expectedRowVersion,
            kind,
            request.Label,
            request.ClearLabel ?? false,
            request.SortOrder,
            request.Geometry,
            request.Properties);

        var result = await Mediator.Send(command);

        return HandleResultNoContent(result);
    }

    /// <summary>
    /// Delete a decoration from a layout. No structural guard — decorations
    /// have no seats. Requires the <c>If-Match</c> header. Slice 5 Chunk 7.
    /// </summary>
    [HttpDelete("{id:guid}/decorations/{decorationId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteDecoration(Guid id, Guid decorationId)
    {
        if (!TryParseIfMatch(out var expectedRowVersion, out var badRequest))
        {
            return badRequest!;
        }

        Logger.LogInformation(
            "Deleting decoration {DecorationId} from layout {LayoutId}: ExpectedRowVersion={RowVersion}",
            decorationId, id, expectedRowVersion);

        var command = new DeleteDecorationCommand(id, decorationId, expectedRowVersion);
        var result = await Mediator.Send(command);

        return HandleResultNoContent(result);
    }

    // ==================== TIER ASSIGNMENTS (Slice 5 Chunk 8) ====================

    /// <summary>
    /// Assign a ticket tier to a zone or table on the layout via the polymorphic
    /// <c>tier_assignments</c> junction. Idempotent — re-assigning an existing
    /// tuple is a no-op. Requires the <c>If-Match</c> header (validated against
    /// the layout's current RowVersion). Does NOT bump the layout's xmin
    /// (assignments live on the <c>TicketTier</c> aggregate). Slice 5 Chunk 8.
    /// </summary>
    [HttpPost("{id:guid}/tier-assignments")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignTier(Guid id, [FromBody] AssignTierRequest request)
    {
        if (!TryParseIfMatch(out var expectedRowVersion, out var badRequest))
        {
            return badRequest!;
        }

        if (!Enum.TryParse<AssignableKind>(request.Kind, ignoreCase: true, out var kind))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Bad Request",
                Status = StatusCodes.Status400BadRequest,
                Detail = $"Invalid assignable kind: '{request.Kind}'. Valid: Zone, Table"
            });
        }

        Logger.LogInformation(
            "Assigning tier {TierId} to {Kind} {AssignableId} on layout {LayoutId}: ExpectedRowVersion={RowVersion}",
            request.TierId, kind, request.AssignableId, id, expectedRowVersion);

        var command = new AssignTierCommand(
            id,
            expectedRowVersion,
            request.TierId,
            kind,
            request.AssignableId);

        var result = await Mediator.Send(command);

        return HandleResultNoContent(result);
    }

    /// <summary>
    /// Remove a ticket tier from a zone or table. Returns 404 when the tuple
    /// does not exist (so the client can distinguish stale UI state from
    /// success). Requires the <c>If-Match</c> header. Slice 5 Chunk 8.
    /// </summary>
    [HttpDelete("{id:guid}/tier-assignments/{tierId:guid}/{kind}/{assignableId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RemoveTierAssignment(Guid id, Guid tierId, string kind, Guid assignableId)
    {
        if (!TryParseIfMatch(out var expectedRowVersion, out var badRequest))
        {
            return badRequest!;
        }

        if (!Enum.TryParse<AssignableKind>(kind, ignoreCase: true, out var parsedKind))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Bad Request",
                Status = StatusCodes.Status400BadRequest,
                Detail = $"Invalid assignable kind: '{kind}'. Valid: Zone, Table"
            });
        }

        Logger.LogInformation(
            "Removing tier {TierId} from {Kind} {AssignableId} on layout {LayoutId}: ExpectedRowVersion={RowVersion}",
            tierId, parsedKind, assignableId, id, expectedRowVersion);

        var command = new RemoveTierAssignmentCommand(
            id,
            expectedRowVersion,
            tierId,
            parsedKind,
            assignableId);

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

/// <summary>
/// Slice 5 Chunk 6: POST table body. Shape is accepted as string for
/// JSON-friendliness and parsed against <c>TableShape</c> at the controller
/// layer. <c>StartAngleDeg</c> applies only to round tables (default 0°).
/// </summary>
public record AddTableRequest(
    string Label,
    string Shape,
    int Capacity,
    int SortOrder,
    Guid? ZoneId,
    string? Geometry,
    double? StartAngleDeg);

/// <summary>
/// Slice 5 Chunk 6: response returned from POST /tables so the client can
/// reference the newly created table before re-fetching the layout.
/// </summary>
public record AddTableResponse(Guid TableId);

/// <summary>
/// Slice 5 Chunk 6: PATCH table body. All fields optional — at least one must
/// be provided. To detach a table from its current zone pass
/// <c>clearZoneId: true</c> (supplying <c>zoneId: null</c> alone is treated as
/// "keep current zone" to preserve JSON omission semantics).
/// </summary>
public record UpdateTableRequest(
    string? Label,
    string? Shape,
    int? Capacity,
    int? SortOrder,
    Guid? ZoneId,
    bool? ClearZoneId,
    string? Geometry);

/// <summary>
/// Slice 5 Chunk 7: POST decoration body. Kind is accepted as a string for
/// JSON-friendliness and parsed against <c>DecorationKind</c> at the controller
/// layer. <c>Label</c> is required only for the <c>Text</c> kind.
/// </summary>
public record AddDecorationRequest(
    string Kind,
    string? Label,
    int SortOrder,
    string? Geometry,
    string? Properties);

/// <summary>
/// Slice 5 Chunk 7: response returned from POST /decorations so the client can
/// reference the newly created decoration before re-fetching the layout.
/// </summary>
public record AddDecorationResponse(Guid DecorationId);

/// <summary>
/// Slice 5 Chunk 7: PATCH decoration body. All fields optional — at least one
/// must be provided. Pass <c>clearLabel: true</c> to detach the label (not
/// valid when target kind is <c>Text</c>).
/// </summary>
public record UpdateDecorationRequest(
    string? Kind,
    string? Label,
    bool? ClearLabel,
    int? SortOrder,
    string? Geometry,
    string? Properties);

/// <summary>
/// Slice 5 Chunk 8: POST tier-assignment body. <c>Kind</c> is a string
/// ("Zone" or "Table") parsed against <see cref="AssignableKind"/> at the
/// controller layer for JSON-friendliness.
/// </summary>
public record AssignTierRequest(
    Guid TierId,
    string Kind,
    Guid AssignableId);

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
