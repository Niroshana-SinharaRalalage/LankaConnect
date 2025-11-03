using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Events.Commands.CreateEvent;
using LankaConnect.Application.Events.Commands.UpdateEvent;
using LankaConnect.Application.Events.Commands.DeleteEvent;
using LankaConnect.Application.Events.Commands.PublishEvent;
using LankaConnect.Application.Events.Commands.CancelEvent;
using LankaConnect.Application.Events.Commands.PostponeEvent;
using LankaConnect.Application.Events.Commands.SubmitEventForApproval;
using LankaConnect.Application.Events.Commands.RsvpToEvent;
using LankaConnect.Application.Events.Commands.CancelRsvp;
using LankaConnect.Application.Events.Commands.UpdateRsvp;
using LankaConnect.Application.Events.Commands.AdminApproval;
using LankaConnect.Application.Events.Queries.GetEventById;
using LankaConnect.Application.Events.Queries.GetEvents;
using LankaConnect.Application.Events.Queries.GetUserRsvps;
using LankaConnect.Application.Events.Queries.GetUpcomingEventsForUser;
using LankaConnect.Application.Events.Queries.GetPendingEventsForApproval;
using LankaConnect.Application.Events.Commands.AddImageToEvent;
using LankaConnect.Application.Events.Commands.DeleteEventImage;
using LankaConnect.Application.Events.Commands.ReorderEventImages;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Shared.Enums;

namespace LankaConnect.API.Controllers;

public class EventsController : BaseController<EventsController>
{
    public EventsController(IMediator mediator, ILogger<EventsController> logger) : base(mediator, logger)
    {
    }

    // ==================== PUBLIC ENDPOINTS ====================

