using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using LankaConnect.Application.Events.Commands.CreateEvent;
using LankaConnect.Application.Events.Commands.UpdateEvent;
using LankaConnect.Application.Events.Commands.DeleteEvent;
using LankaConnect.Application.Events.Commands.PublishEvent;
using LankaConnect.Application.Events.Commands.UnpublishEvent;
using LankaConnect.Application.Events.Commands.CancelEvent;
using LankaConnect.Application.Events.Commands.PostponeEvent;
using LankaConnect.Application.Events.Commands.SubmitEventForApproval;
using LankaConnect.Application.Events.Commands.RsvpToEvent;
using LankaConnect.Application.Events.Commands.CancelRsvp;
using LankaConnect.Application.Events.Commands.UpdateRsvp;
using LankaConnect.Application.Events.Commands.ResendTicketEmail;
using LankaConnect.Application.Events.Commands.UpdateRegistrationDetails;
using LankaConnect.Application.Events.Commands.RegisterAnonymousAttendee;
using LankaConnect.Application.Events.Commands.AdminApproval;
using LankaConnect.Application.Events.Queries.GetEventById;
using LankaConnect.Application.Events.Queries.GetEvents;
using LankaConnect.Application.Events.Queries.GetEventsByOrganizer;
using LankaConnect.Application.Events.Queries.GetMyRegisteredEvents;
using LankaConnect.Application.Events.Queries.GetNearbyEvents;
using LankaConnect.Application.Events.Queries.GetUserRsvps;
using LankaConnect.Application.Events.Queries.GetUserRegistrationForEvent;
using LankaConnect.Application.Events.Queries.GetEventRegistrationByEmail;
using LankaConnect.Application.Events.Queries.GetUpcomingEventsForUser;
using LankaConnect.Application.Events.Queries.GetPendingEventsForApproval;
using LankaConnect.Application.Events.Queries.SearchEvents;
using LankaConnect.Application.Events.Queries.GetFeaturedEvents;
using LankaConnect.Application.Common.Models;
using LankaConnect.Application.Events.Commands.AddImageToEvent;
using LankaConnect.Application.Events.Commands.DeleteEventImage;
using LankaConnect.Application.Events.Commands.ReorderEventImages;
using LankaConnect.Application.Events.Commands.SetPrimaryImage;
using LankaConnect.Application.Events.Commands.ReplaceEventImage;
using LankaConnect.Application.Events.Commands.AddVideoToEvent;
using LankaConnect.Application.Events.Commands.DeleteEventVideo;
using LankaConnect.Application.Events.Common;
using LankaConnect.Application.Analytics.Commands.RecordEventView;
using LankaConnect.Application.Analytics.Commands.RecordEventShare;
using LankaConnect.Application.Events.Commands.AddToWaitingList;
using LankaConnect.Application.Events.Commands.RemoveFromWaitingList;
using LankaConnect.Application.Events.Commands.PromoteFromWaitingList;
using LankaConnect.Application.Events.Queries.GetWaitingList;
using LankaConnect.Application.Events.Queries.GetEventIcs;
using LankaConnect.Application.Events.Commands.AddPassToEvent;
using LankaConnect.Application.Events.Commands.RemovePassFromEvent;
using LankaConnect.Application.Events.Queries.GetEventPasses;
using LankaConnect.Application.Events.Commands.RemoveSignUpListFromEvent;
using LankaConnect.Application.Events.Queries.GetEventSignUpLists;
using LankaConnect.Application.Events.Commands.CreateSignUpListWithItems;
using LankaConnect.Application.Events.Commands.UpdateSignUpList;
using LankaConnect.Application.Events.Commands.AddSignUpItem;
using LankaConnect.Application.Events.Commands.UpdateSignUpItem;
using LankaConnect.Application.Events.Commands.RemoveSignUpItem;
using LankaConnect.Application.Events.Commands.CommitToSignUpItem;
using LankaConnect.Application.Events.Commands.CommitToSignUpItemAnonymous;
using LankaConnect.Application.Events.Commands.AddOpenSignUpItem;
using LankaConnect.Application.Events.Commands.UpdateOpenSignUpItem;
using LankaConnect.Application.Events.Commands.CancelOpenSignUpItem;
using LankaConnect.Application.Events.Queries.CheckEventRegistration;
using LankaConnect.Application.Events.Queries.GetTicket;
using LankaConnect.Application.Events.Queries.GetTicketPdf;
using LankaConnect.API.Extensions;
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
    /// Get all events with optional filtering and location-based sorting
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
        [FromQuery] string? city = null,
        [FromQuery] string? state = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] decimal? latitude = null,
        [FromQuery] decimal? longitude = null,
        [FromQuery] List<Guid>? metroAreaIds = null)
    {
        Logger.LogInformation(
            "Getting events with filters: status={Status}, category={Category}, city={City}, state={State}, userId={UserId}",
            status, category, city, state, userId);

        var query = new GetEventsQuery(
            status,
            category,
            startDateFrom,
            startDateTo,
            isFreeOnly,
            city,
            state,
            userId,
            latitude,
            longitude,
            metroAreaIds);

        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    /// <summary>
    /// Search events using full-text search (Epic 2 Phase 3 - PostgreSQL FTS)
    /// </summary>
    /// <param name="searchTerm">Search term to match against event titles and descriptions</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20, max: 100)</param>
    /// <param name="category">Optional category filter</param>
    /// <param name="isFreeOnly">Optional filter for free events only</param>
    /// <param name="startDateFrom">Optional filter for events starting from this date</param>
    /// <returns>Paginated list of matching events ordered by relevance</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(PagedResult<EventSearchResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchEvents(
        [FromQuery] string searchTerm,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] EventCategory? category = null,
        [FromQuery] bool? isFreeOnly = null,
        [FromQuery] DateTime? startDateFrom = null)
    {
        Logger.LogInformation("Searching events: term='{SearchTerm}', page={Page}, pageSize={PageSize}, category={Category}",
            searchTerm, page, pageSize, category);

        var query = new SearchEventsQuery(searchTerm, page, pageSize, category, isFreeOnly, startDateFrom);
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

        // Fire-and-forget: Record event view for analytics (non-blocking)
        // This runs asynchronously and doesn't affect the response time
        if (result.IsSuccess && result.Value != null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var userId = User.Identity?.IsAuthenticated == true ? User.TryGetUserId() : null;
                    var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "0.0.0.0";
                    var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

                    var recordViewCommand = new RecordEventViewCommand(id, userId, ipAddress, userAgent);
                    await Mediator.Send(recordViewCommand);

                    Logger.LogDebug("Event view recorded for: {EventId}, User: {UserId}, IP: {IpAddress}",
                        id, userId, ipAddress);
                }
                catch (Exception ex)
                {
                    // Fail-silent: Don't let analytics errors affect the main request
                    Logger.LogWarning(ex, "Failed to record event view for: {EventId}", id);
                }
            });
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Get nearby events within a specified radius of a location (Epic 2 Phase 3 - Spatial Queries)
    /// </summary>
    /// <param name="latitude">Latitude coordinate (-90 to 90)</param>
    /// <param name="longitude">Longitude coordinate (-180 to 180)</param>
    /// <param name="radiusKm">Search radius in kilometers (0.1 to 1000)</param>
    /// <param name="category">Optional event category filter</param>
    /// <param name="isFreeOnly">Optional filter for free events only</param>
    /// <param name="startDateFrom">Optional filter for events starting from this date</param>
    [HttpGet("nearby")]
    [ProducesResponseType(typeof(IReadOnlyList<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetNearbyEvents(
        [FromQuery] decimal latitude,
        [FromQuery] decimal longitude,
        [FromQuery] double radiusKm,
        [FromQuery] EventCategory? category = null,
        [FromQuery] bool? isFreeOnly = null,
        [FromQuery] DateTime? startDateFrom = null)
    {
        Logger.LogInformation("Getting nearby events: lat={Latitude}, lon={Longitude}, radius={RadiusKm}km",
            latitude, longitude, radiusKm);

        var query = new GetNearbyEventsQuery(latitude, longitude, radiusKm, category, isFreeOnly, startDateFrom);
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    /// <summary>
    /// Get featured events for the landing page
    /// Returns up to 4 events sorted by location relevance
    /// - For authenticated users: Uses preferred metro areas or user location
    /// - For anonymous users: Uses provided coordinates or default location
    /// </summary>
    /// <param name="userId">Optional authenticated user ID</param>
    /// <param name="latitude">Optional latitude for anonymous users</param>
    /// <param name="longitude">Optional longitude for anonymous users</param>
    [HttpGet("featured")]
    [ProducesResponseType(typeof(IReadOnlyList<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetFeaturedEvents(
        [FromQuery] Guid? userId = null,
        [FromQuery] decimal? latitude = null,
        [FromQuery] decimal? longitude = null)
    {
        Logger.LogInformation("Getting featured events: userId={UserId}, lat={Latitude}, lon={Longitude}",
            userId, latitude, longitude);

        var query = new GetFeaturedEventsQuery(userId, latitude, longitude);
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    // ==================== AUTHENTICATED ENDPOINTS ====================

    /// <summary>
    /// Create a new event (Event Organizers, Admins only)
    /// Phase 6A.3: Requires EventOrganizer, Admin, or AdminManager role with active subscription
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "CanCreateEvents")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventCommand command)
    {
        // PHASE 6A.10: Comprehensive diagnostic logging
        var userId = User.GetUserId(); // Get authenticated user ID
        var userRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var isAuthenticated = User.Identity?.IsAuthenticated ?? false;

        // CRITICAL FIX: Override OrganizerId with authenticated user ID for security
        // The client should NOT be able to set OrganizerId - it must come from the JWT token
        var secureCommand = command with { OrganizerId = userId };

        Logger.LogInformation("🎯 CreateEvent - Request Details:");
        Logger.LogInformation("   User ID: {UserId}", userId);
        Logger.LogInformation("   User Role: {UserRole}", userRole);
        Logger.LogInformation("   Is Authenticated: {IsAuthenticated}", isAuthenticated);
        Logger.LogInformation("   Event Title: {Title}", secureCommand.Title);
        Logger.LogInformation("   Organizer ID (from JWT): {OrganizerId}", secureCommand.OrganizerId);
        Logger.LogInformation("   Authorization Policy: CanCreateEvents");

        // Log all user claims for debugging
        var claims = User.Claims.Select(c => $"{c.Type}={c.Value}");
        Logger.LogInformation("   User Claims: {Claims}", string.Join(", ", claims));

        Logger.LogInformation("⏳ Sending command to MediatR handler...");
        var result = await Mediator.Send(secureCommand);

        if (result.IsSuccess)
        {
            Logger.LogInformation("✅ Event created successfully: EventId={EventId}", result.Value);
            return HandleResultWithCreated(result, nameof(GetEventById), new { id = result.Value });
        }
        else
        {
            Logger.LogError("❌ Event creation failed: {Errors}", string.Join(", ", result.Errors));
            return HandleResult(result);
        }
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
    /// Phase 6A.41: Unpublish an event (return to Draft status) (Owner only)
    /// Allows organizers to make corrections after premature publication.
    /// </summary>
    [HttpPost("{id:guid}/unpublish")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UnpublishEvent(Guid id)
    {
        Logger.LogInformation("Unpublishing event: {EventId}", id);

        var command = new UnpublishEventCommand(id);
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
    /// Phase 6A.11: Updated to support multi-attendee registrations with detailed attendee information
    /// - Legacy format: { userId, quantity } - simple quantity-based RSVP
    /// - New format: { userId, attendees: [{name, age}, ...], email, phoneNumber, address, successUrl, cancelUrl } - multi-attendee with details
    /// </summary>
    [HttpPost("{id:guid}/rsvp")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RsvpToEvent(Guid id, [FromBody] RsvpRequest request)
    {
        // Phase 6A.11: Determine format and log appropriately
        if (request.Attendees?.Any() == true)
        {
            Logger.LogInformation("User {UserId} RSVPing to event {EventId} with {AttendeeCount} multi-attendee registrations (new format)",
                request.UserId, id, request.Attendees.Count);
        }
        else
        {
            Logger.LogInformation("User {UserId} RSVPing to event {EventId} with quantity {Quantity} (legacy format)",
                request.UserId, id, request.Quantity);
        }

        // Phase 6A.11: Map all DTO fields to command (including multi-attendee fields)
        var command = new RsvpToEventCommand(
            id,
            request.UserId,
            request.Quantity,
            request.Attendees,
            request.Email,
            request.PhoneNumber,
            request.Address,
            request.SuccessUrl,
            request.CancelUrl
        );
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Phase 6A.24: Resend ticket email to the registered user
    /// Only the registration owner can resend their ticket
    /// </summary>
    [HttpPost("registrations/{registrationId:guid}/resend-ticket")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ResendTicket(Guid registrationId)
    {
        // Get current user ID from claims
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            Logger.LogWarning("Resend ticket attempted without valid user ID claim");
            return Unauthorized();
        }

        Logger.LogInformation("User {UserId} requesting to resend ticket for Registration {RegistrationId}",
            userId, registrationId);

        var command = new ResendTicketEmailCommand(registrationId, userId);
        var result = await Mediator.Send(command);

        if (result.IsSuccess)
        {
            return Ok(new { message = "Ticket email resent successfully" });
        }

        // Check if it's an authorization error
        if (result.Errors.Any(e => e.Contains("Not authorized")))
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = result.Errors.First() });
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Register anonymous attendee for an event (No authentication required)
    /// </summary>
    [HttpPost("{id:guid}/register-anonymous")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterAnonymousAttendee(Guid id, [FromBody] AnonymousRegistrationRequest request)
    {
        Logger.LogInformation("Anonymous attendee {Email} registering for event {EventId}",
            request.Email, id);

        // Phase 6A.43: Support both legacy and multi-attendee formats
        // Convert AnonymousAttendeeDto to Application layer AttendeeDto if provided
        List<LankaConnect.Application.Events.Commands.RegisterAnonymousAttendee.AttendeeDto>? attendees = null;
        if (request.Attendees != null && request.Attendees.Any())
        {
            attendees = request.Attendees.Select(a =>
                new LankaConnect.Application.Events.Commands.RegisterAnonymousAttendee.AttendeeDto(
                    a.Name,
                    a.AgeCategory,
                    a.Gender
                )).ToList();
        }

        var command = new RegisterAnonymousAttendeeCommand(
            EventId: id,
            Name: request.Name,
            Age: request.Age,
            Attendees: attendees, // Phase 6A.43: Pass attendees array from request
            Email: request.Email,
            PhoneNumber: request.PhoneNumber,
            Address: request.Address,
            Quantity: request.Quantity
        );

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Cancel RSVP to an event (Authenticated users)
    /// Phase 6A.28: Added deleteSignUpCommitments parameter for user choice
    /// </summary>
    /// <param name="id">The event ID</param>
    /// <param name="deleteSignUpCommitments">
    /// If true, deletes all sign-up commitments and restores remaining quantities.
    /// If false (default), keeps sign-up commitments intact.
    /// </param>
    [HttpDelete("{id:guid}/rsvp")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CancelRsvp(Guid id, [FromQuery] bool deleteSignUpCommitments = false)
    {
        var userId = User.GetUserId();
        Logger.LogInformation("User {UserId} cancelling RSVP to event {EventId}, DeleteSignUpCommitments={DeleteSignUpCommitments}",
            userId, id, deleteSignUpCommitments);

        var command = new CancelRsvpCommand(id, userId, deleteSignUpCommitments);
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

    // ==================== TICKET ENDPOINTS (Phase 6A.24) ====================

    /// <summary>
    /// Get ticket details for a user's registration
    /// Phase 6A.24: Ticket viewing for paid events
    /// </summary>
    [HttpGet("{eventId:guid}/my-registration/ticket")]
    [Authorize]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyTicket(Guid eventId)
    {
        var userId = User.GetUserId();
        Logger.LogInformation("Getting ticket for user {UserId} for event {EventId}", userId, eventId);

        // First get the registration to get the registration ID
        var registrationQuery = new GetUserRegistrationForEventQuery(eventId, userId);
        var registrationResult = await Mediator.Send(registrationQuery);

        if (registrationResult.IsFailure || registrationResult.Value == null)
        {
            Logger.LogInformation("No registration found for user {UserId} for event {EventId}", userId, eventId);
            return NotFound(new { message = "You are not registered for this event" });
        }

        var query = new GetTicketQuery(eventId, registrationResult.Value.Id, userId);
        var result = await Mediator.Send(query);

        if (result.IsFailure && result.Errors.FirstOrDefault()?.Contains("not found") == true)
        {
            return NotFound(new { message = result.Error });
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Download ticket as PDF
    /// Phase 6A.24: Ticket PDF download for paid events
    /// </summary>
    [HttpGet("{eventId:guid}/my-registration/ticket/pdf")]
    [Authorize]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DownloadMyTicketPdf(Guid eventId)
    {
        var userId = User.GetUserId();
        Logger.LogInformation("Downloading ticket PDF for user {UserId} for event {EventId}", userId, eventId);

        // First get the registration to get the registration ID
        var registrationQuery = new GetUserRegistrationForEventQuery(eventId, userId);
        var registrationResult = await Mediator.Send(registrationQuery);

        if (registrationResult.IsFailure || registrationResult.Value == null)
        {
            Logger.LogInformation("No registration found for user {UserId} for event {EventId}", userId, eventId);
            return NotFound(new { message = "You are not registered for this event" });
        }

        var query = new GetTicketPdfQuery(eventId, registrationResult.Value.Id, userId);
        var result = await Mediator.Send(query);

        if (result.IsFailure)
        {
            if (result.Errors.FirstOrDefault()?.Contains("not found") == true)
            {
                return NotFound(new { message = result.Error });
            }
            return HandleResult(result);
        }

        return File(result.Value.PdfBytes, "application/pdf", result.Value.FileName);
    }

    /// <summary>
    /// Resend ticket email to registration contact
    /// Phase 6A.24: Allows users to request ticket email resend
    /// </summary>
    [HttpPost("{eventId:guid}/my-registration/ticket/resend-email")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ResendTicketEmail(Guid eventId)
    {
        var userId = User.GetUserId();
        Logger.LogInformation("Resending ticket email for user {UserId} for event {EventId}", userId, eventId);

        // First get the registration to get the registration ID
        var registrationQuery = new GetUserRegistrationForEventQuery(eventId, userId);
        var registrationResult = await Mediator.Send(registrationQuery);

        if (registrationResult.IsFailure || registrationResult.Value == null)
        {
            Logger.LogInformation("No registration found for user {UserId} for event {EventId}", userId, eventId);
            return NotFound(new { message = "You are not registered for this event" });
        }

        var command = new ResendTicketEmailCommand(registrationResult.Value.Id, userId);
        var result = await Mediator.Send(command);

        if (result.IsFailure && result.Errors.FirstOrDefault()?.Contains("not found") == true)
        {
            return NotFound(new { message = result.Error });
        }

        return HandleResult(result);
    }

    // ==================== USER DASHBOARD ENDPOINTS ====================

    /// <summary>
    /// Get events created by current user (Authenticated Event Organizers/Admins)
    /// Epic 1: Dashboard my-events endpoint
    /// </summary>
    [HttpGet("my-events")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyEvents()
    {
        var userId = User.GetUserId();
        Logger.LogInformation("Getting events created by user: {UserId}", userId);

        var query = new GetEventsByOrganizerQuery(userId);
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    /// <summary>
    /// Get events user has registered for (Authenticated users)
    /// Epic 1: Returns full EventDto instead of RsvpDto for better dashboard UX
    /// </summary>
    [HttpGet("my-rsvps")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyRsvps()
    {
        var userId = User.GetUserId();
        Logger.LogInformation("Getting registered events for user: {UserId}", userId);

        var query = new GetMyRegisteredEventsQuery(userId);
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    /// <summary>
    /// Get user's registration details for a specific event
    /// Returns full registration with attendee names and ages
    /// Fix 1: Registration Status Detection Enhancement
    /// </summary>
    [HttpGet("{eventId}/my-registration")]
    [Authorize]
    [ProducesResponseType(typeof(RegistrationDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyRegistrationForEvent(Guid eventId)
    {
        var userId = User.GetUserId();
        Logger.LogInformation("Getting registration details for user {UserId} for event {EventId}", userId, eventId);

        var query = new GetUserRegistrationForEventQuery(eventId, userId);
        var result = await Mediator.Send(query);

        if (result == null)
        {
            Logger.LogInformation("No registration found for user {UserId} for event {EventId}", userId, eventId);
            return NotFound(new { message = "You are not registered for this event" });
        }

        return Ok(result);
    }

    /// <summary>
    /// Phase 6A.14: Update user's registration details (attendees and contact information)
    /// Allows users to edit their registration after initial RSVP
    /// Business Rules:
    /// - User must have an active registration
    /// - Cannot change attendee count on paid registrations (only names/ages)
    /// - Maximum 10 attendees per registration
    /// - Cannot update cancelled or refunded registrations
    /// </summary>
    [HttpPut("{eventId}/my-registration")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMyRegistration(Guid eventId, [FromBody] UpdateRegistrationRequest request)
    {
        var userId = User.GetUserId();
        Logger.LogInformation("User {UserId} updating registration for event {EventId} with {AttendeeCount} attendees",
            userId, eventId, request.Attendees?.Count ?? 0);

        var command = new UpdateRegistrationDetailsCommand(
            eventId,
            userId,
            request.Attendees?.Select(a => new Application.Events.Commands.RsvpToEvent.AttendeeDto(a.Name, a.AgeCategory, a.Gender)).ToList()
                ?? new List<Application.Events.Commands.RsvpToEvent.AttendeeDto>(),
            request.Email,
            request.PhoneNumber,
            request.Address);

        var result = await Mediator.Send(command);

        if (result.IsFailure)
        {
            Logger.LogWarning("Failed to update registration for user {UserId} for event {EventId}: {Errors}",
                userId, eventId, string.Join(", ", result.Errors));
        }

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
    public async Task<IActionResult> GetUpcomingEvents()
    {
        var userId = User.GetUserId();
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
    /// Replace an existing event image with a new one
    /// </summary>
    [HttpPut("{eventId:guid}/images/{imageId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReplaceEventImage(Guid eventId, Guid imageId, IFormFile image)
    {
        Logger.LogInformation("Replacing image {ImageId} in event {EventId}", imageId, eventId);

        if (image == null || image.Length == 0)
        {
            return BadRequest("Image file is required");
        }

        using var memoryStream = new MemoryStream();
        await image.CopyToAsync(memoryStream);

        var command = new ReplaceEventImageCommand
        {
            EventId = eventId,
            ImageId = imageId,
            ImageData = memoryStream.ToArray(),
            FileName = image.FileName
        };

        var result = await Mediator.Send(command);

        if (result.IsFailure && result.Errors.FirstOrDefault()?.Contains("not found") == true)
        {
            return NotFound(result.Error);
        }

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

    /// <summary>
    /// Set an image as the primary/main thumbnail for the event
    /// Phase 6A.13: Primary image selection feature
    /// </summary>
    [HttpPost("{id:guid}/images/{imageId:guid}/set-primary")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPrimaryImage(Guid id, Guid imageId)
    {
        Logger.LogInformation("Setting image {ImageId} as primary for event {EventId}", imageId, id);

        var command = new SetPrimaryImageCommand
        {
            EventId = id,
            ImageId = imageId
        };

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    #endregion

    #region Event Videos (Epic 2 Phase 2)

    /// <summary>
    /// Add video to event gallery
    /// </summary>
    [HttpPost("{id:guid}/videos")]
    [Authorize]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(100 * 1024 * 1024)] // 100MB limit for video uploads
    [ProducesResponseType(typeof(EventVideo), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddVideoToEvent(Guid id, IFormFile video, IFormFile thumbnail)
    {
        if (video == null || video.Length == 0)
            return BadRequest("Video file is required");

        if (thumbnail == null || thumbnail.Length == 0)
            return BadRequest("Thumbnail image is required");

        // Read video data
        using var videoStream = new MemoryStream();
        await video.CopyToAsync(videoStream);
        var videoData = videoStream.ToArray();

        // Read thumbnail data
        using var thumbnailStream = new MemoryStream();
        await thumbnail.CopyToAsync(thumbnailStream);
        var thumbnailData = thumbnailStream.ToArray();

        Logger.LogInformation("Adding video to event {EventId}, VideoFile={VideoFile}, ThumbnailFile={ThumbnailFile}, VideoSize={VideoSize}, ThumbSize={ThumbSize}",
            id, video.FileName, thumbnail.FileName, videoData.Length, thumbnailData.Length);

        var command = new AddVideoToEventCommand
        {
            EventId = id,
            VideoData = videoData,
            VideoFileName = video.FileName,
            ThumbnailData = thumbnailData,
            ThumbnailFileName = thumbnail.FileName,
            // TODO: Extract video metadata (duration, format) from file
            Duration = null,
            Format = Path.GetExtension(video.FileName).TrimStart('.')
        };

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Delete video from event gallery
    /// </summary>
    [HttpDelete("{eventId:guid}/videos/{videoId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteEventVideo(Guid eventId, Guid videoId)
    {
        Logger.LogInformation("Deleting video {VideoId} from event {EventId}", videoId, eventId);

        var command = new DeleteEventVideoCommand
        {
            EventId = eventId,
            VideoId = videoId
        };

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    #endregion

    #region Waiting List Endpoints (Epic 2)

    /// <summary>
    /// Add user to event waiting list (Authenticated users)
    /// </summary>
    [HttpPost("{id:guid}/waiting-list")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AddToWaitingList(Guid id)
    {
        var userId = User.GetUserId();
        Logger.LogInformation("Adding user {UserId} to waiting list for event {EventId}", userId, id);

        var command = new AddToWaitingListCommand(id, userId);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Remove user from event waiting list (Authenticated users)
    /// </summary>
    [HttpDelete("{id:guid}/waiting-list")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveFromWaitingList(Guid id)
    {
        var userId = User.GetUserId();
        Logger.LogInformation("Removing user {UserId} from waiting list for event {EventId}", userId, id);

        var command = new RemoveFromWaitingListCommand(id, userId);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Promote user from waiting list to confirmed registration (Authenticated users)
    /// </summary>
    [HttpPost("{id:guid}/waiting-list/promote")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PromoteFromWaitingList(Guid id)
    {
        var userId = User.GetUserId();
        Logger.LogInformation("Promoting user {UserId} from waiting list for event {EventId}", userId, id);

        var command = new PromoteFromWaitingListCommand(id, userId);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Get waiting list for an event
    /// </summary>
    [HttpGet("{id:guid}/waiting-list")]
    [ProducesResponseType(typeof(IReadOnlyList<WaitingListEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWaitingList(Guid id)
    {
        Logger.LogInformation("Getting waiting list for event {EventId}", id);

        var query = new GetWaitingListQuery(id);
        var result = await Mediator.Send(query);

        if (result.IsFailure && result.Errors.FirstOrDefault()?.Contains("not found") == true)
        {
            return NotFound();
        }

        return HandleResult(result);
    }

    #endregion

    #region Calendar Export (Epic 2)

    /// <summary>
    /// Export event as ICS calendar file (for Google Calendar, Apple Calendar, Outlook)
    /// </summary>
    [HttpGet("{id:guid}/ics")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetEventIcs(Guid id)
    {
        Logger.LogInformation("Generating ICS file for event {EventId}", id);

        var query = new GetEventIcsQuery(id);
        var result = await Mediator.Send(query);

        if (result.IsFailure && result.Errors.FirstOrDefault()?.Contains("not found") == true)
        {
            return NotFound();
        }

        if (result.IsFailure)
        {
            return HandleResult(result);
        }

        // Return ICS file as downloadable content
        var icsContent = System.Text.Encoding.UTF8.GetBytes(result.Value);
        return File(icsContent, "text/calendar", $"event-{id}.ics");
    }

    #endregion

    #region Social Sharing Analytics (Epic 2)

    /// <summary>
    /// Record a social share of an event (for analytics tracking)
    /// </summary>
    [HttpPost("{id:guid}/share")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordEventShare(Guid id, [FromBody] RecordShareRequest? request = null)
    {
        Logger.LogInformation("Recording social share for event {EventId}", id);

        var userId = User.Identity?.IsAuthenticated == true ? User.TryGetUserId() : null;
        var command = new RecordEventShareCommand(id, userId, request?.Platform);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    #endregion

    #region Event Pass Management

    /// <summary>
    /// Get all passes/tickets for an event
    /// </summary>
    [HttpGet("{id:guid}/passes")]
    [ProducesResponseType(typeof(IReadOnlyList<EventPassDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetEventPasses(Guid id)
    {
        Logger.LogInformation("Getting passes for event {EventId}", id);

        var query = new GetEventPassesQuery(id);
        var result = await Mediator.Send(query);

        if (result.IsFailure && result.Errors.FirstOrDefault()?.Contains("not found") == true)
        {
            return NotFound();
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Add a new pass/ticket type to an event (Event Organizer/Admin only)
    /// </summary>
    [HttpPost("{id:guid}/passes")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddPassToEvent(Guid id, [FromBody] AddPassRequest request)
    {
        Logger.LogInformation("Adding pass '{PassName}' to event {EventId}", request.PassName, id);

        var command = new AddPassToEventCommand(
            id,
            request.PassName,
            request.PassDescription,
            request.PriceAmount,
            request.PriceCurrency,
            request.TotalQuantity);

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Remove a pass/ticket type from an event (Event Organizer/Admin only)
    /// </summary>
    [HttpDelete("{eventId:guid}/passes/{passId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePassFromEvent(Guid eventId, Guid passId)
    {
        Logger.LogInformation("Removing pass {PassId} from event {EventId}", passId, eventId);

        var command = new RemovePassFromEventCommand(eventId, passId);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    #endregion

    #region Sign-Up Lists Management

    /// <summary>
    /// Get all sign-up lists for an event
    /// </summary>
    [HttpGet("{id:guid}/signups")]
    [ProducesResponseType(typeof(List<SignUpListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetEventSignUpLists(Guid id)
    {
        Logger.LogInformation("Getting sign-up lists for event {EventId}", id);

        var query = new GetEventSignUpListsQuery(id);
        var result = await Mediator.Send(query);

        if (result.IsFailure && result.Errors.FirstOrDefault()?.Contains("not found") == true)
        {
            return NotFound();
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Create a new sign-up list with items (Event Organizer/Admin only)
    /// Matches requirement: Create list WITH items in single API call
    /// </summary>
    [HttpPost("{id:guid}/signups")]
    [Authorize]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateSignUpList(Guid id, [FromBody] CreateSignUpListRequest request)
    {
        Logger.LogInformation("Creating sign-up list '{Category}' with {ItemCount} items for event {EventId}",
            request.Category, request.Items.Count, id);

        // Map API DTOs to Application layer DTOs
        var items = request.Items.Select(item => new LankaConnect.Application.Events.Commands.CreateSignUpListWithItems.SignUpItemDto(
            item.ItemDescription,
            item.Quantity,
            item.ItemCategory,
            item.Notes)).ToList();

        var command = new CreateSignUpListWithItemsCommand(
            id,
            request.Category,
            request.Description,
            request.HasMandatoryItems,
            request.HasPreferredItems,
            request.HasSuggestedItems,
            items,
            request.HasOpenItems); // Phase 6A.28: Open Items support

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Update sign-up list details (category, description, and category flags) (Event Organizer/Admin only)
    /// Phase 6A.13: Edit Sign-Up List feature
    /// </summary>
    [HttpPut("{eventId:guid}/signups/{signupId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSignUpList(Guid eventId, Guid signupId, [FromBody] UpdateSignUpListRequest request)
    {
        Logger.LogInformation("Updating sign-up list {SignUpId} for event {EventId} with category '{Category}'",
            signupId, eventId, request.Category);

        var command = new UpdateSignUpListCommand(
            eventId,
            signupId,
            request.Category,
            request.Description,
            request.HasMandatoryItems,
            request.HasPreferredItems,
            request.HasSuggestedItems,
            request.HasOpenItems); // Phase 6A.28: Open Items support

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Remove a sign-up list from an event (Event Organizer/Admin only)
    /// </summary>
    [HttpDelete("{eventId:guid}/signups/{signupId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveSignUpListFromEvent(Guid eventId, Guid signupId)
    {
        Logger.LogInformation("Removing sign-up list {SignUpId} from event {EventId}", signupId, eventId);

        var command = new RemoveSignUpListFromEventCommand(eventId, signupId);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    #region Category-Based Sign-Up Item Management

    /// <summary>
    /// Add an item to a category-based sign-up list (Event Organizer/Admin only)
    /// </summary>
    [HttpPost("{eventId:guid}/signups/{signupId:guid}/items")]
    [Authorize]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddSignUpItem(Guid eventId, Guid signupId, [FromBody] AddSignUpItemRequest request)
    {
        Logger.LogInformation("Adding item '{ItemDescription}' to sign-up list {SignUpId} for event {EventId}",
            request.ItemDescription, signupId, eventId);

        var command = new AddSignUpItemCommand(
            eventId,
            signupId,
            request.ItemDescription,
            request.Quantity,
            request.ItemCategory,
            request.Notes);

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Update an item in a category-based sign-up list (Event Organizer/Admin only)
    /// Phase 6A.14: Edit Sign-Up Item feature
    /// </summary>
    [HttpPut("{eventId:guid}/signups/{signupId:guid}/items/{itemId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSignUpItem(Guid eventId, Guid signupId, Guid itemId, [FromBody] UpdateSignUpItemRequest request)
    {
        Logger.LogInformation("Updating item {ItemId} in sign-up list {SignUpId} for event {EventId}",
            itemId, signupId, eventId);

        var command = new UpdateSignUpItemCommand(
            eventId,
            signupId,
            itemId,
            request.ItemDescription,
            request.Quantity,
            request.Notes);

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Remove an item from a category-based sign-up list (Event Organizer/Admin only)
    /// </summary>
    [HttpDelete("{eventId:guid}/signups/{signupId:guid}/items/{itemId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveSignUpItem(Guid eventId, Guid signupId, Guid itemId)
    {
        Logger.LogInformation("Removing item {ItemId} from sign-up list {SignUpId} for event {EventId}",
            itemId, signupId, eventId);

        var command = new RemoveSignUpItemCommand(eventId, signupId, itemId);
        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// User commits to bringing a specific item from a category-based sign-up list
    /// </summary>
    [HttpPost("{eventId:guid}/signups/{signupId:guid}/items/{itemId:guid}/commit")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CommitToSignUpItem(Guid eventId, Guid signupId, Guid itemId, [FromBody] CommitToSignUpItemRequest request)
    {
        Logger.LogInformation("User {UserId} committing to item {ItemId} in sign-up list {SignUpId} for event {EventId}",
            request.UserId, itemId, signupId, eventId);

        var command = new CommitToSignUpItemCommand(
            eventId,
            signupId,
            itemId,
            request.UserId,
            request.Quantity,
            request.Notes,
            request.ContactName,
            request.ContactEmail,
            request.ContactPhone);

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Check if an email has registered for an event (for sign-up validation)
    /// Phase 6A.15: Enhanced sign-up list UX with email validation
    /// Phase 6A.23: Updated to return detailed member/registration status for proper UX flow
    /// </summary>
    [HttpPost("{eventId:guid}/check-registration")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(EventRegistrationCheckResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CheckEventRegistrationByEmail(Guid eventId, [FromBody] CheckRegistrationRequest request)
    {
        Logger.LogInformation("Checking if email {Email} is registered for event {EventId}", request.Email, eventId);

        var query = new CheckEventRegistrationQuery(eventId, request.Email);
        var result = await Mediator.Send(query);

        return HandleResult(result);
    }

    /// <summary>
    /// Anonymous user commits to bringing a specific item from a category-based sign-up list
    /// Phase 6A.23: Supports anonymous sign-up workflow
    /// Flow: Check member status → Check event registration → Allow/Deny commitment
    /// </summary>
    [HttpPost("{eventId:guid}/signups/{signupId:guid}/items/{itemId:guid}/commit-anonymous")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CommitToSignUpItemAnonymous(
        Guid eventId,
        Guid signupId,
        Guid itemId,
        [FromBody] CommitToSignUpItemAnonymousRequest request)
    {
        Logger.LogInformation("Anonymous user with email {Email} committing to item {ItemId} in sign-up list {SignUpId} for event {EventId}",
            request.ContactEmail, itemId, signupId, eventId);

        var command = new CommitToSignUpItemAnonymousCommand(
            eventId,
            signupId,
            itemId,
            request.ContactEmail,
            request.Quantity,
            request.Notes,
            request.ContactName,
            request.ContactPhone);

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    #endregion

    #region Open Sign-Up Items (Phase 6A.27)

    /// <summary>
    /// Add a user-submitted Open item to a sign-up list
    /// Phase 6A.27: Allows authenticated users to add their own items to Open sign-up lists
    /// The user who creates the item is automatically committed to bringing it
    /// </summary>
    [HttpPost("{eventId:guid}/signups/{signupId:guid}/open-items")]
    [Authorize]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddOpenSignUpItem(
        Guid eventId,
        Guid signupId,
        [FromBody] AddOpenSignUpItemRequest request)
    {
        var userId = User.GetUserId();
        Logger.LogInformation("User {UserId} adding Open item '{ItemName}' to sign-up list {SignUpId} for event {EventId}",
            userId, request.ItemName, signupId, eventId);

        var command = new AddOpenSignUpItemCommand(
            eventId,
            signupId,
            userId,
            request.ItemName,
            request.Quantity,
            request.Notes,
            request.ContactName,
            request.ContactEmail,
            request.ContactPhone);

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Update a user-submitted Open item
    /// Phase 6A.27: Allows users to update their own Open items
    /// </summary>
    [HttpPut("{eventId:guid}/signups/{signupId:guid}/open-items/{itemId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOpenSignUpItem(
        Guid eventId,
        Guid signupId,
        Guid itemId,
        [FromBody] UpdateOpenSignUpItemRequest request)
    {
        var userId = User.GetUserId();
        Logger.LogInformation("User {UserId} updating Open item {ItemId} in sign-up list {SignUpId} for event {EventId}",
            userId, itemId, signupId, eventId);

        var command = new UpdateOpenSignUpItemCommand(
            eventId,
            signupId,
            itemId,
            userId,
            request.ItemName,
            request.Quantity,
            request.Notes,
            request.ContactName,
            request.ContactEmail,
            request.ContactPhone);

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    /// <summary>
    /// Cancel (delete) a user-submitted Open item
    /// Phase 6A.27: Allows users to cancel their own Open items
    /// </summary>
    [HttpDelete("{eventId:guid}/signups/{signupId:guid}/open-items/{itemId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelOpenSignUpItem(
        Guid eventId,
        Guid signupId,
        Guid itemId)
    {
        var userId = User.GetUserId();
        Logger.LogInformation("User {UserId} canceling Open item {ItemId} in sign-up list {SignUpId} for event {EventId}",
            userId, itemId, signupId, eventId);

        var command = new CancelOpenSignUpItemCommand(
            eventId,
            signupId,
            itemId,
            userId);

        var result = await Mediator.Send(command);

        return HandleResult(result);
    }

    #endregion

    #endregion
}

// Request DTOs
public record CancelEventRequest(string Reason);
public record PostponeEventRequest(string Reason);
// Phase 6A.11: Updated to support multi-attendee registrations with detailed attendee information
public record RsvpRequest(
    Guid UserId,
    // Legacy format (backward compatibility)
    int Quantity = 1,
    // New format (Session 21 - multi-attendee)
    List<LankaConnect.Application.Events.Commands.RsvpToEvent.AttendeeDto>? Attendees = null,
    // Contact information (new format only)
    string? Email = null,
    string? PhoneNumber = null,
    string? Address = null,
    // Session 23: Payment integration - URLs for Stripe Checkout redirect
    string? SuccessUrl = null,
    string? CancelUrl = null
);

// Phase 6A.11: AttendeeDto is imported from Application layer (RsvpToEvent namespace)

/// <summary>
/// Phase 6A.43: Updated to support multi-attendee format with AgeCategory/Gender
/// Supports both legacy format (Name/Age) and new format (Attendees array)
/// </summary>
public record AnonymousRegistrationRequest(
    // Legacy format fields (backward compatibility)
    string? Name = null,
    int? Age = null,
    // New format (Phase 6A.43 - multi-attendee with AgeCategory/Gender)
    List<AnonymousAttendeeDto>? Attendees = null,
    // Contact information (required)
    string Address = "",
    string Email = "",
    string PhoneNumber = "",
    // Quantity for multiple attendees
    int Quantity = 1);

/// <summary>
/// Attendee DTO for anonymous registration
/// </summary>
public record AnonymousAttendeeDto(
    string Name,
    LankaConnect.Domain.Events.Enums.AgeCategory AgeCategory,
    LankaConnect.Domain.Events.Enums.Gender? Gender = null);
public record UpdateRsvpRequest(Guid UserId, int NewQuantity);
/// <summary>
/// Phase 6A.14: Request to update registration details
/// </summary>
public record UpdateRegistrationRequest(
    List<UpdateRegistrationAttendeeDto>? Attendees,
    string Email,
    string PhoneNumber,
    string? Address = null);
/// <summary>
/// Phase 6A.14: Attendee DTO for registration update
/// Phase 6A.43: Updated to use AgeCategory and Gender instead of Age
/// </summary>
public record UpdateRegistrationAttendeeDto(
    string Name,
    LankaConnect.Domain.Events.Enums.AgeCategory AgeCategory,
    LankaConnect.Domain.Events.Enums.Gender? Gender = null);
public record ApproveEventRequest(Guid ApprovedByAdminId);
public record RejectEventRequest(Guid RejectedByAdminId, string Reason);
public record EventReorderImagesRequest(Dictionary<Guid, int> NewOrders); // Epic 2 Phase 2
public record RecordShareRequest(string? Platform = null); // Epic 2: Social sharing tracking
public record AddPassRequest(
    string PassName,
    string PassDescription,
    decimal PriceAmount,
    Currency PriceCurrency,
    int TotalQuantity); // Event Pass management

public record AddSignUpListRequest(
    string Category,
    string Description,
    SignUpType SignUpType,
    List<string>? PredefinedItems = null); // Sign-up list management

// Category-Based Sign-Up Requests
public record CreateSignUpListRequest(
    string Category,
    string Description,
    bool HasMandatoryItems,
    bool HasPreferredItems,
    bool HasSuggestedItems,
    List<SignUpItemRequestDto> Items,
    bool HasOpenItems = false); // Phase 6A.28: Open Items support

public record CheckRegistrationRequest(string Email); // Phase 6A.15: Email validation for sign-ups

public record UpdateSignUpListRequest(
    string Category,
    string Description,
    bool HasMandatoryItems,
    bool HasPreferredItems,
    bool HasSuggestedItems,
    bool HasOpenItems = false); // Phase 6A.28: Open Items support

public record SignUpItemRequestDto(
    string ItemDescription,
    int Quantity,
    SignUpItemCategory ItemCategory,
    string? Notes = null);

public record AddSignUpItemRequest(
    string ItemDescription,
    int Quantity,
    SignUpItemCategory ItemCategory,
    string? Notes = null);

/// <summary>
/// Request to update a sign-up item
/// Phase 6A.14: Edit Sign-Up Item feature
/// </summary>
public record UpdateSignUpItemRequest(
    string ItemDescription,
    int Quantity,
    string? Notes = null);

/// <summary>
/// Request to commit to bringing an item
/// Phase 2: Added optional contact information
/// </summary>
public record CommitToSignUpItemRequest(
    Guid UserId,
    int Quantity,
    string? Notes = null,
    string? ContactName = null,
    string? ContactEmail = null,
    string? ContactPhone = null);

/// <summary>
/// Request for anonymous user to commit to bringing an item
/// Phase 6A.23: Supports anonymous sign-up workflow
/// Email is used to verify event registration and identify the anonymous user
/// </summary>
public record CommitToSignUpItemAnonymousRequest(
    string ContactEmail,
    int Quantity,
    string? Notes = null,
    string? ContactName = null,
    string? ContactPhone = null);

/// <summary>
/// Request to add a user-submitted Open item to a sign-up list
/// Phase 6A.27: Open sign-up items feature
/// </summary>
public record AddOpenSignUpItemRequest(
    string ItemName,
    int Quantity,
    string? Notes = null,
    string? ContactName = null,
    string? ContactEmail = null,
    string? ContactPhone = null);

/// <summary>
/// Request to update a user-submitted Open item
/// Phase 6A.27: Open sign-up items feature
/// </summary>
public record UpdateOpenSignUpItemRequest(
    string ItemName,
    int Quantity,
    string? Notes = null,
    string? ContactName = null,
    string? ContactEmail = null,
    string? ContactPhone = null);