    /// <summary>
    /// Get all events with optional filtering
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetEvents(
        [FromQuery] EventStatus? status = null,
        [FromQuery] EventCategory? category = null,
        [FromQuery] DateTime? startDateFrom = null,
        [FromQuery] DateTime? startDateTo = null,
        [FromQuery] bool? isFreeOnly = null,
        [FromQuery] string? city = null)
    {
        Logger.LogInformation("Getting events with filters: status={Status}, category={Category}, city={City}",
            status, category, city);

        var query = new GetEventsQuery(status, category, startDateFrom, startDateTo, isFreeOnly, city);
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    /// <summary>
    /// Get event details by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetEventById(Guid id)
    {
        Logger.LogInformation("Getting event by ID: {EventId}", id);

        var query = new GetEventByIdQuery(id);
        var result = await Mediator.Send(query);

        if (result.IsFailure && result.Errors.FirstOrDefault()?.Contains("not found") == true)
        {
            return NotFound();
        }

        return HandleResult(result);
    }

    // ==================== AUTHENTICATED ENDPOINTS ====================

    /// <summary>
    /// Create a new event (Organizers only)
    /// </summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventCommand command)
    {
        Logger.LogInformation("Creating event: {Title}", command.Title);

        var result = await Mediator.Send(command);

        return HandleResultWithCreated(result, nameof(GetEventById), new { id = result.Value });
    }

    /// <summary>
    /// Update an existing event (Owner only)
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvent(Guid id, [FromBody] UpdateEventCommand command)
    {
        Logger.LogInformation("Updating event: {EventId}", id);

        // Ensure ID in route matches command
        if (id != command.EventId)
        {
            return BadRequest("Event ID mismatch");
        }

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Delete an event (Owner only, draft/cancelled events only)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvent(Guid id)
    {
        Logger.LogInformation("Deleting event: {EventId}", id);

        var command = new DeleteEventCommand(id);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Submit event for approval (Owner only)
    /// </summary>
    [HttpPost("{id:guid}/submit")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SubmitForApproval(Guid id)
    {
        Logger.LogInformation("Submitting event for approval: {EventId}", id);

        var command = new SubmitEventForApprovalCommand(id);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    // ==================== STATUS CHANGE ENDPOINTS ====================

    /// <summary>
    /// Publish an event (Owner only)
    /// </summary>
    [HttpPost("{id:guid}/publish")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PublishEvent(Guid id)
    {
        Logger.LogInformation("Publishing event: {EventId}", id);

        var command = new PublishEventCommand(id);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Cancel an event with reason (Owner only)
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CancelEvent(Guid id, [FromBody] CancelEventRequest request)
    {
        Logger.LogInformation("Cancelling event: {EventId}", id);

        var command = new CancelEventCommand(id, request.Reason);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Postpone an event with reason (Owner only)
    /// </summary>
    [HttpPost("{id:guid}/postpone")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PostponeEvent(Guid id, [FromBody] PostponeEventRequest request)
    {
        Logger.LogInformation("Postponing event: {EventId}", id);

        var command = new PostponeEventCommand(id, request.Reason);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    // ==================== RSVP ENDPOINTS ====================

    /// <summary>
    /// RSVP to an event (Authenticated users)
    /// </summary>
    [HttpPost("{id:guid}/rsvp")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RsvpToEvent(Guid id, [FromBody] RsvpRequest request)
    {
        Logger.LogInformation("User {UserId} RSVPing to event {EventId} with quantity {Quantity}",
            request.UserId, id, request.Quantity);

        var command = new RsvpToEventCommand(id, request.UserId, request.Quantity);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Cancel RSVP to an event (Authenticated users)
    /// </summary>
    [HttpDelete("{id:guid}/rsvp")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CancelRsvp(Guid id, [FromQuery] Guid userId)
    {
        Logger.LogInformation("User {UserId} cancelling RSVP to event {EventId}", userId, id);

        var command = new CancelRsvpCommand(id, userId);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Update RSVP quantity (Authenticated users)
    /// </summary>
    [HttpPut("{id:guid}/rsvp")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateRsvp(Guid id, [FromBody] UpdateRsvpRequest request)
    {
        Logger.LogInformation("User {UserId} updating RSVP quantity for event {EventId} to {Quantity}",
            request.UserId, id, request.NewQuantity);

        var command = new UpdateRsvpCommand(id, request.UserId, request.NewQuantity);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    // ==================== USER DASHBOARD ENDPOINTS ====================

    /// <summary>
    /// Get user's RSVPs (Authenticated users)
    /// </summary>
    [HttpGet("my-rsvps")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<RsvpDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyRsvps([FromQuery] Guid userId)
    {
        Logger.LogInformation("Getting RSVPs for user: {UserId}", userId);

        var query = new GetUserRsvpsQuery(userId);
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    /// <summary>
    /// Get upcoming events for user (Authenticated users)
    /// </summary>
    [HttpGet("upcoming")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUpcomingEvents([FromQuery] Guid userId)
    {
        Logger.LogInformation("Getting upcoming events for user: {UserId}", userId);

        var query = new GetUpcomingEventsForUserQuery(userId);
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    // ==================== ADMIN ENDPOINTS ====================

    /// <summary>
    /// Get events pending approval (Admins only)
    /// </summary>
    [HttpGet("admin/pending")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(typeof(IReadOnlyList<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPendingEvents()
    {
        Logger.LogInformation("Getting events pending approval");

        var query = new GetPendingEventsForApprovalQuery();
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    /// <summary>
    /// Approve event (Admins only)
    /// </summary>
    [HttpPost("admin/{id:guid}/approve")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ApproveEvent(Guid id, [FromBody] ApproveEventRequest request)
    {
        Logger.LogInformation("Approving event {EventId} by admin {AdminId}", id, request.ApprovedByAdminId);

        var command = new ApproveEventCommand(id, request.ApprovedByAdminId);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Reject event with reason (Admins only)
    /// </summary>
    [HttpPost("admin/{id:guid}/reject")]
    [Authorize(Policy = "AdminOnly")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RejectEvent(Guid id, [FromBody] RejectEventRequest request)
    {
        Logger.LogInformation("Rejecting event {EventId} by admin {AdminId}", id, request.RejectedByAdminId);

        var command = new RejectEventCommand(id, request.RejectedByAdminId, request.Reason);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    #region Event Images (Epic 2 Phase 2)

    /// <summary>
    /// Add image to event gallery
    /// </summary>
    [HttpPost("{id:guid}/images")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(EventImage), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddImageToEvent(Guid id, IFormFile image)
    {
        if (image == null || image.Length == 0)
            return BadRequest("Image file is required");

        // Read image data
        using var memoryStream = new MemoryStream();
        await image.CopyToAsync(memoryStream);
        var imageData = memoryStream.ToArray();

        Logger.LogInformation("Adding image to event {EventId}, FileName={FileName}, Size={Size}",
            id, image.FileName, imageData.Length);

        var command = new AddImageToEventCommand
        {
            EventId = id,
            ImageData = imageData,
            FileName = image.FileName
        };

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Delete image from event gallery
    /// </summary>
    [HttpDelete("{eventId:guid}/images/{imageId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteEventImage(Guid eventId, Guid imageId)
    {
        Logger.LogInformation("Deleting image {ImageId} from event {EventId}", imageId, eventId);

        var command = new DeleteEventImageCommand
        {
            EventId = eventId,
            ImageId = imageId
        };

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Reorder event images
    /// </summary>
    [HttpPut("{id:guid}/images/reorder")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ReorderEventImages(Guid id, [FromBody] EventReorderImagesRequest request)
    {
        Logger.LogInformation("Reordering images for event {EventId}", id);

        var command = new ReorderEventImagesCommand
        {
            EventId = id,
            NewOrders = request.NewOrders
        };

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    #endregion
}

// Request DTOs
public record CancelEventRequest(string Reason);
public record PostponeEventRequest(string Reason);
public record RsvpRequest(Guid UserId, int Quantity = 1);
public record UpdateRsvpRequest(Guid UserId, int NewQuantity);
public record ApproveEventRequest(Guid ApprovedByAdminId);
public record RejectEventRequest(Guid RejectedByAdminId, string Reason);
public record EventReorderImagesRequest(Dictionary<Guid, int> NewOrders); // Epic 2 Phase 2
